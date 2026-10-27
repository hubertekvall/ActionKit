#nullable enable
using Godot;

namespace ActionKit.Components;

[GlobalClass]
public partial class VelocityShaderParameter : ActorComponent
{
	[Export] public GeometryInstance3D? VisualInstance { get; set; }
	[Export] public StringName? ParameterName { get; set; }
	[Export] public float RestValue { get; set; }
	[Export] public float ValueOffset { get; set; } = 1f;
	[Export] public float MaxSpeed { get; set; } = 10f;
	[Export] public float SmoothSpeed { get; set; } = 8f;

	private float _currentValue;

	public override void _Ready()
	{
		base._Ready();

		_currentValue = RestValue;
	}

	public override void PhysicsTick(double delta)
	{
		if (ActorOwner.ActorBody == null || VisualInstance == null) return;

		Vector3 velocity = ActorOwner.ActorBody.LinearVelocity;

		float speedFraction = MaxSpeed > 0f ? Mathf.Clamp(velocity.Length() / MaxSpeed, 0f, 1f) : 0f;

		float targetValue = RestValue + ValueOffset * speedFraction ;

		float smoothT = 1f - Mathf.Exp(-SmoothSpeed * (float)delta);
		_currentValue = Mathf.Lerp(_currentValue, targetValue, smoothT);
		
		VisualInstance.SetInstanceShaderParameter(ParameterName, _currentValue);
	}
}
