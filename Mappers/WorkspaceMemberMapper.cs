using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.WorkspaceMember;
using api.Enums;
using api.Models;

namespace api.Mappers
{
    public static class WorkspaceMemberMapper
    {
        public static WorkspaceMember ToWorkspaceMemberModelFromCreateDto(Guid userId, Guid workspaceId, WorkspaceRole role)
        {
            return new WorkspaceMember
            {
                AppUserId = userId,
                WorkspaceId = workspaceId,
                Role = role,
                JoinedAt = DateTime.UtcNow
            };
        }
    }
}