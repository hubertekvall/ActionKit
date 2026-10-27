#nullable enable
using Godot;

namespace ActionKit.Utilities;

/// <summary>A 1D spring-damper you can step every frame to smooth any float value.</summary>
[GlobalClass]
public partial class SpringDamper : Resource
{
	[Export] public float Stiffness { get; set; }
	[Export] public float Damping { get; set; }
	public float Value { get; private set; }
	public float Velocity { get; private set; }

	public SpringDamper() : this(50.0f, 10.0f) { }

	public SpringDamper(float stiffness, float damping)
	{
		Stiffness = stiffness;
		Damping = damping;
	}

	public float Update(float target, float delta)
	{
		float force = (target - Value) * Stiffness - Velocity * Damping;
		Velocity += force * delta;
		Value += Velocity * delta;
		return Value;
	}

	public void Reset(float to = 0.0f)
	{
		Value = to;
		Velocity = 0.0f;
	}
}
