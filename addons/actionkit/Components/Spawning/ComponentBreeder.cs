#nullable enable
using System;
using Godot;

namespace ActionKit.Components;

[GlobalClass]
public partial class ComponentBreeder : ActorComponent
{
  
	
   
	
	
	
	public void AttachComponents()
	{
		foreach (var child in GetChildren())
		{
			if (child is ActorComponent component)
			{
				ActorOwner.AddComponent((ActorComponent)component.Duplicate());
			}
		  
		}
	}
}
