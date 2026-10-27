#nullable enable
using Godot;

namespace ActionKit.AI;

// OwnerActor's own group membership — the complement to IsGroupNearby, which checks
// for other actors nearby. Useful for role/state flags maintained as groups elsewhere
// (e.g. an "Enraged" or "Alerted" group toggled by some other system) without needing
// a dedicated component just to expose a bool to the tree.
[GlobalClass]
public partial class BTIsInGroup : BTNode
{
    [Export] public string GroupName = "";

    protected override BTStatus Tick() =>
        OwnerActor.IsInGroup(GroupName) ? BTStatus.Success : BTStatus.Failure;
}
