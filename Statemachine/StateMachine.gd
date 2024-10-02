class_name StateMachine extends Node


@export var current_state : State
@export var animation_player : AnimationPlayer
@export var print_transitions : bool = false
signal state_changed(state)



func _ready():
	await owner.ready
	for state : State in find_children("*", "State"):
		state.state_machine = self
		state.animation_player = animation_player

	
	if current_state:
		current_state.initialize("", {})
	
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






	
func transition_to_state(state_name:  String, blackboard : Dictionary = {}):
	
	var new_state : State = get_state(state_name)
	
	if new_state and current_state:
			if print_transitions:
				print(state_name)
			# current_state != new_state and
			if  new_state.can_enter() and current_state.can_exit(state_name):
				var previous_state = current_state
				previous_state.exit_state()
				current_state = new_state
				new_state.initialize(previous_state.name, blackboard)
				state_changed.emit(new_state)
				


	
