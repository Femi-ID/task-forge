using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        public static WorkspaceMemberDto ToWorkspaceMemberDto(WorkspaceMember workspaceMember)
        {
            return new WorkspaceMemberDto
            {
                AppUserId = workspaceMember.AppUserId,
                WorkspaceId = workspaceMember.WorkspaceId,
                FullName = workspaceMember.AppUser.FullName,
                Email = workspaceMember.AppUser.Email!,
                Role = workspaceMember.Role,
                JoinedAt = workspaceMember.JoinedAt,
                IsOwner = workspaceMember.AppUserId == workspaceMember.Workspace.OwnerId
            };
        }

        public static readonly Expression<Func<WorkspaceMember, WorkspaceMemberDto>> ToDto = m => new WorkspaceMemberDto
        {
            AppUserId = m.AppUserId,
            WorkspaceId = m.WorkspaceId,
            FullName = m.AppUser.FullName,
            Email = m.AppUser.Email!,
            Role = m.Role,
            JoinedAt = m.JoinedAt,
            IsOwner = m.AppUserId == m.Workspace.OwnerId
        };
    }
}