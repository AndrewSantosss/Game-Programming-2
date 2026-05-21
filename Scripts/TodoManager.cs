using Godot;
using System;

public partial class TodoManager : Control
{
	private Label _todoLabel;

	public override void _Ready()
	{
		// Siguraduhing tama ang Node path ng iyong Label sa eksena
		_todoLabel = GetNodeOrNull<Label>("CanvasLayer/TodoLabel");
		
		UpdateTodoText("");
	}

	// ⚠️ DAPAT EKSAKTONG GANITO ANG PAGKAKASULAT (Capital U, T, T)
	public void UpdateTodoText(string newText)
	{
		if (_todoLabel != null)
		{
			_todoLabel.Text = newText;
		}
	}

	// ⚠️ DAPAT EKSAKTONG GANITO ANG PAGKAKASULAT (Capital C, T)
	public void ClearTodo()
	{
		if (_todoLabel != null)
		{
			_todoLabel.Text = "";
		}
	}
}
