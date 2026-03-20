using Godot;
using System;

public partial class TindahanAudio : Area3D
{
    [Export] public AudioStreamPlayer3D StoreNoise;
    private bool _hasPlayed = false;

    public override void _Ready()
    {
        // Ensure the noise is not playing on start
        if (StoreNoise != null) StoreNoise.Stop();
    }

    public void _on_body_entered(Node body)
    {
        if (body is Player && !_hasPlayed && StoreNoise != null)
        {
            _hasPlayed = true;
            // Reset playback position to start
            StoreNoise.Play(0.0f); 
            GD.Print("Tindahan noise triggered at: " + StoreNoise.GlobalPosition);
        }
    }

    public void _on_body_exited(Node body)
    {
        if (body is Player && StoreNoise != null)
        {
            // Optional: Fade out or stop the noise when leaving
            // Or just let the 3D distance attenuation handle it
            _hasPlayed = false; 
        }
    }
}