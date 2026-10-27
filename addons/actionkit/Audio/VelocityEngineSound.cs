#nullable enable
using ActionKit.Utilities;
using Godot;

namespace ActionKit.Audio;

/// <summary>Drives its RampedAudioStream's RampValue from the actor's speed (0 at rest, 1 at MaxSpeed).</summary>
[GlobalClass]
public partial class VelocityEngineSound : RampedAudioStreamPlayer3D
{
	[Export] public Actor? OwnerActor { get; set; }
	[Export] public float MaxSpeed { get; set; } = 40f;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint()) OwnerActor ??= this.FindAncestorOfType<Actor>();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint() || OwnerActor?.ActorBody == null || RampedStream == null || MaxSpeed <= 0f) return;

		RampedStream.RampValue = OwnerActor.ActorBody.LinearVelocity.LimitLength(MaxSpeed).Length() / MaxSpeed;
	}
}
