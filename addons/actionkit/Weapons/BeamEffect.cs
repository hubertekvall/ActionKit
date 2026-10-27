#nullable enable
using Godot;

namespace ActionKit.Weapons;

/// <summary>
/// A continuous beam actor. Sphere-casts BeamLength forward (-Z) every physics frame, eases
/// its BeamMeshes to the hit distance, and broadcasts an ActorCollisionMessage to itself for
/// the actor it hits, so a CollisionDamage component on the beam deals damage (give it a
/// DamageCooldown for damage-per-tick). Spawned and killed by BeamWeapon.
/// </summary>
[GlobalClass]
public partial class BeamEffect : Actor
{
	[Export] public BeamMesh[] BeamMeshes { get; set; } = [];
	[Export] public float BeamLength { get; set; } = 20f;
	[Export] public float BeamRadius { get; set; } = 0.1f;
	[Export] public float BeamEaseSpeed { get; set; } = 10f;
	[Export(PropertyHint.Layers3DPhysics)] public uint CollisionMask { get; set; } = 1;

	/// <summary>Optional node moved to the beam's end point (impact sparks, a light).</summary>
	[Export] public Node3D? BeamEnd { get; set; }

	public ShapeCast3D? ShapeCast { get; private set; }

	private float _currentBeamScale;

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint()) return;

		ShapeCast = new ShapeCast3D
		{
			Shape = new SphereShape3D { Radius = BeamRadius },
			TargetPosition = Vector3.Forward * BeamLength,
			CollisionMask = CollisionMask,
			CollideWithAreas = false,
			CollideWithBodies = true,
			Enabled = false,
			MaxResults = 8,
		};

		if (Spawner?.ActorBody != null)
			ShapeCast.AddExceptionRid(Spawner.ActorBody.GetRid());

		AddChild(ShapeCast);

		_currentBeamScale = BeamLength;
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if (ShapeCast == null) return;

		ShapeCast.TargetPosition = Vector3.Forward * BeamLength;
		ShapeCast.ForceShapecastUpdate();

		var targetScale = ShapeCast.IsColliding() ? ProcessHit(ShapeCast) : BeamLength;
		_currentBeamScale = Mathf.Lerp(_currentBeamScale, targetScale, BeamEaseSpeed * (float)delta);

		foreach (var beamMesh in BeamMeshes)
			beamMesh.BeamScale = _currentBeamScale;

		if (BeamEnd != null)
			BeamEnd.Position = Vector3.Forward * _currentBeamScale;
	}

	private float ProcessHit(ShapeCast3D shapeCast)
	{
		var hitPoint = shapeCast.GetCollisionPoint(0);
		var hitNormal = shapeCast.GetCollisionNormal(0);
		var collider = shapeCast.GetCollider(0) as CollisionObject3D;

		TrySendCollisionMessage(collider, hitPoint, hitNormal);

		return hitPoint.DistanceTo(GlobalPosition);
	}

	private void TrySendCollisionMessage(CollisionObject3D? collider, Vector3 hitPoint, Vector3 hitNormal)
	{
		if (collider?.GetParent() is not Actor actor) return;

		var contact = new ActorCollisionContact(
			position: hitPoint,
			normal: hitNormal,
			otherBody: collider,
			otherPosition: actor.GlobalPosition,
			otherVelocity: actor.ActorBody?.LinearVelocity ?? Vector3.Zero,
			otherActor: actor);

		Broadcast(new ActorCollisionMessage(actor, contact).Signed(this));
	}
}
