using Godot;

/// <summary>
/// Drop this on an Area3D near the entrance of any work scene.
/// Fires the shift-start monologue and optionally starts a FlickerLight.
/// Replaces CafeIntro.cs for GasStation.tscn (and any future work scene).
/// </summary>
public partial class WorkIntro : Area3D
{
    [Export] public string       ShiftLocation = "gas station";
    [Export] public int          TaskCount     = 3;
    [Export] public FlickerLight ExteriorLight;

    public bool HasStarted = false;

    public void _on_body_entered(Node body)
    {
        if (body is not Player player || HasStarted) return;

        HasStarted = true;
        player.IsLocked = true;

        ExteriorLight?.StartFlicker();

        var dm = GetNode<DialogueManager>("/root/DialogueManager");
        string[] lines = {
            $"John: Hay nako, panibagong shift sa {ShiftLocation}.",
            $"Manager (Internal): 'John! Tapusin mo yung {TaskCount} tasks bago ka umuwi!'",
            $"John: Sige po... {TaskCount} lang pala. Kayang kaya."
        };

        dm.StartDialogue(lines, () => player.IsLocked = false);
    }
}
