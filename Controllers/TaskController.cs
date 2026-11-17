using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Tasks;
using api.Helpers;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/v1/workspaces/{workspaceId:Guid}/projects/{projectId:Guid}/tasks")]
    [ApiController, Authorize]

    public class TaskController(ITaskService taskService) : ApiControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromRoute] Guid workspaceId, [FromRoute] Guid projectId, [FromBody] CreateTaskDto dto)
        {
            var result = await taskService.CreateTaskAsync(dto, workspaceId, projectId, CurrentUserId);
            if (!result.Succeeded) return HandleResult(result);
            return CreatedAtAction(nameof(GetTaskById), new { workspaceId, projectId, taskId = result.Data!.Id }, result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTasks([FromRoute] Guid workspaceId, [FromRoute] Guid projectId, [FromQuery] TaskQueryObject query)
        {
            return HandleResult(await taskService.GetAllTasksAsync(workspaceId, projectId, CurrentUserId, query));
        }

        [HttpGet("{taskId:Guid}")]
        public async Task<IActionResult> GetTaskById([FromRoute] Guid workspaceId, [FromRoute] Guid projectId, [FromRoute] Guid taskId)
        {
            return HandleResult(await taskService.GetTaskByIdAsync(workspaceId, projectId, taskId, CurrentUserId));
        }

        [HttpPatch("{taskId:Guid}")]
        public async Task<IActionResult> UpdateTask([FromRoute] Guid workspaceId, [FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromBody] UpdateTaskDto dto)
        {
            return HandleResult(await taskService.UpdateTaskAsync(workspaceId, projectId, taskId, CurrentUserId, dto));
        }

        [HttpPatch("{taskId:Guid}/status")]
        public async Task<IActionResult> UpdateTaskStatus([FromRoute] Guid workspaceId, [FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromBody] UpdateTaskStatusDto dto)
        {
            return HandleResult(await taskService.UpdateTaskStatusAsync(workspaceId, projectId, taskId, CurrentUserId, dto.Status));
        }

        [HttpDelete("{taskId:Guid}")]
        public async Task<IActionResult> DeleteTask([FromRoute] Guid workspaceId, [FromRoute] Guid projectId, [FromRoute] Guid taskId)
        {
            var result = await taskService.DeleteTaskAsync(workspaceId, projectId, taskId, CurrentUserId);
            return result.Succeeded ? NoContent() : HandleResult(result);
        }
    }
}