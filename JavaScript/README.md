# Arcanum Vivum — Spell Engine Library

A pure JavaScript spell casting engine for games. Enables both **symbol-based** and **voice-based** spell casting with deterministic semantic matching.

## Features

- **Symbol-based casting**: Draw spell patterns, extract 11 geometric features, match via 32D embeddings
- **Voice-based casting**: Wake-word detection ("Arcanum"), transcription via Whisper API
- **Semantic matching**: Cosine similarity search with 4-tier spell categorization (REUSE/VARIANT/RELATED/NEW)
- **Backend-agnostic**: Pluggable databases (Firebase, Supabase, REST API, in-memory)
- **Pluggable resolvers**: Swap Claude API for OpenAI, Ollama, or rule-based resolvers
- **Game balancing**: Mana costs, stability scoring, misfire probability, cooldowns
- **Evolution engine**: Usage tracking, community consensus, mastery discounts, variant genealogy
- **10 elements, 10 forces, 5 intents**: Full game mechanics system

## Installation

```bash
npm install @arcanum/spell-engine
```

## Quick Start

### Symbol-Based Casting

```javascript
import { castSpell, InMemorySpellDatabase } from '@arcanum/spell-engine';

// Initialize database
const db = new InMemorySpellDatabase();

// Draw spell pattern (array of {x, y} points)
const strokes = [
  {x: 100, y: 100}, {x: 150, y: 50}, {x: 200, y: 100}, 
  {x: 180, y: 150}, {x: 120, y: 150}
];

// Cast with optional incantation text
const result = await castSpell({
  strokes,
  incantation: "fire lance",
  db,
  resolver: resolveWithClaude  // Your resolver function
});

console.log(result);
// {
//   spell: { id, name, element, force, mana_cost, stability, ... },
//   matchType: "REUSE" | "VARIANT" | "RELATED" | "NEW",
//   matchScore: 0.95,
//   features: { symmetry, spirality, complexity, ... }
// }
```

### Voice-Based Casting

```javascript
import { AudioSpellCaster } from '@arcanum/spell-engine';

const caster = new AudioSpellCaster({
  wakeWord: 'arcanum',
  silenceThresholdDb: -50,
  silenceDurationMs: 2000,
  db,
  resolver: resolveWithClaude
});

// Listen for spell casts
caster.on('spell', (result) => {
  console.log(`Cast ${result.spell.name}!`);
  console.log(`Mana cost: ${result.spell.mana_cost}`);
});

// Audio level visualization
caster.on('level', (db) => {
  updateAudioLevelUI(db);
});

await caster.initialize();
```

## Database Backends

### In-Memory (Development)

```javascript
import { InMemorySpellDatabase } from '@arcanum/spell-engine';

const db = new InMemorySpellDatabase();
```

All spells reset on reload. Useful for testing.

### Firebase (Cloud Firestore)

```bash
npm install firebase
```

```javascript
import { initializeApp } from 'firebase/app';
import { FirebaseSpellDatabase } from '@arcanum/spell-engine';

const firebaseApp = initializeApp(firebaseConfig);
const db = new FirebaseSpellDatabase(firebaseApp);
```

**Firebase Firestore Rules:**
```json
{
  "rules": {
    "spells": {
      "{document=**}": {
        "read": true,
        "write": true
      }
    }
  }
}
```

### Supabase (PostgreSQL + pgvector)

```bash
npm install @supabase/supabase-js
```

```javascript
import { createClient } from '@supabase/supabase-js';
import { SupabaseSpellDatabase } from '@arcanum/spell-engine';

const supabase = createClient(url, key);
const db = new SupabaseSpellDatabase(supabase);
```

**Setup Postgres table:**
```sql
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE spells (
  spell_id TEXT PRIMARY KEY,
  spell_name TEXT NOT NULL,
  element TEXT,
  force TEXT,
  symbol_embedding VECTOR(32),
  mana_cost INT,
  stability FLOAT,
  usage_count INT,
  created_at TIMESTAMP DEFAULT NOW(),
  metadata JSONB
);

CREATE INDEX ON spells USING HNSW (symbol_embedding vector_cosine_ops);
```

### REST API (Custom Backend)

```javascript
import { RestApiSpellDatabase } from '@arcanum/spell-engine';

const db = new RestApiSpellDatabase({
  baseUrl: 'https://your-api.com',
  apiKey: 'your-api-key'
});
```

**Expected API endpoints:**
- `POST /spells/search` - Search by embedding
- `GET /spells/:id` - Get spell by ID
- `POST /spells` - Create/update spell
- `GET /spells` - List all spells

## Resolvers (AI Integration)

By default, spells use Claude API. Implement your own resolver:

```javascript
import Anthropic from '@anthropic-ai/sdk';

const client = new Anthropic();

export async function resolveWithClaude(features, incantation, spell) {
  const message = await client.messages.create({
    model: 'claude-3-5-sonnet-20241022',
    max_tokens: 500,
    messages: [{
      role: 'user',
      content: `You are a spell engine resolver. Given a spell pattern and incantation, resolve the spell's properties.

Pattern features:
- Symmetry: ${features.symmetry.toFixed(2)}
- Spirality: ${features.spirality.toFixed(2)}
- Complexity: ${features.complexity.toFixed(2)}
- Enclosure: ${features.enclosure.toFixed(2)}

Incantation: "${incantation}"

Respond in JSON: { element, force, intent, name }
Where element is one of: celestial, fire, water, earth, wind, void, arcane, shadow, lightning, nature
Force is one of: projection, containment, explosion, gravity, time, perception, healing, transformation, absorption, summoning
Intent is one of: offensive, defensive, utility, ritual, chaotic`
    }]
  });

  return JSON.parse(message.content[0].text);
}
```

Or use OpenAI instead:

```javascript
import OpenAI from 'openai';

const openai = new OpenAI({ apiKey: process.env.OPENAI_API_KEY });

export async function resolveWithOpenAI(features, incantation, spell) {
  const response = await openai.chat.completions.create({
    model: 'gpt-4',
    messages: [/* same as above */]
  });
  
  return JSON.parse(response.choices[0].message.content);
}
```

## Game Engine Integration Examples

### Unity (C# + HTTP)

```csharp
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class SpellCaster : MonoBehaviour {
    private string engineUrl = "http://localhost:3000/cast";

    public void CastSpell(Vector3[] strokePoints, string incantation) {
        StartCoroutine(SendCastRequest(strokePoints, incantation));
    }

    IEnumerator SendCastRequest(Vector3[] points, string text) {
        var strokeData = new System.Collections.Generic.List<object>();
        foreach (var p in points) {
            strokeData.Add(new { x = p.x, y = p.y });
        }

        var payload = JsonUtility.ToJson(new {
            strokes = strokeData,
            incantation = text
        });

        using (var request = new UnityWebRequest(engineUrl, "POST")) {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(payload));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success) {
                var result = JsonUtility.FromJson<SpellResult>(request.downloadHandler.text);
                ExecuteSpell(result);
            }
        }
    }
}
```

### Node.js Backend Server

```javascript
import express from 'express';
import { castSpell, SupabaseSpellDatabase } from '@arcanum/spell-engine';
import { createClient } from '@supabase/supabase-js';

const app = express();
app.use(express.json());

const db = new SupabaseSpellDatabase(
  createClient(process.env.SUPABASE_URL, process.env.SUPABASE_KEY)
);

app.post('/cast', async (req, res) => {
  const { strokes, incantation } = req.body;
  
  try {
    const result = await castSpell({
      strokes,
      incantation,
      db,
      resolver: resolveWithClaude
    });

    res.json(result);
  } catch (error) {
    res.status(400).json({ error: error.message });
  }
});

app.listen(3000, () => console.log('Spell engine listening on port 3000'));
```

## Architecture

### Feature Extraction (11D)
Analyzes drawn spell patterns:
- **Symmetry** (0-1): Bilateral mirror symmetry
- **Spirality** (0-1): Spiral or circular motion
- **Complexity** (0-1): Path length vs bounding box
- **Enclosure** (0-1): Enclosed vs open shape
- **Angularity** (0-1): Sharp corners vs smooth curves
- **Intersection count**: Number of self-crossings
- **Point count**: Extrema in path
- **Direction bias** (0-1): Horizontal vs vertical dominance
- **Stroke count**: Number of separate strokes
- **Aspect ratio**: Width/height of bounding box
- **Compactness** (0-1): Fills bounding box

### Semantic Embeddings (32D)
Combines geometry (16D) + text semantics (16D):
- **Geometry embedding**: Normalized feature vector
- **Text embedding**: Lexicon lookup + hash-based for unknown words
- **Final**: Cosine normalized, float32

### Similarity Matching
```
REUSE:   score >= 0.92   (exact or near-exact match)
VARIANT: 0.82 <= score < 0.92   (dialect/variant of existing spell)
RELATED: 0.65 <= score < 0.82   (conceptually related)
NEW:     score < 0.65   (new spell, create via resolver)
```

### Balancing Formula
```
mana_cost = base_power 
          + complexity × 30 
          + ambiguity × 20 
          + intersections × 2 
          + spirality × 15 
          - symmetry × 8 
          - enclosure × 5

stability = 0.9 × symmetry 
          - spirality × 0.3 
          - complexity × 0.2 
          - ambiguity × 0.15 
          + enclosure × 0.1

misfire_prob = (1-symmetry) × 0.3 
             + spirality × 0.25 
             + angularity × (1-enclosure) × 0.2 
             + ambiguity × 0.15

cooldown_sec = max(0.5, mana_cost / 40)
```

## API Reference

### `castSpell(options)`

**Parameters:**
- `strokes` (Array): [{x, y}, ...] points forming spell pattern
- `incantation` (String): Text incantation (optional)
- `db` (SpellDatabase): Database instance
- `resolver` (Function): AI resolver function

**Returns:**
```javascript
{
  spell: { /* full spell object */ },
  matchType: "REUSE" | "VARIANT" | "RELATED" | "NEW",
  matchScore: 0.0-1.0,
  features: { /* 11D feature vector */ }
}
```

### `AudioSpellCaster` class

**Constructor options:**
```javascript
{
  wakeWord: 'arcanum',           // Trigger phrase
  silenceThresholdDb: -50,       // Silence detection threshold
  silenceDurationMs: 2000,       // Silence required to end recording
  db,                            // Database instance
  resolver                       // AI resolver function
}
```

**Methods:**
- `initialize()` - Start listening
- `stop()` - Stop listening
- `on(event, callback)` - Register event listener

**Events:**
- `spell` - Spell cast completed
- `level` - Audio level changed (dB)

### Database Interface

All databases implement:

```javascript
class SpellDatabase {
  async search(embedding, topK = 3) 
    // Returns [{id, score}, ...] sorted by score
  
  async get(id) 
    // Returns full spell object
  
  async upsert(spell) 
    // Create/update spell
  
  async getAll() 
    // Returns all spells
  
  async size() 
    // Returns count
}
```

## Contributing

Contributions welcome! Areas of interest:

- [ ] TypeScript definitions
- [ ] Performance optimizations
- [ ] Unreal Engine integration
- [ ] Godot engine integration
- [ ] Additional LLM providers (OpenAI, Ollama, local)
- [ ] Advanced balancing AI
- [ ] Spell genealogy visualization
- [ ] Network sync for multiplayer

## License

MIT

## Community

For discussions, spell sharing, and PvP matchmaking: [Discord Link]

---

**Built with ❤️ for game developers**
