using Godot;

/// <summary>
/// Generic area trigger that reveals a GhostlyFigure and optionally fires dialogue.
/// Replaces CafeGhostTrigger.cs — update the script reference in your scene inspector.
/// </summary>
public partial class SuspenseTrigger : Area3D
{
    [Export] public GhostlyFigure TargetGhost;
    [Export] public string[]      TriggerDialogue;

    public bool Triggered = false;

    public void _on_body_entered(Node body)
    {
        if (!body.IsInGroup("player") || Triggered || TargetGhost == null) return;

        Triggered = true;
        TargetGhost.TriggerAppearance();

        if (TriggerDialogue != null && TriggerDialogue.Length > 0)
        {
            var dm = GetNodeOrNull<DialogueManager>("/root/DialogueManager");
            dm?.StartDialogue(TriggerDialogue);
        }
    }
}
