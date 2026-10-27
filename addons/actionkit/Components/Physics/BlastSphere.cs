#nullable enable
using Godot.Collections;
using Godot;

namespace ActionKit.Components;

[GlobalClass]
public partial class BlastSphere : ActorComponent
{
	/// <summary>Actors in this group are not pushed.</summary>
	[Export] public StringName ExcludeGroup { get; set; } = "";
	[Export] public float Radius { get; set; } = 10.0f;
	[Export] public float ImpulseStrength { get; set; } = 500.0f;
	[Export(PropertyHint.Layers3DPhysics)] public uint CollisionMask { get; set; } = 1;

	public override void OnActorReady()
	{
		
		TriggerBlast();
	}

	public void TriggerBlast()
	{
		if (ActorOwner == null) return;
		
		var spaceState = ActorOwner.GetWorld3D().DirectSpaceState;
		var origin = ActorOwner.GlobalPosition;

		var shape = new SphereShape3D { Radius = Radius };

		var query = new PhysicsShapeQueryParameters3D
		{
			Shape = shape,
			Transform = new Transform3D(Basis.Identity, origin),
			CollisionMask = CollisionMask
		};

		if (ActorOwner.ActorBody != null) query.Exclude = [ActorOwner.ActorBody.GetRid()];

		var results = spaceState.IntersectShape(query);
	
		foreach (var result in results)
		{
			var collider = result["collider"].As<Node>();
			
			if (collider?.GetParent() is Actor actor)
			{
				if (actor.ActorBody == null) continue;
				if (!ExcludeGroup.IsEmpty && actor.IsInGroup(ExcludeGroup)) continue;
				var direction = actor.ActorBody.GlobalPosition - origin;
				var dist = direction.Length();

				if (dist < 0.001f)
					direction = Vector3.Up;
				else
					direction /= dist;

				var falloff = Mathf.Clamp(1.0f - (dist / Radius), 0.0f, 1.0f);
				actor.ActorBody.ApplyImpulse(direction * ImpulseStrength * falloff);
			}

		   
		}
	}
}
