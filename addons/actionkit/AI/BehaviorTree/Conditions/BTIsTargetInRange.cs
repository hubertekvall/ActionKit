#nullable enable
using Godot;

namespace ActionKit.AI;

// Succeeds if BTKeys.CurrentTarget is within Range of OwnerActor. Pairs naturally with
// weapon ScanRadius (gate firing/aiming on range before committing to it) or with a
// bigger Range to trigger "engage" behavior from farther out than a weapon can reach.
[GlobalClass]
public partial class BTIsTargetInRange : BTNode
{
	[Export] public float Range { get; set; } = 10.0f;

	protected override BTStatus Tick()
	{
		if (!BTree.TryGet(BTKeys.CurrentTarget, out var target) || !IsInstanceValid(target))
			return BTStatus.Failure;

		return OwnerActor.GlobalPosition.DistanceTo(target.GlobalPosition) <= Range
			? BTStatus.Success
			: BTStatus.Failure;
	}
}
