#nullable enable
using System;
using Godot;

namespace ActionKit.Utilities;
[GlobalClass]
public partial class CopyPosition : Node
{
	[Flags]
	public enum Dimensions : uint
	{
		X = 1,
		Y = 2,
		Z = 4,
	}

	[Export(PropertyHint.Flags)] public Dimensions CopyDimensions { get; set; }
	[Export] public Node3D? TargetNode { get; set; }

	private Node3D? _parent;

	public override void _Ready()
	{
		_parent = GetParentOrNull<Node3D>();
	}

	public override void _Process(double delta)
	{
		if (_parent == null || !IsInstanceValid(TargetNode)) return;

		var position = _parent.GlobalPosition;

		if ((CopyDimensions & Dimensions.X) != 0) position.X = TargetNode.GlobalPosition.X;
		if ((CopyDimensions & Dimensions.Y) != 0) position.Y = TargetNode.GlobalPosition.Y;
		if ((CopyDimensions & Dimensions.Z) != 0) position.Z = TargetNode.GlobalPosition.Z;

		_parent.GlobalPosition = position;
	}
}
