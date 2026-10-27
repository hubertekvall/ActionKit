#nullable enable
using Godot;

namespace ActionKit.AI;

/// <summary>Base for nodes that steer the actor (through its ISteerable) to a position.</summary>
public abstract partial class BTMoveTo : BTNode
{
	/// <summary>Throttle passed to the steerable (heading length).</summary>
	[Export] public float HeadingMultiplier { get; set; } = 1.0f;

	[Export] public double DistanceThreshold { get; set; } = 1.0;

	/// <summary>Ignore height difference (steer on the XZ plane).</summary>
	[Export] public bool Flatten { get; set; } = true;

	protected Vector3 MovePosition { get; set; }

	public bool HasArrived()
	{
		var from = OwnerActor.GlobalPosition;
		var to = MovePosition;
		if (Flatten) { from.Y = 0; to.Y = 0; }
		return from.DistanceTo(to) <= DistanceThreshold;
	}

	protected BTStatus SteerTowardsMovePosition()
	{
		if (Steerable == null) return BTStatus.Failure;
		if (HasArrived()) return BTStatus.Success;

		var direction = OwnerActor.GlobalPosition.DirectionTo(MovePosition);
		if (Flatten) direction = new Vector3(direction.X, 0, direction.Z).Normalized();

		Steerable.SetHeading(direction * HeadingMultiplier);
		return BTStatus.Running;
	}
}
