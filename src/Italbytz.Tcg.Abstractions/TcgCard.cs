using System;
using System.Collections.Generic;

namespace Italbytz.Tcg.Abstractions;

public class TcgCard
{
    public string Game { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? LocalId { get; set; }

    public string? Number { get; set; }

    public string? Category { get; set; }

    public string? Supertype { get; set; }

    public string? Rarity { get; set; }

    public string? Illustrator { get; set; }

    public string? SetId { get; set; }

    public string? SetName { get; set; }

    public Uri? ImageUri { get; set; }

    public IDictionary<string, string> Attributes { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}