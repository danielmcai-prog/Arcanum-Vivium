#include "arcanum/balancing.hpp"

#include <algorithm>
#include <cmath>

namespace arcanum {

namespace {

float clampf(float value, float min_v, float max_v) {
    return std::max(min_v, std::min(max_v, value));
}

float round_to(float value, int decimals) {
    const float p = std::pow(10.0f, static_cast<float>(decimals));
    return std::round(value * p) / p;
}

} // namespace

BalanceResult BalancingSystem::compute(const std::optional<SpellFeatures>& features, const std::optional<ResolverOutput>& resolver_output) {
    if (!features.has_value()) {
        return BalanceResult{};
    }

    const SpellFeatures& f = *features;
    const float ambiguity = resolver_output.has_value() ? resolver_output->ambiguity : 0.1f;
    const float base = resolver_output.has_value() ? static_cast<float>(resolver_output->base_power) : 10.0f;

    float mana_cost =
        base
        + f.complexity * 30.0f
        + ambiguity * 20.0f
        + static_cast<float>(f.intersections) * 2.0f
        + f.spirality * 15.0f
        - f.symmetry * 8.0f
        - f.enclosure * 5.0f;

    float stability = clampf(
        0.9f * f.symmetry
        - f.spirality * 0.3f
        - f.complexity * 0.2f
        - ambiguity * 0.15f
        + f.enclosure * 0.1f,
        0.05f,
        1.0f);

    float instability = clampf(
        (1.0f - f.symmetry) * 0.3f
        + f.spirality * 0.25f
        + f.angularity * (1.0f - f.enclosure) * 0.2f
        + ambiguity * 0.15f,
        0.0f,
        0.8f);

    float cooldown = std::max(0.5f, mana_cost / 40.0f);

    BalanceResult result;
    result.mana_cost = std::max(5, static_cast<int>(std::lround(mana_cost)));
    result.stability = round_to(stability, 2);
    result.instability_prob = round_to(instability, 2);
    result.cooldown = round_to(cooldown, 1);
    return result;
}

} // namespace arcanum
