#nullable enable
using Godot;

namespace ActionKit.Components;

[GlobalClass]
public partial class OnKillSpawn : SpawnComponent
{
	// Spawns a random number of times between MinCount and MaxCount (inclusive) on kill.
	[Export] public int MinCount { get; set; } = 1;
	[Export] public int MaxCount { get; set; } = 1;

	public override void OnActorReady()
	{
		ActorOwner.Connect(Actor.SignalName.IsKilled, Callable.From(OnKilled));
	}


	private void OnKilled()
	{
		// Hand the spawned actor our own spawner when we have one (a missile's explosion
		// belongs to the ship that fired it — NoCollisionWithSpawner etc. rely on this),
		// otherwise ourselves. The owner is queued for deletion at this point, so
		// Parent = Source falls back to the scene inside ActorFactory.
		var source = IsInstanceValid(ActorOwner.Spawner) ? ActorOwner.Spawner : ActorOwner;
		var count = GD.RandRange(MinCount, Mathf.Max(MinCount, MaxCount));
		for (var i = 0; i < count; i++)
			Spawn(ActorOwner.GlobalTransform, source);
	}
}
