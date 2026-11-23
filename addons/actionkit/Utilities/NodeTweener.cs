#nullable enable
using Godot;

namespace ActionKit.Utilities;

[GlobalClass]
[Tool]
public partial class NodeTweener : Node
{
	/// <summary>Emitted when the tween completes (after the reverse part, if AutoReverseTween is on).</summary>
	[Signal] public delegate void FinishedEventHandler();

	[Export] public Node? TweenTarget { get; set; }
	[Export] public StringName TargetProperty { get; set; } = "";
	[Export] public Variant TargetValue { get; set; }
	[Export] public float Duration { get; set; }

	/// <summary>Seconds to wait before the tween starts.</summary>
	[Export] public float Delay { get; set; } = 0f;
	[Export] public Tween.EaseType EaseType { get; set; }
	[Export] public Tween.TransitionType TransitionType { get; set; }

	[Export] public bool AutoTween { get; set; } = true;
	[Export] public bool AutoReverseTween { get; set; } = false;

	// Adds TargetValue on top of the property instead of overwriting it, so it stacks with other changes.
	[Export] public bool Additive { get; set; } = false;

	
	// Plays the tween in the editor, then puts the property back to what it was.
	[ExportToolButton("Test Tween")] public Callable TestTweenButton => Callable.From(TestTween);
	

	private Variant _cachedOriginValue;
	private Tween? _currentTween;
	private float _lastWeight;
	private Variant _testRestoreValue;
	private bool _testing;

	public override void _EnterTree()
	{
		if (!IsInstanceValid(TweenTarget)) return;
		if (AutoTween && !Engine.IsEditorHint()) ExecuteTween();
	}

	private void TestTween()
	{
		if (!IsInstanceValid(TweenTarget)) return;

		// Pressing again mid-test keeps the value from before the first press.
		if (!_testing) _testRestoreValue = TweenTarget!.Get(TargetProperty);
		_testing = true;

		ExecuteTween();
		_currentTween!.TweenInterval(0.5f);
		_currentTween.TweenCallback(Callable.From(() =>
		{
			TweenTarget!.Set(TargetProperty, _testRestoreValue);
			_testing = false;
		}));
	}

	/// <summary>Stops the tween where it is (e.g. to start an out-animation on the same property).</summary>
	public void Stop()
	{
		if (_currentTween != null && _currentTween.IsValid()) _currentTween.Kill();
	}




	public void ExecuteTween()
	{
		if (!IsInstanceValid(TweenTarget)) return;

		if (_currentTween != null && _currentTween.IsValid()) _currentTween.Kill();

		_cachedOriginValue = TweenTarget!.Get(TargetProperty);

		_currentTween = CreateTween();
		_currentTween.SetEase(EaseType);
		_currentTween.SetTrans(TransitionType);
		_currentTween.Finished += () => EmitSignal(SignalName.Finished);

		if (Delay > 0f) _currentTween.TweenInterval(Delay);

		if (Additive)
		{
			_lastWeight = 0f;
			_currentTween.TweenMethod(Callable.From<float>(ApplyAdditive), 0f, 1f, Duration);
			if (AutoReverseTween)
				_currentTween.TweenMethod(Callable.From<float>(ApplyAdditive), 1f, 0f, Duration);
			return;
		}

		_currentTween.TweenProperty(TweenTarget, (string)TargetProperty, TargetValue, Duration);

		if (AutoReverseTween)
			_currentTween.TweenProperty(TweenTarget, (string)TargetProperty, _cachedOriginValue, Duration);
	}

	// Each frame, add only the change in weight since last frame, so other changes to the property are kept.
	private void ApplyAdditive(float weight)
	{
		float step = weight - _lastWeight;
		_lastWeight = weight;

		if (!IsInstanceValid(TweenTarget)) return;
		Variant current = TweenTarget!.Get(TargetProperty);
		switch (current.VariantType)
		{
			case Variant.Type.Float: TweenTarget.Set(TargetProperty, current.AsDouble() + TargetValue.AsDouble() * step); break;
			case Variant.Type.Vector2: TweenTarget.Set(TargetProperty, current.AsVector2() + TargetValue.AsVector2() * step); break;
			case Variant.Type.Vector3: TweenTarget.Set(TargetProperty, current.AsVector3() + TargetValue.AsVector3() * step); break;
			case Variant.Type.Color: TweenTarget.Set(TargetProperty, current.AsColor() + TargetValue.AsColor() * step); break;
			default: GD.PushWarning($"NodeTweener: additive mode doesn't support {current.VariantType}"); break;
		}
	}
}
