using Godot;
using System;

public partial class Player : CharacterBody3D
{
    [ExportGroup("Movement Settings")]
    [Export] public float Speed = 3.0f;
    [Export] public float SprintMultiplier = 1.7f;
    [Export] public float Sensitivity = 0.0015f; 
    [Export] public float VerticalSensitivityMultiplier = 0.7f;
    [Export] public float Acceleration = 10.0f;
    [Export] public float Friction = 15.0f;
    [Export] public float MouseSmoothing = 20.0f;

    [ExportGroup("Crouch Settings")]
    [Export] public float CrouchSpeed = 1.5f;
    [Export] public float CrouchHeight = 1.0f;
    [Export] public float DefaultHeight = 2.0f; // Matches your collision shape height
    [Export] public float CrouchLerpSpeed = 10.0f;

    [ExportGroup("Camera Dynamics")]
    [Export] public float BobFreq = 2.4f;
    [Export] public float BobAmp = 0.06f;
    [Export] public float IdleBobFreq = 1.0f; 
    [Export] public float IdleBobAmp = 0.02f;
    
    public bool IsLocked = false; 
    private float _tBob = 0.0f;
    private float _tIdle = 0.0f;
    private Camera3D _camera;
    private CollisionShape3D _collisionShape;

    private float _targetRotationY;
    private float _targetRotationX;
    private float _currentCameraHeight = 1.93f;

    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
        _collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
        Input.MouseMode = Input.MouseModeEnum.Captured;
        
        _targetRotationY = Rotation.Y;
        _targetRotationX = _camera.Rotation.X;

        AddToGroup("player");
        CallDeferred(nameof(StartSpawnDialogue));
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion && !IsLocked)
        {
            _targetRotationY -= mouseMotion.Relative.X * Sensitivity;
            _targetRotationX -= mouseMotion.Relative.Y * Sensitivity * VerticalSensitivityMultiplier;
            _targetRotationX = Mathf.Clamp(_targetRotationX, Mathf.DegToRad(-85), Mathf.DegToRad(85));
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float lerpWeight = (float)delta * MouseSmoothing;
        Rotation = new Vector3(Rotation.X, Mathf.LerpAngle(Rotation.Y, _targetRotationY, lerpWeight), Rotation.Z);
        _camera.Rotation = new Vector3(Mathf.LerpAngle(_camera.Rotation.X, _targetRotationX, lerpWeight), _camera.Rotation.Y, _camera.Rotation.Z);

        Vector3 velocity = Velocity;

        if (!IsOnFloor()) 
            velocity.Y -= gravity * (float)delta;

        if (IsLocked)
        {
            velocity.X = Mathf.Lerp(velocity.X, 0, (float)delta * Friction);
            velocity.Z = Mathf.Lerp(velocity.Z, 0, (float)delta * Friction);
            Velocity = velocity;
            MoveAndSlide();
            HandleCameraMovements(delta, false, Vector3.Zero, false);
            return;
        }

        // --- Crouch Logic ---
        bool isCrouching = Input.IsActionPressed("crouch") || Input.IsKeyPressed(Key.Ctrl);
        float targetHeight = isCrouching ? CrouchHeight : DefaultHeight;
        
        // Lerp the Collision Shape Height
        if (_collisionShape.Shape is BoxShape3D box)
        {
            Vector3 size = box.Size;
            size.Y = Mathf.Lerp(size.Y, targetHeight, (float)delta * CrouchLerpSpeed);
            box.Size = size;
        }

        // Add inside _PhysicsProcess in Player.cs
        // Inside Player.cs -> _PhysicsProcess
        if (Input.IsActionJustPressed("interact")) // Make sure "interact" is mapped to 'E' in Project Settings
        {
            // Try to find the RayCast3D we added to the Camera
            var ray = GetNodeOrNull<RayCast3D>("Camera3D/RayCast3D");
            
            if (ray != null && ray.IsColliding())
            {
                var collider = ray.GetCollider();
                if (collider is CafeInteractable station)
                {
                    station.OnInteract();
                }
            }
        }
        // Adjust Camera Base Height based on Crouch
        float targetCamHeight = isCrouching ? 0.9f : 1.93f; 
        _currentCameraHeight = Mathf.Lerp(_currentCameraHeight, targetCamHeight, (float)delta * CrouchLerpSpeed);

        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        bool isSprinting = Input.IsKeyPressed(Key.Shift) && !isCrouching;
        float targetSpeed = Speed;
        
        if (isCrouching) targetSpeed = CrouchSpeed;
        else if (isSprinting) targetSpeed = Speed * SprintMultiplier;

        if (direction != Vector3.Zero)
        {
            velocity.X = Mathf.Lerp(velocity.X, direction.X * targetSpeed, (float)delta * Acceleration);
            velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * targetSpeed, (float)delta * Acceleration);
        }
        else
        {
            velocity.X = Mathf.Lerp(velocity.X, 0, (float)delta * Friction);
            velocity.Z = Mathf.Lerp(velocity.Z, 0, (float)delta * Friction);
        }

        Velocity = velocity;
        MoveAndSlide();
        HandleCameraMovements(delta, isSprinting, direction, isCrouching);
    }

    private void HandleCameraMovements(double delta, bool isSprinting, Vector3 direction, bool isCrouching)
    {
        Vector3 pos = _camera.Position;

        if (direction != Vector3.Zero && IsOnFloor())
        {
            _tBob += (float)delta * Velocity.Length();
            float cFreq = isSprinting ? BobFreq * 1.4f : (isCrouching ? BobFreq * 0.7f : BobFreq);
            float cAmp = isSprinting ? BobAmp * 1.2f : (isCrouching ? BobAmp * 0.5f : BobAmp);
            
            pos.Y = Mathf.Lerp(pos.Y, _currentCameraHeight + Mathf.Sin(_tBob * cFreq) * cAmp, (float)delta * 5.0f);
            pos.X = Mathf.Lerp(pos.X, Mathf.Cos(_tBob * cFreq * 0.5f) * cAmp, (float)delta * 5.0f);
        }
        else
        {
            _tIdle += (float)delta;
            float breatheEffect = Mathf.Sin(_tIdle * IdleBobFreq) * IdleBobAmp;
            pos.Y = Mathf.Lerp(pos.Y, _currentCameraHeight + breatheEffect, (float)delta * 2.0f);
            pos.X = Mathf.Lerp(pos.X, 0.0f, (float)delta * 2.0f);
        }
        _camera.Position = pos;
    }

    private void HandleInteraction()
    {
        if (Input.IsActionJustPressed("interact"))
        {
            var ray = GetNodeOrNull<RayCast3D>("Camera3D/RayCast3D"); 
            
            if (ray != null && ray.IsColliding())
            {
                // CHANGE: Cast the collider to a Node so we can read its Name
                var collider = ray.GetCollider();
                
                if (collider is Node hitNode)
                {
                    GD.Print("Ray hit: " + hitNode.Name);
                }

                // Check if the hit object is our cafe interactable script
                if (collider is CafeInteractable workStation)
                {
                    workStation.OnInteract();
                }
            }
        }
    }

    // --- Helper for Dialogue Spawn ---
    private void StartSpawnDialogue()
    {
        var dialogue = GetNodeOrNull<DialogueManager>("/root/DialogueManager");
        if (dialogue == null) return;

        // Checks the name of the Scene's Root Node
        string sceneName = GetTree().CurrentScene.Name;

        if (sceneName == "map" || sceneName == "CafeWork" || sceneName == "cafework") 
        {
            // Plays the original Cafe dialogue
            PanToTarget(GlobalPosition + Transform.Basis.Z * -1.0f + Vector3.Down * 0.5f, true, 45.0f);
            dialogue.StartDialogue(new string[] { 
                "John: (Sigh) 11:30 PM na... wala pa ring reply. Lowbat pa yata ako.",
                "John: Galing talagang timing. Kailangan ko na makauwi bago mawalan ng masakyan."
            }, () => ResetCamera());
        }
        else if (sceneName == "Run" || sceneName == "map_night")
        {
            // Plays the Night walk dialogue
            dialogue.StartDialogue(new string[] { 
                "John: The fog is so thick tonight...",
                "John: I can barely see the road. I need to find the bus stop and get out of here."
            });
        }
    }

    public void PanToTarget(Vector3 targetGlobalPos, bool useZoom = false, float zoomFov = 30.0f, float yOffset = 0.0f)
    {
        Vector3 lookDir = (targetGlobalPos - GlobalPosition).Normalized();
        _targetRotationY = Mathf.Atan2(-lookDir.X, -lookDir.Z);
        _targetRotationX = 0.0f;

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
    public void LookLeftRight(float intensity = 0.5f, float duration = 0.4f)
    {
        float startRotation = _targetRotationY;
        Tween tween = GetTree().CreateTween();
        
        // Look Left
        tween.TweenProperty(this, "_targetRotationY", startRotation + intensity, duration).SetTrans(Tween.TransitionType.Sine);
        // Look Right
        tween.TweenProperty(this, "_targetRotationY", startRotation - intensity, duration * 2).SetTrans(Tween.TransitionType.Sine);
        // Return to Center
        tween.TweenProperty(this, "_targetRotationY", startRotation, duration).SetTrans(Tween.TransitionType.Sine);
    }

    
}