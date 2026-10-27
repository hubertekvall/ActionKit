#nullable enable
using ActionKit.Weapons;
using Godot;

namespace ActionKit.Components;

/// <summary>Stops the actor colliding with the actor that spawned it (a bullet with its own ship).</summary>
[GlobalClass]
public partial class NoCollisionWithSpawner : ActorComponent
{
	public override void OnActorReady()
	{
		if (ActorOwner.Spawner?.Body is not CollisionObject3D spawnerBody) return;

		if (ActorOwner.ActorBody is PhysicsBody3D physicsBody3D)
			physicsBody3D.AddCollisionExceptionWith(spawnerBody);

		if (ActorOwner is BeamEffect beam)
			beam.ShapeCast?.AddException(spawnerBody);
	}
}
