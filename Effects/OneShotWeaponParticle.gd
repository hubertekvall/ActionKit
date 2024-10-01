class_name OneShotWeaponParticle extends AreaWeaponEffect


@export var particles : CPUParticles3D
func _ready():
	super._ready()
	particles.emitting = true
	particles.finished.connect(finish_effect)
	
func set_finish_timer():
	if config.lifetime:
		particles.one_shot = false 
		particles.lifetime = config.lifetime / particles.amount
		
		get_tree().create_timer(config.lifetime).timeout.connect(terminate_particles)

func terminate_particles():
	particles.one_shot = true


	
