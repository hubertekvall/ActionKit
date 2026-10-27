#nullable enable
using Godot;

namespace ActionKit.Components;

[GlobalClass]
public partial class VelocityScalingMeshes : ActorComponent
{
	
	
	[Export] public MeshInstance3D[] ScaledMeshes { get; set; } = [];
	[Export(PropertyHint.Range, "0, 1")] public float BaseScale { get; set; } = 1.5f;
	[Export(PropertyHint.Range, "1, 10")] public float ScaleMultiplier { get; set; } = 1.5f;

	[Export] public float MaxSpeed { get; set; } = 40f;

	public override void ProcessTick(double delta)
	{
		if (ActorOwner?.ActorBody == null) return;
		
		var normalizedSpeed = ActorOwner.ActorBody.LinearVelocity.LimitLength(MaxSpeed).Length() / MaxSpeed;
		var scaleFactor = normalizedSpeed * ScaleMultiplier;
		foreach (var scaledMesh in ScaledMeshes)
		{
			
			
			scaledMesh.Scale = new Vector3(1, 1, BaseScale * scaleFactor);
		}
	}
}
