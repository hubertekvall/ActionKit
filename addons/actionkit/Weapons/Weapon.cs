#nullable enable
using System;
using ActionKit.Utilities;
using Godot;

namespace ActionKit.Weapons;

/// <summary>
/// A weapon is an Actor of its own, nested anywhere under the actor that carries it (its
/// Spawner). It runs a small state machine (Ready → Firing → Ready/Reloading, and Released
/// after sitting idle), cycles through Emitters, and on every shot calls SpawnPayload, which
/// by default broadcasts a SpawnEffectMessage to its own components. Put a
/// SpawnEffectsComponent on the weapon to turn that into projectiles, muzzle flashes, sounds.
///
/// Targeting: with AutoTarget on, the weapon finds its own targets (TargetGroup within
/// ScanRadius, nearest first, sweeping between up to SweepMaxTargets of them). Turn it off
/// and set Target from outside instead (a behavior tree, a player aim script). With AutoFire
/// on it fires whenever it's on target; otherwise call FireWeapons() yourself.
/// </summary>
[GlobalClass]
public partial class Weapon : Actor
{
	[ExportGroup("Targeting")]
	[Export] public bool AutoTarget { get; set; } = true;

	[Export] public float ScanRadius { get; set; } = 10.0f;

	/// <summary>Group that AutoTarget looks for. Empty means any Actor other than this weapon's own carrier.</summary>
	[Export] public StringName TargetGroup { get; set; } = "";

	[Export(PropertyHint.Layers3DPhysics)] public uint TargetCollisionMask { get; set; } = uint.MaxValue;

	[Export] public double SweepSpeed { get; set; } = 1.0;
	[Export] public int SweepMaxTargets { get; set; }

	public Node3D? Target { get; set; }

	[ExportGroup("Firing")]
	[Export] public bool AutoFire { get; set; } = true;

	/// <summary>When off, FireWeapons() fires even without a target (manual, player-aimed weapons).</summary>
	[Export] public bool RequireTarget { get; set; } = true;

	/// <summary>The muzzles shots come from, used round-robin. Defaults to the weapon itself.</summary>
	[Export] public Node3D[] Emitters { get; set; } = [];

	/// <summary>Random sideways/vertical offset applied to each shot's spawn point.</summary>
	[Export] public float EmissionSpread { get; set; } = 0.4f;

	/// <summary>Seconds between shots, cycled in order. [0.1, 0.1, 0.6] gives three-round bursts.</summary>
	[Export] public float[] FireTimings { get; set; } = [0.2f];

	[ExportGroup("Ammo")]
	[Export] public double ReloadTime { get; set; } = 2.0f;

	[Export] public int MagazineSize { get; set; } = 10;

	/// <summary>How long the weapon can sit idle in Ready before it's considered Released.</summary>
	[Export] public double ReleaseTimer { get; set; } = 0.5f;

	private int _fireTimingIndex;

	public enum WeaponState
	{
		Ready,
		Firing,
		Reloading,
		Released,
	}

	/// <summary>Current state. UI can poll this as well as listen to the signals.</summary>
	public WeaponState State { get; private set; } = WeaponState.Ready;

	public double Timer { get; protected set; }
	public int Ammo { get; protected set; }

	public float GetFireTiming() => FireTimings.Length == 0 ? 0.2f : FireTimings[Mathf.Clamp(_fireTimingIndex, 0, FireTimings.Length - 1)];

	public float PopFireTiming()
	{
		if (FireTimings.Length == 0) return 0.2f;
		_fireTimingIndex = (_fireTimingIndex + 1) % FireTimings.Length;
		return FireTimings[_fireTimingIndex];
	}

	/// <summary>0..1 progress of the current timed state (shot cooldown or reload). Bind UI to it.</summary>
	public double Progress => State switch
	{
		WeaponState.Firing => Timer / GetFireTiming(),
		WeaponState.Reloading => Timer / ReloadTime,
		_ => 0.0,
	};

	// EnterState is the single place any Weapon signal is emitted — one signal per state,
	// fired exactly when that state is entered.
	[Signal]
	public delegate void FiredEventHandler();

	[Signal]
	public delegate void ReloadingEventHandler();

	[Signal]
	public delegate void ReadyStateEventHandler();

	[Signal]
	public delegate void ReleasedEventHandler();

	[Signal]
	public delegate void StateChangedEventHandler(WeaponState state);


	private double _sweepIndex;
	private int _emitterIndex;
	private QueryList _targets = [];

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint()) return;

		Spawner ??= this.FindAncestorOfType<Actor>();
		if (Emitters.Length == 0) Emitters = [this];

		Ammo = MagazineSize;
		EnterState(WeaponState.Ready);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (Engine.IsEditorHint()) return;

		if (AutoTarget) SweepTargets(delta);
		if (AutoFire) FireWeapons();
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if (Engine.IsEditorHint()) return;

		TickTimer(delta);
	}


	public virtual void SweepTargets(double delta)
	{
		if (Engine.GetPhysicsFrames() % 5 != 0) return;

		_targets = AcquireTargets() ?? [];
		if (_targets.Count == 0)
		{
			Target = null;
			return;
		}

		if (SweepMaxTargets <= 1)
		{
			_sweepIndex = 0.0;
			Target = _targets[0];
			return;
		}

		_sweepIndex += SweepSpeed * delta;
		_sweepIndex %= SweepMaxTargets - 1;

		// Clamp in case the list shrank (a target died) since the last pick.
		var targetIndex = Mathf.Min(Mathf.RoundToInt(_sweepIndex), _targets.Count - 1);
		Target = _targets[targetIndex];
	}

	/// <summary>
	/// Default targeting: members of TargetGroup (or any actor) within ScanRadius, nearest
	/// first, never the weapon's own carrier. Override for anything more elaborate.
	/// </summary>
	protected virtual QueryList? AcquireTargets()
	{
		var found = QueryList.QueryRadius(this, ScanRadius, TargetCollisionMask).Excluding(this, Spawner);
		found = TargetGroup.IsEmpty ? found.OfActors() : found.OfGroup(TargetGroup);
		return found.OrderByClosest(GlobalPosition);
	}


	/// <summary>
	/// Starts a shot if the weapon is idle (Ready/Released), has ammo and is on target (or
	/// RequireTarget is off). Safe to call every frame, e.g. while a fire button is held.
	/// </summary>
	public void FireWeapons()
	{
		if (RequireTarget && !IsOnTarget()) return;
		if (!CanFire()) return;

		FireNextShot();
	}

	private void FireNextShot()
	{
		var emitter = GetEmitter();
		var transform = emitter.GlobalTransform.TranslatedLocal(GetEmissionOffset());

		SpawnPayload(emitter, transform);

		EnterState(WeaponState.Firing);
	}

	/// <summary>
	/// What a shot produces. Default: a SpawnEffectMessage broadcast to this weapon's
	/// components. Override to fire something else (a beam, a hitscan ray, a visible payload).
	/// </summary>
	protected virtual void SpawnPayload(Node3D emitter, Transform3D transform)
	{
		Broadcast(new SpawnEffectMessage
		{
			Spawner = Spawner,
			Transform = transform,
			Target = IsInstanceValid(Target) ? Target : null,
		}.Signed(this));
	}


	public virtual bool IsOnTarget() => IsInstanceValid(Target);


	/// <summary>Direction to aim, blending across swept targets. Forward when there's nothing to aim at.</summary>
	public virtual Vector3 GetAimDirection()
	{
		if (!AutoTarget || _targets.Count == 0)
			return IsInstanceValid(Target) ? GlobalPosition.DirectionTo(Target!.GlobalPosition) : -GlobalBasis.Z;

		var firstTarget = _targets[0];
		var lastTarget = _targets[^1];

		var firstDir = IsInstanceValid(firstTarget) ? GlobalPosition.DirectionTo(firstTarget.GlobalPosition) : -GlobalBasis.Z;
		var lastDir = IsInstanceValid(lastTarget) ? GlobalPosition.DirectionTo(lastTarget.GlobalPosition) : -GlobalBasis.Z;

		// _sweepIndex runs 0..(count-1); normalize to 0..1 for the slerp weight.
		var weight = (float)(_sweepIndex / Mathf.Max(1, _targets.Count - 1));
		return firstDir.Slerp(lastDir, Mathf.Clamp(weight, 0f, 1f));
	}


	protected Node3D GetEmitter()
	{
		var emitter = Emitters[_emitterIndex % Emitters.Length];
		_emitterIndex = (_emitterIndex + 1) % Emitters.Length;
		return emitter;
	}

	protected Vector3 GetEmissionOffset() =>
		new((float)GD.RandRange(-EmissionSpread, EmissionSpread), (float)GD.RandRange(-EmissionSpread, EmissionSpread), 0);


	protected virtual void EnterState(WeaponState state)
	{
		State = state;

		switch (state)
		{
			case WeaponState.Ready:
				Timer = ReleaseTimer;
				EmitSignalReadyState();
				break;
			case WeaponState.Firing:
				Timer = PopFireTiming();
				Ammo--;
				EmitSignalFired();
				break;
			case WeaponState.Reloading:
				Timer = ReloadTime;
				EmitSignalReloading();
				break;
			case WeaponState.Released:
				Timer = double.PositiveInfinity;
				EmitSignalReleased();
				break;
		}

		EmitSignalStateChanged(state);
	}

	protected virtual void OnTimerFinished()
	{
		switch (State)
		{
			case WeaponState.Ready:
				// Nothing fired before ReleaseTimer ran out.
				EnterState(WeaponState.Released);
				break;
			case WeaponState.Firing:
				EnterState(Ammo > 0 ? WeaponState.Ready : WeaponState.Reloading);
				break;
			case WeaponState.Reloading:
				Reload();
				break;
		}
	}

	protected virtual void Reload()
	{
		Ammo = MagazineSize;
		EnterState(WeaponState.Ready);
	}

	// Ready and Released are both "idle, can start firing" — the only difference is
	// whether the release signal has already fired for this gap.
	protected bool CanFire() => State is WeaponState.Ready or WeaponState.Released && Ammo > 0;

	protected virtual void TickTimer(double delta)
	{
		Timer -= delta;
		if (Timer <= 0) OnTimerFinished();
	}
}
