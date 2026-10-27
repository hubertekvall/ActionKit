#nullable enable
using Godot;

namespace ActionKit.Components;

[GlobalClass]
public partial class SphereImpulse : ActorComponent
{
	[Export] public float Force { get; set; } = 10.0f;

	// 0 = straight up, 90 = flat hemisphere, 180 = full sphere
	[Export(PropertyHint.Range, "0,180,0.1")]
	public float SpreadDegrees { get; set; } = 90.0f;

	public override void OnActorReady()
	{
		Vector3 randomDirection = GetRandomDirectionInCone(SpreadDegrees);
		ActorOwner.ActorBody?.ApplyCentralImpulse(Force * randomDirection);
	}

	private static Vector3 GetRandomDirectionInCone(float spreadDegrees)
	{
		float maxPhi = Mathf.DegToRad(spreadDegrees);
		float theta = (float)GD.RandRange(0.0, Mathf.Tau);

		// Uniform sampling over the spherical cap [0, maxPhi]
		float cosMaxPhi = Mathf.Cos(maxPhi);
		float phi = Mathf.Acos(Mathf.Lerp(1.0f, cosMaxPhi, GD.Randf()));

		return new Vector3(
			Mathf.Sin(phi) * Mathf.Cos(theta),
			Mathf.Cos(phi),
			Mathf.Sin(phi) * Mathf.Sin(theta)
		);
	}
}
