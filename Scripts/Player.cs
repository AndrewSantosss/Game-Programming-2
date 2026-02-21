
public partial class Player : CharacterBody3D
{
    [Export] public float Speed = 3.0f;
    [Export] public float SprintMultiplier = 1.7f;
    [Export] public float Sensitivity = 0.0015f;
    [Export] public float VerticalSensitivityMultiplier = 0.5f;
    [Export] public float Acceleration = 10.0f;
    [Export] public float Friction = 15.0f;
    
    public bool IsLocked = false; 

    // Camera Dynamics (Head Bob & Idle)
    [Export] public float BobFreq = 2.4f;
    [Export] public float BobAmp = 0.06f;
    [Export] public float IdleBobFreq = 1.0f; 
    [Export] public float IdleBobAmp = 0.02f;
    
    private float _tBob = 0.0f;
    private float _tIdle = 0.0f;

    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    private Camera3D _camera;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
        Input.MouseMode = Input.MouseModeEnum.Captured;
        AddToGroup("player");
        CallDeferred(nameof(StartSpawnDialogue));
    }

    private void StartSpawnDialogue()
    {
        var dialogue = GetNodeOrNull<DialogueManager>("/root/DialogueManager");
        if (dialogue != null)
        {
            PanToTarget(GlobalPosition + Transform.Basis.Z * -3.0f, true, 30.0f, 0.0f);
            dialogue.StartDialogue(new string[] { 
                "Sa wakas natapos din yung practice na yan, ginabi na ako...",
                "Makakapag pahinga na rin ako."
            }, () => ResetCamera());
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion && !IsLocked)
        {
            RotateY(-mouseMotion.Relative.X * Sensitivity);
            Vector3 camRot = _camera.Rotation;
            float vSens = Sensitivity * VerticalSensitivityMultiplier;
            camRot.X = Mathf.Clamp(camRot.X - mouseMotion.Relative.Y * vSens, Mathf.DegToRad(-85), Mathf.DegToRad(85));
            _camera.Rotation = camRot;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        if (!IsOnFloor()) 
            velocity.Y -= gravity * (float)delta;

        if (IsLocked)
        {
            velocity.X = Mathf.Lerp(velocity.X, 0, (float)delta * Friction);
            velocity.Z = Mathf.Lerp(velocity.Z, 0, (float)delta * Friction);
            Velocity = velocity;
            MoveAndSlide();
            HandleCameraMovements(delta, false, Vector3.Zero);
            return;
        }

        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        bool isSprinting = Input.IsKeyPressed(Key.Shift);
        float currentSpeed = isSprinting && direction != Vector3.Zero ? Speed * SprintMultiplier : Speed;

        if (direction != Vector3.Zero)
        {
            velocity.X = Mathf.Lerp(velocity.X, direction.X * currentSpeed, (float)delta * Acceleration);
            velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * currentSpeed, (float)delta * Acceleration);
        }
        else
        {
            velocity.X = Mathf.Lerp(velocity.X, 0, (float)delta * Friction);
            velocity.Z = Mathf.Lerp(velocity.Z, 0, (float)delta * Friction);
        }

        Velocity = velocity;
        MoveAndSlide();
        HandleCameraMovements(delta, isSprinting, direction);
    }

    private void HandleCameraMovements(double delta, bool isSprinting, Vector3 direction)
    {
        Vector3 pos = _camera.Position;
        float defaultHeight = 1.93f; 

        if (direction != Vector3.Zero && IsOnFloor())
        {
            _tBob += (float)delta * Velocity.Length();
            float cFreq = isSprinting ? BobFreq * 1.5f : BobFreq;
            float cAmp = isSprinting ? BobAmp * 1.3f : BobAmp;
            pos.Y = Mathf.Lerp(pos.Y, defaultHeight + Mathf.Sin(_tBob * cFreq) * cAmp, (float)delta * 10.0f);
            pos.X = Mathf.Lerp(pos.X, Mathf.Cos(_tBob * cFreq * 0.5f) * cAmp, (float)delta * 10.0f);
        }
        else
        {
            _tIdle += (float)delta;
            float breatheEffect = Mathf.Sin(_tIdle * IdleBobFreq) * IdleBobAmp;
            pos.Y = Mathf.Lerp(pos.Y, defaultHeight + breatheEffect, (float)delta * 5.0f);
            pos.X = Mathf.Lerp(pos.X, 0.0f, (float)delta * 5.0f);
        }
        _camera.Position = pos;
    }

    public void PanToTarget(Vector3 targetGlobalPos, bool useZoom = false, float zoomFov = 30.0f, float yOffset = 0.0f)
    {
        Vector3 lookDir = (targetGlobalPos - GlobalPosition).Normalized();
        float targetRotY = Mathf.Atan2(-lookDir.X, -lookDir.Z);

        Tween tween = GetTree().CreateTween().SetParallel(true);
        tween.TweenProperty(this, "rotation:y", targetRotY, 0.5f).SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(_camera, "rotation:x", 0.0f, 0.5f).SetTrans(Tween.TransitionType.Sine);
        
        if (useZoom)
            tween.TweenProperty(_camera, "fov", zoomFov, 0.3f).SetTrans(Tween.TransitionType.Expo);
    }

    public void SitAtTarget(Vector3 chairPos, Vector3 lookAtPos)
    {
        IsLocked = true;
        Tween tween = GetTree().CreateTween().SetParallel(true);
        tween.TweenProperty(this, "global_position", new Vector3(chairPos.X, GlobalPosition.Y, chairPos.Z), 1.0f);
        Vector3 lookDir = (lookAtPos - chairPos).Normalized();
        float targetRotY = Mathf.Atan2(-lookDir.X, -lookDir.Z);
        tween.TweenProperty(this, "rotation:y", targetRotY, 1.0f);
        tween.TweenProperty(_camera, "rotation:x", 0.0f, 1.0f);
        tween.TweenProperty(_camera, "position:y", 1.0f, 1.0f);
    }

    public void HeadWiggle()
    {
        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(_camera, "rotation:z", Mathf.DegToRad(4), 0.04f);
        tween.TweenProperty(_camera, "rotation:z", Mathf.DegToRad(-4), 0.04f);
        tween.TweenProperty(_camera, "rotation:z", 0.0f, 0.04f);
    }

    public void ResetCamera()
    {
        Tween tween = GetTree().CreateTween().SetParallel(true);
        tween.TweenProperty(_camera, "fov", 75.0f, 0.5f).SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(_camera, "rotation:x", 0.0f, 0.5f).SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(_camera, "position:y", 1.93f, 0.5f);
    }
}