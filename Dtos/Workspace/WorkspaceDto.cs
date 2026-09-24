using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using api.Models;

namespace api.Dtos.Workspace
{
    public class WorkspaceDto
    {
        public Guid Id { get; set; }

        [Required]
        public required string Name { get; set; } = null!;
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }
        // public AppUser Owner { get; set; } = null!;
        // public List<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
        // public List<Project> Projects { get; set; } = new List<Project>();
        // public List<Label> Labels { get; set; } = [];  
        // public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // public DateTime? UpdatedAt { get; set; }
    }
}