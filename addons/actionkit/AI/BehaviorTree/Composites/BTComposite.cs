#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System;
using Godot;

namespace ActionKit.AI;

// No [GlobalClass] — abstract, never instantiated in the editor.
public abstract partial class BTComposite : BTNode
{
    /// <summary>Status from a child that lets iteration continue to the next one.</summary>
    protected abstract BTStatus ContinueOn { get; }

    /// <summary>Result when every child returned <see cref="ContinueOn"/>.</summary>
    protected virtual BTStatus ExhaustedStatus => ContinueOn;

    /// <summary>Order children are evaluated in. Override to resume, shuffle, filter, prioritize.</summary>
    protected virtual IEnumerable<BTNode> GetTickOrder() => GetNodes();

    /// <summary>Called when a child breaks the chain. Last chance to record state or remap the result.</summary>
    protected virtual BTStatus OnChildBrokeChain(BTNode child, BTStatus status) => status;


    [Export] public bool DebugNodeTicks { get; set; } = false;
    
    protected override BTStatus Tick()
    {
        foreach (var node in GetTickOrder())
        {
            var status = node.Evaluate();
            if(DebugNodeTicks) GD.Print(node.Name, " returned: ", Enum.GetName(typeof(BTStatus), status));
            
            if (status != ContinueOn) return OnChildBrokeChain(node, status);
        }

        return ExhaustedStatus;
    }
}
