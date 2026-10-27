#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Random-area spawning that runs for a fixed Duration and then stops on its own — a
/// spark shower after a hull breach, a limited-time debris field, anything that should
/// burn itself out rather than keep spawning for the rest of the actor's life. Starts
/// automatically on ready, and Activate() can be called again later (e.g. from a
/// health-threshold trigger) to restart the countdown. For an always-on-until-told-
/// otherwise version instead, see ToggledRandomSpawnEffectsComponent.
/// </summary>
[GlobalClass]
public partial class TimedRandomSpawnEffectsComponent : RandomAreaSpawnEffectsComponent
{
	[ExportGroup("Timing")]
	[Export] public float Duration { get; set; } = 5.0f;

	private float _remaining;

	public override void OnActorReady()
	{
		base.OnActorReady();
		Activate();
	}

	public override void Activate()
	{
		_remaining = Duration;
		base.Activate();
	}

	public override void PhysicsTick(double delta)
	{
		// Count down first, then let the base class check IsSpawning() — so the tick that
		// crosses zero still gets evaluated as "just finished" rather than lagging a frame.
		if (_remaining > 0f)
			_remaining -= (float)delta;

		base.PhysicsTick(delta);
	}

	protected override bool IsSpawning() => _remaining > 0f;
}
