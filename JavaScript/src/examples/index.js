/**
 * ARCANUM VIVUM — Engine Usage Examples
 * 
 * Shows how to integrate the spell engine into games and applications
 */

import {
  FeatureExtractor,
  EmbeddingGenerator,
  castSpell,
  BalancingSystem,
  AudioRecorderWithSilenceDetection,
  WakeWordDetector,
  transcribeAudio,
  castAudioSpell,
  InMemorySpellDatabase,
  SIMILARITY_THRESHOLDS
} from '../index.js';

// ─── EXAMPLE 1: Basic Symbol Casting ────────────────────────────────
export async function example_symbolCasting() {
  // Setup
  const database = new InMemorySpellDatabase();
  
  // Mock Claude resolver
  const claudeResolver = async (features, incantation) => ({
    spell_name: "Fireball",
    element: "fire",
    force: "explosion",
    scale: "large",
    delivery: "projectile",
    intent: "offensive",
    mana_base: 25,
    ambiguity: 0.1,
    base_power: 30,
    semantic_tags: ["fire", "explosion", "damage"],
    effect_description: "Launch a ball of fire at enemies",
    lore_hint: "The oldest spell in the grimoire",
    grammar_analysis: "Sharp angular strokes indicate fire",
    misfire_effect: "Caster takes damage"
  });

  // Simulate drawing strokes
  const strokes = [
    [
      { x: 100, y: 100 }, { x: 150, y: 150 }, { x: 200, y: 100 }
    ]
  ];

  // Cast spell
  const result = await castSpell({
    strokes,
    incantation: "fireball",
    db: database,
    resolver: claudeResolver
  });

  console.log("Spell cast:", result.spell.spell_name);
  console.log("Match type:", result.matchType);
  console.log("Mana cost:", result.spell.mana_cost);
  console.log("Stability:", result.spell.stability);
  
  return result;
}

// ─── EXAMPLE 2: Audio Spell Casting ────────────────────────────────
export async function example_audioCasting() {
  const database = new InMemorySpellDatabase();
  
  // Mock resolver for audio spells
  const audioResolver = async (transcribedText) => ({
    spell_name: "Arcane Blast",
    element: "arcane",
    force: "projection",
    scale: "medium",
    delivery: "beam",
    intent: "offensive",
    mana_base: 20,
    ambiguity: 0.15,
    base_power: 25,
    semantic_tags: ["arcane", "beam", "magic"],
    effect_description: "Fire an arcane projectile",
    lore_hint: "Pure magical energy unleashed",
    grammar_analysis: "Strong command in voice",
    misfire_effect: "Spell rebounds to caster"
  });

  // Simulate recording and transcription
  const recorder = new AudioRecorderWithSilenceDetection();
  const wakeWordDetector = new WakeWordDetector("arcanum");

  // In real usage:
  // await recorder.start();
  // const audioBlob = await recorder.stop();
  // const text = await transcribeAudio(audioBlob, apiKey);
  
  // For this example, we'll mock the transcription
  const transcribedText = "Arcane Blast";

  const result = await castAudioSpell({
    transcribedText,
    resolver: audioResolver,
    audioBlob: null
  });

  console.log("Audio spell cast:", result.spell.spell_name);
  return result;
}

// ─── EXAMPLE 3: Game Integration ────────────────────────────────────
export class GameSpellSystem {
  constructor(database, claudeResolver, config = {}) {
    this.database = database;
    this.claudeResolver = claudeResolver;
    this.config = {
      spellCooldowns: new Map(),
      activeMana: 100,
      maxMana: 100,
      ...config
    };
  }

  async castSymbolSpell(strokes, incantation) {
    const result = await castSpell({
      strokes,
      incantation,
      db: this.database,
      resolver: this.claudeResolver
    });

    if (!result) return null;

    // Apply game logic
    if (result.spell.mana_cost > this.config.activeMana) {
      return { error: "Not enough mana" };
    }

    // Check cooldown
    const now = Date.now();
    const lastCast = this.config.spellCooldowns.get(result.spell.spell_id) || 0;
    if (now - lastCast < result.spell.cooldown * 1000) {
      return { error: "Spell on cooldown" };
    }

    // Consume mana
    this.config.activeMana -= result.spell.mana_cost;
    this.config.spellCooldowns.set(result.spell.spell_id, now);

    // Calculate actual damage (game-specific)
    const damage = result.spell.mana_cost * result.spell.stability;
    const willMisfire = Math.random() < result.spell.instability_prob;

    return {
      success: true,
      spell: result.spell,
      damage,
      willMisfire,
      remainingMana: this.config.activeMana
    };
  }

  getSpellStats(spellId) {
    return this.database.get(spellId);
  }

  async getAllSpells() {
    return this.database.getAll();
  }

  restoreMana(amount) {
    this.config.activeMana = Math.min(
      this.config.maxMana,
      this.config.activeMana + amount
    );
  }
}

// ─── EXAMPLE 4: Custom Database Implementation ─────────────────────
export class GameSpecificDatabase {
  constructor() {
    this.spells = new Map();
  }

  async search(embedding, topK = 3) {
    // Game-specific ranking: combine similarity with player progression
    if (!this.spells.size) return [];

    const results = [];
    for (const [id, spell] of this.spells) {
      const score = this._cosine(embedding, spell.symbol_embedding);
      // Boost score based on player unlock level
      const boostedScore = score * (spell.unlocked ? 1.2 : 0.5);
      results.push({ id, score: boostedScore });
    }

    return results.sort((a, b) => b.score - a.score).slice(0, topK);
  }

  async upsert(spell) {
    spell.unlocked = false; // Track unlock status
    spell.playerLevel = 0;
    this.spells.set(spell.spell_id, spell);
  }

  async get(id) {
    return this.spells.get(id);
  }

  async getAll() {
    return Array.from(this.spells.values());
  }

  async size() {
    return this.spells.size;
  }

  _cosine(a, b) {
    let dot = 0;
    for (let i = 0; i < Math.min(a.length, b.length); i++) dot += a[i] * b[i];
    return dot;
  }

  // Game-specific: unlock spell
  unlockSpell(spellId, playerLevel) {
    const spell = this.spells.get(spellId);
    if (spell && playerLevel >= 5) {
      spell.unlocked = true;
      spell.playerLevel = playerLevel;
    }
  }
}

// ─── EXAMPLE 5: Feature Analysis ────────────────────────────────────
export function example_featureAnalysis() {
  const strokes = [
    [
      { x: 50, y: 50 }, { x: 100, y: 100 }, { x: 150, y: 50 }
    ]
  ];

  const features = FeatureExtractor.extract(strokes);
  console.log("Extracted features:", features);

  const embedding = EmbeddingGenerator.generate(features, "fireball");
  console.log("32D embedding:", Array.from(embedding));

  const balancing = BalancingSystem.compute(features, { ambiguity: 0.1, base_power: 20 });
  console.log("Game balance stats:", balancing);

  return { features, embedding, balancing };
}

// ─── EXAMPLE 6: Real-time Voice Integration ────────────────────────
export class VoiceSpellCaster {
  constructor(database, audioResolver, options = {}) {
    this.database = database;
    this.audioResolver = audioResolver;
    this.wakeWord = options.wakeWord || "arcanum";
    this.isListening = false;
    this.recorder = null;
    this.onSpellCast = options.onSpellCast || (() => {});
    this.onError = options.onError || (() => {});
  }

  startListening() {
    if (this.isListening) return;
    this.isListening = true;

    // Use browser Speech Recognition for wake word
    const SpeechRecognition = typeof window !== 'undefined'
      ? window.SpeechRecognition || window.webkitSpeechRecognition
      : null;

    if (!SpeechRecognition) {
      this.onError("Speech Recognition not available");
      return;
    }

    const recognition = new SpeechRecognition();
    recognition.continuous = true;
    recognition.interimResults = true;

    const wakeWordDetector = new WakeWordDetector(this.wakeWord);

    recognition.onresult = async (event) => {
      for (let i = event.resultIndex; i < event.results.length; i++) {
        const transcript = event.results[i][0].transcript;

        if (event.results[i].isFinal) {
          if (wakeWordDetector.processText(transcript)) {
            await this._recordSpell();
          }
        }
      }
    };

    recognition.start();
  }

  async _recordSpell() {
    this.recorder = new AudioRecorderWithSilenceDetection();
    this.recorder.onSilenceDetected = () => this._processRecording();

    await this.recorder.start();
  }

  async _processRecording() {
    const audioBlob = await this.recorder.stop();

    const result = await castAudioSpell({
      transcribedText: "voice spell",
      resolver: this.audioResolver,
      audioBlob
    });

    if (result) {
      await this.database.upsert(result.spell);
      this.onSpellCast(result);
    }
  }
}

// Export for testing
export default {
  example_symbolCasting,
  example_audioCasting,
  example_featureAnalysis,
  GameSpellSystem,
  GameSpecificDatabase,
  VoiceSpellCaster
};
