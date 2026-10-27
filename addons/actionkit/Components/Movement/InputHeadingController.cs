#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Reads a 2D input vector from four input actions and feeds it, rotated to the camera's
/// view on the ground plane, into the actor's ISteerable (e.g. FlightMovement).
/// </summary>
[GlobalClass]
public partial class InputHeadingController : ActorComponent
{
	[Export] public StringName LeftAction { get; set; } = "ui_left";
	[Export] public StringName RightAction { get; set; } = "ui_right";
	[Export] public StringName UpAction { get; set; } = "ui_up";
	[Export] public StringName DownAction { get; set; } = "ui_down";

	/// <summary>Camera to steer relative to. Defaults to the viewport's active camera.</summary>
	[Export] public Camera3D? Camera { get; set; }

	private ISteerable? _steerable;

	public override void OnActorReady()
	{
		_steerable = ActorOwner.GetSteerable();
	}

	public override void ProcessTick(double delta)
	{
		if (_steerable == null) return;

		var camera = Camera ?? GetViewport().GetCamera3D();
		var input = Input.GetVector(LeftAction, RightAction, UpAction, DownAction);
		var heading = new Vector3(input.X, 0f, input.Y);

		if (camera != null)
		{
			heading = camera.GlobalBasis * heading;
			heading.Y = 0f;
			heading = heading.LimitLength(input.Length());
		}

		_steerable.SetHeading(heading);
	}
}
