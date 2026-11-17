using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Helpers;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/v1/workspaces/{workspaceId:Guid}/projects/{projectId:Guid}/tasks/{taskId:Guid}/labels/{labelId:Guid}")]
    [ApiController, Authorize]
    public class TaskLabelController(ITaskLabelService taskLabelService) : ApiControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> AttachLabel([FromRoute] Guid workspaceId, [FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromRoute] Guid labelId)
        {
            var result = await taskLabelService.AttachLabelAsync(workspaceId, projectId, taskId, labelId, CurrentUserId);
            if (!result.Succeeded) return HandleResult(result);
            return CreatedAtAction(nameof(GetTaskLabels), new { workspaceId, projectId, taskId, CurrentUserId}, result.Data);
        }

        [HttpDelete]
        public async Task<IActionResult> DetachLabel([FromRoute] Guid workspaceId, [FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromRoute] Guid labelId)
        {
            var result = await taskLabelService.DetachLabelAsync(workspaceId, projectId, taskId, labelId, CurrentUserId);
            return result.Succeeded ? NoContent() : HandleResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetTaskLabels([FromRoute] Guid workspaceId, [FromRoute] Guid projectId, [FromRoute] Guid taskId)
        {
            var result = await taskLabelService.GetTaskLabelsAsync(workspaceId, projectId, taskId, CurrentUserId);
            return HandleResult(result);
        }
    }
}