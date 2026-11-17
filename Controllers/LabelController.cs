using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Labels;
using api.Helpers;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/v1/workspaces/{workspaceId:Guid}/labels")]
    [ApiController, Authorize]
    public class LabelController(ILabelService labelService): ApiControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateLabel([FromRoute] Guid workspaceId, [FromBody] CreateLabelDto dto)
        {
            var result = await labelService.CreateLabelAsync(dto, workspaceId, CurrentUserId);
            if (!result.Succeeded) return HandleResult(result);
            return CreatedAtAction(nameof(TaskController.GetTaskById), new { workspaceId, taskId = result.Data!.Id }, result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLabels([FromRoute] Guid workspaceId)
        {
            return HandleResult(await labelService.GetAllLabelsAsync(workspaceId, CurrentUserId));
        }

        [HttpPatch("{labelId:Guid}")]
        public async Task<IActionResult> UpdateLabel([FromRoute] Guid workspaceId, [FromRoute] Guid labelId, [FromBody] UpdateLabelDto dto)
        {
            return HandleResult(await labelService.UpdateLabelAsync(dto, workspaceId, labelId, CurrentUserId));
        }

        [HttpDelete("{labelId:Guid}")]
        public async Task<IActionResult> DeleteLabel([FromRoute] Guid workspaceId, [FromRoute] Guid labelId)
        {
            var result = await labelService.DeleteLabelAsync(workspaceId, labelId, CurrentUserId);
            return result.Succeeded ? NoContent() : HandleResult(result);
        }
    }
}