# Arcanum Vivum C# Port (Unity)

This folder contains a Unity-ready C# port of the JavaScript spell engine.

## Structure

- `ArcanumVivum.Unity/package.json` - Unity package manifest
- `ArcanumVivum.Unity/Runtime/Core` - Feature extraction, embeddings, balancing, evolution, spell casting service
- `ArcanumVivum.Unity/Runtime/Audio` - Wake-word support, audio spell service, Whisper transcription client
- `ArcanumVivum.Unity/Runtime/Database` - Pluggable database interfaces and implementations
- `ArcanumVivum.Unity/Runtime/Resolvers` - Resolver interfaces and rule-based resolver fallback
- `ArcanumVivum.Unity/Runtime/Integration` - Unity/game integration classes
- `ArcanumVivum.Unity/Samples~` - Usage samples

## Feature Parity with JavaScript Engine

Implemented in C#:

- 11D geometric feature extraction
- Ramer-Douglas-Peucker stroke simplification
- 32D embedding generation (symbol + text)
- Similarity matching tiers (REUSE/VARIANT/RELATED/NEW)
- Balancing formulas (mana, stability, instability, cooldown)
- Evolution engine (usage reinforcement + variant linking)
- Audio spell creation path
- Wake-word detector
- Whisper transcription HTTP client
- In-memory + REST database adapters
- Unity microphone recorder with silence detection

## Unity Installation

1. Open Unity Package Manager.
2. Add package from disk and select `C#/ArcanumVivum.Unity/package.json`.
3. Add your resolver implementation for AI spell resolution.

## Quick Start

```csharp
using ArcanumVivum.SpellEngine.Core;
using ArcanumVivum.SpellEngine.Database;
using ArcanumVivum.SpellEngine.Resolvers;
using ArcanumVivum.SpellEngine.Models;

var db = new InMemorySpellDatabase();
var resolver = new RuleBasedResolver();
var engine = new SpellEngineService(db, resolver);

var strokes = new List<IReadOnlyList<Point2>>
{
    new List<Point2>
    {
        new Point2(100, 100),
        new Point2(150, 150),
        new Point2(200, 100)
    }
};

var result = await engine.CastSpellAsync(strokes, "fire blast");
```

## Notes

- Firebase/Supabase concrete adapters are intentionally left as extension points in C# to keep runtime dependencies minimal in Unity.
- For live wake-word speech recognition in Unity, pair `WakeWordDetector` with your ASR provider stream.
