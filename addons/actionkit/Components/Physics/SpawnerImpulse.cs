#nullable enable
using ActionKit.Utilities;
using Godot;

namespace ActionKit.Components;

/// <summary>
/// One-shot spawn kick away from wherever the actor was spawned from: debris flying off a
/// wreck, an ejected pod, a mine drifting clear of its layer. Falls back to a random direction
/// when there's no spawner, or it sits at the same position.
/// </summary>
[GlobalClass]
public partial class SpawnerImpulse : ActorComponent
{
	[Export] public float ImpulseStrength { get; set; } = 10.0f;

	private const float MinOffsetLengthSquared = 0.0001f;

	public override void OnActorReady()
	{
		ActorOwner.ActorBody?.ApplyCentralImpulse(GetDirection() * ImpulseStrength);
	}

	private Vector3 GetDirection()
	{
		var spawner = ActorOwner.Spawner;
		if (IsInstanceValid(spawner))
		{
			var offset = ActorOwner.GlobalPosition - spawner!.GlobalPosition;
			if (offset.LengthSquared() > MinOffsetLengthSquared) return offset.Normalized();
		}

		return VectorExtensions.RandomDirection();
	}
}
