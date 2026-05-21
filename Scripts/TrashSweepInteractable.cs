using Godot;
using System;

// Final task of the cafe shift: sweep the trash area.
// Extends Node3D so it can be attached directly to a GLB model instance.
// Activated by BroomInteractable. After sweeping, plays the ending dialogue
// and fades to the next scene.
public partial class TrashSweepInteractable : Node3D
{
	[Export] public string NextScene = SceneFlow.Ending;

	private bool _isActive = false;
	public bool IsActive
	{
		get => _isActive;
		set => _isActive = value;
	}

	private bool _done = false;
	private bool _isBusy = false;

	public void OnInteract()
	{
		if (!_isActive || _isBusy || _done) return;

		_isBusy = true;

		var dm = GetNode<DialogueManager>("/root/DialogueManager");
		dm.StartDialogue(new string[] { "John: Winalisan na yung basura... (sweeping)" });

		GetTree().CreateTimer(2.0f).Timeout += () => {
			_done = true;

			var player = GetTree().GetFirstNodeInGroup("player") as Player;
			if (player != null) player.IsLocked = true;

			// INAYOS: Tinanggal ang TodoManager sa loob ng string array
			dm.StartDialogue(new string[] {
				"John: FINALLY DONE NA! Sige na, uwi na ako. Nakakaod naman itong trabaho..."
			}, () => {
				// INAYOS: Dito sa loob ng callback ipinasok ang pag-update ng To-Do list
				GetNode<TodoManager>("/root/TodoManager").UpdateTodoText("Go Home");

				dm.DoFade(() => GetTree().ChangeSceneToFile(NextScene));
			});
		};
	}
}
