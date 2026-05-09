#include "arcanum/database.hpp"

#include <algorithm>

#include "arcanum/embedding.hpp"

namespace arcanum {

std::vector<SpellSearchHit> InMemorySpellDatabase::search(const std::vector<float>& embedding, int top_k) {
    std::vector<SpellSearchHit> hits;
    hits.reserve(spells_.size());

    for (const auto& item : spells_) {
        SpellSearchHit hit;
        hit.id = item.first;
        hit.score = EmbeddingGenerator::cosine(embedding, item.second.symbol_embedding);
        hits.push_back(hit);
    }

    std::sort(hits.begin(), hits.end(), [](const SpellSearchHit& a, const SpellSearchHit& b) {
        return a.score > b.score;
    });

    if (static_cast<int>(hits.size()) > top_k) {
        hits.resize(top_k);
    }

    return hits;
}

void InMemorySpellDatabase::upsert(const SpellRecord& spell) {
    spells_[spell.spell_id] = spell;
}

std::optional<SpellRecord> InMemorySpellDatabase::get(const std::string& id) {
    const auto it = spells_.find(id);
    if (it == spells_.end()) {
        return std::nullopt;
    }
    return it->second;
}

std::vector<SpellRecord> InMemorySpellDatabase::get_all() {
    std::vector<SpellRecord> out;
    out.reserve(spells_.size());
    for (const auto& item : spells_) {
        out.push_back(item.second);
    }
    return out;
}

int InMemorySpellDatabase::size() const {
    return static_cast<int>(spells_.size());
}

} // namespace arcanum
