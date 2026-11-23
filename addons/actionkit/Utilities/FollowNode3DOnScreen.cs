#nullable enable
using Godot;

namespace ActionKit.Utilities;

/// <summary>
/// Put this under a Node2D or Control. Every frame it moves that parent to where Target shows
/// up on screen, and hides the parent while Target is behind the camera. A Control is placed
/// with its PivotOffset on the point, so set the pivot to where it should be anchored.
/// Scales from the camera's (Sub)Viewport up to the window, so the parent should be on a
/// CanvasLayer that isn't offset or scaled. Good for health bars, name tags, markers.
/// </summary>
[GlobalClass]
public partial class FollowNode3DOnScreen : Node
{
	[Export] public Node3D? Target { get; set; }

	/// <summary>Camera Target is seen through. Defaults to the viewport's active camera.</summary>
	[Export] public Camera3D? Camera { get; set; }

	private CanvasItem? _parent;

	public override void _Ready()
	{
		_parent = GetParentOrNull<CanvasItem>();
	}

	public override void _Process(double delta)
	{
		var camera = Camera ?? GetViewport().GetCamera3D();
		if (_parent == null || camera == null || !IsInstanceValid(Target)) return;

		var worldPosition = Target!.GlobalPosition;
		_parent.Visible = !camera.IsPositionBehind(worldPosition);
		if (!_parent.Visible) return;

		var screenPosition = camera.UnprojectPositionScaleAdjusted(worldPosition);
		if (_parent is Control control) control.GlobalPosition = screenPosition - control.PivotOffset;
		else if (_parent is Node2D node2D) node2D.GlobalPosition = screenPosition;
	}
}
