using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Workspace;
using api.Helpers;
using api.Models;

namespace api.Interfaces
{
    public interface IWorkspace
    {
        Task<Workspace> CreateWorkspaceAsync(CreateWorkspaceDto workspaceDto, Guid userId);
        Task<List<Workspace>> GetAllWorkspacesAsync(WorkspaceQueryObject query);
        Task<Workspace?> GetWorkspaceByIdAsync(Guid Id);
        Task<Workspace?> UpdateWorkspaceAsync(Guid Id, UpdateWorkspaceDto updateDto);
        Task<bool> DeleteWorkspaceAsync(Guid Id, Guid OwnerId);
        Task<Workspace?> WorkspaceExists(Guid Id);
    }
}