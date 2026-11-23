#nullable enable
using Godot;

namespace ActionKit.Weapons;

/// <summary>
/// A Weapon that slews a yaw node and a pitch node towards GetAimDirection() and only counts
/// as on target within FireArc degrees. Put the emitters under PitchNode. YawMin/YawMax limit
/// how far it can turn from its rest direction (a broadside gun would use -90..90); limited
/// turrets never swing through the blocked side, and CanFace tells targeting what's reachable.
/// </summary>
[GlobalClass]
public partial class Turret : Weapon
{
	[ExportGroup("Turret")]
	[Export] public Node3D? YawNode { get; set; }
	[Export] public Node3D? PitchNode { get; set; }

	/// <summary>How quickly the yaw node follows the aim (lerp rate per second).</summary>
	[Export] public float YawSpeed { get; set; } = 90.0f;

	/// <summary>How quickly the pitch node follows the aim (lerp rate per second).</summary>
	[Export] public float PitchSpeed { get; set; } = 60.0f;

	[Export] public float FireArc { get; set; } = 5.0f;

	/// <summary>
	/// Yaw limits in degrees, measured from the direction YawNode faces in the scene.
	/// -180..180 means it can turn all the way round.
	/// </summary>
	[Export] public float YawMin { get; set; } = -180.0f;
	[Export] public float YawMax { get; set; } = 180.0f;

	[Export] public float PitchMin { get; set; } = -30.0f;
	[Export] public float PitchMax { get; set; } = 60.0f;

	private float _yawRest;

	public override void _Ready()
	{
		base._Ready();
		if (YawNode != null) _yawRest = YawNode.Rotation.Y;
	}

	private void SlewToward(Vector3 worldDir, float delta)
	{
		Yaw(worldDir, delta);
		Pitch(worldDir, delta);
	}

	// Yaw toward worldDir in YawNode's parent space, or null if worldDir is straight up/down.
	private float? LocalYaw(Vector3 worldDir)
	{
		if (YawNode?.GetParent() is not Node3D parent) return null;

		var local = parent.GlobalBasis.Inverse() * worldDir;
		local.Y = 0;
		if (local.LengthSquared() <= 0.000001f) return null;

		return Mathf.Atan2(-local.X, -local.Z);
	}

	// How far a yaw is from rest, wrapped to -PI..PI.
	private float OffsetFromRest(float yaw) => Mathf.Wrap(yaw - _yawRest, -Mathf.Pi, Mathf.Pi);

	private bool IsYawLimited => YawMax - YawMin < 360.0f;

	private void Yaw(Vector3 worldDir, float delta)
	{
		if (YawNode == null) return;
		if (LocalYaw(worldDir) is not { } targetYaw) return;

		var targetOffset = Mathf.Clamp(OffsetFromRest(targetYaw), Mathf.DegToRad(YawMin), Mathf.DegToRad(YawMax));
		var w = Mathf.Clamp(YawSpeed * delta, 0f, 1f);

		var rot = YawNode.Rotation;
		// With limits, lerp the offset so the turret never swings through the blocked side.
		rot.Y = IsYawLimited
			? _yawRest + Mathf.Lerp(OffsetFromRest(rot.Y), targetOffset, w)
			: Mathf.LerpAngle(rot.Y, _yawRest + targetOffset, w);
		YawNode.Rotation = rot;
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

	public override bool CanFace(Vector3 point)
	{
		if (!IsYawLimited || YawNode == null) return true;
		if (LocalYaw(point - YawNode.GlobalPosition) is not { } yaw) return true;

		var offset = OffsetFromRest(yaw);
		var arc = Mathf.DegToRad(FireArc);
		return offset >= Mathf.DegToRad(YawMin) - arc && offset <= Mathf.DegToRad(YawMax) + arc;
	}

	public override bool IsOnTarget()
	{
		var aimNode = PitchNode ?? YawNode;
		if (GetAimPoint() is not { } aimPoint || aimNode == null) return false;

		var toTarget = aimPoint - aimNode.GlobalPosition;
		if (toTarget.LengthSquared() <= 0.001f) return false;

		var forward = -aimNode.GlobalTransform.Basis.Z.Normalized();
		var angleDeg = Mathf.RadToDeg(Mathf.Acos(Mathf.Clamp(forward.Dot(toTarget.Normalized()), -1.0f, 1.0f)));
		return angleDeg <= FireArc;
	}

	public virtual void TrackTarget(float delta)
	{
		if (Mode == WeaponMode.Off) return;
		SlewToward(GetAimDirection(), delta);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (Engine.IsEditorHint()) return;
		TrackTarget((float)delta);
	}
}
