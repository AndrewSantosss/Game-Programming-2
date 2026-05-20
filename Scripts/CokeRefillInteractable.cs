using Godot;

public partial class CokeRefillInteractable : Area3D
{
    [Export] public GhostlyFigure StalkerAtWindow;
    [Export] public BroomInteractable Broom;
    [Export] public float StalkerLingerTime = 4.0f;
    
    private bool _isActive = false;
    [Export] public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; UpdateLabel(); }
    }

    private bool _done = false;
    private bool _isBusy = false;
    private bool _playerInRange = false;
    private Label _promptLabel;

    public override void _Ready()
    {
        // Fixed: Cleaned up the path and double newline syntax issue
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
            _promptLabel.Visible = _playerInRange && _isActive && !_isBusy && !_done;
    }

    public void OnInteract()
    {
        if (!_isActive || _isBusy || _done) return;

        _isBusy = true;
        UpdateLabel();

        // Fixed: Removed backslashes from strings and Autoload paths
        var dm = GetNode<DialogueManager>("/root/DialogueManager");
        dm.StartDialogue(new string[] { "John: Refilling the coke... (Stay here for 2 seconds)" });

        GetTree().CreateTimer(2.0f).Timeout += () => {
            _done = true;
            if (IsInstanceValid(StalkerAtWindow))
                StalkerAtWindow.TriggerAppearance();
                
            dm.StartDialogue(new string[] { "John: Done. Wait... sino yung nakatayo sa labas?" }, () => {
                _isBusy = false;
                UpdateLabel();
                
                GetTree().CreateTimer(StalkerLingerTime).Timeout += () => {
                    if (IsInstanceValid(StalkerAtWindow))
                        StalkerAtWindow.Visible = false;
                        
                    var dm2 = GetNode<DialogueManager>("/root/DialogueManager");
                    dm2.StartDialogue(new string[] { "John: Hm. Sige na lang. Kumuha na ko ng walis." }, () => {
                        if (Broom != null) Broom.IsActive = true;
                    });
                };
            });
        };
    }
}