#nullable enable
using Godot;

namespace ActionKit.AI;

// The mirror of Succeeder: always evaluates the child (so its side effects still
// happen) but reports Failure regardless of what it returned. Useful for a branch
// that should run purely for effect — play a sound, log something — without ever
// letting its result end a Sequence in success, or to temporarily disable a branch
// of a Selector without deleting it.
[GlobalClass]
public partial class BTFailer : BTDecorator
{
    protected override BTStatus Tick()
    {
        DecoratedNode?.Evaluate();
        return BTStatus.Failure;
    }
}
