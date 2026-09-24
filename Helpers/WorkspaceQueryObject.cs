using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Helpers
{
    public class WorkspaceQueryObject
    {
        public string? Name { get; set; }
        public DateTime? BeforeCreatedAt { get; set; }
        public DateTime? AfterCreatedAt { get; set; }
        public DateTime? BeforeUpdatedAt { get; set; }
        public DateTime? AfterUpdatedAt { get; set; }
        public DateTime? ExactDate { get; set; }
        public string? SortBy { get; set; } = null;
        public bool IsDescending { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}