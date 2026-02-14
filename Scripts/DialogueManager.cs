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
		// Siguraduhing "ColorRect" at "Label" ang pangalan ng nodes sa .tscn mo
		_box = GetNodeOrNull<Control>("ColorRect");
		_label = GetNodeOrNull<Label>("ColorRect/Label");
		
		if (_box != null) _box.Hide();
	}

	public void StartDialogue(string[] lines, Action onComplete = null)
	{
		_currentLines = lines;
		_index = 0;
		_callback = onComplete;
		
		if (_box != null) _box.Show();
		UpdateText();
		
		// I-lock ang player movement
		var player = GetTree().Root.FindChild("Player", true, false) as Player;
		if (player != null) player.IsLocked = true;
	}

	private void UpdateText()
	{
		if (_label != null) _label.Text = _currentLines[_index];
	}
	public void DoFade(Action onMidPoint)
{
	// Gamitin ang ColorRect node mo
	var box = GetNode<ColorRect>("ColorRect");
	box.Show();
	box.Color = new Color(0, 0, 0, 0);

	Tween tween = GetTree().CreateTween();
	tween.TweenProperty(box, "color", new Color(0, 0, 0, 1), 0.5f);
	tween.TweenCallback(Callable.From(() => {
		onMidPoint?.Invoke();
	}));
	tween.TweenProperty(box, "color", new Color(0, 0, 0, 0), 0.5f);
}

	public override void _Input(InputEvent @event)
	{
		if (_box != null && _box.Visible && @event.IsActionPressed("ui_accept"))
		{
			_index++;
			if (_index < _currentLines.Length) 
			{
				UpdateText();
			}
			else 
			{
				_box.Hide();
				
				// Unlock player movement
				var player = GetTree().Root.FindChild("Player", true, false) as Player;
				if (player != null) player.IsLocked = false;
				
				_callback?.Invoke();
			}
		}
	}
}
