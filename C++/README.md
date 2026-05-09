# Arcanum Vivum C++ Port

This folder contains a C++17 port of the Arcanum spell engine with the same core behavior as the JavaScript and C# versions.

## Included Features

- 11D geometric feature extraction
- Ramer-Douglas-Peucker stroke simplification
- 32D embedding generation (symbol + text)
- Similarity thresholds: REUSE, VARIANT, RELATED, NEW
- Balancing formulas (mana, stability, instability, cooldown)
- Evolution engine (usage reinforcement + variant linking)
- Pluggable resolver interfaces (symbol/audio)
- Pluggable database interface with in-memory implementation
- Audio helpers (wake-word detector + deterministic audio spell packing)

## Build

```bash
cmake -S . -B build
cmake --build build
```

## Example

The example executable is built as `arcanum_basic_example` and demonstrates casting with in-memory storage.
