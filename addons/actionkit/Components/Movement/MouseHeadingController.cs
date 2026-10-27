#nullable enable
using ActionKit.Utilities;
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Click-and-hold steering: while the mouse button is held, the actor heads towards the
/// point under the cursor on its own ground plane, with throttle growing with distance up to
/// FullThrottleDistance. Releasing the button zeroes the heading.
/// </summary>
[GlobalClass]
public partial class MouseHeadingController : ActorComponent
{
	[Export] public MouseButton Button { get; set; } = MouseButton.Left;
	[Export] public float FullThrottleDistance { get; set; } = 25.0f;

	/// <summary>Camera to cast from. Defaults to the viewport's active camera.</summary>
	[Export] public Camera3D? Camera { get; set; }

	private ISteerable? _steerable;
	private bool _pressed;

	public override void OnActorReady()
	{
		_steerable = ActorOwner.GetSteerable();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == Button)
			_pressed = mouseButton.Pressed;
	}

	public override void PhysicsTick(double delta)
	{
		if (_steerable == null) return;

		var camera = Camera ?? GetViewport().GetCamera3D();
		if (!_pressed || camera == null)
		{
			_steerable.SetHeading(Vector3.Zero, false);
			return;
		}

		var screenPosition = camera.GetMousePositionScaleAdjusted();
		var origin = camera.ProjectRayOrigin(screenPosition);
		var normal = camera.ProjectRayNormal(screenPosition);
		var plane = new Plane(Vector3.Up, ActorOwner.GlobalPosition.Y);

		if (plane.IntersectsRay(origin, normal) is not { } point) return;

		var heading = ActorOwner.GlobalPosition.DirectionTo(point).Flattened();
		var throttle = Mathf.Clamp(ActorOwner.GlobalPosition.DistanceTo(point) / FullThrottleDistance, 0.0f, 1.0f);
		_steerable.SetHeading(heading * throttle);
	}
}
