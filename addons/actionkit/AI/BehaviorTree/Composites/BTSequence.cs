#nullable enable
using Godot;

namespace ActionKit.AI;

[GlobalClass]
public partial class BTSequence : BTComposite
{
	
	// Reactive: restarts from the top each tick, so early conditions are re-checked.
	protected override BTStatus ContinueOn => BTStatus.Success;
}
