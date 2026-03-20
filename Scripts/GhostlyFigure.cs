using Godot;
using System;

public partial class GhostlyFigure : Node3D
{
    [Export] public float DetectionThreshold = 0.98f; // How "centered" the crosshair must be
    private Camera3D _playerCamera;
    private bool _isActive = false;

    public override void _Ready()
    {
        Hide(); // <--- This line makes it invisible at the start!
        _playerCamera = GetViewport().GetCamera3D();
    }

    public override void _Process(double delta)
    {
        if (!_isActive) return;

        // Calculate direction from camera to ghost
        Vector3 directionToGhost = (GlobalPosition - _playerCamera.GlobalPosition).Normalized();
        // Calculate camera's forward looking direction
        Vector3 cameraForward = -_playerCamera.GlobalTransform.Basis.Z;

        // Dot product checks how much the two vectors align (1.0 is a perfect look-at)
        float lookAlignment = cameraForward.Dot(directionToGhost);

        if (lookAlignment > DetectionThreshold)
        {
            OnSpotted();
        }
    }

    public void TriggerAppearance()
    {
        Show();
        _isActive = true;
    }

    private void OnSpotted()
    {
        _isActive = false;
        // Jumpscare or disappearance logic
        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(this, "scale", Vector3.Zero, 0.2f);
        tween.TweenCallback(Callable.From(() => QueueFree()));
    }
}