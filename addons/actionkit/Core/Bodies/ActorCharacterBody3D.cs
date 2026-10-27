#nullable enable
using Godot;

namespace ActionKit;

[GlobalClass]
public partial class ActorCharacterBody3D : CharacterBody3D, IActorBody3D
{
	public Actor ActorOwner { get; set; } = null!;

	[Export] public float Mass { get; set; } = 1f;

	public Vector3 AngularVelocity { get; set; }

	public Vector3 LinearVelocity
	{
		get => Velocity;
		set => Velocity = value;
	}


	// --- Impulse / Force accumulation ---

	private Vector3 _accumulatedForce;
	private Vector3 _accumulatedTorque;

	public void ApplyImpulse(Vector3 impulse, Vector3? position = null)
	{
		Velocity += impulse / Mass;
		if (position.HasValue)
			AngularVelocity += position.Value.Cross(impulse) / Mass;
	}

	public void ApplyCentralImpulse(Vector3 impulse) =>
		Velocity += impulse / Mass;

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



	// Slide collisions are only known after MoveAndSlide, so _PhysicsProcess broadcasts them.
	public void CheckForContacts()
	{
		this.CollectAndBroadcast();
	}

	public override void _PhysicsProcess(double delta)
	{
		var dt = (float)delta;

		Velocity += _accumulatedForce / Mass * dt;
		AngularVelocity += _accumulatedTorque / Mass * dt;
		_accumulatedForce = Vector3.Zero;
		_accumulatedTorque = Vector3.Zero;

		GlobalBasis = (GlobalBasis * Basis.FromEuler(AngularVelocity * dt)).Orthonormalized();

		MoveAndSlide();
		CheckForContacts();
	}
}
