#include "arcanum/audio.hpp"

#include <algorithm>
#include <chrono>
#include <cctype>
#include <random>
#include <sstream>

namespace arcanum {

namespace {

std::string to_lower(std::string s) {
    for (char& c : s) {
        c = static_cast<char>(std::tolower(static_cast<unsigned char>(c)));
    }
    return s;
}

std::string pseudo_base64(const std::vector<std::uint8_t>& bytes) {
    static const char* alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    std::string out;
    out.reserve((bytes.size() * 4) / 3 + 4);

    int val = 0;
    int valb = -6;
    for (std::uint8_t c : bytes) {
        val = (val << 8) + c;
        valb += 8;
        while (valb >= 0) {
            out.push_back(alphabet[(val >> valb) & 0x3F]);
            valb -= 6;
        }
    }
    if (valb > -6) {
        out.push_back(alphabet[((val << 8) >> (valb + 8)) & 0x3F]);
    }
    while (out.size() % 4) {
        out.push_back('=');
    }
    return out;
}

std::string build_audio_spell_id() {
    static std::mt19937 rng{std::random_device{}()};
    static std::uniform_int_distribution<int> dist(0, 15);

    const auto now = std::chrono::duration_cast<std::chrono::milliseconds>(
        std::chrono::system_clock::now().time_since_epoch()).count();

    std::stringstream ss;
    ss << "spell_audio_" << std::hex << now << "_";
    for (int i = 0; i < 6; ++i) {
        ss << std::hex << dist(rng);
    }
    return ss.str();
}

std::vector<float> audio_embedding(const std::string& text) {
    std::vector<float> embedding(32, 0.0f);
    std::stringstream ss(to_lower(text));
    std::string word;
    int i = 0;
    while (ss >> word) {
        if (i >= 32 || word.empty()) {
            break;
        }
        embedding[static_cast<std::size_t>(i)] = static_cast<float>(static_cast<unsigned char>(word[0]) % 100) / 100.0f;
        ++i;
    }
    return embedding;
}

std::int64_t now_unix_ms() {
    return std::chrono::duration_cast<std::chrono::milliseconds>(
        std::chrono::system_clock::now().time_since_epoch()).count();
}

} // namespace

WakeWordDetector::WakeWordDetector(std::string wake_word)
    : wake_word_(to_lower(std::move(wake_word))) {}

bool WakeWordDetector::process_text(const std::string& text) {
    buffer_ += to_lower(text);
    if (buffer_.size() > 100) {
        buffer_ = buffer_.substr(buffer_.size() - 100);
    }
    return buffer_.find(wake_word_) != std::string::npos;
}

void WakeWordDetector::reset() {
    buffer_.clear();
}

AudioSpellService::AudioSpellService(IAudioResolver& resolver)
    : resolver_(resolver) {}

std::optional<SpellMatchResult> AudioSpellService::cast_audio_spell(const std::string& transcribed_text, const std::vector<std::uint8_t>& audio_bytes) {
    const std::optional<ResolverOutput> resolved = resolver_.resolve_audio(transcribed_text);
    if (!resolved.has_value()) {
        return std::nullopt;
    }

    SpellRecord spell;
    spell.spell_id = build_audio_spell_id();
    spell.symbol_embedding = audio_embedding(transcribed_text);
    spell.semantic_tags = resolved->semantic_tags;
    spell.spell_name = resolved->spell_name;
    spell.element = resolved->element;
    spell.force = resolved->force;
    spell.scale = resolved->scale;
    spell.delivery = resolved->delivery;
    spell.intent = resolved->intent;
    spell.effect_description = resolved->effect_description;
    spell.lore_hint = resolved->lore_hint;
    spell.grammar_analysis = resolved->grammar_analysis;
    spell.misfire_effect = resolved->misfire_effect;
    spell.mana_cost = resolved->base_power;
    spell.stability = std::max(0.5f, 1.0f - resolved->ambiguity);
    spell.instability_prob = resolved->ambiguity;
    spell.cooldown = static_cast<float>(resolved->base_power) / 40.0f;
    spell.created_at_unix_ms = now_unix_ms();
    spell.incantation = transcribed_text;
    spell.source = "voice";
    spell.audio_base64 = audio_bytes.empty() ? std::string{} : pseudo_base64(audio_bytes);

    SpellMatchResult out;
    out.spell = spell;
    out.match_type = SpellMatchType::NEW;
    out.match_score = 0.0f;
    out.has_features = false;
    return out;
}

} // namespace arcanum
