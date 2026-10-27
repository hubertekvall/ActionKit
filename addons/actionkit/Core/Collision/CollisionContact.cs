#nullable enable
using System.Collections.Generic;
using Godot;

namespace ActionKit;

/// <summary>Builds CollisionContacts from the different kinds of physics results.</summary>
public static class CollisionContactFactory
{
	public static CollisionContact? GenerateContact(PhysicsDirectBodyState3D state, int contactIndex) =>
		ConstructContact(
			state.GetContactLocalPosition(contactIndex),
			state.GetContactLocalNormal(contactIndex),
			state.GetContactColliderObject(contactIndex) as CollisionObject3D,
			state.GetContactColliderPosition(contactIndex),
			state.GetContactColliderVelocityAtPosition(contactIndex));


	public static CollisionContact? GenerateContact(KinematicCollision3D collision)
	{
		var collider = collision.GetCollider() as CollisionObject3D;
		return ConstructContact(
			collision.GetPosition(),
			collision.GetNormal(),
			collider,
			collider?.GlobalPosition ?? collision.GetPosition(),
			collision.GetColliderVelocity());
	}


	// Area3D overlaps carry no true contact point/normal or collider velocity the way a
	// RigidBody3D contact or CharacterBody3D slide collision does. Approximate: use our own
	// position as the contact point and the direction toward the other collider as a
	// stand-in normal, and read the other side's velocity through its IActorBody3D.
	public static CollisionContact? GenerateContact(Node3D self, CollisionObject3D other)
	{
		var otherVelocity = (other.GetParent() as Actor)?.ActorBody?.LinearVelocity ?? Vector3.Zero;

		return ConstructContact(
			self.GlobalPosition,
			self.GlobalPosition.DirectionTo(other.GlobalPosition),
			other,
			other.GlobalPosition,
			otherVelocity);
	}


	private static CollisionContact? ConstructContact(Vector3 position, Vector3 normal, CollisionObject3D? otherCollider,
		Vector3 otherPosition, Vector3 otherVelocity)
	{
		if (otherCollider == null || !GodotObject.IsInstanceValid(otherCollider)) return null;

		return otherCollider.GetParent() is Actor otherActor
			? new ActorCollisionContact(position, normal, otherCollider, otherPosition, otherVelocity, otherActor)
			: new CollisionContact(position, normal, otherCollider, otherPosition, otherVelocity, otherCollider);
	}


	public static IEnumerable<CollisionContact> GetContacts(IActorBody3D actorBody)
	{
		switch (actorBody)
		{
			case ActorCharacterBody3D actorCharacterBody:
			{
				for (var i = 0; i < actorCharacterBody.GetSlideCollisionCount(); i++)
				{
					var contact = GenerateContact(actorCharacterBody.GetSlideCollision(i));
					if (contact != null) yield return contact;
				}

				break;
			}
			case ActorRigidBody3D { PhysicsState: { } state }:
			{
				for (var i = 0; i < state.GetContactCount(); i++)
				{
					var contact = GenerateContact(state, i);
					if (contact != null) yield return contact;
				}

				break;
			}
		}
	}


	/// <summary>Broadcasts every current contact of the body to its actor as a CollisionMessage.</summary>
	public static void CollectAndBroadcast(this IActorBody3D actorBody)
	{
		if (actorBody.ActorOwner == null) return;

		foreach (var contact in GetContacts(actorBody))
			actorBody.ActorOwner.Broadcast(contact.GenerateCollisionMessage());
	}
}

/// <summary>
/// One contact point. OtherOwner is the other Actor when there is one, otherwise the
/// collider itself, so it is never null.
/// </summary>
public class CollisionContact(
	Vector3 position,
	Vector3 normal,
	CollisionObject3D otherBody,
	Vector3 otherPosition,
	Vector3 otherVelocity,
	Node otherOwner)
{
	public Vector3 Position = position;
	public Vector3 Normal = normal;
	public CollisionObject3D OtherBody = otherBody;
	public Vector3 OtherPosition = otherPosition;
	public Vector3 OtherVelocity = otherVelocity;
	public Node OtherOwner = otherOwner;

	public virtual CollisionMessage GenerateCollisionMessage() => new(this);
}

/// <summary>A contact whose other side is an Actor.</summary>
public class ActorCollisionContact(
	Vector3 position,
	Vector3 normal,
	CollisionObject3D otherBody,
	Vector3 otherPosition,
	Vector3 otherVelocity,
	Actor otherActor) :
	CollisionContact(position, normal, otherBody, otherPosition, otherVelocity, otherActor)
{
	public Actor OtherActor = otherActor;

	public override CollisionMessage GenerateCollisionMessage() => new ActorCollisionMessage(OtherActor, this);
}
