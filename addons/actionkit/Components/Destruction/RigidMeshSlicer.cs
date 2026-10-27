#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// MeshSlicer whose pieces are physics debris: each one gets an ActorRigidBody3D as its Body,
/// using the baked convex collider. Pieces inherit the ship's velocity and are flung outward from
/// the ship's center.
/// </summary>
[Tool]
[GlobalClass]
public partial class RigidMeshSlicer : MeshSlicer
{
	[Export] public float ScatterSpeed { get; set; } = 4.0f;
	[Export] public float TumbleSpeed { get; set; } = 2.0f;
	[Export] public float GravityScale { get; set; } = 1.0f;

	// Mass of each piece. 0 splits the dying actor's mass evenly across the pieces.
	[Export] public float PieceMass { get; set; } = 0.0f;

	protected override Actor CreateDebris(ArrayMesh mesh, Shape3D shape, Vector3 center)
	{
		var shipBody = ActorOwner.ActorBody;
		var pieceCount = Fracture is { Patterns.Count: > 0 } ? Fracture.Patterns[0].Meshes.Count : SliceCount;
		var outward = MeshInstance!.GlobalBasis * (center - MeshInstance.Mesh.GetAabb().GetCenter());

		var body = new ActorRigidBody3D
		{
			Mass = PieceMass > 0f ? PieceMass : (shipBody?.Mass ?? pieceCount) / pieceCount,
			GravityScale = GravityScale,
			LinearVelocity = (shipBody?.LinearVelocity ?? Vector3.Zero) + outward.Normalized() * ScatterSpeed,
			AngularVelocity = new Vector3((float)GD.RandRange(-1.0, 1.0), (float)GD.RandRange(-1.0, 1.0), (float)GD.RandRange(-1.0, 1.0)) * TumbleSpeed,
		};
		body.AddChild(CreatePieceMesh(mesh));
		body.AddChild(new CollisionShape3D { Shape = shape });

		var actor = new Actor { Name = "Debris", Body = body };
		actor.AddChild(body);
		return actor;
	}
}
