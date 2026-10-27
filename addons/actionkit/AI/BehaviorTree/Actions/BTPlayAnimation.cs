#nullable enable
using System.Collections.Generic;
using Godot;

namespace ActionKit.AI;

[GlobalClass]
public partial class BTPlayAnimation : BTNode
{
    [Export] public AnimationPlayer? AnimationPlayer;
    [Export] public string AnimationName { get; set; } = "";

    [Export] public float CustomBlend { get; set; }

    protected override BTStatus Tick()
    {
        if (AnimationPlayer == null || !AnimationPlayer.HasAnimation(AnimationName)) return BTStatus.Failure;
        AnimationPlayer.Play(AnimationName, CustomBlend);

        return BTStatus.Success;
    }

}
