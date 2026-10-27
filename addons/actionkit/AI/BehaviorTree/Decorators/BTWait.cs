#nullable enable
using System.Collections.Generic;
using System;
using Godot;

namespace ActionKit.AI;

[GlobalClass]
public partial class BTWait : BTNode
{
	[Export] public float Seconds = 1.0f;
	private ulong _deadlineMs;
	private bool _waiting;
	
	protected override BTStatus Tick()
	{
		if (!_waiting)
		{
			_deadlineMs = Time.GetTicksMsec() + (ulong)(Seconds * 1000f);
			_waiting = true;
		}

		if (Time.GetTicksMsec() < _deadlineMs) return BTStatus.Running;

		
	
		
		
		_waiting = false;
		return BTStatus.Success;
	}
}
