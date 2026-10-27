#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ActionKit;

/// <summary>
/// Spawns actors at its own transform. Child ActorComponents are templates stamped onto every
/// spawned actor. Subclass and override GetEmitter/GetPositionOffset for spawn patterns.
/// </summary>
[GlobalClass]
public partial class ActorSpawner : Node3D
{
	[Export] public PackedScene[] Scenes { get; set; } = [];

	public List<Actor> SpawnAllScenes(Actor? spawner = null) =>
		Scenes.Select(scene => SpawnScene(scene, spawner)).OfType<Actor>().ToList();

	public Actor? SpawnScene(PackedScene scene, Actor? spawner = null)
	{
		var emitter = GetEmitter();
		var transform = new Transform3D(emitter.GlobalBasis, emitter.GlobalPosition + GetPositionOffset());

		return ActorFactory.Spawn(scene, new SpawnParams(transform, spawner), GetTemplates());
	}

	protected virtual Node3D GetEmitter() => this;
	protected virtual Vector3 GetPositionOffset() => Vector3.Zero;

	private IEnumerable<ActorComponent> GetTemplates() => GetChildren().OfType<ActorComponent>();
}
