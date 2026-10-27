#nullable enable
using Godot;

namespace ActionKit.AI;

/// <summary>Steers straight at BTKeys.CurrentTarget with RamMultiplier throttle. Running until contact.</summary>
[GlobalClass]
public partial class BTRamTarget : BTNode
{
	[Export] public float RamMultiplier { get; set; } = 1.0f;

	/// <summary>Counts as a hit (Success) within this distance.</summary>
	[Export] public float HitDistance { get; set; } = 1.0f;

	protected override BTStatus Tick()
	{
		if (Steerable == null) return BTStatus.Failure;
		if (!BTree.TryGet(BTKeys.CurrentTarget, out var currentTarget) || !IsInstanceValid(currentTarget)) return BTStatus.Failure;

		var direction = OwnerActor.GlobalPosition.DirectionTo(currentTarget.GlobalPosition);
		Steerable.SetHeading(direction * RamMultiplier);

		return OwnerActor.GlobalPosition.DistanceTo(currentTarget.GlobalPosition) <= HitDistance
			? BTStatus.Success
			: BTStatus.Running;
	}
}
