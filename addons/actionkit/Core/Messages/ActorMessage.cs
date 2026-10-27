#nullable enable
using Godot;

namespace ActionKit;

/// <summary>
/// Base for everything sent through Actor.Broadcast. Subclass it for your own game events;
/// components pattern-match on the type in OnMessage.
/// </summary>
public abstract class ActorMessage
{
	/// <summary>Who sent it (an Actor or an ActorComponent), if anyone signed it.</summary>
	public Node? Sender;

	public ActorMessage Signed(Node signer)
	{
		Sender = signer;
		return this;
	}
}
