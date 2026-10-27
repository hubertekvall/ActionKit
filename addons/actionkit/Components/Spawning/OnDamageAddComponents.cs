#nullable enable
using Godot;

namespace ActionKit.Components;

[GlobalClass]
public partial class OnDamageAddComponents : ComponentBreeder
{
	
	[Export] public float DamageThreshold { get; set; }
	[Export] public bool Repeat { get; set; } = false;
	private HealthComponent? _healthComponent;
	
	public override void OnActorReady()
	{
		_healthComponent = ActorOwner.GetComponent<HealthComponent>();
		if(_healthComponent != null) _healthComponent.Damaged += CheckThreshold;
	}


	public void CheckThreshold()
	{
		if (_healthComponent == null) return;
		var ratio = (float)_healthComponent.Health / (float)_healthComponent.MaxHealth;
		

	
		if(ratio <= DamageThreshold) AttachComponents();
		else return;
	
		if (!Repeat) _healthComponent = null;
	}
	


}
