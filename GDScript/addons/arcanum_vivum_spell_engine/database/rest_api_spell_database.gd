class_name RestApiSpellDatabase
extends SpellDatabase

var _base_url: String
var _headers: Dictionary

func _init(base_url: String, headers: Dictionary = {}) -> void:
	_base_url = base_url.rstrip("/")
	_headers = headers.duplicate(true)
	if not _headers.has("Content-Type"):
		_headers["Content-Type"] = "application/json"

func search(embedding: PackedFloat32Array, top_k: int = 3) -> Array:
	# Godot networking is asynchronous; implement with HTTPRequest in your game layer.
	# This method is intentionally a sync placeholder to keep interface parity.
	push_warning("RestApiSpellDatabase.search() requires async HTTPRequest implementation in project context")
	return []

func upsert(spell: Dictionary) -> void:
	push_warning("RestApiSpellDatabase.upsert() requires async HTTPRequest implementation in project context")

func get_spell(spell_id: String) -> Dictionary:
	push_warning("RestApiSpellDatabase.get_spell() requires async HTTPRequest implementation in project context")
	return {}

func get_all() -> Array:
	push_warning("RestApiSpellDatabase.get_all() requires async HTTPRequest implementation in project context")
	return []

func size() -> int:
	push_warning("RestApiSpellDatabase.size() requires async HTTPRequest implementation in project context")
	return 0
