#nullable enable
using Godot;

namespace ActionKit.Utilities;

public static class Ballistics
{
	/// <summary>
	/// Where to aim from <paramref name="from"/> so a projectile at <paramref name="speed"/>
	/// meets a target at <paramref name="position"/> moving at <paramref name="velocity"/>.
	/// Returns the target position itself for speed 0 (hitscan) or when the projectile can't
	/// catch it.
	/// </summary>
	public static Vector3 LeadPoint(Vector3 from, Vector3 position, Vector3 velocity, float speed)
	{
		if (speed <= 0.0f) return position;

		// Solve |offset + velocity * t| = speed * t for the smallest t > 0.
		var offset = position - from;
		var a = velocity.Dot(velocity) - speed * speed;
		var b = 2.0f * offset.Dot(velocity);
		var c = offset.Dot(offset);

		float t;
		if (Mathf.Abs(a) < 0.0001f)
		{
			if (b >= 0.0f) return position;
			t = -c / b;
		}
		else
		{
			var discriminant = b * b - 4.0f * a * c;
			if (discriminant < 0.0f) return position;
			var root = Mathf.Sqrt(discriminant);
			var t1 = (-b - root) / (2.0f * a);
			var t2 = (-b + root) / (2.0f * a);
			t = t1 > 0.0f && (t1 < t2 || t2 <= 0.0f) ? t1 : t2;
			if (t <= 0.0f) return position;
		}

		return position + velocity * t;
	}
}
