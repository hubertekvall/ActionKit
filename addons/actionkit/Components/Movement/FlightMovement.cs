#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Arcade flight/hover-craft movement on the XZ plane. Accelerates the body towards
/// Heading * MaxSpeed, turns it to face LookDirection with torque, keeps it upright, and
/// rolls ModelPivot into turns. Pair with HoverComponent to hold altitude.
///
/// Drive it through ISteerable (SetHeading / SetLookDirection) from an input controller, a
/// behavior tree or your own code.
/// </summary>
[GlobalClass]
public partial class FlightMovement : ActorComponent, ISteerable
{
	[ExportGroup("Movement")]
	[Export] public float TurnSpeed { get; set; } = 2.5f;
	[Export] public float MaxSpeed { get; set; } = 40.0f;
	[Export] public float Acceleration { get; set; } = 800.0f;
	[Export] public float MaxAccelerationForce { get; set; } = 500f;

	[ExportGroup("Stabilization")]
	[Export] public float StabilizationStiffness { get; set; } = 30.0f;
	[Export] public float StabilizationDamping { get; set; } = 6.0f;

	[ExportGroup("Roll")]
	[Export] public float RollFactor { get; set; } = 0.3f;
	[Export] public float RollSpeed { get; set; } = 5.0f;

	public Vector3 Heading { get; private set; } = Vector3.Zero;
	public Vector3 LookDirection { get; private set; } = Vector3.Zero;

	private Vector3 _targetVelocity;

	public void SetHeading(Vector3 heading, bool lookAtHeading = true)
	{
		Heading = heading;
		if (lookAtHeading) LookDirection = heading;
	}

	public void SetLookDirection(Vector3 lookDirection) => LookDirection = lookDirection;

	public void ResetHeading() => Heading = Vector3.Zero;

	public override void PhysicsTick(double delta)
	{
		var body = ActorOwner.ActorBody;
		if (body == null) return;

		TurnActor(body);
		MoveInHeading(body, (float)delta);
		Stabilize(body);
		HandleRoll(body, (float)delta);
	}

	private void TurnActor(IActorBody3D body)
	{
		var currentForward = -body.GlobalBasis.Z;
		var targetForward = new Vector3(LookDirection.X, 0f, LookDirection.Z).Normalized();
		if (targetForward.IsZeroApprox()) return;

		var q = new Quaternion(currentForward, targetForward);
		var desiredAv = q.GetAxis() * q.GetAngle() * TurnSpeed;
		body.ApplyTorque(desiredAv * body.Mass);
	}

	private void MoveInHeading(IActorBody3D body, float dt)
	{
		var unitVel = _targetVelocity.Normalized();
		float velocityDot = unitVel.Dot(body.LinearVelocity.Normalized());
		float accCurve = Mathf.Lerp(2.0f, 1.0f, Mathf.SmoothStep(-1.0f, 1.0f, velocityDot));

		var goalVelocity = Heading * MaxSpeed;
		_targetVelocity = _targetVelocity.MoveToward(goalVelocity, Acceleration * accCurve * dt);

		var currentHorizontalVel = new Vector3(body.LinearVelocity.X, 0f, body.LinearVelocity.Z);
		var neededAcceleration = ((_targetVelocity - currentHorizontalVel) / dt).LimitLength(MaxAccelerationForce * accCurve);

		body.ApplyCentralForce(neededAcceleration);
	}

	private void Stabilize(IActorBody3D body)
	{
		var correctionAxis = body.GlobalBasis.Y.Cross(Vector3.Up);
		var torque = correctionAxis * StabilizationStiffness - body.AngularVelocity * StabilizationDamping;
		body.ApplyTorque(torque * body.Mass);
	}

	private void HandleRoll(IActorBody3D body, float dt)
	{
		var pivot = ActorOwner.ModelPivot;
		if (pivot == null) return;

		var cross = (-body.GlobalBasis.Z).Cross(Heading);
		var rot = pivot.Rotation;
		rot.Z = Mathf.Lerp(rot.Z, cross.Y * RollFactor, RollSpeed * dt);
		pivot.Rotation = rot;
	}
}
