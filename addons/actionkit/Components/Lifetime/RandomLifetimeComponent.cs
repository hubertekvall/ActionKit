#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// LifetimeComponent that rolls its own lifetime once when it starts: Lifetime plus or minus a
/// random amount up to Spread, never below zero. Each duplicate (e.g. every MeshSlicer debris
/// piece) rolls separately.
/// </summary>
[GlobalClass]
public partial class RandomLifetimeComponent : LifetimeComponent
{
	[Export] public float Spread { get; set; } = 0.5f;

	public override void OnActorReady()
	{
		Lifetime = Mathf.Max(0f, Lifetime + (float)GD.RandRange(-Spread, Spread));
		base.OnActorReady();
	}
}
