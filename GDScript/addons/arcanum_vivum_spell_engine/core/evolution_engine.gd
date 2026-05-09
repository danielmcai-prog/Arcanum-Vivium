class_name EvolutionEngine
extends RefCounted

static func reinforce(spell: Dictionary, db: SpellDatabase) -> void:
	spell["usage_count"] = int(spell.get("usage_count", 0)) + 1
	spell["community_consensus"] = min(1.0, log(float(spell["usage_count"]) + 1.0) / log(10.0) / 4.0)
	spell["stability"] = snappedf(float(spell.get("stability", 0.9)) * 0.9 + float(spell["community_consensus"]) * 0.1, 0.001)
	if int(spell["usage_count"]) > 100:
		spell["mana_cost"] = max(5, int(spell.get("mana_cost", 10)) - 1)
	db.upsert(spell)

static func check_variant(existing: Dictionary, incoming: Dictionary, score: float) -> void:
	if score < 0.82 or score >= 0.92:
		return
	var variants: Array = existing.get("variants", [])
	if not variants.has(incoming.get("spell_id", "")):
		variants.append(incoming.get("spell_id", ""))
		existing["variants"] = variants
		incoming["variant_of"] = existing.get("spell_id", "")
