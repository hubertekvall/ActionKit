#nullable enable
namespace ActionKit;

/// <summary>Broadcast by Actor.Kill right before the actor is freed.</summary>
public class DeathMessage : ActorMessage
{
	public enum DeathCause
	{
		Collision,
		Damage,
		Timeout,
		Call
	}

	public DeathCause Cause { get; set; }

	/// <summary>Best guess at the cause from the most recent message the actor received.</summary>
	public static DeathCause GetDeathCause(Actor dyingActor)
	{
		if (dyingActor.MessageBuffer.TryPeek(out var message))
		{
			return message switch
			{
				CollisionMessage => DeathCause.Collision,
				DamageMessage => DeathCause.Damage,
				_ => DeathCause.Timeout
			};
		}

		return DeathCause.Timeout;
	}
}
