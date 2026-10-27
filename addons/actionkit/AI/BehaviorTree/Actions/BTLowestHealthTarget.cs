#nullable enable
using System.Linq;
using ActionKit.Components;
using Godot;

namespace ActionKit.AI;

// Focus-fires whichever entry in BTKeys.Targets has the least health remaining, rather
// than simply the closest one — a common "finish off the weak one first" priority.
// Targets without a HealthComponent (or that aren't Actors at all) are skipped, since
// there's nothing to compare.
[GlobalClass]
public partial class BTLowestHealthTarget : BTNode
{
    protected override BTStatus Tick()
    {
        if (!BTree.TryGet(BTKeys.Targets, out var queryList) || queryList.Count == 0) return BTStatus.Failure;

        var weakest = queryList
            .OfType<Actor>()
            .Select(actor => (actor, health: actor.GetComponent<HealthComponent>()))
            .Where(pair => pair.health != null)
            .OrderBy(pair => pair.health!.Health)
            .FirstOrDefault();

        if (weakest.actor == null) return BTStatus.Failure;

        BTree.Set(BTKeys.CurrentTarget, weakest.actor);
        return BTStatus.Success;
    }
}
