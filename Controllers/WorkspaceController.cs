using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Workspace;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/v1/workspaces")]
    [ApiController, Authorize]
    public class WorkspaceController(IWorkspace workspaceService) : ApiControllerBase
    {
        [HttpPost("create")]
        public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceDto workspaceDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await workspaceService.CreateWorkspaceAsync(workspaceDto, CurrentUserId);
            if (!response.Succeeded) return HandleResult(response);
            return CreatedAtAction(nameof(GetWorkspaceById), new { workspaceId = response.Data!.Id }, response.Data!);
        }

        [HttpDelete("{workspaceId:Guid}")]
        public async Task<IActionResult> DeleteWorkspace([FromRoute] Guid workspaceId)
        {
            var response = await workspaceService.DeleteWorkspaceAsync(workspaceId, CurrentUserId);
            return response.Succeeded ? Ok(response.Data) : HandleResult(response);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllWorkspace([FromQuery] WorkspaceQueryObject queryObject)
        {
            var response = await workspaceService.GetAllWorkspacesAsync(queryObject, CurrentUserId);
            return response.Succeeded ? Ok(response.Data) : HandleResult(response);
        }

        [HttpGet("{workspaceId:Guid}")]
        public async Task<IActionResult> GetWorkspaceById([FromRoute] Guid workspaceId)
        {
            var response = await workspaceService.GetWorkspaceByIdAsync(workspaceId, CurrentUserId);
            return response.Succeeded ? Ok(response.Data) : HandleResult(response);
        }

        [HttpPut("{workspaceId:Guid}")]
        public async Task<IActionResult> UpdateWorkspace([FromRoute] Guid workspaceId, [FromBody] UpdateWorkspaceDto workspaceDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await workspaceService.UpdateWorkspaceAsync(workspaceId, workspaceDto, CurrentUserId);
            return response.Succeeded ? Ok(response.Data) : HandleResult(response);
        }

        public record TransferOwnershipDto(Guid Id);

        [HttpPost("{workspaceId:Guid}/transfer-ownership")]
        public async Task<IActionResult> TransferWorkspaceOwnership([FromRoute] Guid workspaceId, [FromBody] TransferOwnershipDto NewOwner)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await workspaceService.TransferWorkspaceOwnershipAsync(workspaceId, CurrentUserId, NewOwner.Id);

            return response.Succeeded ? NoContent() : HandleResult(response);
        }

        // workspaceLogos
        [HttpGet("{workspaceId:Guid}/logo")]
        public async Task<IActionResult> GetWorkspaceLogo([FromRoute] Guid workspaceId, CancellationToken ct)
        {
            var result = await workspaceService.GetWorkspaceLogoUrlAsync(workspaceId, CurrentUserId, ct);
            if (!result.Succeeded) return HandleResult(result);
            return Redirect(result.Data!.ToString());
        }

        [HttpPost("{workspaceId:Guid}/logo")]
        public async Task<IActionResult> SetWorkspaceLogo([FromRoute] Guid workspaceId, IFormFile file, CancellationToken ct)
        {
            var result = await workspaceService.SetWorkspaceLogoAsync(workspaceId, CurrentUserId, file, ct);
            return HandleResult(result);
        }

        [HttpDelete("{workspaceId:Guid}/logo")]
        public async Task<IActionResult> DeleteWorkspaceLogo([FromRoute] Guid workspaceId, CancellationToken ct)
        {
            var result = await workspaceService.DeleteWorkspaceLogoAsync(workspaceId, CurrentUserId, ct);
            return result.Succeeded ? NoContent() : HandleResult(result);
        }
    }
}