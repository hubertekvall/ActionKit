#nullable enable
using System.Collections.Generic;
using System;
using Godot;

namespace ActionKit.Effects;

/// <summary>
/// A dynamically generated ribbon trail. Samples an emitter's world position over time and
/// builds a tapered, fading strip mesh behind it — drop it anywhere (an engine nozzle, a
/// missile tail, a turret muzzle, a fast-moving projectile) and point <see cref="EmitterPath"/>
/// at the node to trail; leave it unset and it trails itself.
///
/// The mesh is rebuilt every physics frame from a short history of recorded points plus the
/// emitter's live current position, so the head always tracks smoothly even between recorded
/// segments. Segment density is controlled by <see cref="MinVertexDistance"/> (record a new
/// point only after moving at least this far) and capped by <see cref="MaxSegments"/>; points
/// additionally age out after <see cref="SegmentLifetime"/> seconds when
/// <see cref="UseLifetime"/> is on, giving the classic fading-exhaust look independent of speed.
///
/// The ribbon always faces the active viewport camera. Points are recorded in world space but
/// converted into this node's local space when the mesh is built, so it renders normally as a
/// child of its parent. Assign a double-sided (cull disabled) material with vertex colors
/// enabled as albedo alpha — width taper and fade are written into the geometry and vertex
/// color alpha, not baked into a shader, so any unlit/additive engine-glow material works.
/// </summary>
[Tool]
[GlobalClass]
public partial class TrailMesh3D : MeshInstance3D
{
	[ExportGroup("Emission")]
	[Export] public NodePath EmitterPath { get; set; } = new();

	[Export] public bool Emitting { get; set; } = true;

	/// <summary>Node whose world position is trailed. Falls back to this node itself when unset. Safe to assign from code (e.g. a spawner wiring up a pooled trail).</summary>
	public Node3D? Emitter { get; set; }

	[ExportGroup("Segments")]
	[Export(PropertyHint.Range, "2,256")]
	public int MaxSegments { get; set; } = 32;

	[Export(PropertyHint.Range, "0.0,2.0,0.01,or_greater")]
	public float MinVertexDistance { get; set; } = 0.05f;

	[Export] public bool UseLifetime { get; set; } = true;

	[Export(PropertyHint.Range, "0.05,5.0,0.01,or_greater")]
	public float SegmentLifetime { get; set; } = 0.6f;

	/// <summary>Clears the trail instead of stretching a streak across the gap when the emitter jumps farther than this in one frame (teleport/respawn guard). 0 disables the check.</summary>
	[Export] public float MaxJumpDistance { get; set; } = 10f;

	[ExportGroup("Shape")]
	[Export(PropertyHint.Range, "0.0,10.0,0.01,or_greater")]
	public float Width { get; set; } = 0.3f;

	/// <summary>Sampled 0 (head, at the emitter) to 1 (tail, oldest point) and multiplied into <see cref="Width"/>. Null = constant width.</summary>
	[Export] public Curve? WidthCurve { get; set; }

	/// <summary>Sampled 0 (head, at the emitter) to 1 (tail, oldest point) and multiplied into vertex color alpha. Null = fully opaque.</summary>
	[Export] public Curve? AlphaCurve { get; set; }

	[ExportGroup("UV")]
	/// <summary>V spans 0..1 across the whole current trail (a stretchy decal look). Off = V scrolls with <see cref="UvWorldLength"/> instead.</summary>
	[Export] public bool StretchUv { get; set; } = true;

	/// <summary>World units per full V tile when <see cref="StretchUv"/> is off.</summary>
	[Export(PropertyHint.Range, "0.01,10.0,0.01,or_greater")]
	public float UvWorldLength { get; set; } = 1f;

	private struct TrailPoint
	{
		public Vector3 Position;
		public float Age;
	}

	private readonly List<TrailPoint> _history = new();
	private readonly List<TrailPoint> _combinedBuffer = new();
	private ArrayMesh _arrayMesh = null!;

	public override void _Ready()
	{
		if (Engine.IsEditorHint()) return;

		Emitter ??= GetNodeOrNull<Node3D>(EmitterPath) ?? this;

		_arrayMesh = new ArrayMesh();
		Mesh = _arrayMesh;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint() || Emitter == null) return;

		AgeAndCullHistory((float)delta);
		TryRecordPoint();
		RebuildMesh();
	}

	/// <summary>Discards all recorded history immediately and clears the mesh (e.g. on respawn/teleport, or when reclaiming a pooled trail).</summary>
	public void Clear()
	{
		_history.Clear();
		_arrayMesh?.ClearSurfaces();
	}

	private void AgeAndCullHistory(float delta)
	{
		for (var i = 0; i < _history.Count; i++)
		{
			var point = _history[i];
			point.Age += delta;
			_history[i] = point;
		}

		while (UseLifetime && _history.Count > 0 && _history[0].Age > SegmentLifetime)
			_history.RemoveAt(0);

		while (_history.Count > MaxSegments)
			_history.RemoveAt(0);
	}

	private void TryRecordPoint()
	{
		if (!Emitting) return;

		var emitterPos = Emitter!.GlobalPosition;

		if (_history.Count > 0 && MaxJumpDistance > 0f
			&& emitterPos.DistanceTo(_history[^1].Position) > MaxJumpDistance)
		{
			Clear();
		}

		if (_history.Count > 0 && emitterPos.DistanceTo(_history[^1].Position) < MinVertexDistance)
			return;

		_history.Add(new TrailPoint { Position = emitterPos, Age = 0f });
	}

	private void RebuildMesh()
	{
		_combinedBuffer.Clear();
		_combinedBuffer.AddRange(_history);

		// Live head: the emitter's current position, appended transiently (never stored in
		// history) so the trail tip tracks smoothly between recorded segments instead of
		// snapping from point to point.
		if (Emitting)
			_combinedBuffer.Add(new TrailPoint { Position = Emitter!.GlobalPosition, Age = 0f });

		var count = _combinedBuffer.Count;
		if (count < 2)
		{
			_arrayMesh.ClearSurfaces();
			return;
		}

		var camera = GetViewport()?.GetCamera3D();
		var worldToLocal = GlobalTransform.AffineInverse();

		// Arc length up to each point, used for even width/alpha tapering and world-scaled UVs
		// regardless of how unevenly the points ended up spaced.
		Span<float> cumulativeLength = count <= 256 ? stackalloc float[count] : new float[count];
		cumulativeLength[0] = 0f;
		for (var i = 1; i < count; i++)
			cumulativeLength[i] = cumulativeLength[i - 1] + _combinedBuffer[i - 1].Position.DistanceTo(_combinedBuffer[i].Position);
		var totalLength = cumulativeLength[count - 1];

		var vertices = new Vector3[count * 2];
		var normals = new Vector3[count * 2];
		var uvs = new Vector2[count * 2];
		var colors = new Color[count * 2];

		for (var i = 0; i < count; i++)
		{
			var position = _combinedBuffer[i].Position;

			var tangent =
				i == 0 ? _combinedBuffer[1].Position - _combinedBuffer[0].Position :
				i == count - 1 ? _combinedBuffer[i].Position - _combinedBuffer[i - 1].Position :
				_combinedBuffer[i + 1].Position - _combinedBuffer[i - 1].Position;
			tangent = tangent.LengthSquared() > 1e-8f ? tangent.Normalized() : Vector3.Forward;

			var right = ComputeRight(tangent, position, camera);

			// t runs 0 at the head (the emitter, newest point) to 1 at the tail (oldest point),
			// so a curve's left edge is the start of the trail and its right edge is the end —
			// matching how the ribbon actually streams away from the emitter.
			var t = totalLength > 0f ? 1f - cumulativeLength[i] / totalLength : 1f - (float)i / (count - 1);
			var halfWidth = Width * (WidthCurve?.Sample(t) ?? 1f) * 0.5f;

			var alpha = AlphaCurve?.Sample(t) ?? 1f;
			if (UseLifetime && SegmentLifetime > 0f)
				alpha *= 1f - Mathf.Clamp(_combinedBuffer[i].Age / SegmentLifetime, 0f, 1f);

			var normal = tangent.Cross(right);
			normal = normal.LengthSquared() > 1e-8f ? normal.Normalized() : Vector3.Up;

			var uvY = StretchUv ? t : cumulativeLength[i] / UvWorldLength;

			var leftWorld = position - right * halfWidth;
			var rightWorld = position + right * halfWidth;
			vertices[i * 2] = worldToLocal * leftWorld;
			vertices[i * 2 + 1] = worldToLocal * rightWorld;

			var localNormal = (worldToLocal.Basis * normal).Normalized();
			normals[i * 2] = localNormal;
			normals[i * 2 + 1] = localNormal;

			uvs[i * 2] = new Vector2(0f, uvY);
			uvs[i * 2 + 1] = new Vector2(1f, uvY);

			var color = new Color(1f, 1f, 1f, alpha);
			colors[i * 2] = color;
			colors[i * 2 + 1] = color;
		}

		var indices = new int[(count - 1) * 6];
		for (var i = 0; i < count - 1; i++)
		{
			var b = i * 2;
			var o = i * 6;
			indices[o] = b;
			indices[o + 1] = b + 1;
			indices[o + 2] = b + 2;
			indices[o + 3] = b + 1;
			indices[o + 4] = b + 3;
			indices[o + 5] = b + 2;
		}

		_arrayMesh.ClearSurfaces();

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Normal] = normals;
		arrays[(int)Mesh.ArrayType.TexUV] = uvs;
		arrays[(int)Mesh.ArrayType.Color] = colors;
		arrays[(int)Mesh.ArrayType.Index] = indices;

		_arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
	}

	/// <summary>Ribbon width vector: perpendicular to the tangent, facing the active camera. Falls back to a world-up-based perpendicular when no camera is present.</summary>
	private static Vector3 ComputeRight(Vector3 tangent, Vector3 position, Camera3D? camera)
	{
		var right = Vector3.Zero;

		if (camera != null)
		{
			var toCamera = camera.GlobalPosition - position;
			if (toCamera.LengthSquared() > 1e-8f)
				right = tangent.Cross(toCamera.Normalized());
		}

		if (right.LengthSquared() < 1e-6f)
			right = tangent.Cross(tangent.Cross(Vector3.Up).LengthSquared() > 1e-6f ? Vector3.Up : Vector3.Right);

		return right.Normalized();
	}
}
