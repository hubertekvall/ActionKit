#nullable enable
using System.Collections.Generic;
using Godot;

namespace ActionKit.UI;

/// <summary>
/// Shows and hides cursor scenes. Turning a scene on instances a fresh copy as a child, so its
/// animations and state start over each time; turning it off frees that copy. Several can be
/// on at once.
/// - A Node3D cursor (e.g. a Decal) is moved to the world point passed to Set.
/// - A Control cursor follows the mouse, with its PivotOffset on the mouse point (the hotspot).
///   The OS cursor is hidden while any Control cursor is on.
/// Put one in a scene (under a CanvasLayer for Control cursors); other code reaches it through Instance.
/// </summary>
[GlobalClass]
public partial class CursorSwitcher : Node
{
	public static CursorSwitcher? Instance { get; private set; }

	private readonly Dictionary<PackedScene, Node> _shown = [];
	private bool _hidingMouse;

	public override void _EnterTree() => Instance = this;

	public override void _Process(double delta)
	{
		var anyControl = false;
		foreach (var cursor in _shown.Values)
		{
			if (cursor is not Control control) continue;
			FollowMouse(control);
			anyControl = true;
		}

		if (anyControl != _hidingMouse) HideMouse(anyControl);
	}

	public override void _ExitTree()
	{
		if (_hidingMouse) HideMouse(false);
		if (Instance == this) Instance = null;
	}

	/// <summary>
	/// Turns a cursor scene on or off. Calling it again while on keeps the same copy (a 3D
	/// cursor is just moved to <paramref name="at"/>).
	/// </summary>
	public void Set(PackedScene? scene, bool on, Vector3 at = default)
	{
		if (scene == null) return;
		_shown.TryGetValue(scene, out var cursor);

		if (!on)
		{
			if (cursor == null) return;
			cursor.QueueFree();
			_shown.Remove(scene);
			return;
		}

		if (cursor == null)
		{
			cursor = scene.Instantiate();
			AddChild(cursor);
			_shown[scene] = cursor;
			if (cursor is Control control) FollowMouse(control);
		}

		if (cursor is Node3D node3D) node3D.GlobalPosition = at;
	}

	private static void FollowMouse(Control control) =>
		control.GlobalPosition = control.GetViewport().GetMousePosition() - control.PivotOffset;

	private void HideMouse(bool hide)
	{
		_hidingMouse = hide;
		Input.MouseMode = hide ? Input.MouseModeEnum.Hidden : Input.MouseModeEnum.Visible;
	}
}
