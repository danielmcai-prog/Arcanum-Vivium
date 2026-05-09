extends Node

const InMemorySpellDatabase = preload("res://addons/arcanum_vivum_spell_engine/database/in_memory_spell_database.gd")
const RuleBasedResolver = preload("res://addons/arcanum_vivum_spell_engine/resolvers/rule_based_resolver.gd")
const SpellEngineService = preload("res://addons/arcanum_vivum_spell_engine/core/spell_engine_service.gd")
const GameSpellSystem = preload("res://addons/arcanum_vivum_spell_engine/integration/game_spell_system.gd")

func _ready() -> void:
	var db = InMemorySpellDatabase.new()
	var resolver = RuleBasedResolver.new()
	var engine = SpellEngineService.new(db, resolver)
	var game = GameSpellSystem.new(engine)

	var strokes := [
		[
			{"x": 100.0, "y": 100.0},
			{"x": 150.0, "y": 50.0},
			{"x": 200.0, "y": 100.0},
			{"x": 180.0, "y": 150.0},
			{"x": 120.0, "y": 150.0}
		]
	]

	var result: Dictionary = game.cast_symbol_spell(strokes, "fire lance")
	print(result)
