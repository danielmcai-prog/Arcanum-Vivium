#pragma once

#include <optional>
#include <string>
#include <unordered_map>
#include <vector>

#include "arcanum/types.hpp"

namespace arcanum {

class ISpellDatabase {
public:
    virtual ~ISpellDatabase() = default;
    virtual std::vector<SpellSearchHit> search(const std::vector<float>& embedding, int top_k = 3) = 0;
    virtual void upsert(const SpellRecord& spell) = 0;
    virtual std::optional<SpellRecord> get(const std::string& id) = 0;
    virtual std::vector<SpellRecord> get_all() = 0;
    virtual int size() const = 0;
};

class InMemorySpellDatabase final : public ISpellDatabase {
public:
    std::vector<SpellSearchHit> search(const std::vector<float>& embedding, int top_k = 3) override;
    void upsert(const SpellRecord& spell) override;
    std::optional<SpellRecord> get(const std::string& id) override;
    std::vector<SpellRecord> get_all() override;
    int size() const override;

private:
    std::unordered_map<std::string, SpellRecord> spells_;
};

} // namespace arcanum
