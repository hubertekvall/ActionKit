#nullable enable
using ActionKit.Utilities;
using Godot.Collections;
using Godot;

namespace ActionKit.AI;

// Line-of-sight check: succeeds only if nothing solid sits between OwnerActor and
// BTKeys.CurrentTarget. Lets a tree gate "should I even try to engage" on whether the
// target is actually visible rather than just in range — a target hiding behind a hull
// or terrain won't get shot at through it.
[GlobalClass]
public partial class BTIsTargetVisible : BTNode
{
    [Export] public uint CollisionMask { get; set; } = 1;

    protected override BTStatus Tick()
    {
        if (!BTree.TryGet(BTKeys.CurrentTarget, out var target) || !IsInstanceValid(target))
            return BTStatus.Failure;

        var spaceState = OwnerActor.GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(OwnerActor.GlobalPosition, target.GlobalPosition, CollisionMask);

        if (OwnerActor.ActorBody is CollisionObject3D body)
            query.Exclude = new Array<Rid> { body.GetRid() };

        var result = spaceState.IntersectRay(query);
        if (result.Count == 0) return BTStatus.Success;

        // Resolve the hit back to its owning Actor the same way QueryList/TargetFinder
        // do, so hitting the target's hull collider still counts as "seeing the target"
        // rather than "seeing something in the way".
        var collider = result["collider"].As<Node>();
        var hit = collider?.GetParent() is Actor hitActor ? (Node3D)hitActor : collider;

        return hit == target ? BTStatus.Success : BTStatus.Failure;
    }
}
