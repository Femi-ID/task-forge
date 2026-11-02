using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Enums;

namespace api.Dtos.WorkspaceMember
{
    public class WorkspaceMemberDto
    {
        public Guid AppUserId { get; set; }
        public Guid WorkspaceId { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public WorkspaceRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsOwner { get; set; }
    }
}