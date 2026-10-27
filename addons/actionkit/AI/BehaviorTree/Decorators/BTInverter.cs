#nullable enable
using System.Collections.Generic;
using Godot;

namespace ActionKit.AI;

[GlobalClass]
public partial class BTInverter : BTDecorator
{
    protected override BTStatus Tick()
    {
        var result = DecoratedNode?.Evaluate() ?? BTStatus.Failure;

        return result switch
        {
            BTStatus.Failure => BTStatus.Success,
            BTStatus.Success => BTStatus.Failure,
            _ => BTStatus.Running
        };
    }
}
