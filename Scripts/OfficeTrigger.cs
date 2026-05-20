using Godot;
using System;

public partial class OfficeTrigger : Area3D
{
    [Export] public Node3D ShopLookTarget;
    [Export] public AudioStreamPlayer StaticAudio;
    private bool _isDone = false;

    public void _on_body_entered(Node body)
    {
        if (body is not Player player || _isDone) return;

        _isDone = true;
        player.IsLocked = true;

        if (ShopLookTarget != null)
            player.PanToTarget(ShopLookTarget.GlobalPosition, false, 30.0f);

        if (StaticAudio != null)
        {
            StaticAudio.VolumeDb = -20.0f;
            StaticAudio.Play();
        }

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
                FadeOutStatic();
                player.IsLocked = false;
                player.ResetCamera();
            });
        }
    }

    private void FadeOutStatic()
    {
        if (StaticAudio == null || !StaticAudio.Playing) return;
        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(StaticAudio, "volume_db", -80.0f, 1.5f).SetTrans(Tween.TransitionType.Sine);
        tween.TweenCallback(Callable.From(() => StaticAudio.Stop()));
    }
}
