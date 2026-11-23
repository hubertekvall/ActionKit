#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Scales a particle system's AmountRatio with the body's speed, from MinRatio at rest to
/// MaxRatio at MaxSpeed and above, smoothed. Engine exhaust, dust kicked up, wake spray.
/// </summary>
[GlobalClass]
public partial class VelocityParticleAmount : ActorComponent
{
	[Export] public GpuParticles3D? Particles { get; set; }
	[Export(PropertyHint.Range, "0,1")] public float MinRatio { get; set; } = 0f;
	[Export(PropertyHint.Range, "0,1")] public float MaxRatio { get; set; } = 1f;
	[Export] public float MaxSpeed { get; set; } = 10f;

	/// <summary>Higher = snappier. Roughly how many times per second the gap is closed.</summary>
	[Export] public float SmoothSpeed { get; set; } = 8f;

	public override void PhysicsTick(double delta)
	{
		if (ActorOwner.ActorBody == null || Particles == null) return;

		var speed = ActorOwner.ActorBody.LinearVelocity.Length();
		var speedFraction = MaxSpeed > 0f ? Mathf.Clamp(speed / MaxSpeed, 0f, 1f) : 0f;
		var targetRatio = Mathf.Lerp(MinRatio, MaxRatio, speedFraction);

		var weight = 1f - Mathf.Exp(-SmoothSpeed * (float)delta);
		Particles.AmountRatio = Mathf.Lerp(Particles.AmountRatio, targetRatio, weight);
	}
}
