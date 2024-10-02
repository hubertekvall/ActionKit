class_name WeaponEffect extends Effect


var damping : float = 0.0
var speed : float  = 1.0

	
func initialize_effect():
	super.initialize_effect()
	speed = config.speed
	damping = config.damping

func _physics_process(delta : float):
	speed_damp()
	move_effect(delta)
		
func move_effect(delta : float):
	position += -transform.basis.z * speed  * delta
	
func speed_damp():
	speed = move_toward(speed, 0.0, damping)
		
func resolve_collision (colliding_body : PhysicsBody3D):
	if colliding_body.has_method("receive_damage"):
		if colliding_body.collision_layer & config.damage_exclusion == 0:
			colliding_body.receive_damage(self, config.damage)
			
	if config.finish_on_impact:
		finish_effect()


	
