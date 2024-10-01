class_name OneShotParticle extends Effect


@export var particles : CPUParticles3D
func _ready():
	super._ready()
	particles.one_shot = true
	particles.emitting = true
	particles.finished.connect(finish_effect)
