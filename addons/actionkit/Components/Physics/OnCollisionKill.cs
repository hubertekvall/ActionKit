#nullable enable
using Godot;

namespace ActionKit.Components;

[GlobalClass]
public partial class OnCollisionKill : ExcludeCollision
{
	[Export] public bool FreeImmediately = true;
	
	public override void ExecuteEffect(CollisionMessage collisionMessage)
	{	
		
		ActorOwner.Kill(new DeathMessage{Cause = DeathMessage.DeathCause.Collision}, FreeImmediately);
	}
}
