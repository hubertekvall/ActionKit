class_name Weapon extends Node3D

@export var weapon_configuration : WeaponEffectConfiguration
@export var auto_fire : bool = true
@export var rhythm : Array[float] = [0.5]
@export var x_spread : float = 0.0
@export var y_spread : float = 0.0

@export var shake_when_firing : bool = false

var ridx : int = 0

var can_fire : bool = true



	
func enable_fire():
	can_fire = true	

func fire():
	if can_fire:
		if shake_when_firing:
			EventBus.shake_event.emit(Impact.Magnitude.LOW, global_position)			
		
		can_fire = false
		weapon_configuration.instantiate(get_viewport(), global_position + (Vector3(randf_range(-1.0, 1.0) * x_spread, randf_range(-1.0, 1.0)* y_spread,0)), global_basis)
		
		
		ridx = wrapi(ridx + 1, 0, rhythm.size())
		var rhythm_time = rhythm[ridx]
		get_tree().create_timer(rhythm_time).timeout.connect(enable_fire)
		
		
