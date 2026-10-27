#nullable enable
using ActionKit.UI;
using Godot;

namespace ActionKit.Components;

/// <summary>Spawns a DamageText scene showing "-N" each time the actor takes damage.</summary>
[GlobalClass]
public partial class DamageTextSpawner : ActorComponent
{
	/// <summary>A scene whose root is a DamageText actor.</summary>
	[Export] public PackedScene? DamageTextScene { get; set; }

	[Export] public string Format { get; set; } = "-{0}";

	public override void PostMessage(ActorMessage message)
	{
		if (message is not DamageMessage { DamageValue: > 0 } damageMessage || DamageTextScene == null) return;

		var position = damageMessage.Contact?.Position ?? ActorOwner.GlobalPosition;
		var spawn = new SpawnParams(new Transform3D(Basis.Identity, position), ActorOwner);
		if (ActorFactory.Spawn(DamageTextScene, spawn) is not DamageText damageText) return;

		damageText.Label?.SetText(string.Format(Format, damageMessage.DamageValue));
	}
}
