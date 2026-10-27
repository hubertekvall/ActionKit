#nullable enable
using ActionKit.Utilities;
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Moves Effects (sounds, particles, ...) out of the actor into the current scene and keeps
/// it glued to the spot it used to sit at. When the actor leaves the tree, Effects stays
/// behind: looping particles stop emitting so they trail off, everything already playing
/// finishes, and Effects is freed after FreeDelay seconds.
/// </summary>
[GlobalClass]
public partial class DetachedEffects : ActorComponent
{
	[Export] public Node3D? Effects { get; set; }
	[Export] public float FreeDelay { get; set; } = 5.0f;

	private Node3D _anchor = null!;
	private Transform3D _offset;

	public override void OnActorReady()
	{
		if (Effects == null) return;

		// Follow whatever Effects was attached to (actor, body, model pivot...).
		_anchor = Effects.GetParentNode3D() ?? ActorOwner;
		_offset = _anchor.GlobalTransform.AffineInverse() * Effects.GlobalTransform;

		// Deferred: the actor is still setting up its children here.
		Callable.From(Detach).CallDeferred();
	}

	private void Detach()
	{
		if (!IsInstanceValid(Effects)) return;
		Effects!.Reparent(GetTree().CurrentScene);
	}

	public override void ProcessTick(double delta)
	{
		if (!IsInstanceValid(Effects) || !IsInstanceValid(_anchor)) return;
		Effects!.GlobalTransform = _anchor.GlobalTransform * _offset;
	}

	public override void _ExitTree()
	{
		if (!IsInstanceValid(Effects) || Effects!.IsQueuedForDeletion()) return;

		var effects = Effects;
		foreach (var particles in effects.FindDescendantsOfType<GpuParticles3D>())
			if (!particles.OneShot) particles.Emitting = false;
		if (effects is GpuParticles3D { OneShot: false } self) self.Emitting = false;

		effects.GetTree().CreateTimer(FreeDelay).Timeout += () =>
		{
			if (IsInstanceValid(effects)) effects.QueueFree();
		};
	}
}
