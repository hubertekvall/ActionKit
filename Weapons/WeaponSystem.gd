class_name WeaponSystem extends Node

@export var state_machine : StateMachine

var weapons : Array[Weapon]
var current_weapon : Weapon
var current_index : int :
	get:
		return current_index
	set(value):
		current_index = value
		current_index = wrap(current_index, 0, weapons.size())


func _unhandled_input(event):
	if event.is_action_pressed("scroll_up"):
		current_index += 1
		current_weapon = weapons[current_index]
	
	elif event.is_action_pressed("scroll_down"):
		current_index -= 1
		current_weapon = weapons[current_index]

func _ready():
	await owner.ready
	for weapon in get_node("Weapons").get_children():
		weapons.append(weapon)
	
	current_weapon = weapons[0]	

	

func fire_current_weapon():
	if current_weapon:
		current_weapon.fire()
