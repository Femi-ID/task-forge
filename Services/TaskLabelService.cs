using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos;
using api.Dtos.Labels;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class TaskLabelService(AppDbContext context, IWorkspaceAuthorization workspaceAuth, IActivityLogService activityLog) : ITaskLabelService
    {
        public async Task<Result<LabelResponseDto>> AttachLabelAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid labelId, Guid userId)
        {
            var isMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, userId);
            if (!isMember) return Result<LabelResponseDto>.Failure("Only workspace members can attach labels.", ResultError.Forbidden);

            var task = await context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId && t.Project.WorkspaceId == workspaceId);
            if (task is null) return Result<LabelResponseDto>.Failure("Task not found", ResultError.NotFound);

            var label = await context.Labels.FirstOrDefaultAsync(l => l.Id == labelId && l.WorkspaceId == workspaceId);
            if (label is null) return Result<LabelResponseDto>.Failure("Label not found in this workspace", ResultError.NotFound);

            var alreadyAttached = await context.TaskLabels.AnyAsync(tl => tl.TaskItemId == taskId && tl.LabelId == labelId);
            if (alreadyAttached) return Result<LabelResponseDto>.Failure("This label is already attached to the task.", ResultError.Conflict);

            await context.TaskLabels.AddAsync(new TaskLabel { TaskItemId = taskId, LabelId = labelId });
            await activityLog.LogAsync(workspaceId, userId, "task.label_attached", $"attached label '{label.Name}' to task '{task.Title}'", taskId: taskId, projectId: projectId);
            await context.SaveChangesAsync(); // task-label row + log entry, one round trip, one transaction

            return Result<LabelResponseDto>.Success(LabelMapper.ToDto(label));
        }

        public async Task<Result<bool>> DetachLabelAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid labelId, Guid userId)
        {
            var isMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, userId);
            if (!isMember) return Result<bool>.Failure("Only workspace members can detach labels.", ResultError.Forbidden);

            var taskLabel = await context.TaskLabels.Include(tl => tl.Label).Include(tl => tl.TaskItem)
                .FirstOrDefaultAsync(tl => tl.TaskItemId == taskId && tl.LabelId == labelId &&
                                           tl.TaskItem.ProjectId == projectId && tl.TaskItem.Project.WorkspaceId == workspaceId);
            if (taskLabel is null) return Result<bool>.Failure("This label isn't attached to the task.", ResultError.NotFound);

            context.TaskLabels.Remove(taskLabel);
            await activityLog.LogAsync(workspaceId, userId, "task.label_detached",
            $"removed label '{taskLabel.Label.Name}' from task '{taskLabel.TaskItem.Title}'", taskId: taskId, projectId: projectId);
            await context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }

        public async Task<Result<List<LabelResponseDto>>> GetTaskLabelsAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId)
        {
            var isMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, userId);
            if (!isMember) return Result<List<LabelResponseDto>>.Failure("Only workspace members can view this.", ResultError.Forbidden);

            // i am verifying with project cause some tasks may have the same name but belong to different projects and workspaces
            var labels = await context.TaskLabels.AsNoTracking()
                .Where(tl => tl.TaskItemId == taskId && tl.TaskItem.ProjectId == projectId && tl.TaskItem.Project.WorkspaceId == workspaceId)
                .Select(tl => new LabelResponseDto { Id = tl.Label.Id, Name = tl.Label.Name, Colour = tl.Label.Colour, WorkspaceId = tl.Label.WorkspaceId })
                .ToListAsync();
            
            return Result<List<LabelResponseDto>>.Success(labels);
        }
    }
}