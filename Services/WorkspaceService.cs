using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos;
using api.Dtos.Workspace;
using api.Enums;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class WorkspaceService(AppDbContext context) : IWorkspace, IWorkspaceAuthorization
    {
        public async Task<Result<Workspace>> CreateWorkspaceAsync(CreateWorkspaceDto workspaceDto, Guid userId)
        {
            // confirm if this workspace name doesn't exist for the user
            var nameTaken = await context.Workspaces.AnyAsync(w => w.Name == workspaceDto.Name && w.OwnerId == userId);
            if (nameTaken) return Result<Workspace>.Failure("You already have a workspace with this name", ResultError.Conflict);

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
            if (workspaceModel is null) return Result<bool>.Failure("Workspace does not exist", ResultError.NotFound);

            // To confirm that the user has an admin role in the workspace they want to delete
            var isAdmin = await IsWorkspaceAdminMemberAsync(id, userId);
            if (!isAdmin) return Result<bool>.Failure("403: You have no permission for this request", ResultError.Forbidden);

            if (workspaceModel.Projects.Count > 0) return Result<bool>.Failure("Delete workspace projects first!", ResultError.Validation);

            // Confirms that the user isn't the owner of the project (has transferred ownership)
            if (workspaceModel.OwnerId == userId)
            {
                context.Workspaces.Remove(workspaceModel);
                await context.SaveChangesAsync();
                return Result<bool>.Success(true);
            }
            return Result<bool>.Failure("You must transfer ownership to another admin member first", ResultError.Validation);
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
            if (workspaceModel is null) return Result<Workspace?>.Failure("Workspace does not exist", ResultError.NotFound);
            return Result<Workspace?>.Success(workspaceModel);
        }

        public async Task<Result<Workspace?>> UpdateWorkspaceAsync(Guid id, UpdateWorkspaceDto updateDto, Guid userId)
        {
            var existingWorkspaceModel = await GetTrackedWorkspaceAsync(id);
            if (existingWorkspaceModel is null) return Result<Workspace?>.Failure("Workspace does not exist", ResultError.NotFound);

            // To confirm first that the user is the owner or an admin member of the workspace
            var isAdmin = await IsWorkspaceAdminMemberAsync(id, userId);
            if (!isAdmin) return Result<Workspace?>.Failure("403: You are not permitted to make this request", ResultError.Forbidden);

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

        public async Task<bool> IsWorkspaceAdminMemberAsync(Guid workspaceId, Guid userId)
        {
            var isAdminMember = await context.WorkspaceMembers.AnyAsync(m =>
                m.WorkspaceId == workspaceId && m.AppUserId == userId && m.Role == WorkspaceRole.Admin);
            if (!isAdminMember) return false;
            return true;
        }

        public async Task<Result<bool>> TransferWorkspaceOwnershipAsync(Guid workspaceId, Guid currentOwnerId, Guid newOwnerId)
        {
            //  get workspace
            var workspace = await GetTrackedWorkspaceAsync(workspaceId);
            if (workspace is null) return Result<bool>.Failure("Workspace does not exist", ResultError.NotFound);
      
            // confirm the requesting user is the workspace owner
            if (workspace.OwnerId != currentOwnerId)
                return Result<bool>.Failure("Only the current owner can transfer ownership", ResultError.Forbidden);

            // confirm new owner is already a member
            var newOwnerMember = await context.WorkspaceMembers.FirstOrDefaultAsync(m =>
                m.WorkspaceId == workspaceId && m.AppUserId == newOwnerId);
            if (newOwnerMember is null)
                return Result<bool>.Failure("The new owner must already be a member of this workspace", ResultError.Validation);

            newOwnerMember.Role = WorkspaceRole.Admin;
            workspace.OwnerId = newOwnerId;
            await context.SaveChangesAsync();
            return Result<bool>.Success(true);
        }
    }
}