#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Scores a candidate target for TargetFinder. Higher is better; scores from all selectors
/// are summed, each weighted by Priority. Subclass it for threat, health, angle, ...
/// </summary>
[GlobalClass]
public abstract partial class TargetSelector : Resource
{
	/// <summary>The actor doing the targeting. Set by TargetFinder.</summary>
	public Actor Actor { get; set; } = null!;

	[Export(PropertyHint.Range, "1, 10")] public int Priority { get; set; } = 1;

	public virtual float Evaluate(Actor target) => EvaluateMethod(target) * Priority;

	/// <summary>A score, ideally 0..1.</summary>
	protected abstract float EvaluateMethod(Actor target);
}
