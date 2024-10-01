class_name KinematicWeaponEffect extends WeaponEffect



@export var effect_actor : Actor
# Called when the node enters the scene tree for the first time.


func initalize_collision():
	effect_actor.collision_mask = config.target_layer



func move_effect(delta:float):
	effect_actor.move_and_slide()
	
	if effect_actor.get_slide_collision_count():
		var collider = effect_actor.get_slide_collision(0).get_collider()
		resolve_collision(collider)	
	
	
