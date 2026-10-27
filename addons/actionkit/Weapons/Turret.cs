#nullable enable
using Godot;

namespace ActionKit.Weapons;

/// <summary>
/// A Weapon that slews a yaw node and a pitch node towards GetAimDirection() and only counts
/// as on target within FireArc degrees. Put the emitters under PitchNode.
/// </summary>
[GlobalClass]
public partial class Turret : Weapon
{
	[ExportGroup("Turret")]
	[Export] public Node3D? YawNode { get; set; }
	[Export] public Node3D? PitchNode { get; set; }

	/// <summary>How quickly the yaw node follows the aim (slerp rate per second).</summary>
	[Export] public float YawSpeed { get; set; } = 90.0f;

	/// <summary>How quickly the pitch node follows the aim (lerp rate per second).</summary>
	[Export] public float PitchSpeed { get; set; } = 60.0f;

	[Export] public float FireArc { get; set; } = 5.0f;
	[Export] public float PitchMin { get; set; } = -30.0f;
	[Export] public float PitchMax { get; set; } = 60.0f;

	private void SlewToward(Vector3 worldDir, float delta)
	{
		Yaw(worldDir, delta);
		Pitch(worldDir, delta);
	}

	private void Yaw(Vector3 worldDir, float delta)
	{
		if (YawNode == null) return;

		worldDir = worldDir.Normalized();

		var yawUp = YawNode.GlobalTransform.Basis.Y.Normalized();
		var yawForward = worldDir - yawUp * worldDir.Dot(yawUp);
		if (yawForward.LengthSquared() <= 0.001f) return;

		var desired = Basis.LookingAt(yawForward.Normalized(), yawUp);

		var t = YawNode.GlobalTransform;
		var w = Mathf.Clamp(YawSpeed * delta, 0f, 1f);

		t.Basis = t.Basis.Orthonormalized().Slerp(desired, w);
		YawNode.GlobalTransform = t;
	}

	private void Pitch(Vector3 worldDir, float delta)
	{
		if (PitchNode == null) return;

		worldDir = worldDir.Normalized();

		var localForward = PitchNode.GlobalTransform.Basis.Orthonormalized().Transposed() * worldDir;
		if (localForward.LengthSquared() <= 0.001f) return;

		var deltaPitch = Mathf.Atan2(localForward.Y, -localForward.Z);

		var rot = PitchNode.Rotation;
		var target = Mathf.Clamp(rot.X + deltaPitch, Mathf.DegToRad(PitchMin), Mathf.DegToRad(PitchMax));

		rot.X = Mathf.LerpAngle(rot.X, target, Mathf.Clamp(PitchSpeed * delta, 0f, 1f));
		PitchNode.Rotation = rot;
	}

	public override bool IsOnTarget()
	{
		var aimNode = PitchNode ?? YawNode;
		if (!IsInstanceValid(Target) || aimNode == null) return false;

		var toTarget = Target!.GlobalPosition - aimNode.GlobalPosition;
		if (toTarget.LengthSquared() <= 0.001f) return false;

		var forward = -aimNode.GlobalTransform.Basis.Z.Normalized();
		var angleDeg = Mathf.RadToDeg(Mathf.Acos(Mathf.Clamp(forward.Dot(toTarget.Normalized()), -1.0f, 1.0f)));
		return angleDeg <= FireArc;
	}

	public virtual void TrackTarget(float delta)
	{
		SlewToward(GetAimDirection(), delta);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (Engine.IsEditorHint()) return;
		TrackTarget((float)delta);
	}
}
