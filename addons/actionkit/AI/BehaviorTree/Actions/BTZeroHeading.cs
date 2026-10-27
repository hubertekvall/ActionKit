#nullable enable
using Godot;

namespace ActionKit.AI;

/// <summary>Stops steering (zero heading, keeps facing).</summary>
[GlobalClass]
public partial class BTZeroHeading : BTNode
{
	protected override BTStatus Tick()
	{
		if (Steerable == null) return BTStatus.Failure;

		Steerable.SetHeading(Vector3.Zero, false);
		return BTStatus.Success;
	}
}
