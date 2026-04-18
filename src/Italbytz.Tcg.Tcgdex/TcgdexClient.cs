using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Italbytz.Tcg.Abstractions;

namespace Italbytz.Tcg.Tcgdex;

public sealed class TcgdexClient : ITcgCatalog, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;
    private readonly TcgdexOptions _options;
    private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public TcgdexClient(HttpClient? httpClient = null, TcgdexOptions? options = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _disposeHttpClient = httpClient is null;
        _options = options ?? new TcgdexOptions();
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

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private static void EnsurePokemonGame(string game)
    {
        if (!string.IsNullOrWhiteSpace(game) && !string.Equals(game, TcgGameIds.Pokemon, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException("TCGDex currently supports Pokemon only.");
        }
    }

    private string BuildPath(string resource, IDictionary<string, string> filters)
    {
        if (filters.Count == 0)
        {
            return resource;
        }

        var query = string.Join("&", filters.Select(filter => $"{Uri.EscapeDataString(filter.Key)}={Uri.EscapeDataString(filter.Value)}"));
        return $"{resource}?{query}";
    }

    private async Task<T?> GetAsync<T>(string relativePath, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(BuildAbsoluteUri(relativePath), cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(json, _serializerOptions);
    }

    private string BuildAbsoluteUri(string relativePath)
    {
        return $"{_options.BaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
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
            ImageUri = BuildImageUri(card.Image)
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
            ReleaseDate = ParseDate(set.ReleaseDate),
            LogoUri = BuildImageUri(set.Logo),
            SymbolUri = BuildImageUri(set.Symbol)
        };

        if (!string.IsNullOrWhiteSpace(set.TcgOnline))
        {
            mappedSet.Metadata["tcgOnline"] = set.TcgOnline!;
        }

        return mappedSet;
    }

    private static Uri? BuildImageUri(string? image)
    {
        if (string.IsNullOrWhiteSpace(image))
        {
            return null;
        }

        var normalizedImage = image!;
        var imagePath = normalizedImage.EndsWith("/high.png", StringComparison.OrdinalIgnoreCase)
            ? normalizedImage
            : normalizedImage.TrimEnd('/') + "/high.png";

        return Uri.TryCreate(imagePath, UriKind.Absolute, out var imageUri) ? imageUri : null;
    }

    private static DateTimeOffset? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var releaseDate)
            ? releaseDate
            : null;
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