class_name StateMachine extends Node


@export var current_state : State
@export var animation_player : AnimationPlayer
@export var print_transitions : bool = false
var previous_state : State
signal state_changed(state)



func _ready():
	await owner.ready
	for state : State in get_children():
		state.state_machine = self
		state.animation_player = animation_player

	
	if current_state:
		initialize_state(current_state)
	
func _physics_process(delta):
	if current_state:	
		current_state.process_state()	
	

	
func _unhandled_input(event):
	if current_state:
		current_state.handle_input(event)



func get_state(state_name : String) -> State: 
	if has_node(state_name):
		var new_state : State = get_node(state_name)
		return new_state	
	return null


func set_state(state_name : String):
	var new_state = get_state(state_name)
	
	
	call_deferred("transition_to_state", new_state)
	#transition_to_state(new_state)



func initialize_state(state : State):
	state.play_animation()
	state.enter_state()
	state.instantiate_enter_effect()
	
func transition_to_state(new_state:  State):
	
	if new_state:
			if print_transitions:
				print(new_state.name)
			if current_state == new_state or not new_state.can_enter(): return
			if current_state and not current_state.can_transition(new_state.name): return 
			
					
			previous_state = current_state
			previous_state.exit_state()
			current_state = new_state
			initialize_state(new_state)
				
		
			
			state_changed_to(new_state.name)
				



	
		
func state_changed_to(state_name):
	state_changed.emit(state_name)
