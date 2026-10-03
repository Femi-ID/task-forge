using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos;
using api.Dtos.WorkspaceInvite;
using api.Enums;
using api.Models;

namespace api.Mappers
{
    public static class WorkspaceInviteMapper
    {
        public static WorkspaceInvite ToWorkspaceInviteModelFromGenerateInviteDto(GenerateWorkspaceInviteDto dto, string tokenHash, Guid invitedByUserId)
        {
            return new WorkspaceInvite
            {
                WorkspaceId = dto.WorkspaceId,
                InvitedEmail = dto.InvitedEmail,
                TokenHash = tokenHash,
                Role = dto.Role,
                Status = WorkspaceInviteStatus.Pending,
                InvitedByUserId = invitedByUserId
            };
        }

        public static WorkspaceInviteDto ToWorkspaceInviteDto(WorkspaceInvite i)
        {
            return new WorkspaceInviteDto
            {
                Id = i.Id,
                WorkspaceId = i.WorkspaceId,
                WorkspaceName = i.Workspace.Name,
                InvitedByName = i.InvitedByUser.FullName,
                Role = i.Role,
                ExpireAt = i.ExpireAt
            };
        }
    }
}