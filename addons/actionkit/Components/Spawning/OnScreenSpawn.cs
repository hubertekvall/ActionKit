#nullable enable
using Godot;

namespace ActionKit.Components;

public enum ScreenEvent
{
	Enter,
	Exit,
}

/// <summary>
/// Spawns Scenes when the actor enters or exits the screen, picked by Trigger.
/// Adds a VisibleOnScreenNotifier3D to the actor: a cube of Size centered on the actor.
/// </summary>
[GlobalClass]
public partial class OnScreenSpawn : SpawnComponent
{
	[Export] public ScreenEvent Trigger { get; set; } = ScreenEvent.Enter;
	[Export] public float Size { get; set; } = 2.0f;
	[Export] public bool Once { get; set; } = false;

	private VisibleOnScreenNotifier3D _notifier = null!;

	public override void OnActorReady()
	{
		_notifier = new VisibleOnScreenNotifier3D { Aabb = new Aabb(Vector3.One * -Size / 2, Vector3.One * Size) };
		if (Trigger == ScreenEvent.Enter) _notifier.ScreenEntered += OnScreenEvent;
		else _notifier.ScreenExited += OnScreenEvent;

		// Deferred: the actor is still setting up its children here.
		Callable.From(() => ActorOwner.AddChild(_notifier)).CallDeferred();
	}

	private void OnScreenEvent()
	{
		// Leaving the tree (death, despawn) can count as exiting the screen; skip that.
		if (!IsInstanceValid(ActorOwner) || ActorOwner.IsQueuedForDeletion() || !ActorOwner.IsInsideTree()) return;
		Spawn(ActorOwner.GlobalTransform, ActorOwner);
		if (Once) _notifier.QueueFree();
	}
}
