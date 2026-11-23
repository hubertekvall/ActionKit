#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Spawns Scenes at the contact point and parents them to the body that was hit, so they move
/// with it (scorch marks, embedded harpoons, sparks that stick to a hull). -Z of the spawned
/// scene points into the hit surface.
/// </summary>
[GlobalClass]
public partial class OnCollisionAttachSpawn : SpawnComponent
{
	/// <summary>Contacts are reported every physics tick while touching, so throttle.</summary>
	[Export] public float Cooldown { get; set; } = 0.25f;

	private double _nextSpawnTime;

	public override void OnMessage(ActorMessage message)
	{
		if (message is not CollisionMessage collisionMessage) return;

		var now = Time.GetTicksMsec() / 1000.0;
		if (now < _nextSpawnTime) return;

		var contact = collisionMessage.CollisionData;
		var other = contact.OtherBody;
		if (!IsInstanceValid(other) || other.IsQueuedForDeletion()) return;

		_nextSpawnTime = now + Cooldown;

		// Contact normals point different ways per body type, so face it toward the other body.
		var normal = contact.Normal.Normalized();
		if (normal.Dot(contact.OtherPosition - contact.Position) < 0) normal = -normal;
		if (normal.LengthSquared() < 0.0001f) normal = Vector3.Forward;
		var up = Mathf.Abs(normal.Dot(Vector3.Up)) > 0.99f ? Vector3.Forward : Vector3.Up;

		Spawn(new Transform3D(Basis.LookingAt(normal, up), contact.Position), ActorOwner, other);
	}
}
