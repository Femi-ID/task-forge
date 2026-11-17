using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.ActivityLogs
{
    public class ActivityLogQueryObject
    {
        public Guid? TaskItemId { get; set; } // optional: narrow to one task's history instead of the whole workspace feed
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}