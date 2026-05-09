#include "arcanum/evolution.hpp"

#include <algorithm>
#include <cmath>

namespace arcanum {

void EvolutionEngine::reinforce(SpellRecord& spell, ISpellDatabase& database) {
    spell.usage_count += 1;
    spell.community_consensus = std::min(1.0f, static_cast<float>(std::log10(static_cast<double>(spell.usage_count + 1))) / 4.0f);
    spell.stability = static_cast<float>(std::round((spell.stability * 0.9f + spell.community_consensus * 0.1f) * 1000.0f) / 1000.0f);

    if (spell.usage_count > 100) {
        spell.mana_cost = std::max(5, spell.mana_cost - 1);
    }

    database.upsert(spell);
}

void EvolutionEngine::check_variant(SpellRecord& existing, SpellRecord& incoming, float score) {
    if (score < 0.82f || score >= 0.92f) {
        return;
    }

    const auto it = std::find(existing.variants.begin(), existing.variants.end(), incoming.spell_id);
    if (it == existing.variants.end()) {
        existing.variants.push_back(incoming.spell_id);
        incoming.variant_of = existing.spell_id;
    }
}

} // namespace arcanum
