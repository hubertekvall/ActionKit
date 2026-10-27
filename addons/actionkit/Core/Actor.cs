#nullable enable
using System.Collections.Generic;
using ActionKit.Utilities;
using ActionKit.Weapons;
using Godot;

namespace ActionKit;

/// <summary>
/// The root of a game entity. An Actor is a thin coordinator: it owns an optional physics
/// <see cref="Body"/>, ticks its <see cref="ActorComponent"/> children and routes
/// <see cref="ActorMessage"/>s to them. Behaviour lives in components.
///
/// Lifecycle: on _Ready every direct ActorComponent child is bound first, then each gets
/// OnActorReady, so components can always find their siblings regardless of tree order.
/// Components added later (AddComponent, templates, ...) become active at the start of the
/// next tick.
/// </summary>
[Tool]
[GlobalClass]
public partial class Actor : Node3D
{
	[ExportToolButton("Add CharacterBody")]
	public Callable CharacterBodyButton => Callable.From(AddCharacterBody3D);

	[ExportToolButton("Add RigidBody")] public Callable RigidBodyButton => Callable.From(AddRigidBody3D);

	[ExportToolButton("Add AreaBody")] public Callable AreaBodyButton => Callable.From(AddAreaBody3D);

	/// <summary>The physics body driving this actor. Must implement IActorBody3D.</summary>
	[Export] public Node3D? Body { get; set; }

	/// <summary>Visual root that components may tilt, roll or point (separate from the body).</summary>
	[Export] public Node3D? ModelPivot { get; set; }

	/// <summary>The actor that spawned this one (the ship that fired a missile, ...).</summary>
	[Export] public Actor? Spawner { get; set; }

	public IActorBody3D? ActorBody { get; private set; }

	private readonly List<ActorComponent> _components = [];
	private readonly List<ActorComponent> _pending = [];

	/// <summary>The last few broadcast messages, newest on top.</summary>
	public readonly Stack<ActorMessage> MessageBuffer = new();

	/// <summary>Every Weapon anywhere under this actor, gathered on ready.</summary>
	public List<Weapon> Weapons { get; private set; } = [];

	public bool IsActorReady { get; private set; }

	[Signal]
	public delegate void IsKilledEventHandler();

	[Signal]
	public delegate void IsCollidingEventHandler();

	[Signal]
	public delegate void IsDamagedEventHandler();


	// ---------------------------------------------------------------- Lifecycle

	public override void _Ready()
	{
		if (Engine.IsEditorHint()) return;

		// Phase 1 — gather. Bind only; no component logic runs yet.
		InitializeBody();
		Weapons = this.FindDescendantsOfType<Weapon>();
		foreach (var child in GetChildren())
		{
			if (child is ActorComponent component && component.Bind(this))
				_components.Add(component);
		}

		// Any component added from here on (including from another component's OnActorReady)
		// goes through _pending and is readied at the next tick.
		ChildEnteredTree += OnChildEnteredTree;
		ChildExitingTree += OnChildExitingTree;

		// Phase 2 — ready. Every sibling, the body and the weapons exist now, whatever the
		// tree order. _components can't change during this loop: adds are deferred.
		foreach (var component in _components)
			component.OnActorReady();

		IsActorReady = true;
		OnActorReady();
	}

	/// <summary>
	/// Actor-level hook, run synchronously after all components are ready. Defer inside the
	/// override if something truly needs to wait a frame.
	/// </summary>
	protected virtual void OnActorReady()
	{
	}


	private void InitializeBody()
	{
		if (Body is not IActorBody3D actorBody) return;
		ActorBody = actorBody;
		ActorBody.ActorOwner = this;
	}

	private void OnChildEnteredTree(Node node)
	{
		if (node is ActorComponent component && component.Bind(this))
			_pending.Add(component);
	}

	private void OnChildExitingTree(Node node)
	{
		if (node is not ActorComponent component) return;

		if (_pending.Remove(component))
		{
			component.Unbind(); // never became active, so nothing to detach
		}
		else if (_components.Remove(component))
		{
			component.OnActorDetached();
			component.Unbind();
		}
	}


	/// <summary>First active component of type T (class or interface), or null.</summary>
	public T? GetComponent<T>() where T : class
	{
		foreach (var component in _components)
			if (component is T match)
				return match;
		return null;
	}

	/// <summary>Every active component of type T (class or interface).</summary>
	public IEnumerable<T> GetComponents<T>() where T : class
	{
		foreach (var component in _components)
			if (component is T match)
				yield return match;
	}

	public void AddComponent(ActorComponent component) => AddChild(component, forceReadableName: true);

	// Runs at the start of each tick, outside any component loop. Newly added components
	// become active (and get OnActorReady) here, never mid-iteration.
	private void FlushPendingComponents()
	{
		if (_pending.Count == 0) return;

		var added = _pending.ToArray();
		_pending.Clear();
		_components.AddRange(added);

		foreach (var component in added)
			component.OnActorReady();
	}


	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint()) return;
		FlushPendingComponents();

		foreach (var component in _components)
		{
			if (component.Enabled)
				component.PhysicsTick(delta);
		}
	}


	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint()) return;
		FlushPendingComponents();

		foreach (var component in _components)
		{
			if (component.Enabled)
				component.ProcessTick(delta);
		}

		if (Body == null) return;

		// The body simulates in world space; the actor follows it so that everything else
		// parented to the actor (models, weapons, effects) moves with it.
		Body.TopLevel = true;
		GlobalTransform = Body.GlobalTransform;
	}

	/// <summary>Delivers a message to every enabled component (PreMessage, OnMessage, PostMessage).</summary>
	public virtual void Broadcast(ActorMessage message)
	{
		while (MessageBuffer.Count > 2) MessageBuffer.Pop();
		MessageBuffer.Push(message);

		switch (message)
		{
			case DamageMessage:
				EmitSignalIsDamaged();
				break;

			case CollisionMessage:
				EmitSignalIsColliding();
				break;
		}

		foreach (var component in _components)
		{
			if (!component.Enabled) continue;
			component.PreMessage(message);
			component.OnMessage(message);
			component.PostMessage(message);
		}
	}

	// ---------------------------------------------------------------- Death

	/// <summary>Emits IsKilled, broadcasts the DeathMessage, then frees the actor unless told not to.</summary>
	public virtual void Kill(DeathMessage deathMessage, bool queueFree = true)
	{
		if (IsQueuedForDeletion()) return;
		EmitSignalIsKilled();
		Broadcast(deathMessage);
		if (queueFree) QueueFree();
	}

	public virtual void Kill(bool queueFree = true)
	{
		Kill(new DeathMessage { Cause = DeathMessage.DeathCause.Call }, queueFree);
	}

	public virtual void Kill()
	{
		Kill(true);
	}


	// ---------------------------------------------------------------- Editor tooling

	public void AddCharacterBody3D() => AddBody(new ActorCharacterBody3D());

	public void AddRigidBody3D() => AddBody(new ActorRigidBody3D());

	public void AddAreaBody3D() => AddBody(new ActorAreaBody3D());

	private void AddBody<T>(T body) where T : Node3D, IActorBody3D
	{
		if (!Engine.IsEditorHint()) return;

		AddChild(body, forceReadableName: true);
		body.Owner = Owner ?? this;

		Body = body;
		ActorBody = body;
	}
}
