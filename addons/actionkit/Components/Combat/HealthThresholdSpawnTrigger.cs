#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Sits alongside a plain HealthComponent (doesn't replace it, doesn't touch how or when
/// the actor actually dies) and watches for Health dropping to HealthThreshold or below,
/// then calls Activate() once on EffectsToTrigger — works with either
/// TimedRandomSpawnEffectsComponent or ToggledRandomSpawnEffectsComponent, since both are
/// just a RandomAreaSpawnEffectsComponent as far as this is concerned.
///
/// Composable rather than a single all-in-one death sequence: drop one of these on a
/// ship at 0.5 for an early warning shower of sparks, another at 0.15 pointed at a
/// heavier effect for the "about to go" state, and — if the actual Kill() should also
/// wait for the show to finish rather than firing the instant Health hits zero — pair
/// with a HealthComponent that has FreeOnDepleted turned off.
/// </summary>
[GlobalClass]
public partial class HealthThresholdSpawnTrigger : ActorComponent
{
	[Export(PropertyHint.Range, "0,1")] public float HealthThreshold { get; set; } = 0.25f;
	[Export] public RandomAreaSpawnEffectsComponent? EffectsToTrigger { get; set; }

	private HealthComponent? _health;
	private bool _hasTriggered;

	public override void OnActorReady()
	{
		_health = ActorOwner.GetComponent<HealthComponent>();
		if (_health != null)
			_health.Damaged += CheckThreshold;
	}

	public override void OnActorDetached()
	{
		if (IsInstanceValid(_health))
			_health.Damaged -= CheckThreshold;
	}

	private void CheckThreshold()
	{
		if (_hasTriggered || _health == null || EffectsToTrigger == null) return;
		if (_health.MaxHealth <= 0) return;

		var fraction = (float)_health.Health / _health.MaxHealth;
		if (fraction > HealthThreshold) return;

		_hasTriggered = true;
		EffectsToTrigger.Activate();
	}
}
