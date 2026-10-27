#nullable enable
using ActionKit.Utilities;
using System.Linq;
using Godot;

namespace ActionKit.AI;

[GlobalClass]
public partial class BTFindTargetsWithinRadius : BTNode
{
	[Export] public float Radius { get; set; }



	protected override BTStatus Tick()
	{

		var queryList = QueryList.QueryRadius(OwnerActor, Radius);
		BTree.Set(BTKeys.Targets, queryList);


		return queryList.Any() ? BTStatus.Success : BTStatus.Failure;
	}
}
