#nullable enable
using Godot;

namespace ActionKit.Components;

[GlobalClass]
public partial class HurtMesh : ActorComponent
{
	private Timer _hurtTimer = new();
	[Export] public double OverlayTime { get; set; } = 0.25;
	[Export] public MeshInstance3D[] MeshInstances { get; set; } = [];
	[Export] public Material? HurtOverlayMaterial { get; set; }

	public override void OnActorReady()
	{
		AddChild(_hurtTimer);
		_hurtTimer.WaitTime = OverlayTime;
		_hurtTimer.Connect(Timer.SignalName.Timeout, Callable.From(ResetMeshes));
		ResetMeshes();
	}
	public override void OnMessage(ActorMessage message)
	{
		if (message is not DamageMessage damageMessage) return;
		
		_hurtTimer.Start();
		
		foreach (var meshInstance in MeshInstances)
		{
			
			if(!IsInstanceValid(meshInstance)) continue;
			meshInstance.Visible = true;
			meshInstance.MaterialOverlay = HurtOverlayMaterial;
		}
	}

	private void ResetMeshes()
	{
				
		foreach (var meshInstance in MeshInstances)
		{
			if(!IsInstanceValid(meshInstance)) continue;
			meshInstance.Visible = false;
			meshInstance.MaterialOverlay = null;

		}
	}
}
