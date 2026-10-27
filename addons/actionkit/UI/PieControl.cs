#nullable enable
using Godot;

namespace ActionKit.UI;

// A filled pie / ring sector with an outline, centered in the control's rect.
// Value sweeps from StartAngle clockwise. The band is Thickness wide inward from Radius,
// so it keeps a constant width as Radius changes; Thickness >= Radius gives a full pie.
[Tool]
[GlobalClass]
public partial class PieControl : Control
{
	// Arc points used for a full turn; partial sweeps use a proportional share.
	private const int Resolution = 64;

	private float _radius = 32.0f;
	private float _thickness = 8.0f;
	private float _value = 0.75f;
	private float _startAngle = -90.0f;
	private Color _fillColor = Colors.White;
	private Color _strokeColor = Colors.Black;
	private float _strokeWidth = 2.0f;

	[Export]
	public float Radius
	{
		get => _radius;
		set { _radius = Mathf.Max(value, 0.0f); UpdateMinimumSize(); QueueRedraw(); }
	}


	
	
	
	[Export(PropertyHint.Range, "0, 64, or_greater")]
	public float Thickness
	{
		get => _thickness;
		set { _thickness = Mathf.Max(value, 0.0f); QueueRedraw(); }
	}

	[Export(PropertyHint.Range, "0, 1")]
	public float Value
	{
		get => _value;
		set { _value = Mathf.Clamp(value, 0.0f, 1.0f); QueueRedraw(); }
	}
	
	// Degrees, 0 = right, -90 = top.
	[Export(PropertyHint.Range, "-360, 360")]
	public float StartAngle
	{
		get => _startAngle;
		set { _startAngle = value; QueueRedraw(); }
	}

	[Export]
	public Color FillColor
	{
		get => _fillColor;
		set { _fillColor = value; QueueRedraw(); }
	}

	[Export]
	public Color StrokeColor
	{
		get => _strokeColor;
		set { _strokeColor = value; QueueRedraw(); }
	}

	// 0 disables the outline.
	[Export(PropertyHint.Range, "0, 16, or_greater")]
	public float StrokeWidth
	{
		get => _strokeWidth;
		set { _strokeWidth = Mathf.Max(value, 0.0f); UpdateMinimumSize(); QueueRedraw(); }
	}

	[Export] public bool AntiAliased { get; set; } = true;

	// The radius actually drawn — override to animate it without touching the stored Radius.
	public virtual float GetRadius() => _radius;
	
	public override Vector2 _GetMinimumSize() => Vector2.One * (_radius + _strokeWidth) * 2.0f;

	public override void _Draw() => DrawSector(_value, _fillColor, _strokeColor);

	// One sector of this pie's shape (Radius/Thickness/StartAngle/StrokeWidth) swept to
	// value, in the given colors — so a subclass can layer extra lanes under the main one.
	protected void DrawSector(float value, Color fillColor, Color strokeColor)
	{
		var radius = GetRadius();
		if (value <= 0.0f || radius <= 0.0f) return;

		var center = Size / 2.0f;
		var inner = Mathf.Max(radius - _thickness, 0.0f);
		var start = Mathf.DegToRad(_startAngle);
		var end = start + Mathf.Tau * value;
		var points = Mathf.Max(2, Mathf.CeilToInt(Resolution * value) + 1);

		// Fill as one thick arc: centered between the radii, as wide as the ring.
		// Covers both the pie (inner = 0) and the full-circle case without a seam.
		var band = radius - inner;
		if (band > 0.0f)
			DrawArc(center, inner + band / 2.0f, start, end, points, fillColor, band, AntiAliased);

		if (_strokeWidth <= 0.0f) return;

		if (value >= 1.0f)
		{
			DrawArc(center, radius, 0.0f, Mathf.Tau, Resolution + 1, strokeColor, _strokeWidth, AntiAliased);
			if (inner > 0.0f)
				DrawArc(center, inner, 0.0f, Mathf.Tau, Resolution + 1, strokeColor, _strokeWidth, AntiAliased);
			return;
		}

		// Closed outline: outer arc forward, inner arc back (or the center for a pie).
		var innerPoints = inner > 0.0f ? points : 1;
		var outline = new Vector2[points + innerPoints + 1];

		for (var i = 0; i < points; i++)
		{
			var angle = Mathf.Lerp(start, end, (float)i / (points - 1));
			outline[i] = center + Vector2.FromAngle(angle) * radius;
		}

		if (inner > 0.0f)
		{
			for (var i = 0; i < points; i++)
			{
				var angle = Mathf.Lerp(end, start, (float)i / (points - 1));
				outline[points + i] = center + Vector2.FromAngle(angle) * inner;
			}
		}
		else
		{
			outline[points] = center;
		}

		outline[^1] = outline[0];
		DrawPolyline(outline, strokeColor, _strokeWidth, AntiAliased);
	}
}
