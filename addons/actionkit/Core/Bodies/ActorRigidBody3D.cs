#nullable enable
using Godot;

namespace ActionKit;

/// <summary>RigidBody3D-backed actor body. Enable contact monitoring to get CollisionMessages.</summary>
[GlobalClass]
public partial class ActorRigidBody3D : RigidBody3D, IActorBody3D
{
	public PhysicsDirectBodyState3D? PhysicsState { get; private set; }

	public Actor ActorOwner { get; set; } = null!;

	public override void _Ready()
	{
		PhysicsState = PhysicsServer3D.BodyGetDirectState(GetRid());
	}

	public override void _PhysicsProcess(double delta)
	{
		CheckForContacts();
	}

	public void CheckForContacts()
	{
		this.CollectAndBroadcast();
	}
}
