#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Tank-style steering for the actor's ISteerable (e.g. FlightMovement): ForwardAction and
/// BackAction thrust along the actor's nose, LeftAction and RightAction turn. Turning works by
/// telling it to look TurnAngle degrees to the side of its nose, so a bigger angle turns
/// harder. Letting go points it back at its own nose and it stops turning.
/// Compare InputHeadingController, which moves relative to the camera instead.
/// </summary>
[GlobalClass]
public partial class KeyboardSteering : ActorComponent
{
	[Export] public StringName ForwardAction { get; set; } = "ui_up";
	[Export] public StringName BackAction { get; set; } = "ui_down";
	[Export] public StringName LeftAction { get; set; } = "ui_left";
	[Export] public StringName RightAction { get; set; } = "ui_right";

	[Export] public float TurnAngle { get; set; } = 45.0f;

	private ISteerable? _steerable;

	public override void OnActorReady()
	{
		_steerable = ActorOwner.GetSteerable();
	}

	public override void ProcessTick(double delta)
	{
		if (_steerable == null) return;

		var thrust = Input.GetAxis(BackAction, ForwardAction);
		var turn = Input.GetAxis(RightAction, LeftAction);

		var basis = ActorOwner.ActorBody?.GlobalBasis ?? ActorOwner.GlobalBasis;
		var nose = -basis.Z;
		var forward = new Vector3(nose.X, 0f, nose.Z).Normalized();
		if (forward == Vector3.Zero) return;

		_steerable.SetHeading(forward * thrust, false);
		_steerable.SetLookDirection(forward.Rotated(Vector3.Up, Mathf.DegToRad(TurnAngle) * turn));
	}
}
