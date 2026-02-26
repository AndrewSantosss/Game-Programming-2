using Godot;
using System;

public partial class CafeGhostTrigger : Area3D
{
    [Export] public GhostlyFigure TargetGhost;
    private bool _triggered = false;

    public void _on_body_entered(Node body)
    {
        if (body.IsInGroup("player") && !_triggered)
        {
            if (TargetGhost != null)
            {
                _triggered = true;
                GD.Print("Ghost Triggered!"); 
                TargetGhost.TriggerAppearance();
            }
        }
    }
}