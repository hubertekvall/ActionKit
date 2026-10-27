#nullable enable
using Godot;

namespace ActionKit;

/// <summary>
/// Anything that moves an actor towards a desired heading. Movement components implement it
/// (see FlightMovement); input controllers and behavior-tree movement nodes only talk to this
/// interface, so they work with any movement model.
/// </summary>
public interface ISteerable
{
	/// <summary>Desired movement direction. Its length is the throttle (0..1, may exceed 1 for a boost).</summary>
	Vector3 Heading { get; }

	/// <summary>Direction the actor should face.</summary>
	Vector3 LookDirection { get; }

	void SetHeading(Vector3 heading, bool lookAtHeading = true);

	void SetLookDirection(Vector3 lookDirection);
}

public static class SteerableExtensions
{
	/// <summary>The actor itself if it is ISteerable, otherwise its first ISteerable component.</summary>
	public static ISteerable? GetSteerable(this Actor actor) =>
		actor as ISteerable ?? actor.GetComponent<ISteerable>();
}
