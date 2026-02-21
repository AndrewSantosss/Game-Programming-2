
public partial class Player : CharacterBody3D
{
    [ExportGroup("Movement Settings")]
    [Export] public float Speed = 3.0f;
    [Export] public float SprintMultiplier = 1.7f;
    [Export] public float Sensitivity = 0.0015f; 
    [Export] public float VerticalSensitivityMultiplier = 0.7f;
    
    // Smoothing Factors
    [Export] public float Acceleration = 10.0f;
    [Export] public float Friction = 15.0f;
    [Export] public float MouseSmoothing = 20.0f;

    [ExportGroup("Camera Dynamics")]
    [Export] public float BobFreq = 2.4f;
    [Export] public float BobAmp = 0.06f;
    [Export] public float IdleBobFreq = 1.0f; 
    [Export] public float IdleBobAmp = 0.02f;
    
    public bool IsLocked = false; 
    private float _tBob = 0.0f;
    private float _tIdle = 0.0f;
    private Camera3D _camera;

    // Values for smoothing mouse rotation
    private float _targetRotationY;
    private float _targetRotationX;

    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
        Input.MouseMode = Input.MouseModeEnum.Captured;
        
        // Initialize rotation targets to current rotation
        _targetRotationY = Rotation.Y;
        _targetRotationX = _camera.Rotation.X;

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
            // Set target rotations based on relative mouse movement
            _targetRotationY -= mouseMotion.Relative.X * Sensitivity;
            _targetRotationX -= mouseMotion.Relative.Y * Sensitivity * VerticalSensitivityMultiplier;
            _targetRotationX = Mathf.Clamp(_targetRotationX, Mathf.DegToRad(-85), Mathf.DegToRad(85));
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // 1. Smooth Mouse Movement
        // Interpolate current rotation toward the target rotation for a silky feel
        float lerpWeight = (float)delta * MouseSmoothing;
        Rotation = new Vector3(Rotation.X, Mathf.LerpAngle(Rotation.Y, _targetRotationY, lerpWeight), Rotation.Z);
        _camera.Rotation = new Vector3(Mathf.LerpAngle(_camera.Rotation.X, _targetRotationX, lerpWeight), _camera.Rotation.Y, _camera.Rotation.Z);

        Vector3 velocity = Velocity;

        // 2. Gravity
        if (!IsOnFloor()) 
            velocity.Y -= gravity * (float)delta;

        // 3. Movement Logic
        if (IsLocked)
        {
            // Smoothly slide to a stop when talking
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
        float targetSpeed = isSprinting && direction != Vector3.Zero ? Speed * SprintMultiplier : Speed;

        if (direction != Vector3.Zero)
        {
            // Smooth Acceleration
            velocity.X = Mathf.Lerp(velocity.X, direction.X * targetSpeed, (float)delta * Acceleration);
            velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * targetSpeed, (float)delta * Acceleration);
        }
        else
        {
            // Smooth Friction (Deceleration)
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
            float cFreq = isSprinting ? BobFreq * 1.4f : BobFreq;
            float cAmp = isSprinting ? BobAmp * 1.2f : BobAmp;
            
            // Interpolate the head bob so it doesn't snap when starting/stopping
            pos.Y = Mathf.Lerp(pos.Y, defaultHeight + Mathf.Sin(_tBob * cFreq) * cAmp, (float)delta * 10.0f);
            pos.X = Mathf.Lerp(pos.X, Mathf.Cos(_tBob * cFreq * 0.5f) * cAmp, (float)delta * 10.0f);
        }
        else
        {
            _tIdle += (float)delta;
            float breatheEffect = Mathf.Sin(_tIdle * IdleBobFreq) * IdleBobAmp;
            
            // Return to center smoothly when idle
            pos.Y = Mathf.Lerp(pos.Y, defaultHeight + breatheEffect, (float)delta * 5.0f);
            pos.X = Mathf.Lerp(pos.X, 0.0f, (float)delta * 5.0f);
        }
        _camera.Position = pos;
    }

    // --- Utility Methods ---

    public void PanToTarget(Vector3 targetGlobalPos, bool useZoom = false, float zoomFov = 30.0f, float yOffset = 0.0f)
    {
        Vector3 lookDir = (targetGlobalPos - GlobalPosition).Normalized();
        _targetRotationY = Mathf.Atan2(-lookDir.X, -lookDir.Z);
        _targetRotationX = 0.0f; // Look straight ahead

        Tween tween = GetTree().CreateTween().SetParallel(true);
        tween.TweenProperty(this, "rotation:y", _targetRotationY, 0.5f).SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(_camera, "rotation:x", 0.0f, 0.5f).SetTrans(Tween.TransitionType.Sine);
        
        if (useZoom)
            tween.TweenProperty(_camera, "fov", zoomFov, 0.3f).SetTrans(Tween.TransitionType.Expo);
    }

    public void ResetCamera()
    {
        _targetRotationX = 0.0f;
        Tween tween = GetTree().CreateTween().SetParallel(true);
        tween.TweenProperty(_camera, "fov", 75.0f, 0.5f).SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(_camera, "rotation:x", 0.0f, 0.5f).SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(_camera, "position:y", 1.93f, 0.5f);
    }

    public void SitAtTarget(Vector3 chairPos, Vector3 lookAtPos)
    {
        IsLocked = true;
        Tween tween = GetTree().CreateTween().SetParallel(true);
        tween.TweenProperty(this, "global_position", new Vector3(chairPos.X, GlobalPosition.Y, chairPos.Z), 1.0f);
        
        Vector3 lookDir = (lookAtPos - chairPos).Normalized();
        _targetRotationY = Mathf.Atan2(-lookDir.X, -lookDir.Z);
        _targetRotationX = 0.0f;
        
        tween.TweenProperty(this, "rotation:y", _targetRotationY, 1.0f);
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
}