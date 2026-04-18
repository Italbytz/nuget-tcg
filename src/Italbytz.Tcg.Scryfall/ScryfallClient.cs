using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Italbytz.Tcg.Abstractions;

namespace Italbytz.Tcg.Scryfall;

public sealed class ScryfallClient : ITcgCatalog, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;
    private readonly ScryfallOptions _options;
    private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public ScryfallClient(HttpClient? httpClient = null, ScryfallOptions? options = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _disposeHttpClient = httpClient is null;
        _options = options ?? new ScryfallOptions();
    }

    public async Task<TcgPage<TcgCard>> SearchCardsAsync(TcgCardQuery query, CancellationToken cancellationToken = default)
    {
        EnsureMtgGame(query.Game);

        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["q"] = BuildSearchExpression(query),
            ["page"] = query.PageNumber.ToString(CultureInfo.InvariantCulture)
        };

        var response = await GetAsync<ScryfallListResponse<ScryfallCardDto>>(BuildPath("cards/search", parameters), cancellationToken).ConfigureAwait(false);
        var cards = response?.Data?.Select(MapCard).ToArray() ?? Array.Empty<TcgCard>();

        return new TcgPage<TcgCard>(cards, query.PageNumber, query.PageSize, response?.TotalCards);
    }

    public async Task<TcgCard?> GetCardByIdAsync(string game, string id, CancellationToken cancellationToken = default)
    {
        EnsureMtgGame(game);

        var card = await GetAsync<ScryfallCardDto>($"cards/{Uri.EscapeDataString(id)}", cancellationToken).ConfigureAwait(false);
        return card is null ? null : MapCard(card);
    }

    public async Task<TcgPage<TcgSet>> GetSetsAsync(string game, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        EnsureMtgGame(game);

        var response = await GetAsync<ScryfallListResponse<ScryfallSetDto>>("sets", cancellationToken).ConfigureAwait(false);
        var sets = response?.Data?.Select(MapSet).ToArray() ?? Array.Empty<TcgSet>();
        var pagedSets = sets.Skip(Math.Max(0, (pageNumber - 1) * pageSize)).Take(pageSize).ToArray();

        return new TcgPage<TcgSet>(pagedSets, pageNumber, pageSize, sets.Length);
    }

    public async Task<TcgSet?> GetSetByIdAsync(string game, string id, CancellationToken cancellationToken = default)
    {
        EnsureMtgGame(game);

        var set = await GetAsync<ScryfallSetDto>($"sets/{Uri.EscapeDataString(id)}", cancellationToken).ConfigureAwait(false);
        return set is null ? null : MapSet(set);
    }

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private static void EnsureMtgGame(string game)
    {
        if (!string.IsNullOrWhiteSpace(game) && !string.Equals(game, TcgGameIds.MagicTheGathering, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException("Scryfall currently supports Magic: The Gathering only.");
        }
    }

    private static string BuildSearchExpression(TcgCardQuery query)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.Text))
        {
            parts.Add(query.Text!);
        }

        foreach (var filter in query.Filters)
        {
            parts.Add($"{filter.Key}:{filter.Value}");
        }

        return parts.Count == 0 ? "game:paper" : string.Join(" ", parts);
    }

    private string BuildPath(string resource, IDictionary<string, string> parameters)
    {
        if (parameters.Count == 0)
        {
            return resource;
        }

        var query = string.Join("&", parameters.Select(parameter => $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
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

    private static TcgCard MapCard(ScryfallCardDto card)
    {
        var mappedCard = new TcgCard
        {
            Game = TcgGameIds.MagicTheGathering,
            Id = card.Id ?? string.Empty,
            Name = card.Name ?? string.Empty,
            Number = card.CollectorNumber,
            Category = card.Layout,
            Supertype = card.TypeLine,
            Rarity = card.Rarity,
            SetId = card.Set,
            SetName = card.SetName,
            ImageUri = BuildImageUri(card.ImageUris?.Normal, card.ImageUris?.Large)
        };

        if (!string.IsNullOrWhiteSpace(card.ManaCost))
        {
            mappedCard.Attributes["manaCost"] = card.ManaCost!;
        }

        if (!string.IsNullOrWhiteSpace(card.OracleText))
        {
            mappedCard.Attributes["oracleText"] = card.OracleText!;
        }

        if (card.ColorIdentity is { Length: > 0 })
        {
            mappedCard.Attributes["colorIdentity"] = string.Join(",", card.ColorIdentity);
        }

        return mappedCard;
    }

    private static TcgSet MapSet(ScryfallSetDto set)
    {
        var mappedSet = new TcgSet
        {
            Game = TcgGameIds.MagicTheGathering,
            Id = set.Code ?? string.Empty,
            Name = set.Name ?? string.Empty,
            ReleaseDate = ParseDate(set.ReleasedAt),
            LogoUri = BuildImageUri(set.IconSvgUri, null),
            SymbolUri = BuildImageUri(set.IconSvgUri, null)
        };

        if (set.CardCount.HasValue)
        {
            mappedSet.CardCount = set.CardCount.Value;
        }

        if (!string.IsNullOrWhiteSpace(set.SetType))
        {
            mappedSet.Metadata["setType"] = set.SetType!;
        }

        return mappedSet;
    }

    private static Uri? BuildImageUri(string? preferred, string? fallback)
    {
        var candidate = !string.IsNullOrWhiteSpace(preferred) ? preferred : fallback;
        return Uri.TryCreate(candidate, UriKind.Absolute, out var imageUri) ? imageUri : null;
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

    private sealed class ScryfallListResponse<T>
    {
        [JsonPropertyName("data")]
        public T[]? Data { get; set; }

        [JsonPropertyName("total_cards")]
        public int? TotalCards { get; set; }
    }

    private sealed class ScryfallCardDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("collector_number")]
        public string? CollectorNumber { get; set; }

        [JsonPropertyName("layout")]
        public string? Layout { get; set; }

        [JsonPropertyName("type_line")]
        public string? TypeLine { get; set; }

        [JsonPropertyName("rarity")]
        public string? Rarity { get; set; }

        [JsonPropertyName("set")]
        public string? Set { get; set; }

        [JsonPropertyName("set_name")]
        public string? SetName { get; set; }

        [JsonPropertyName("mana_cost")]
        public string? ManaCost { get; set; }

        [JsonPropertyName("oracle_text")]
        public string? OracleText { get; set; }

        [JsonPropertyName("color_identity")]
        public string[]? ColorIdentity { get; set; }

        [JsonPropertyName("image_uris")]
        public ScryfallImageUrisDto? ImageUris { get; set; }
    }

    private sealed class ScryfallImageUrisDto
    {
        [JsonPropertyName("normal")]
        public string? Normal { get; set; }

        [JsonPropertyName("large")]
        public string? Large { get; set; }
    }

    private sealed class ScryfallSetDto
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("released_at")]
        public string? ReleasedAt { get; set; }

        [JsonPropertyName("icon_svg_uri")]
        public string? IconSvgUri { get; set; }

        [JsonPropertyName("card_count")]
        public int? CardCount { get; set; }

        [JsonPropertyName("set_type")]
        public string? SetType { get; set; }
    }
}