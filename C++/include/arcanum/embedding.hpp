#pragma once

#include <vector>

#include "arcanum/types.hpp"

namespace arcanum {

class EmbeddingGenerator {
public:
    static std::vector<float> generate(const std::optional<SpellFeatures>& features, const std::string& incantation_text = "");
    static float cosine(const std::vector<float>& a, const std::vector<float>& b);
};

} // namespace arcanum
