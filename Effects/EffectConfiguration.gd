class_name EffectConfiguration extends Resource



@export_group("This effect")
@export var effect : PackedScene
@export var lifetime : float = 0.0
@export_group("Chained effect")
@export var parallel : bool = false
@export var next_effect : EffectConfiguration




func instantiate(viewport : Viewport, position : Vector3,  orientation : Basis):
	if effect:
		var new_effect : Effect = effect.instantiate() as Effect
		new_effect.config = self
		new_effect.position = position
		new_effect.basis = orientation
		viewport.add_child(new_effect)
	
		return new_effect
