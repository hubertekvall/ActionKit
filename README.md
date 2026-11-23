# ActionKit

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

An actor/component framework for **Godot 4 (.NET / C#)**, extracted from Miasmacorps and made
game-agnostic. Drop it into any C# Godot project: actors and components, messages, health and
damage, spawning, weapons, behavior trees, a state machine, and a set of effects and UI helpers.

Requires Godot 4.7+ with .NET (C# 12, .NET 8).

## Install

1. Copy `addons/actionkit/` into your project's `addons/` folder.
2. Build the C# project (the hammer button, or `dotnet build`).
3. Optional: enable **ActionKit** under *Project Settings → Plugins*. Nothing depends on it; every
   node and resource is a `[GlobalClass]` and shows up in the *Create Node* dialog either way.

No autoloads are needed. `AudioEmitterManager` and `ParticleEmitterManager` create themselves on
first use (you can still register them as autoloads if you prefer).

All code lives in the `ActionKit.*` namespaces and every file sets `#nullable enable`, so it builds
warning-free whether or not your project enables nullable reference types.

## Core idea

```
Ship (Actor)                     ← thin coordinator
├─ Body (ActorRigidBody3D)       ← physics; the Actor follows it every frame
│  └─ CollisionShape3D
├─ Model (Node3D)                ← assign as Actor.ModelPivot for tilt/roll effects
├─ HealthComponent               ← components: direct children of the Actor
├─ FlightMovement
├─ HoverComponent
├─ InputHeadingController
└─ Cannon (Weapon)               ← weapons are Actors too, nested anywhere
   └─ SpawnEffectsComponent      ← spawns the projectile scene when the weapon fires
```

- **`Actor`** (`Node3D`) owns an optional `Body` (`ActorRigidBody3D`, `ActorCharacterBody3D` or
  `ActorAreaBody3D`; inspector buttons add one) and ticks its `ActorComponent` children. On
  `_Ready` it binds every component first and then calls `OnActorReady` on each, so components can
  always find their siblings with `ActorOwner.GetComponent<T>()` regardless of tree order.
- **`ActorComponent`** (`Node`) overrides `OnActorReady`, `PhysicsTick`, `ProcessTick`,
  `OnMessage` (plus `PreMessage`/`PostMessage`) and `OnActorDetached`. Set `Enabled = false` to
  silence one.
- **Messages.** `actor.Broadcast(message)` delivers an `ActorMessage` to every enabled component.
  Built in: `CollisionMessage`/`ActorCollisionMessage` (sent every frame by the body for each
  contact), `DamageMessage` (negative heals), `DeathMessage`, `SpawnEffectMessage`,
  `TargetAssignedMessage`. Subclass `ActorMessage` for your own events.
- **Death.** `actor.Kill()` emits `IsKilled`, raises the static `Actor.AnyKilled` event (for score
  counters, kill feeds), broadcasts a `DeathMessage` and frees the actor.
- **Steering.** Movement components implement `ISteerable` (`SetHeading`, `SetLookDirection`).
  Input controllers and behavior-tree movement nodes only talk to `ISteerable`
  (`actor.GetSteerable()`), so you can swap in your own movement model.

## Modules

| Folder | Namespace | Contents |
|---|---|---|
| `Core/`, `Spawning/` | `ActionKit` | `Actor`, `ActorComponent`, bodies, `CollisionContact`, messages, `ISteerable`, `ActorFactory`, `ActorSpawner`, `EffectSpawn` |
| `Components/Combat` | `ActionKit.Components` | `HealthComponent`, `CollisionDamage`, `VelocityDamage` (ramming), `HealthThresholdSpawnTrigger`, `OnDepletedDisableComponents` |
| `Components/Lifetime` | | `LifetimeComponent`, `RandomLifetimeComponent`, `DisableSceneComponents` |
| `Components/Movement` | | `FlightMovement` (`ISteerable`), `InputHeadingController`, `MouseHeadingController`, `KeyboardSteering` (tank-style) |
| `Components/Physics` | | `HoverComponent`, `HomingComponent`, `SpringBounceComponent`, `BlastSphere`, `ForwardImpulse`, `SphereImpulse`, `SpawnerImpulse`, `NoCollisionWithSpawner`, `OnCollisionKill` |
| `Components/Spawning` | | `SpawnComponent` base and `SpawnEffectsComponent`, `OnReadySpawn`, `OnKillSpawn`, `OnDamageSpawn`, `OnCollisionSpawn`, `OnCollisionAttachSpawn`, `OnScreenSpawn`, timed/toggled random-area spawners, `OnDamageAddComponents`, `OnDeathAddComponents` |
| `Components/Targeting` | | `TargetFinder` with pluggable `TargetSelector` resources (`DistanceSelector`) |
| `Components/Visual` | | `HurtMesh`, `ShieldMesh`, `DetachedEffects`, `DamageTextSpawner`, `CameraShakeOnReady`, `HealthDamageShaderParameter`, velocity-driven fade/offset/scale/shader parameter/particle amount |
| `Components/Destruction` | | `MeshSlicer` / `RigidMeshSlicer` (Voronoi fracture, bakeable in the editor), `MeshFracturer` |
| `Weapons/` | `ActionKit.Weapons` | `Weapon` (Auto/Manual/Off modes), `Turret` (yaw limits), `HardpointWeapon`, `BeamWeapon` + `BeamEffect` + `BeamMesh`, `WeaponPlatform`, `AutoTargeting`, `MouseAim`, `WeaponHotkeys`, `WeaponModeMarker` |
| `AI/BehaviorTree` | `ActionKit.AI` | `BehaviorTree`, `AmortizedBehaviorTree`, typed blackboard, composites, decorators, targeting/movement/weapon actions and conditions (all `BT*`) |
| `StateMachine/` | `ActionKit.StateMachines` | `StateMachine`, `State` |
| `Audio/` | `ActionKit.Audio` | `AudioStreamOrigin3D` (sounds that outlive their node), `RampedAudioStream`, `VelocityEngineSound` |
| `Effects/` | `ActionKit.Effects` | `CameraShake`, `TrailMesh3D`, `ParticleOrigin3D`, one-shot particles, `BlendedTexture`, `DepthCopyEffect` |
| `UI/` | `ActionKit.UI` | `PieControl`, `DeltaPieControl`, `DeltaProgressbar`, `SmoothStack`, `DamageText`, `PropertyReader` (inspector data binding), `CursorSwitcher`, `WeaponMarker` |
| `Utilities/` | `ActionKit.Utilities` | `QueryList`, node/vector/float extensions, `NodeTweener`, `SpringDamper`, `PoissonDisk`, `CopyPosition`, `FollowNode3DOnScreen`, `ShowRenderLayers`, `Ballistics.LeadPoint` |

## Recipes

**Projectile.** A scene with an `Actor` root, an `ActorRigidBody3D` body (gravity 0, *Contact
Monitor* on, *Max Contacts Reported* > 0), plus `ForwardImpulse`, `CollisionDamage`,
`OnCollisionKill`, `LifetimeComponent` and `NoCollisionWithSpawner`. Add `HomingComponent` to make
it a missile; it picks up the weapon's target automatically.

**Weapon.** Add a `Weapon` under your ship, set `Emitters` and `FireTimings`, and give it a
`SpawnEffectsComponent` with the projectile scene (its `RandomOffset` is the spread). Add an
`AutoTargeting` child (`TargetGroup`, `ScanRadius`) and with `AutoFire` on it fights on its own.
`Mode` switches between `Auto` (aims at `Target`), `Manual` (aims at `ManualAimPoint`, fires while
`TriggerHeld`) and `Off`; `CycleMode()` steps through them. For player aiming put `MouseAim` on the
ship (it aims every Manual weapon at the mouse, snapping to and leading targets using each
weapon's `ProjectileSpeed`) and `WeaponHotkeys` to cycle modes with the number keys set on each
`WeaponPlatform.Hotkey`. A fixed forward gun can instead set `RequireTarget = false` and call
`FireWeapons()` itself. `Fired`, `Reloading`, `ReadyState`, `Released`, `Disabled`,
`StateChanged` and `ModeChanged` signals plus `Ammo`, `AmmoFraction` and `Progress` drive UI.

**Enemy AI.** Add a `BehaviorTree` (or `AmortizedBehaviorTree` for crowds) under the actor with a
composite as its first child, e.g.

```
BTSelector
├─ BTSequence                        ← flee when hurt
│  ├─ BTIsHealthBelowThreshold
│  └─ BTFleeTarget
└─ BTSequence                        ← otherwise engage
   ├─ BTFindTargetsWithinRadius
   ├─ BTFilterTargetsByGroup (player)
   ├─ BTClosestTarget
   ├─ BTAimWeaponsAtTarget
   └─ BTOrbitTarget
```

Movement nodes steer through the actor's `ISteerable` (e.g. `FlightMovement`).

**Templates.** `ActorComponent`s placed as children of a spawn component (or an `ActorSpawner`, or a
`MeshSlicer`) aren't active on the owner; they are copied onto every actor it spawns.

## Not included

Game-specific parts of Miasmacorps stayed in the game: the radar boxes, the weapon-card HUD, the
monster wave manager, encampment spawning, the monster/player states, the combat-mode overlays
and aim guide, the room/pause/armory game shell, and the Phantom Camera zoom. The camera shake no
longer needs Phantom Camera.

## Coming from Miasmacorps

Mostly the same API under new namespaces. The renames:

| Miasmacorps | ActionKit |
|---|---|
| `FlightActor` (Actor subclass) | `FlightMovement` component + `HoverComponent` |
| `Damage` | `CollisionDamage` |
| `Impulse` | `ForwardImpulse` |
| `NoCollisionWithSpawn` | `NoCollisionWithSpawner` |
| `DisableFlightActor` | `DisableSceneComponents` |
| `OnDepletedDisableHover` | `OnDepletedDisableComponents` |
| `HealthComponentDeathTrigger` | `HealthComponent.FreeOnDepleted = false` |
| `BeamMeshEffect` | `BeamEffect` |
| `TraumaCamera` / `TraumaCameraShake` | `CameraShake` / `CameraShakeOnReady` |
| `DamageTextUI` | `DamageText` |
| `ToggledRandomSpawnEffectsComponent.Enabled` | `.IsActive` (`Enabled` is now on every component) |
| Behavior tree nodes (`Sequence`, `Wait`, `ClosestTarget`, ...) | `BT`-prefixed (`BTSequence`, `BTWait`, `BTClosestTarget`, ...) |
| `MovementKeys.*` | `BTKeys.TargetPosition` / `BTKeys.TargetNode` |
| `RandomPlayerPositionOffset` | `BTRandomPointAroundGroup` |
| `OneShotParticleGPU` / `OneShotParticle` | `OneShotGpuParticles3D` / `OneShotCpuParticles3D` |
| `NodeCreation` / `VectorUtilities` | `NodeExtensions` / `VectorExtensions` |
| `Weapon.ScanRadius` / `TargetGroup` / `Sweep*` | same properties on an `AutoTargeting` child |
| `Weapon.EmissionSpread` | `SpawnComponent.RandomOffset` |
| `CombatMode` | `MouseAim` (input action names and active-only scenes are exports) |
| `GlobalEventBus.ActorKilled` | `Actor.AnyKilled` |

Hard-coded groups (`"Monster"`, `"Player"`, `"ActionCamera"`) are gone: weapons, homing and
targeting take a group export, and camera users take a `Camera3D` export that falls back to the
viewport's active camera.

## Developing

This repository is also a Godot project (`project.godot`, `ActionKit.csproj`) so the addon can be
opened and built on its own. Only `addons/actionkit/` is meant to be copied into games.

## License

ActionKit is released under the [MIT License](LICENSE). A copy also ships inside
`addons/actionkit/LICENSE`, so it travels with the addon when you copy it into a game.
