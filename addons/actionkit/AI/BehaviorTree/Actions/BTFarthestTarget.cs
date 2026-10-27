#nullable enable
using System.Linq;
using Godot;

namespace ActionKit.AI;

// The mirror of ClosestTarget — picks the farthest entry in BTKeys.Targets instead of
// the nearest. Useful for spreading attention across a spread-out group, or for a
// support-style actor moving towards whichever ally is farthest away.
[GlobalClass]
public partial class BTFarthestTarget : BTNode
{
    protected override BTStatus Tick()
    {
        if (OwnerActor is null) return BTStatus.Failure;
        if (!BTree.TryGet(BTKeys.Targets, out var queryList) || queryList.Count == 0) return BTStatus.Failure;

        var farthest = queryList.OrderByDescending(t => t.GlobalPosition.DistanceTo(OwnerActor.GlobalPosition)).First();
        BTree.Set(BTKeys.CurrentTarget, farthest);
        return BTStatus.Success;
    }
}
