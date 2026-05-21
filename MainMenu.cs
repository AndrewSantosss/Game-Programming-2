using Godot;
using System;

public partial class MainMenu : Node
{
	// Heto ang function na tatawagin kapag clinick ang button
	public void _on_play_button_pressed()
	{
		// Ligtas na ililipat ang laro patungo sa iyong map scene
		GetTree().ChangeSceneToFile("res://map.tscn");
	}
}
