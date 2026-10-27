#nullable enable
using Godot;

namespace ActionKit.Components;

[GlobalClass]
public partial class OnDamageFadeTransparency : ActorComponent
{
	[Export] public MeshInstance3D? MeshInstance { get; set; }
	[Export] public float StartValue { get; set; }
	[Export] public float EndValue { get; set; }
	[Export] public float Duration { get; set; }

	public override void OnMessage(ActorMessage message)
	{
		if (message is not DamageMessage damageMessage) return;
		
		
		

		var tween = GetTree().CreateTween();
		tween.TweenProperty(MeshInstance, "transparency", StartValue, Duration);
		tween.TweenProperty(MeshInstance, "transparency", EndValue, Duration);
	}
}
