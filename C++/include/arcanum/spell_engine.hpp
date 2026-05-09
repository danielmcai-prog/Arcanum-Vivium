#pragma once

#include "arcanum/database.hpp"
#include "arcanum/resolver.hpp"

namespace arcanum {

struct SimilarityThresholds {
    static constexpr float REUSE = 0.92f;
    static constexpr float VARIANT = 0.82f;
    static constexpr float RELATED = 0.65f;
};

class SpellEngineService {
public:
    SpellEngineService(ISpellDatabase& database, ISymbolResolver& resolver);
    std::optional<SpellMatchResult> cast_spell(const std::vector<std::vector<Point2>>& strokes, const std::string& incantation);

private:
    ISpellDatabase& database_;
    ISymbolResolver& resolver_;
};

} // namespace arcanum
