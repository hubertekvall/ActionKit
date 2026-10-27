#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>Baked fracture patterns for one mesh. MeshSlicer picks one at random per death.</summary>
[GlobalClass]
public partial class FractureSet : Resource
{
	[Export] public Godot.Collections.Array<FracturePattern> Patterns { get; set; } = new();
}
