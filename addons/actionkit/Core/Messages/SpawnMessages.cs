#nullable enable
using Godot;

namespace ActionKit;

/// <summary>
/// Broadcast by a Weapon when it fires, so anything listening (SpawnEffectsComponent,
/// audio, VFX, ...) can react without the Weapon needing a reference to them.
/// </summary>
public class SpawnEffectMessage : ActorMessage
{
	/// <summary>The actor that should own whatever gets spawned (usually the ship carrying the weapon).</summary>
	public Actor? Spawner { get; set; }

	public Transform3D Transform { get; set; }

	/// <summary>What the weapon was aiming at, if anything. Passed on to spawned actors.</summary>
	public Node3D? Target { get; set; }
}

/// <summary>
/// Tells an actor what to go after. SpawnEffectsComponent sends it to every projectile it
/// spawns for a weapon with a target; HomingComponent listens for it.
/// </summary>
public class TargetAssignedMessage(Node3D target) : ActorMessage
{
	public Node3D Target = target;
}
