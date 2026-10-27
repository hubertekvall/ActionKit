#nullable enable
using System.Linq;
using Godot;

namespace ActionKit.AI;

public abstract partial class BTDecorator : BTNode
{
    protected BTNode? DecoratedNode;
    public override void _Ready()
    {
        base._Ready();
        DecoratedNode = GetNodes().FirstOrDefault();
    }
}
