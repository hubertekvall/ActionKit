#nullable enable
using Godot;

namespace ActionKit.AI;

// Ticks every child every time, regardless of what the others return — unlike
// BTComposite's Sequence/Selector, which stop at the first child that breaks their
// chain. Use it for things that genuinely need to happen together every frame (turn
// towards a target while also firing at it, orbit while also playing a hum animation),
// instead of faking simultaneity by relying on tick order.
[GlobalClass]
public partial class BTParallel : BTNode
{
    public enum Policy { RequireAll, RequireOne }

    // Defaults mirror running a Sequence's children in parallel: everything has to
    // succeed, and any single failure fails the whole thing — except nothing is
    // "early" here, since every child already got ticked this frame either way.
    [Export] public Policy SuccessPolicy { get; set; } = Policy.RequireAll;
    [Export] public Policy FailurePolicy { get; set; } = Policy.RequireOne;

    protected override BTStatus Tick()
    {
        var successes = 0;
        var failures = 0;
        var total = 0;

        foreach (var child in GetNodes())
        {
            total++;
            switch (child.Evaluate())
            {
                case BTStatus.Success: successes++; break;
                case BTStatus.Failure: failures++; break;
            }
        }

        if (total == 0) return BTStatus.Failure;

        var failed = FailurePolicy == Policy.RequireOne ? failures > 0 : failures == total;
        if (failed) return BTStatus.Failure;

        var succeeded = SuccessPolicy == Policy.RequireOne ? successes > 0 : successes == total;
        return succeeded ? BTStatus.Success : BTStatus.Running;
    }
}
