using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos;
using api.Dtos.Workspace;
using api.Dtos.WorkspaceMember;
using api.Helpers;
using api.Models;

namespace api.Interfaces
{
    public interface IWorkspace
    {
        Task<Result<Workspace>> CreateWorkspaceAsync(CreateWorkspaceDto workspaceDto, Guid userId);
        Task<Result<List<Workspace>>> GetAllWorkspacesAsync(WorkspaceQueryObject query, Guid userId);
        Task<Result<Workspace?>> GetWorkspaceByIdAsync(Guid Id, Guid userId);
        Task<Result<Workspace?>> UpdateWorkspaceAsync(Guid Id, UpdateWorkspaceDto updateDto, Guid userId);
        Task<Result<bool>> DeleteWorkspaceAsync(Guid Id, Guid userId);
        Task<Workspace?> GetTrackedWorkspaceAsync(Guid Id);
        Task<Result<WorkspaceMember>> AddWorkspaceMemberAsync(Guid workspaceId, Guid requestingUserId, CreateWorkspaceMemberDto dto);
    }
}