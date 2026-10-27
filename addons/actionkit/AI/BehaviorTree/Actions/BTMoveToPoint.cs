#nullable enable
using Godot;

namespace ActionKit.AI;

/// <summary>Steers to BTKeys.TargetPosition. Success on arrival.</summary>
[GlobalClass]
public partial class BTMoveToPoint : BTMoveTo
{
	protected override BTStatus Tick()
	{
		if (!BTree.TryGet(BTKeys.TargetPosition, out var targetPosition)) return BTStatus.Failure;

		MovePosition = targetPosition;
		return SteerTowardsMovePosition();
	}
}
