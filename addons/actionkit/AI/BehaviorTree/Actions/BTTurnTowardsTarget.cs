#nullable enable
using Godot;

namespace ActionKit.AI;

/// <summary>Sets the look direction at BTKeys.CurrentTarget. Success once facing it (forward · dir > DotThreshold).</summary>
[GlobalClass]
public partial class BTTurnTowardsTarget : BTNode
{
	[Export(PropertyHint.Range, "-1, 1")] public float DotThreshold { get; set; } = 0.9f;

	protected override BTStatus Tick()
	{
		if (Steerable == null) return BTStatus.Failure;
		if (!BTree.TryGet(BTKeys.CurrentTarget, out var targetNode) || !IsInstanceValid(targetNode)) return BTStatus.Failure;

		var lookDirection = OwnerActor.GlobalPosition.DirectionTo(targetNode.GlobalPosition);
		Steerable.SetLookDirection(lookDirection);

		var forward = -OwnerActor.GlobalBasis.Z;
		return forward.Dot(lookDirection) > DotThreshold ? BTStatus.Success : BTStatus.Running;
	}
}
