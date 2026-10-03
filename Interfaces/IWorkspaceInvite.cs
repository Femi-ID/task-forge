using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos;
using api.Dtos.WorkspaceInvite;
using api.Models;

namespace api.Interfaces
{
    public interface IWorkspaceInvite
    {
        Task<Result<WorkspaceInviteResponseDto>> GenerateWorkspaceInviteAsync(GenerateWorkspaceInviteDto dto, Guid invitedByUserId);
        Task<Result<List<WorkspaceInviteDto>>> GetMyInvitesAsync(Guid invitedUserId);
        Task<Result<List<WorkspaceInviteDto>>> GetAllSentWorkspaceInvitesAsync(Guid workspaceId, Guid requestingUserId);
        Task<Result<WorkspaceInviteDto>> GetWorkspaceInviteByIdAsync(Guid workspaceId, Guid inviteId, Guid requestingUserId);
        Task<Result<bool>> RevokeWorkspaceInviteAsync(Guid WorkspaceInviteId, Guid requestingUserId);
    }
}