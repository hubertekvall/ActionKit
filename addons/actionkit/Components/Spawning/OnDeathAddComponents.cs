#nullable enable
using Godot;

namespace ActionKit.Components;

[GlobalClass]
public partial class OnDeathAddComponents : ComponentBreeder
{
	public override void PostMessage(ActorMessage message)
	{
		if (message is not DeathMessage dm) return;
		
		AttachComponents();
	}
}
