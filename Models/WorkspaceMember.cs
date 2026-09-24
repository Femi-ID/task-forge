using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Enums;

namespace api.Models
{
    public class WorkspaceMember // this is a join table of users and workspaces
    {
        public Guid AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        public Guid WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;

        public WorkspaceRole Role { get; set; } = WorkspaceRole.Member;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}