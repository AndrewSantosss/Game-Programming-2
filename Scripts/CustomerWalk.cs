using Godot;
using System;
using System.Linq;

public partial class CustomerWalk : Node3D
{
    // Set this in the Inspector if the auto-detected animation is wrong.
    [Export] public string PreferredAnimationName = "";

    // Names to try, in priority order (case-insensitive contains match used as fallback).
    private static readonly string[] WalkNames =
    {
        "walk", "Walk", "walking", "Walking",
        "WalkCycle", "walk_cycle", "Walk_Cycle",
        "Armature|Walk", "Armature|walk",
        "mixamo.com", "run", "Run"
    };

    public override void _Ready()
    {
        AnimationPlayer anim = FindAnimationPlayer(this);
        if (anim == null)
        {
            GD.PushError($"[CustomerWalk] No AnimationPlayer found under '{Name}'");
            return;
        }

        string[] available = anim.GetAnimationList();
        GD.Print($"[CustomerWalk] '{Name}' animations: {string.Join(", ", available)}");

        string chosen = PreferredAnimationName;

        if (string.IsNullOrEmpty(chosen))
            chosen = PickWalkAnimation(available);

        if (!string.IsNullOrEmpty(chosen) && anim.HasAnimation(chosen))
        {
            // CRITICAL ADDITION: Force the targeted animation resource to loop 
            // so your customer asset doesn't stop walking after one cycle.
            var animResource = anim.GetAnimation(chosen);
            if (animResource != null && animResource.LoopMode == Animation.LoopModeEnum.None)
            {
                animResource.LoopMode = Animation.LoopModeEnum.Linear;
            }

            anim.Play(chosen);
        }
        else
        {
            GD.PushWarning($"[CustomerWalk] Could not find a walk animation for '{Name}'. " +
                          $"Set PreferredAnimationName in the Inspector. Available: {string.Join(", ", available)}");
        }
    }

    private static string PickWalkAnimation(string[] available)
    {
        // Exact match first
        foreach (string candidate in WalkNames)
        {
            string match = available.FirstOrDefault(a =>
                a.Equals(candidate, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }

        // Partial match: animation name contains "walk"
        string partial = available.FirstOrDefault(a =>
            a.IndexOf("walk", StringComparison.OrdinalIgnoreCase) >= 0);
        if (partial != null) return partial;

        // Last resort: first non-RESET animation
        return available.FirstOrDefault(a =>
            !a.Equals("RESET", StringComparison.OrdinalIgnoreCase));
    }

    private static AnimationPlayer FindAnimationPlayer(Node node)
    {
        // Modern C# Type Pattern Matching optimization
        if (node is AnimationPlayer ap) return ap;
        
        foreach (Node child in node.GetChildren())
        {
            AnimationPlayer found = FindAnimationPlayer(child);
            if (found != null) return found;
        }
        return null;
    }
}