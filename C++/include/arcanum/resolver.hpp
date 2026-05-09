#pragma once

#include <optional>
#include <string>

#include "arcanum/types.hpp"

namespace arcanum {

class ISymbolResolver {
public:
    virtual ~ISymbolResolver() = default;
    virtual std::optional<ResolverOutput> resolve_symbol(const std::optional<SpellFeatures>& features, const std::string& incantation) = 0;
};

class IAudioResolver {
public:
    virtual ~IAudioResolver() = default;
    virtual std::optional<ResolverOutput> resolve_audio(const std::string& transcribed_text) = 0;
};

class RuleBasedResolver final : public ISymbolResolver, public IAudioResolver {
public:
    std::optional<ResolverOutput> resolve_symbol(const std::optional<SpellFeatures>& features, const std::string& incantation) override;
    std::optional<ResolverOutput> resolve_audio(const std::string& transcribed_text) override;
};

} // namespace arcanum
