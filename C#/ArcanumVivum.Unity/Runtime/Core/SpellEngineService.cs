using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcanumVivum.SpellEngine.Database;
using ArcanumVivum.SpellEngine.Models;
using ArcanumVivum.SpellEngine.Resolvers;

namespace ArcanumVivum.SpellEngine.Core
{
    public static class SimilarityThresholds
    {
        public const float Reuse = 0.92f;
        public const float Variant = 0.82f;
        public const float Related = 0.65f;
    }

    public sealed class SpellEngineService
    {
        private readonly ISpellDatabase _database;
        private readonly ISymbolSpellResolver _resolver;

        public SpellEngineService(ISpellDatabase database, ISymbolSpellResolver resolver)
        {
            _database = database;
            _resolver = resolver;
        }

        public async Task<SpellMatchResult?> CastSpellAsync(
            IReadOnlyList<IReadOnlyList<Point2>> strokes,
            string? incantation,
            CancellationToken cancellationToken = default)
        {
            var features = FeatureExtractor.Extract(strokes);
            var embedding = EmbeddingGenerator.Generate(features, incantation);

            var results = await _database.SearchAsync(embedding, 3, cancellationToken);
            var best = results.Count > 0 ? results[0] : null;

            SpellRecord spell;
            SpellMatchType matchType;
            var matchScore = best?.Score ?? 0f;

            if (best != null && best.Score >= SimilarityThresholds.Reuse)
            {
                var existing = await _database.GetAsync(best.Id, cancellationToken);
                if (existing == null)
                {
                    return null;
                }

                spell = existing;
                matchType = SpellMatchType.Reuse;
                await EvolutionEngine.ReinforceAsync(spell, _database, cancellationToken);

                return new SpellMatchResult
                {
                    Spell = spell,
                    MatchType = matchType,
                    MatchScore = matchScore,
                    Features = features
                };
            }

            var resolved = await _resolver.ResolveSymbolAsync(features, incantation, cancellationToken);
            if (resolved == null)
            {
                return null;
            }

            var balancing = BalancingSystem.Compute(features, resolved);
            spell = BuildSpellRecord(embedding, features, incantation, resolved, balancing);

            if (best != null && best.Score >= SimilarityThresholds.Variant)
            {
                var parent = await _database.GetAsync(best.Id, cancellationToken);
                if (parent != null)
                {
                    EvolutionEngine.CheckVariant(parent, spell, best.Score);
                    await _database.UpsertAsync(parent, cancellationToken);
                }

                matchType = SpellMatchType.Variant;
            }
            else if (best != null && best.Score >= SimilarityThresholds.Related)
            {
                matchType = SpellMatchType.Related;
            }
            else
            {
                matchType = SpellMatchType.New;
            }

            await _database.UpsertAsync(spell, cancellationToken);

            return new SpellMatchResult
            {
                Spell = spell,
                MatchType = matchType,
                MatchScore = matchScore,
                Features = features
            };
        }

        private static SpellRecord BuildSpellRecord(
            IReadOnlyList<float> embedding,
            SpellFeatures? features,
            string? incantation,
            ResolverOutput resolved,
            BalanceResult balancing)
        {
            var id = $"spell_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds():x}_{Guid.NewGuid():N}".Substring(0, 22);

            return new SpellRecord
            {
                SpellId = id,
                SymbolEmbedding = new List<float>(embedding),
                SemanticTags = resolved.SemanticTags ?? new List<string>(),
                SpellName = resolved.SpellName,
                Element = resolved.Element,
                Force = resolved.Force,
                Scale = resolved.Scale,
                Delivery = resolved.Delivery,
                Intent = resolved.Intent,
                EffectDescription = resolved.EffectDescription,
                LoreHint = resolved.LoreHint,
                GrammarAnalysis = resolved.GrammarAnalysis,
                MisfireEffect = resolved.MisfireEffect,
                ManaCost = balancing.ManaCost,
                Stability = balancing.Stability,
                InstabilityProbability = balancing.InstabilityProbability,
                Cooldown = balancing.CooldownSeconds,
                UsageCount = 1,
                CommunityConsensus = 0f,
                Variants = new List<string>(),
                VariantOf = null,
                CreatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Incantation = incantation,
                Source = "symbol",
                Features = new SpellFeaturesSnapshot
                {
                    Symmetry = features?.Symmetry ?? 0f,
                    Enclosure = features?.Enclosure ?? 0f,
                    Spirality = features?.Spirality ?? 0f,
                    Complexity = features?.Complexity ?? 0f,
                    Angularity = features?.Angularity ?? 0f
                }
            };
        }
    }
}
