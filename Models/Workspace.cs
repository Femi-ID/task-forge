using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class Workspace: IUpdatable
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }
        public AppUser Owner { get; set; } = null!; // this is the navigation to the user table
        public List<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
        public List<Project> Projects { get; set; } = new List<Project>();
        public List<Label> Labels { get; set; } = [];  
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}