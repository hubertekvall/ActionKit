#nullable enable
using Godot;

namespace ActionKit.Weapons;

// A Weapon whose magazine is physically visible on the ship — missiles slung under
// wings, boarding pods docked on a hull, anything where firing should visibly remove
// something from the model rather than fire from an invisible muzzle point.
//
// Each entry in the inherited Emitters array does double duty here: it's both the
// emission transform (as for any Weapon) and the visible payload mount that hides on
// firing and reappears on reload. That's deliberate — a second array mapping "mount"
// to "thing to hide" would just be state that can drift out of sync with Emitters
// (wrong mount hides, or a shot fires from an already-emptied one). MagazineSize is
// likewise derived from Emitters.Length rather than left independently tunable, so the
// ammo count always matches what's actually mounted.
[GlobalClass]
public partial class HardpointWeapon : Weapon
{
	// The live actor a mount's contents become once fired — a missile, a boarding pod,
	// whatever. When unset, firing hides the mount and falls back to the regular
	// SpawnEffectMessage, so a SpawnEffectsComponent on the weapon can do the spawning.
	[Export] public PackedScene? PayloadScene { get; set; }

	public override void _Ready()
	{
		if (Emitters.Length > 0) MagazineSize = Emitters.Length;
		base._Ready();
	}

	protected override void SpawnPayload(Node3D emitter, Transform3D transform)
	{
		emitter.Visible = false;

		if (PayloadScene == null)
		{
			base.SpawnPayload(emitter, transform);
			return;
		}

		var payload = ActorFactory.Spawn(PayloadScene, new SpawnParams(transform, Spawner));
		if (payload != null && IsInstanceValid(Target))
			payload.Broadcast(new TargetAssignedMessage(Target!).Signed(this));
	}

	protected override void Reload()
	{
		foreach (var emitter in Emitters)
			emitter.Visible = true;

		base.Reload();
	}
}
