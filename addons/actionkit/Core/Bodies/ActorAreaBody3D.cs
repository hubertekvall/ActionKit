#nullable enable
using Godot;

namespace ActionKit;

// An IActorBody3D backed by an Area3D instead of a physics body. There's no physics
// solver behind it, so it moves by directly integrating position/rotation every
// physics frame (see _PhysicsProcess) rather than being simulated, and it detects hits
// by overlap instead of collision response — nothing pushes it and it pushes nothing
// back, it just reports whatever it's currently overlapping, the same way
// ActorRigidBody3D/ActorCharacterBody3D report their contacts every frame.
//
// Good for anything that should move straight through obstacles it isn't meant to
// collide with while still detecting hits — fast projectiles, homing pods, ghostly
// enemies — and it plugs straight into existing steering (HomingComponent, FlightMovement)
// unchanged, since those only ever talk to IActorBody3D, never to a concrete body type.
[GlobalClass]
public partial class ActorAreaBody3D : Area3D, IActorBody3D
{
	public Actor ActorOwner { get; set; } = null!;

	[Export] public float Mass { get; set; } = 1f;

	public Vector3 AngularVelocity { get; set; }
	public Vector3 LinearVelocity { get; set; }


	// --- Impulse / Force accumulation --- (mirrors ActorCharacterBody3D exactly: there's
	// no physics solver here either, so forces/torques accumulate and get integrated by
	// hand in _PhysicsProcess instead of being handed off to one.)

	private Vector3 _accumulatedForce;
	private Vector3 _accumulatedTorque;

	public void ApplyImpulse(Vector3 impulse, Vector3? position = null)
	{
		LinearVelocity += impulse / Mass;
		if (position.HasValue)
			AngularVelocity += position.Value.Cross(impulse) / Mass;
	}

	public void ApplyCentralImpulse(Vector3 impulse) =>
		LinearVelocity += impulse / Mass;

	public void ApplyForce(Vector3 force, Vector3? position = null)
	{
		_accumulatedForce += force;
		if (position.HasValue)
			_accumulatedTorque += position.Value.Cross(force);
	}

	public void ApplyCentralForce(Vector3 force) =>
		_accumulatedForce += force;

	public void ApplyTorqueImpulse(Vector3 torque) =>
		AngularVelocity += torque / Mass;

	public void ApplyTorque(Vector3 torque) =>
		_accumulatedTorque += torque;


	public void CheckForContacts()
	{
		foreach (var body in GetOverlappingBodies())
			if (body is CollisionObject3D other) Report(other);

		foreach (var area in GetOverlappingAreas())
			Report(area);
	}

	private void Report(CollisionObject3D other)
	{
		var contact = CollisionContactFactory.GenerateContact(this, other);
		if (contact != null) ActorOwner.Broadcast(contact.GenerateCollisionMessage());
	}

	public override void _PhysicsProcess(double delta)
	{
		var dt = (float)delta;

		LinearVelocity += _accumulatedForce / Mass * dt;
		AngularVelocity += _accumulatedTorque / Mass * dt;
		_accumulatedForce = Vector3.Zero;
		_accumulatedTorque = Vector3.Zero;

		// No MoveAndSlide — there's no solver to resolve against, so position/rotation
		// are just integrated directly. That's the whole point of choosing this body:
		// it moves through anything it overlaps instead of being stopped by it.
		GlobalPosition += LinearVelocity * dt;
		GlobalBasis = (GlobalBasis * Basis.FromEuler(AngularVelocity * dt)).Orthonormalized();

		CheckForContacts();
	}
}
