#nullable enable
namespace ActionKit;

/// <summary>Sent every physics frame for every contact the actor's body has.</summary>
public class CollisionMessage(CollisionContact contact) : ActorMessage
{
	public CollisionContact CollisionData = contact;
}

/// <summary>A CollisionMessage where the other side is an Actor.</summary>
public class ActorCollisionMessage(Actor otherActor, CollisionContact contact) : CollisionMessage(contact)
{
	public Actor OtherActor = otherActor;
}
