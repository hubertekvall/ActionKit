#nullable enable
using Godot;

namespace ActionKit.Components;

[GlobalClass]
public partial class VelocityFade : ActorComponent
{
	[Export] public GeometryInstance3D? VisualInstance { get; set; }
	[Export] public float TransparencyOffset { get; set; } = 1f;
	[Export] public float MaxSpeed { get; set; } = 10f;
	[Export] public float SmoothSpeed { get; set; } = 8f;

	private float _restTransparency;

	public override void _Ready()
	{
		base._Ready();

		if (VisualInstance != null)
			_restTransparency = VisualInstance.Transparency;
	}

	public override void PhysicsTick(double delta)
	{
		if (ActorOwner.ActorBody == null || VisualInstance == null) return;

		Vector3 velocity = ActorOwner.ActorBody.LinearVelocity;

		float speedFraction = MaxSpeed > 0f ? Mathf.Clamp(velocity.Length() / MaxSpeed, 0f, 1f) : 0f;

		float targetTransparency = Mathf.Clamp(_restTransparency + TransparencyOffset * speedFraction * speedFraction * speedFraction, 0f, 1f);

		float smoothT = 1f - Mathf.Exp(-SmoothSpeed * (float)delta);
		VisualInstance.Transparency = Mathf.Lerp(VisualInstance.Transparency, targetTransparency, smoothT);
	}
}
