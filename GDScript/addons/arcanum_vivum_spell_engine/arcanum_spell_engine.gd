class_name ArcanumSpellEngine
extends RefCounted

const FeatureExtractor = preload("res://addons/arcanum_vivum_spell_engine/core/feature_extractor.gd")
const EmbeddingGenerator = preload("res://addons/arcanum_vivum_spell_engine/core/embedding_generator.gd")
const BalancingSystem = preload("res://addons/arcanum_vivum_spell_engine/core/balancing_system.gd")
const EvolutionEngine = preload("res://addons/arcanum_vivum_spell_engine/core/evolution_engine.gd")
const SpellEngineService = preload("res://addons/arcanum_vivum_spell_engine/core/spell_engine_service.gd")

const SpellDatabase = preload("res://addons/arcanum_vivum_spell_engine/database/spell_database.gd")
const InMemorySpellDatabase = preload("res://addons/arcanum_vivum_spell_engine/database/in_memory_spell_database.gd")
const RestApiSpellDatabase = preload("res://addons/arcanum_vivum_spell_engine/database/rest_api_spell_database.gd")
const FirebaseSpellDatabase = preload("res://addons/arcanum_vivum_spell_engine/database/cloud_spell_databases.gd")
const SupabaseSpellDatabase = preload("res://addons/arcanum_vivum_spell_engine/database/supabase_spell_database.gd")

const RuleBasedResolver = preload("res://addons/arcanum_vivum_spell_engine/resolvers/rule_based_resolver.gd")

const AudioConfig = preload("res://addons/arcanum_vivum_spell_engine/audio/audio_config.gd")
const WakeWordDetector = preload("res://addons/arcanum_vivum_spell_engine/audio/wake_word_detector.gd")
const AudioSpellService = preload("res://addons/arcanum_vivum_spell_engine/audio/audio_spell_service.gd")
const WhisperTranscriber = preload("res://addons/arcanum_vivum_spell_engine/audio/whisper_transcriber.gd")

const GameSpellSystem = preload("res://addons/arcanum_vivum_spell_engine/integration/game_spell_system.gd")
