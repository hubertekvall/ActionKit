#nullable enable
using ActionKit.Utilities;
using System.Linq;
using Godot;

namespace ActionKit.AI;

[GlobalClass]
public partial class BTFilterTargetsByGroup : BTNode
{
	[Export] public string GroupName = "";



	protected override BTStatus Tick()
	{
		if (BTree.TryGet(BTKeys.Targets, out var queryList))
		{

			var byGroup = queryList.Where(message => message.IsInGroup(GroupName)).ToQueryList();
			BTree.Set(BTKeys.Targets, byGroup);
			return byGroup.Any() ? BTStatus.Success : BTStatus.Failure;
		}

		return BTStatus.Failure;
	}
}
