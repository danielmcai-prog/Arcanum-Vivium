#pragma once

#include <cstdint>
#include <string>
#include <vector>

namespace arcanum {

struct Point2 {
    float x = 0.0f;
    float y = 0.0f;
};

struct SpellFeatures {
    float symmetry = 0.0f;
    float enclosure = 0.0f;
    float spirality = 0.0f;
    float angularity = 0.0f;
    float complexity = 0.0f;
    int point_count = 0;
    float dir_bias = 0.0f;
    int intersections = 0;
    int stroke_count = 0;
    float aspect_ratio = 1.0f;
    float compactness = 0.0f;
    std::vector<std::vector<Point2>> normalized_strokes;
};

struct ResolverOutput {
    std::string spell_name = "Unnamed Spell";
    std::string element = "arcane";
    std::string force = "projection";
    std::string scale = "medium";
    std::string delivery = "projectile";
    std::string intent = "utility";
    std::string effect_description;
    std::string lore_hint;
    std::string grammar_analysis;
    std::string misfire_effect;
    float ambiguity = 0.1f;
    int base_power = 10;
    std::vector<std::string> semantic_tags;
};

struct BalanceResult {
    int mana_cost = 10;
    float stability = 0.9f;
    float instability_prob = 0.05f;
    float cooldown = 1.0f;
};

struct SpellFeaturesSnapshot {
    float symmetry = 0.0f;
    float enclosure = 0.0f;
    float spirality = 0.0f;
    float complexity = 0.0f;
    float angularity = 0.0f;
};

struct SpellRecord {
    std::string spell_id;
    std::string spell_name;
    std::string element;
    std::string force;
    std::string scale;
    std::string delivery;
    std::string intent;

    std::vector<float> symbol_embedding;
    std::vector<std::string> semantic_tags;

    std::string effect_description;
    std::string lore_hint;
    std::string grammar_analysis;
    std::string misfire_effect;

    int mana_cost = 10;
    float stability = 0.9f;
    float instability_prob = 0.05f;
    float cooldown = 1.0f;

    int usage_count = 1;
    float community_consensus = 0.0f;
    std::vector<std::string> variants;
    std::string variant_of;

    std::int64_t created_at_unix_ms = 0;
    std::string incantation;
    SpellFeaturesSnapshot features;
    std::string source = "symbol";
    std::string audio_base64;
};

enum class SpellMatchType {
    REUSE,
    VARIANT,
    RELATED,
    NEW
};

struct SpellSearchHit {
    std::string id;
    float score = 0.0f;
};

struct SpellMatchResult {
    SpellRecord spell;
    SpellMatchType match_type = SpellMatchType::NEW;
    float match_score = 0.0f;
    bool has_features = false;
    SpellFeatures features;
};

} // namespace arcanum
