#nullable enable
using System.Collections.Generic;
using System.Linq;
using ActionKit.Utilities;
using Godot;

namespace ActionKit.StateMachines;

/// <summary>
/// A flat finite state machine. Its State children are the states; RootState (or the first
/// one) is entered on start. The current state is ticked on physics frames and gets
/// unhandled input. Works under an Actor (states get it as Actor) or on its own.
/// </summary>
[GlobalClass]
public partial class StateMachine : Node
{
	[Export] public State? RootState { get; set; }

	public Actor? Actor { get; private set; }
	public State? CurrentState { get; private set; }
	public State? PreviousState { get; private set; }

	private readonly List<State> _states = [];
	private bool _isInitialized;

	[Signal]
	public delegate void StateChangedEventHandler(State? from, State to);

	public override void _Ready()
	{
		if (Engine.IsEditorHint()) return;

		Actor = this.FindAncestorOfType<Actor>();

		// Deferred so the owning actor (whose _Ready runs after ours) has bound its
		// components before any state looks at them.
		Callable.From(Initialize).CallDeferred();
	}

	private void Initialize()
	{
		foreach (var state in GetChildren().OfType<State>())
		{
			_states.Add(state);
			state.Initialize(this, Actor);
		}

		_isInitialized = true;
		TransitionTo(RootState ?? _states.FirstOrDefault());
	}

	/// <summary>First state of type T (IdleState, RunState, ...).</summary>
	public T? GetState<T>() where T : State => _states.OfType<T>().FirstOrDefault();

	/// <summary>State by node name.</summary>
	public State? GetState(string name) => _states.Find(s => s.Name == name);

	public void TransitionTo(State? nextState)
	{
		if (nextState is null || nextState == CurrentState) return;
		if (CurrentState != null && !CurrentState.CanTransitionTo(nextState)) return;

		CurrentState?.ExitState();
		PreviousState = CurrentState;

		CurrentState = nextState;
		CurrentState.TransitioningFrom(PreviousState);
		EmitSignalStateChanged(PreviousState, CurrentState);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_isInitialized) return;
		CurrentState?.Tick(delta);
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (!_isInitialized) return;
		CurrentState?.OnInput(inputEvent);

		foreach (var state in _states)
			state.CheckTransition(inputEvent);
	}
}
