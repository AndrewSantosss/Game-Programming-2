using Godot;
using System;

public partial class TindahanAudio : Area3D
{
    [Export] public AudioStreamPlayer3D StoreNoise;
    [Export] public float FadeInTime = 1.5f;
    [Export] public float FadeOutTime = 1.0f;
    private bool _isInside = false;

    public override void _Ready()
    {
        if (StoreNoise != null)
        {
            StoreNoise.VolumeDb = -80.0f;
            StoreNoise.Stop();
        }
    }

    public void _on_body_entered(Node body)
    {
        if (body is not Player || StoreNoise == null || _isInside) return;

        _isInside = true;
        StoreNoise.VolumeDb = -80.0f;
        StoreNoise.Play(0.0f);

        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(StoreNoise, "volume_db", 0.0f, FadeInTime).SetTrans(Tween.TransitionType.Sine);
    }

    public void _on_body_exited(Node body)
    {
        if (body is not Player || StoreNoise == null || !_isInside) return;

        _isInside = false;
        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(StoreNoise, "volume_db", -80.0f, FadeOutTime).SetTrans(Tween.TransitionType.Sine);
        tween.TweenCallback(Callable.From(() => StoreNoise.Stop()));
    }
}
