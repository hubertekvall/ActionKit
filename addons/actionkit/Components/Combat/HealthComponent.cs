#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Hit points. Listens for DamageMessages (negative values heal, capped at MaxHealth) and
/// kills the actor when Health reaches zero.
/// </summary>
[GlobalClass]
public partial class HealthComponent : ActorComponent
{
	[Export] public int MaxHealth { get; set; } = 100;

	/// <summary>
	/// Free the actor when it dies. Turn off to keep the wreck around (it still gets IsKilled
	/// and the DeathMessage), e.g. for a death sequence or debris that sinks.
	/// </summary>
	[Export] public bool FreeOnDepleted { get; set; } = true;

	[Signal]
	public delegate void DamagedEventHandler();

	[Signal]
	public delegate void HealedEventHandler();

	// Fired once, right when Health first reaches zero — before OnDepleted runs, so
	// anything just watching health (a UI bar, an alarm sound) can react without needing
	// to know whether this actor dies instantly or has a whole death sequence ahead of it.
	[Signal]
	public delegate void DepletedEventHandler();

	public int Health { get; private set; }

	/// <summary>Health / MaxHealth, 0..1.</summary>
	public float Fraction => MaxHealth > 0 ? (float)Health / MaxHealth : 0f;

	public override void OnActorReady()
	{
		Health = MaxHealth;
	}

	public override void OnMessage(ActorMessage message)
	{
		if (message is not DamageMessage damage) return;
		if (Health <= 0) return;

		var oldHealth = Health;
		Health = Mathf.Min(Health - damage.DamageValue, MaxHealth);

		if (Health < oldHealth) EmitSignalDamaged();
		else if (Health > oldHealth) EmitSignalHealed();

		if (Health > 0) return;

		EmitSignalDepleted();
		OnDepleted();
	}

	protected virtual void OnDepleted() =>
		ActorOwner.Kill(new DeathMessage { Cause = DeathMessage.DeathCause.Damage }, FreeOnDepleted);
}
