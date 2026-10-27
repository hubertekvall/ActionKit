#nullable enable
using Godot;

namespace ActionKit.UI;

// A PieControl with two extra lanes under the main one: a full background lane, and a
// delta lane that chases Value the way DeltaProgressbar's Delta does — when Value drops
// the main pie retreats at once and the delta eases down after it; when Value rises the
// delta snaps up with it.
[Tool]
[GlobalClass]
public partial class DeltaPieControl : PieControl
{
	private Color _backgroundColor = new(0.0f, 0.0f, 0.0f, 0.35f);
	private Color _deltaColor = new(1.0f, 1.0f, 1.0f, 0.5f);

	[Export]
	public Color BackgroundColor
	{
		get => _backgroundColor;
		set { _backgroundColor = value; QueueRedraw(); }
	}

	[Export]
	public Color DeltaColor
	{
		get => _deltaColor;
		set { _deltaColor = value; QueueRedraw(); }
	}

	[Export] public float ChaseForce { get; set; } = 200.0f;
	[Export] public float ChaseDamping { get; set; } = 50.0f;
	[Export] public float DelayTime { get; set; } = 0.35f;

	private float _deltaValue;
	private float _lastValue;
	private float _chaseVelocity;
	private double _delayBuffer;

	public override void _Ready()
	{
		_deltaValue = _lastValue = Value;
	}

	public override void _Process(double delta)
	{
		var dt = (float)delta;

		if (Value < _lastValue - 0.0001f)
			_delayBuffer = DelayTime;
		_lastValue = Value;

		if (_deltaValue <= Value)
		{
			if (_deltaValue < Value) QueueRedraw();
			_deltaValue = Value;
			_chaseVelocity = 0.0f;
			return;
		}

		_delayBuffer = Mathf.Max(_delayBuffer - dt, 0.0f);
		var gate = _delayBuffer > 0.0f ? 0.0f : 1.0f;
		// Gate only the spring term — damping stays live, same as DeltaProgressbar.
		var force = (Value - _deltaValue) * ChaseForce * gate - _chaseVelocity * ChaseDamping;

		_chaseVelocity += force * dt;
		// Never past the real value: the delta lane only ever trails the main one.
		_deltaValue = Mathf.Max(_deltaValue + _chaseVelocity * dt, Value);
		QueueRedraw();
	}

	public override void _Draw()
	{
		DrawSector(1.0f, _backgroundColor, _backgroundColor);
		DrawSector(_deltaValue, _deltaColor, _deltaColor);
		base._Draw();
	}
}
