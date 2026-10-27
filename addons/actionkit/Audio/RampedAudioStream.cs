#nullable enable
using System;
using Godot;

namespace ActionKit.Audio;

/// <summary>
/// An AudioStreamSynchronized that crossfades between its streams by RampValue (0..1): with
/// three streams, 0 plays the first, 0.5 the second, 1 the third, blending in between. Good
/// for engine sounds layered by intensity.
/// </summary>
[Tool]
[GlobalClass]
public partial class RampedAudioStream : AudioStreamSynchronized
{
	[Export(PropertyHint.Range, "0, 1")]
	public float RampValue
	{
		get => _rampValue;
		set
		{
			_rampValue = value;
			UpdateStreamVolumes();
		}
	}

	private float _rampValue;

	private void UpdateStreamVolumes()
	{
		int count = StreamCount;
		for (int i = 0; i < count; i++)
			SetSyncStreamVolume(i, CalculateVolumeDb(i, count));
	}

	private float CalculateVolumeDb(int index, int count)
	{
		if (count == 1) return 0f;

		float streamPos = index / (float)(count - 1);
		float distance = Math.Abs(RampValue - streamPos);
		float segmentSize = 1f / (count - 1);
		float gain = Math.Max(0f, 1f - distance / segmentSize);

		return Mathf.LinearToDb(gain);
	}
}
