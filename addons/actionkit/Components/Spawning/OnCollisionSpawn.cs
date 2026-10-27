#nullable enable
using Godot;

namespace ActionKit.Components;

[GlobalClass]
public partial class OnCollisionSpawn : SpawnComponent
{
	public override void OnMessage(ActorMessage message)
	{
		if (message is not CollisionMessage collisionMessage) return;
		Spawn(new Transform3D(Basis.Identity, collisionMessage.CollisionData.Position), ActorOwner);
	}
}
