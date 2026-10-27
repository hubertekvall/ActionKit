#nullable enable
using Godot;

namespace ActionKit.AI;

// Gates the child behind a cooldown: once it finishes (Success or Failure), this
// decorator reports Failure on its own — without even evaluating the child — until
// Seconds have passed. Good for "special attack every N seconds" pacing without
// needing a separate timer actor/component just to remember when it last fired.
[GlobalClass]
public partial class BTCooldown : BTDecorator
{
    [Export] public float Seconds { get; set; } = 1.0f;
    private ulong _readyAtMs;

    protected override BTStatus Tick()
    {
        if (Time.GetTicksMsec() < _readyAtMs) return BTStatus.Failure;

        var status = DecoratedNode?.Evaluate() ?? BTStatus.Failure;
        if (status != BTStatus.Running)
            _readyAtMs = Time.GetTicksMsec() + (ulong)(Seconds * 1000f);

        return status;
    }
}
