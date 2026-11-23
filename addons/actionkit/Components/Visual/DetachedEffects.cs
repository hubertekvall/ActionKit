#nullable enable
using ActionKit.Utilities;
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Moves Effects (sounds, particles, ...) out of the actor into the current scene and keeps
/// it glued to the spot it used to sit at. When the actor leaves the tree, Effects stays
/// behind: particles stop emitting, everything already playing finishes on its own, and
/// Effects frees itself once the last particle or sound is done. Looping sounds never
/// finish, so they don't hold it up; they go when Effects goes.
/// </summary>
[GlobalClass]
public partial class DetachedEffects : ActorComponent
{
	[Export] public Node3D? Effects { get; set; }

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
		var nodes = effects.FindDescendantsOfType<Node>();
		nodes.Add(effects);

		// Starts at 1 so nothing can free Effects while we're still hooking things up;
		// the OnFinished() call at the end releases that hold.
		var remaining = 1;
		void OnFinished()
		{
			remaining--;
			if (remaining == 0 && IsInstanceValid(effects)) effects.QueueFree();
		}

		foreach (var node in nodes)
		{
			switch (node)
			{
				case GpuParticles3D { Emitting: true } particles:
					remaining++;
					particles.Finished += OnFinished;
					particles.Emitting = false;
					break;
				case CpuParticles3D { Emitting: true } particles:
					remaining++;
					particles.Finished += OnFinished;
					particles.Emitting = false;
					break;
				case AudioStreamPlayer3D { Playing: true } audio when !IsLooping(audio.Stream):
					remaining++;
					audio.Finished += OnFinished;
					break;
			}
		}

		OnFinished();
	}

	private static bool IsLooping(AudioStream? stream) => stream switch
	{
		AudioStreamWav wav => wav.LoopMode != AudioStreamWav.LoopModeEnum.Disabled,
		AudioStreamOggVorbis ogg => ogg.Loop,
		AudioStreamMP3 mp3 => mp3.Loop,
		_ => false,
	};
}
