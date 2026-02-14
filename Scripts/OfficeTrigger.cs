using Godot;
using System;

public partial class OfficeTrigger : Area3D
{
	[Export] public Node3D ShopLookTarget; // I-drag dito ang tindahan o ang Tindera node
	private bool _isDone = false;

	public void _on_body_entered(Node body)
	{
		if (body is Player player && !_isDone)
		{
			_isDone = true;
			player.IsLocked = true;
			
			// Lilingon sa tindahan habang nagtatanong
			if (ShopLookTarget != null) 
				player.PanToTarget(ShopLookTarget.GlobalPosition, false, 30.0f, 0.0f);

			string[] lines = {
				"John: Ate may Cash Out kayo?",
				"Tindera: Wala!",
				"John: Ay sige pala te.",
				"John: Sunod na ako, wala naman palang Gcash dito."
			};

			var dialogue = GetNodeOrNull<DialogueManager>("/root/DialogueManager");
			if (dialogue != null)
			{
				dialogue.StartDialogue(lines, () => {
					player.IsLocked = false;
					player.ResetCamera();
				});
			}
		}
	}
}
