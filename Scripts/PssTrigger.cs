using Godot;
using System;

public partial class PssTrigger : Area3D
{
    [Export] public Node3D ManNode;
    [Export] public Node3D LookAtCorner;
    [Export] public FlickerLight NearbyLight;
    
    private bool _used = false;
    private Vector3 _originalManPosition;
    private Vector3 _hiddenManPosition;

    public override void _Ready()
    {
        // Connect the native Godot 4 event signal path using Type-Safe parameters
        BodyEntered += OnCustomBodyEntered;

        if (ManNode != null)
        {
            // Cache the exact height targets early to prevent stacking math drift bugs
            _originalManPosition = ManNode.GlobalPosition;
            _hiddenManPosition = _originalManPosition + new Vector3(0, -2.5f, 0);
            ManNode.Hide();
        }
    }

    private void OnCustomBodyEntered(Node3D body)
    {
        // Early escape constraints check
        if (body is not Player player || _used || ManNode == null) return;

        _used = true;
        player.IsLocked = true;
        NearbyLight?.StartFlicker();

        var dm = GetNode<DialogueManager>("/root/DialogueManager");
        if (dm == null) return;

        // Step 1: Trigger the ambient whisper sound sequence
        dm.StartDialogue(new string[] { "???: Psssst..." }, () => {
            
            // Step 2: Startled local camera shake response effect
            player.CameraShake(0.45f, 0.55f);

            // Step 3: Wait for shake to settle before executing looking animation loop
            GetTree().CreateTimer(0.4f).Timeout += () => {
                player.LookLeftRight(0.65f, 0.35f);

                // Step 4: Time window passes, the figure manifests at the designated corner
                GetTree().CreateTimer(1.1f).Timeout += () => {
                    
                    // Set to static offset instead of capturing accumulated drift data
                    ManNode.GlobalPosition = _hiddenManPosition;
                    ManNode.Show();

                    // Track look constraints targeting layout configurations
                    Vector3 lookTarget = LookAtCorner != null
                        ? LookAtCorner.GlobalPosition
                        : _originalManPosition;
                        
                    player.PanToTarget(lookTarget, true, 38.0f);

                    // Step 5: Tween interpolation sequence to make the entity rise into frame
                    Tween manRise = GetTree().CreateTween();
                    manRise.TweenProperty(ManNode, "global_position:y", _originalManPosition.Y, 0.22f)
                        .SetTrans(Tween.TransitionType.Back)
                        .SetEase(Tween.EaseType.Out);

                    // Step 6: Trigger head wiggles and react elements once transition is complete
                    GetTree().CreateTimer(0.22f).Timeout += () => {
                        player.HeadWiggle();
                        player.CameraShake(0.2f, 0.35f);
                        NearbyLight?.StopFlicker();

                        dm.StartDialogue(new string[] { "John: Sino yan?!" }, () => {
                            
                            // Step 7: Fade out transition sequence and return camera control variables
                            Tween fadeMan = GetTree().CreateTween();
                            fadeMan.TweenProperty(ManNode, "global_position:y", _hiddenManPosition.Y, 1.5f)
                                .SetTrans(Tween.TransitionType.Quad);
                                
                            fadeMan.TweenCallback(Callable.From(() => {
                                ManNode.Hide();
                                player.ResetCamera();
                                player.IsLocked = false;
                                
                                // Free memory layers of trigger once execution cycle finishes
                                QueueFree();
                            }));
                        });
                    };
                };
            };
        });
    }
}