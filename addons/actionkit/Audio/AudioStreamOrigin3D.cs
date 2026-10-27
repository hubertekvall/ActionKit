#nullable enable
using Godot;

namespace ActionKit.Audio;

/// <summary>
/// A one-shot 3D sound that doesn't cut off when its node is freed. On ready it hands its
/// settings to AudioEmitterManager, which plays a copy that follows FollowTarget (the parent
/// by default) for as long as it exists, then this node frees itself. Use it for explosions,
/// impacts and death sounds on short-lived actors.
/// </summary>
[GlobalClass]
public partial class AudioStreamOrigin3D : AudioStreamPlayer3D
{
	[Export] public Node3D? FollowTarget { get; set; }

	public override void _Ready()
	{
		if (Engine.IsEditorHint()) return;

		FollowTarget ??= GetParentOrNull<Node3D>();
		AudioEmitterManager.Play3D(this);
		QueueFree();
	}
}
