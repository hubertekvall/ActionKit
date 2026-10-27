#nullable enable
using Godot;

namespace ActionKit;

/// <summary>Where a component came from: authored in the actor's scene, or added at runtime.</summary>
public enum ComponentOrigin
{
	Scene,
	Dynamic,
}

/// <summary>
/// Base class for behaviour that hangs off an <see cref="Actor"/>. Place components as
/// direct children of the actor. They don't use _Process/_PhysicsProcess; the actor drives
/// them through the hooks below so that ordering and enabling stay in one place.
/// </summary>
public abstract partial class ActorComponent : Node
{
	protected Actor ActorOwner { get; private set; } = null!;
	public ComponentOrigin Origin { get; private set; }

	/// <summary>Disabled components get no ticks and no messages.</summary>
	[Export] public bool Enabled { get; set; } = true;

	public bool Bind(Actor actorOwner)
	{
		if (ActorOwner == actorOwner) return false;
		ActorOwner = actorOwner;
		Origin = Owner == actorOwner ? ComponentOrigin.Scene : ComponentOrigin.Dynamic;
		return true;
	}

	public void Unbind() => ActorOwner = null!;


	// --- Overridable hooks ---

	/// <summary>Called once all sibling components are bound. Safe to call GetComponent here.</summary>
	public virtual void OnActorReady() { }

	/// <summary>Called when the component is removed from its actor.</summary>
	public virtual void OnActorDetached() { }

	public virtual void ProcessTick(double delta) { }

	public virtual void PhysicsTick(double delta) { }

	public virtual void PreMessage(ActorMessage message) { }

	public virtual void OnMessage(ActorMessage message) { }

	public virtual void PostMessage(ActorMessage message) { }
}
