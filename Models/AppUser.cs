using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace api.Models
{
    public class AppUser : IdentityUser
    {
        // custom fields
        public Guid Uuid { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //A user can have many workspaces and a workspace can have many users- WOrkspaceMembers handles this
        public List<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
        public List<Task> Tasks { get; set; } = new List<Task>();
        public List<Comment> Comments { get; set; } = new List<Comment>();

        // refresh-token fields
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}