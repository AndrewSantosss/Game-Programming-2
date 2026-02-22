using Godot;
using System;

public partial class CafeGhostTrigger : Area3D
{
    [Export] public GhostlyFigure TargetGhost;
    private bool _triggered = false;

    public void _on_body_entered(Node body)
    {
        if (body is Player && !_triggered)
        {
            _triggered = true;
            TargetGhost?.TriggerAppearance();
        }
    }
}