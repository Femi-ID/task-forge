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
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class ProjectService(
        AppDbContext context,
        IWorkspaceAuthorization workspaceAuth) : IProjectService
    {
        private const string NameTakenMessage = "A project with this name already exists in this workspace (it may be archived).";

        public async Task<Result<ProjectResponseDto>> CreateProjectAsync(CreateProjectDto dto, Guid workspaceId, Guid userId)
        {
            // confirm if the requesting user is an admin in that workspace
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, userId);
            if (!requesterIsAdmin) return Result<ProjectResponseDto>.Failure("Only workspace admins can create a project.", ResultError.Forbidden);

            // check first that the name does not exist, unique index on (WorkspaceId, Name)
            if (await ProjectNameTakenAsync(workspaceId, dto.Name.Trim()))
                return Result<ProjectResponseDto>.Failure(NameTakenMessage, ResultError.Conflict);

            // save the new project
            var projectModel = ProjectMappers.ToProjectModelFromCreateProjectDto(dto, workspaceId);
            await context.Projects.AddAsync(projectModel);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
            {
                // if two requests passed the check above at the same moment, the index caught the loser
                return Result<ProjectResponseDto>.Failure(NameTakenMessage, ResultError.Conflict);
            }

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

        public async Task<Result<PagedResult<ProjectResponseDtoList>>> GetAllProjectAsync(Guid workspaceId, Guid userId, ProjectQueryObject query)
        {
            // confirm the user is a member
            var isMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, userId);
            if (isMember is false) return Result<PagedResult<ProjectResponseDtoList>>.Failure("Only workspace members can fetch this result.", ResultError.Forbidden);

            var projects = context.Projects.Where(p => p.WorkspaceId == workspaceId).AsQueryable();

            // queries
            if (!string.IsNullOrWhiteSpace(query.Name))
            {
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

            // count BEFORE ordering/paging so the total reflects every match, not just this page
            var totalCount = await projects.CountAsync();

            var orderedProjects = (query.SortBy ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "name" => query.IsDescending ? projects.OrderByDescending(p => p.Name) : projects.OrderBy(p => p.Name),
                "status" => query.IsDescending ? projects.OrderByDescending(p => p.Status) : projects.OrderBy(p => p.Status),
                "updatedat" => query.IsDescending ? projects.OrderByDescending(p => p.UpdatedAt) : projects.OrderBy(p => p.UpdatedAt),
                "createdat" => query.IsDescending ? projects.OrderByDescending(p => p.CreatedAt) : projects.OrderBy(p => p.CreatedAt),
                _ => projects.OrderByDescending(p => p.CreatedAt) // default: newest first
            };
            orderedProjects = orderedProjects.ThenBy(p => p.Id); // tie-breaker so pages never overlap or skip rows

            //  sorting
            // if (!string.IsNullOrWhiteSpace(query.SortBy))
            // {
            //     // Compare SortBy with StringComparison.OrdinalIgnoreCase, and use a switch instead of four separate ifs.
            //     if (query.SortBy.Equals("Name", StringComparison.OrdinalIgnoreCase))
            //         projects = query.IsDescending ? projects.OrderByDescending(x => x.Name) : projects.OrderBy(x => x.Name);
            //     if (query.SortBy.Equals("Status", StringComparison.OrdinalIgnoreCase))
            //         projects = query.IsDescending ? projects.OrderByDescending(x => x.Status) : projects.OrderBy(x => x.Status);
            //     if (query.SortBy.Equals("UpdatedAt", StringComparison.OrdinalIgnoreCase))
            //         projects = query.IsDescending ? projects.OrderByDescending(x => x.UpdatedAt) : projects.OrderBy(x => x.UpdatedAt);
            //     if (query.SortBy.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase))
            //         projects = query.IsDescending ? projects.OrderByDescending(x => x.CreatedAt) : projects.OrderBy(x => x.CreatedAt);
            // }
            // else
            // {
            //     projects = projects.OrderByDescending(x => x.CreatedAt);
            //     projects = projects.OrderBy(x => x.Id);
            // }

            // var page = Math.Max(1, query.PageNumber);
            // var size = Math.Clamp(query.PageSize, 1, 20);
            var (page, size) = PaginationHelper.Normalize(query.PageNumber, query.PageSize);

            // var projectLists = await projects.Skip((page - 1) * size).Take(size).ToListAsync();
            // var projectListsDto = ProjectMappers.ToProjectResponseDtoList(projectLists);
            // return Result<List<ProjectResponseDtoList>>.Success(projectListsDto);
            var items = await orderedProjects
                .Skip((page - 1) * size)
                .Take(size)
                .Select(ProjectMappers.ToListDto)
                .ToListAsync();

            var pagedProjects = new PagedResult<ProjectResponseDtoList>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = size
            };
            return Result<PagedResult<ProjectResponseDtoList>>.Success(pagedProjects);
        }

        public async Task<Result<ProjectResponseDto>> GetProjectByIdAsync(Guid workspaceId, Guid projectId, Guid userId)
        {
            // confirm the user is a member
            var isMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, userId);
            if (isMember is false) return Result<ProjectResponseDto>.Failure("Only workspace members can fetch this result.", ResultError.Forbidden);

            // i want the project to display the tasks when fetched- projected straight to the DTO: tasks come back as { id, title, status, priority } pointers, never as entities
            var projectDto = await context.Projects
                .AsNoTracking()
                .Include(p => p.Tasks) // to feed the TaskSummaryDto
                .Select(ProjectMappers.ToDetailDto)
                .FirstOrDefaultAsync(p => p.Id == projectId && p.WorkspaceId == workspaceId);
            if (projectDto is null) return Result<ProjectResponseDto>.Failure("Project not found", ResultError.NotFound);

            // var projectResponseDto = ProjectMappers.ToProjectDto(project);
            return Result<ProjectResponseDto>.Success(projectDto);
        }

        public async Task<Result<ProjectResponseDto>> UpdateProjectAsync(Guid workspaceId, Guid projectId, Guid userId, UpdateProjectDto dto)
        {
            // confirm if the requesting user is an admin in that workspace
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, userId);
            if (!requesterIsAdmin) return Result<ProjectResponseDto>.Failure("Only workspace admins can update a project", ResultError.Forbidden);

            var existingProject = await context.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.WorkspaceId == workspaceId);
            if (existingProject is null) return Result<ProjectResponseDto>.Failure("Project not found", ResultError.NotFound);

            // Add the updates
            if (!string.IsNullOrWhiteSpace(dto.Name)) 
            {
                var newName = dto.Name.Trim();
                // excluding this project lets "alpha" -> "Alpha" (case-only rename) through
                if (await ProjectNameTakenAsync(workspaceId, newName, excludeProjectId: projectId))
                    return Result<ProjectResponseDto>.Failure(NameTakenMessage, ResultError.Conflict);
                existingProject.Name = dto.Name;
            }

            // null = unchanged, while "" = clear the description
            if (dto.Description is not null) 
                existingProject.Description = dto.Description.Length == 0 ? null : dto.Description;

            if (dto.Status.HasValue) existingProject.Status = dto.Status.Value;
            existingProject.UpdatedAt = DateTime.UtcNow;

            try
            {
                await context.SaveChangesAsync(); // AppDbContext stamps UpdatedAt automatically
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
            {
                return Result<ProjectResponseDto>.Failure(NameTakenMessage, ResultError.Conflict);
            }
            var projectResponseDto = ProjectMappers.ToProjectDto(existingProject);
            return Result<ProjectResponseDto>.Success(projectResponseDto);
        }

        private async Task<bool> ProjectNameTakenAsync(Guid workspaceId, string name, Guid excludeProjectId = default)
        {
            var loweredName = name.ToLower();
            return await context.Projects.AnyAsync(p =>
                p.WorkspaceId == workspaceId &&
                p.Id != excludeProjectId &&
                p.Name.ToLower() == loweredName);
        }
    }
}