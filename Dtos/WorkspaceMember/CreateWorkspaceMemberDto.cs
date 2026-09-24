using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Enums;

namespace api.Dtos.WorkspaceMember
{
    public class CreateWorkspaceMemberDto
    {
        public Guid AppUserId { get; set; }

        public Guid WorkspaceId { get; set; }

        public WorkspaceRole Role { get; set; } = WorkspaceRole.Admin;
    }
}