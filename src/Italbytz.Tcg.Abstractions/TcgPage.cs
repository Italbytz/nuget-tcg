using System.Collections.Generic;

namespace Italbytz.Tcg.Abstractions;

public class TcgPage<T>
{
    public TcgPage(IReadOnlyList<T> items, int pageNumber, int pageSize, int? totalCount = null)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }

    public int PageNumber { get; }

    public int PageSize { get; }

    public int? TotalCount { get; }
}