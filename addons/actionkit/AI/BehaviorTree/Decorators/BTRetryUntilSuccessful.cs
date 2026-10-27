#nullable enable
using Godot;

namespace ActionKit.AI;

// Keeps re-running the child on Failure — Running in between — until it either
// succeeds (reported immediately) or MaxAttempts failed attempts are used up
// (reported as Failure). MaxAttempts <= 0 retries forever.
[GlobalClass]
public partial class BTRetryUntilSuccessful : BTDecorator
{
    [Export] public int MaxAttempts { get; set; } = 0;
    private int _attempts;

    protected override BTStatus Tick()
    {
        var status = DecoratedNode?.Evaluate() ?? BTStatus.Failure;

        if (status == BTStatus.Success)
        {
            _attempts = 0;
            return BTStatus.Success;
        }

        if (status == BTStatus.Running) return BTStatus.Running;

        _attempts++;
        if (MaxAttempts > 0 && _attempts >= MaxAttempts)
        {
            _attempts = 0;
            return BTStatus.Failure;
        }

        return BTStatus.Running;
    }

    public override void OnAbort()
    {
        _attempts = 0;
    }
}
