using Godot;

// Order 1 of the cafe shift: make a coffee at the counter.
// After completing, activates the CokeRefill interactable at the back.
public partial class CafeInteractable : Area3D
{
	[Export] public CokeRefillInteractable CokeRefill;

	private bool _done = false;
	private bool _isBusy = false;
	private bool _playerInRange = false;
	private Label _promptLabel;

	public override void _Ready()
	{
		_promptLabel = GetNodeOrNull<Label>("Label");
		if (_promptLabel != null) _promptLabel.Visible = false;

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is Player) { _playerInRange = true; UpdateLabel(); }
	}

	private void OnBodyExited(Node3D body)
	{
		if (body is Player) { _playerInRange = false; UpdateLabel(); }
	}

	private void UpdateLabel()
	{
		if (_promptLabel != null)
			_promptLabel.Visible = _playerInRange && !_isBusy && !_done;
	}

	public void OnInteract()
	{
		if (_isBusy || _done) return;

		_isBusy = true;
		UpdateLabel();

		var dm = GetNode<DialogueManager>("/root/DialogueManager");
		dm.StartDialogue(new string[] { "John: Making the coffee... (Stay here for 2 seconds)" });

		GetTree().CreateTimer(2.0f).Timeout += () => {
			_done = true;
			dm.StartDialogue(new string[] {
				"John: One down. Hmm, yung coke sa likod kailangan i-refill. Punta ka doon."
			}, () => {
				_isBusy = false;
				UpdateLabel();
				if (CokeRefill != null) CokeRefill.IsActive = true;
				
				// INAYOS: Tamang pagtawag sa TodoManager pagkatapos ng dialogue box
				GetNode<TodoManager>("/root/TodoManager").UpdateTodoText("Refill Coke");
			});
		}; // INAYOS: Tinanggal ang mga sumobrang bracket at semicolon dito
	}
}
