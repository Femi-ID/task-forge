using System;
using System.Collections.Generic;

namespace api.Dtos
{
    /// <summary>
    /// Standard envelope for paginated list endpoints so a frontend can render page controls.
    /// </summary>
    public class PagedResult<T>
    {
        public required IReadOnlyList<T> Items { get; init; }
        public int TotalCount { get; init; }
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
