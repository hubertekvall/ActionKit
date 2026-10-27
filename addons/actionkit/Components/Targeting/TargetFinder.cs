#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Scans for actors within ScanRadius every physics tick and keeps the best one, as scored by
/// the summed Selectors, in CurrentTarget. Optionally limited to actors in TargetGroup.
/// </summary>
[GlobalClass]
public partial class TargetFinder : ActorComponent
{
	[Export] public TargetSelector[] Selectors { get; set; } = [];
	[Export] public float ScanRadius { get; set; } = 10f;
	[Export(PropertyHint.Layers3DPhysics)] public uint CollisionMask { get; set; } = 1;

	/// <summary>When set, only actors in this group are considered.</summary>
	[Export] public StringName TargetGroup { get; set; } = "";

	public Actor? CurrentTarget { get; protected set; }

	private readonly PhysicsShapeQueryParameters3D _queryParams = new();

	public override void OnActorReady()
	{
		_queryParams.Shape = new SphereShape3D { Radius = ScanRadius };
		_queryParams.CollisionMask = CollisionMask;
		_queryParams.CollideWithAreas = false;
		_queryParams.CollideWithBodies = true;

		foreach (var selector in Selectors)
			selector.Actor = ActorOwner;
	}

	public override void PhysicsTick(double delta)
	{
		ScanForTargets();
	}

	public bool TryGetTarget(out Actor? target)
	{
		target = IsInstanceValid(CurrentTarget) ? CurrentTarget : null;
		return target != null;
	}

	private void ScanForTargets()
	{
		CurrentTarget = GetTargets()
			.Select(actor => (actor, score: Selectors.Sum(selector => selector.Evaluate(actor))))
			.OrderByDescending(tuple => tuple.score)
			.FirstOrDefault().actor;
	}

	private IEnumerable<Actor> GetTargets()
	{
		var spaceState = ActorOwner.GetWorld3D().DirectSpaceState;
		_queryParams.Transform = new Transform3D(Basis.Identity, ActorOwner.GlobalPosition);

		var seen = new HashSet<Actor>();
		foreach (var result in spaceState.IntersectShape(_queryParams))
		{
			if (result["collider"].As<Node>()?.GetParent() is not Actor actor) continue;
			if (actor == ActorOwner || actor == ActorOwner.Spawner) continue;
			if (!TargetGroup.IsEmpty && !actor.IsInGroup(TargetGroup)) continue;
			if (seen.Add(actor)) yield return actor;
		}
	}
}
