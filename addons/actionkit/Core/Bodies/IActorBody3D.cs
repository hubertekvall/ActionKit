#nullable enable
using Godot;

namespace ActionKit;

/// <summary>
/// Common surface over rigid, character and area bodies so components can push, steer and
/// read velocity without caring which physics model an actor uses.
/// </summary>
public interface IActorBody3D
{
	public Actor ActorOwner { get; set; }
	public Transform3D GlobalTransform { get; set; }
	public Basis GlobalBasis { get; set; }
	public Vector3 GlobalPosition { get; set; }
	public float Mass { get; set; }
	public Rid GetRid();

	public Vector3 AngularVelocity { get; set; }
	public Vector3 LinearVelocity { get; set; }
	public void ApplyImpulse(Vector3 impulse, Vector3? position = null);
	public void ApplyCentralImpulse(Vector3 impulse);
	public void ApplyForce(Vector3 force, Vector3? position = null);
	public void ApplyCentralForce(Vector3 force);
	public void ApplyTorqueImpulse(Vector3 torque);
	public void ApplyTorque(Vector3 torque);
	public void LookAt(Vector3 target, Vector3? up = null, bool useModelFront = false);
	public void Rotate(Vector3 axis, float angle);

	/// <summary>Collects this frame's contacts and broadcasts them to ActorOwner as CollisionMessages.</summary>
	public void CheckForContacts();
}
