#include <iostream>
#include <vector>

#include "arcanum/audio.hpp"
#include "arcanum/database.hpp"
#include "arcanum/spell_engine.hpp"

int main() {
    arcanum::InMemorySpellDatabase db;
    arcanum::RuleBasedResolver resolver;
    arcanum::SpellEngineService engine(db, resolver);

    std::vector<std::vector<arcanum::Point2>> strokes = {
        {
            {100.0f, 100.0f},
            {150.0f, 50.0f},
            {200.0f, 100.0f},
            {180.0f, 150.0f},
            {120.0f, 150.0f}
        }
    };

    auto result = engine.cast_spell(strokes, "fire lance");
    if (!result.has_value()) {
        std::cerr << "Failed to cast spell\n";
        return 1;
    }

    std::cout << "Spell: " << result->spell.spell_name << "\n";
    std::cout << "Mana: " << result->spell.mana_cost << "\n";
    std::cout << "DB size: " << db.size() << "\n";

    arcanum::WakeWordDetector detector("arcanum");
    std::cout << "Wake word heard: " << (detector.process_text("Arcanum ignite") ? "yes" : "no") << "\n";

    return 0;
}
