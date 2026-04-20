using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Italbytz.Common.Http;
using Italbytz.Tcg.Abstractions;

namespace Italbytz.Tcg.Tcgdex;

public sealed class TcgdexClient : HttpJsonApiClientBase, ITcgCatalog
{
    public TcgdexClient(HttpClient? httpClient = null, TcgdexOptions? options = null)
        : base((options ?? new TcgdexOptions()).BaseUrl, httpClient)
    {
    }

    public async Task<TcgPage<TcgCard>> SearchCardsAsync(TcgCardQuery query, CancellationToken cancellationToken = default)
    {
        EnsurePokemonGame(query.Game);

        var filters = new Dictionary<string, string>(query.Filters, StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(query.Text) && !filters.ContainsKey("name"))
        {
            filters["name"] = query.Text!;
        }

        filters["pagination:page"] = query.PageNumber.ToString(CultureInfo.InvariantCulture);
        filters["pagination:itemsPerPage"] = query.PageSize.ToString(CultureInfo.InvariantCulture);

        var path = BuildPath("cards", filters);
        var cards = await GetAsync<TcgdexCardDto[]>(path, cancellationToken).ConfigureAwait(false) ?? Array.Empty<TcgdexCardDto>();
        var mappedCards = cards.Select(MapCard).ToArray();

        return new TcgPage<TcgCard>(mappedCards, query.PageNumber, query.PageSize);
    }

    public async Task<TcgCard?> GetCardByIdAsync(string game, string id, CancellationToken cancellationToken = default)
    {
        EnsurePokemonGame(game);

        var card = await GetAsync<TcgdexCardDto>($"cards/{Uri.EscapeDataString(id)}", cancellationToken).ConfigureAwait(false);
        return card is null ? null : MapCard(card);
    }

    public async Task<TcgPage<TcgSet>> GetSetsAsync(string game, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        EnsurePokemonGame(game);

        var filters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["pagination:page"] = pageNumber.ToString(CultureInfo.InvariantCulture),
            ["pagination:itemsPerPage"] = pageSize.ToString(CultureInfo.InvariantCulture)
        };

        var path = BuildPath("sets", filters);
        var sets = await GetAsync<TcgdexSetDto[]>(path, cancellationToken).ConfigureAwait(false) ?? Array.Empty<TcgdexSetDto>();
        var mappedSets = sets.Select(MapSet).ToArray();

        return new TcgPage<TcgSet>(mappedSets, pageNumber, pageSize);
    }

    public async Task<TcgSet?> GetSetByIdAsync(string game, string id, CancellationToken cancellationToken = default)
    {
        EnsurePokemonGame(game);

        var set = await GetAsync<TcgdexSetDto>($"sets/{Uri.EscapeDataString(id)}", cancellationToken).ConfigureAwait(false);
        return set is null ? null : MapSet(set);
    }

    private static void EnsurePokemonGame(string game)
    {
        if (!string.IsNullOrWhiteSpace(game) && !string.Equals(game, TcgGameIds.Pokemon, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException("TCGDex currently supports Pokemon only.");
        }
    }

    private static TcgCard MapCard(TcgdexCardDto card)
    {
        var mappedCard = new TcgCard
        {
            Game = TcgGameIds.Pokemon,
            Id = card.Id ?? string.Empty,
            Name = card.Name ?? string.Empty,
            LocalId = card.LocalId,
            Number = card.LocalId,
            Category = card.Category,
            Supertype = card.Category,
            Rarity = card.Rarity,
            Illustrator = card.Illustrator,
            SetId = card.Set?.Id,
            SetName = card.Set?.Name,
            ImageUri = TcgClientMappingHelpers.BuildHighResolutionImageUri(card.Image)
        };

        if (card.Hp.HasValue)
        {
            mappedCard.Attributes["hp"] = card.Hp.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(card.Stage))
        {
            mappedCard.Attributes["stage"] = card.Stage!;
        }

        if (card.Types is { Length: > 0 })
        {
            mappedCard.Attributes["types"] = string.Join(",", card.Types);
        }

        return mappedCard;
    }

    private static TcgSet MapSet(TcgdexSetDto set)
    {
        var mappedSet = new TcgSet
        {
            Game = TcgGameIds.Pokemon,
            Id = set.Id ?? string.Empty,
            Name = set.Name ?? string.Empty,
            SeriesName = set.Serie?.Name,
            CardCount = set.CardCount?.Total,
            ReleaseDate = TcgClientMappingHelpers.ParseReleaseDate(set.ReleaseDate),
            LogoUri = TcgClientMappingHelpers.BuildHighResolutionImageUri(set.Logo),
            SymbolUri = TcgClientMappingHelpers.BuildHighResolutionImageUri(set.Symbol)
        };

        if (!string.IsNullOrWhiteSpace(set.TcgOnline))
        {
            mappedSet.Metadata["tcgOnline"] = set.TcgOnline!;
        }

        return mappedSet;
    }

    private sealed class TcgdexCardDto
    {
        public string? Id { get; set; }

        public string? LocalId { get; set; }

        public string? Name { get; set; }

        public string? Category { get; set; }

        public string? Rarity { get; set; }

        public string? Illustrator { get; set; }

        public string? Image { get; set; }

        public int? Hp { get; set; }

        public string? Stage { get; set; }

        public string[]? Types { get; set; }

        public TcgdexSetReferenceDto? Set { get; set; }
    }

    private sealed class TcgdexSetReferenceDto
    {
        public string? Id { get; set; }

        public string? Name { get; set; }
    }

    private sealed class TcgdexSetDto
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? Logo { get; set; }

        public string? Symbol { get; set; }

        public string? ReleaseDate { get; set; }

        public string? TcgOnline { get; set; }

        public TcgdexSerieDto? Serie { get; set; }

        public TcgdexCardCountDto? CardCount { get; set; }
    }

    private sealed class TcgdexSerieDto
    {
        public string? Name { get; set; }
    }

    private sealed class TcgdexCardCountDto
    {
        public int? Total { get; set; }
    }
}