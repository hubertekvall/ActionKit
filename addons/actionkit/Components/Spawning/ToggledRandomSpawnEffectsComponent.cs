#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Random-area spawning gated by a bool instead of a timer — runs for as long as IsActive
/// is true, with no Duration to run out on its own. Toggle it from gameplay code (a
/// system taking damage, a status effect starting or ending, a room's atmosphere
/// venting) via SetActive or Activate, or flip the export to have it start pre-armed in
/// the scene.
/// </summary>
[GlobalClass]
public partial class ToggledRandomSpawnEffectsComponent : RandomAreaSpawnEffectsComponent
{
	[Export] public bool IsActive { get; set; } = false;

	// Turning on goes through Activate so a fresh interval gets rolled instead of firing
	// off whatever stale (possibly already-elapsed) timer value was left from before.
	// Turning off doesn't need that — it just stops the countdown wherever it is.
	public void SetActive(bool active)
	{
		if (active) Activate();
		else IsActive = false;
	}

	public override void Activate()
	{
		IsActive = true;
		base.Activate();
	}

	protected override bool IsSpawning() => IsActive;
}
