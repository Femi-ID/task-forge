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
        Task<Workspace> CreateWorkspaceAsync(CreateWorkspaceDto workspaceDto, Guid userId);
        Task<List<Workspace>> GetAllWorkspacesAsync(WorkspaceQueryObject query, Guid userId);
        Task<Workspace?> GetWorkspaceByIdAsync(Guid Id, Guid userId);
        Task<Workspace?> UpdateWorkspaceAsync(Guid Id, UpdateWorkspaceDto updateDto, Guid userId);
        Task<bool> DeleteWorkspaceAsync(Guid Id, Guid OwnerId);
        Task<Workspace?> GetTrackedWorkspaceAsync(Guid Id);
        // IWorkspace
        Task<Result<WorkspaceMember>> AddWorkspaceMemberAsync(Guid workspaceId, Guid requestingUserId, CreateWorkspaceMemberDto dto);
    }
}