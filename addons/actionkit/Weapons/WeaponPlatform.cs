#nullable enable
using ActionKit.Utilities;
using Godot;

namespace ActionKit.Weapons;

/// <summary>
/// A slot a weapon sits in. Place it where the weapon should go and put the weapon scene
/// under it as a child; SetWeapon swaps that child at runtime (loadout screens, pickups).
/// SortOrder is free for UI to sort weapon displays by.
/// </summary>
[GlobalClass]
public partial class WeaponPlatform : Node3D
{
	[Export] public int SortOrder { get; set; }

	public Weapon? Weapon { get; private set; }

	public override void _Ready()
	{
		foreach (var child in GetChildren())
			if (child is Weapon weapon)
				Weapon = weapon;
	}

	/// <summary>Frees the current weapon and mounts the new one (null leaves the platform empty).</summary>
	public void SetWeapon(Weapon? weapon)
	{
		var actor = this.FindAncestorOfType<Actor>();

		if (Weapon != null)
		{
			actor?.Weapons.Remove(Weapon);
			RemoveChild(Weapon);
			Weapon.QueueFree();
		}

		Weapon = weapon;
		if (weapon == null) return;

		weapon.Spawner ??= actor;
		AddChild(weapon);
		actor?.Weapons.Add(weapon);
	}
}
