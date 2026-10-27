#nullable enable
using System.Linq;
using ActionKit.Utilities;
using Godot;

namespace ActionKit.AI;

/// <summary>
/// Writes a random point on a circle of OffsetRadius around the nearest node in GroupName
/// (e.g. "player") to BTKeys.TargetPosition. Good for enemies that hover around the player.
/// </summary>
[GlobalClass]
public partial class BTRandomPointAroundGroup : BTNode
{
	[Export] public StringName GroupName { get; set; } = "player";
	[Export] public float OffsetRadius { get; set; } = 10f;

	protected override BTStatus Tick()
	{
		var nearest = GetTree().GetNodesInGroup(GroupName).OfType<Node3D>()
			.Where(node => node != OwnerActor)
			.OrderBy(node => OwnerActor.GlobalPosition.DistanceSquaredTo(node.GlobalPosition))
			.FirstOrDefault();

		if (nearest == null) return BTStatus.Failure;

		var angle = GD.Randf() * Mathf.Tau;
		var position = nearest.GlobalPosition.Flattened() + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * OffsetRadius;

		BTree.Set(BTKeys.TargetPosition, position);
		return BTStatus.Success;
	}
}
