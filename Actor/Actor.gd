class_name Actor extends CharacterBody3D



@export var state_machine : StateMachine
@export var health : float = 1.0 
@export var base_defense : float = 0.0
@export_flags_3d_physics var layer : int
@export var on_death_effect : EffectConfiguration
@export var on_damage_effect : EffectConfiguration
@export var on_spawn_effect : EffectConfiguration

signal death(actor : Actor)
signal hit(actor : Actor)


var as_character : CharacterBody3D:
	get:
		return self

		


			
			
func _ready():
	if on_spawn_effect:
		on_spawn_effect.instantiate(get_viewport(), global_position, global_basis)
		
	collision_layer = layer



func receive_damage(weapon_effect : WeaponEffect, incoming_damage : Damage):
	# If for some reason we collide with damage when we're dead, just return
	if not health > 0:
		return false
	
	
	var damage_value : float = incoming_damage.damage
	var distance_attenuation : float = incoming_damage.distance_attenuation
	var distance : float = global_position.distance_to(weapon_effect.global_position)
	var normalized_distance : float = clampf(distance, 0.0, 10.0 ) / 10.0
	var attenuated_damage = damage_value * (1 - (distance_attenuation * normalized_distance))
	
	
	health -= attenuated_damage
	
	if health < 1:
		on_death()

	on_hit(attenuated_damage)
	return true	


	

func _notification(what):
	match(what):
		NOTIFICATION_PREDELETE: death.emit(self)




func on_hit(damage : float):
	if on_damage_effect:
		on_damage_effect.instantiate(get_viewport(), global_position, global_basis)
		
	state_machine.set_state("Hit")
	
	hit.emit(self)

func on_death():
	if on_death_effect:
		var new_effect = on_death_effect.instantiate(get_viewport(), global_position, global_basis)
	
	state_machine.set_state("Death")	
	
	if state_machine.animation_player:
		state_machine.animation_player.animation_finished.connect(func(animation): queue_free())
	else:
		queue_free()
	


func slow_call(function : Callable, tick : int):
	if Engine.get_physics_frames() % tick == 0:
		function.call()
