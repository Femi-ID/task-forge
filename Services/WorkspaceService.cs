using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.Workspace;
using api.Dtos.WorkspaceMember;
using api.Enums;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class WorkspaceService(AppDbContext context) : IWorkspace
    {
        public async Task<Workspace> CreateWorkspaceAsync(CreateWorkspaceDto workspaceDto, Guid userId)
        {
            var workspaceModel = WorkspaceMappers.ToWorkspaceModelFromCreateDto(workspaceDto, userId);
            await context.Workspaces.AddAsync(workspaceModel);
            await context.SaveChangesAsync();

            // create workspaceMember instance and save
            // var newWorkspaceMemberDto = new CreateWorkspaceMemberDto(userId, workspaceModel.Id);
            var workspaceMemberModel = WorkspaceMemberMapper.ToWorkspaceMemberModelFromCreateDto(userId, workspaceModel.Id, WorkspaceRole.Admin);
            await context.WorkspaceMembers.AddAsync(workspaceMemberModel);
            await context.SaveChangesAsync();
            return workspaceModel;
        }

        public async Task<bool> DeleteWorkspaceAsync(Guid Id, Guid OwnerId)
        {
            var workspaceModel = await WorkspaceExists(Id);
            if (workspaceModel is null) return false;

            // I need to confirm that the user has an admin role in the workspace they want to delete, 
            // can't i determine via the workspace model and not the workspaceMember model
            // var workspaceMemberModel = await context.WorkspaceMembers.FirstOrDefaultAsync(w => w.WorkspaceId==workspaceModel.Id);
            if (workspaceModel.Projects is null && workspaceModel.OwnerId != OwnerId)
            {
                context.Workspaces.Remove(workspaceModel);
                await context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<Workspace>> GetAllWorkspacesAsync(WorkspaceQueryObject query)
        {
            var workspaces = context.Workspaces.Include(w => w.Projects).Include(w => w.Labels).AsQueryable();
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
                workspaces = workspaces.Where(w => w.UpdatedAt >= query.BeforeUpdatedAt);
            }
            if (query.AfterUpdatedAt.HasValue)
            {
                workspaces = workspaces.Where(w => w.UpdatedAt >= query.AfterUpdatedAt);
            }
            if (query.ExactDate.HasValue)
            {
                workspaces = workspaces.Where(w => w.UpdatedAt >= query.ExactDate);
            }

            //  sorting
            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                if (query.SortBy.Equals("Name"))
                {
                    workspaces = query.IsDescending ? workspaces.OrderByDescending(x => x.Name) : workspaces.OrderBy(x => x.Name);
                }
                if (query.SortBy.Equals("UpdatedAt"))
                {
                    workspaces = query.IsDescending ? workspaces.OrderByDescending(x => x.UpdatedAt) : workspaces.OrderBy(x => x.UpdatedAt);
                }
                if (query.SortBy.Equals("CreatedAt"))
                {
                    workspaces = query.IsDescending ? workspaces.OrderByDescending(x => x.CreatedAt) : workspaces.OrderBy(x => x.CreatedAt);
                }
            }

            var skipNumber = (query.PageNumber - 1) * query.PageSize;
            return await workspaces.Skip(skipNumber).Take(query.PageSize).ToListAsync();
        }

        public async Task<Workspace?> GetWorkspaceByIdAsync(Guid workspaceId)
        {
            var workspaceModel = await context.Workspaces.Include(w => w.WorkspaceMembers)
                                .Include(w => w.Projects)
                                .Include(w => w.Labels)
                                .FirstOrDefaultAsync(w => w.Id == workspaceId);
            if (workspaceModel is null) return null;
            return workspaceModel;
        }

        public async Task<Workspace?> UpdateWorkspaceAsync(Guid Id, UpdateWorkspaceDto updateDto)
        {
            var existingWorkspaceModel = await WorkspaceExists(Id);
            if (existingWorkspaceModel is null) return null;
            // need to confirm first that the user is the owner or an admin member of the workspace

            var updateWorkspace = WorkspaceMappers.ToWorkspaceModelFromUpdateDto(updateDto);
            if (updateWorkspace is null) return null;

            existingWorkspaceModel.Name = updateWorkspace.Name;
            existingWorkspaceModel.Description = updateWorkspace.Description;

            await context.SaveChangesAsync();
            return existingWorkspaceModel;
        }

        public async Task<Workspace?> WorkspaceExists(Guid workspaceId)
        {
            var workspaceModel = await context.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId);
            if (workspaceModel is null) return null;
            return workspaceModel;
        }
    }
}