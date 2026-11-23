#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// On every DamageMessage, sets an instance shader parameter ("damage" by default) on the
/// meshes to the fraction of health lost (0 = full health, 1 = dead). Drive cracks, scorching
/// or flicker from it in the shader.
/// </summary>
[GlobalClass]
public partial class HealthDamageShaderParameter : ActorComponent
{
	[Export] public GeometryInstance3D[] VisualInstances { get; set; } = [];
	[Export] public StringName ParameterName { get; set; } = "damage";

	private HealthComponent? _health;

	public override void OnActorReady()
	{
		_health = ActorOwner.GetComponent<HealthComponent>();
		if (_health == null) GD.PushWarning($"{Name}: no HealthComponent on {ActorOwner.Name}.");
	}

	public override void OnMessage(ActorMessage message)
	{
		if (message is not DamageMessage) return;

		// Deferred so HealthComponent has applied the damage, whatever the component order.
		Callable.From(UpdateDamage).CallDeferred();
	}

	private void UpdateDamage()
	{
		if (_health == null || _health.MaxHealth <= 0) return;

		var damage = 1f - Mathf.Clamp(_health.Fraction, 0f, 1f);

		foreach (var instance in VisualInstances)
		{
			if (!IsInstanceValid(instance)) continue;
			instance.SetInstanceShaderParameter(ParameterName, damage);
		}
	}
}
