#nullable enable
using System.Linq;
using ActionKit.Utilities;
using ActionKit.Weapons;
using Godot;

namespace ActionKit.UI;

/// <summary>
/// Optional root script for a WeaponModeMarker scene. Weapon and Hotkey are set before the
/// marker enters the tree, so they're already there in _Ready; HotkeyLabel (optional) shows
/// the number.
///
/// Dismiss() plays the marker's way out and frees it when that's done. The out-animation is
/// OutTweener (a NodeTweener with AutoTween off); any other NodeTweener on the marker (the
/// in-animation) is stopped first so the two don't fight over the same property. Calling
/// Dismiss again does nothing. Without an OutTweener the marker is freed right away. The
/// tweens live on the marker, so it still finishes leaving if the weapon is destroyed.
/// </summary>
[GlobalClass]
public partial class WeaponMarker : Control
{
	[Export] public NodeTweener? OutTweener { get; set; }
	[Export] public Label? HotkeyLabel { get; set; }

	public Weapon? Weapon { get; set; }

	/// <summary>The number key of the weapon's WeaponPlatform (0 = no key).</summary>
	public int Hotkey { get; set; }

	private bool _dismissed;

	public override void _Ready()
	{
		if (HotkeyLabel != null)
		{
			HotkeyLabel.Text = Hotkey > 0 ? Hotkey.ToString() : "";
			HotkeyLabel.Visible = Hotkey > 0;
		}
	}

	public void Dismiss()
	{
		if (_dismissed) return;
		_dismissed = true;

		if (OutTweener == null)
		{
			QueueFree();
			return;
		}

		foreach (var tweener in GetChildren().OfType<NodeTweener>())
			if (tweener != OutTweener) tweener.Stop();

		OutTweener.Finished += QueueFree;
		OutTweener.ExecuteTween();
	}
}
