#nullable enable
using Godot;

namespace ActionKit.StateMachines;

/// <summary>
/// One state of a StateMachine. Subclass it and override the hooks. Optionally plays an
/// animation on enter, and can be entered directly by an input action (TransitionAction).
/// </summary>
[GlobalClass]
public partial class State : Node
{
	[Export] public string AnimationName { get; set; } = "";
	[Export] public AnimationPlayer? AnimationPlayer { get; set; }
	[Export] public double BlendTime { get; set; } = -1;

	/// <summary>When set, this action (pressed or released, as configured) transitions here.</summary>
	[Export] public InputEventAction? TransitionAction { get; set; }

	public StateMachine StateMachine { get; set; } = null!;

	/// <summary>The actor the machine belongs to, if any.</summary>
	public Actor? Actor { get; private set; }

	public virtual void Initialize(StateMachine stateMachine, Actor? actor)
	{
		StateMachine = stateMachine;
		Actor = actor;
	}

	public virtual void TransitioningFrom(State? previousState)
	{
		PlayAnimation();
		EnterState(previousState);
	}

	protected virtual void PlayAnimation()
	{
		if (AnimationPlayer != null && !string.IsNullOrEmpty(AnimationName))
			AnimationPlayer.Play(AnimationName, BlendTime);
	}

	public virtual void Tick(double delta) { }

	public virtual void OnInput(InputEvent inputEvent) { }

	public virtual void EnterState(State? previousState) { }

	public virtual void ExitState() { }

	/// <summary>Veto a transition out of this state.</summary>
	public virtual bool CanTransitionTo(State nextState) => true;

	public void CheckTransition(InputEvent inputEvent)
	{
		if (TransitionAction is null) return;
		if (!inputEvent.IsAction(TransitionAction.Action)) return;
		if (inputEvent.IsPressed() != TransitionAction.Pressed) return;

		StateMachine.TransitionTo(this);
	}
}
