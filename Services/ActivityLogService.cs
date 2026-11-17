using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos;
using api.Dtos.ActivityLogs;
using api.Helpers;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class ActivityLogService(AppDbContext context, IWorkspaceAuthorization workspaceAuth) : IActivityLogService
    {

        public async Task LogAsync(Guid workspaceId, Guid actorUserId, string action, string description, Guid? taskId = null, Guid? projectId = null)
        {
            var actorFullName = await context.Users.Where(u => u.Id == actorUserId).Select(u => u.FullName).FirstAsync();
            context.ActivityLogs.Add(new ActivityLog
            {
                WorkspaceId = workspaceId,
                ActorUserId = actorUserId,
                ActorFullName = actorFullName,
                Action = action,
                Description = description,
                TaskItemId = taskId,
                ProjectId = projectId
            });
            // deliberately no SaveChangesAsync, it will be used whena after it's called in other action's method (creating/updating task/taskStatus and other similar events) 
        }

        public async Task<Result<PagedResult<ActivityLogDto>>> GetWorkspaceActivityAsync(Guid workspaceId, Guid userId, ActivityLogQueryObject query)
        {
            var isMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, userId);
            if (!isMember) return Result<PagedResult<ActivityLogDto>>.Failure("Only workspace members can view activity.", ResultError.Forbidden);

            var logs = context.ActivityLogs.AsNoTracking().Where(a => a.WorkspaceId == workspaceId);
            if (query.TaskItemId.HasValue) logs = logs.Where(a => a.TaskItemId == query.TaskItemId); // to get logs for a specific task

            var totalCount = await logs.CountAsync();

            // Newest first, unlike comments (oldest first): a feed is scanned top-down for "what just happened";
            // a conversation is read oldest-first for context. Same PagedResult shape, opposite default order,
            // because the two are read differently.
            var (page, size) = PaginationHelper.Normalize(query.PageNumber, query.PageSize);
            var items = await logs.OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * size).Take(size)
                // Plain projection, no Expression needed: every field here is already a scalar column on
                // ActivityLog itself (that's the payoff of denormalizing) — there's no navigation to translate.
                .Select(a => new ActivityLogDto
                {
                    Id = a.Id,
                    ActorFullName = a.ActorFullName,
                    Action = a.Action,
                    Description = a.Description,
                    TaskItemId = a.TaskItemId,
                    ProjectId = a.ProjectId,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Result<PagedResult<ActivityLogDto>>.Success(new PagedResult<ActivityLogDto>
            { Items = items, TotalCount = totalCount, PageNumber = page, PageSize = size });
        }
    }
}