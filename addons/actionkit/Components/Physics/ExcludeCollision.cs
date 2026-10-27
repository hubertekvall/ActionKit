#nullable enable
using System.Linq;
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Base for components that react to collisions, with a group filter: contacts whose other
/// side is in any of ExcludeGroups are ignored.
/// </summary>
public abstract partial class ExcludeCollision : ActorComponent
{
	[Export] public StringName[] ExcludeGroups { get; set; } = [];

	public override void OnMessage(ActorMessage message)
	{
		if (message is not CollisionMessage collisionMessage) return;

		var other = collisionMessage.CollisionData.OtherOwner;
		if (IsInstanceValid(other) && ExcludeGroups.Any(other.IsInGroup)) return;

		ExecuteEffect(collisionMessage);
	}

	public abstract void ExecuteEffect(CollisionMessage collisionMessage);
}
