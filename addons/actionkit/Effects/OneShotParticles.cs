#nullable enable
using Godot;

namespace ActionKit.Effects;

/// <summary>GPU particles that fire once as soon as they enter the scene.</summary>
[GlobalClass]
public partial class OneShotGpuParticles3D : GpuParticles3D
{
	public override void _Ready()
	{
		if (Engine.IsEditorHint()) return;
		OneShot = true;
		Restart();
	}
}

/// <summary>CPU particles that fire once as soon as they enter the scene.</summary>
[GlobalClass]
public partial class OneShotCpuParticles3D : CpuParticles3D
{
	public override void _Ready()
	{
		if (Engine.IsEditorHint()) return;
		OneShot = true;
		Emitting = true;
	}
}
