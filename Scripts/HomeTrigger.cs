using Godot;

/// <summary>
/// Place on a Bed or Bag Area3D in Home.tscn.
/// When the player enters the area the departure dialogue fires,
/// then a fade transitions to the next scene.
/// </summary>
public partial class HomeTrigger : Area3D
{
	[Export] public string   NextScene = SceneFlow.School;
	[Export] public string[] DepartureDialogue;

	public bool Used = false;

	public void _on_body_entered(Node body)
	{
		if (body is not Player player || Used) return;

		Used = true;
		player.IsLocked = true;

		string[] lines = (DepartureDialogue != null && DepartureDialogue.Length > 0)
			? DepartureDialogue
			: new string[] {
				"John: Alright... school muna ngayon.",
				"John: Tapos kape mamaya kasama si pre, tapos trabaho ng gabi sa gas station.",
                "John: Ang haba pa ng araw ko ngayon..."
			};

		var dm = GetNodeOrNull<DialogueManager>("/root/DialogueManager");
		if (dm != null)
			dm.StartDialogue(lines, () => dm.DoFade(() => GetTree().ChangeSceneToFile(NextScene)));
		else
			GetTree().ChangeSceneToFile(NextScene);
	}
}
