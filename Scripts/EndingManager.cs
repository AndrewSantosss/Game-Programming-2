using Godot;
using System;

public partial class EndingManager : Node3D
{
    [Export] public Node3D StalkerSpawnPoint; 
    [Export] public PackedScene GhostScene;   
    [Export] public AudioStreamPlayer HorrorStinger; 

    private bool _endingTriggered = false;

    public override void _Ready()
    {
        // This plays as soon as the night level starts
        CallDeferred(nameof(PlaySpawnDialogue));
    }

    private void PlaySpawnDialogue()
    {
        var dm = GetNodeOrNull<DialogueManager>("/root/DialogueManager");
        if (dm != null)
        {
            dm.StartDialogue(new string[] { 
                "John: The fog is so thick tonight...",
                "John: I can barely see the road. I need to find the bus stop and get out of here."
            });
        }
    }

    public void TriggerTheVanishing()
    {
        if (_endingTriggered) return;
        _endingTriggered = true;

        var player = GetTree().GetFirstNodeInGroup("player") as Player;
        var dm = GetNode<DialogueManager>("/root/DialogueManager");

        if (player == null) return;

        player.IsLocked = true;

        // Dialogue right before the jumpscare
        dm.StartDialogue(new string[] { 
            "John: There it is... finally.",
            "John: Wait... did I just hear footsteps behind me?",
            "John: ...Is someone there?"
        }, () => {
            // 1. Create the ghost
            Node3D ghost = GhostScene.Instantiate<Node3D>();
            GetTree().Root.AddChild(ghost);

            // 2. FORCE SHOW: This fixes the "ghost not showing" issue
            ghost.Show();
            if (ghost is GhostlyFigure gf) 
            {
                gf.Show();
                // If your GhostlyFigure has a TriggerAppearance method, call it too!
                gf.TriggerAppearance(); 
            }
            
            // 3. Position the ghost
            if (StalkerSpawnPoint != null)
                ghost.GlobalPosition = StalkerSpawnPoint.GlobalPosition;
            else
                ghost.GlobalPosition = player.GlobalPosition + (player.Transform.Basis.Z * 2.5f);

            // 4. Make sure it faces the player
            ghost.LookAt(player.GlobalPosition);
            
            // 5. Force the player to look at the ghost
            player.PanToTarget(ghost.GlobalPosition, true, 30.0f);

            // 6. The Scare Timer
                GetTree().CreateTimer(0.8f).Timeout += () => {
                HorrorStinger?.Play();
                player.HeadWiggle(); 
                
                // 3. FINAL FADE TO BLACK
                dm.DoFade(() => {
                    // Keep the player locked here. 
                    // They shouldn't be able to move while the Game Over UI shows.
                    dm.ShowGameOver(); 
                });
            };
        });
    }

    public void _on_end_trigger_body_entered(Node3D body)
    {
        if (body.IsInGroup("player"))
        {
            TriggerTheVanishing();
        }
    }
}