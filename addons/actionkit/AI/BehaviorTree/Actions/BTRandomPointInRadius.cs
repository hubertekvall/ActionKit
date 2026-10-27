#nullable enable
using ActionKit.Utilities;
using Godot;

namespace ActionKit.AI;

/// <summary>
/// Writes a random point within Radius of the actor to BTKeys.TargetPosition (wandering).
/// Flatness blends from a full sphere (0) to a flat disc (1).
/// </summary>
[GlobalClass]
public partial class BTRandomPointInRadius : BTNode
{
	[Export] public float Radius { get; set; } = 10f;
	[Export(PropertyHint.Range, "0, 1")] public float Flatness { get; set; } = 1.0f;

	protected override BTStatus Tick()
	{
		var offset = VectorExtensions.RandomPointInSphere(Radius);
		offset.Y *= 1.0f - Flatness;

		BTree.Set(BTKeys.TargetPosition, OwnerActor.GlobalPosition + offset);
		return BTStatus.Success;
	}
}
