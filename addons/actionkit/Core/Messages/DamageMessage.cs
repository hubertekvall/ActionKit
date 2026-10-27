#nullable enable
namespace ActionKit;

/// <summary>Deals DamageValue to the receiving actor (HealthComponent listens for it). Negative values heal.</summary>
public class DamageMessage(int damageValue, CollisionContact? contact = null) : ActorMessage
{
	public int DamageValue = damageValue;

	/// <summary>Where the hit landed, when it came from a collision.</summary>
	public CollisionContact? Contact = contact;
}
