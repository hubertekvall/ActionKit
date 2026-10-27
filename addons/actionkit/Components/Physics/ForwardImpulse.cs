#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>One-shot push along the actor's forward (-Z) when it becomes ready. Launches projectiles.</summary>
[GlobalClass]
public partial class ForwardImpulse : ActorComponent
{
	[Export] public float Force { get; set; } = 10.0f;

	public override void OnActorReady()
	{
		ActorOwner.ActorBody?.ApplyCentralImpulse(Force * -ActorOwner.GlobalBasis.Z);
	}
}
