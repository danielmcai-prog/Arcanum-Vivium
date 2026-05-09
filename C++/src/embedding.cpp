#include "arcanum/embedding.hpp"

#include <algorithm>
#include <cmath>
#include <cstdint>
#include <sstream>
#include <string>
#include <unordered_map>

namespace arcanum {

namespace {

std::vector<float> make_vec(std::initializer_list<std::pair<int, float>> values) {
    std::vector<float> out(32, 0.0f);
    for (const auto& v : values) {
        if (v.first >= 0 && v.first < 32) {
            out[v.first] = v.second;
        }
    }
    return out;
}

std::unordered_map<std::string, std::vector<float>> build_lexicon() {
    return {
        {"star", make_vec({{6, 0.8f}, {16, 0.9f}})},
        {"fire", make_vec({{17, 0.8f}})},
        {"ice", make_vec({{18, 0.8f}})},
        {"water", make_vec({{19, 0.8f}})},
        {"wind", make_vec({{20, 0.8f}})},
        {"earth", make_vec({{21, 0.8f}})},
        {"void", make_vec({{22, 0.8f}})},
        {"eye", make_vec({{23, 0.8f}})},
        {"bomb", make_vec({{24, 0.9f}})},
        {"cluster", make_vec({{25, 0.7f}})},
        {"frozen", make_vec({{18, 0.7f}, {26, 0.8f}})},
        {"heaven", make_vec({{16, 0.8f}, {27, 0.7f}})},
        {"abyss", make_vec({{22, 0.9f}, {28, 0.8f}})},
        {"gravity", make_vec({{29, 0.8f}})},
        {"collapse", make_vec({{29, 0.9f}})},
        {"spiral", make_vec({{1, 0.7f}, {20, 0.6f}, {30, 0.8f}})},
        {"chaos", make_vec({{3, 0.7f}, {31, 0.9f}})}
    };
}

int stable_hash(const std::string& value) {
    int hash = 17;
    for (unsigned char c : value) {
        hash = hash * 31 + static_cast<int>(c);
    }
    return hash;
}

std::vector<float> normalize(const std::vector<float>& in) {
    float mag = 0.0f;
    for (float x : in) {
        mag += x * x;
    }
    mag = std::sqrt(mag);
    if (mag < 1e-6f) {
        mag = 1.0f;
    }

    std::vector<float> out(in.size(), 0.0f);
    for (std::size_t i = 0; i < in.size(); ++i) {
        out[i] = in[i] / mag;
    }
    return out;
}

std::vector<float> text_embedding(const std::string& text) {
    std::vector<float> vec(32, 0.0f);
    if (text.empty()) {
        return vec;
    }

    static const auto lexicon = build_lexicon();
    std::stringstream ss(text);
    std::string word;
    while (ss >> word) {
        std::transform(word.begin(), word.end(), word.begin(), [](unsigned char c) {
            return static_cast<char>(std::tolower(c));
        });

        auto it = lexicon.find(word);
        if (it != lexicon.end()) {
            for (int i = 0; i < 32; ++i) {
                vec[i] = std::max(vec[i], it->second[i]);
            }
            continue;
        }

        const int idx = (std::abs(stable_hash(word)) % 16) + 16;
        vec[idx] = std::max(vec[idx], 0.3f);
    }

    return vec;
}

} // namespace

std::vector<float> EmbeddingGenerator::generate(const std::optional<SpellFeatures>& features, const std::string& incantation_text) {
    if (!features.has_value()) {
        return std::vector<float>(32, 0.0f);
    }

    const SpellFeatures& f = *features;
    const std::vector<float> txt = text_embedding(incantation_text);

    std::vector<float> sym = {
        f.symmetry,
        f.enclosure,
        f.spirality,
        f.angularity,
        f.complexity,
        std::min(1.0f, static_cast<float>(f.point_count) / 8.0f),
        f.dir_bias,
        std::min(1.0f, static_cast<float>(f.intersections) / 10.0f),
        std::min(1.0f, static_cast<float>(f.stroke_count) / 5.0f),
        std::min(1.0f, std::abs(f.aspect_ratio - 1.0f)),
        f.compactness,
        f.symmetry * f.enclosure,
        f.spirality * f.complexity,
        f.angularity * static_cast<float>(f.point_count) / 8.0f,
        f.enclosure * (1.0f - f.spirality),
        (1.0f - f.symmetry) * f.complexity
    };

    std::vector<float> out(32, 0.0f);
    for (int i = 0; i < 16; ++i) {
        out[i] = sym[i] * 0.6f + txt[i] * 0.4f;
    }
    for (int i = 0; i < 16; ++i) {
        out[i + 16] = txt[i + 16] * 0.8f + sym[i % 16] * 0.2f;
    }

    return normalize(out);
}

float EmbeddingGenerator::cosine(const std::vector<float>& a, const std::vector<float>& b) {
    const std::size_t n = std::min(a.size(), b.size());
    float dot = 0.0f;
    for (std::size_t i = 0; i < n; ++i) {
        dot += a[i] * b[i];
    }
    return dot;
}

} // namespace arcanum
