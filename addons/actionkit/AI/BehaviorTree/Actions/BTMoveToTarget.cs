#nullable enable
using Godot;

namespace ActionKit.AI;

/// <summary>
/// Steers to a node: TargetNode if set, otherwise BTKeys.TargetNode, otherwise
/// BTKeys.CurrentTarget. Success on arrival.
/// </summary>
[GlobalClass]
public partial class BTMoveToTarget : BTMoveTo
{
	[Export] public Node3D? TargetNode { get; set; }

	protected override BTStatus Tick()
	{
		var target = TargetNode;
		if (target == null && !BTree.TryGet(BTKeys.TargetNode, out target) && !BTree.TryGet(BTKeys.CurrentTarget, out target))
			return BTStatus.Failure;
		if (!IsInstanceValid(target)) return BTStatus.Failure;

		MovePosition = target!.GlobalPosition;
		return SteerTowardsMovePosition();
	}
}
