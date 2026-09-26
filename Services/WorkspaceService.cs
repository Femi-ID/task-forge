using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos;
using api.Dtos.Workspace;
using api.Dtos.WorkspaceMember;
using api.Enums;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class WorkspaceService(AppDbContext context) : IWorkspace
    {
        public async Task<Result<Workspace>> CreateWorkspaceAsync(CreateWorkspaceDto workspaceDto, Guid userId)
        {
            var workspaceModel = WorkspaceMappers.ToWorkspaceModelFromCreateDto(workspaceDto, userId);
            await context.Workspaces.AddAsync(workspaceModel);

            // create workspaceMember instance and save
            var workspaceMemberModel = WorkspaceMemberMapper.ToWorkspaceMemberModelFromCreateDto(userId, workspaceModel.Id, WorkspaceRole.Admin);
            await context.WorkspaceMembers.AddAsync(workspaceMemberModel);
            await context.SaveChangesAsync();
            return Result<Workspace>.Success(workspaceModel);
        }

        public async Task<Result<bool>> DeleteWorkspaceAsync(Guid id, Guid userId)
        {
            var workspaceModel = await context.Workspaces.Include(w => w.Projects).FirstOrDefaultAsync(w => w.Id == id);
            if (workspaceModel is null) return Result<bool>.Failure("Workspace does not exist");

            // To confirm that the user has an admin role in the workspace they want to delete
            var isAdmin = await IsWorkspaceAdminMember(id, userId);
            if (!isAdmin) return Result<bool>.Failure("403: You have no permission for this request");

            if (workspaceModel.Projects.Count > 0) return Result<bool>.Failure("Delete workspace projects first!");

            // Confirms that the user isn't the owner of the project (has transferred ownership)
            if (workspaceModel.OwnerId != userId)
            {
                context.Workspaces.Remove(workspaceModel);
                await context.SaveChangesAsync();
                return Result<bool>.Success(true);
            }
            return Result<bool>.Failure("You must transfer ownership to another admin member first");
        }

        public async Task<Result<List<Workspace>>> GetAllWorkspacesAsync(WorkspaceQueryObject query, Guid userId)
        {
            var workspaces = context.Workspaces
                .Where(w => w.WorkspaceMembers.Any(m => m.AppUserId == userId))
                .Include(w => w.Projects)
                .Include(w => w.Labels)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(query.Name))
            {
                workspaces = workspaces.Where(w => w.Name == query.Name);
            }
            // i want to get all the workspaces created on or before the given date
            if (query.BeforeCreatedAt.HasValue)
            {
                workspaces = workspaces.Where(w => w.CreatedAt <= query.BeforeCreatedAt);
            }
            if (query.AfterCreatedAt.HasValue)
            {
                workspaces = workspaces.Where(w => w.CreatedAt >= query.AfterCreatedAt);
            }
            if (query.BeforeUpdatedAt.HasValue)
            {
                workspaces = workspaces.Where(w => w.UpdatedAt <= query.BeforeUpdatedAt);
            }
            if (query.AfterUpdatedAt.HasValue)
            {
                workspaces = workspaces.Where(w => w.UpdatedAt >= query.AfterUpdatedAt);
            }
            if (query.ExactDate.HasValue)
            {
                workspaces = workspaces.Where(w => w.UpdatedAt == query.ExactDate);
            }

            //  sorting
            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                if (query.SortBy.Equals("Name"))
                    workspaces = query.IsDescending ? workspaces.OrderByDescending(x => x.Name) : workspaces.OrderBy(x => x.Name);
                if (query.SortBy.Equals("UpdatedAt"))
                    workspaces = query.IsDescending ? workspaces.OrderByDescending(x => x.UpdatedAt) : workspaces.OrderBy(x => x.UpdatedAt);
                if (query.SortBy.Equals("CreatedAt"))
                    workspaces = query.IsDescending ? workspaces.OrderByDescending(x => x.CreatedAt) : workspaces.OrderBy(x => x.CreatedAt);
            }

            var skipNumber = (query.PageNumber - 1) * query.PageSize;
            return Result<List<Workspace>>.Success(await workspaces.Skip(skipNumber).Take(query.PageSize).ToListAsync());
        }

        public async Task<Result<Workspace?>> GetWorkspaceByIdAsync(Guid workspaceId, Guid userId)
        {
            var workspaceModel = await context.Workspaces.Include(w => w.WorkspaceMembers)
                                .Include(w => w.Projects)
                                .Include(w => w.Labels)
                                .FirstOrDefaultAsync(
                                    w => w.Id == workspaceId &&
                                    w.WorkspaceMembers.Any(m => m.AppUserId == userId));
            if (workspaceModel is null) return Result<Workspace?>.Failure("Workspace does not exist");
            return Result<Workspace?>.Success(workspaceModel);
        }

        public async Task<Result<Workspace?>> UpdateWorkspaceAsync(Guid id, UpdateWorkspaceDto updateDto, Guid userId)
        {
            var existingWorkspaceModel = await GetTrackedWorkspaceAsync(id);
            if (existingWorkspaceModel is null) return Result<Workspace?>.Failure("Workspace does not exist");

            // To confirm first that the user is the owner or an admin member of the workspace
            var isAdmin = await IsWorkspaceAdminMember(id, userId);
            if (!isAdmin) return Result<Workspace?>.Failure("403: You are not permitted to make this request");

            if (!string.IsNullOrWhiteSpace(updateDto.Name)) existingWorkspaceModel.Name = updateDto.Name;
            if (updateDto.Description is not null) existingWorkspaceModel.Description = updateDto.Description;
            existingWorkspaceModel.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return Result<Workspace?>.Success(existingWorkspaceModel);
        }

        public async Task<Workspace?> GetTrackedWorkspaceAsync(Guid workspaceId)
        {
            var workspaceModel = await context.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId);
            if (workspaceModel is null) return null;
            return workspaceModel;
        }

        public async Task<bool> IsWorkspaceAdminMember(Guid workspaceId, Guid userId)
        {
            var isAdminMember = await context.WorkspaceMembers.AnyAsync(m =>
                m.WorkspaceId == workspaceId && m.AppUserId == userId && m.Role == WorkspaceRole.Admin);
            if (!isAdminMember) return false;
            return true;
        }


        // WorkspaceMember CRUD logic
        public async Task<Result<WorkspaceMember>> AddWorkspaceMemberAsync(
            Guid workspaceId, Guid requestingUserId, CreateWorkspaceMemberDto dto)
        {
            // confirm if the requesting user is an admin in that workspace
            var requesterIsAdmin = await IsWorkspaceAdminMember(workspaceId, requestingUserId);
            if (!requesterIsAdmin) return Result<WorkspaceMember>.Failure("Only workspace admins can add members");

            // confirm if the target user exists
            var targetUserExists = await context.Users.AnyAsync(u => u.Id == dto.AppUserId);
            if (!targetUserExists) return Result<WorkspaceMember>.Failure("User not found");

            // check if the target user is already a member of the workspace
            var alreadyMember = await context.WorkspaceMembers.AnyAsync(m =>
                m.AppUserId == dto.AppUserId && m.WorkspaceId == workspaceId);
            if (alreadyMember) return Result<WorkspaceMember>.Failure("User is already a member of this workspace");

            // add the target user to the workspace 
            var newWorkspaceMemberModel = WorkspaceMemberMapper.ToWorkspaceMemberModelFromCreateDto(dto.AppUserId, workspaceId, dto.Role);
            await context.WorkspaceMembers.AddAsync(newWorkspaceMemberModel);
            await context.SaveChangesAsync();
            return Result<WorkspaceMember>.Success(newWorkspaceMemberModel);
        }

        public async Task<Result<WorkspaceMember?>> UpdateWorkspaceMemberRoleAsync(
                Guid workspaceId,
                Guid requestingUserId,
                Guid targetUserId,
                WorkspaceRole newRole)
        {
            // confirm if the requesting user is an admin in that workspace
            var requesterIsAdmin = await IsWorkspaceAdminMember(workspaceId, requestingUserId);
            if (!requesterIsAdmin) return Result<WorkspaceMember?>.Failure("Only workspace admins can remove members");

            // check if the target user is already a member of the workspace
            var workspaceMember = await GetWorkspaceMember(targetUserId, workspaceId);
            if (workspaceMember is null) return Result<WorkspaceMember?>.Failure("Target user is not a member of the workspace");

            workspaceMember.Role = newRole;
            await context.SaveChangesAsync();
            return Result<WorkspaceMember?>.Success(workspaceMember);
        }

        public async Task<Result<bool>> RemoveWorkspaceMemberAsync(Guid workspaceId, Guid requestingUserId, Guid targetUserId)
        {
            // confirm if the requesting user is an admin in that workspace
            var requesterIsAdmin = await IsWorkspaceAdminMember(workspaceId, requestingUserId);
            if (!requesterIsAdmin) return Result<bool>.Failure("Only workspace admins can remove members");

            // check if the target user is already a member of the workspace
            var workspaceMember = await GetWorkspaceMember(targetUserId, workspaceId);
            if (workspaceMember is null) return Result<bool>.Failure("Target user is not a member of the workspace");

            //  suggestion: confirm that number of admins left is at least 1, although you need to be an admin to even remove another admin

            context.WorkspaceMembers.Remove(workspaceMember); // this is incorrect, needs fixing
            await context.SaveChangesAsync();
            return Result<bool>.Success(true); // user successfully removed from the workspace
        }

        public async Task<WorkspaceMember?> GetWorkspaceMember(Guid targetUserId, Guid workspaceId)
        {
            var workspaceMember = await context.WorkspaceMembers.FirstOrDefaultAsync(m =>
                m.AppUserId == targetUserId && m.WorkspaceId == workspaceId);
            if (workspaceMember is null) return null;

            return workspaceMember;
        }
    }
}