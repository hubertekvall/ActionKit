#nullable enable
using Godot;

namespace ActionKit.AI;

[GlobalClass]
public partial class BTSelector : BTComposite
{
	protected override BTStatus ContinueOn => BTStatus.Failure;
}
