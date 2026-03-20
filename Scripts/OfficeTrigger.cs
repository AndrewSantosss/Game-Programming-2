using Godot;
using System;

public partial class OfficeTrigger : Area3D
{
    [Export] public Node3D ShopLookTarget; 
    private bool _isDone = false;

    public void _on_body_entered(Node body)
    {
        if (body is Player player && !_isDone)
        {
            _isDone = true;
            player.IsLocked = true;
            
            if (ShopLookTarget != null) 
                player.PanToTarget(ShopLookTarget.GlobalPosition, false, 30.0f);

            string[] lines = {
                "John: May Cash Out po ba kayo?",
                "Tindera: Wala. Ubos na.",
                "John: Ah... sige po.",
                "John: Sungit naman nanonood lang naman ng tv",
                "Tindera: Ano yun?",
                "John: Wala po at una na po ako"
            };

            var dialogue = GetNodeOrNull<DialogueManager>("/root/DialogueManager");
            if (dialogue != null)
            {
                dialogue.StartDialogue(lines, () => {
                    player.IsLocked = false;
                    player.ResetCamera();
                });
            }
        }
    }
}