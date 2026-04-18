using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Italbytz.Tcg.Abstractions;

public interface ITcgCardRepository
{
    Task UpsertAsync(TcgCard card, CancellationToken cancellationToken = default);

    Task<TcgCard?> GetByIdAsync(string game, string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TcgCard>> ListByGameAsync(string game, CancellationToken cancellationToken = default);

    Task RemoveAsync(string game, string id, CancellationToken cancellationToken = default);
}