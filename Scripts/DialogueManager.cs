using Godot;
using System;

public partial class DialogueManager : CanvasLayer
{
    private Label _label;
    private Control _box;
    private string[] _currentLines;
    private int _index = 0;
    private Action _callback;

    public override void _Ready()
    {
        _box = GetNodeOrNull<Control>("ColorRect");
        _label = GetNodeOrNull<Label>("ColorRect/Label");
        
        if (_box != null) 
        {
            _box.Hide();

            // FIX: Only set the black box (ColorRect) to Full Rect
            if (_box is ColorRect rect)
            {
                rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                rect.OffsetLeft = 0;
                rect.OffsetTop = 0;
                rect.OffsetRight = 0;
                rect.OffsetBottom = 0;
            }
        }

        // RESTORE ORIGINAL HEIGHT:
        // We ensure the label doesn't inherit the full-screen stretch
        if (_label != null)
        {
            _label.SetAnchorsPreset(Control.LayoutPreset.TopLeft); // Reset from Full Rect if it changed

            _label.OffsetTop = 200.0f; 
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_box != null && _box.Visible)
        {
            // Progress dialogue if Spacebar OR "E" (interact) is pressed
            if (@event.IsActionPressed("ui_accept") || @event.IsActionPressed("interact") || 
               (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Space))
            {
                _index++;
                
                if (_index < _currentLines.Length)
                {
                    UpdateText();
                }
                else
                {
                    CloseDialogue();
                }
                
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public void StartDialogue(string[] lines, Action onComplete = null)
    {
        _currentLines = lines;
        _index = 0;
        _callback = onComplete;
        
        if (_box != null) _box.Show();
        UpdateText();
        
        var player = GetTree().Root.FindChild("Player", true, false) as Player;
        if (player != null) player.IsLocked = true;
    }

    private void UpdateText()
    {
        if (_label != null) _label.Text = _currentLines[_index];
    }

    private void CloseDialogue()
    {
        _box.Hide();
        var player = GetTree().Root.FindChild("Player", true, false) as Player;
        if (player != null) player.IsLocked = false;
        _callback?.Invoke();
    }

    public void DoFade(Action onMidPoint)
	{
		var box = GetNode<ColorRect>("ColorRect");
		box.Show();
		box.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		box.Color = new Color(0, 0, 0, 0);

		Tween tween = GetTree().CreateTween();
		// Fade to black and STAY black
		tween.TweenProperty(box, "color", new Color(0, 0, 0, 1), 1.2f);
		tween.TweenCallback(Callable.From(() => {
			onMidPoint?.Invoke();
		}));
	}

	public void ShowGameOver()
	{
		// Ensure we are fully black
		var box = GetNode<ColorRect>("ColorRect");
		box.Color = new Color(0, 0, 0, 1);
		box.Show();

		if (_label != null)
		{
			// Setup the final screen
			_label.Text = "GAME OVER\n\nThank you for playing!";
			_label.HorizontalAlignment = HorizontalAlignment.Center;
			_label.VerticalAlignment = VerticalAlignment.Center;
			
			// Center the text
			_label.SetAnchorsPreset(Control.LayoutPreset.Center);
			_label.GrowHorizontal = Control.GrowDirection.Both;
			_label.GrowVertical = Control.GrowDirection.Both;
			_label.Visible = true;
		}

		// Keep the game open so they can see the message
		// If you want it to close after 10 seconds, uncomment the line below:
		// GetTree().CreateTimer(10.0f).Timeout += () => GetTree().Quit();
}
}