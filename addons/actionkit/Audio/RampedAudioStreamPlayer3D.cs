#nullable enable
using Godot;

namespace ActionKit.Audio;

/// <summary>An AudioStreamPlayer3D that plays a RampedAudioStream.</summary>
[Tool]
[GlobalClass]
public partial class RampedAudioStreamPlayer3D : AudioStreamPlayer3D
{
	[Export] public RampedAudioStream? RampedStream { get; set; }

	public override void _Ready()
	{
		if (RampedStream != null)
			Stream = RampedStream;
	}
}
