#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// One way a mesh breaks apart. Piece i is Meshes[i], centered on its own origin, with a convex
/// collider in Shapes[i]; Centers[i] is where that origin sits in the source mesh's space.
/// </summary>
[GlobalClass]
public partial class FracturePattern : Resource
{
	[Export] public Godot.Collections.Array<ArrayMesh> Meshes { get; set; } = new();
	[Export] public Godot.Collections.Array<Shape3D> Shapes { get; set; } = new();
	[Export] public Vector3[] Centers { get; set; } = [];
}
