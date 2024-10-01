class_name Effect extends Node3D


@export var config : EffectConfiguration


func _ready():
	if not config:
		config = EffectConfiguration.new()
		

	
	initialize_effect()
	if config.lifetime:
		set_finish_timer()
	


func initialize_effect():
	if config.parallel and config.next_effect:
		config.next_effect.instantiate(get_viewport(), global_position, global_basis)


func set_finish_timer():
	get_tree().create_timer(config.lifetime).timeout.connect(finish_effect)		


func instantiate_next_effect():
	if config.next_effect and not config.parallel: 
		config.next_effect.instantiate(get_viewport(), global_position, global_basis)	


func finish_effect():
	instantiate_next_effect()
	queue_free()
