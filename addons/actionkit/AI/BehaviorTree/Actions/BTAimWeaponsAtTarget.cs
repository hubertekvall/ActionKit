#nullable enable
using Godot;

namespace ActionKit.AI;

/// <summary>
/// Points every weapon under the actor (Actor.Weapons) at BTKeys.CurrentTarget, so the tree
/// makes the targeting decision once for the whole actor. Turns the weapons' own AutoTarget
/// off so they don't override it.
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
			weapon.AutoTarget = false;
			weapon.Target = target;
		}

		return BTStatus.Success;
	}
}
