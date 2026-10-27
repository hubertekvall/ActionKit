#nullable enable
using Godot;

namespace ActionKit.Utilities;

public static class VectorExtensions
{
	/// <summary>The vector with Y zeroed (projected onto the ground plane).</summary>
	public static Vector3 Flattened(this Vector3 vector) => new(vector.X, 0f, vector.Z);

	/// <summary>A uniformly distributed random unit vector.</summary>
	public static Vector3 RandomDirection()
	{
		var dir = new Vector3(
			(float)GD.RandRange(-1.0, 1.0),
			(float)GD.RandRange(-1.0, 1.0),
			(float)GD.RandRange(-1.0, 1.0));

		if (dir.LengthSquared() < 0.0001f)
			dir = Vector3.Up; // degenerate guard; any fixed axis works

		return dir.Normalized();
	}

	/// <summary>A random point inside a sphere, uniform over its volume.</summary>
	public static Vector3 RandomPointInSphere(float radius) =>
		RandomDirection() * radius * Mathf.Pow(GD.Randf(), 1.0f / 3.0f);
}
