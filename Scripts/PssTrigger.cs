using Godot;
using System;

public partial class PssTrigger : Area3D
{
    [Export] public Node3D ManNode; 
    [Export] public Node3D LookAtCorner; 
    private bool _used = false;

    public override void _Ready()
    {
        if (ManNode != null) ManNode.Hide();
    }

    public void _on_body_entered(Node body)
    {
        if (body is Player player && !_used && ManNode != null)
        {
            _used = true;
            player.IsLocked = true; 
            var dm = GetNodeOrNull<DialogueManager>("/root/DialogueManager");

            if (dm != null)
            {
                dm.StartDialogue(new string[] { "???: Psssst..." }, () => {
                    player.LookLeftRight(0.6f, 0.3f); 
                    
                    GetTree().CreateTimer(1.2f).Timeout += () => {
                        Vector3 targetHeight = ManNode.GlobalPosition;
                        ManNode.GlobalPosition = targetHeight + new Vector3(0, -2.5f, 0);
                        ManNode.Show();
                        
                        player.PanToTarget(ManNode.GlobalPosition, true, 35.0f);

                        Tween manTween = GetTree().CreateTween();
                        manTween.TweenProperty(ManNode, "global_position:y", targetHeight.Y, 0.15f).SetTrans(Tween.TransitionType.Back);
                        
                        GetTree().CreateTimer(0.15f).Timeout += () => {
                            player.HeadWiggle();
                            dm.StartDialogue(new string[] { "John: Sino yan?!" }, () => {
                                Tween fadeMan = GetTree().CreateTween();
                                fadeMan.TweenProperty(ManNode, "global_position:y", targetHeight.Y - 2.5f, 1.5f);
                                fadeMan.TweenCallback(Callable.From(() => {
                                    ManNode.Hide();
                                    player.ResetCamera();
                                    player.IsLocked = false; 
                                }));
                            });
                        }; 
                    };
                });
            }
        }
    }
}