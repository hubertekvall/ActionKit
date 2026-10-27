#nullable enable
using System;
using System.Collections.Generic;
using Godot;

namespace ActionKit.Utilities;

/// <summary>
/// Blue-noise point sampling over a 2D area (Bridson's algorithm). Every returned point is
/// at least MinDistance from every other, but otherwise scattered — no grid lines, no
/// clumps, which is what makes the result read as "natural" rather than random or tiled.
///
/// Uses System.Random rather than GD.Randf so a layout is fully reproducible from a seed
/// and the sampler can run outside the engine (tests, tools).
/// </summary>
public static class PoissonDisk
{
	/// <param name="area">Rectangle to fill.</param>
	/// <param name="minDistance">Minimum spacing between any two points.</param>
	/// <param name="random">Source of randomness.</param>
	/// <param name="accept">Optional extra filter (exclusion zones etc.). Rejected points don't seed new ones.</param>
	/// <param name="attempts">Candidates tried around each active point before it's retired. 30 is the textbook value.</param>
	public static List<Vector2> Sample(Rect2 area, float minDistance, Random random, Func<Vector2, bool>? accept = null, int attempts = 30)
	{
		var points = new List<Vector2>();
		if (minDistance <= 0f || area.Size.X <= 0f || area.Size.Y <= 0f) return points;

		// Cell size r/√2 guarantees at most one point per cell, so a neighbour check only
		// has to look at the surrounding 5x5 block.
		var cellSize = minDistance / Mathf.Sqrt2;
		var columns = Mathf.CeilToInt(area.Size.X / cellSize);
		var rows = Mathf.CeilToInt(area.Size.Y / cellSize);
		var grid = new int[columns * rows];
		Array.Fill(grid, -1);

		var active = new List<int>();

		// A rejected first point would stall the whole fill, so hunt for one that passes.
		for (var i = 0; i < attempts * 10; i++)
		{
			var seed = area.Position + new Vector2(random.NextSingle() * area.Size.X, random.NextSingle() * area.Size.Y);
			if (accept != null && !accept(seed)) continue;

			Insert(seed);
			break;
		}

		while (active.Count > 0)
		{
			var activeIndex = random.Next(active.Count);
			var origin = points[active[activeIndex]];
			var found = false;

			for (var i = 0; i < attempts; i++)
			{
				// Annulus [r, 2r] around the active point.
				var angle = random.NextSingle() * Mathf.Tau;
				var radius = minDistance * (1f + random.NextSingle());
				var candidate = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

				if (!area.HasPoint(candidate) || !IsFarEnough(candidate)) continue;
				if (accept != null && !accept(candidate)) continue;

				Insert(candidate);
				found = true;
				break;
			}

			if (!found)
			{
				// Swap-remove: order of the active list doesn't matter.
				active[activeIndex] = active[^1];
				active.RemoveAt(active.Count - 1);
			}
		}

		return points;

		void Insert(Vector2 point)
		{
			points.Add(point);
			active.Add(points.Count - 1);
			grid[CellIndex(point)] = points.Count - 1;
		}

		int CellIndex(Vector2 point)
		{
			var x = Mathf.Clamp((int)((point.X - area.Position.X) / cellSize), 0, columns - 1);
			var y = Mathf.Clamp((int)((point.Y - area.Position.Y) / cellSize), 0, rows - 1);
			return y * columns + x;
		}

		bool IsFarEnough(Vector2 candidate)
		{
			var cx = (int)((candidate.X - area.Position.X) / cellSize);
			var cy = (int)((candidate.Y - area.Position.Y) / cellSize);
			var minDistanceSquared = minDistance * minDistance;

			for (var y = Math.Max(cy - 2, 0); y <= Math.Min(cy + 2, rows - 1); y++)
			for (var x = Math.Max(cx - 2, 0); x <= Math.Min(cx + 2, columns - 1); x++)
			{
				var index = grid[y * columns + x];
				if (index >= 0 && points[index].DistanceSquaredTo(candidate) < minDistanceSquared)
					return false;
			}

			return true;
		}
	}
}
