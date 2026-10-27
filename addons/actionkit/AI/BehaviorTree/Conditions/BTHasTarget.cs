#nullable enable
using Godot;

namespace ActionKit.AI;

// Cheap guard for anything downstream that needs BTKeys.CurrentTarget to exist and
// still be alive — lets a Sequence bail out in one node instead of every target-reading
// action re-deriving the same null/freed check.
[GlobalClass]
public partial class BTHasTarget : BTNode
{
    protected override BTStatus Tick() =>
        BTree.TryGet(BTKeys.CurrentTarget, out var target) && IsInstanceValid(target)
            ? BTStatus.Success
            : BTStatus.Failure;
}
