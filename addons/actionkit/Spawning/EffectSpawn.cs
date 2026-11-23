#nullable enable
using Godot;

namespace ActionKit;

/// <summary>
/// One effect to spawn through ActorFactory, set up in the inspector as a resource (so a
/// component can export a few of them: an on-zoom whoosh, a boost flash, a pickup sparkle).
/// Spawn() puts it at origin, moved by Offset in origin's local space. Parent = Source
/// attaches it to the source actor so it moves along with it; Parent = Scene leaves it in the
/// world where it spawned. The spawned scene should free itself when done.
/// </summary>
[GlobalClass]
public partial class EffectSpawn : Resource
{
	[Export] public PackedScene? Scene { get; set; }
	[Export] public SpawnParent Parent { get; set; } = SpawnParent.Scene;
	[Export] public Vector3 Offset { get; set; }

	public Actor? Spawn(Node3D origin, Actor? source)
	{
		var transform = origin.GlobalTransform.TranslatedLocal(Offset);
		return ActorFactory.Spawn(Scene, new SpawnParams(transform, source, Parent));
	}
}
