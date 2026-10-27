#nullable enable
using Godot;

namespace ActionKit.Components;

[GlobalClass]
public partial class OnReadySpawn : SpawnComponent
{
	// Deferred: OnActorReady runs inside the owner's _Ready, and when the owner is part of a
	// scene that's still loading, the current scene is "busy setting up children" and
	// refuses AddChild. One deferred call later everything is settled.
	public override void OnActorReady() =>
		Callable.From(SpawnOnReady).CallDeferred();

	private void SpawnOnReady()
	{
		if (!IsInstanceValid(ActorOwner)) return;
		Spawn(ActorOwner.GlobalTransform, ActorOwner);
	}
}
