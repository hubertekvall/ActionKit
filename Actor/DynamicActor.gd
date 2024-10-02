class_name DynamicActor extends Actor



@onready var body : CharacterBody3D 

func _ready() -> void:
	var actor_body = (self as CollisionObject3D)
	assert(actor_body is CharacterBody3D)
	body = actor_body
	
	
	
