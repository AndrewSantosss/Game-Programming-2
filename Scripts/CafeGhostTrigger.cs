using Godot;
using System;

public partial class CafeGhostTrigger : Area3D
{
    [Export] public GhostlyFigure TargetGhost;
    private bool _triggered = false;

    public void _on_body_entered(Node body)
    {
        // Check if the body is in the "player" group and we haven't triggered yet
        if (body.IsInGroup("player") && !_triggered)
        {
            if (TargetGhost != null)
            {
                _triggered = true;
                GD.Print("Ghost Triggered!"); 
                TargetGhost.TriggerAppearance();
            }
            else
            {
                GD.PushWarning("CafeGhostTrigger: TargetGhost is not assigned in the Inspector!");
            }
        }
    }
}