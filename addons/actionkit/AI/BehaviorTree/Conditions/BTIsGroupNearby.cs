#nullable enable
using ActionKit.Utilities;
using System.Linq;
using Godot;

namespace ActionKit.AI;

[GlobalClass]
public partial class BTIsGroupNearby : BTNode
{
    [Export] public float Radius = 10.0f;
    [Export] public string GroupName = "";
    protected override BTStatus Tick()
    {
        var query = QueryList.QueryRadius(OwnerActor, Radius).Where(message => message.IsInGroup(GroupName));
        return query.Any() ? BTStatus.Success : BTStatus.Failure;
    }
}
