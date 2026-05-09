# Arcanum Vivum GDScript Port

This folder contains a Godot/GDScript port of the spell engine with the same core feature set as the JavaScript, C#, and C++ versions.

## Included Features

- 11D geometric feature extraction
- Ramer-Douglas-Peucker stroke simplification
- 32D embeddings (symbol + text)
- Similarity tiers: REUSE, VARIANT, RELATED, NEW
- Balancing formulas (mana, stability, instability, cooldown)
- Evolution and variant tracking
- Pluggable resolver interfaces and rule-based fallback resolver
- Pluggable database interface with in-memory implementation
- Audio utilities: wake-word detector and audio spell packaging
- Godot game integration helper

## Layout

- `addons/arcanum_vivum_spell_engine/core` - core engine logic
- `addons/arcanum_vivum_spell_engine/audio` - audio spell helpers
- `addons/arcanum_vivum_spell_engine/database` - database layer
- `addons/arcanum_vivum_spell_engine/resolvers` - resolver contracts and fallback resolver
- `addons/arcanum_vivum_spell_engine/integration` - game-facing integration system
- `addons/arcanum_vivum_spell_engine/examples` - example script

## Usage

Copy `addons/arcanum_vivum_spell_engine` into your Godot project's `addons` directory, then load scripts from:

- `res://addons/arcanum_vivum_spell_engine/arcanum_spell_engine.gd`
