using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos;
using api.Dtos.ActivityLogs;

namespace api.Interfaces
{
    public interface IActivityLogService
    {
        Task LogAsync(Guid workspaceId, Guid actorUserId, string action, string description, Guid? taskId = null, Guid? projectId = null);
        Task<Result<PagedResult<ActivityLogDto>>> GetWorkspaceActivityAsync(Guid workspaceId, Guid userId, ActivityLogQueryObject query);
    }
}