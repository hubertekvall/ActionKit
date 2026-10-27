#nullable enable
using Godot;

namespace ActionKit.AI;

// Watchdog for a child that might otherwise run indefinitely (a chase, an orbit, ...).
// Passes the child's Success/Failure straight through, but forces a Failure if it's
// still Running once Seconds have elapsed since this decorator started ticking it.
[GlobalClass]
public partial class BTTimeLimit : BTDecorator
{
    [Export] public float Seconds { get; set; } = 5.0f;
    private ulong _deadlineMs;
    private bool _running;

    protected override BTStatus Tick()
    {
        if (!_running)
        {
            _deadlineMs = Time.GetTicksMsec() + (ulong)(Seconds * 1000f);
            _running = true;
        }

        var status = DecoratedNode?.Evaluate() ?? BTStatus.Failure;

        if (status == BTStatus.Running && Time.GetTicksMsec() >= _deadlineMs)
        {
            _running = false;
            return BTStatus.Failure;
        }

        if (status != BTStatus.Running) _running = false;
        return status;
    }

    public override void OnAbort()
    {
        _running = false;
    }
}
