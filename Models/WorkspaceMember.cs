using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Enums;

namespace api.Models
{
    public class WorkspaceMember // this is a join table of users and workspaces
    {
        public required string AppUserId { get; set; }
        public required AppUser AppUser { get; set; }
        public required string WorkspaceId { get; set; }
        public required Workspace Workspace { get; set; }
        public WorkspaceRole Role { get; set; }
    }
}