using Godot;
using System;

/// <summary>
/// Placed near the gas station selling spot in map.tscn.
///
/// Flow: player enters → intro → items drop → second dialogue →
///   black-screen cut → player sits on mat → 3 customers arrive one by one →
///   each paying customer: coin zooms in with slow spin → flies to chest → disappears.
///
/// Customer markers (Customer1Arrive, Customer2Arrive, Customer3Arrive) set the
/// exact arrival spots. CoinPlacedMarker is the chest target for coin animations.
/// </summary>
public partial class SellingAreaTrigger : Area3D
{
    [Export] public Node3D   SellingDisplay;
    [Export] public Node3D   MatPosition;
    [Export] public Marker3D Customer1Arrive;
    [Export] public Marker3D Customer2Arrive;
    [Export] public Marker3D Customer3Arrive;
    [Export] public Marker3D CoinPlacedMarker;

    private const string CopperCoin = "res://Scenes/CoinCopper.tscn";
    private const string SilverCoin = "res://Scenes/CoinSilver.tscn";
    private const string GoldCoin   = "res://Scenes/CoinGold.tscn";
    private const float  CoinScale  = 0.05f;

    private bool _used;
    private Player _player;
    private DialogueManager _dm;
    private float _groundY;
    private Vector3 _basePos;
    private Vector3 _matPos; // player sit position, used to face customers toward player

    public override void _Ready()
    {
        if (SellingDisplay != null)
            SellingDisplay.Hide();
    }

    public void _on_body_entered(Node body)
    {
        if (body is not Player player || _used) return;
        _used    = true;
        _player  = player;
        _dm      = GetNode<DialogueManager>("/root/DialogueManager");
        _groundY = player.GlobalPosition.Y;
        _basePos = GlobalPosition;

        player.IsLocked = true;

        _dm.StartDialogue(new string[] {
            "John: Okay... dito na muna ako. Ilabas ko na mga gamit ko."
        }, () => {
            player.PanToTarget(
                player.GlobalPosition + new Vector3(0f, -0.7f, 3.0f),
                false, 52.0f
            );

            if (SellingDisplay != null)
            {
                Vector3 origPos = SellingDisplay.GlobalPosition;
                SellingDisplay.GlobalPosition = new Vector3(origPos.X, origPos.Y + 1.6f, origPos.Z);
                SellingDisplay.Show();

                Tween dropTween = GetTree().CreateTween();
                dropTween.TweenProperty(SellingDisplay, "global_position:y", origPos.Y, 0.7f)
                    .SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
            }

            GetTree().CreateTimer(1.1f).Timeout += () => {
                _dm.StartDialogue(new string[] {
                    "John: (Hinga) Bahala na... baka may bumili habang naghihintay ang mga sasakyan.",
                    "John: Tsaka, may trabaho naman din akong hihintayin dito sa gas station mamaya."
                }, () => {
                    player.ResetCamera();

                    _dm.DoFade(() => {
                        _matPos = MatPosition != null
                            ? MatPosition.GlobalPosition
                            : new Vector3(_basePos.X, _groundY, _basePos.Z - 0.5f);
                        player.SitAtTarget(_matPos, _matPos + new Vector3(0f, 0f, 5f));

                        GetTree().CreateTimer(1.1f).Timeout += () => {
                            _dm.FadeFromBlack(() => {
                                GetTree().CreateTimer(0.5f).Timeout += RunCustomer1;
                            });
                        };
                    });
                });
            };
        });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Customer 1 — from the building, buys 2 balot, pays with copper coins
    // ═══════════════════════════════════════════════════════════════════════
    private void RunCustomer1()
    {
        _player.IsLocked = true;

        var c1 = GD.Load<PackedScene>("res://Scenes/customer.tscn").Instantiate<Node3D>();
        Vector3 spawnPos  = new Vector3(_basePos.X + 12f, _groundY, _basePos.Z - 5f);
        Vector3 arrivePos = Customer1Arrive != null
            ? new Vector3(Customer1Arrive.GlobalPosition.X, _groundY, Customer1Arrive.GlobalPosition.Z)
            : new Vector3(_basePos.X + 1.5f, _groundY, _basePos.Z + 1.5f);

        GetTree().CurrentScene.AddChild(c1);
        c1.GlobalPosition = spawnPos;

        _player.TrackNode(c1);
        PlayWalkAnim(c1);
        MoveNpcTo(c1, arrivePos, 3.0f, () => {
            _player.StopTracking();
            StopWalkAnim(c1);
            FaceTarget(c1, _matPos);
            _player.IsLocked = true;

            _dm.StartDialogue(new string[] {
                "Customer: Kuya, pabili nga ng dalawang balot!",
                "John: Heto po! Dalawampu piso."
            }, () => {
                _player.IsLocked = true;
                AnimateCoinToChest(CopperCoin, 2, arrivePos);

                _dm.StartDialogue(new string[] {
                    "Customer: Ang mura! Salamat ha, masarap luto mo!",
                    "John: Salamat po, bumalik kayo ha!"
                }, () => {
                    _player.IsLocked = true;
                    PlayWalkAnim(c1);
                    _player.TrackNode(c1);
                    MoveNpcTo(c1, spawnPos, 3.0f, () => {
                        _player.StopTracking();
                        c1.QueueFree();
                        GetTree().CreateTimer(1.2f).Timeout += RunCustomer2;
                    });
                });
            });
        });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Customer 2 — from the cafe, buys 1 balot, pays with a silver coin
    // ═══════════════════════════════════════════════════════════════════════
    private void RunCustomer2()
    {
        _player.IsLocked = true;

        var c2 = GD.Load<PackedScene>("res://Scenes/customer_2.tscn").Instantiate<Node3D>();
        Vector3 spawnPos  = new Vector3(_basePos.X - 10f, _groundY, _basePos.Z - 12f);
        Vector3 arrivePos = Customer2Arrive != null
            ? new Vector3(Customer2Arrive.GlobalPosition.X, _groundY, Customer2Arrive.GlobalPosition.Z)
            : new Vector3(_basePos.X - 1.5f, _groundY, _basePos.Z + 1.5f);

        GetTree().CurrentScene.AddChild(c2);
        c2.GlobalPosition = spawnPos;

        _player.TrackNode(c2);
        PlayWalkAnim(c2);
        MoveNpcTo(c2, arrivePos, 3.0f, () => {
            _player.StopTracking();
            StopWalkAnim(c2);
            FaceTarget(c2, _matPos);
            _player.IsLocked = true;

            _dm.StartDialogue(new string[] {
                "Customer2: Isang balot lang muna ha kuya, pauwi na ko eh.",
                "John: Sige po! Sampung piso lang."
            }, () => {
                _player.IsLocked = true;
                AnimateCoinToChest(SilverCoin, 1, arrivePos);

                _dm.StartDialogue(new string[] {
                    "Customer2: Heto! Ingat ka diyan ha, gabi na.",
                    "John: Salamat! Kayo rin po, ingat!"
                }, () => {
                    _player.IsLocked = true;
                    PlayWalkAnim(c2);
                    _player.TrackNode(c2);
                    MoveNpcTo(c2, spawnPos, 3.0f, () => {
                        _player.StopTracking();
                        c2.QueueFree();
                        GetTree().CreateTimer(1.2f).Timeout += RunCustomer3;
                    });
                });
            });
        });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Customer 3 — from the trees, 5 balot, SCARY (werewolf)
    //   Does not pay in dialogue — gold coin materialises after it retreats.
    // ═══════════════════════════════════════════════════════════════════════
    private void RunCustomer3()
    {
        _player.IsLocked = true;

        _player.PanToTarget(_player.GlobalPosition + new Vector3(7f, 0f, 4f), false, 52f);

        _dm.StartDialogue(new string[] {
            "John: (Sa sarili) Ha...? Sino naman ito galing sa madilim?"
        }, () => {
            _player.IsLocked = true;

            var c3 = GD.Load<PackedScene>("res://Scenes/customer_3.tscn").Instantiate<Node3D>();
            Vector3 spawnPos  = new Vector3(_basePos.X + 14f, _groundY, _basePos.Z + 9f);
            Vector3 arrivePos = Customer3Arrive != null
                ? new Vector3(Customer3Arrive.GlobalPosition.X, _groundY, Customer3Arrive.GlobalPosition.Z)
                : new Vector3(_basePos.X + 1.5f, _groundY, _basePos.Z + 2f);

            GetTree().CurrentScene.AddChild(c3);
            c3.GlobalPosition = spawnPos;

            _player.TrackNode(c3);
            PlayWalkAnim(c3);
            MoveNpcTo(c3, arrivePos, 1.8f, () => {
                _player.StopTracking();
                StopWalkAnim(c3);
                FaceTarget(c3, _matPos);
                _player.IsLocked = true;
                _player.HeadWiggle();

                _dm.StartDialogue(new string[] {
                    "???: ... Lima.",
                    "John: A-ah... opo... l-limang balot po..."
                }, () => {
                    _player.IsLocked = true;
                    _player.CameraShake(0.4f, 0.7f);

                    GetTree().CreateTimer(0.7f).Timeout += () => {
                        _dm.StartDialogue(new string[] {
                            "???: ... (tumitingin nang walang imik. Hindi nagbabayad.)",
                            "John: (nanginginig) H-heto po... kunin na ninyo...",
                            "???: ...",
                            "John: I-i-ingat po kayo... (halos pabulong)"
                        }, () => {
                            _player.IsLocked = true;
                            _player.CameraShake(0.3f, 0.5f);

                            PlayWalkAnim(c3);
                            Vector3 goldCoinPos = arrivePos;
                            _player.TrackNode(c3);
                            MoveNpcTo(c3, spawnPos, 1.5f, () => {
                                _player.StopTracking();
                                c3.QueueFree();

                                // Gold coin appears where the creature stood
                                GetTree().CreateTimer(0.3f).Timeout += () => {
                                    AnimateCoinToChest(GoldCoin, 1, goldCoinPos);
                                    // Wait for full animation (zoom 1.5s + fly 0.45s) before wrap-up
                                    GetTree().CreateTimer(2.2f).Timeout += EndSellingSequence;
                                };
                            });
                        });
                    };
                });
            });
        });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // End — wrap-up dialogue then transition to the work shift
    // ═══════════════════════════════════════════════════════════════════════
    private void EndSellingSequence()
    {
        _player.IsLocked = true;
        _player.ResetCamera();

        _dm.StartDialogue(new string[] {
            "John: (huminga nang malalim) ...Ano ba 'yung huling customer na 'yon?",
            "John: Sige na, bago pa lumipas ang oras. Oras na para sa trabaho."
        }, () => {
            _dm.DoFade(() => GetTree().ChangeSceneToFile(SceneFlow.CafeWork));
        });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NPC movement — rotate to face travel direction then tween position
    // ═══════════════════════════════════════════════════════════════════════
    private void MoveNpcTo(Node3D npc, Vector3 to, float speed, Action onDone)
    {
        Vector3 from = npc.GlobalPosition;
        float dist   = (to - from).Length();
        float dur    = Mathf.Max(0.5f, dist / speed);

        float targetRotY = Mathf.Atan2(-(to.X - from.X), -(to.Z - from.Z));

        Tween rotTween = GetTree().CreateTween();
        rotTween.TweenProperty(npc, "rotation:y", targetRotY, 0.25f)
            .SetTrans(Tween.TransitionType.Sine);

        Tween moveTween = GetTree().CreateTween();
        moveTween.TweenProperty(npc, "global_position", to, dur)
            .SetTrans(Tween.TransitionType.Linear);
        moveTween.TweenCallback(Callable.From(() => onDone?.Invoke()));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Face a target position on the XZ plane (used to turn toward the player)
    // ═══════════════════════════════════════════════════════════════════════
    private void FaceTarget(Node3D npc, Vector3 target, float duration = 0.3f)
    {
        Vector3 dir = target - npc.GlobalPosition;
        dir.Y = 0f;
        if (dir.LengthSquared() < 0.001f) return;
        float rotY = Mathf.Atan2(-dir.X, -dir.Z);
        Tween t = GetTree().CreateTween();
        t.TweenProperty(npc, "rotation:y", rotY, duration).SetTrans(Tween.TransitionType.Sine);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Animation helpers — find the embedded AnimationPlayer in a GLB
    // ═══════════════════════════════════════════════════════════════════════
    private AnimationPlayer FindAnimPlayer(Node3D npc)
        => npc.FindChild("AnimationPlayer", true, false) as AnimationPlayer;

    private void PlayWalkAnim(Node3D npc)
    {
        var anim = FindAnimPlayer(npc);
        if (anim == null) return;

        foreach (StringName name in anim.GetAnimationList())
        {
            string lower = name.ToString().ToLower();
            if (lower.Contains("walk") || lower.Contains("run"))
            {
                anim.Play(name);
                return;
            }
        }
        var list = anim.GetAnimationList();
        if (list.Length > 0) anim.Play(list[0]);
    }

    private void StopWalkAnim(Node3D npc)
    {
        var anim = FindAnimPlayer(npc);
        if (anim == null) return;

        foreach (StringName name in anim.GetAnimationList())
        {
            string lower = name.ToString().ToLower();
            if (lower.Contains("idle") || lower.Contains("stand"))
            {
                anim.Play(name);
                return;
            }
        }
        anim.Stop();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Coin animation: zoom in with slow spin → fly to chest → disappear
    //
    // Phase 1 (1.5 s): coin scales from zero to 3× CoinScale while spinning.
    // Phase 2 (0.45 s): coin flies to CoinPlacedMarker and shrinks to nothing.
    // ═══════════════════════════════════════════════════════════════════════
    private void AnimateCoinToChest(string scenePath, int count, Vector3 spawnPos)
    {
        var coinScene = GD.Load<PackedScene>(scenePath);

        for (int i = 0; i < count; i++)
        {
            int idx = i;
            GetTree().CreateTimer(idx * 0.45f).Timeout += () => {
                var coin = coinScene.Instantiate<Node3D>();

                GetTree().CurrentScene.AddChild(coin);
                coin.Scale = Vector3.Zero;
                float xOff = (idx % 2 == 0 ? 1f : -1f) * 0.12f;
                coin.GlobalPosition = new Vector3(spawnPos.X + xOff, spawnPos.Y + 0.8f, spawnPos.Z);

                // Phase 1a: zoom in
                Vector3 displayScale = new Vector3(CoinScale * 3f, CoinScale * 3f, CoinScale * 3f);
                Tween zoomTween = GetTree().CreateTween();
                zoomTween.TweenProperty(coin, "scale", displayScale, 1.5f)
                    .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

                // Phase 1b: slow continuous spin while zooming
                var spinTween = GetTree().CreateTween().SetLoops();
                spinTween.TweenProperty(coin, "rotation_degrees:y", 360f, 1.1f)
                    .AsRelative().SetTrans(Tween.TransitionType.Linear);

                // Phase 2: fly to chest after zoom completes
                GetTree().CreateTimer(1.5f).Timeout += () => {
                    spinTween.Kill();

                    Vector3 chestPos = CoinPlacedMarker != null
                        ? CoinPlacedMarker.GlobalPosition
                        : spawnPos;

                    Tween flyTween = GetTree().CreateTween();
                    flyTween.TweenProperty(coin, "global_position", chestPos, 0.45f)
                        .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);

                    Tween shrinkTween = GetTree().CreateTween();
                    shrinkTween.TweenProperty(coin, "scale", Vector3.Zero, 0.45f)
                        .SetTrans(Tween.TransitionType.Linear);
                    shrinkTween.TweenCallback(Callable.From(() => coin.QueueFree()));
                };
            };
        }
    }
}
