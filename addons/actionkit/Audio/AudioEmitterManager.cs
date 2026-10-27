#nullable enable
using System.Collections.Generic;
using Godot;

namespace ActionKit.Audio;

/// <summary>
/// Plays copies of AudioStreamOrigin3D sounds that outlive their source node, and keeps each
/// copy following its source while the source exists. Creates itself under the scene root on
/// first use; you can also register it as an autoload.
/// </summary>
public partial class AudioEmitterManager : Node
{
	private static AudioEmitterManager? _instance;

	public static AudioEmitterManager Instance
	{
		get
		{
			if (_instance != null && IsInstanceValid(_instance)) return _instance;

			_instance = new AudioEmitterManager { Name = nameof(AudioEmitterManager) };
			if (Engine.GetMainLoop() is SceneTree tree)
				tree.Root.CallDeferred(Node.MethodName.AddChild, _instance);
			return _instance;
		}
	}

	private readonly Dictionary<AudioStreamPlayer3D, Node3D?> _emitters = [];

	public override void _EnterTree()
	{
		if (_instance == null || !IsInstanceValid(_instance)) _instance = this;
	}

	public override void _ExitTree()
	{
		if (_instance == this) _instance = null;
	}

	/// <summary>Plays a copy of the template's sound at its position, following template.FollowTarget.</summary>
	public static void Play3D(AudioStreamOrigin3D template)
	{
		var parent = template.GetTree().CurrentScene ?? template.GetTree().Root;
		var emitter = new AudioStreamPlayer3D
		{
			Stream = template.Stream,
			VolumeDb = template.VolumeDb,
			PitchScale = template.PitchScale,
			Bus = template.Bus,
			MaxDb = template.MaxDb,
			UnitSize = template.UnitSize,
			MaxDistance = template.MaxDistance,
			AttenuationModel = template.AttenuationModel,
			Autoplay = false,
		};

		parent.AddChild(emitter);
		emitter.GlobalPosition = template.GlobalPosition;

		var manager = Instance;
		emitter.Finished += () => manager.FreeEmitter(emitter);
		emitter.Play();

		manager._emitters[emitter] = template.FollowTarget;
	}

	private void FreeEmitter(AudioStreamPlayer3D emitter)
	{
		_emitters.Remove(emitter);
		if (IsInstanceValid(emitter)) emitter.QueueFree();
	}

	public override void _PhysicsProcess(double delta)
	{
		foreach (var (emitter, target) in _emitters)
		{
			if (IsInstanceValid(emitter) && IsInstanceValid(target))
				emitter.GlobalPosition = target!.GlobalPosition;
		}
	}
}
