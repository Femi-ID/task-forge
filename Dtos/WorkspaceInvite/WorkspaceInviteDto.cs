using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Enums;

namespace api.Dtos.WorkspaceInvite
{
    public class WorkspaceInviteDto
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string WorkspaceName { get; set; } = null!;
        public string InvitedByName { get; set; } = null!;
        public WorkspaceRole Role { get; set; }
        public DateTime ExpireAt { get; set; }
    }
}