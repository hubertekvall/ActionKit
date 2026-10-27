#nullable enable
using Godot;

namespace ActionKit.AI;

// Re-runs the child Count times (or forever, if Count <= 0) before finally reporting
// Success — Running in between. With StopOnFailure on (the default) a single Failure
// from the child ends the whole repeat early as a Failure, rather than being counted
// as one more completed rep.
[GlobalClass]
public partial class BTRepeater : BTDecorator
{
    [Export] public int Count { get; set; } = 0;
    [Export] public bool StopOnFailure { get; set; } = true;
    private int _completed;

    protected override BTStatus Tick()
    {
        var status = DecoratedNode?.Evaluate() ?? BTStatus.Failure;
        if (status == BTStatus.Running) return BTStatus.Running;

        if (StopOnFailure && status == BTStatus.Failure)
        {
            _completed = 0;
            return BTStatus.Failure;
        }

        _completed++;
        if (Count > 0 && _completed >= Count)
        {
            _completed = 0;
            return BTStatus.Success;
        }

        return BTStatus.Running;
    }

    public override void OnAbort()
    {
        _completed = 0;
    }
}
