using Godot;
using System;

public partial class Player : CharacterBody3D
{
	public const float Speed = 5.0f;

	[ExportGroup("Movement Settings")]
	[Export] public float MouseSensitivity = 0.3f;
	[Export] public float SprintMultiplier = 1.7f;
	[Export] public float Sensitivity = 0.0015f; 
	[Export] public float VerticalSensitivityMultiplier = 0.7f;
	[Export] public float Acceleration = 10.0f;
	[Export] public float Friction = 15.0f;
	[Export] public float MouseSmoothing = 20.0f;

	[ExportGroup("Crouch Settings")]
	[Export] public float CrouchSpeed = 1.5f;
	[Export] public float CrouchHeight = 1.0f;
	[Export] public float DefaultHeight = 2.0f; 
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
	private Node3D _trackTarget;
	private Vector3 _trackOffset = Vector3.Zero; // Added to store target tracking offset shifts

	private float _targetRotationY;
	private float _targetRotationX;
	private float _currentCameraHeight = 1.93f;

	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	private Node3D _neck;
	private float _headRotationX = 0.0f;
	private float _headRotationY = 0.0f;

	public override void _Ready()
	{
		_neck = GetNodeOrNull<Node3D>("Neck");
		_camera = GetNodeOrNull<Camera3D>("Neck/Camera3D") ?? GetNodeOrNull<Camera3D>("Camera3D");
		_collisionShape = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");

		Input.MouseMode = Input.MouseModeEnum.Captured;
		
		_headRotationY = Rotation.Y;
		if (_neck != null) _headRotationX = _neck.Rotation.X;
		
		_targetRotationY = _headRotationY;
		_targetRotationX = _headRotationX;

		CallDeferred(MethodName.StartSpawnDialogue);
	}

	public override void _Input(InputEvent @event)
	{
		if (IsLocked || _trackTarget != null) return;

		if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			_headRotationY -= mouseMotion.Relative.X * (MouseSensitivity / 100);
			_headRotationX -= mouseMotion.Relative.Y * (MouseSensitivity / 100) * VerticalSensitivityMultiplier;
			
			_headRotationX = Mathf.Clamp(_headRotationX, Mathf.DegToRad(-89), Mathf.DegToRad(89));

			Rotation = new Vector3(Rotation.X, _headRotationY, Rotation.Z);
			if (_neck != null)
			{
				_neck.Rotation = new Vector3(_headRotationX, _neck.Rotation.Y, _neck.Rotation.Z);
			}
			else if (_camera != null)
			{
				_camera.Rotation = new Vector3(_headRotationX, _camera.Rotation.Y, _camera.Rotation.Z);
			}

			_targetRotationY = _headRotationY;
			_targetRotationX = _headRotationX;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		// 1. --- Target Tracking System Logic ---
		if (_trackTarget != null && IsInstanceValid(_trackTarget))
		{
			// Fixed: Math target now includes the tracking spatial offset to prevent looking down at the floor
			Vector3 targetLookAtPos = _trackTarget.GlobalPosition + _trackOffset;
			Vector3 dir = (targetLookAtPos - _camera.GlobalPosition).Normalized();
			
			_targetRotationY = Mathf.Atan2(-dir.X, -dir.Z);
			float hLen = new Vector2(dir.X, dir.Z).Length();
			_targetRotationX = Mathf.Clamp(Mathf.Atan2(dir.Y, hLen), Mathf.DegToRad(-85), Mathf.DegToRad(85));
			
			SyncMouseAccumulators();
		}

		float lerpWeight = (float)delta * MouseSmoothing;
		Rotation = new Vector3(Rotation.X, Mathf.LerpAngle(Rotation.Y, _targetRotationY, lerpWeight), Rotation.Z);
		
		if (_neck != null)
			_neck.Rotation = new Vector3(Mathf.LerpAngle(_neck.Rotation.X, _targetRotationX, lerpWeight), _neck.Rotation.Y, _neck.Rotation.Z);
		else if (_camera != null)
			_camera.Rotation = new Vector3(Mathf.LerpAngle(_camera.Rotation.X, _targetRotationX, lerpWeight), _camera.Rotation.Y, _camera.Rotation.Z);

		Vector3 velocity = Velocity;

		if (!IsOnFloor()) 
			velocity.Y -= gravity * (float)delta;

		// 2. --- Interaction Locked / Sit Cutscene States ---
		if (IsLocked)
		{
			velocity.X = Mathf.Lerp(velocity.X, 0, (float)delta * Friction);
			velocity.Z = Mathf.Lerp(velocity.Z, 0, (float)delta * Friction);
			Velocity = velocity;
			MoveAndSlide();
			HandleCameraMovements(delta, false, Vector3.Zero, false);
			return;
		}

		// 3. --- Shape Transformation Crouch System ---
		bool isCrouching = Input.IsActionPressed("crouch") || Input.IsKeyPressed(Key.Ctrl);
		float targetHeight = isCrouching ? CrouchHeight : DefaultHeight;
		
		if (_collisionShape != null && _collisionShape.Shape is BoxShape3D box)
		{
			Vector3 size = box.Size;
			size.Y = Mathf.Lerp(size.Y, targetHeight, (float)delta * CrouchLerpSpeed);
			box.Size = size;
		}

		// 4. --- Active Action Raycast Handlers ---
		if (Input.IsActionJustPressed("interact"))
		{
			var ray = GetNodeOrNull<RayCast3D>("Neck/Camera3D/RayCast3D") ?? GetNodeOrNull<RayCast3D>("Camera3D/RayCast3D");
			if (ray != null && ray.IsColliding())
			{
				var collider = ray.GetCollider() as Node;
				if (collider != null)
				{
					if (collider is CafeInteractable station) station.OnInteract();
					else if (collider is CokeRefillInteractable coke) coke.OnInteract();
					else if (collider is WorldInteractable wi) wi.OnInteract();
					else if (FindInParents<BroomInteractable>(collider) is { } broomP) broomP.OnInteract();
					else if (FindInChildren<BroomInteractable>(collider) is { } broomC) broomC.OnInteract();
					else if (FindInParents<TrashSweepInteractable>(collider) is { } trashP) trashP.OnInteract();
					else if (FindInChildren<TrashSweepInteractable>(collider) is { } trashC) trashC.OnInteract();
				}
			}
		}

		float targetCamHeight = isCrouching ? 0.9f : 1.93f; 
		_currentCameraHeight = Mathf.Lerp(_currentCameraHeight, targetCamHeight, (float)delta * CrouchLerpSpeed);

		// 5. --- Ground Movement Vectors Processing ---
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
		if (_camera == null) return;
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

	public void SyncMouseAccumulators()
	{
		_headRotationY = Rotation.Y;
		_headRotationX = _neck != null ? _neck.Rotation.X : _camera.Rotation.X;
		_targetRotationY = _headRotationY;
		_targetRotationX = _headRotationX;
	}

	// Exposes tracking node target configurations with custom eye-level offsets
	public void TrackNode(Node3D target, Vector3 offset)
	{
		_trackTarget = target;
		_trackOffset = offset;
	}

	// Overload variant for backwards compatibility with baseline positions
	public void TrackNode(Node3D target)
	{
		TrackNode(target, Vector3.Zero);
	}

	public void StopTracking()
	{
		_trackTarget = null;
		_trackOffset = Vector3.Zero;
	}

	public void ForceLookAt(Vector3 targetPosition)
	{
		Vector3 lookDir = (targetPosition - (_camera != null ? _camera.GlobalPosition : GlobalPosition)).Normalized();
		_targetRotationY = Mathf.Atan2(-lookDir.X, -lookDir.Z);
		float hLen = new Vector2(lookDir.X, lookDir.Z).Length();
		_targetRotationX = Mathf.Clamp(Mathf.Atan2(lookDir.Y, hLen), Mathf.DegToRad(-85), Mathf.DegToRad(85));

		Rotation = new Vector3(Rotation.X, _targetRotationY, Rotation.Z);
		
		if (_neck != null)
			_neck.Rotation = new Vector3(_targetRotationX, _neck.Rotation.Y, _neck.Rotation.Z);
		else if (_camera != null)
			_camera.Rotation = new Vector3(_targetRotationX, _camera.Rotation.Y, _camera.Rotation.Z);

		SyncMouseAccumulators();
	}

	private void StartSpawnDialogue()
	{
		var dialogue = GetNodeOrNull<DialogueManager>("/root/DialogueManager");
		if (dialogue == null) return;

		string sceneFile = GetTree().CurrentScene.SceneFilePath;

		if (sceneFile == SceneFlow.Home)
		{
			PanToTarget(GlobalPosition + Transform.Basis.Z * -1.0f + Vector3.Down * 0.5f, true, 45.0f);
			dialogue.StartDialogue(new string[] {
				"John: (Yawn)... Isa na naman itong mahabang araw.",
                "John: School muna, tapos kape kasama si pre, tapos trabaho ng gabi sa gas station."
			}, () => ResetCamera());
		}
		else if (sceneFile == SceneFlow.FriendCafe)
		{
			PanToTarget(GlobalPosition + Transform.Basis.Z * -1.0f + Vector3.Down * 0.5f, true, 45.0f);
			dialogue.StartDialogue(new string[] {
				"John: (Sigh) 11:30 PM na... wala pa ring reply. Lowbat pa yata ako.",
                "John: Galing talagang timing. Kailangan ko na makauwi bago mawalan ng masakyan."
			}, () => ResetCamera());
		}
		else if (sceneFile == SceneFlow.Ending)
		{
			dialogue.StartDialogue(new string[] {
				"John: The fog is so thick tonight...",
                "John: I can barely see the road. I need to find the bus stop and get out of here."
			});
		}
	}

	public void PanToTarget(Vector3 targetGlobalPos, bool useZoom = false, float zoomFov = 30.0f, float yOffset = 0.0f)
	{
		Vector3 lookDir = (targetGlobalPos - (_camera != null ? _camera.GlobalPosition : GlobalPosition)).Normalized();
		_targetRotationY = Mathf.Atan2(-lookDir.X, -lookDir.Z);
		_targetRotationX = Mathf.Clamp(Mathf.Atan2(lookDir.Y, new Vector2(lookDir.X, lookDir.Z).Length()), Mathf.DegToRad(-85), Mathf.DegToRad(85));

		Tween tween = GetTree().CreateTween().SetParallel(true);
		tween.TweenProperty(this, "rotation:y", _targetRotationY, 0.5f).SetTrans(Tween.TransitionType.Sine);
		
		string targetPath = _neck != null ? "neck/rotation:x" : "camera_3d/rotation:x";
		tween.TweenProperty(_neck ?? (Node)_camera, "rotation:x", _targetRotationX, 0.5f).SetTrans(Tween.TransitionType.Sine);
		
		if (useZoom && _camera != null)
			tween.TweenProperty(_camera, "fov", zoomFov, 0.3f).SetTrans(Tween.TransitionType.Expo);

		tween.Finished += SyncMouseAccumulators;
	}

	public void ResetCamera()
	{
		_targetRotationX = 0.0f;
		Tween tween = GetTree().CreateTween().SetParallel(true);
		if (_camera != null) tween.TweenProperty(_camera, "fov", 75.0f, 0.5f).SetTrans(Tween.TransitionType.Sine);
		
		string targetPath = _neck != null ? "neck/rotation:x" : "camera_3d/rotation:x";
		tween.TweenProperty(_neck ?? (Node)_camera, "rotation:x", 0.0f, 0.5f).SetTrans(Tween.TransitionType.Sine);
		if (_camera != null) tween.TweenProperty(_camera, "position:y", _currentCameraHeight, 0.5f);

		tween.Finished += SyncMouseAccumulators;
	}

	public void SitAtTarget(Vector3 chairPos, Vector3 lookAtPos)
	{
		IsLocked = true;
		Tween tween = GetTree().CreateTween().SetParallel(true);
		tween.TweenProperty(this, "global_position", new Vector3(chairPos.X, GlobalPosition.Y, chairPos.Z), 1.0f);
		
		Vector3 lookDir = (lookAtPos - chairPos).Normalized();
		_targetRotationY = Mathf.Atan2(-lookDir.X, -lookDir.Z);
		_targetRotationX = Mathf.Clamp(Mathf.Atan2(lookDir.Y, new Vector2(lookDir.X, lookDir.Z).Length()), Mathf.DegToRad(-85), Mathf.DegToRad(85));
		
		tween.TweenProperty(this, "rotation:y", _targetRotationY, 1.0f);
		
		if (_neck != null) tween.TweenProperty(_neck, "rotation:x", _targetRotationX, 1.0f);
		else if (_camera != null) tween.TweenProperty(_camera, "rotation:x", _targetRotationX, 1.0f);
		
		if (_camera != null) tween.TweenProperty(_camera, "position:y", 1.0f, 1.0f);

		tween.Finished += () => {
			_currentCameraHeight = 1.0f;
			SyncMouseAccumulators();
		};
	}

	public void HeadWiggle()
	{
		if (_camera == null) return;
		Tween tween = GetTree().CreateTween();
		tween.TweenProperty(_camera, "rotation:z", Mathf.DegToRad(4), 0.04f);
		tween.TweenProperty(_camera, "rotation:z", Mathf.DegToRad(-4), 0.04f);
		tween.TweenProperty(_camera, "rotation:z", 0.0f, 0.04f);
	}

	public void LookLeftRight(float intensity = 0.5f, float duration = 0.4f)
	{
		float startRotation = _targetRotationY;
		Tween tween = GetTree().CreateTween();
		tween.TweenMethod(Callable.From<float>(v => _targetRotationY = v), startRotation, startRotation + intensity, duration).SetTrans(Tween.TransitionType.Sine);
		tween.TweenMethod(Callable.From<float>(v => _targetRotationY = v), startRotation + intensity, startRotation - intensity, duration * 2).SetTrans(Tween.TransitionType.Sine);
		tween.TweenMethod(Callable.From<float>(v => _targetRotationY = v), startRotation - intensity, startRotation, duration).SetTrans(Tween.TransitionType.Sine);
		tween.Finished += SyncMouseAccumulators;
	}

	public void CameraShake(float intensity = 0.4f, float duration = 0.6f)
	{
		if (_camera == null) return;
		Tween tween = GetTree().CreateTween();
		int steps = Mathf.Max(4, (int)(duration / 0.07f));
		float stepTime = duration / steps;
		for (int i = 0; i < steps; i++)
		{
			float decay = 1.0f - (float)i / steps;
			float angle = (i % 2 == 0 ? intensity : -intensity) * decay;
			tween.TweenProperty(_camera, "rotation:z", Mathf.DegToRad(angle), stepTime);
		}
		tween.TweenProperty(_camera, "rotation:z", 0.0f, stepTime).SetTrans(Tween.TransitionType.Sine);
	}

	private static T FindInParents<T>(Node node) where T : class
	{
		while (node != null)
		{
			if (node is T match) return match;
			node = node.GetParent();
		}
		return null;
	}

	private static T FindInChildren<T>(Node node) where T : class
	{
		if (node == null) return null;
		if (node is T match) return match;
		
		foreach (var child in node.GetChildren())
		{
			var result = FindInChildren<T>(child);
			if (result != null) return result;
		}
		return null;
	}
}
