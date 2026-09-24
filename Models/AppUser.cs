using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace api.Models
{
    public class AppUser : IdentityUser<Guid>
    {
        // custom fields
        public required string FullName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //A user can have many workspaces and a workspace can have many users- WOrkspaceMembers handles this
        public List<WorkspaceMember> WorkspaceMembers { get; set; } = [];
        public List<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
        public List<Comment> Comments { get; set; } = new List<Comment>();

        // refresh-token fields
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}