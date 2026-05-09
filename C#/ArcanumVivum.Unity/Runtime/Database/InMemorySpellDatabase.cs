using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcanumVivum.SpellEngine.Core;
using ArcanumVivum.SpellEngine.Models;

namespace ArcanumVivum.SpellEngine.Database
{
    public sealed class InMemorySpellDatabase : ISpellDatabase
    {
        private readonly Dictionary<string, SpellRecord> _spells = new();

        public Task<IReadOnlyList<SpellSearchHit>> SearchAsync(IReadOnlyList<float> embedding, int topK = 3, CancellationToken cancellationToken = default)
        {
            var hits = _spells
                .Select(pair => new SpellSearchHit
                {
                    Id = pair.Key,
                    Score = EmbeddingGenerator.Cosine(embedding, pair.Value.SymbolEmbedding)
                })
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .ToList();

            return Task.FromResult<IReadOnlyList<SpellSearchHit>>(hits);
        }

        public Task UpsertAsync(SpellRecord spell, CancellationToken cancellationToken = default)
        {
            _spells[spell.SpellId] = spell;
            return Task.CompletedTask;
        }

        public Task<SpellRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            _spells.TryGetValue(id, out var spell);
            return Task.FromResult(spell);
        }

        public Task<IReadOnlyList<SpellRecord>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<SpellRecord>>(_spells.Values.ToList());
        }

        public Task<int> SizeAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_spells.Count);
        }
    }
}
