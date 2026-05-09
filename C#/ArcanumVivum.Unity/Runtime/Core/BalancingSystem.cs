using System;
using ArcanumVivum.SpellEngine.Models;

namespace ArcanumVivum.SpellEngine.Core
{
    public static class BalancingSystem
    {
        public static BalanceResult Compute(SpellFeatures? features, ResolverOutput? resolverOutput)
        {
            if (features == null)
            {
                return new BalanceResult
                {
                    ManaCost = 10,
                    Stability = 0.9f,
                    InstabilityProbability = 0.05f,
                    CooldownSeconds = 1f
                };
            }

            var ambiguity = resolverOutput?.Ambiguity ?? 0.1f;
            var basePower = resolverOutput?.BasePower ?? 10;

            var manaCost = (int)MathF.Round(
                basePower
                + features.Complexity * 30f
                + ambiguity * 20f
                + features.Intersections * 2f
                + features.Spirality * 15f
                - features.Symmetry * 8f
                - features.Enclosure * 5f);

            var stability = Clamp(
                0.9f * features.Symmetry
                - features.Spirality * 0.3f
                - features.Complexity * 0.2f
                - ambiguity * 0.15f
                + features.Enclosure * 0.1f,
                0.05f,
                1f);

            var instability = Clamp(
                (1f - features.Symmetry) * 0.3f
                + features.Spirality * 0.25f
                + features.Angularity * (1f - features.Enclosure) * 0.2f
                + ambiguity * 0.15f,
                0f,
                0.8f);

            var cooldown = MathF.Max(0.5f, manaCost / 40f);

            return new BalanceResult
            {
                ManaCost = Math.Max(5, manaCost),
                Stability = Round(stability, 2),
                InstabilityProbability = Round(instability, 2),
                CooldownSeconds = Round(cooldown, 1)
            };
        }

        private static float Clamp(float value, float min, float max)
        {
            return MathF.Max(min, MathF.Min(max, value));
        }

        private static float Round(float value, int decimals)
        {
            return (float)Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        }
    }
}
