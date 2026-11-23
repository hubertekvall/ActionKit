#nullable enable
using System.Collections.Generic;
using ActionKit.Components;
using ActionKit.Utilities;
using Godot;

namespace ActionKit.Weapons;

/// <summary>
/// Goes on the player's actor. Number keys 1-9 cycle the mode (Auto → Manual → Off) of the
/// weapon on the WeaponPlatform with that Hotkey.
/// </summary>
[GlobalClass]
public partial class WeaponHotkeys : ActorComponent
{
	private List<WeaponPlatform> _platforms = [];

	public override void OnActorReady()
	{
		_platforms = ActorOwner.FindDescendantsOfType<WeaponPlatform>();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Enabled || @event is not InputEventKey { Pressed: true, Echo: false } key) return;

		var number = (long)key.PhysicalKeycode - (long)Key.Key0;
		if (number < 1 || number > 9) return;

		foreach (var platform in _platforms)
			if (platform.Hotkey == number)
				platform.Weapon?.CycleMode();
	}
}
