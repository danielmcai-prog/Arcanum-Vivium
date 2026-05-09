using System;
using System.Collections.Generic;

namespace ArcanumVivum.SpellEngine.Models
{
    [Serializable]
    public struct Point2
    {
        public float X;
        public float Y;

        public Point2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    [Serializable]
    public sealed class SpellFeatures
    {
        public float Symmetry { get; set; }
        public float Enclosure { get; set; }
        public float Spirality { get; set; }
        public float Angularity { get; set; }
        public float Complexity { get; set; }
        public int PointCount { get; set; }
        public float DirBias { get; set; }
        public int Intersections { get; set; }
        public int StrokeCount { get; set; }
        public float AspectRatio { get; set; }
        public float Compactness { get; set; }
        public List<List<Point2>> NormalizedStrokes { get; set; } = new();
    }

    [Serializable]
    public sealed class ResolverOutput
    {
        public string SpellName { get; set; } = "Unnamed Spell";
        public string Element { get; set; } = "arcane";
        public string Force { get; set; } = "projection";
        public string Scale { get; set; } = "medium";
        public string Delivery { get; set; } = "projectile";
        public string Intent { get; set; } = "utility";
        public string EffectDescription { get; set; } = string.Empty;
        public string LoreHint { get; set; } = string.Empty;
        public string GrammarAnalysis { get; set; } = string.Empty;
        public string MisfireEffect { get; set; } = string.Empty;
        public float Ambiguity { get; set; } = 0.1f;
        public int BasePower { get; set; } = 10;
        public List<string> SemanticTags { get; set; } = new();
    }

    [Serializable]
    public sealed class BalanceResult
    {
        public int ManaCost { get; set; }
        public float Stability { get; set; }
        public float InstabilityProbability { get; set; }
        public float CooldownSeconds { get; set; }
    }

    [Serializable]
    public sealed class SpellRecord
    {
        public string SpellId { get; set; } = string.Empty;
        public string SpellName { get; set; } = string.Empty;
        public string Element { get; set; } = string.Empty;
        public string Force { get; set; } = string.Empty;
        public string Scale { get; set; } = string.Empty;
        public string Delivery { get; set; } = string.Empty;
        public string Intent { get; set; } = string.Empty;
        public List<float> SymbolEmbedding { get; set; } = new();
        public List<string> SemanticTags { get; set; } = new();
        public string EffectDescription { get; set; } = string.Empty;
        public string LoreHint { get; set; } = string.Empty;
        public string GrammarAnalysis { get; set; } = string.Empty;
        public string MisfireEffect { get; set; } = string.Empty;
        public int ManaCost { get; set; }
        public float Stability { get; set; }
        public float InstabilityProbability { get; set; }
        public float Cooldown { get; set; }
        public int UsageCount { get; set; } = 1;
        public float CommunityConsensus { get; set; }
        public List<string> Variants { get; set; } = new();
        public string? VariantOf { get; set; }
        public long CreatedAtUnixMs { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public string? Incantation { get; set; }
        public SpellFeaturesSnapshot Features { get; set; } = new();
        public string Source { get; set; } = "symbol";
        public string? AudioBase64 { get; set; }
    }

    [Serializable]
    public sealed class SpellFeaturesSnapshot
    {
        public float Symmetry { get; set; }
        public float Enclosure { get; set; }
        public float Spirality { get; set; }
        public float Complexity { get; set; }
        public float Angularity { get; set; }
    }

    public enum SpellMatchType
    {
        Reuse,
        Variant,
        Related,
        New
    }

    public sealed class SpellMatchResult
    {
        public SpellRecord Spell { get; set; } = new();
        public SpellMatchType MatchType { get; set; }
        public float MatchScore { get; set; }
        public SpellFeatures? Features { get; set; }
    }

    public sealed class SpellSearchHit
    {
        public string Id { get; set; } = string.Empty;
        public float Score { get; set; }
    }
}
