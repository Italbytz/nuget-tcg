using System;
using System.Collections.Generic;

namespace Italbytz.Tcg.Abstractions;

public class TcgSet
{
    public string Game { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? SeriesName { get; set; }

    public int? CardCount { get; set; }

    public DateTimeOffset? ReleaseDate { get; set; }

    public Uri? LogoUri { get; set; }

    public Uri? SymbolUri { get; set; }

    public IDictionary<string, string> Metadata { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}