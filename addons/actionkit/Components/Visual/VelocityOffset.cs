#nullable enable
using Godot;

namespace ActionKit.Components;

[GlobalClass]
public partial class VelocityOffset : ActorComponent
{
	[Export] public Node3D? OffsetNode { get; set; }
	[Export] public Vector3 Offset { get; set; }
	[Export] public bool IsLocal { get; set; } = true;
	[Export] public float MaxSpeed { get; set; } = 10f;

	[Export] public float SmoothSpeed { get; set; } = 8f;

	private Vector3 _restPosition;

	public override void _Ready()
	{
		base._Ready();

		if (OffsetNode != null)
			_restPosition = OffsetNode.Position;
	}

	public override void PhysicsTick(double delta)
	{
		if (ActorOwner.ActorBody == null || OffsetNode == null) return;

		Vector3 velocity = ActorOwner.ActorBody.LinearVelocity;

		/*
		if (IsLocal)
			velocity = ActorOwner.ActorBody.GlobalBasis.Inverse() * velocity;
			*/

		float speedFraction = MaxSpeed > 0f ? Mathf.Clamp(velocity.Length() / MaxSpeed, 0f, 1f) : 0f;

   

		Vector3 targetPosition = _restPosition + Offset * speedFraction;

		float smoothT = 1f - Mathf.Exp(-SmoothSpeed * (float)delta);
		OffsetNode.Position = OffsetNode.Position.Lerp(targetPosition, smoothT);
	}
}
