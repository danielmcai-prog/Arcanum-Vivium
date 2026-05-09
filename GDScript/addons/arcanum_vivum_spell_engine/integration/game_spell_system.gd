class_name GameSpellSystem
extends RefCounted

var _engine: SpellEngineService
var active_mana: int = 100
var max_mana: int = 100
var _cooldowns: Dictionary = {}

func _init(engine: SpellEngineService, starting_mana: int = 100, max_mana_value: int = 100) -> void:
	_engine = engine
	active_mana = starting_mana
	max_mana = max_mana_value

func cast_symbol_spell(strokes: Array, incantation: String) -> Dictionary:
	var result: Dictionary = _engine.cast_spell(strokes, incantation)
	if result.is_empty():
		return {"success": false, "error": "Resolver returned no spell"}

	var spell: Dictionary = result.get("spell", {})
	if int(spell.get("mana_cost", 0)) > active_mana:
		return {"success": false, "error": "Not enough mana"}

	var now_ms := int(Time.get_unix_time_from_system() * 1000)
	var spell_id := spell.get("spell_id", "")
	var last_cast := int(_cooldowns.get(spell_id, 0))
	if now_ms - last_cast < int(float(spell.get("cooldown", 0.0)) * 1000.0):
		return {"success": false, "error": "Spell on cooldown"}

	active_mana -= int(spell.get("mana_cost", 0))
	_cooldowns[spell_id] = now_ms

	var damage := float(spell.get("mana_cost", 0)) * float(spell.get("stability", 1.0))
	var will_misfire := randf() < float(spell.get("instability_prob", 0.0))
	return {
		"success": true,
		"spell": spell,
		"match_type": result.get("match_type", "NEW"),
		"match_score": result.get("match_score", 0.0),
		"damage": damage,
		"will_misfire": will_misfire,
		"remaining_mana": active_mana
	}

func restore_mana(amount: int) -> void:
	active_mana = min(max_mana, active_mana + max(amount, 0))
