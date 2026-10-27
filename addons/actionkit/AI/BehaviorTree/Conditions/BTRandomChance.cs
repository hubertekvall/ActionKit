#nullable enable
using Godot;

namespace ActionKit.AI;

// Succeeds with probability Chance, re-rolled every tick. Drop it in front of a branch
// in a Sequence to make an enemy only sometimes take that path — e.g. an occasional
// taunt, or a random pick between two otherwise-equal attack options via two branches
// each gated by a RandomChance.
[GlobalClass]
public partial class BTRandomChance : BTNode
{
    [Export(PropertyHint.Range, "0, 1")] public float Chance { get; set; } = 0.5f;

    protected override BTStatus Tick() =>
        GD.Randf() < Chance ? BTStatus.Success : BTStatus.Failure;
}
