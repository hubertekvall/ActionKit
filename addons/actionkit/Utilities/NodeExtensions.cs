#nullable enable
using System.Collections.Generic;
using Godot;


namespace ActionKit.Utilities;

public static class NodeExtensions
{
	public static T? FindAncestorOfType<T>(this Node node) where T : class
	{
		var current = node.GetParent();
		while (current != null)
		{
			if (current is T match) return match;
			current = current.GetParent();
		}
		return null;
	}

	/// <summary>
	/// Recursively collects every descendant of <paramref name="node"/> that is of type
	/// <typeparamref name="T"/>, at any depth. The mirror image of FindAncestorOfType —
	/// use it for "find every X anywhere under this node" (e.g. every Weapon nested under
	/// an actor, regardless of how many mount/pivot nodes sit in between) instead of
	/// requiring a fixed, manually-wired reference to each one.
	/// </summary>
	public static List<T> FindDescendantsOfType<T>(this Node node) where T : class
	{
		var results = new List<T>();

		foreach (var child in node.GetChildren())
		{
			if (child is T match) results.Add(match);
			results.AddRange(child.FindDescendantsOfType<T>());
		}

		return results;
	}


	/// <summary>
	/// Ensures <paramref name="node"/> exists as a child of <paramref name="parent"/>.
	/// If <paramref name="node"/> is null, creates a new instance of <typeparamref name="T"/>,
	/// adds it to <paramref name="parent"/>, and sets its owner so it is saved with the scene.
	/// No-ops outside the editor, and no-ops if the node is already present.
	/// </summary>
	/// <param name="parent">The node that will own the new child.</param>
	/// <param name="node">Ref to the field holding the node — populated if null.</param>
	/// <param name="name">Optional name for the created node.</param>
	/// <typeparam name="T">Any Godot Node type.</typeparam>
	/// <returns>True if a new node was created, false if it already existed.</returns>
	public static bool EnsureChild<T>(this Node parent, ref T? node, string? name = null)
		where T : Node, new()
	{
		if (!Engine.IsEditorHint())
			return false;


		if (node is not null) return true;

		node = new T();

		if (name is not null)
			node.Name = name;

		parent.AddChild(node, true);

		// SetOwner makes the node part of the saved scene tree.
		// Walk up to the true scene root so nested nodes are handled correctly.
		node.Owner = parent.GetSceneRoot();

		return true;
	}

	public static T CreateAndAddChild<T>(this Node parent, string? name = null) where T : Node, new()
	{
		var node = new T();

		if (name is not null) node.Name = name;
		parent.AddChild(node, true);

		node.Owner = parent.GetSceneRoot();

		return node;
	}

	/// <summary>
	/// Returns the topmost node in the scene tree (the scene root),
	/// which is the correct owner for newly created editor nodes.
	/// </summary>
	private static Node GetSceneRoot(this Node node)
	{
		var root = node;
		while (root.Owner is not null)
			root = root.Owner;
		return root;
	}
}
