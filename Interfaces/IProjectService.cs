using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos;
using api.Dtos.Projects;
using api.Helpers;
using api.Models;

namespace api.Interfaces
{
    public interface IProjectService
    {
        Task<Result<ProjectResponseDto>> CreateProjectAsync(CreateProjectDto dto, Guid workspaceId, Guid userId);
        Task<Result<List<ProjectResponseDtoList>>> GetAllProjectAsync(Guid workspaceId, Guid userId, ProjectQueryObject query);
        Task<Result<ProjectResponseDto>> GetProjectByIdAsync(Guid workspaceId, Guid projectId, Guid userId);
        Task<Result<ProjectResponseDto>> UpdateProjectAsync(Guid workspaceId, Guid projectId, Guid userId, UpdateProjectDto dto);
        Task<Result<bool>> DeleteProjectAsync(Guid workspaceId, Guid projectId, Guid userId);
    }
}