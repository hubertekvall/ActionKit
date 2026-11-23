#nullable enable
using ActionKit.Components;
using ActionKit.Utilities;
using Godot;

namespace ActionKit.Weapons;

/// <summary>
/// Something instanced by MouseAim while it is active (an aiming overlay, a reticle, a guide
/// line). MouseAim hands itself over before the node enters the tree, so it can read AimPoint,
/// AimHit, SnapTarget and so on from _Ready on.
/// </summary>
public interface IMouseAimView
{
	MouseAim? MouseAim { get; set; }
}

/// <summary>
/// Goes on the player's actor. Every physics frame, weapons under the actor that are in Manual
/// mode get their aim point from the mouse, in this order:
/// 1. Snap: a TargetGroup member within SnapRadius screen pixels of the mouse. Each weapon
///    leads it using the target's velocity and the weapon's ProjectileSpeed.
/// 2. Otherwise, where the mouse ray crosses the actor's height plane (PlanePoint), a sphere of
///    AimRadius is swept from the actor towards it. If it touches something, aim there.
/// 3. Otherwise, aim at PlanePoint itself, so you can lead a target into empty space.
///
/// Manual weapons fire while the aim is Active and FireAction is held. Active follows
/// ActivateAction while it's held (an "aim mode" on right mouse, say), or is always on when
/// ActivateAction is empty. While Active, each of ActiveScenes is instanced into the current
/// scene and freed again afterwards; roots implementing IMouseAimView get this component.
/// </summary>
[GlobalClass]
public partial class MouseAim : ActorComponent
{
	/// <summary>Camera the mouse is projected through. Defaults to the viewport's active camera.</summary>
	[Export] public Camera3D? Camera { get; set; }

	[ExportGroup("Input")]
	/// <summary>Input action that must be held for aiming. Empty means always active.</summary>
	[Export] public StringName ActivateAction { get; set; } = "";

	/// <summary>Input action that pulls the trigger of Manual weapons.</summary>
	[Export] public StringName FireAction { get; set; } = "fire";

	[ExportGroup("Aiming")]
	[Export(PropertyHint.Layers3DPhysics)] public uint AimCollisionMask { get; set; } = uint.MaxValue;

	/// <summary>World-unit radius of the sphere swept from the actor. Bigger = more forgiving aim.</summary>
	[Export] public float AimRadius { get; set; } = 2.0f;

	/// <summary>Targets this close to the mouse (in screen pixels) are snapped to and led.</summary>
	[Export] public float SnapRadius { get; set; } = 40.0f;

	/// <summary>Group that can be snapped to. Empty turns snapping off.</summary>
	[Export] public StringName TargetGroup { get; set; } = "";

	[ExportGroup("While Active")]
	/// <summary>Scenes added to the current scene while active (overlays, reticles), freed afterwards.</summary>
	[Export] public PackedScene[] ActiveScenes { get; set; } = [];

	[Signal]
	public delegate void ActiveChangedEventHandler(bool active);

	public bool Active { get; private set; }

	/// <summary>Where the aim currently hits something (the snapped target, or the sphere hit), or null.</summary>
	public Vector3? AimHit { get; private set; }

	/// <summary>The target snapped to this frame, or null.</summary>
	public Node3D? SnapTarget { get; private set; }

	/// <summary>Where the aim ends up this frame: AimHit, else PlanePoint. Null if the mouse ray misses the plane.</summary>
	public Vector3? AimPoint { get; private set; }

	/// <summary>Where the mouse ray crosses the actor's height plane this frame, or null.</summary>
	public Vector3? PlanePoint { get; private set; }

	/// <summary>The actor's (body's) position this frame; the origin of the aim.</summary>
	public Vector3 OriginPosition { get; private set; }

	private readonly SphereShape3D _aimShape = new();
	private readonly System.Collections.Generic.List<Node> _activeNodes = [];

	public override void OnActorDetached()
	{
		if (Active) SetActive(false);
	}

	public override void _ExitTree()
	{
		if (Active) SetActive(false);
	}

	public override void ProcessTick(double delta)
	{
		var active = ActivateAction.IsEmpty || (InputMap.HasAction(ActivateAction) && Input.IsActionPressed(ActivateAction));
		if (active != Active) SetActive(active);
	}

	public override void PhysicsTick(double delta)
	{
		var camera = Camera ?? GetViewport().GetCamera3D();
		if (camera == null) return;

		var mouse = camera.GetMousePositionScaleAdjusted();
		var origin = camera.ProjectRayOrigin(mouse);
		var direction = camera.ProjectRayNormal(mouse);
		var originPosition = ActorOwner.ActorBody?.GlobalPosition ?? ActorOwner.GlobalPosition;
		var planePoint = new Plane(Vector3.Up, originPosition.Y).IntersectsRay(origin, direction);

		var snap = SnapTarget = FindSnapTarget(camera);
		AimHit = snap != null ? TargetPosition(snap)
			: planePoint is { } p ? ShapeCast(originPosition, p - originPosition)
			: null;
		AimPoint = AimHit ?? planePoint;
		PlanePoint = planePoint;
		OriginPosition = originPosition;

		var triggerHeld = Active && !FireAction.IsEmpty && InputMap.HasAction(FireAction) && Input.IsActionPressed(FireAction);

		foreach (var weapon in ActorOwner.Weapons)
		{
			if (!IsInstanceValid(weapon) || weapon.Mode != Weapon.WeaponMode.Manual) continue;
			weapon.ManualAimPoint = snap != null
				? Ballistics.LeadPoint(weapon.GlobalPosition, TargetPosition(snap), TargetVelocity(snap), weapon.ProjectileSpeed)
				: AimPoint;
			weapon.TriggerHeld = triggerHeld;
		}
	}

	private void SetActive(bool on)
	{
		Active = on;

		foreach (var node in _activeNodes)
			if (IsInstanceValid(node)) node.QueueFree();
		_activeNodes.Clear();

		if (on && IsInsideTree())
		{
			var parent = GetTree().CurrentScene ?? GetTree().Root;
			foreach (var scene in ActiveScenes)
			{
				if (scene == null) continue;
				var node = scene.Instantiate();
				if (node is IMouseAimView view) view.MouseAim = this;
				parent.AddChild(node);
				_activeNodes.Add(node);
			}
		}

		EmitSignalActiveChanged(on);
	}

	// The TargetGroup member nearest the mouse on screen, if it's within SnapRadius pixels.
	private Node3D? FindSnapTarget(Camera3D camera)
	{
		if (TargetGroup.IsEmpty) return null;

		var mouse = GetTree().Root.GetMousePosition();
		Node3D? best = null;
		var bestDistance = SnapRadius;

		foreach (var node in GetTree().GetNodesInGroup(TargetGroup))
		{
			if (node is not Node3D target || !target.IsInsideTree() || target == ActorOwner) continue;
			var position = TargetPosition(target);
			if (camera.IsPositionBehind(position)) continue;

			var distance = camera.UnprojectPositionScaleAdjusted(position).DistanceTo(mouse);
			if (distance > bestDistance) continue;
			bestDistance = distance;
			best = target;
		}

		return best;
	}

	private static Vector3 TargetPosition(Node3D target) =>
		(target as Actor)?.ActorBody?.GlobalPosition ?? target.GlobalPosition;

	private static Vector3 TargetVelocity(Node3D target) =>
		(target as Actor)?.ActorBody?.LinearVelocity ?? Vector3.Zero;

	// Sweeps a sphere from origin along motion. Returns the point where it first touches something.
	private Vector3? ShapeCast(Vector3 origin, Vector3 motion)
	{
		_aimShape.Radius = AimRadius;
		var query = new PhysicsShapeQueryParameters3D
		{
			Shape = _aimShape,
			Transform = new Transform3D(Basis.Identity, origin),
			Motion = motion,
			CollisionMask = AimCollisionMask,
		};
		if (ActorOwner.ActorBody != null) query.Exclude = [ActorOwner.ActorBody.GetRid()];

		var space = ActorOwner.GetWorld3D().DirectSpaceState;
		var fractions = space.CastMotion(query);
		if (fractions.Length < 2 || fractions[1] >= 1.0f) return null;

		// Move the sphere to where it first touches and ask what it touched.
		query.Transform = new Transform3D(Basis.Identity, origin + query.Motion * fractions[1]);
		query.Motion = Vector3.Zero;
		var info = space.GetRestInfo(query);
		if (info.Count == 0) return null;

		return info["point"].AsVector3();
	}
}
