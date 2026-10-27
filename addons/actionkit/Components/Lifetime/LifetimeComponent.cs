#nullable enable
using Godot;

namespace ActionKit.Components;

[GlobalClass]
public partial class LifetimeComponent : ActorComponent
{
	[Export] public float Lifetime { get; set; } = 1.0f;

	public override void OnActorReady()
	{
		ActorOwner.GetTree().CreateTimer(Lifetime).Timeout += TerminateActor;
	}

	private void TerminateActor()
	{
		if (IsInstanceValid(ActorOwner))
			ActorOwner.Kill(new DeathMessage{Cause = DeathMessage.DeathCause.Timeout});
	}
}
