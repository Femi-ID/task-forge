using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Enums;

namespace api.Models
{
    public class Project
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public ProjectStatus Status { get; set; }
         public required string WorkspaceId { get; set; }
        public required Workspace Workspace { get; set; }
        public List<Task> Tasks { get; set; } = new List<Task>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}