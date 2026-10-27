#nullable enable
using Godot;

namespace ActionKit.Weapons;

/// <summary>
/// A beam's visual: scales along local Z to BeamScale and sets the instance shader
/// parameter "instance_tiling" to (1, BeamScale) so a scrolling texture doesn't stretch.
/// Model it one unit long, pointing down -Z.
/// </summary>
[Tool]
[GlobalClass]
public partial class BeamMesh : MeshInstance3D
{
	[Export(PropertyHint.Range, "0,100,or_greater")]
	public float BeamScale
	{
		get => _beamScale;
		set
		{
			_beamScale = value;
			Scale = new Vector3(Scale.X, Scale.Y, value);
			SetInstanceShaderParameter("instance_tiling", new Vector2(1.0f, _beamScale));
		}
	}

	private float _beamScale = 1f;
}
