class_name FirebaseSpellDatabase
extends RestApiSpellDatabase

func _init(cloud_function_base_url: String, headers: Dictionary = {}) -> void:
	super._init(cloud_function_base_url, headers)
