#nullable enable
using Godot;

namespace ActionKit.Utilities;

[GlobalClass]
[Tool]
public partial class NodeTweener : Node
{
	[Export] public Node? TweenTarget { get; set; }
	[Export] public StringName TargetProperty { get; set; } = "";
	[Export] public Variant TargetValue { get; set; }
	[Export] public float Duration { get; set; }
	[Export] public Tween.EaseType EaseType { get; set; }
	[Export] public Tween.TransitionType TransitionType { get; set; }

	[Export] public bool AutoTween { get; set; } = true;
	[Export] public bool AutoReverseTween { get; set; } = false;

	// Adds TargetValue on top of the property instead of overwriting it, so it stacks with other changes.
	[Export] public bool Additive { get; set; } = false;

	
	[ExportToolButton("ExecuteTween")] public Callable ExecuteTweenButton => Callable.From(ExecuteTween);
	

	private Variant _cachedOriginValue;
	private Tween? _currentTween;
	private float _lastWeight;

	public override void _EnterTree()
	{
		if (!IsInstanceValid(TweenTarget)) return;
		if (AutoTween) ExecuteTween();
	}




	public void ExecuteTween()
	{
		if (!IsInstanceValid(TweenTarget)) return;

		if (_currentTween != null && _currentTween.IsValid()) _currentTween.Kill();

		_cachedOriginValue = TweenTarget!.Get(TargetProperty);

		_currentTween = CreateTween();
		_currentTween.SetEase(EaseType);
		_currentTween.SetTrans(TransitionType);

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
