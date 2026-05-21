using Godot;
using System;

public partial class EndingManager : Node3D
{
	[Export] public Node3D       StalkerSpawnPoint;
	[Export] public PackedScene  GhostScene;
	[Export] public AudioStreamPlayer3D HorrorStinger;

	public bool EndingTriggered = false;

	public void TriggerTheVanishing()
	{
		if (EndingTriggered) return;
		EndingTriggered = true;

		var player = GetTree().GetFirstNodeInGroup("player") as Player;
		var dm     = GetNodeOrNull<DialogueManager>("/root/DialogueManager");

		if (player == null || dm == null) return;

		player.IsLocked = true;

		dm.StartDialogue(new string[] {
			"John: There it is... finally.",
			"John: Wait... did I just hear footsteps behind me?",
            "John: ...Is someone there?"
		}, () => {
			Node3D ghost = GhostScene.Instantiate<Node3D>();
			GetTree().Root.AddChild(ghost);

			if (ghost is GhostlyFigure gf)
				gf.ShowForCutscene();
			else
				ghost.Show();

			if (StalkerSpawnPoint != null)
				ghost.GlobalPosition = StalkerSpawnPoint.GlobalPosition;
			else
				ghost.GlobalPosition = player.GlobalPosition + (player.Transform.Basis.Z * 2.5f);

			ghost.LookAt(player.GlobalPosition);
			player.PanToTarget(ghost.GlobalPosition, true, 30.0f);

			GetTree().CreateTimer(0.8f).Timeout += () => {
				HorrorStinger?.Play();
				player.HeadWiggle();

				dm.DoFade(() => dm.ShowGameOver());
			};
		});
	}

	public void _on_end_trigger_body_entered(Node3D body)
	{
		if (body.IsInGroup("player"))
			TriggerTheVanishing();
	}
}
