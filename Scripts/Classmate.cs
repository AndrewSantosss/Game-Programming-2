using Godot;
using System;

public partial class Classmate : Node3D
{
	[Export] public Node3D CoffeeShopTarget;
	[Export] public Node3D BoothTarget;
	[Export] public Node3D SeatTarget;
	[Export] public Node3D PlayerSeatTarget;
	[Export] public Node3D PauwiTarget;
	[Export] public Node3D GasStationLookTarget;

	private bool _isWalking = false;
	private Vector3 _currentDestination;
	private Action _onReached;

	private bool _firstDialogueDone = false;
	private bool _reachedCoffeeShop = false;
	private float _walkSpeed = 4.5f;

	public override void _PhysicsProcess(double delta)
	{
		if (!_isWalking) return;

		Vector3 direction = (_currentDestination - GlobalPosition);
		direction.Y = 0;
		float dist = direction.Length();

		if (dist > 0.4f)
		{
			GlobalPosition += direction.Normalized() * _walkSpeed * (float)delta;
			LookAt(new Vector3(_currentDestination.X, GlobalPosition.Y, _currentDestination.Z), Vector3.Up);
		}
		else
		{
			_isWalking = false;
			_onReached?.Invoke();
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
		if (body is not Player player) return;

		if (!_firstDialogueDone)
		{
			_firstDialogueDone = true;
			player.IsLocked = true;
			player.PanToTarget(GlobalPosition, false, 42.0f, 0.0f);

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
				if (CoffeeShopTarget != null)
				{
					GlobalPosition = CoffeeShopTarget.GlobalPosition;
					_reachedCoffeeShop = true;
				}
			});
		}
		else if (_reachedCoffeeShop)
		{
			_reachedCoffeeShop = false;
			player.IsLocked = true;
			player.PanToTarget(GlobalPosition, false, 42.0f, 0.0f);

			string[] arrival = { "Classmate: Ayan, andyan ka na pala. Tara, order na tayo." };
			GetNode<DialogueManager>("/root/DialogueManager").StartDialogue(arrival, () => {
				player.IsLocked = false;
				player.ResetCamera();
				if (BoothTarget != null)
					MoveTo(BoothTarget.GlobalPosition, StartOrdering);
			});
		}
	}

	private void StartOrdering()
	{
		var player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player == null) return;

		player.IsLocked = true;
		player.PanToTarget(GlobalPosition, false, 42.0f, 0.0f);

		string[] lines = {
			"Classmate: Kuya, isang Strawberry Matcha po. Ikaw pre?",
			"John: Salted Caramel nalang akin.",
			"Tindero: Noted sir, tawagin nalang po namin kayo.",
			"Classmate: Tara, upo muna tayo doon habang naghihintay."
		};

		GetNode<DialogueManager>("/root/DialogueManager").StartDialogue(lines, () => {
			player.ResetCamera();
			if (SeatTarget != null)
				MoveTo(SeatTarget.GlobalPosition, () => {
					if (PlayerSeatTarget != null)
						player.SitAtTarget(PlayerSeatTarget.GlobalPosition, GlobalPosition);
					StartGhostStory();
				});
		});
	}

	private void StartGhostStory()
	{
		var player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player == null) return;

		player.PanToTarget(GlobalPosition, false, 42.0f, 0.0f);

		string[] story = {
			"Classmate: Uy alam mo ba, may nag aabang daw na di malaman kung sino dun malapit sa kanto nyo.",
			"John: Gage, naniniwala ka sa mga ganyan? Panakot lang nila yan sa mga bata.",
			"Classmate: Oo naman, yung tatay ko nakita eh, may dalang itak. Andilim dilim pa naman dun sa kanto niyo tapos walang masyadong bahay.",
			"Classmate: Teka, kuhanin ko lang yung order natin, andyan na yata."
		};

		GetNode<DialogueManager>("/root/DialogueManager").StartDialogue(story, () => {
			player.ResetCamera();
			player.IsLocked = true;
			if (BoothTarget != null)
				MoveTo(BoothTarget.GlobalPosition, () => {
					string[] selfTalk = { "John: Nag-aabang? Di siguro totoo yun..." };
					GetNode<DialogueManager>("/root/DialogueManager").StartDialogue(selfTalk, () => {
						if (SeatTarget != null)
							MoveTo(SeatTarget.GlobalPosition, EndConvo);
					});
				});
		});
	}

	private void EndConvo()
	{
		var player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player == null) return;
		var dm = GetNode<DialogueManager>("/root/DialogueManager");

		string[] lastLines = {
			"Classmate: Oh eto na kape mo. Mauuna na rin ako ha, pinapauwi na ako ni Mama.",
			"John: Sige pre, ingat.",
		};

		dm.StartDialogue(lastLines, () => {
			// Classmate walks off while player thinks about selling
			if (PauwiTarget != null)
				MoveTo(PauwiTarget.GlobalPosition, null);

			string[] sellingThought = {
				"John: Hala, need ko pa mag benta para may baon ako bukas.",
				"John: Sa may gas station na lang kaya ako pumwesto... may dadaan pa kaya doon?"
			};

			dm.StartDialogue(sellingThought, () => {
				// Point the camera towards the gas station
				Vector3 lookTarget = GasStationLookTarget != null
					? GasStationLookTarget.GlobalPosition
					: player.GlobalPosition + new Vector3(-20f, 0f, 80f);

				player.PanToTarget(lookTarget, false, 42.0f);

				// After the dramatic pan, let the player walk to the gas station
				GetTree().CreateTimer(1.8f).Timeout += () => {
					player.ResetCamera();
					player.IsLocked = false;
				};
			});
		});
	}
}
