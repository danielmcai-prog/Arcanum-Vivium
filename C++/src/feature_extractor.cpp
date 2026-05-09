#include "arcanum/feature_extractor.hpp"

#include <algorithm>
#include <cmath>

namespace arcanum {

namespace {

constexpr float kPi = 3.14159265358979323846f;

float hypot2(float x, float y) {
    return std::sqrt(x * x + y * y);
}

float perpendicular_dist(const Point2& p, const Point2& a, const Point2& b) {
    const float dx = b.x - a.x;
    const float dy = b.y - a.y;
    if (std::abs(dx) < 1e-6f && std::abs(dy) < 1e-6f) {
        return hypot2(p.x - a.x, p.y - a.y);
    }
    const float t = ((p.x - a.x) * dx + (p.y - a.y) * dy) / (dx * dx + dy * dy);
    return hypot2(p.x - (a.x + t * dx), p.y - (a.y + t * dy));
}

std::vector<std::vector<Point2>> normalize(const std::vector<std::vector<Point2>>& strokes) {
    std::vector<Point2> all;
    for (const auto& stroke : strokes) {
        all.insert(all.end(), stroke.begin(), stroke.end());
    }

    if (all.empty()) {
        return strokes;
    }

    float min_x = all.front().x;
    float max_x = all.front().x;
    float min_y = all.front().y;
    float max_y = all.front().y;

    for (const auto& p : all) {
        min_x = std::min(min_x, p.x);
        max_x = std::max(max_x, p.x);
        min_y = std::min(min_y, p.y);
        max_y = std::max(max_y, p.y);
    }

    const float cx = (min_x + max_x) * 0.5f;
    const float cy = (min_y + max_y) * 0.5f;
    float scale = std::max(max_x - min_x, max_y - min_y);
    if (scale < 1e-6f) {
        scale = 1.0f;
    }

    std::vector<std::vector<Point2>> out;
    out.reserve(strokes.size());
    for (const auto& stroke : strokes) {
        std::vector<Point2> ns;
        ns.reserve(stroke.size());
        for (const auto& p : stroke) {
            ns.push_back({((p.x - cx) / scale) * 64.0f, ((p.y - cy) / scale) * 64.0f});
        }
        out.push_back(ns);
    }

    return out;
}

float symmetry(const std::vector<Point2>& points) {
    if (points.empty()) {
        return 0.0f;
    }

    float score = 0.0f;
    for (const auto& p : points) {
        Point2 mirror{-p.x, p.y};
        float nearest = 1e9f;
        for (const auto& q : points) {
            nearest = std::min(nearest, hypot2(q.x - mirror.x, q.y - mirror.y));
        }
        score += std::max(0.0f, 1.0f - nearest / 20.0f);
    }

    return score / static_cast<float>(points.size());
}

float enclosure(const std::vector<std::vector<Point2>>& strokes) {
    for (const auto& stroke : strokes) {
        if (stroke.size() < 4) {
            continue;
        }
        const Point2& first = stroke.front();
        const Point2& last = stroke.back();
        if (hypot2(first.x - last.x, first.y - last.y) < 12.0f) {
            return 1.0f;
        }
    }

    if (strokes.size() > 1) {
        std::vector<Point2> all;
        for (const auto& stroke : strokes) {
            all.insert(all.end(), stroke.begin(), stroke.end());
        }
        if (all.size() > 1) {
            if (hypot2(all.front().x - all.back().x, all.front().y - all.back().y) < 15.0f) {
                return 0.7f;
            }
        }
    }

    return 0.0f;
}

float spirality(const std::vector<Point2>& points) {
    if (points.size() < 6) {
        return 0.0f;
    }

    std::vector<float> angles;
    angles.reserve(points.size() - 1);
    for (std::size_t i = 1; i < points.size(); ++i) {
        angles.push_back(std::atan2(points[i].y - points[i - 1].y, points[i].x - points[i - 1].x));
    }

    float total_turn = 0.0f;
    for (std::size_t i = 1; i < angles.size(); ++i) {
        float d = angles[i] - angles[i - 1];
        if (d > kPi) d -= 2.0f * kPi;
        if (d < -kPi) d += 2.0f * kPi;
        total_turn += d;
    }

    return std::min(1.0f, std::abs(total_turn) / (2.0f * kPi * 2.0f));
}

float angularity(const std::vector<std::vector<Point2>>& strokes) {
    int sharp = 0;
    int total = 0;
    for (const auto& stroke : strokes) {
        for (std::size_t i = 1; i + 1 < stroke.size(); ++i) {
            const float a = std::atan2(stroke[i].y - stroke[i - 1].y, stroke[i].x - stroke[i - 1].x);
            const float b = std::atan2(stroke[i + 1].y - stroke[i].y, stroke[i + 1].x - stroke[i].x);
            float diff = std::abs(a - b);
            if (diff > kPi) {
                diff = 2.0f * kPi - diff;
            }
            if (diff > 0.5f) {
                sharp++;
            }
            total++;
        }
    }
    return total == 0 ? 0.0f : static_cast<float>(sharp) / static_cast<float>(total);
}

float complexity(const std::vector<std::vector<Point2>>& strokes) {
    float total_len = 0.0f;
    for (const auto& stroke : strokes) {
        for (std::size_t i = 1; i < stroke.size(); ++i) {
            total_len += hypot2(stroke[i].x - stroke[i - 1].x, stroke[i].y - stroke[i - 1].y);
        }
    }
    return std::min(1.0f, total_len / 400.0f);
}

int detect_points(const std::vector<Point2>& points) {
    if (points.size() < 5) {
        return 0;
    }

    int extrema = 0;
    for (std::size_t i = 1; i + 1 < points.size(); ++i) {
        const float d1 = hypot2(points[i].x, points[i].y);
        const float d0 = hypot2(points[i - 1].x, points[i - 1].y);
        const float d2 = hypot2(points[i + 1].x, points[i + 1].y);
        if (d1 > d0 && d1 > d2 && d1 > 15.0f) {
            extrema++;
        }
    }

    return std::min(extrema, 12);
}

float direction_bias(const std::vector<Point2>& points) {
    if (points.empty()) {
        return 0.0f;
    }

    float cx = 0.0f;
    float cy = 0.0f;
    for (const auto& p : points) {
        cx += p.x;
        cy += p.y;
    }
    cx /= static_cast<float>(points.size());
    cy /= static_cast<float>(points.size());
    return std::atan2(cy, cx) / kPi;
}

bool seg_intersects(const Point2& a, const Point2& b, const Point2& c, const Point2& d) {
    const float det = (b.x - a.x) * (d.y - c.y) - (b.y - a.y) * (d.x - c.x);
    if (std::abs(det) < 0.001f) {
        return false;
    }

    const float t = ((c.x - a.x) * (d.y - c.y) - (c.y - a.y) * (d.x - c.x)) / det;
    const float u = -((a.x - c.x) * (b.y - a.y) - (a.y - c.y) * (b.x - a.x)) / det;
    return t > 0.01f && t < 0.99f && u > 0.01f && u < 0.99f;
}

int intersection_count(const std::vector<std::vector<Point2>>& strokes) {
    std::vector<std::pair<Point2, Point2>> segs;
    for (const auto& stroke : strokes) {
        for (std::size_t i = 0; i + 1 < stroke.size(); ++i) {
            segs.push_back({stroke[i], stroke[i + 1]});
        }
    }

    int count = 0;
    for (std::size_t i = 0; i < segs.size(); ++i) {
        for (std::size_t j = i + 2; j < segs.size(); ++j) {
            if (seg_intersects(segs[i].first, segs[i].second, segs[j].first, segs[j].second)) {
                count++;
            }
        }
    }

    return std::min(count, 20);
}

float compactness(const std::vector<Point2>& points) {
    if (points.size() < 3) {
        return 0.0f;
    }

    float min_x = points.front().x;
    float max_x = points.front().x;
    float min_y = points.front().y;
    float max_y = points.front().y;

    for (const auto& p : points) {
        min_x = std::min(min_x, p.x);
        max_x = std::max(max_x, p.x);
        min_y = std::min(min_y, p.y);
        max_y = std::max(max_y, p.y);
    }

    const float area = (max_x - min_x) * (max_y - min_y);

    float perim = 0.0f;
    for (std::size_t i = 1; i < points.size(); ++i) {
        perim += hypot2(points[i].x - points[i - 1].x, points[i].y - points[i - 1].y);
    }

    if (perim < 1e-6f) {
        return 0.0f;
    }

    return std::min(1.0f, (4.0f * kPi * area) / (perim * perim));
}

} // namespace

std::vector<Point2> FeatureExtractor::rdp(const std::vector<Point2>& points, float epsilon) {
    if (points.size() < 3) {
        return points;
    }

    float max_dist = 0.0f;
    std::size_t idx = 0;
    const std::size_t end = points.size() - 1;

    for (std::size_t i = 1; i < end; ++i) {
        const float d = perpendicular_dist(points[i], points[0], points[end]);
        if (d > max_dist) {
            max_dist = d;
            idx = i;
        }
    }

    if (max_dist <= epsilon) {
        return {points[0], points[end]};
    }

    const std::vector<Point2> left = rdp(std::vector<Point2>(points.begin(), points.begin() + static_cast<long long>(idx) + 1), epsilon);
    const std::vector<Point2> right = rdp(std::vector<Point2>(points.begin() + static_cast<long long>(idx), points.end()), epsilon);

    std::vector<Point2> out;
    out.reserve(left.size() + right.size());
    out.insert(out.end(), left.begin(), left.end() - 1);
    out.insert(out.end(), right.begin(), right.end());
    return out;
}

std::optional<SpellFeatures> FeatureExtractor::extract(const std::vector<std::vector<Point2>>& strokes) {
    std::size_t total_points = 0;
    for (const auto& s : strokes) {
        total_points += s.size();
    }

    if (total_points < 2) {
        return std::nullopt;
    }

    std::vector<std::vector<Point2>> simplified;
    simplified.reserve(strokes.size());
    for (const auto& stroke : strokes) {
        simplified.push_back(rdp(stroke, 2.0f));
    }

    std::vector<std::vector<Point2>> norm = normalize(simplified);
    std::vector<Point2> all;
    for (const auto& stroke : norm) {
        all.insert(all.end(), stroke.begin(), stroke.end());
    }

    if (all.size() < 2) {
        return std::nullopt;
    }

    float min_x = all.front().x;
    float max_x = all.front().x;
    float min_y = all.front().y;
    float max_y = all.front().y;
    for (const auto& p : all) {
        min_x = std::min(min_x, p.x);
        max_x = std::max(max_x, p.x);
        min_y = std::min(min_y, p.y);
        max_y = std::max(max_y, p.y);
    }

    const float width = std::max(max_x - min_x, 1.0f);
    const float height = std::max(max_y - min_y, 1.0f);

    SpellFeatures out;
    out.symmetry = symmetry(all);
    out.enclosure = enclosure(norm);
    out.spirality = spirality(all);
    out.angularity = angularity(norm);
    out.complexity = complexity(norm);
    out.point_count = detect_points(all);
    out.dir_bias = direction_bias(all);
    out.intersections = intersection_count(norm);
    out.stroke_count = static_cast<int>(norm.size());
    out.aspect_ratio = width / height;
    out.compactness = compactness(all);
    out.normalized_strokes = norm;

    return out;
}

} // namespace arcanum
