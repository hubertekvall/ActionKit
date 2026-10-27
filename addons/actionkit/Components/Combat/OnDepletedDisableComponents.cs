#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// When the actor's HealthComponent is depleted, disables the listed components (a
/// HoverComponent so the wreck sinks, an input controller, a weapon trigger, ...), while
/// everything else keeps running.
/// </summary>
[GlobalClass]
public partial class OnDepletedDisableComponents : ActorComponent
{
	[Export] public ActorComponent[] Components { get; set; } = [];

	private HealthComponent? _health;

	public override void OnActorReady()
	{
		_health = ActorOwner.GetComponent<HealthComponent>();
		if (_health != null)
			_health.Depleted += Disable;
	}

	public override void OnActorDetached()
	{
		if (IsInstanceValid(_health))
			_health!.Depleted -= Disable;
	}

	private void Disable()
	{
		foreach (var component in Components)
			if (IsInstanceValid(component))
				component.Enabled = false;
	}
}
