#nullable enable
using ActionKit.Utilities;
using Godot;

namespace ActionKit.AI;

/// <summary>
/// Shared blackboard keys. The targeting pipeline: BTFindTargetsWithinRadius /
/// BTFilterTargetsByGroup build up Targets, BTClosestTarget / BTFarthestTarget /
/// BTRandomTarget / BTLowestHealthTarget narrow it to CurrentTarget, and everything
/// downstream reads CurrentTarget. Movement destinations (TargetPosition, TargetNode) are
/// separate from what is being targeted. Declare your own keys the same way.
/// </summary>
public static class BTKeys
{
	public static readonly BlackboardKey<QueryList> Targets = new("Targets");
	public static readonly BlackboardKey<Node3D> CurrentTarget = new("CurrentTarget");

	public static readonly BlackboardKey<Vector3> TargetPosition = new("TargetPosition");
	public static readonly BlackboardKey<Node3D> TargetNode = new("TargetNode");
}
