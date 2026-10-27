#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>Scores targets by distance, normalized over MaxDistance.</summary>
[GlobalClass]
public partial class DistanceSelector : TargetSelector
{
	[Export] public float MaxDistance { get; set; } = 50f;

	/// <summary>On: nearer targets score higher. Off: farther targets score higher.</summary>
	[Export] public bool PreferCloser { get; set; } = true;

	protected override float EvaluateMethod(Actor target)
	{
		if (MaxDistance <= 0f) return 0f;

		var distance = Mathf.Clamp(target.GlobalPosition.DistanceTo(Actor.GlobalPosition), 0, MaxDistance);
		var normalized = distance / MaxDistance;
		return PreferCloser ? 1f - normalized : normalized;
	}
}
