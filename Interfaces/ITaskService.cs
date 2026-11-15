using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos;
using api.Dtos.Tasks;
using api.Enums;
using api.Helpers;

namespace api.Interfaces
{
    public interface ITaskService
    {
        Task<Result<TaskResponseDto>> CreateTaskAsync(CreateTaskDto dto, Guid workspaceId, Guid projectId, Guid userId);
        Task<Result<PagedResult<TaskResponseDtoList>>> GetAllTasksAsync(Guid workspaceId, Guid projectId, Guid userId, TaskQueryObject query);
        Task<Result<TaskResponseDto>> GetTaskByIdAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId);
        Task<Result<TaskResponseDto>> UpdateTaskAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId, UpdateTaskDto dto);
        Task<Result<TaskStatusResponseDto>> UpdateTaskStatusAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId, TaskItemStatus newStatus);
        Task<Result<bool>> DeleteTaskAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId);
    }
}