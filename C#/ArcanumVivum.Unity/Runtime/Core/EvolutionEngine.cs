using System;
using System.Threading;
using System.Threading.Tasks;
using ArcanumVivum.SpellEngine.Database;
using ArcanumVivum.SpellEngine.Models;

namespace ArcanumVivum.SpellEngine.Core
{
    public static class EvolutionEngine
    {
        public static async Task ReinforceAsync(SpellRecord spell, ISpellDatabase database, CancellationToken cancellationToken = default)
        {
            spell.UsageCount += 1;
            spell.CommunityConsensus = MathF.Min(1f, (float)Math.Log10(spell.UsageCount + 1) / 4f);
            spell.Stability = Round(spell.Stability * 0.9f + spell.CommunityConsensus * 0.1f, 3);

            if (spell.UsageCount > 100)
            {
                spell.ManaCost = Math.Max(5, spell.ManaCost - 1);
            }

            await database.UpsertAsync(spell, cancellationToken);
        }

        public static void CheckVariant(SpellRecord existing, SpellRecord incoming, float score)
        {
            if (score < 0.82f || score >= 0.92f)
            {
                return;
            }

            if (!existing.Variants.Contains(incoming.SpellId))
            {
                existing.Variants.Add(incoming.SpellId);
                incoming.VariantOf = existing.SpellId;
            }
        }

        private static float Round(float value, int decimals)
        {
            return (float)Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        }
    }
}
