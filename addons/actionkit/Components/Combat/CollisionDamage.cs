#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Sends a DamageMessage of DamageMin..DamageMax to any actor this one touches. Put it on
/// projectiles, hazards, melee hitboxes, ...
/// </summary>
[GlobalClass]
public partial class CollisionDamage : ExcludeCollision
{
	[Export] public int DamageMin { get; set; } = 0;
	[Export] public int DamageMax { get; set; } = 0;

	/// <summary>Minimum seconds between damage applications. 0 = no limit.</summary>
	[Export] public float DamageCooldown { get; set; } = 0f;

	private double _lastDamageTime = double.NegativeInfinity;

	public override void ExecuteEffect(CollisionMessage collisionMessage)
	{
		if (collisionMessage is ActorCollisionMessage actorCollisionMessage)
			SendDamage(actorCollisionMessage);
	}

	protected virtual void SendDamage(ActorCollisionMessage actorCollisionMessage)
	{
		var now = Time.GetTicksMsec() / 1000.0;
		if (now - _lastDamageTime < DamageCooldown)
			return;

		_lastDamageTime = now;

		var recipient = actorCollisionMessage.OtherActor;
		recipient.Broadcast(new DamageMessage(CalculateDamage(), actorCollisionMessage.CollisionData).Signed(this));
	}

	protected virtual int CalculateDamage() => GD.RandRange(DamageMin, Mathf.Max(DamageMin, DamageMax));
}
