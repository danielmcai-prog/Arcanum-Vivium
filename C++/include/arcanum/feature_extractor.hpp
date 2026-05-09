#pragma once

#include <optional>
#include <vector>

#include "arcanum/types.hpp"

namespace arcanum {

class FeatureExtractor {
public:
    static std::optional<SpellFeatures> extract(const std::vector<std::vector<Point2>>& strokes);
    static std::vector<Point2> rdp(const std::vector<Point2>& points, float epsilon = 2.0f);
};

} // namespace arcanum
