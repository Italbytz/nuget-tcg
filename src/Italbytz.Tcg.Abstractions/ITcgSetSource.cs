using System.Threading;
using System.Threading.Tasks;

namespace Italbytz.Tcg.Abstractions;

public interface ITcgSetSource
{
    Task<TcgPage<TcgSet>> GetSetsAsync(string game, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<TcgSet?> GetSetByIdAsync(string game, string id, CancellationToken cancellationToken = default);
}