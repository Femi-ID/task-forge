using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.ActivityLogs
{
    public class ActivityLogDto
    {
        public Guid Id { get; set; }
        public required string ActorFullName { get; set; }
        public required string Action { get; set; }
        public required string Description { get; set; }
        public Guid? TaskItemId { get; set; }
        public Guid? ProjectId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}