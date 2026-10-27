#nullable enable
using Godot;

namespace ActionKit.Components;

[GlobalClass]
public partial class OnDamageSpawn : SpawnComponent
{
	private HealthComponent? _healthComponent;

	[Export(PropertyHint.Range, "0, 1")] public float DamageThreshold { get; set; }
	[Export] public float NormalOffset = -1.0f;
	[Export] public float Scaling = 1.0f;
	[Export] public bool ScaleWithDamage = false;

	public override void OnActorReady()
	{
		_healthComponent = ActorOwner.GetComponent<HealthComponent>();
	}

	public override void OnMessage(ActorMessage message)
	{
		if (message is not DamageMessage damageMessage) return;
		if (damageMessage.Contact == null) return;
		if (_healthComponent == null || _healthComponent.MaxHealth <= 0) return;

		var damageFraction = (float)damageMessage.DamageValue / _healthComponent.MaxHealth;
		if (damageFraction >= DamageThreshold) return;

		var position = damageMessage.Contact.Position;
		var normal = damageMessage.Contact.Normal;
		var transform = new Transform3D(Basis.LookingAt(normal, Vector3.Up), position + normal * NormalOffset);

		if (ScaleWithDamage)
			transform = transform.ScaledLocal(Vector3.One * (Scaling * damageFraction));

		Spawn(transform, ActorOwner);
	}
}
