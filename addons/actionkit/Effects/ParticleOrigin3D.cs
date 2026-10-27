#nullable enable
using Godot;

namespace ActionKit.Effects;

/// <summary>
/// A one-shot GPUParticles3D that doesn't vanish when its node is freed. On ready it hands a
/// copy to ParticleEmitterManager, which plays it in the scene following OriginNode3D (the
/// parent by default), and this node frees itself.
/// </summary>
[GlobalClass]
public partial class ParticleOrigin3D : GpuParticles3D
{
	[Export] public Node3D? OriginNode3D { get; set; }

	/// <summary>True on the copy the manager plays, so it doesn't hand itself off again.</summary>
	public bool IsCopy { get; set; }

	public override void _Ready()
	{
		if (Engine.IsEditorHint() || IsCopy) return;
		Callable.From(HandOff).CallDeferred();
	}

	private void HandOff()
	{
		if (!IsInstanceValid(this)) return;

		OriginNode3D ??= GetParentOrNull<Node3D>();
		ParticleEmitterManager.Instance.Emit(this);
		QueueFree();
	}
}
