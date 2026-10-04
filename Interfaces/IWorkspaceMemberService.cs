using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos;
using api.Dtos.WorkspaceMember;
using api.Enums;
using api.Models;

namespace api.Interfaces
{
    public interface IWorkspaceMemberService
    {
        Task<Result<WorkspaceMember>> AddWorkspaceMemberAsync(Guid workspaceId, Guid requestingUserId, CreateWorkspaceMemberDto dto);
        Task<Result<bool>> RemoveWorkspaceMemberAsync(Guid workspaceId, Guid requestingUserId, Guid targetUserId);
        Task<Result<bool>> RemoveSelfMemberAsync(Guid workspaceId, Guid userId);
        Task<Result<WorkspaceMember?>> UpdateWorkspaceMemberRoleAsync( Guid workspaceId, Guid requestingUserId, Guid targetUserId, WorkspaceRole newRole);
        Task<WorkspaceMember?> GetWorkspaceMember(Guid targetUserId, Guid workspaceId);
        Task<Result<WorkspaceMember>> VerifyAndAcceptInviteAsync(WorkspaceInvite invite, Guid acceptingUserId);
        Task<Result<WorkspaceMember>> AcceptWorkspaceInviteByTokenAsync(string rawToken, Guid acceptingUserId);
        Task<Result<WorkspaceMember>> AcceptWorkspaceInviteByIdAsync(Guid inviteId, Guid acceptingUserId);
    }
}