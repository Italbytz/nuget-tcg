using System.Threading;
using System.Threading.Tasks;

namespace Italbytz.Tcg.Abstractions;

public interface ITcgCardSource
{
    Task<TcgPage<TcgCard>> SearchCardsAsync(TcgCardQuery query, CancellationToken cancellationToken = default);

    Task<TcgCard?> GetCardByIdAsync(string game, string id, CancellationToken cancellationToken = default);
}