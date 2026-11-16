using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos;
using api.Dtos.Tasks;
using api.Enums;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class TaskService(
        AppDbContext context,
        IWorkspaceAuthorization workspaceAuth) : ITaskService
    {

        public async Task<Result<TaskResponseDto>> CreateTaskAsync(CreateTaskDto dto, Guid workspaceId, Guid projectId, Guid userId)
        {
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, userId);
            if (!requesterIsAdmin) return Result<TaskResponseDto>.Failure("Only workspace admins can create a task.", ResultError.Forbidden);

            var projectInWorkspace = await context.Projects.AnyAsync(p => p.Id == projectId && p.WorkspaceId == workspaceId);
            if (!projectInWorkspace) return Result<TaskResponseDto>.Failure("Project not found in this workspace", ResultError.NotFound);

            if (dto.AssigneeId.HasValue)
            {
                var assigneeIsMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, dto.AssigneeId.Value);
                if (!assigneeIsMember) return Result<TaskResponseDto>.Failure("Assignee must be a member of this workspace", ResultError.Validation);
            }

            var taskModel = TaskMappers.ToTaskModelFromCreateDto(dto, projectId);
            await context.Tasks.AddAsync(taskModel);
            await context.SaveChangesAsync();

            string? assigneeFullName = null;
            if (taskModel.AssigneeId.HasValue)
            {
                assigneeFullName = await context.Users
                    .Where(u => u.Id == taskModel.AssigneeId)
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync();
            }

            // Re-select through the Expression rather than mapping taskModel directly: AssigneeFullName
            // needs t.Assignee.FullName, and Assignee is never loaded on a freshly-added entity.
            // var taskDto = await context.Tasks.AsNoTracking() // isn't this another db query made to retrieve the entity when we've just created the entity
            //     .Where(t => t.Id == taskModel.Id)
            //     .Select(TaskMappers.ToDetailDto)
            //     .FirstAsync();

            var taskDto = new TaskResponseDto
            {
                Id = taskModel.Id,
                Title = taskModel.Title,
                Description = taskModel.Description,
                ProjectId = taskModel.ProjectId,
                Status = taskModel.Status,
                Priority = taskModel.Priority,
                DueDate = taskModel.DueDate,
                AssigneeId = taskModel.AssigneeId,
                AssigneeFullName = assigneeFullName,
                Labels = [],        // can't exist yet — no attach-label call happened in this method
                CommentCount = 0,   // can't exist yet — the task didn't exist for a comment to reference
                CreatedAt = taskModel.CreatedAt,
                UpdatedAt = taskModel.UpdatedAt
            };
            return Result<TaskResponseDto>.Success(taskDto);
        }

        public async Task<Result<bool>> DeleteTaskAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId)
        {
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, userId);
            if (!requesterIsAdmin) return Result<bool>.Failure("Only workspace admins can delete a task.", ResultError.Forbidden);

            // this is a hard delete
            var task = await context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId && t.Project.WorkspaceId == workspaceId);
            if (task is null) return Result<bool>.Failure("Task not found", ResultError.NotFound);

            context.Tasks.Remove(task);
            await context.SaveChangesAsync();
            return Result<bool>.Success(true);
        }

        public async Task<Result<PagedResult<TaskResponseDtoList>>> GetAllTasksAsync(Guid workspaceId, Guid projectId, Guid userId, TaskQueryObject query)
        {
            var isMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, userId);
            if (!isMember) return Result<PagedResult<TaskResponseDtoList>>.Failure("Only workspace members can fetch this result.", ResultError.Forbidden);

            var tasks = context.Tasks.Where(t => t.ProjectId == projectId && t.Project.WorkspaceId == workspaceId).AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Title))
            {
                // explain this foreach block 
                foreach (var term in query.Title.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var t = term.ToLower();
                    tasks = tasks.Where(x => x.Title.ToLower().Contains(t));
                }
            }
            if (query.Status.HasValue) tasks = tasks.Where(t => t.Status == query.Status.Value);
            if (query.Priority.HasValue) tasks = tasks.Where(t => t.Priority == query.Priority.Value);
            if (query.AssigneeId.HasValue) tasks = tasks.Where(t => t.AssigneeId == query.AssigneeId.Value);
            if (query.BeforeDueDate.HasValue) tasks = tasks.Where(t => t.DueDate <= query.BeforeDueDate);
            if (query.AfterDueDate.HasValue) tasks = tasks.Where(t => t.DueDate >= query.AfterDueDate);
            if (query.ExactDueDate.HasValue)
            {
                var day = query.ExactDueDate.Value.Date;
                tasks = tasks.Where(t => t.DueDate >= day && t.DueDate < day.AddDays(1));
            }

            var totalCount = await tasks.CountAsync();

            var orderedTasks = (query.SortBy ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "title" => query.IsDescending ? tasks.OrderByDescending(t => t.Title) : tasks.OrderBy(t => t.Title),
                "status" => query.IsDescending ? tasks.OrderByDescending(t => t.Status) : tasks.OrderBy(t => t.Status),
                "duedate" => query.IsDescending ? tasks.OrderByDescending(t => t.DueDate) : tasks.OrderBy(t => t.DueDate),
                "priority" => query.IsDescending ? tasks.OrderByDescending(t => t.Priority) : tasks.OrderBy(t => t.Priority),
                "createdat" => query.IsDescending ? tasks.OrderByDescending(t => t.CreatedAt) : tasks.OrderBy(t => t.CreatedAt),
                _ => tasks.OrderByDescending(t => t.Priority).ThenBy(t => t.DueDate) // so the default: most urgent, soonest due, first
            };

            orderedTasks = orderedTasks.ThenBy(t => t.Id); // tie-breaker so pages never overlap or skip rows

            var (page, size) = PaginationHelper.Normalize(query.PageNumber, query.PageSize);
            var TaskItems = await orderedTasks.Skip((page - 1) * size).Take(size)
                .Select(TaskMappers.ToListDto)
                .ToListAsync();

            return Result<PagedResult<TaskResponseDtoList>>.Success(new PagedResult<TaskResponseDtoList>
            {
                Items = TaskItems,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = size
            });
        }

        public async Task<Result<TaskResponseDto>> GetTaskByIdAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId)
        {
            var isMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, userId);
            if (!isMember) return Result<TaskResponseDto>.Failure("Only workspace members can fetch this result.", ResultError.Forbidden);

            var taskDto = await context.Tasks.AsNoTracking()
                .Where(t => t.Id == taskId && t.ProjectId == projectId && t.Project.WorkspaceId == workspaceId)
                .Select(TaskMappers.ToDetailDto)
                .FirstOrDefaultAsync();

            if (taskDto is null) return Result<TaskResponseDto>.Failure("Task not found", ResultError.NotFound);

            return Result<TaskResponseDto>.Success(taskDto);
        }

        public async Task<Result<TaskResponseDto>> UpdateTaskAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId, UpdateTaskDto dto)
        {
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, userId);
            if (!requesterIsAdmin) return Result<TaskResponseDto>.Failure("Only workspace admins can update a task.", ResultError.Forbidden);

            var task = await context.Tasks.FirstOrDefaultAsync(t =>
                t.Id == taskId && t.ProjectId == projectId && t.Project.WorkspaceId == workspaceId);
            if (task is null) return Result<TaskResponseDto>.Failure("Task not found", ResultError.NotFound);

            if (!string.IsNullOrWhiteSpace(dto.Title)) task.Title = dto.Title.Trim();
            if (dto.Description is not null) task.Description = dto.Description.Length == 0 ? null : dto.Description;
            if (dto.Priority.HasValue) task.Priority = dto.Priority.Value;
            if (dto.DueDate.HasValue) task.DueDate = dto.DueDate.Value;

            // Guid? can't distinguish "not sent" from "sent as null" any more cleanly than string? can.
            // Same convention as Description: null = don't touch AssigneeId at all; Guid.Empty = explicitly
            // unassign; any other Guid = reassign (after checking they're actually a workspace member).
            if (dto.AssigneeId.HasValue)
            {
                if (dto.AssigneeId.Value == Guid.Empty)
                {
                    task.AssigneeId = null;
                }
                else
                {
                    var assigneeIsMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, dto.AssigneeId.Value);
                    if (!assigneeIsMember) return Result<TaskResponseDto>.Failure("Assignee must be a member of this workspace", ResultError.Validation);
                    task.AssigneeId = dto.AssigneeId.Value;
                }
            }

            await context.SaveChangesAsync();
            // this is another db call to get the task, 
            // is this resourceful given that i've already done this earlier to get and update this entity
            var taskDto = await context.Tasks.AsNoTracking()
                .Where(t => t.Id == taskId)
                .Select(TaskMappers.ToDetailDto)
                .FirstAsync();
            return Result<TaskResponseDto>.Success(taskDto);
        }

        public async Task<Result<TaskStatusResponseDto>> UpdateTaskStatusAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId, TaskItemStatus newStatus)
        {
            var task = await context.Tasks.FirstOrDefaultAsync(t =>
                t.Id == taskId && t.ProjectId == projectId && t.Project.WorkspaceId == workspaceId);
            if (task is null) return Result<TaskStatusResponseDto>.Failure("Task not found", ResultError.NotFound);

            // Here the task's status is updated by either the current assignee, or any admin.
            var isAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, userId);
            var isAssignee = task.AssigneeId == userId;
            if (!isAdmin && !isAssignee)
                return Result<TaskStatusResponseDto>.Failure("Only the assignee or a workspace admin can change this task's status.", ResultError.Forbidden);

            task.Status = newStatus;
            await context.SaveChangesAsync();

            
            var taskDto = new TaskStatusResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Status = task.Status,
                DueDate = task.DueDate
            };
            // var taskDto = await context.Tasks.AsNoTracking()
            //     .Where(t => t.Id == taskId)
            //     .Select(TaskMappers.ToListDto)
            //     .FirstAsync();
            return Result<TaskStatusResponseDto>.Success(taskDto); // why is the return result type- TaskResponseDtoList ?
        }
    }
}