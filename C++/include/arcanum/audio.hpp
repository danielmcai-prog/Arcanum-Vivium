#pragma once

#include <optional>
#include <string>
#include <vector>

#include "arcanum/resolver.hpp"
#include "arcanum/types.hpp"

namespace arcanum {

struct AudioConfig {
    std::string wake_word = "arcanum";
    float silence_threshold_db = -50.0f;
    int silence_duration_ms = 2000;
    int sample_rate = 16000;
    int chunk_duration_ms = 500;
};

class WakeWordDetector {
public:
    explicit WakeWordDetector(std::string wake_word = "arcanum");
    bool process_text(const std::string& text);
    void reset();

private:
    std::string wake_word_;
    std::string buffer_;
};

class AudioSpellService {
public:
    explicit AudioSpellService(IAudioResolver& resolver);
    std::optional<SpellMatchResult> cast_audio_spell(const std::string& transcribed_text, const std::vector<std::uint8_t>& audio_bytes = {});

private:
    IAudioResolver& resolver_;
};

} // namespace arcanum
