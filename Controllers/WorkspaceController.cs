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
    [Route("api/v1/workspace")]
    [ApiController]
    public class WorkspaceController(IWorkspace workspaceService) : ApiControllerBase
    {
        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceDto workspaceDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await workspaceService.CreateWorkspaceAsync(workspaceDto, CurrentUserId);
            if (!response.Succeeded) return HandleResult(response);
            return CreatedAtAction(nameof(GetWorkspaceById), new { workspaceId = response.Data!.Id}, WorkspaceMappers.ToWorkspaceDto(response.Data!, CurrentUserId));
        }

        [HttpDelete("{workspaceId:Guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteWorkspace([FromRoute] Guid workspaceId)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await workspaceService.DeleteWorkspaceAsync(workspaceId, CurrentUserId);
            // if (!response.Succeeded) return HandleResult(response);
            return response.Succeeded ? Ok(response.Data) : HandleResult(response);
        }

        [HttpGet("all")]
        [Authorize]
        public async Task<IActionResult> GetAllWorkspace([FromQuery] WorkspaceQueryObject queryObject)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await workspaceService.GetAllWorkspacesAsync(queryObject, CurrentUserId);
            if (!response.Succeeded) return HandleResult(response);
            var workspaceDto = WorkspaceMappers.ToWorkspaceDtoList(response.Data!);
            return Ok(workspaceDto);
        }

        [HttpGet("{workspaceId:Guid}")]
        [Authorize]
        public async Task<IActionResult> GetWorkspaceById([FromRoute] Guid workspaceId)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await workspaceService.GetWorkspaceByIdAsync(workspaceId, CurrentUserId);
            return response.Succeeded ? Ok(WorkspaceMappers.ToWorkspaceDto(response.Data!, CurrentUserId)) : HandleResult(response);
        }

        [HttpPut("{workspaceId:Guid}")]
        [Authorize]
        public async Task<IActionResult> UpdateWorkspace([FromRoute] Guid workspaceId, [FromBody] UpdateWorkspaceDto workspaceDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await workspaceService.UpdateWorkspaceAsync(workspaceId, workspaceDto, CurrentUserId);
            return response.Succeeded ? Ok(WorkspaceMappers.ToWorkspaceDto(response.Data!, CurrentUserId)): HandleResult(response); 
        }

        [HttpPost("{workspace:Guid}/transfer-ownership")]
        [Authorize]
        public async Task<IActionResult> TransferWorkspaceOwnership([FromRoute] Guid workspaceId, [FromBody] Guid newOwnerId)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await workspaceService.TransferWorkspaceOwnershipAsync(workspaceId, CurrentUserId, newOwnerId);

            if (!response.Succeeded) return HandleResult(response);
            return Ok(response.Data);
        }
    }
}