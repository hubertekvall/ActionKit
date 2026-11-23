#nullable enable
using System.Linq;
using ActionKit.Components;
using ActionKit.Utilities;
using Godot;

namespace ActionKit.Weapons;

/// <summary>
/// Put this under a Weapon (as a direct child). While the weapon is in Auto mode it looks for
/// members of TargetGroup (or any actor, when empty) within ScanRadius that the weapon can
/// face, nearest first, and sets the weapon's Target. Never targets the weapon's own carrier.
/// With SweepMaxTargets above 1 it sweeps between that many of them.
/// Weapons aimed by something else (a behavior tree's BTAimWeaponsAtTarget) just don't have one.
/// </summary>
[GlobalClass]
public partial class AutoTargeting : ActorComponent
{
	[Export] public float ScanRadius { get; set; } = 10.0f;

	/// <summary>Group to look for. Empty means any Actor other than the weapon's own carrier.</summary>
	[Export] public StringName TargetGroup { get; set; } = "";

	[Export(PropertyHint.Layers3DPhysics)] public uint TargetCollisionMask { get; set; } = uint.MaxValue;

	[Export] public double SweepSpeed { get; set; } = 1.0;
	[Export] public int SweepMaxTargets { get; set; }

	/// <summary>Physics frames between scans. Higher is cheaper with many weapons.</summary>
	[Export(PropertyHint.Range, "1,30")] public int ScanInterval { get; set; } = 5;

	private Weapon? _weapon;
	private double _sweepIndex;

	public override void OnActorReady()
	{
		_weapon = ActorOwner as Weapon;
		if (_weapon == null) GD.PushWarning($"{Name}: AutoTargeting must be a direct child of a Weapon.");
	}

	public override void PhysicsTick(double delta)
	{
		if (_weapon is not { Mode: Weapon.WeaponMode.Auto }) return;
		if (Engine.GetPhysicsFrames() % (ulong)Mathf.Max(1, ScanInterval) != 0) return;

		var targets = AcquireTargets(_weapon);
		if (targets.Count == 0)
		{
			_weapon.Target = null;
			return;
		}

		if (SweepMaxTargets <= 1)
		{
			_sweepIndex = 0.0;
			_weapon.Target = targets[0];
			return;
		}

		_sweepIndex += SweepSpeed * delta;
		_sweepIndex %= SweepMaxTargets - 1;

		// Clamp in case there are fewer targets than SweepMaxTargets.
		var index = Mathf.Min(Mathf.RoundToInt(_sweepIndex), targets.Count - 1);
		_weapon.Target = targets[index];
	}

	/// <summary>Candidate targets, best first. Override for threat/aggro or other priorities.</summary>
	protected virtual QueryList AcquireTargets(Weapon weapon)
	{
		var found = QueryList.QueryRadius(weapon, ScanRadius, TargetCollisionMask).Excluding(weapon, weapon.Spawner);
		found = TargetGroup.IsEmpty ? found.OfActors() : found.OfGroup(TargetGroup);
		return found.Where(target => weapon.CanFace(target.GlobalPosition)).ToQueryList()
			.OrderByClosest(weapon.GlobalPosition);
	}
}
