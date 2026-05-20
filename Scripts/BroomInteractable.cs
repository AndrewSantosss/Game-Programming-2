using Godot;

public partial class BroomInteractable : Area3D
{
    [Export] public TrashSweepInteractable Trash;
    [Export] public Node3D BroomVisual; 
    
    private bool _isActive = false;
    [Export] public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; UpdateLabel(); }
    }

    private bool _done = false;
    private bool _playerInRange = false;
    private Label _promptLabel;

    public override void _Ready()
    {
        // Fixed: Removed the backslash typo
        _promptLabel = GetNodeOrNull<Label>("Label");
        if (_promptLabel != null) _promptLabel.Visible = false;

        BodyEntered += (body) => { if (body is Player) { _playerInRange = true; UpdateLabel(); } };
        BodyExited += (body) => { if (body is Player) { _playerInRange = false; UpdateLabel(); } };
    }

    private void UpdateLabel()
    {
        if (_promptLabel != null)
            _promptLabel.Visible = _playerInRange && _isActive && !_done;
    }

    public void OnInteract()
    {
        if (!_isActive || _done) return;

        _done = true;
        UpdateLabel();

        if (BroomVisual != null) BroomVisual.Visible = false;

        // Fixed: Added missing semicolon and removed backslash typos
        var dm = GetNode<DialogueManager>("/root/DialogueManager");
        dm.StartDialogue(new string[] { "John: (Kinuha ang walis.) Sige, punta na sa basura." }, () => {
            if (Trash != null) Trash.IsActive = true;
        });
    }
}