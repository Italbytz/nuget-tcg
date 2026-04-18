using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Italbytz.Tcg.Abstractions;

namespace Italbytz.Tcg.InMemory;

public class InMemoryTcgCardRepository : ITcgCardRepository
{
    private readonly ConcurrentDictionary<string, TcgCard> _cards = new ConcurrentDictionary<string, TcgCard>(StringComparer.OrdinalIgnoreCase);

    public Task UpsertAsync(TcgCard card, CancellationToken cancellationToken = default)
    {
        _cards[BuildKey(card.Game, card.Id)] = card;
        return Task.CompletedTask;
    }

    public Task<TcgCard?> GetByIdAsync(string game, string id, CancellationToken cancellationToken = default)
    {
        _cards.TryGetValue(BuildKey(game, id), out var card);
        return Task.FromResult<TcgCard?>(card);
    }

    public Task<IReadOnlyList<TcgCard>> ListByGameAsync(string game, CancellationToken cancellationToken = default)
    {
        var cards = _cards.Values
            .Where(card => string.Equals(card.Game, game, StringComparison.OrdinalIgnoreCase))
            .OrderBy(card => card.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult<IReadOnlyList<TcgCard>>(cards);
    }

    public Task RemoveAsync(string game, string id, CancellationToken cancellationToken = default)
    {
        _cards.TryRemove(BuildKey(game, id), out _);
        return Task.CompletedTask;
    }

    private static string BuildKey(string game, string id) => $"{game}::{id}";
}