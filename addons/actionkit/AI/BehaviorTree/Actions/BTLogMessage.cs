#nullable enable
using Godot;

namespace ActionKit.AI;

// A no-op leaf that just prints — drop it in a Sequence/Selector branch while building
// or debugging a tree to see when that branch actually gets reached, without needing
// DebugNodeTicks on every composite above it.
[GlobalClass]
public partial class BTLogMessage : BTNode
{
    [Export] public string Message { get; set; } = "";

    protected override BTStatus Tick()
    {
        GD.Print($"{OwnerActor?.Name}: {Message}");
        return BTStatus.Success;
    }
}
