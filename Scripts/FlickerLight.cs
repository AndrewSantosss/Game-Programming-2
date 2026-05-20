using Godot;
using System;

// Changed from OmniLight3D to SpotLight3D to match your post.tscn
public partial class FlickerLight : SpotLight3D
{
    [Export] public float MinDelay = 0.05f;
    [Export] public float MaxDelay = 0.2f;
    [Export] public float MinEnergy = 0.5f;
    [Export] public float MaxEnergy = 4.0f;
    
    private bool _isFlickering = false;
    private float _timer = 0.0f;
    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        _rng.Randomize();
    }

    public override void _Process(double delta)
    {
        if (!_isFlickering) return;

        _timer -= (float)delta;
        if (_timer <= 0)
        {
            _timer = _rng.RandfRange(MinDelay, MaxDelay);
            LightEnergy = _rng.RandfRange(MinEnergy, MaxEnergy);
        }
    }

    // This method allows PssTrigger to start the effect
    public void StartFlicker()
    {
        _isFlickering = true;
    }

    public void StopFlicker()
    {
        _isFlickering = false;
        LightEnergy = MaxEnergy; 
    }
}