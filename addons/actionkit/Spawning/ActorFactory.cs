#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ActionKit;

/// <summary>Where a spawned actor is parented.</summary>
public enum SpawnParent
{
	/// <summary>The current scene (world space, independent of the source).</summary>
	Scene,

	/// <summary>The source actor, so it moves along with it. Falls back to Scene if the source is gone.</summary>
	Source,
}

public readonly record struct SpawnParams(Transform3D Transform, Actor? Source = null, SpawnParent Parent = SpawnParent.Scene);

/// <summary>
/// Instantiates actor scenes. Scenes whose root isn't an Actor are wrapped in one, and any
/// template components are duplicated onto the new actor before it enters the tree, so they
/// are registered in its _Ready like hand-placed components.
/// </summary>
public static class ActorFactory
{
	public static Actor? Spawn(PackedScene? scene, in SpawnParams spawn, IEnumerable<ActorComponent>? templates = null)
	{
		if (scene == null || Engine.IsEditorHint()) return null;

		var parent = ResolveParent(spawn);
		if (parent == null) return null;

		var actor = WrapInActor(scene.Instantiate());

		AttachTemplates(actor, templates);

		if (GodotObject.IsInstanceValid(spawn.Source))
			actor.Spawner = spawn.Source;

		var world = spawn.Transform * actor.Transform;
		actor.Transform = parent is Node3D parent3D ? parent3D.GlobalTransform.AffineInverse() * world : world;

		parent.AddChild(actor);
		return actor;
	}

	/// <summary>Spawns every scene with the same parameters. Null entries are skipped.</summary>
	public static List<Actor> SpawnAll(IEnumerable<PackedScene?>? scenes, in SpawnParams spawn, IEnumerable<ActorComponent>? templates = null)
	{
		var spawned = new List<Actor>();
		if (scenes == null) return spawned;

		// Materialize once — the templates get walked once per scene.
		var templateList = templates as IReadOnlyCollection<ActorComponent> ?? templates?.ToList();

		foreach (var scene in scenes)
		{
			if (Spawn(scene, spawn, templateList) is { } actor)
				spawned.Add(actor);
		}

		return spawned;
	}

	private static Node? ResolveParent(in SpawnParams spawn)
	{
		if (spawn.Parent == SpawnParent.Source
		    && GodotObject.IsInstanceValid(spawn.Source)
		    && !spawn.Source!.IsQueuedForDeletion())
			return spawn.Source;

		var tree = Engine.GetMainLoop() as SceneTree;
		return tree?.CurrentScene ?? tree?.Root;
	}

	private static Actor WrapInActor(Node root)
	{
		if (root is Actor actor) return actor;

		var host = new Actor { Name = $"{root.Name}Host" };
		host.AddChild(root);
		return host;
	}

	private static void AttachTemplates(Actor actor, IEnumerable<ActorComponent>? templates)
	{
		if (templates == null) return;

		foreach (var template in templates)
		{
			if (template?.Duplicate() is not ActorComponent component) continue;
			actor.AddChild(component);
		}
	}
}
