using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Helpers
{
    public static class PaginationHelper
    {
        public const int MaxPageSize = 20;
        private const int MaxPageNumber = 100_000; // keeps (page - 1) * size far away from int overflow

        /// <summary>Clamps client-supplied paging values to safe bounds.</summary>
        public static (int Page, int Size) Normalize(int pageNumber, int pageSize) =>
            (Math.Clamp(pageNumber, 1, MaxPageNumber), Math.Clamp(pageSize, 1, MaxPageSize));
    }
}