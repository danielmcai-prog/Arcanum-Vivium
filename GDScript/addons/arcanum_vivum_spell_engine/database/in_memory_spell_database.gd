class_name InMemorySpellDatabase
extends SpellDatabase

const EmbeddingGenerator = preload("res://addons/arcanum_vivum_spell_engine/core/embedding_generator.gd")

var _spells: Dictionary = {}

func search(embedding: PackedFloat32Array, top_k: int = 3) -> Array:
	var hits: Array = []
	for spell_id in _spells.keys():
		var spell: Dictionary = _spells[spell_id]
		var score := EmbeddingGenerator.cosine(embedding, PackedFloat32Array(spell.get("symbol_embedding", [])))
		hits.append({"id": spell_id, "score": score})
	hits.sort_custom(func(a: Dictionary, b: Dictionary): return a["score"] > b["score"])
	if hits.size() > top_k:
		hits.resize(top_k)
	return hits

func upsert(spell: Dictionary) -> void:
	_spells[spell.get("spell_id", "")] = spell.duplicate(true)

func get_spell(spell_id: String) -> Dictionary:
	if not _spells.has(spell_id):
		return {}
	return _spells[spell_id].duplicate(true)

func get_all() -> Array:
	var out: Array = []
	for spell in _spells.values():
		out.append((spell as Dictionary).duplicate(true))
	return out

func size() -> int:
	return _spells.size()
