#nullable enable
using System.Collections.Generic;
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Breaks a mesh into Voronoi cells around seeds scattered over its surface. Each cell is the
/// set of triangles on its side of every bisecting plane, clipped where they cross one. The cut
/// is left open (no caps), so the inside of the hull shows through; give the hull a double
/// sided material that shades back faces as burnt.
///
/// Works on any triangle mesh, closed or not. Keeps normals, tangents, colors, UV and UV2;
/// skinning and blend shapes are dropped.
/// </summary>
public static class MeshFracturer
{
	private record struct Vertex
	{
		public Vector3 Position;
		public Vector3 Normal;
		public Plane Tangent;
		public Color Color;
		public Vector2 Uv;
		public Vector2 Uv2;

		public static Vertex Lerp(in Vertex a, in Vertex b, float t) => new()
		{
			Position = a.Position.Lerp(b.Position, t),
			Normal = a.Normal.Lerp(b.Normal, t).Normalized(),
			Tangent = new Plane(a.Tangent.Normal.Lerp(b.Tangent.Normal, t).Normalized(), a.Tangent.D),
			Color = a.Color.Lerp(b.Color, t),
			Uv = a.Uv.Lerp(b.Uv, t),
			Uv2 = a.Uv2.Lerp(b.Uv2, t),
		};
	}

	private class Surface
	{
		public readonly List<Vertex> Triangles = [];
		public int Index;
		public Material? Material;
		public bool HasNormal, HasTangent, HasColor, HasUv, HasUv2;
	}

	public static FracturePattern Fracture(MeshInstance3D meshInstance, int pieceCount)
	{
		var surfaces = ReadSurfaces(meshInstance);
		var seeds = ScatterSeeds(surfaces, pieceCount);
		var pattern = new FracturePattern();
		var centers = new List<Vector3>();

		// Voronoi cells are convex, so a triangle whose three corners share a nearest seed lies
		// wholly in that cell. Only triangles whose corners disagree need clipping.
		var owners = new List<int[]>();
		foreach (var surface in surfaces)
			owners.Add(NearestSeeds(surface.Triangles, seeds));

		var planes = new List<Plane>();
		var cell = new List<Vertex>();
		for (var i = 0; i < seeds.Count; i++)
		{
			planes.Clear();
			for (var j = 0; j < seeds.Count; j++)
			{
				var toOther = seeds[j] - seeds[i];
				if (j != i && !toOther.IsZeroApprox())
					planes.Add(new Plane(toOther.Normalized(), seeds[i] + toOther * 0.5f));
			}

			var piece = new ArrayMesh();
			Aabb? bounds = null;
			var clippedSurfaces = new List<(Surface Source, Vertex[] Triangles)>();
			for (var s = 0; s < surfaces.Count; s++)
			{
				var surface = surfaces[s];
				cell.Clear();
				ClipToCell(surface.Triangles, owners[s], i, planes, cell);
				if (cell.Count == 0) continue;

				foreach (var vertex in cell)
					bounds = bounds?.Expand(vertex.Position) ?? new Aabb(vertex.Position, Vector3.Zero);
				clippedSurfaces.Add((surface, cell.ToArray()));
			}
			if (bounds is not { } cellBounds) continue;

			var center = cellBounds.GetCenter();
			foreach (var (source, triangles) in clippedSurfaces)
				Commit(piece, source, triangles, center);

			pattern.Meshes.Add(piece);
			pattern.Shapes.Add(piece.CreateConvexShape());
			centers.Add(center);
		}

		pattern.Centers = centers.ToArray();
		return pattern;
	}

	// Triangles fully owned by the cell are copied, ones fully owned by a single other cell are
	// skipped, and the rest are clipped against every plane of the cell (keeping the side facing
	// its seed). A triangle clipped by planes stays convex, so a fan re-triangulates it.
	private static void ClipToCell(List<Vertex> triangles, int[] owners, int cellIndex, List<Plane> planes, List<Vertex> output)
	{
		var polygon = new List<Vertex>(8);
		var scratch = new List<Vertex>(8);

		for (var t = 0; t < triangles.Count; t += 3)
		{
			var owner = owners[t];
			if (owner == owners[t + 1] && owner == owners[t + 2])
			{
				if (owner != cellIndex) continue;
				output.Add(triangles[t]);
				output.Add(triangles[t + 1]);
				output.Add(triangles[t + 2]);
				continue;
			}

			polygon.Clear();
			polygon.Add(triangles[t]);
			polygon.Add(triangles[t + 1]);
			polygon.Add(triangles[t + 2]);

			foreach (var plane in planes)
			{
				ClipPolygon(polygon, plane, scratch);
				(polygon, scratch) = (scratch, polygon);
				if (polygon.Count < 3) break;
			}
			if (polygon.Count < 3) continue;

			for (var k = 1; k < polygon.Count - 1; k++)
			{
				output.Add(polygon[0]);
				output.Add(polygon[k]);
				output.Add(polygon[k + 1]);
			}
		}
	}

	private static int[] NearestSeeds(List<Vertex> vertices, List<Vector3> seeds)
	{
		var owners = new int[vertices.Count];
		for (var v = 0; v < vertices.Count; v++)
		{
			var best = float.MaxValue;
			for (var i = 0; i < seeds.Count; i++)
			{
				var distance = vertices[v].Position.DistanceSquaredTo(seeds[i]);
				if (distance < best)
				{
					best = distance;
					owners[v] = i;
				}
			}
		}
		return owners;
	}

	// Sutherland–Hodgman against one plane, keeping DistanceTo <= 0.
	private static void ClipPolygon(List<Vertex> polygon, Plane plane, List<Vertex> output)
	{
		output.Clear();
		for (var k = 0; k < polygon.Count; k++)
		{
			var current = polygon[k];
			var next = polygon[(k + 1) % polygon.Count];
			var currentDistance = plane.DistanceTo(current.Position);
			var nextDistance = plane.DistanceTo(next.Position);

			if (currentDistance <= 0f) output.Add(current);
			if (currentDistance <= 0f != nextDistance <= 0f)
				output.Add(Vertex.Lerp(current, next, currentDistance / (currentDistance - nextDistance)));
		}
	}

	// Welds identical vertices back together and appends the result as one surface. Built as raw
	// arrays rather than through SurfaceTool to keep it to a single engine call per surface.
	private static void Commit(ArrayMesh piece, Surface source, Vertex[] triangles, Vector3 center)
	{
		var lookup = new Dictionary<Vertex, int>();
		var unique = new List<Vertex>();
		var indices = new int[triangles.Length];
		for (var n = 0; n < triangles.Length; n++)
		{
			if (!lookup.TryGetValue(triangles[n], out var index))
			{
				index = unique.Count;
				lookup[triangles[n]] = index;
				unique.Add(triangles[n]);
			}
			indices[n] = index;
		}

		var positions = new Vector3[unique.Count];
		var normals = new Vector3[unique.Count];
		var tangents = new float[unique.Count * 4];
		var colors = new Color[unique.Count];
		var uvs = new Vector2[unique.Count];
		var uv2s = new Vector2[unique.Count];
		for (var v = 0; v < unique.Count; v++)
		{
			var vertex = unique[v];
			positions[v] = vertex.Position - center;
			normals[v] = vertex.Normal;
			tangents[v * 4] = vertex.Tangent.X;
			tangents[v * 4 + 1] = vertex.Tangent.Y;
			tangents[v * 4 + 2] = vertex.Tangent.Z;
			tangents[v * 4 + 3] = vertex.Tangent.D;
			colors[v] = vertex.Color;
			uvs[v] = vertex.Uv;
			uv2s[v] = vertex.Uv2;
		}

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = positions;
		arrays[(int)Mesh.ArrayType.Index] = indices;
		if (source.HasNormal) arrays[(int)Mesh.ArrayType.Normal] = normals;
		if (source.HasTangent) arrays[(int)Mesh.ArrayType.Tangent] = tangents;
		if (source.HasColor) arrays[(int)Mesh.ArrayType.Color] = colors;
		if (source.HasUv) arrays[(int)Mesh.ArrayType.TexUV] = uvs;
		if (source.HasUv2) arrays[(int)Mesh.ArrayType.TexUV2] = uv2s;

		piece.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		var surface = piece.GetSurfaceCount() - 1;
		// Pieces skip surfaces they don't touch, so remember which source surface this was.
		piece.SurfaceSetName(surface, source.Index.ToString());
		if (source.Material != null) piece.SurfaceSetMaterial(surface, source.Material);
	}

	// Flattens every surface into a plain triangle list. The instance's active material is baked
	// in so surface overrides on the source mesh carry over to the debris.
	private static List<Surface> ReadSurfaces(MeshInstance3D meshInstance)
	{
		var mesh = meshInstance.Mesh;
		var surfaces = new List<Surface>();

		for (var s = 0; s < mesh.GetSurfaceCount(); s++)
		{
			var arrays = mesh.SurfaceGetArrays(s);
			var positions = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
			var normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
			var tangents = arrays[(int)Mesh.ArrayType.Tangent].AsFloat32Array();
			var colors = arrays[(int)Mesh.ArrayType.Color].AsColorArray();
			var uvs = arrays[(int)Mesh.ArrayType.TexUV].AsVector2Array();
			var uv2s = arrays[(int)Mesh.ArrayType.TexUV2].AsVector2Array();
			var indices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();

			var surface = new Surface
			{
				Index = s,
				Material = meshInstance.GetActiveMaterial(s),
				HasNormal = normals.Length > 0,
				HasTangent = tangents.Length > 0,
				HasColor = colors.Length > 0,
				HasUv = uvs.Length > 0,
				HasUv2 = uv2s.Length > 0,
			};

			var count = indices.Length > 0 ? indices.Length : positions.Length;
			for (var n = 0; n < count; n++)
			{
				var i = indices.Length > 0 ? indices[n] : n;
				surface.Triangles.Add(new Vertex
				{
					Position = positions[i],
					Normal = surface.HasNormal ? normals[i] : Vector3.Zero,
					Tangent = surface.HasTangent ? new Plane(tangents[i * 4], tangents[i * 4 + 1], tangents[i * 4 + 2], tangents[i * 4 + 3]) : new Plane(),
					Color = surface.HasColor ? colors[i] : Colors.White,
					Uv = surface.HasUv ? uvs[i] : Vector2.Zero,
					Uv2 = surface.HasUv2 ? uv2s[i] : Vector2.Zero,
				});
			}
			surfaces.Add(surface);
		}

		return surfaces;
	}

	// Seeds on the surface, area weighted, so every cell gets some of the hull and big panels
	// get more pieces than small greebles.
	private static List<Vector3> ScatterSeeds(List<Surface> surfaces, int count)
	{
		var triangles = new List<Vertex>();
		foreach (var surface in surfaces)
			triangles.AddRange(surface.Triangles);

		var cumulativeArea = new float[triangles.Count / 3];
		var total = 0f;
		for (var i = 0; i < cumulativeArea.Length; i++)
		{
			var a = triangles[i * 3].Position;
			total += (triangles[i * 3 + 1].Position - a).Cross(triangles[i * 3 + 2].Position - a).Length();
			cumulativeArea[i] = total;
		}

		var seeds = new List<Vector3>(count);
		for (var s = 0; s < count && total > 0f; s++)
		{
			var tri = System.Array.BinarySearch(cumulativeArea, GD.Randf() * total);
			if (tri < 0) tri = System.Math.Min(~tri, cumulativeArea.Length - 1);

			var u = GD.Randf();
			var v = GD.Randf();
			if (u + v > 1f) (u, v) = (1f - u, 1f - v);

			var a = triangles[tri * 3].Position;
			seeds.Add(a + (triangles[tri * 3 + 1].Position - a) * u + (triangles[tri * 3 + 2].Position - a) * v);
		}
		return seeds;
	}
}
