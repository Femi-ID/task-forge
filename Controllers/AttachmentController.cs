using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Helpers;
using api.Interfaces;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController, Authorize]
    public class AttachmentController(IAttachmentService attachmentService) : ApiControllerBase
    {
        // IFormFile comes from [FromForm]: multipart/form-data, not JSON. Multipart is how browsers/clients actually send files.
        // these are the 3 options to UPLOAD a file (to project, task or comment)
        [HttpPost("api/v1/workspaces/{workspaceId:Guid}/projects/{projectId:Guid}/attachments")]
        public async Task<IActionResult> UploadToProject(
            [FromRoute] Guid workspaceId, [FromRoute] Guid projectId, IFormFile file, CancellationToken ct)
        {
            var result = await attachmentService.UploadAsync(workspaceId, AttachmentOwnerType.Project, projectId, CurrentUserId, file, ct);
            return HandleResult(result);
        }

        [HttpPost("api/v1/workspaces/{workspaceId:Guid}/projects/{projectId:Guid}/tasks/{taskId:Guid}/attachments")]
        public async Task<IActionResult> UploadToTask(
            [FromRoute] Guid workspaceId, [FromRoute] Guid taskId, IFormFile file, CancellationToken ct)
        {
            var result = await attachmentService.UploadAsync(workspaceId, AttachmentOwnerType.Task, taskId, CurrentUserId, file, ct);
            return HandleResult(result);
        }

        [HttpPost("api/v1/workspaces/{workspaceId:Guid}/projects/{projectId:Guid}/tasks/{taskId:Guid}/comments/{commentId:Guid}/attachments")]
        public async Task<IActionResult> UploadToComment(
            [FromRoute] Guid workspaceId, [FromRoute] Guid commentId, IFormFile file, CancellationToken ct)
        {
            var result = await attachmentService.UploadAsync(workspaceId, AttachmentOwnerType.Comment, commentId, CurrentUserId, file, ct);
            return HandleResult(result);
        }


        // to LIST endpoints — same path structure as upload, just GET
        [HttpGet("api/v1/workspaces/{workspaceId:Guid}/projects/{projectId:Guid}/attachments")]
        public async Task<IActionResult> ListProjectAttachments([FromRoute] Guid workspaceId, [FromRoute] Guid projectId, CancellationToken ct)
        {
            return HandleResult(await attachmentService.ListAsync(workspaceId, AttachmentOwnerType.Project, projectId, CurrentUserId, ct));
        }

        [HttpGet("api/v1/workspaces/{workspaceId:Guid}/projects/{projectId:Guid}/tasks/{taskId:Guid}/attachments")]
        public async Task<IActionResult> ListTaskAttachments([FromRoute] Guid workspaceId, [FromRoute] Guid taskId, CancellationToken ct)
        {
            return HandleResult(await attachmentService.ListAsync(workspaceId, AttachmentOwnerType.Task, taskId, CurrentUserId, ct));
        }

        [HttpGet("api/v1/workspaces/{workspaceId:Guid}/projects/{projectId:Guid}/tasks/{taskId:Guid}/comments/{commentId:Guid}/attachments")]
        public async Task<IActionResult> ListCommentAttachments([FromRoute] Guid workspaceId, [FromRoute] Guid commentId, CancellationToken ct)
        {
            return HandleResult(await attachmentService.ListAsync(workspaceId, AttachmentOwnerType.Comment, commentId, CurrentUserId, ct));
        }


        // Download endpoint: NOT scoped to parent, because an attachment's id is globally unique
        // and the (workspaceId, attachmentId) pair is enough to authorize.
        [HttpGet("api/v1/workspaces/{workspaceId:Guid}/attachments/{attachmentId:Guid}/download")]
        public async Task<IActionResult> Download([FromRoute] Guid workspaceId, [FromRoute] Guid attachmentId, CancellationToken ct)
        {
            var result = await attachmentService.GetDownloadUrlAsync(workspaceId, attachmentId, CurrentUserId, ct);
            if (!result.Succeeded) return HandleResult(result);
            // Return 302 Redirect, not the URL in a JSON body. The browser follows the redirect
            // and goes straight to S3 for the bytes. If we returned the URL in JSON, the frontend
            // would have to parse it and set window.location — this makes <a href="..."> work directly.
            return Redirect(result.Data!.ToString());
        }

        [HttpDelete("api/v1/workspaces/{workspaceId:Guid}/attachments/{attachmentId:Guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid workspaceId, [FromRoute] Guid attachmentId, CancellationToken ct)
        {
            var result = await attachmentService.DeleteAsync(workspaceId, attachmentId, CurrentUserId, ct);
            return result.Succeeded ? NoContent() : HandleResult(result);
        }
    }
}