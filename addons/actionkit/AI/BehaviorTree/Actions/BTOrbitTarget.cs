#nullable enable
using ActionKit.Utilities;
using Godot;

namespace ActionKit.AI;

/// <summary>
/// Circle-strafes BTKeys.CurrentTarget at OrbitRadius: a tangential heading plus a radial
/// correction so the actor settles at the orbit distance. Never succeeds on its own; put it
/// in a Selector behind higher-priority branches to break out.
/// </summary>
[GlobalClass]
public partial class BTOrbitTarget : BTNode
{
	[Export] public float OrbitRadius { get; set; } = 15.0f;
	[Export] public float OrbitSpeed { get; set; } = 1.0f;
	[Export] public bool Clockwise { get; set; } = true;

	protected override BTStatus Tick()
	{
		if (Steerable == null) return BTStatus.Failure;
		if (!BTree.TryGet(BTKeys.CurrentTarget, out var target) || !IsInstanceValid(target)) return BTStatus.Failure;

		var awayFromTarget = target.GlobalPosition.Flattened().DirectionTo(OwnerActor.GlobalPosition.Flattened());
		var tangent = Clockwise ? awayFromTarget.Cross(Vector3.Up) : Vector3.Up.Cross(awayFromTarget);

		var distance = OwnerActor.GlobalPosition.DistanceTo(target.GlobalPosition);
		var radialError = Mathf.Clamp((distance - OrbitRadius) / OrbitRadius, -1f, 1f);
		var radial = -awayFromTarget * radialError;

		Steerable.SetHeading((tangent * OrbitSpeed + radial).Normalized());
		return BTStatus.Running;
	}
}
