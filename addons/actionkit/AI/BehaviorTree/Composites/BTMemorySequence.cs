#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ActionKit.AI;

[GlobalClass]
public partial class BTMemorySequence : BTSequence
{
    // Reference, not an index — survives reordering/insertion in the editor.
    private BTNode? _resumeFrom;

    protected override IEnumerable<BTNode> GetTickOrder()
    {
        var nodes = base.GetTickOrder();

        // Stale reference: child was removed, freed, or reparented since last tick.
        if (_resumeFrom is null || !IsInstanceValid(_resumeFrom) || !nodes.Contains(_resumeFrom))
        {
            _resumeFrom = null;
            return nodes;
        }

        return nodes.SkipWhile(node => node != _resumeFrom);
    }

    protected override BTStatus OnChildBrokeChain(BTNode child, BTStatus status)
    {
        if (status == BTStatus.Running) _resumeFrom = child;
        return status;
    }

    protected override BTStatus Tick()
    {
        var status = base.Tick();
        if (status != BTStatus.Running) _resumeFrom = null;  // Success or Failure ends the run
        return status;
    }

    // Pre-empted from above — drop the resume point and let the running leaf clean up.
    // Rename to whatever your BTNode lifecycle hook is called.
    public override void OnAbort()
    {
        _resumeFrom = null;
    }
}
