using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcanumVivum.SpellEngine.Models;
using ArcanumVivum.SpellEngine.Resolvers;

namespace ArcanumVivum.SpellEngine.Audio
{
    public sealed class AudioSpellService
    {
        private readonly IAudioSpellResolver _resolver;

        public AudioSpellService(IAudioSpellResolver resolver)
        {
            _resolver = resolver;
        }

        public async Task<SpellMatchResult?> CastAudioSpellAsync(
            string transcribedText,
            byte[]? audioBytes,
            CancellationToken cancellationToken = default)
        {
            var resolved = await _resolver.ResolveAudioAsync(transcribedText, cancellationToken);
            if (resolved == null)
            {
                return null;
            }

            var embedding = BuildAudioEmbedding(transcribedText);
            var spell = new SpellRecord
            {
                SpellId = $"spell_audio_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds():x}_{Guid.NewGuid():N}".Substring(0, 28),
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
                ManaCost = resolved.BasePower,
                Stability = MathF.Max(0.5f, 1f - resolved.Ambiguity),
                InstabilityProbability = resolved.Ambiguity,
                Cooldown = resolved.BasePower / 40f,
                UsageCount = 1,
                CommunityConsensus = 0f,
                CreatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Incantation = transcribedText,
                Source = "voice",
                AudioBase64 = audioBytes == null ? null : Convert.ToBase64String(audioBytes)
            };

            return new SpellMatchResult
            {
                Spell = spell,
                MatchType = SpellMatchType.New,
                MatchScore = 0f,
                Features = null
            };
        }

        private static float[] BuildAudioEmbedding(string text)
        {
            var embedding = new float[32];
            var words = text.ToLowerInvariant().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < Math.Min(words.Length, 32); i++)
            {
                embedding[i] = (words[i][0] % 100) / 100f;
            }

            return embedding;
        }
    }
}
