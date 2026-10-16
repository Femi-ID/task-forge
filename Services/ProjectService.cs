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
        IWorkspaceAuthorization workspaceAuth,
        IWorkspaceMemberService workspaceMemberService) : IProjectService
    {
        public async Task<Result<ProjectResponseDto>> CreateProjectAsync(CreateProjectDto dto, Guid userId)
        {
            // confirm if the requesting user is an admin in that workspace
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(dto.WorkspaceId, userId);
            if (!requesterIsAdmin) return Result<ProjectResponseDto>.Failure("Only workspace admins can create a project.", ResultError.Forbidden);

            // save the new project
            var projectModel = ProjectMappers.ToProjectModelFromCreateProjectDto(dto);
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

            var project = await context.Projects.FindAsync(projectId);
            if (project is null) return Result<bool>.Failure("Project not found", ResultError.NotFound);

            project.Status = ProjectStatus.Archived;
            await context.SaveChangesAsync();
            return Result<bool>.Success(true);
        }

        public async Task<Result<List<ProjectResponseDtoList>>> GetAllProjectAsync(Guid workspaceId, Guid userId, ProjectQueryObject query)
        {
            // confirm the user is a member
            var isMember = await workspaceMemberService.GetWorkspaceMember(userId, workspaceId);
            if (isMember is null) return Result<List<ProjectResponseDtoList>>.Failure("Only workspace members can fetch this result.", ResultError.Forbidden);

            var projects = context.Projects.Where(p => p.WorkspaceId == workspaceId).AsQueryable();

            // queries
            if (!string.IsNullOrWhiteSpace(query.Name))
            {
                // i need this query to check if the word(s) is in the project name and not depend on capital or small letters, i think lexical search
                projects = projects.Where(p => p.Name == query.Name);
            }
            if (query.Status.HasValue)
            {
                projects = projects.Where(p => p.Status == (ProjectStatus)query.Status);
            }
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
                projects = projects.Where(w => w.UpdatedAt == query.ExactDate);
            }

            //  sorting
            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                if (query.SortBy.Equals("Name"))
                    projects = query.IsDescending ? projects.OrderByDescending(x => x.Name) : projects.OrderBy(x => x.Name);
                if (query.SortBy.Equals("Status"))
                    projects = query.IsDescending ? projects.OrderByDescending(x => x.Status) : projects.OrderBy(x => x.Status);
                if (query.SortBy.Equals("UpdatedAt"))
                    projects = query.IsDescending ? projects.OrderByDescending(x => x.UpdatedAt) : projects.OrderBy(x => x.UpdatedAt);
                if (query.SortBy.Equals("CreatedAt"))
                    projects = query.IsDescending ? projects.OrderByDescending(x => x.CreatedAt) : projects.OrderBy(x => x.CreatedAt);
            }

            var skipNumber = (query.PageNumber - 1) * query.PageSize;
            var projectLists = await projects.Skip(skipNumber).Take(query.PageSize).ToListAsync();
            var projectListsDto = ProjectMappers.ToProjectResponseDtoList(projectLists);

            return Result<List<ProjectResponseDtoList>>.Success(projectListsDto);

            // var projectListsDto = ProjectMappers.ToProjectResponseDtoList(projects);
            // return Result<List<ProjectResponseDtoList>>.Success(projectListsDto);
        }

        public async Task<Result<ProjectResponseDto>> GetProjectByIdAsync(Guid workspaceId, Guid projectId, Guid userId)
        {
            // confirm the user is a member
            var isMember = await workspaceMemberService.GetWorkspaceMember(userId, workspaceId);
            if (isMember is null) return Result<ProjectResponseDto>.Failure("Only workspace members can fetch this result.", ResultError.Forbidden);

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
            if (!string.IsNullOrWhiteSpace(dto.Description)) existingProject.Description = dto.Description;
            if (dto.Status.HasValue) existingProject.Status = (ProjectStatus)dto.Status!; // what if the user wants to remove the description 
            existingProject.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            var projectResponseDto = ProjectMappers.ToProjectDto(existingProject);
            return Result<ProjectResponseDto>.Success(projectResponseDto);
        }
    }
}