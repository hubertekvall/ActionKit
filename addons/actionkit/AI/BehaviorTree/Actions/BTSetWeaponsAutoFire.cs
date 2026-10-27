#nullable enable
using Godot;

namespace ActionKit.AI;

// Toggles AutoFire on every weapon under OwnerActor (Actor.Weapons) at once — lets a
// tree hold weapons fire during a stealth-approach or reload-adjacent phase, then free
// them once some other condition (in range, facing, visible, ...) is met.
[GlobalClass]
public partial class BTSetWeaponsAutoFire : BTNode
{
    [Export] public bool AutoFire { get; set; } = true;

    protected override BTStatus Tick()
    {
        if (OwnerActor is null || OwnerActor.Weapons.Count == 0) return BTStatus.Failure;

        foreach (var weapon in OwnerActor.Weapons)
            weapon.AutoFire = AutoFire;

        return BTStatus.Success;
    }
}
