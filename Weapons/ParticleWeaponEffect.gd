class_name ParticleWeaponEffect extends WeaponEffect

@export var particle_system : CPUParticles3D

#
#func configure_lifetime():
	#particle_system.lifetime = config.lifetime / particle_system.amount
	#particle_system.finished.connect(trigger_impact)
	#particle_system.emitting = true

	
