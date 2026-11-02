using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos;
using api.Dtos.Workspace;
using api.Dtos.WorkspaceMember;
using api.Enums;
using api.Helpers;
using api.Models;

namespace api.Interfaces
{
    public interface IWorkspace
    {
        Task<Result<WorkspaceDto>> CreateWorkspaceAsync(CreateWorkspaceDto workspaceDto, Guid userId);
        Task<Result<PagedResult<WorkspaceDto>>> GetAllWorkspacesAsync(WorkspaceQueryObject query, Guid userId);
        Task<Result<WorkspaceDto>> GetWorkspaceByIdAsync(Guid Id, Guid userId);
        Task<Result<WorkspaceDto>> UpdateWorkspaceAsync(Guid Id, UpdateWorkspaceDto updateDto, Guid userId);
        Task<Result<bool>> DeleteWorkspaceAsync(Guid Id, Guid userId);
        Task<Result<bool>> TransferWorkspaceOwnershipAsync(Guid workspaceId, Guid currentOwnerId, Guid newOwnerId);
    }
}