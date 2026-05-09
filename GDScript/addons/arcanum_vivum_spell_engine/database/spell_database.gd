class_name SpellDatabase
extends RefCounted

func search(_embedding: PackedFloat32Array, _top_k: int = 3) -> Array:
	push_error("SpellDatabase.search() must be implemented")
	return []

func upsert(_spell: Dictionary) -> void:
	push_error("SpellDatabase.upsert() must be implemented")

func get_spell(_spell_id: String) -> Dictionary:
	push_error("SpellDatabase.get_spell() must be implemented")
	return {}

func get_all() -> Array:
	push_error("SpellDatabase.get_all() must be implemented")
	return []

func size() -> int:
	push_error("SpellDatabase.size() must be implemented")
	return 0
