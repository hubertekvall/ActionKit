#nullable enable
using Godot;

namespace ActionKit.Utilities;

/// <summary>
/// Put this in a scene that comes and goes (an aiming overlay, a scanner mode). While it is in
/// the tree the camera also draws Layers; when it leaves, they are hidden again. Leave those
/// layers off the camera's own Cull Mask in the scene.
/// </summary>
[GlobalClass]
public partial class ShowRenderLayers : Node
{
	[Export(PropertyHint.Layers3DRender)] public uint Layers { get; set; }

	/// <summary>Camera to change. Defaults to the viewport's active camera.</summary>
	[Export] public Camera3D? Camera { get; set; }

	private Camera3D? _camera;

	public override void _Ready()
	{
		_camera = Camera ?? GetViewport().GetCamera3D();
		if (_camera != null) _camera.CullMask |= Layers;
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(_camera)) _camera!.CullMask &= ~Layers;
	}
}
