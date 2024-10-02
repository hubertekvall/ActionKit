class_name State extends Node


var state_machine : StateMachine
var previous_state : String
var blackboard : Dictionary

@export var animation_player : AnimationPlayer
@export var animation_name : String
@export var enter_effect : EffectConfiguration
@export var exit_effect : EffectConfiguration





func instantiate_enter_effect():
	if enter_effect:
		enter_effect.instantiate(get_viewport(), owner.global_position, owner.global_basis)

func initialize(previous_state : String, blackboard : Dictionary = {}):
	self.previous_state = previous_state
	self.blackboard = blackboard
	
	instantiate_enter_effect()
	play_animation()
	enter_state()

func play_animation():
	if animation_player and animation_name:
		animation_player.play(animation_name)

# Is there something that blocks us from entering
func can_enter() -> bool:
	return true

# Are we locked in an animation
func can_exit(next_state : String) -> bool:
	return true

func enter_state():
	pass

func handle_input(event):
	pass

func process_state():
	pass
	
func exit_state():
	if exit_effect:
		exit_effect.instantiate(get_viewport(), owner.global_position, owner.global_basis)


func slow_call(function : Callable, tick : int):
	if Engine.get_physics_frames() % tick == 0:
		function.call()
