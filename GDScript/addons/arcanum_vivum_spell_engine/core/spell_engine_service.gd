class_name SpellEngineService
extends RefCounted

const FeatureExtractor = preload("res://addons/arcanum_vivum_spell_engine/core/feature_extractor.gd")
const EmbeddingGenerator = preload("res://addons/arcanum_vivum_spell_engine/core/embedding_generator.gd")
const BalancingSystem = preload("res://addons/arcanum_vivum_spell_engine/core/balancing_system.gd")
const EvolutionEngine = preload("res://addons/arcanum_vivum_spell_engine/core/evolution_engine.gd")

const REUSE_THRESHOLD := 0.92
const VARIANT_THRESHOLD := 0.82
const RELATED_THRESHOLD := 0.65

var _database: SpellDatabase
var _resolver: RefCounted

func _init(database: SpellDatabase, resolver: RefCounted) -> void:
	_database = database
	_resolver = resolver

func cast_spell(strokes: Array, incantation: String) -> Dictionary:
	var features: Dictionary = FeatureExtractor.extract(strokes)
	var embedding: PackedFloat32Array = EmbeddingGenerator.generate(features, incantation)
	var hits: Array = _database.search(embedding, 3)
	var best: Dictionary = hits[0] if not hits.is_empty() else {}

	if not best.is_empty() and float(best.get("score", 0.0)) >= REUSE_THRESHOLD:
		var reused: Dictionary = _database.get_spell(best.get("id", ""))
		if reused.is_empty():
			return {}
		EvolutionEngine.reinforce(reused, _database)
		return {
			"spell": reused,
			"match_type": "REUSE",
			"match_score": float(best.get("score", 0.0)),
			"features": features
		}

	var resolved: Dictionary = _resolver.resolve_symbol(features, incantation)
	if resolved.is_empty():
		return {}

	var balancing: Dictionary = BalancingSystem.compute(features, resolved)
	var spell_id := _build_spell_id()
	var spell := {
		"spell_id": spell_id,
		"symbol_embedding": embedding,
		"semantic_tags": resolved.get("semantic_tags", []),
		"spell_name": resolved.get("spell_name", "Unnamed Spell"),
		"element": resolved.get("element", "arcane"),
		"force": resolved.get("force", "projection"),
		"scale": resolved.get("scale", "medium"),
		"delivery": resolved.get("delivery", "projectile"),
		"intent": resolved.get("intent", "utility"),
		"effect_description": resolved.get("effect_description", ""),
		"lore_hint": resolved.get("lore_hint", ""),
		"grammar_analysis": resolved.get("grammar_analysis", ""),
		"misfire_effect": resolved.get("misfire_effect", ""),
		"mana_cost": balancing.get("mana_cost", 10),
		"stability": balancing.get("stability", 0.9),
		"instability_prob": balancing.get("instability_prob", 0.05),
		"cooldown": balancing.get("cooldown", 1.0),
		"usage_count": 1,
		"community_consensus": 0.0,
		"variants": [],
		"variant_of": "",
		"created_at_unix_ms": Time.get_unix_time_from_system() * 1000,
		"incantation": incantation,
		"source": "symbol",
		"features": {
			"symmetry": features.get("symmetry", 0.0),
			"enclosure": features.get("enclosure", 0.0),
			"spirality": features.get("spirality", 0.0),
			"complexity": features.get("complexity", 0.0),
			"angularity": features.get("angularity", 0.0)
		}
	}

	var match_type := "NEW"
	var match_score: float = float(best.get("score", 0.0))
	if not best.is_empty() and match_score >= VARIANT_THRESHOLD:
		var parent := _database.get_spell(best.get("id", ""))
		if not parent.is_empty():
			EvolutionEngine.check_variant(parent, spell, match_score)
			_database.upsert(parent)
		match_type = "VARIANT"
	elif not best.is_empty() and match_score >= RELATED_THRESHOLD:
		match_type = "RELATED"

	_database.upsert(spell)
	return {
		"spell": spell,
		"match_type": match_type,
		"match_score": match_score,
		"features": features
	}

func _build_spell_id() -> String:
	var millis := int(Time.get_unix_time_from_system() * 1000)
	var random := randi() % 0xFFFFF
	return "spell_%s_%s" % [String.num_int64(millis, 36), String.num_int64(random, 36)]
