using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ArcanumVivum.SpellEngine.Models;

namespace ArcanumVivum.SpellEngine.Database
{
    public sealed class FirebaseSpellDatabase : ISpellDatabase
    {
        private readonly RestApiSpellDatabase _rest;

        public FirebaseSpellDatabase(HttpClient httpClient, string cloudFunctionBaseUrl)
        {
            _rest = new RestApiSpellDatabase(httpClient, cloudFunctionBaseUrl);
        }

        public Task<IReadOnlyList<SpellSearchHit>> SearchAsync(IReadOnlyList<float> embedding, int topK = 3, CancellationToken cancellationToken = default)
            => _rest.SearchAsync(embedding, topK, cancellationToken);

        public Task UpsertAsync(SpellRecord spell, CancellationToken cancellationToken = default)
            => _rest.UpsertAsync(spell, cancellationToken);

        public Task<SpellRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
            => _rest.GetAsync(id, cancellationToken);

        public Task<IReadOnlyList<SpellRecord>> GetAllAsync(CancellationToken cancellationToken = default)
            => _rest.GetAllAsync(cancellationToken);

        public Task<int> SizeAsync(CancellationToken cancellationToken = default)
            => _rest.SizeAsync(cancellationToken);
    }

    public sealed class SupabaseSpellDatabase : ISpellDatabase
    {
        private readonly RestApiSpellDatabase _rest;

        public SupabaseSpellDatabase(HttpClient httpClient, string edgeFunctionBaseUrl)
        {
            _rest = new RestApiSpellDatabase(httpClient, edgeFunctionBaseUrl);
        }

        public Task<IReadOnlyList<SpellSearchHit>> SearchAsync(IReadOnlyList<float> embedding, int topK = 3, CancellationToken cancellationToken = default)
            => _rest.SearchAsync(embedding, topK, cancellationToken);

        public Task UpsertAsync(SpellRecord spell, CancellationToken cancellationToken = default)
            => _rest.UpsertAsync(spell, cancellationToken);

        public Task<SpellRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
            => _rest.GetAsync(id, cancellationToken);

        public Task<IReadOnlyList<SpellRecord>> GetAllAsync(CancellationToken cancellationToken = default)
            => _rest.GetAllAsync(cancellationToken);

        public Task<int> SizeAsync(CancellationToken cancellationToken = default)
            => _rest.SizeAsync(cancellationToken);
    }
}
