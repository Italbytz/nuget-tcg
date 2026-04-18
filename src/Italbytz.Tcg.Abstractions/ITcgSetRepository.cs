using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Italbytz.Tcg.Abstractions;

public interface ITcgSetRepository
{
    Task UpsertAsync(TcgSet set, CancellationToken cancellationToken = default);

    Task<TcgSet?> GetByIdAsync(string game, string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TcgSet>> ListByGameAsync(string game, CancellationToken cancellationToken = default);

    Task RemoveAsync(string game, string id, CancellationToken cancellationToken = default);
}