extends Node


signal score_event(value : int)
signal score_changed(value : int)
signal combo_changed(value :int) 
signal reset_combo
signal shake_event(value : int, position : Vector3)


signal player_state_changed(state)	
signal boost()
signal dodge()

signal monster_created(monster)
signal monster_died(monster)
signal monster_hit(position : Vector3, monster : Monster)


signal instantiate_scene(scene : PackedScene, position : Vector3, basis : Basis)	


signal player_health_changed(value, max_health )

	

func monster_damaged(position : Vector3, damage : float, monster : Monster):
	var damage_marker = preload("res://Scenes/UI/ScoreEffects/DamageMarker.tscn").instantiate()
	damage_marker.value = int(damage)
	damage_marker.world_origin = position
	Globals.game_world.add_child(damage_marker)
	
	monster_hit.emit(position, monster)
