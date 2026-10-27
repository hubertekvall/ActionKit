#nullable enable
using ActionKit.Effects;
using Godot;

namespace ActionKit.Components;

/// <summary>Adds trauma to the CameraShake when the actor becomes ready. Put it on explosions.</summary>
[GlobalClass]
public partial class CameraShakeOnReady : ActorComponent
{
	[Export(PropertyHint.Range, "0,1")] public float Trauma { get; set; } = 0.25f;

	public override void OnActorReady()
	{
		CameraShake.Instance?.AddTrauma(Trauma, ActorOwner.GlobalPosition);
	}
}
