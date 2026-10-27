#nullable enable
using ActionKit.Components;
using Godot;

namespace ActionKit.AI;

/// <summary>Success when health is below ThresholdPercentage (0..1). Uses the actor's HealthComponent unless Health is set.</summary>
[GlobalClass]
public partial class BTIsHealthBelowThreshold : BTNode
{
	[Export] public HealthComponent? Health { get; set; }
	[Export(PropertyHint.Range, "0, 1")] public float ThresholdPercentage { get; set; } = 0.25f;

	protected override BTStatus Tick()
	{
		var health = Health ?? OwnerActor.GetComponent<HealthComponent>();
		if (health == null) return BTStatus.Failure;

		return health.Fraction < ThresholdPercentage ? BTStatus.Success : BTStatus.Failure;
	}
}
