#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// A spring-damper that holds the actor's body at HoverHeight (a corrective vertical force
/// each physics tick) and tilts ModelPivot to lean into vertical velocity. Disable it, or
/// lower Strength, and whatever gravity the body has takes over.
/// </summary>
[GlobalClass]
public partial class HoverComponent : ActorComponent
{
	[ExportGroup("Hover")]
	[Export] public float HoverHeight { get; set; } = 3.0f;
	[Export] public float HoverStiffness { get; set; } = 40.0f;
	[Export] public float HoverDamping { get; set; } = 8.0f;
	[Export] public float HoverMaxError { get; set; } = 0.5f;

	/// <summary>Multiplier on the hover force. Tween it to 0 to let the actor sink.</summary>
	[Export(PropertyHint.Range, "0,2,0.01,or_greater")] public float Strength { get; set; } = 1.0f;

	[ExportGroup("Tilt")]
	[Export] public float HoverTiltMaxAngle { get; set; } = 0.3f;
	[Export] public float TiltSpeed { get; set; } = 5.0f;

	public override void PhysicsTick(double delta)
	{
		var actorBody = ActorOwner.ActorBody;
		if (actorBody == null) return;

		float dt = (float)delta;
		float heightError = HoverHeight - actorBody.GlobalPosition.Y;
		float verticalVelocity = actorBody.LinearVelocity.Y;

		// Clamped before scaling by stiffness so a big sudden drop doesn't produce a runaway
		// corrective force.
		float clampedError = Mathf.Clamp(heightError, -HoverMaxError, HoverMaxError);
		float hoverForce = clampedError * HoverStiffness - verticalVelocity * HoverDamping;

		actorBody.ApplyCentralForce(Vector3.Up * hoverForce * actorBody.Mass * Strength);

		var pivot = ActorOwner.ModelPivot;
		if (pivot == null) return;

		float tiltAngle = Mathf.Clamp(verticalVelocity, -HoverTiltMaxAngle, HoverTiltMaxAngle);
		var rot = pivot.Rotation;
		rot.X = Mathf.Lerp(rot.X, tiltAngle, TiltSpeed * dt);
		pivot.Rotation = rot;
	}
}
