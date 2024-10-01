class_name AreaWeaponEffect extends WeaponEffect


@export var collision_zone : Area3D





func initialize_effect():
	super.initialize_effect()
	collision_zone.body_entered.connect(resolve_collision)
	collision_zone.collision_mask = config.target_layer
	

		
