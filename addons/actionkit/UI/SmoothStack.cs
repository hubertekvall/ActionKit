#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ActionKit.UI;

[Tool]
[GlobalClass]
public partial class SmoothStack : Control
{
	private float _verticalGap;
	private Vector2 _topOffset;
	private readonly Stack<Node> _pendingInsertions = new();
	private Tween? _reflowTween;
	private Control? _pendingEntry;
	private double _popTimer;

	[Export] public double PopTimer { get; set; } = 0.5;
	[Export] public bool GrowUp { get; set; }

	[Export]
	public float VerticalGap
	{
		get => _verticalGap;
		set { _verticalGap = value; UpdateOffsets(); }
	}

	

	public override void _Ready() => UpdateOffsets();

	public override void _Process(double delta)
	{
		_popTimer += delta;
		if (_popTimer < PopTimer) return;

		while (_pendingInsertions.TryPop(out var insertion))
		{
			if (!IsInstanceValid(insertion)) continue;
			AddChild(insertion);
			break;
		}

		_popTimer = 0.0;
	}

	public void PushItem(Node item) => _pendingInsertions.Push(item);

	public override void _EnterTree()
	{
		ChildEnteredTree += OnChildEnteredTree;
		ChildExitingTree += OnChildExitingTree;
	}

	public override void _ExitTree()
	{
		ChildEnteredTree -= OnChildEnteredTree;
		ChildExitingTree -= OnChildExitingTree;
	}

	private void OnChildEnteredTree(Node node)
	{
		if (node is Control control)
		{
			control.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
			_pendingEntry = control;
		}

		UpdateOffsets();
	}

	private void OnChildExitingTree(Node node) => UpdateOffsets();

	public void UpdateOffsets()
	{
		if (IsInsideTree()) CallDeferred(MethodName.TweenOffsets);
	}

	public void TweenOffsets()
	{
		_reflowTween?.Kill();
		_reflowTween = null;

		var controls = GetChildren().OfType<Control>().ToList();
		
		var sign = GrowUp ? -1f : 1f;
		var cursor = 0f;

		foreach (var control in controls)
		{
			
			var target = cursor * sign;
			// Growing up: anchor the bottom edge at `target` rather than the top edge.
			var position = new Vector2(0, GrowUp ? target - control.Size.Y : target);
		

		

			if (control == _pendingEntry)
				control.Position = position - new Vector2(0, (control.Size.Y + _verticalGap) * sign);

			_reflowTween ??= CreateTween().SetParallel().SetTrans(Tween.TransitionType.Cubic);
			_reflowTween.TweenProperty(control, "position", position, 0.25f);

			cursor += control.Size.Y + _verticalGap;
		}

		_pendingEntry = null;
	}
}
