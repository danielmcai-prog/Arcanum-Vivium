using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcanumVivum.SpellEngine.Models;

namespace ArcanumVivum.SpellEngine.Database
{
    public interface ISpellDatabase
    {
        Task<IReadOnlyList<SpellSearchHit>> SearchAsync(IReadOnlyList<float> embedding, int topK = 3, CancellationToken cancellationToken = default);
        Task UpsertAsync(SpellRecord spell, CancellationToken cancellationToken = default);
        Task<SpellRecord?> GetAsync(string id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SpellRecord>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<int> SizeAsync(CancellationToken cancellationToken = default);
    }
}
