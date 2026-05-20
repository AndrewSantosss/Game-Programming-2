using Godot;

/// <summary>
/// Generic task interactable for any work scene (gas station, cafe, etc.).
/// Attach to an Area3D. Call OnInteract() from Player's raycast.
/// Replaces CafeInteractable.cs — update the script reference in your scene inspector.
/// </summary>
public partial class WorldInteractable : Area3D
{
    [Export] public GhostlyFigure StalkerEntity;
    [Export] public int TasksToComplete = 3;

    // Configurable dialogue per task — set in Inspector
    [Export] public string WorkingLine  = "John: Let me handle this... (Wait a moment)";
    [Export] public string Task1Done    = "John: One down. Two more to go.";
    [Export] public string Task2Done    = "John: Done. Wait... sino yung nakatayo sa labas?";
    [Export] public string FinalLine    = "John: Last task. I should close up and head home.";
    [Export] public string NextScene    = SceneFlow.Ending;
    [Export] public float  TaskDuration = 2.0f;
    [Export] public float  StalkerLingerTime = 4.0f;

    public int  CurrentTasks = 0;
    public bool IsBusy       = false;

    public void OnInteract()
    {
        if (IsBusy || CurrentTasks >= TasksToComplete) return;

        IsBusy = true;
        var dm = GetNode<DialogueManager>("/root/DialogueManager");
        dm.StartDialogue(new string[] { WorkingLine });

        GetTree().CreateTimer(TaskDuration).Timeout += () => {
            CurrentTasks++;

            if (CurrentTasks == 1)
            {
                dm.StartDialogue(new string[] { Task1Done }, () => IsBusy = false);
            }
            else if (CurrentTasks == 2)
            {
                StalkerEntity?.TriggerAppearance();
                dm.StartDialogue(new string[] { Task2Done }, () => {
                    IsBusy = false;
                    GetTree().CreateTimer(StalkerLingerTime).Timeout += () => {
                        if (IsInstanceValid(StalkerEntity))
                            StalkerEntity.Visible = false;
                    };
                });
            }
            else if (CurrentTasks >= TasksToComplete)
            {
                var player = GetTree().GetFirstNodeInGroup("player") as Player;
                if (player != null) player.IsLocked = true;

                dm.StartDialogue(new string[] { FinalLine }, () => {
                    dm.DoFade(() => GetTree().ChangeSceneToFile(NextScene));
                });
            }
        };
    }
}
