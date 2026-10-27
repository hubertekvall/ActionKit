#nullable enable
using System;
using Godot;

namespace ActionKit.UI;

/// <summary>
/// A health-bar style progress bar: Foreground jumps to Value at once, Delta (drawn behind)
/// waits DelayTime then springs after it, showing how much was just lost. Scales the two
/// ColorRects horizontally, so anchor them to the left. Bind a value with SetValueGetter.
/// </summary>
[Tool]
[GlobalClass]
public partial class DeltaProgressbar : PanelContainer
{

	[Export] public ColorRect? Delta { get; set; }
	[Export] public ColorRect? Foreground { get; set; }
	
	[Export(PropertyHint.Range, "0, 1")] public float Value { get; set; } = 1.0f;

	[Export] public float ChaseForce { get; set; } = 5.0f;
	[Export] public float ChaseDamping { get; set; } = 2.5f;


	[Export] public Color ForegroundColor { get; set; } = Colors.Red;
	[Export] public Color DeltaColor { get; set; } = Colors.White;
	
	
	[Export] public float DelayTime { get; set; } = 0.35f;

	private Func<float>? ValueGetter { get; set; }
	
	
	private Vector2 _chaseVelocity;
	private double _delayBuffer;


	public void SetValueGetter(Func<float> getter)
	{
		ValueGetter = getter;
	}

	private float FetchValue() => Mathf.Clamp(ValueGetter!(), 0.0f, 1.0f);
	
	public override void _Process(double delta)
	{
		if (Delta == null || Foreground == null) return;
		if (ValueGetter != null) Value = FetchValue();
		
		var dt = (float)delta;
		var target = new Vector2(Value, 1.0f);
		 
		
		// Foreground still holds LAST frame's Value at this point —
		// free edge detection for "took damage" without another field.
		if (Value < Foreground.Scale.X - 0.0001f)
			_delayBuffer = DelayTime;

		Foreground.Scale = target;


		_delayBuffer = Mathf.Max(_delayBuffer - dt, 0.0f);
		var gate = _delayBuffer > 0.0f ? 0.0f : 1.0f;
		// Gate only the spring term — damping stays live, so the bar
		// eases to a stop instead of freezing mid-motion.
		var force = (target - Delta.Scale) * ChaseForce * gate - _chaseVelocity * ChaseDamping;

		_chaseVelocity += force * dt;
		Delta.Scale += _chaseVelocity * dt;

		Foreground.Color = ForegroundColor;
		Delta.Color = DeltaColor;
	}
}
