/**
 * AUDIO SPELL ENGINE — Pure JavaScript Audio Processing
 * No UI dependencies, works in Node.js and browsers
 */

export const AUDIO_CONFIG = {
  WAKE_WORD: "arcanum",
  SILENCE_THRESHOLD_DB: -50,
  SILENCE_DURATION_MS: 2000,
  SAMPLE_RATE: 16000,
  CHUNK_DURATION_MS: 500
};

// ─── WAKE WORD DETECTOR ────────────────────────────────────────────
export class WakeWordDetector {
  constructor(wakeWord = AUDIO_CONFIG.WAKE_WORD) {
    this.wakeWord = wakeWord.toLowerCase();
    this.buffer = "";
  }

  processText(text) {
    this.buffer = (this.buffer + text).toLowerCase().slice(-100);
    return this.buffer.includes(this.wakeWord);
  }

  reset() {
    this.buffer = "";
  }
}

// ─── AUDIO RECORDER WITH SILENCE DETECTION ─────────────────────────
export class AudioRecorderWithSilenceDetection {
  constructor(
    silenceThresholdDb = AUDIO_CONFIG.SILENCE_THRESHOLD_DB,
    silenceDurationMs = AUDIO_CONFIG.SILENCE_DURATION_MS
  ) {
    this.silenceThresholdDb = silenceThresholdDb;
    this.silenceDurationMs = silenceDurationMs;
    this.mediaRecorder = null;
    this.audioContext = null;
    this.analyser = null;
    this.microphone = null;
    this.recordedChunks = [];
    this.isRecording = false;
    this.silenceTimer = null;
    this.audioLevel = 0;
    this.onSilenceDetected = null;
    this.onAudioLevel = null;
  }

  async start() {
    try {
      this.audioContext = new (typeof window !== 'undefined' 
        ? window.AudioContext || window.webkitAudioContext 
        : null)();
      
      if (!this.audioContext) {
        throw new Error("AudioContext not available");
      }

      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      this.microphone = this.audioContext.createMediaStreamSource(stream);
      this.analyser = this.audioContext.createAnalyser();
      this.analyser.fftSize = 2048;

      this.microphone.connect(this.analyser);

      this.mediaRecorder = new MediaRecorder(stream);
      this.recordedChunks = [];

      this.mediaRecorder.ondataavailable = (e) => {
        if (e.data.size > 0) this.recordedChunks.push(e.data);
      };

      this.mediaRecorder.start(100);
      this.isRecording = true;

      this._monitorAudioLevel();
    } catch (e) {
      console.error("Failed to start audio recording:", e);
      throw e;
    }
  }

  _monitorAudioLevel() {
    if (!this.isRecording) return;

    const dataArray = new Uint8Array(this.analyser.frequencyBinCount);
    this.analyser.getByteFrequencyData(dataArray);

    let sum = 0;
    for (let i = 0; i < dataArray.length; i++) {
      const normalized = dataArray[i] / 255;
      sum += normalized * normalized;
    }
    const rms = Math.sqrt(sum / dataArray.length);
    this.audioLevel = 20 * Math.log10(Math.max(rms, 0.001));

    if (this.onAudioLevel) this.onAudioLevel(this.audioLevel);

    if (this.audioLevel < this.silenceThresholdDb) {
      if (!this.silenceTimer) {
        this.silenceTimer = setTimeout(() => {
          if (this.onSilenceDetected) this.onSilenceDetected();
          this.silenceTimer = null;
        }, this.silenceDurationMs);
      }
    } else {
      if (this.silenceTimer) {
        clearTimeout(this.silenceTimer);
        this.silenceTimer = null;
      }
    }

    requestAnimationFrame(() => this._monitorAudioLevel());
  }

  async stop() {
    return new Promise((resolve) => {
      if (!this.mediaRecorder) {
        resolve(null);
        return;
      }

      this.isRecording = false;
      if (this.silenceTimer) clearTimeout(this.silenceTimer);

      this.mediaRecorder.onstop = () => {
        const blob = new Blob(this.recordedChunks, { type: "audio/webm" });
        if (this.microphone) this.microphone.disconnect();
        if (this.audioContext) this.audioContext.close();
        resolve(blob);
      };

      this.mediaRecorder.stop();
    });
  }
}

// ─── TRANSCRIPTION (Swappable) ──────────────────────────────────────
export async function transcribeAudio(audioBlob, apiKey, provider = 'whisper') {
  if (provider === 'whisper') {
    return transcribeWithWhisper(audioBlob, apiKey);
  }
  throw new Error(`Unknown transcription provider: ${provider}`);
}

async function transcribeWithWhisper(audioBlob, apiKey) {
  try {
    const formData = new FormData();
    formData.append("file", audioBlob, "audio.webm");
    formData.append("model", "whisper-1");
    formData.append("language", "en");

    const response = await fetch("https://api.openai.com/v1/audio/transcriptions", {
      method: "POST",
      headers: {
        Authorization: `Bearer ${apiKey}`
      },
      body: formData
    });

    if (!response.ok) throw new Error(`Transcription failed: ${response.status}`);
    const data = await response.json();
    return data.text || "";
  } catch (e) {
    console.error("Transcription error:", e);
    return null;
  }
}

// ─── AUDIO SPELL RESOLUTION ────────────────────────────────────────
export async function resolveAudioSpell(transcribedText, resolver) {
  return resolver(transcribedText);
}

// ─── AUDIO SPELL CASTING ───────────────────────────────────────────
export async function castAudioSpell({ transcribedText, resolver, audioBlob }) {
  const resolved = await resolveAudioSpell(transcribedText, resolver);
  if (!resolved) return null;

  const id = `spell_audio_${Date.now().toString(36)}_${Math.random().toString(36).slice(2, 7)}`;

  const audioEmbedding = new Float32Array(32);
  const words = transcribedText.toLowerCase().split(/\s+/);
  for (let i = 0; i < Math.min(words.length, 32); i++) {
    audioEmbedding[i] = (words[i].charCodeAt(0) % 100) / 100;
  }

  const spell = {
    spell_id: id,
    symbol_embedding: Array.from(audioEmbedding),
    semantic_tags: resolved.semantic_tags || [],
    spell_name: resolved.spell_name,
    element: resolved.element,
    force: resolved.force,
    scale: resolved.scale,
    delivery: resolved.delivery,
    intent: resolved.intent,
    effect_description: resolved.effect_description,
    lore_hint: resolved.lore_hint,
    grammar_analysis: resolved.grammar_analysis,
    misfire_effect: resolved.misfire_effect,
    mana_cost: resolved.mana_base,
    stability: Math.max(0.5, 1 - resolved.ambiguity),
    instability_prob: resolved.ambiguity,
    cooldown: resolved.mana_base / 40,
    usage_count: 1,
    community_consensus: 0,
    variants: [],
    variant_of: null,
    created_at: Date.now(),
    incantation: transcribedText,
    audio_blob: audioBlob ? await blobToBase64(audioBlob) : null,
    source: "voice"
  };

  return { spell, features: null };
}

// Helper: Convert blob to base64
async function blobToBase64(blob) {
  return new Promise((resolve) => {
    const reader = new FileReader();
    reader.onloadend = () => resolve(reader.result);
    reader.readAsDataURL(blob);
  });
}
