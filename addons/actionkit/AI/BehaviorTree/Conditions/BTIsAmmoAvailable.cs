#nullable enable
using System.Linq;
using Godot;

namespace ActionKit.AI;

// Succeeds if any Weapon anywhere under OwnerActor currently has ammo left. Reads
// straight off Actor.Weapons — like IsGroupNearby reading OwnerActor directly — rather
// than needing a manually-wired reference to each weapon, so it keeps working as
// weapons are added, removed, or renested under mount pivots (HardpointWeapon, turrets,
// ...) without this node ever needing to change.
[GlobalClass]
public partial class BTIsAmmoAvailable : BTNode
{
	protected override BTStatus Tick()
	{
		if (OwnerActor == null) return BTStatus.Failure;
		return OwnerActor.Weapons.Any(weapon => weapon.Ammo > 0) ? BTStatus.Success : BTStatus.Failure;
	}
}
