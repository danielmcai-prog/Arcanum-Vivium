using System.Threading;
using System.Threading.Tasks;
using ArcanumVivum.SpellEngine.Models;

namespace ArcanumVivum.SpellEngine.Resolvers
{
    public interface ISymbolSpellResolver
    {
        Task<ResolverOutput?> ResolveSymbolAsync(SpellFeatures? features, string? incantation, CancellationToken cancellationToken = default);
    }

    public interface IAudioSpellResolver
    {
        Task<ResolverOutput?> ResolveAudioAsync(string transcribedText, CancellationToken cancellationToken = default);
    }
}
