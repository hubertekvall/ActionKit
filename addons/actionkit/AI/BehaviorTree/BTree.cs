#nullable enable
using System;
using System.Collections.Generic;
using ActionKit.Utilities;
using Godot;

namespace ActionKit.AI;

/// <summary>
/// Root of a behavior tree. Place it under an Actor (or set OwnerActor) with a single BTNode
/// child as the root. Ticks on physics frames when CanTick() allows it.
/// </summary>
public abstract partial class BTree : Node
{
	[Export] public Actor? OwnerActor { get; set; }

	private readonly Dictionary<string, object?> _blackBoard = new();
	private readonly List<BTNode> _nodes = [];

	private BTNode? _rootNode;
	private bool _referencesResolved;

	public override void _Ready()
	{
		if (Engine.IsEditorHint()) return;

		OwnerActor ??= this.FindAncestorOfType<Actor>() ?? throw new NullReferenceException($"{Name}: no Actor ancestor found");
		_rootNode = GetChildCount() > 0 ? GetChild(0) as BTNode : null;
		if (_rootNode == null) throw new NullReferenceException($"{Name}: first child must be a BTNode");

		GatherBTNodes(_rootNode);
	}

	private void GatherBTNodes(BTNode node)
	{
		node.Initialize(this);
		_nodes.Add(node);

		foreach (var child in node.GetChildren())
		{
			if (child is BTNode btChild)
				GatherBTNodes(btChild);
		}
	}

	public bool TryGet<T>(BlackboardKey<T> key, out T value) => TryGet(key.Name, out value);

	public bool TryGet<T>(string key, out T value)
	{
		if (_blackBoard.TryGetValue(key, out var v) && v is T t)
		{
			value = t;
			return true;
		}

		value = default!;
		return false;
	}

	public void Set<T>(BlackboardKey<T> key, T value) => _blackBoard[key.Name] = value;
	public void Set(string key, object? value) => _blackBoard[key] = value;
	public void Erase<T>(BlackboardKey<T> key) => _blackBoard.Remove(key.Name);

	public abstract bool CanTick();

	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint()) return;
		Tick();
	}

	public void Tick()
	{
		if (!CanTick() || _rootNode == null || !IsInstanceValid(OwnerActor)) return;

		if (!_referencesResolved)
		{
			foreach (var node in _nodes) node.ResolveActorReferences();
			_referencesResolved = true;
		}

		_rootNode.Evaluate();

		// Anything not reached this tick (a Selector branch that lost priority, a Sequence
		// step skipped after an earlier failure, ...) gets a chance to notice it was
		// pre-empted while Running and clean up.
		PostTick();
	}

	public void PostTick()
	{
		foreach (var node in _nodes)
			node.OnPostTick();
	}
}

/// <summary>A behavior tree that ticks every physics frame.</summary>
[GlobalClass]
public partial class BehaviorTree : BTree
{
	public override bool CanTick() => true;
}
