using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos;
using api.Dtos.Projects;
using api.Enums;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class ProjectService(
        AppDbContext context,
        IWorkspaceAuthorization workspaceAuth) : IProjectService
    {
        public async Task<Result<ProjectResponseDto>> CreateProjectAsync(CreateProjectDto dto, Guid workspaceId, Guid userId)
        {
            // confirm if the requesting user is an admin in that workspace
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, userId);
            if (!requesterIsAdmin) return Result<ProjectResponseDto>.Failure("Only workspace admins can create a project.", ResultError.Forbidden);

            // save the new project
            var projectModel = ProjectMappers.ToProjectModelFromCreateProjectDto(dto, workspaceId);
            await context.Projects.AddAsync(projectModel);
            await context.SaveChangesAsync();

            var projectResponseDto = ProjectMappers.ToProjectDto(projectModel);
            return Result<ProjectResponseDto>.Success(projectResponseDto);
        }

        public async Task<Result<bool>> DeleteProjectAsync(Guid workspaceId, Guid projectId, Guid userId)
        {
            // confirm if the requesting user is an admin in that workspace
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, userId);
            if (!requesterIsAdmin) return Result<bool>.Failure("Only workspace admins can delete a project.", ResultError.Forbidden);

            var project = await context.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.WorkspaceId == workspaceId);
            if (project is null) return Result<bool>.Failure("Project not found", ResultError.NotFound);

            project.Status = ProjectStatus.Archived;
            await context.SaveChangesAsync();
            return Result<bool>.Success(true);
        }

        public async Task<Result<List<ProjectResponseDtoList>>> GetAllProjectAsync(Guid workspaceId, Guid userId, ProjectQueryObject query)
        {
            // confirm the user is a member
            var isMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, userId);
            if (isMember is false) return Result<List<ProjectResponseDtoList>>.Failure("Only workspace members can fetch this result.", ResultError.Forbidden);

            var projects = context.Projects.Where(p => p.WorkspaceId == workspaceId).AsQueryable();

            // queries
            if (!string.IsNullOrWhiteSpace(query.Name))
            {
                // projects = projects.Where(p => p.Name == query.Name);
                foreach (var term in query.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var t = term.ToLower();
                    projects = projects.Where(p => p.Name.ToLower().Contains(t));
                }
            }
            if (query.Status.HasValue)
            {
                projects = projects.Where(p => p.Status == query.Status.Value);
            }
            else projects = projects.Where(p => p.Status != ProjectStatus.Archived);

            if (query.BeforeCreatedAt.HasValue)
            {
                projects = projects.Where(p => p.CreatedAt <= query.BeforeCreatedAt);
            }
            if (query.AfterCreatedAt.HasValue)
            {
                projects = projects.Where(p => p.CreatedAt >= query.AfterCreatedAt);
            }
            if (query.BeforeUpdatedAt.HasValue)
            {
                projects = projects.Where(p => p.UpdatedAt <= query.BeforeUpdatedAt);
            }
            if (query.AfterUpdatedAt.HasValue)
            {
                projects = projects.Where(p => p.UpdatedAt >= query.AfterUpdatedAt);
            }
            if (query.ExactDate.HasValue)
            {
                var day = query.ExactDate.Value.Date;
                projects = projects.Where(p => p.CreatedAt >= day && p.CreatedAt < day.AddDays(1));
            }

            //  sorting
            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                // Compare SortBy with StringComparison.OrdinalIgnoreCase, and use a switch instead of four separate ifs.
                if (query.SortBy.Equals("Name", StringComparison.OrdinalIgnoreCase))
                    projects = query.IsDescending ? projects.OrderByDescending(x => x.Name) : projects.OrderBy(x => x.Name);
                if (query.SortBy.Equals("Status", StringComparison.OrdinalIgnoreCase))
                    projects = query.IsDescending ? projects.OrderByDescending(x => x.Status) : projects.OrderBy(x => x.Status);
                if (query.SortBy.Equals("UpdatedAt", StringComparison.OrdinalIgnoreCase))
                    projects = query.IsDescending ? projects.OrderByDescending(x => x.UpdatedAt) : projects.OrderBy(x => x.UpdatedAt);
                if (query.SortBy.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase))
                    projects = query.IsDescending ? projects.OrderByDescending(x => x.CreatedAt) : projects.OrderBy(x => x.CreatedAt);
            }
            else
            {
                projects = projects.OrderByDescending(x => x.CreatedAt);
                projects = projects.OrderBy(x => x.Id);
            }

            var page = Math.Max(1, query.PageNumber);
            var size = Math.Clamp(query.PageSize, 1, 20);
            
            var skipNumber = (page - 1) * size;
            var projectLists = await projects.Skip(skipNumber).Take(size).ToListAsync();
            var projectListsDto = ProjectMappers.ToProjectResponseDtoList(projectLists);

            return Result<List<ProjectResponseDtoList>>.Success(projectListsDto);

            // var projectListsDto = ProjectMappers.ToProjectResponseDtoList(projects);
            // return Result<List<ProjectResponseDtoList>>.Success(projectListsDto);
        }

        public async Task<Result<ProjectResponseDto>> GetProjectByIdAsync(Guid workspaceId, Guid projectId, Guid userId)
        {
            // confirm the user is a member
            var isMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, userId);
            if (isMember is false) return Result<ProjectResponseDto>.Failure("Only workspace members can fetch this result.", ResultError.Forbidden);

            // i want the project to display the tasks when fetched
            var project = await context.Projects.Include(p => p.Tasks).FirstOrDefaultAsync(p => p.Id == projectId && p.WorkspaceId == workspaceId);
            if (project is null) return Result<ProjectResponseDto>.Failure("Project not found", ResultError.NotFound);

            var projectResponseDto = ProjectMappers.ToProjectDto(project);
            return Result<ProjectResponseDto>.Success(projectResponseDto);
        }

        public async Task<Result<ProjectResponseDto>> UpdateProjectAsync(Guid workspaceId, Guid projectId, Guid userId, UpdateProjectDto dto)
        {
            // confirm if the requesting user is an admin in that workspace
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, userId);
            if (!requesterIsAdmin) return Result<ProjectResponseDto>.Failure("Only workspace admins can update a project", ResultError.Forbidden);

            var existingProject = await context.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.WorkspaceId == workspaceId);
            if (existingProject is null) return Result<ProjectResponseDto>.Failure("Project not found", ResultError.NotFound);

            if (!string.IsNullOrWhiteSpace(dto.Name)) existingProject.Name = dto.Name;
            if (dto.Description is not null) existingProject.Description = dto.Description.Length == 0 ? null : dto.Description;
            if (dto.Status.HasValue) existingProject.Status = (ProjectStatus)dto.Status!; // what if the user wants to remove the description 
            existingProject.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            var projectResponseDto = ProjectMappers.ToProjectDto(existingProject);
            return Result<ProjectResponseDto>.Success(projectResponseDto);
        }
    }
}