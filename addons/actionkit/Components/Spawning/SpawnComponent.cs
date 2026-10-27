#nullable enable
using System.Collections.Generic;
using ActionKit.Utilities;
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Base for every component that spawns Actor scenes. Holds the what (Scenes), the where
/// (Parent) and the templates, and exposes a single Spawn() call. Deliberately handles no
/// messages — subclasses decide WHEN to spawn and at what transform:
///   SpawnEffectsComponent — on SpawnEffectMessage (weapon fire)
///   OnReadySpawn / OnKillSpawn / OnDamageSpawn / OnCollisionSpawn — on their own events
///   RandomAreaSpawnEffectsComponent — on its own timer
///
/// Any ActorComponents placed as children of this node (LifetimeComponent, HomingComponent,
/// ...) are templates: duplicated onto every actor this spawns. The owning Actor only scans
/// its direct children, so nesting them here keeps them from registering on the owner.
/// </summary>
public abstract partial class SpawnComponent : ActorComponent
{
	[ExportGroup("Spawning")]
	[Export] public PackedScene[] Scenes { get; set; } = [];
	[Export] public SpawnParent Parent { get; set; } = SpawnParent.Scene;

	private List<ActorComponent>? _templates;
	protected IReadOnlyList<ActorComponent> Templates => _templates ??= CollectTemplates();
	// Uniform scale for everything spawned. Meshes and particles follow the node's scale;
	// light ranges don't, so those are scaled by hand.
	[Export] public float SpawnScale { get; set; } = 1.0f;

	protected List<Actor> Spawn(Transform3D transform, Actor? source)
	{
		var spawned = ActorFactory.SpawnAll(Scenes, new SpawnParams(transform.ScaledLocal(Vector3.One * SpawnScale), source, Parent), Templates);
		if (SpawnScale != 1.0f)
			foreach (var actor in spawned) ScaleLights(actor);
		return spawned;
	}

	private void ScaleLights(Actor actor)
	{
		foreach (var omni in actor.FindDescendantsOfType<OmniLight3D>()) omni.OmniRange *= SpawnScale;
		foreach (var spot in actor.FindDescendantsOfType<SpotLight3D>()) spot.SpotRange *= SpawnScale;
	}

	private List<ActorComponent> CollectTemplates()
	{
		var templates = new List<ActorComponent>();
		foreach (var child in GetChildren())
		{
			if (child is ActorComponent component)
				templates.Add(component);
		}
		return templates;
	}
}
