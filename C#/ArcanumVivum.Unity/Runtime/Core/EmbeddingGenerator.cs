using System;
using System.Collections.Generic;
using ArcanumVivum.SpellEngine.Models;

namespace ArcanumVivum.SpellEngine.Core
{
    public static class EmbeddingGenerator
    {
        public static float[] Generate(SpellFeatures? features, string? incantationText = null)
        {
            if (features == null)
            {
                return new float[32];
            }

            var textVec = TextEmbedding(incantationText ?? string.Empty);

            var symVec = new[]
            {
                features.Symmetry,
                features.Enclosure,
                features.Spirality,
                features.Angularity,
                features.Complexity,
                MathF.Min(1f, features.PointCount / 8f),
                features.DirBias,
                MathF.Min(1f, features.Intersections / 10f),
                MathF.Min(1f, features.StrokeCount / 5f),
                MathF.Min(1f, MathF.Abs(features.AspectRatio - 1f)),
                features.Compactness,
                features.Symmetry * features.Enclosure,
                features.Spirality * features.Complexity,
                features.Angularity * features.PointCount / 8f,
                features.Enclosure * (1f - features.Spirality),
                (1f - features.Symmetry) * features.Complexity
            };

            var outVec = new float[32];
            for (var i = 0; i < 16; i++)
            {
                outVec[i] = symVec[i] * 0.6f + textVec[i] * 0.4f;
            }

            for (var i = 0; i < 16; i++)
            {
                outVec[i + 16] = textVec[i + 16] * 0.8f + symVec[i % 16] * 0.2f;
            }

            return Normalize(outVec);
        }

        public static float Cosine(IReadOnlyList<float> a, IReadOnlyList<float> b)
        {
            var length = Math.Min(a.Count, b.Count);
            var dot = 0f;
            for (var i = 0; i < length; i++)
            {
                dot += a[i] * b[i];
            }

            return dot;
        }

        private static float[] TextEmbedding(string text)
        {
            var vec = new float[32];
            if (string.IsNullOrWhiteSpace(text))
            {
                return vec;
            }

            var words = text.ToLowerInvariant().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var lexicon = BuildLexicon();

            foreach (var word in words)
            {
                if (lexicon.TryGetValue(word, out var mapped))
                {
                    for (var i = 0; i < 32; i++)
                    {
                        vec[i] = MathF.Max(vec[i], mapped[i]);
                    }
                    continue;
                }

                var hash = StableHash(word);
                var idx = (Math.Abs(hash) % 16) + 16;
                vec[idx] = MathF.Max(vec[idx], 0.3f);
            }

            return vec;
        }

        private static Dictionary<string, float[]> BuildLexicon()
        {
            return new Dictionary<string, float[]>
            {
                ["star"] = MakeVec((6, 0.8f), (16, 0.9f)),
                ["fire"] = MakeVec((17, 0.8f)),
                ["ice"] = MakeVec((18, 0.8f)),
                ["water"] = MakeVec((19, 0.8f)),
                ["wind"] = MakeVec((20, 0.8f)),
                ["earth"] = MakeVec((21, 0.8f)),
                ["void"] = MakeVec((22, 0.8f)),
                ["eye"] = MakeVec((23, 0.8f)),
                ["bomb"] = MakeVec((24, 0.9f)),
                ["cluster"] = MakeVec((25, 0.7f)),
                ["frozen"] = MakeVec((18, 0.7f), (26, 0.8f)),
                ["heaven"] = MakeVec((16, 0.8f), (27, 0.7f)),
                ["abyss"] = MakeVec((22, 0.9f), (28, 0.8f)),
                ["gravity"] = MakeVec((29, 0.8f)),
                ["collapse"] = MakeVec((29, 0.9f)),
                ["spiral"] = MakeVec((1, 0.7f), (20, 0.6f), (30, 0.8f)),
                ["chaos"] = MakeVec((3, 0.7f), (31, 0.9f))
            };
        }

        private static float[] MakeVec(params (int Index, float Value)[] pairs)
        {
            var vec = new float[32];
            foreach (var pair in pairs)
            {
                if (pair.Index >= 0 && pair.Index < vec.Length)
                {
                    vec[pair.Index] = pair.Value;
                }
            }

            return vec;
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 17;
                foreach (var c in value)
                {
                    hash = hash * 31 + c;
                }
                return hash;
            }
        }

        private static float[] Normalize(IReadOnlyList<float> vec)
        {
            var sum = 0f;
            for (var i = 0; i < vec.Count; i++)
            {
                sum += vec[i] * vec[i];
            }

            var mag = MathF.Sqrt(sum);
            if (mag <= 0.000001f)
            {
                mag = 1f;
            }

            var output = new float[vec.Count];
            for (var i = 0; i < vec.Count; i++)
            {
                output[i] = vec[i] / mag;
            }

            return output;
        }
    }
}
