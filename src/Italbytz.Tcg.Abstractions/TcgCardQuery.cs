using System;
using System.Collections.Generic;

namespace Italbytz.Tcg.Abstractions;

public class TcgCardQuery
{
    public string Game { get; set; } = string.Empty;

    public string? Text { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public IDictionary<string, string> Filters { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}