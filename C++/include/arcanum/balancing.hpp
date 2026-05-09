#pragma once

#include "arcanum/types.hpp"

namespace arcanum {

class BalancingSystem {
public:
    static BalanceResult compute(const std::optional<SpellFeatures>& features, const std::optional<ResolverOutput>& resolver_output);
};

} // namespace arcanum
