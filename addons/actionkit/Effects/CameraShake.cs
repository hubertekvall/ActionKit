#nullable enable
using Godot;

namespace ActionKit.Effects;

/// <summary>
/// Trauma-based camera shake (trauma² drives noise-based offset and roll, decaying over time).
/// Add one to the scene, point Camera at your camera (or leave it to use the active one), and
/// call CameraShake.Instance.AddTrauma(...) from anywhere, or use CameraShakeOnReady on
/// explosion actors. Shakes through Camera3D.HOffset/VOffset plus a small roll, so it doesn't
/// fight whatever moves the camera.
/// </summary>
[GlobalClass]
public partial class CameraShake : Node
{
	public static CameraShake? Instance { get; private set; }

	[Export] public Camera3D? Camera { get; set; }

	/// <summary>Trauma lost per second.</summary>
	[ExportGroup("Trauma")]
	[Export] public float DecayPerSecond { get; set; } = 2.0f;

	/// <summary>Shake = trauma ^ Exponent. 2 is the classic curve, 3 is punchier.</summary>
	[Export] public float Exponent { get; set; } = 2.0f;

	/// <summary>Trauma added with an origin falls off to nothing at this distance from the camera.</summary>
	[Export] public float MaxDistance { get; set; } = 30.0f;

	[ExportGroup("Shake")]
	[Export] public float MaxOffset { get; set; } = 0.5f;
	[Export] public float MaxRollDegrees { get; set; } = 3.0f;
	[Export] public float Frequency { get; set; } = 15.0f;

	public float Trauma { get; private set; }

	private readonly FastNoiseLite _noise = new() { NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex };
	private float _time;
	private float _appliedRoll;

	public override void _EnterTree() => Instance = this;

	public override void _ExitTree()
	{
		if (Instance == this) Instance = null;
	}

	/// <summary>Adds 0..1 trauma. With an origin, it falls off with distance from the camera.</summary>
	public void AddTrauma(float amount, Vector3? origin = null)
	{
		var camera = GetCamera();
		if (origin.HasValue && camera != null && MaxDistance > 0f)
		{
			var falloff = 1.0f - Mathf.Clamp(camera.GlobalPosition.DistanceTo(origin.Value) / MaxDistance, 0f, 1f);
			amount *= falloff * falloff;
		}

		Trauma = Mathf.Clamp(Trauma + amount, 0f, 1f);
	}

	public override void _Process(double delta)
	{
		var camera = GetCamera();
		if (camera == null) return;

		var dt = (float)delta;
		Trauma = Mathf.Max(0f, Trauma - DecayPerSecond * dt);
		_time += dt * Frequency;

		var shake = Mathf.Pow(Trauma, Exponent);
		camera.HOffset = MaxOffset * shake * _noise.GetNoise2D(_time, 0f);
		camera.VOffset = MaxOffset * shake * _noise.GetNoise2D(_time, 100f);

		// Remove last frame's roll before applying this frame's, so other rotation is kept.
		var roll = Mathf.DegToRad(MaxRollDegrees) * shake * _noise.GetNoise2D(_time, 200f);
		camera.RotateObjectLocal(Vector3.Back, roll - _appliedRoll);
		_appliedRoll = roll;
	}

	private Camera3D? GetCamera() => Camera ?? GetViewport()?.GetCamera3D();
}
