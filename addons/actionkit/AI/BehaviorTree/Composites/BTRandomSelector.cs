#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ActionKit.AI;

// A Selector whose children are shuffled before each tick instead of evaluated in a
// fixed order — same short-circuit-on-first-non-Failure semantics as Selector, but
// which branch "wins" varies run to run. Use it wherever a fixed priority order would
// make an enemy feel too predictable (e.g. picking between a few equally valid attack
// patterns).
[GlobalClass]
public partial class BTRandomSelector : BTSelector
{
    protected override IEnumerable<BTNode> GetTickOrder() =>
        base.GetTickOrder().OrderBy(_ => GD.Randf());
}
