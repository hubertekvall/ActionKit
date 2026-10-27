#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ActionKit.Utilities;

/// <summary>
/// A list of Node3Ds from a physics query, with chainable filters. Colliders that belong to
/// an Actor are reported as that Actor, once, so filters and distances work on actors.
/// </summary>
public class QueryList : List<Node3D>
{
	public QueryList()
	{
	}

	public QueryList(IEnumerable<Node3D> items) : base(items)
	{
	}

	/// <summary>Everything whose collider overlaps a sphere of radius around origin.</summary>
	public static QueryList QueryRadius(Node3D? originNode3D, float radius, uint collisionMask = uint.MaxValue)
	{
		if (originNode3D is null || !originNode3D.IsInsideTree()) return new QueryList();

		var shapeQuery = new PhysicsShapeQueryParameters3D
		{
			Transform = new Transform3D(Basis.Identity, originNode3D.GlobalPosition),
			Shape = new SphereShape3D { Radius = radius },
			CollisionMask = collisionMask,
		};

		if (originNode3D is CollisionObject3D collisionObject)
			shapeQuery.Exclude = [collisionObject.GetRid()];

		var spaceState = originNode3D.GetWorld3D().DirectSpaceState;

		var collisions = spaceState.IntersectShape(shapeQuery, 64)
			.Select(dict => dict["collider"].As<Node3D>())
			.Where(collider => collider != null)
			.Select(collider => collider.GetParent() is Actor actor ? actor : collider)
			.Distinct();

		return new QueryList(collisions);
	}

	public QueryList OfGroup(StringName groupName) =>
		this.Where(node => node.IsInGroup(groupName)).ToQueryList();

	public QueryList OfGroups(StringName[] groupNames) =>
		this.Where(node => groupNames.Any(node.IsInGroup)).ToQueryList();

	/// <summary>Only entries that are Actors.</summary>
	public QueryList OfActors() => this.Where(node => node is Actor).ToQueryList();

	/// <summary>Drops the given nodes (e.g. yourself and whoever spawned you).</summary>
	public QueryList Excluding(params Node?[] nodes) =>
		this.Where(node => !nodes.Contains(node)).ToQueryList();

	public QueryList OrderByClosest(Vector3 position) =>
		this.OrderBy(node => node.GlobalPosition.DistanceSquaredTo(position)).ToQueryList();
}

public static class QueryListExtensions
{
	public static QueryList ToQueryList(this IEnumerable<Node3D> source) => new(source);
}
