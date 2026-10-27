#nullable enable
using ActionKit.Utilities;
using Godot;

namespace ActionKit.AI;

/// <summary>
/// Steers directly away from BTKeys.CurrentTarget. Success once SafeDistance is reached,
/// Running until then. Pair with BTIsHealthBelowThreshold for retreat-when-hurt.
/// </summary>
[GlobalClass]
public partial class BTFleeTarget : BTNode
{
	[Export] public double SafeDistance { get; set; } = 20.0;

	protected override BTStatus Tick()
	{
		if (Steerable == null) return BTStatus.Failure;
		if (!BTree.TryGet(BTKeys.CurrentTarget, out var target) || !IsInstanceValid(target)) return BTStatus.Failure;

		if (OwnerActor.GlobalPosition.DistanceTo(target.GlobalPosition) >= SafeDistance) return BTStatus.Success;

		var away = target.GlobalPosition.Flattened().DirectionTo(OwnerActor.GlobalPosition.Flattened());
		Steerable.SetHeading(away);

		return BTStatus.Running;
	}
}
