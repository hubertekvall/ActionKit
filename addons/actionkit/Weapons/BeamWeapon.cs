#nullable enable
using System.Collections.Generic;
using Godot;

namespace ActionKit.Weapons;

/// <summary>
/// A Weapon that holds a beam instead of firing shots. The first shot spawns BeamScene
/// (a BeamEffect) as a child of each emitter so it follows the muzzle; later shots just keep
/// it alive. When the trigger is let go (the weapon goes Released) or it runs dry
/// (Reloading), the beams are killed. Ammo counts down once per FireTimings interval, so
/// MagazineSize × fire timing is how long a beam can be held.
/// </summary>
[GlobalClass]
public partial class BeamWeapon : Weapon
{
	[Export] public PackedScene? BeamScene { get; set; }

	private readonly Dictionary<Node3D, Actor> _activeBeams = new();

	protected override void SpawnPayload(Node3D emitter, Transform3D transform)
	{
		if (BeamScene == null)
		{
			base.SpawnPayload(emitter, transform);
			return;
		}

		if (_activeBeams.TryGetValue(emitter, out var existing) && IsInstanceValid(existing)) return;

		if (BeamScene.Instantiate() is not Actor beam)
		{
			GD.PushError($"{Name}: BeamScene's root must be an Actor (e.g. BeamEffect).");
			return;
		}

		beam.Spawner = Spawner;
		emitter.AddChild(beam);
		_activeBeams[emitter] = beam;
	}

	protected override void EnterState(WeaponState state)
	{
		base.EnterState(state);
		if (state is WeaponState.Released or WeaponState.Reloading)
			KillBeams();
	}

	public void KillBeams()
	{
		foreach (var beam in _activeBeams.Values)
			if (IsInstanceValid(beam)) beam.Kill();

		_activeBeams.Clear();
	}

	public override void _ExitTree()
	{
		KillBeams();
		base._ExitTree();
	}
}
