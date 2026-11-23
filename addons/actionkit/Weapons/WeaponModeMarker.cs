#nullable enable
using ActionKit.Components;
using ActionKit.UI;
using ActionKit.Utilities;
using Godot;

namespace ActionKit.Weapons;

/// <summary>
/// Put this under a Weapon. Shows an on-screen marker pinned to the weapon for its current
/// mode: a fresh instance of that mode's scene is added to the HUD and follows the weapon
/// (FollowNode3DOnScreen), and is dismissed when the mode changes. A mode with no scene shows
/// nothing (e.g. set only ManualMarkerScene to mark weapons in Manual mode).
///
/// Markers go under MarkerParent, or else the first node in MarkerParentGroup (handy when the
/// HUD lives in another scene). Set each marker's PivotOffset to the point that should sit on
/// the weapon. If a marker's root is a WeaponMarker it gets the weapon and its platform's
/// Hotkey, and animates out through WeaponMarker.Dismiss().
/// </summary>
[GlobalClass]
public partial class WeaponModeMarker : ActorComponent
{
	[Export] public PackedScene? AutoMarkerScene { get; set; }
	[Export] public PackedScene? ManualMarkerScene { get; set; }
	[Export] public PackedScene? OffMarkerScene { get; set; }

	[ExportGroup("Placement")]
	[Export] public Control? MarkerParent { get; set; }
	[Export] public StringName MarkerParentGroup { get; set; } = "";

	/// <summary>Camera the weapon is seen through. Defaults to the viewport's active camera.</summary>
	[Export] public Camera3D? Camera { get; set; }

	private Weapon? _weapon;
	private Control? _marker;

	public override void OnActorReady()
	{
		_weapon = ActorOwner as Weapon;
		if (_weapon == null)
		{
			GD.PushWarning($"{Name}: WeaponModeMarker must be a direct child of a Weapon.");
			return;
		}

		_weapon.ModeChanged += OnModeChanged;
		// Deferred: the HUD may still be setting up its children.
		Callable.From(() => OnModeChanged(_weapon.Mode)).CallDeferred();
	}

	public override void OnActorDetached()
	{
		if (IsInstanceValid(_weapon)) _weapon!.ModeChanged -= OnModeChanged;
		HideMarker();
	}

	public override void _ExitTree() => HideMarker();

	private void OnModeChanged(Weapon.WeaponMode mode)
	{
		HideMarker();
		if (_weapon == null || !IsInsideTree()) return;

		var scene = mode switch
		{
			Weapon.WeaponMode.Auto => AutoMarkerScene,
			Weapon.WeaponMode.Manual => ManualMarkerScene,
			_ => OffMarkerScene,
		};
		var parent = ResolveParent();
		if (scene == null || parent == null) return;

		_marker = scene.Instantiate<Control>();
		if (_marker is WeaponMarker weaponMarker)
		{
			weaponMarker.Weapon = _weapon;
			weaponMarker.Hotkey = _weapon.GetParentOrNull<WeaponPlatform>()?.Hotkey ?? 0;
		}

		_marker.AddChild(new FollowNode3DOnScreen { Target = _weapon.Body ?? _weapon, Camera = Camera });
		parent.AddChild(_marker);
	}

	private Node? ResolveParent()
	{
		if (IsInstanceValid(MarkerParent)) return MarkerParent;
		return MarkerParentGroup.IsEmpty ? null : GetTree().GetFirstNodeInGroup(MarkerParentGroup);
	}

	// The old marker plays its own out-animation and frees itself, so it finishes even if
	// this component goes away. _marker is cleared right away, so a quick mode change never
	// dismisses the same marker twice or leaves two live markers.
	private void HideMarker()
	{
		if (_marker is WeaponMarker weaponMarker && IsInstanceValid(weaponMarker)) weaponMarker.Dismiss();
		else if (IsInstanceValid(_marker)) _marker!.QueueFree();
		_marker = null;
	}
}
