#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ActionKit.AI;

public enum BTStatus
{
	Success,
	Failure,
	Running,
}

/// <summary>
/// Base for every behavior tree node. Nodes are regular scene nodes nested under a BTree;
/// override Tick() and read/write the tree's blackboard through BTree.
/// </summary>
public abstract partial class BTNode : Node
{
	private List<BTNode> _children = [];
	protected List<BTNode> GetNodes() => _children;

	/// <summary>The actor this tree controls.</summary>
	protected Actor OwnerActor { get; set; } = null!;

	protected BTree BTree { get; set; } = null!;
	protected bool IsVisited { get; set; }
	protected BTStatus LastStatus { get; set; }

	/// <summary>The actor's ISteerable (movement), looked up once. Null if it has none.</summary>
	protected ISteerable? Steerable { get; private set; }

	public void Initialize(BTree tree)
	{
		BTree = tree;
		OwnerActor = BTree.OwnerActor!;
	}

	/// <summary>Called on the first tick, once the actor's components are ready.</summary>
	public void ResolveActorReferences()
	{
		Steerable = OwnerActor.GetSteerable();
	}

	public void OnPostTick()
	{
		if (!IsVisited && LastStatus == BTStatus.Running)
		{
			OnAbort();
			LastStatus = BTStatus.Failure;
		}

		IsVisited = false;
	}

	/// <summary>Called when the node was Running last tick but wasn't reached this tick.</summary>
	public virtual void OnAbort() { }

	public override void _Ready()
	{
		_children = GetChildren().OfType<BTNode>().ToList();
	}

	protected abstract BTStatus Tick();

	public BTStatus Evaluate()
	{
		IsVisited = true;
		LastStatus = Tick();
		return LastStatus;
	}
}
