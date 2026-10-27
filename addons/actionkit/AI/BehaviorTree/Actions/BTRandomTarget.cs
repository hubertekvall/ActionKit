#nullable enable
using Godot;

namespace ActionKit.AI;

// Picks a uniformly random entry from BTKeys.Targets instead of the closest/farthest/
// weakest — the simplest way to make a group of enemies not all focus-fire the exact
// same target every time.
[GlobalClass]
public partial class BTRandomTarget : BTNode
{
    protected override BTStatus Tick()
    {
        if (!BTree.TryGet(BTKeys.Targets, out var queryList) || queryList.Count == 0) return BTStatus.Failure;

        var pick = queryList[GD.RandRange(0, queryList.Count - 1)];
        BTree.Set(BTKeys.CurrentTarget, pick);
        return BTStatus.Success;
    }
}
