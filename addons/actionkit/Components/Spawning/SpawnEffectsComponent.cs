#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Spawns Scenes whenever a SpawnEffectMessage is broadcast on the owning actor: the
/// weapon-fire path. Put it on a Weapon with projectile/muzzle-flash scenes. If the weapon had
/// a target, every spawned actor gets a TargetAssignedMessage (HomingComponent uses it).
/// </summary>
[GlobalClass]
public partial class SpawnEffectsComponent : SpawnComponent
{
	public override void OnMessage(ActorMessage message)
	{
		if (message is not SpawnEffectMessage spawnMessage) return;

		var spawned = Spawn(spawnMessage.Transform, spawnMessage.Spawner);
		if (!IsInstanceValid(spawnMessage.Target)) return;

		foreach (var actor in spawned)
			actor.Broadcast(new TargetAssignedMessage(spawnMessage.Target!).Signed(this));
	}
}
