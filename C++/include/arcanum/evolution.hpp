#pragma once

#include "arcanum/database.hpp"

namespace arcanum {

class EvolutionEngine {
public:
    static void reinforce(SpellRecord& spell, ISpellDatabase& database);
    static void check_variant(SpellRecord& existing, SpellRecord& incoming, float score);
};

} // namespace arcanum
