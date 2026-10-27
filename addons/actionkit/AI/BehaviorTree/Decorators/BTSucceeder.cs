#nullable enable
using Godot;

namespace ActionKit.AI;

[GlobalClass]
public partial class BTSucceeder : BTDecorator
{
    protected override BTStatus Tick()
    {
        DecoratedNode?.Evaluate();
        return BTStatus.Success;
    }
}
