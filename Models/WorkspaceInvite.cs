using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Enums;

namespace api.Models
{
    public class WorkspaceInvite
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;
        public required string InvitedEmail { get; set; }
        public required string TokenHash { get; set; }
        public required WorkspaceRole Role { get; set; }
        public required WorkspaceInviteStatus Status { get; set; }
        public Guid InvitedByUserId { get; set; }
        public AppUser InvitedByUser { get; set; } = null!;
        public DateTime ExpireAt { get; set; } = DateTime.UtcNow.AddHours(24);
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}