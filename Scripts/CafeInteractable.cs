using Godot;
using System;

public partial class CafeInteractable : Area3D
{
    [Export] public GhostlyFigure StalkerAtWindow;
    [Export] public int OrdersToComplete = 3;
     
    private int _currentOrders = 0;
    private bool _isBusy = false;

    public void OnInteract()
    {
        if (_isBusy || _currentOrders >= OrdersToComplete) return;

        _isBusy = true;
        var dm = GetNode<DialogueManager>("/root/DialogueManager");
        
        dm.StartDialogue(new string[] { "John: Making the coffee... (Stay here for 2 seconds)" });

        GetTree().CreateTimer(2.0f).Timeout += () => {
            _currentOrders++;
            _isBusy = false;  

            if (_currentOrders == 1)
            {
                dm.StartDialogue(new string[] { "John: One down. Two more to go." });
            }
            else if (_currentOrders == 2)
            {
                // Trigger the creepy stalker
                StalkerAtWindow?.TriggerAppearance();
                dm.StartDialogue(new string[] { "John: Done. Wait... sino yung nakatayo sa labas?" });

                // GHOST DISAPPEAR TIMER:
                GetTree().CreateTimer(5.0f).Timeout += () => {
                    if (IsInstanceValid(StalkerAtWindow))
                    {
                        StalkerAtWindow.Visible = false;   
                    }
                };
            }
            else if (_currentOrders >= OrdersToComplete)
            {
                // Find the player and lock them immediately
                var player = GetTree().GetFirstNodeInGroup("player") as Player;
                if (player != null) player.IsLocked = true;

                dm.StartDialogue(new string[] { 
                    "John: Last order. I should close the shop now and head home." 
                }, () => {
                    // This is where the magic happens
                    dm.DoFade(() => {
                        // This code runs ONLY after the screen is fully black
                        GetTree().ChangeSceneToFile("res://Run.tscn");
                    });
                });
            }
        };
    }
}