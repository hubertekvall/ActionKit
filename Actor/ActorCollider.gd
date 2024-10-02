class_name ActorCollider extends CollisionObject3D

var damage_callback : Callable



func receive_damage(weapon_effect : WeaponEffect, incoming_damage : Damage):
	damage_callback.call(weapon_effect, incoming_damage)
