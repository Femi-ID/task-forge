using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.WorkspaceMember;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using api.Enums;
using api.Helpers;

namespace api.Controllers
{
    [Route("api/v1/workspaces/{workspaceId:Guid}/members")]
    [ApiController, Authorize]
    public class WorkspaceMemberController(IWorkspaceMemberService workspaceMemberService) : ApiControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> AddWorkspaceMember([FromRoute] Guid workspaceId, [FromBody] CreateWorkspaceMemberDto memberDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await workspaceMemberService.AddWorkspaceMemberAsync(workspaceId, CurrentUserId, memberDto);
            if (!response.Succeeded) return HandleResult(response);
            return CreatedAtAction(nameof(GetWorkspaceMemberById),
                                    new { workspaceId = response.Data!.WorkspaceId, targetUserId = response.Data!.AppUserId }, 
                                    response.Data);
        }

        [HttpGet]
        public async Task<IActionResult> GetWorkspaceMembers([FromRoute] Guid workspaceId)
        {
            return HandleResult(await workspaceMemberService.GetWorkspaceMembersAsync(workspaceId, CurrentUserId));
        }

        [HttpGet("{targetUserId:Guid}")]
        public async Task<IActionResult> GetWorkspaceMemberById([FromRoute] Guid workspaceId, Guid targetUserId)
        {
            var response = await workspaceMemberService.GetWorkspaceMemberByIdAsync(workspaceId, CurrentUserId, targetUserId);
            return HandleResult(response);
        }

        public record UpdateWorkspaceMemberRoleDto(Guid TargetUserId, WorkspaceRole NewRole);

        [HttpPut]
        public async Task<IActionResult> UpdateWorkspaceMemberRole([FromRoute] Guid workspaceId, [FromBody] UpdateWorkspaceMemberRoleDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await workspaceMemberService.UpdateWorkspaceMemberRoleAsync(workspaceId, CurrentUserId, dto.TargetUserId, dto.NewRole);
            return response.Succeeded ? Ok(response.Data) : HandleResult(response);
        }

        [HttpDelete("{targetUserId:Guid}")]
        public async Task<IActionResult> RemoveWorkspaceMember([FromRoute] Guid workspaceId, Guid targetUserId)
        {
            var response = await workspaceMemberService.RemoveWorkspaceMemberAsync(workspaceId, CurrentUserId, targetUserId);
            return response.Succeeded ? Ok(response.Data) : HandleResult(response);
        }

        [HttpDelete]
        public async Task<IActionResult> RemoveSelfMember([FromRoute] Guid workspaceId)
        {
            var response = await workspaceMemberService.RemoveSelfMemberAsync(workspaceId, CurrentUserId);
            return response.Succeeded ? Ok(response.Data) : HandleResult(response);
        }
    }
}