#nullable enable
using System.Linq;
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Steers the actor's body towards Target with a turn-rate-limited heading, and points
/// ModelPivot along its velocity. Target can be set from code, arrives through a
/// TargetAssignedMessage (weapons send one to their projectiles), or is picked on ready as the
/// nearest node in TargetGroup.
/// </summary>
[GlobalClass]
public partial class HomingComponent : ActorComponent
{
	[Export] public float ThrustForce { get; set; } = 15f;
	[Export] public float MaxSpeed { get; set; } = 10f;

	/// <summary>How fast the heading turns toward the target (roughly rad/s).</summary>
	[Export] public float TurnRate { get; set; } = 5f;

	[Export] public float MaxAccelerationForce { get; set; } = 100f;

	/// <summary>When set and no target was assigned, home in on the nearest node in this group.</summary>
	[Export] public StringName TargetGroup { get; set; } = "";

	public Node3D? Target { get; set; }

	private Vector3 _heading = Vector3.Forward;
	private Vector3 _steerVelocity;

	public override void OnActorReady()
	{
		if (Target == null && !TargetGroup.IsEmpty)
		{
			Target = GetTree().GetNodesInGroup(TargetGroup).OfType<Node3D>()
				.Where(node => node != ActorOwner && node != ActorOwner.Spawner)
				.OrderBy(node => node.GlobalPosition.DistanceSquaredTo(ActorOwner.GlobalPosition))
				.FirstOrDefault();
		}

		if (ActorOwner.ActorBody is { } body)
			_heading = -body.GlobalBasis.Z;
	}

	public override void OnMessage(ActorMessage message)
	{
		if (message is TargetAssignedMessage assigned)
			Target = assigned.Target;
	}

	public override void PhysicsTick(double delta)
	{
		var body = ActorOwner.ActorBody;
		if (body == null) return;

		var dt = (float)delta;

		if (IsInstanceValid(Target))
		{
			var desired = body.GlobalPosition.DirectionTo(Target!.GlobalPosition);
			_heading = _heading.Slerp(desired, 1f - Mathf.Exp(-TurnRate * dt));
		}

		float velocityDot = _heading.Dot(body.LinearVelocity.Normalized());
		float accCurve = Mathf.Lerp(2.0f, 1.0f, Mathf.SmoothStep(-1.0f, 1.0f, velocityDot));

		_steerVelocity = _steerVelocity.MoveToward(_heading * MaxSpeed, ThrustForce * accCurve * dt);

		var neededAcceleration = ((_steerVelocity - body.LinearVelocity) / dt).LimitLength(MaxAccelerationForce * accCurve);
		body.ApplyCentralForce(neededAcceleration);

		// Fake the turn: point the model pivot along the velocity.
		var pivot = ActorOwner.ModelPivot;
		if (pivot == null || body.LinearVelocity.LengthSquared() < 0.01f) return;

		var lookDir = body.LinearVelocity.Normalized();
		var up = Mathf.Abs(lookDir.Dot(Vector3.Up)) > 0.999f ? Vector3.Forward : Vector3.Up;
		pivot.GlobalBasis = Basis.LookingAt(lookDir, up);
	}
}
