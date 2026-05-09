/**
 * ARCANUM VIVUM — Spell Engine Library
 * 
 * Core exports for integrating into games and applications
 * No UI dependencies, framework-agnostic
 */

// Core Engine
export {
  FeatureExtractor,
  EmbeddingGenerator,
  BalancingSystem,
  EvolutionEngine,
  SpellDatabase,
  castSpell,
  SIMILARITY_THRESHOLDS
} from './core/engine.js';

// Audio Engine
export {
  AudioRecorderWithSilenceDetection,
  WakeWordDetector,
  transcribeAudio,
  castAudioSpell,
  resolveAudioSpell,
  AUDIO_CONFIG
} from './audio/index.js';

// Database Implementations
export {
  InMemorySpellDatabase,
  FirebaseSpellDatabase,
  SupabaseSpellDatabase,
  RestApiSpellDatabase
} from './database/index.js';
