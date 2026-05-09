class_name AudioSpellService
extends RefCounted

func cast_audio_spell(transcribed_text: String, resolver: RefCounted, audio_bytes: PackedByteArray = PackedByteArray()) -> Dictionary:
	var resolved: Dictionary = resolver.resolve_audio(transcribed_text)
	if resolved.is_empty():
		return {}

	var embedding := PackedFloat32Array()
	embedding.resize(32)
	var words := transcribed_text.to_lower().split(" ", false)
	for i in range(min(words.size(), 32)):
		if words[i].length() > 0:
			embedding[i] = float(words[i].unicode_at(0) % 100) / 100.0

	var spell := {
		"spell_id": _build_audio_spell_id(),
		"symbol_embedding": embedding,
		"semantic_tags": resolved.get("semantic_tags", []),
		"spell_name": resolved.get("spell_name", "Unnamed Audio Spell"),
		"element": resolved.get("element", "arcane"),
		"force": resolved.get("force", "projection"),
		"scale": resolved.get("scale", "medium"),
		"delivery": resolved.get("delivery", "beam"),
		"intent": resolved.get("intent", "offensive"),
		"effect_description": resolved.get("effect_description", ""),
		"lore_hint": resolved.get("lore_hint", ""),
		"grammar_analysis": resolved.get("grammar_analysis", ""),
		"misfire_effect": resolved.get("misfire_effect", ""),
		"mana_cost": int(resolved.get("base_power", 20)),
		"stability": max(0.5, 1.0 - float(resolved.get("ambiguity", 0.1))),
		"instability_prob": float(resolved.get("ambiguity", 0.1)),
		"cooldown": float(resolved.get("base_power", 20)) / 40.0,
		"usage_count": 1,
		"community_consensus": 0.0,
		"variants": [],
		"variant_of": "",
		"created_at_unix_ms": Time.get_unix_time_from_system() * 1000,
		"incantation": transcribed_text,
		"audio_base64": Marshalls.raw_to_base64(audio_bytes),
		"source": "voice"
	}

	return {
		"spell": spell,
		"match_type": "NEW",
		"match_score": 0.0,
		"features": {}
	}

func _build_audio_spell_id() -> String:
	var millis := int(Time.get_unix_time_from_system() * 1000)
	var random := randi() % 0xFFFFF
	return "spell_audio_%s_%s" % [String.num_int64(millis, 36), String.num_int64(random, 36)]
