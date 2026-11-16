using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Comments;
using api.Helpers;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/v1/workspaces/{workspaceId:Guid}/projects/{projectId:Guid}/tasks/{taskId:Guid}/comments")]
    [ApiController, Authorize]
    public class CommentController(ICommentService commentService): ApiControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateComment([FromRoute] Guid workspaceId, [FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromBody] CreateCommentDto dto)
        {
            var result = await commentService.CreateCommentAsync(dto, workspaceId, projectId, taskId, CurrentUserId);
            if (!result.Succeeded) return HandleResult(result);
            return CreatedAtAction(nameof(GetCommentById), new { workspaceId, projectId, taskId, commentId = result.Data!.Id}, result.Data);
        }

         [HttpGet]
        public async Task<IActionResult> GetAllComments([FromRoute] Guid workspaceId, [FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromQuery] CommentQueryObject query)
        {
            var result = await commentService.GetAllCommentsAsync(workspaceId, projectId, taskId, CurrentUserId, query);
            return HandleResult(result);
        }

        [HttpGet("{commentId:Guid}")]
        public async Task<IActionResult> GetCommentById([FromRoute] Guid workspaceId, [FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromRoute] Guid commentId)
        {
            var result = await commentService.GetCommentByIdAsync(workspaceId, projectId, taskId, commentId, CurrentUserId);
            return HandleResult(result);
        }

        [HttpPatch("{commentId:Guid}")]
        public async Task<IActionResult> UpdateComment([FromRoute] Guid workspaceId, [FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromRoute] Guid commentId, [FromBody] UpdateCommentDto dto)
        {
            var result = await commentService.UpdateCommentAsync(workspaceId, projectId, taskId, commentId, CurrentUserId, dto);
            return HandleResult(result);
        }

        [HttpDelete("{commentId:Guid}")]
        public async Task<IActionResult> DeleteComment([FromRoute] Guid workspaceId, [FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromRoute] Guid commentId)
        {
            var result = await commentService.DeleteCommentAsync(workspaceId, projectId, taskId, commentId, CurrentUserId);
            return result.Succeeded ? NoContent() : HandleResult(result);
        }

    }
}