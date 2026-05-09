#include "arcanum/spell_engine.hpp"

#include <chrono>
#include <random>
#include <sstream>

#include "arcanum/balancing.hpp"
#include "arcanum/embedding.hpp"
#include "arcanum/evolution.hpp"
#include "arcanum/feature_extractor.hpp"

namespace arcanum {

namespace {

std::string build_spell_id() {
    static std::mt19937 rng{std::random_device{}()};
    static std::uniform_int_distribution<int> dist(0, 15);

    const auto now = std::chrono::duration_cast<std::chrono::milliseconds>(
        std::chrono::system_clock::now().time_since_epoch()).count();

    std::stringstream ss;
    ss << "spell_" << std::hex << now << "_";
    for (int i = 0; i < 6; ++i) {
        ss << std::hex << dist(rng);
    }
    return ss.str();
}

std::int64_t now_unix_ms() {
    return std::chrono::duration_cast<std::chrono::milliseconds>(
        std::chrono::system_clock::now().time_since_epoch()).count();
}

} // namespace

SpellEngineService::SpellEngineService(ISpellDatabase& database, ISymbolResolver& resolver)
    : database_(database), resolver_(resolver) {}

std::optional<SpellMatchResult> SpellEngineService::cast_spell(const std::vector<std::vector<Point2>>& strokes, const std::string& incantation) {
    const std::optional<SpellFeatures> features = FeatureExtractor::extract(strokes);
    const std::vector<float> embedding = EmbeddingGenerator::generate(features, incantation);

    const std::vector<SpellSearchHit> results = database_.search(embedding, 3);
    const bool has_best = !results.empty();

    SpellMatchResult out;
    out.match_score = has_best ? results.front().score : 0.0f;

    if (has_best && results.front().score >= SimilarityThresholds::REUSE) {
        const std::optional<SpellRecord> existing = database_.get(results.front().id);
        if (!existing.has_value()) {
            return std::nullopt;
        }

        out.spell = *existing;
        out.match_type = SpellMatchType::REUSE;
        EvolutionEngine::reinforce(out.spell, database_);
        if (features.has_value()) {
            out.has_features = true;
            out.features = *features;
        }
        return out;
    }

    const std::optional<ResolverOutput> resolved = resolver_.resolve_symbol(features, incantation);
    if (!resolved.has_value()) {
        return std::nullopt;
    }

    const BalanceResult balance = BalancingSystem::compute(features, resolved);

    SpellRecord spell;
    spell.spell_id = build_spell_id();
    spell.symbol_embedding = embedding;
    spell.semantic_tags = resolved->semantic_tags;
    spell.spell_name = resolved->spell_name;
    spell.element = resolved->element;
    spell.force = resolved->force;
    spell.scale = resolved->scale;
    spell.delivery = resolved->delivery;
    spell.intent = resolved->intent;
    spell.effect_description = resolved->effect_description;
    spell.lore_hint = resolved->lore_hint;
    spell.grammar_analysis = resolved->grammar_analysis;
    spell.misfire_effect = resolved->misfire_effect;
    spell.mana_cost = balance.mana_cost;
    spell.stability = balance.stability;
    spell.instability_prob = balance.instability_prob;
    spell.cooldown = balance.cooldown;
    spell.usage_count = 1;
    spell.community_consensus = 0.0f;
    spell.created_at_unix_ms = now_unix_ms();
    spell.incantation = incantation;
    spell.source = "symbol";

    if (features.has_value()) {
        spell.features.symmetry = features->symmetry;
        spell.features.enclosure = features->enclosure;
        spell.features.spirality = features->spirality;
        spell.features.complexity = features->complexity;
        spell.features.angularity = features->angularity;
    }

    if (has_best && results.front().score >= SimilarityThresholds::VARIANT) {
        std::optional<SpellRecord> parent = database_.get(results.front().id);
        if (parent.has_value()) {
            EvolutionEngine::check_variant(*parent, spell, results.front().score);
            database_.upsert(*parent);
        }
        out.match_type = SpellMatchType::VARIANT;
    } else if (has_best && results.front().score >= SimilarityThresholds::RELATED) {
        out.match_type = SpellMatchType::RELATED;
    } else {
        out.match_type = SpellMatchType::NEW;
    }

    database_.upsert(spell);

    out.spell = spell;
    if (features.has_value()) {
        out.has_features = true;
        out.features = *features;
    }
    return out;
}

} // namespace arcanum
