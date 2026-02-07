using Godot;
using System;

public partial class Classmate : Node3D
{
	[Export] public Node3D CoffeeShopTarget; 
	[Export] public Node3D BoothTarget; 
	[Export] public Node3D SeatTarget; 
	[Export] public Node3D PlayerSeatTarget; 
	[Export] public Node3D PauwiTarget; 

	private bool _isWalking = false;
	private Vector3 _currentDestination;
	private Action _onReached;
	
	private bool _firstDialogueDone = false;
	private bool _reachedCoffeeShop = false;
	private float _walkSpeed = 2.5f;

	public override void _PhysicsProcess(double delta)
	{
		if (_isWalking)
		{
			Vector3 direction = (_currentDestination - GlobalPosition).Normalized();
			direction.Y = 0;
			if (GlobalPosition.DistanceTo(_currentDestination) > 0.5f)
			{
				GlobalPosition += direction * _walkSpeed * (float)delta;
				LookAt(new Vector3(_currentDestination.X, GlobalPosition.Y, _currentDestination.Z), Vector3.Up);
			}
			else
			{
				_isWalking = false;
				_onReached?.Invoke();
			}
		}
	}

	private void MoveTo(Vector3 target, Action callback)
	{
		_currentDestination = target;
		_onReached = callback;
		_isWalking = true;
	}

	public void _on_body_entered(Node body)
	{
		if (body is Player player)
		{
			if (!_firstDialogueDone)
			{
				_firstDialogueDone = true;
				player.IsLocked = true;
				player.PanToTarget(GlobalPosition, false, 30.0f, 0.0f);

				string[] lines = {
					"Classmate: Pre, tara kape muna tayo!",
					"John: Ay pre gabi na eh, malelate ako ng uwi.",
					"Classmate: Sige na pre, sandali lang naman eh.",
					"John: Sige na nga... Pero magpapa CashG lang muna ako dyan sa tindahan tapos sunod ako.",
					"Classmate: Sige pre, hintayin kita doon."
				};

				GetNode<DialogueManager>("/root/DialogueManager").StartDialogue(lines, () => {
					player.IsLocked = false;
					player.ResetCamera();
					if (CoffeeShopTarget != null) {
						GlobalPosition = CoffeeShopTarget.GlobalPosition;
						_reachedCoffeeShop = true;
					}
				});
			}
			else if (_reachedCoffeeShop)
			{
				_reachedCoffeeShop = false;
				player.IsLocked = true;
				player.PanToTarget(GlobalPosition, false, 30.0f, 0.0f);

				string[] arrival = { "Classmate: Ayan, andyan ka na pala. Tara, order na tayo." };
				GetNode<DialogueManager>("/root/DialogueManager").StartDialogue(arrival, () => {
					player.IsLocked = false;
					player.ResetCamera();
					MoveTo(BoothTarget.GlobalPosition, StartOrdering);
				});
			}
		}
	}

	private void StartOrdering()
	{
		var player = (Player)GetTree().GetFirstNodeInGroup("player");
		player.IsLocked = true;
		player.PanToTarget(GlobalPosition, false, 30.0f, 0.0f);

		string[] lines = {
			"Classmate: Kuya, isang Strawberry Matcha po. Ikaw pre?",
			"John: Salted Caramel nalang akin.",
			"Tindero: Noted sir, tawagin nalang po namin kayo.",
			"Classmate: Tara, upo muna tayo doon habang naghihintay."
		};

		GetNode<DialogueManager>("/root/DialogueManager").StartDialogue(lines, () => {
			player.ResetCamera();
			MoveTo(SeatTarget.GlobalPosition, () => {
				player.SitAtTarget(PlayerSeatTarget.GlobalPosition, GlobalPosition);
				StartGhostStory();
			});
		});
	}

	private void StartGhostStory()
	{
		var player = (Player)GetTree().GetFirstNodeInGroup("player");
		player.PanToTarget(GlobalPosition, false, 30.0f, 0.0f);

		string[] story = {
			"Classmate: Uy alam mo ba, may nag aabang daw na di malaman kung sino dun malapit sa kanto nyo.",
			"John: Gage, naniniwala ka sa mga ganyan? Panakot lang nila yan sa mga bata.",
			"Classmate: Oo naman, yung tatay ko nakita eh, may dalang itak. Andilim dilim pa naman dun sa kanto niyo tapos walang masyadong bahay.",
			"Classmate: Teka, kuhanin ko lang yung order natin, andyan na yata."
		};

		GetNode<DialogueManager>("/root/DialogueManager").StartDialogue(story, () => {
			player.ResetCamera();
			player.IsLocked = true; 
			MoveTo(BoothTarget.GlobalPosition, () => {
				string[] selfTalk = { "John: Nag-aabang? Di siguro totoo yun..." };
				GetNode<DialogueManager>("/root/DialogueManager").StartDialogue(selfTalk, () => {
					MoveTo(SeatTarget.GlobalPosition, EndConvo);
				});
			});
		});
	}

	private void EndConvo()
	{
		var player = (Player)GetTree().GetFirstNodeInGroup("player");
		player.PanToTarget(GlobalPosition, false, 30.0f, 0.0f);
		string[] lastLines = {
			"Classmate: Oh eto na kape mo. Mauuna na rin ako ha, pinapauwi na ako ni Mama.",
			"John: Sige pre, ingat."
		};
		GetNode<DialogueManager>("/root/DialogueManager").StartDialogue(lastLines, () => {
			player.ResetCamera();
			player.IsLocked = false; 
			if (PauwiTarget != null) MoveTo(PauwiTarget.GlobalPosition, () => { QueueFree(); });
		});
	}
}
