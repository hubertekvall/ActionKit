#nullable enable
using System;
using ActionKit.Utilities;
using Godot;

namespace ActionKit.Weapons;

/// <summary>
/// A weapon is an Actor of its own, nested anywhere under the actor that carries it (its
/// Spawner). It runs a small state machine (Ready → Firing → Ready/Reloading, Released after
/// sitting idle, Disabled while switched Off), cycles through Emitters, and on every shot
/// calls SpawnPayload, which by default broadcasts a SpawnEffectMessage to its own
/// components. Put a SpawnEffectsComponent on the weapon to turn that into projectiles,
/// muzzle flashes, sounds (its RandomOffset gives shot spread).
///
/// Mode decides what it aims at:
///   Auto   — aims at Target, which something else picks: an AutoTargeting component under
///            the weapon, a behavior tree (BTAimWeaponsAtTarget), or your own code. Fires on
///            its own while AutoFire is on.
///   Manual — aims at ManualAimPoint (MouseAim sets it from the mouse) and fires only while
///            TriggerHeld is true.
///   Off    — does nothing (State is Disabled).
/// CycleMode() steps Auto → Manual → Off → Auto (WeaponHotkeys binds it to number keys).
/// </summary>
[GlobalClass]
public partial class Weapon : Actor
{
	public enum WeaponMode
	{
		Auto,
		Manual,
		Off,
	}

	private WeaponMode _mode = WeaponMode.Auto;

	[ExportGroup("Firing")]
	[Export]
	public WeaponMode Mode
	{
		get => _mode;
		set
		{
			if (_mode == value) return;
			_mode = value;
			EmitSignalModeChanged(value);
		}
	}

	/// <summary>In Auto mode, fire whenever on target. Off: call FireWeapons() yourself.</summary>
	[Export] public bool AutoFire { get; set; } = true;

	/// <summary>When off, FireWeapons() fires even with nothing to aim at (fixed, forward-firing guns).</summary>
	[Export] public bool RequireTarget { get; set; } = true;

	/// <summary>The muzzles shots come from, used round-robin. Defaults to the weapon itself.</summary>
	[Export] public Node3D[] Emitters { get; set; } = [];

	/// <summary>Seconds between shots, cycled in order. [0.1, 0.1, 0.6] gives three-round bursts.</summary>
	[Export] public float[] FireTimings { get; set; } = [0.2f];

	/// <summary>Random ± fraction applied to each fire timing. 0.1 = each wait is 90%..110% of its FireTimings entry.</summary>
	[Export(PropertyHint.Range, "0,1,0.01")] public float FireTimingJitter { get; set; } = 0f;

	/// <summary>
	/// How fast this weapon's shots travel, in units per second. Aimers use it to lead moving
	/// targets (see Ballistics.LeadPoint). 0 means hitscan: aim straight at the target.
	/// </summary>
	[Export] public float ProjectileSpeed { get; set; } = 0.0f;

	[ExportGroup("Ammo")]
	[Export] public double ReloadTime { get; set; } = 2.0f;

	[Export] public int MagazineSize { get; set; } = 10;

	/// <summary>How long the weapon can sit idle in Ready before it's considered Released.</summary>
	[Export] public double ReleaseTimer { get; set; } = 0.5f;

	/// <summary>What the weapon aims at in Auto mode.</summary>
	public Node3D? Target { get; set; }

	/// <summary>What the weapon aims at in Manual mode.</summary>
	public Vector3? ManualAimPoint { get; set; }

	/// <summary>Manual weapons only fire while this is true.</summary>
	public bool TriggerHeld { get; set; }

	private int _fireTimingIndex;
	private double _fireWait;

	public enum WeaponState
	{
		Ready,
		Firing,
		Reloading,
		Released,
		Disabled,
	}

	/// <summary>Current state. UI can poll this as well as listen to the signals.</summary>
	public WeaponState State { get; private set; } = WeaponState.Ready;

	public double Timer { get; protected set; }
	public int Ammo { get; protected set; }

	/// <summary>Ammo as a 0..1 fraction of MagazineSize, for pies and bars.</summary>
	public float AmmoFraction => MagazineSize > 0 ? (float)Ammo / MagazineSize : 0.0f;

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
		WeaponState.Firing => _fireWait > 0 ? Timer / _fireWait : 0.0,
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
	public delegate void DisabledEventHandler();

	[Signal]
	public delegate void StateChangedEventHandler(WeaponState state);

	/// <summary>Emitted whenever Mode changes (CycleMode or anything else setting it).</summary>
	[Signal]
	public delegate void ModeChangedEventHandler(WeaponMode mode);


	private int _emitterIndex;

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint()) return;

		Spawner ??= this.FindAncestorOfType<Actor>();
		if (Emitters.Length == 0) Emitters = [this];

		Ammo = MagazineSize;
		EnterState(Mode == WeaponMode.Off ? WeaponState.Disabled : WeaponState.Ready);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (Engine.IsEditorHint()) return;

		if (Mode == WeaponMode.Off) return;
		if (Mode == WeaponMode.Manual ? TriggerHeld : AutoFire)
			FireWeapons();
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if (Engine.IsEditorHint()) return;

		TickTimer(delta);
	}


	/// <summary>Auto → Manual → Off → Auto.</summary>
	public void CycleMode()
	{
		SetMode(Mode switch
		{
			WeaponMode.Auto => WeaponMode.Manual,
			WeaponMode.Manual => WeaponMode.Off,
			_ => WeaponMode.Auto,
		});
	}

	/// <summary>
	/// Switches mode and updates State to match: switching Off disables the weapon; switching
	/// back on leaves it idle, or reloading if it's empty (a reload that was in progress when it
	/// was switched off starts over). Clears ManualAimPoint and TriggerHeld.
	/// </summary>
	public void SetMode(WeaponMode mode)
	{
		Mode = mode;
		ManualAimPoint = null;
		TriggerHeld = false;

		if (Mode == WeaponMode.Off) EnterState(WeaponState.Disabled);
		else if (State == WeaponState.Disabled) EnterState(Ammo > 0 ? WeaponState.Released : WeaponState.Reloading);
	}

	/// <summary>Whether the weapon can turn to face a world point. Turret limits this by its yaw range.</summary>
	public virtual bool CanFace(Vector3 point) => true;

	/// <summary>The world point the weapon is aiming at, or null if it has nothing to aim at.</summary>
	public Vector3? GetAimPoint()
	{
		if (Mode == WeaponMode.Manual) return ManualAimPoint;
		return IsInstanceValid(Target) ? Target!.GlobalPosition : null;
	}


	/// <summary>
	/// Starts a shot if the weapon is idle (Ready/Released), has ammo and is on target (or
	/// RequireTarget is off). Safe to call every frame, e.g. while a fire button is held.
	/// </summary>
	public void FireWeapons()
	{
		if (Mode == WeaponMode.Off) return;
		if (RequireTarget && !IsOnTarget()) return;
		if (!CanFire()) return;

		FireNextShot();
	}

	private void FireNextShot()
	{
		var emitter = GetEmitter();
		SpawnPayload(emitter, emitter.GlobalTransform);

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


	public virtual bool IsOnTarget() => GetAimPoint() != null;


	/// <summary>Direction to the aim point, or straight ahead if there is nothing to aim at.</summary>
	public virtual Vector3 GetAimDirection() =>
		GetAimPoint() is { } point ? GlobalPosition.DirectionTo(point) : -GlobalBasis.Z;


	protected Node3D GetEmitter()
	{
		var emitter = Emitters[_emitterIndex % Emitters.Length];
		_emitterIndex = (_emitterIndex + 1) % Emitters.Length;
		return emitter;
	}

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
				_fireWait = PopFireTiming() * (1.0 + GD.RandRange(-FireTimingJitter, FireTimingJitter));
				Timer = _fireWait;
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
			case WeaponState.Disabled:
				Timer = double.PositiveInfinity;
				EmitSignalDisabled();
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
		EnterState(Mode == WeaponMode.Off ? WeaponState.Disabled : WeaponState.Ready);
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
