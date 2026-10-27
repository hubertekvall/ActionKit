#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Drives impact ripples on a shield mesh. Keeps 9 points spread over the mesh's bounding
/// ellipsoid; a hit bumps the intensity of the nearest one and it decays over time. Each
/// frame they are written to the shader as a vec4 array uniform named "positions"
/// (xyz = world position, w = intensity).
/// </summary>
[GlobalClass]
public partial class ShieldMesh : ActorComponent
{
	private const int GridSize = 3; // 3x3 = 9 points

	[Export] public MeshInstance3D? ShieldMeshInstance { get; set; }
	[Export] public ShaderMaterial? ShieldMaterial { get; set; }

	[Export] public float ImpactIntensityPerHit = 0.5f;
	[Export] public float DecayRate = 0.5f; // intensity units per second

	private readonly Vector3[] _localPoints = new Vector3[GridSize * GridSize];
	private readonly float[] _intensities = new float[GridSize * GridSize];
	private readonly Vector4[] _shaderPositions = new Vector4[GridSize * GridSize];

	public override void OnActorReady()
	{
		if (ShieldMeshInstance != null) BuildGridFromAabb();
	}

	public override void ProcessTick(double delta)
	{
		if (ShieldMeshInstance == null || ShieldMaterial == null) return;

		Transform3D worldTransform = ShieldMeshInstance.GlobalTransform;

		for (int i = 0; i < _localPoints.Length; i++)
		{
			_intensities[i] = Mathf.Max(0f, _intensities[i] - DecayRate * (float)delta);

			Vector3 worldPos = worldTransform * _localPoints[i];
			_shaderPositions[i] = new Vector4(worldPos.X, worldPos.Y, worldPos.Z, _intensities[i]);
		}

		ShieldMaterial.SetShaderParameter("positions", _shaderPositions);
	}

	public override void OnMessage(ActorMessage message)
	{
		if (ShieldMeshInstance != null && message is DamageMessage { Contact: { } contact })
		{
			var collisionPosition = contact.Position;
			int closestIndex = GetClosestPointIndex(collisionPosition);
			_intensities[closestIndex] = Mathf.Min(1f, _intensities[closestIndex] + ImpactIntensityPerHit);
		}
	}

	private void BuildGridFromAabb()
{
	Aabb aabb = ShieldMeshInstance!.GetAabb();
	Vector3 center = aabb.Position + aabb.Size * 0.5f;
	Vector3 halfExtents = aabb.Size * 0.5f;

	int index = 0;
	for (int lat = 0; lat < GridSize; lat++)
	{
		// 3 latitude bands, e.g. -45°, 0°, 45°
		float latAngle = ((float)lat / (GridSize - 1) * 2f - 1f) * Mathf.Pi * 0.25f;

		for (int lon = 0; lon < GridSize; lon++)
		{
			// 3 longitudes spaced evenly around the full 360°
			float lonAngle = (float)lon / GridSize * Mathf.Tau;

			var dir = new Vector3(
				Mathf.Cos(latAngle) * Mathf.Sin(lonAngle),
				Mathf.Sin(latAngle),
				Mathf.Cos(latAngle) * Mathf.Cos(lonAngle)
			);

			_localPoints[index] = center + dir * halfExtents; // component-wise scale -> ellipsoid surface
			index++;
		}
	}
}

	private int GetClosestPointIndex(Vector3 worldPosition)
	{
		Transform3D worldTransform = ShieldMeshInstance!.GlobalTransform;

		int closestIndex = 0;
		float closestDistSq = float.MaxValue;

		for (int i = 0; i < _localPoints.Length; i++)
		{
			float distSq = (worldTransform * _localPoints[i]).DistanceSquaredTo(worldPosition);
			if (distSq < closestDistSq)
			{
				closestDistSq = distSq;
				closestIndex = i;
			}
		}

		return closestIndex;
	}
}
