using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Italbytz.Tcg.Abstractions;

namespace Italbytz.Tcg.InMemory;

public class InMemoryTcgSetRepository : ITcgSetRepository
{
    private readonly ConcurrentDictionary<string, TcgSet> _sets = new ConcurrentDictionary<string, TcgSet>(StringComparer.OrdinalIgnoreCase);

    public Task UpsertAsync(TcgSet set, CancellationToken cancellationToken = default)
    {
        _sets[BuildKey(set.Game, set.Id)] = set;
        return Task.CompletedTask;
    }

    public Task<TcgSet?> GetByIdAsync(string game, string id, CancellationToken cancellationToken = default)
    {
        _sets.TryGetValue(BuildKey(game, id), out var set);
        return Task.FromResult<TcgSet?>(set);
    }

    public Task<IReadOnlyList<TcgSet>> ListByGameAsync(string game, CancellationToken cancellationToken = default)
    {
        var sets = _sets.Values
            .Where(set => string.Equals(set.Game, game, StringComparison.OrdinalIgnoreCase))
            .OrderBy(set => set.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult<IReadOnlyList<TcgSet>>(sets);
    }

    public Task RemoveAsync(string game, string id, CancellationToken cancellationToken = default)
    {
        _sets.TryRemove(BuildKey(game, id), out _);
        return Task.CompletedTask;
    }

    private static string BuildKey(string game, string id) => $"{game}::{id}";
}