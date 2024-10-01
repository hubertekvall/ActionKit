class_name SoundEffect extends Effect

@export var audio_player : AudioStreamPlayer

func _ready():
	super._ready()
	audio_player.play()
	audio_player.finished.connect(queue_free)
