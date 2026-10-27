#nullable enable
using System.Collections.Generic;
using Godot;

namespace ActionKit.Effects;

/// <summary>
/// Plays copies of ParticleOrigin3D effects in the current scene so they finish even after
/// their source is freed, keeping each copy glued to its origin while the origin exists.
/// Creates itself under the scene root on first use; you can also register it as an autoload.
/// </summary>
public partial class ParticleEmitterManager : Node
{
	private static ParticleEmitterManager? _instance;

	public static ParticleEmitterManager Instance
	{
		get
		{
			if (_instance != null && IsInstanceValid(_instance)) return _instance;

			_instance = new ParticleEmitterManager { Name = nameof(ParticleEmitterManager) };
			if (Engine.GetMainLoop() is SceneTree tree)
				tree.Root.CallDeferred(Node.MethodName.AddChild, _instance);
			return _instance;
		}
	}

	private readonly Dictionary<GpuParticles3D, Node3D?> _emitters = [];

	public override void _EnterTree()
	{
		if (_instance == null || !IsInstanceValid(_instance)) _instance = this;
	}

	public override void _ExitTree()
	{
		if (_instance == this) _instance = null;
	}

	public void Emit(ParticleOrigin3D template)
	{
		if (template.Duplicate() is not ParticleOrigin3D emitter) return;
		emitter.IsCopy = true;

		var parent = template.GetTree().CurrentScene ?? template.GetTree().Root;
		parent.AddChild(emitter);
		emitter.GlobalTransform = template.GlobalTransform;
		emitter.Finished += () => FreeEmitter(emitter);
		emitter.OneShot = true;
		emitter.Emitting = true;

		_emitters[emitter] = template.OriginNode3D;
	}

	private void FreeEmitter(GpuParticles3D emitter)
	{
		_emitters.Remove(emitter);
		if (IsInstanceValid(emitter)) emitter.QueueFree();
	}

	public override void _PhysicsProcess(double delta)
	{
		foreach (var (emitter, origin) in _emitters)
		{
			if (!IsInstanceValid(origin) || !IsInstanceValid(emitter) || !emitter.IsInsideTree()) continue;
			emitter.GlobalTransform = origin!.GlobalTransform;
		}
	}
}
