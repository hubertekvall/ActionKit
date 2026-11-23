#nullable enable
using ActionKit.Weapons;
using Godot;

namespace ActionKit.AI;

/// <summary>
/// Points every weapon under the actor (Actor.Weapons) at BTKeys.CurrentTarget, so the tree
/// makes the targeting decision once for the whole actor. Don't give these weapons an
/// AutoTargeting component, or it will override the tree's choice. Weapons in Manual or Off
/// mode are left alone.
/// </summary>
[GlobalClass]
public partial class BTAimWeaponsAtTarget : BTNode
{
	protected override BTStatus Tick()
	{
		if (OwnerActor.Weapons.Count == 0) return BTStatus.Failure;
		if (!BTree.TryGet(BTKeys.CurrentTarget, out var target) || !IsInstanceValid(target)) return BTStatus.Failure;

		foreach (var weapon in OwnerActor.Weapons)
		{
			if (weapon.Mode == Weapon.WeaponMode.Auto)
				weapon.Target = target;
		}

		return BTStatus.Success;
	}
}
