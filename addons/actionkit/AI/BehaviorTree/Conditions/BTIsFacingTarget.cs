#nullable enable
using Godot;

namespace ActionKit.AI;

/// <summary>
/// Whether the actor faces BTKeys.CurrentTarget (forward · direction ≥ DotThreshold),
/// independent of whatever does the turning.
/// </summary>
[GlobalClass]
public partial class BTIsFacingTarget : BTNode
{
	[Export(PropertyHint.Range, "-1, 1")] public float DotThreshold { get; set; } = 0.9f;

	protected override BTStatus Tick()
	{
		if (!BTree.TryGet(BTKeys.CurrentTarget, out var target) || !IsInstanceValid(target)) return BTStatus.Failure;

		var forward = -OwnerActor.GlobalBasis.Z;
		var toTarget = OwnerActor.GlobalPosition.DirectionTo(target.GlobalPosition);

		return forward.Dot(toTarget) >= DotThreshold ? BTStatus.Success : BTStatus.Failure;
	}
}
