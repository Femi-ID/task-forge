using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Enums;

namespace api.Models
{
    public class Project: IUpdatable
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public ProjectStatus Status { get; set; } = ProjectStatus.NotStarted;
         public Guid WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}