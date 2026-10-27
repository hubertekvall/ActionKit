#nullable enable
using System.Linq;
using Godot;

namespace ActionKit.AI;

[GlobalClass]
public partial class BTClosestTarget : BTNode
{
	protected override BTStatus Tick()
	{
		if(OwnerActor is null) return BTStatus.Failure;

		if (BTree.TryGet(BTKeys.Targets, out var queryList) && queryList.Count > 0)
		{

			var byDistance = queryList.OrderBy(message => message.GlobalPosition.DistanceTo(OwnerActor.GlobalPosition));
			BTree.Set(BTKeys.CurrentTarget, byDistance.First());
			return BTStatus.Success;
		}


		return BTStatus.Failure;
	}
}
