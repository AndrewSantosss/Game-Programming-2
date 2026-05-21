using Godot;
using System;

public partial class DialogueManager : CanvasLayer
{
	private Label _label;
	private Control _box;
	private int _index = 0;
	private Action _callback;
	private bool _isInFade = false;

	private string[] _currentLines;

	public override void _Ready()
	{
		_box = GetNodeOrNull<Control>("ColorRect");
		_label = GetNodeOrNull<Label>("ColorRect/Label");

		if (_box != null) _box.Hide();
		if (_label != null) _label.Visible = false;
	}

	public override void _Input(InputEvent @event)
	{
		if (_box != null && _box.Visible && !_isInFade && _currentLines != null && _index < _currentLines.Length)
		{
			if (@event.IsActionPressed("ui_accept") || @event.IsActionPressed("interact") ||
			   (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Space))
			{
				_index++;

				if (_index < _currentLines.Length)
					UpdateText();
				else
					CloseDialogue();

				GetViewport().SetInputAsHandled();
			}
		}
	}

	public void StartDialogue(string[] lines, Action onComplete = null)
	{
		_currentLines = lines;
		_index = 0;
		_callback = onComplete;
		_isInFade = false;

		SetDialogueMode();
		if (_box != null) _box.Show();
		UpdateText();

		// Robust Player lookup: Checks groups first, then falls back to recursive tree search
		var player = GetPlayerNode();
		if (player != null) player.IsLocked = true;
	}

	private void UpdateText()
	{
		if (_label != null) _label.Text = _currentLines[_index];
	}

	private void CloseDialogue()
	{
		if (_box != null) _box.Hide();
		
		var player = GetPlayerNode();
		if (player != null) player.IsLocked = false;
		
		var cb = _callback;
		_callback = null;
		cb?.Invoke();
	}

	// Helper method to safely find the Player script without breaking or crashing
	private Player GetPlayerNode()
	{
		// Try to find the Player via the group first (highly recommended to add your Player to a "player" group)
		var nodes = GetTree().GetNodesInGroup("player");
		if (nodes.Count > 0 && nodes[0] is Player p) return p;

		// Fallback to searching the scene tree roots
		return GetTree().Root.FindChild("Player", true, false) as Player;
	}

	private void SetDialogueMode()
	{
		if (_box is ColorRect rect)
		{
			rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			rect.OffsetTop = 0;
			rect.OffsetBottom = 0;
			rect.OffsetLeft = 0;
			rect.OffsetRight = 0;
			rect.Color = new Color(0, 0, 0, 0.6f); // Gave it a slight transparent tint for readability
		}

		if (_label != null)
		{
			_label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			_label.OffsetLeft = 20;
			_label.OffsetTop = 10;
			_label.OffsetRight = -20;
			_label.OffsetBottom = -30;
			_label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			_label.HorizontalAlignment = HorizontalAlignment.Center;
			_label.VerticalAlignment = VerticalAlignment.Bottom;
			_label.Visible = true;
		}
	}

	public void DoFade(Action onMidPoint)
	{
		_isInFade = true;
		if (_label != null) _label.Visible = false;

		if (_box is not ColorRect fadeBox) return;

		fadeBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		fadeBox.OffsetLeft = 0;
		fadeBox.OffsetTop = 0;
		fadeBox.OffsetRight = 0;
		fadeBox.OffsetBottom = 0;
		fadeBox.Color = new Color(0, 0, 0, 0);
		fadeBox.Show();

		Tween tween = GetTree().CreateTween();
		tween.TweenProperty(fadeBox, "color", new Color(0, 0, 0, 1), 1.2f);
		tween.TweenCallback(Callable.From(() => {
			onMidPoint?.Invoke();
		}));
	}

	public void FadeFromBlack(Action onComplete = null)
	{
		_isInFade = true;
		if (_label != null) _label.Visible = false;

		if (_box is not ColorRect fadeBox) return;

		fadeBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		fadeBox.OffsetLeft = 0;
		fadeBox.OffsetTop = 0;
		fadeBox.OffsetRight = 0;
		fadeBox.OffsetBottom = 0;
		fadeBox.Color = new Color(0, 0, 0, 1);
		fadeBox.Show();

		Tween tween = GetTree().CreateTween();
		tween.TweenProperty(fadeBox, "color", new Color(0, 0, 0, 0), 1.0f)
			.SetTrans(Tween.TransitionType.Sine);
		tween.TweenCallback(Callable.From(() => {
			fadeBox.Hide();
			_isInFade = false;
			onComplete?.Invoke();
		}));
	}

	public void ShowGameOver()
	{
		_isInFade = false;
		if (_box is ColorRect rect)
		{
			rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			rect.OffsetLeft = 0;
			rect.OffsetTop = 0;
			rect.OffsetRight = 0;
			rect.OffsetBottom = 0;
			rect.Color = new Color(0, 0, 0, 1);
			rect.Show();
		}

		if (_label != null)
		{
			_label.Text = "GAME OVER\n\nThank you for playing!";
			_label.HorizontalAlignment = HorizontalAlignment.Center;
			_label.VerticalAlignment = VerticalAlignment.Center;
			_label.SetAnchorsPreset(Control.LayoutPreset.Center);
			_label.GrowHorizontal = Control.GrowDirection.Both;
			_label.GrowVertical = Control.GrowDirection.Both;
			_label.Visible = true;
		}
	}
}
