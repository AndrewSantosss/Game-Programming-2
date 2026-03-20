using Godot;
using System;

public partial class CafeIntro : Area3D
{
    private bool _hasStarted = false;

    public void _on_body_entered(Node body)
    {
        if (body is Player player && !_hasStarted)
        {
            _hasStarted = true;
            player.IsLocked = true; // Stop player movement

            var dm = GetNode<DialogueManager>("/root/DialogueManager");
            string[] introLines = {
                "John: Hay nako, panibagong shift nanaman.",
                "Manager (Internal): 'John! Siguraduhin mong matapos 'yung 3 orders bago ka umuwi!'",
                "John: Sige po... 3 orders lang pala eh. Kayang kaya."
            };

            dm.StartDialogue(introLines, () => {
                player.IsLocked = false; // Allow player to work
            });
        }
    }
}