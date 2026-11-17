using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Helpers
{
    public class CommentQueryObject
    {
        public bool IsDescending { get; set; } // default false: oldest first, chronological
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}