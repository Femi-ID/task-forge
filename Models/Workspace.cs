using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class Workspace
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public required string OwnerId { get; set; }
        public required AppUser AppUser { get; set; }
        public List<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
        public List<Project> Projects { get; set; } = new List<Project>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}