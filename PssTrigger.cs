using Godot;
using System;

public partial class PssTrigger : Area3D
{
	[Export] public Node3D ManNode; 
	private bool _used = false;

	public override void _Ready()
	{
		if (ManNode != null) ManNode.Hide();
	}

	public void _on_body_entered(Node body)
	{
		if (body is Player player && !_used && ManNode != null)
		{
			_used = true;
			player.IsLocked = true; // Still lock movement for the event
			var dm = GetNode<DialogueManager>("/root/DialogueManager");

			dm.StartDialogue(new string[] { "???: Psssst..." }, () => {
				Vector3 targetHeight = ManNode.GlobalPosition;
				ManNode.GlobalPosition = targetHeight + new Vector3(0, -2.5f, 0);
				ManNode.Show();

				// Removed PanToTarget - Player must look manually
				
				Tween manTween = GetTree().CreateTween();
				manTween.TweenProperty(ManNode, "global_position:y", targetHeight.Y, 0.15f).SetTrans(Tween.TransitionType.Back);
				
				GetTree().CreateTimer(0.15f).Timeout += () => {
					player.HeadWiggle();
					dm.StartDialogue(new string[] { "John: Sino yan?!" }, () => {
						Tween fadeMan = GetTree().CreateTween();
						fadeMan.TweenProperty(ManNode, "global_position:y", targetHeight.Y - 2.5f, 1.5f);
						fadeMan.TweenCallback(Callable.From(ManNode.Hide));

						player.ResetCamera();
						player.IsLocked = false;
					});
				};
			});
		}
	}
}
