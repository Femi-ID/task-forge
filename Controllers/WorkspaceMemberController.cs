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
using api.Mappers;

namespace api.Controllers
{
    [Route("api/v1/workspace-member")]
    [ApiController]
    public class WorkspaceMemberController(IWorkspaceMemberService workspaceMemberService) : ApiControllerBase
    {
        [HttpPost("{workspaceId:Guid}")]
        [Authorize]
        public async Task<IActionResult> AddWorkspaceMember([FromRoute] Guid workspaceId, [FromBody] CreateWorkspaceMemberDto memberDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await workspaceMemberService.AddWorkspaceMemberAsync(workspaceId, CurrentUserId, memberDto);
            // return response.Succeeded
            //     ? StatusCode(StatusCodes.Status201Created, WorkspaceMemberMapper.ToWorkspaceMemberDto(response.Data!))
            //     : HandleResult(response);
            return CreatedAtAction(nameof(GetWorkspaceMemberById), 
                                    new { workspaceId = response.Data!.WorkspaceId, targetUserId = response.Data!.AppUserId }, 
                                    WorkspaceMemberMapper.ToWorkspaceMemberDto(response.Data));
        }

        [HttpGet("{workspaceId:Guid}/member/{targetUserId:Guid}")]
        [Authorize]
        public async Task<IActionResult> GetWorkspaceMemberById([FromRoute] Guid workspaceId, Guid targetUserId)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await workspaceMemberService.GetWorkspaceMember(targetUserId, workspaceId);
            return (response is null) ? BadRequest("No user found") : Ok(WorkspaceMemberMapper.ToWorkspaceMemberDto(response));
        }

        public record UpdateWorkspaceMemberRoleDto(Guid TargetUserId, WorkspaceRole NewRole);

        [HttpPut("{workspaceId:Guid}")]
        [Authorize]
        public async Task<IActionResult> UpdateWorkspaceMemberRole([FromRoute] Guid workspaceId, [FromBody] UpdateWorkspaceMemberRoleDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await workspaceMemberService.UpdateWorkspaceMemberRoleAsync(workspaceId, CurrentUserId, dto.TargetUserId, dto.NewRole);
            return response.Succeeded ? Ok(WorkspaceMemberMapper.ToWorkspaceMemberDto(response.Data!)) : HandleResult(response);
        }

        [HttpDelete("{workspaceId:Guid}/member/{targetUserId:Guid}")]
        [Authorize]
        public async Task<IActionResult> RemoveWorkspaceMember([FromRoute] Guid workspaceId, Guid targetUserId)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await workspaceMemberService.RemoveWorkspaceMemberAsync(workspaceId, CurrentUserId, targetUserId);
            return response.Succeeded ? Ok(response.Data) : HandleResult(response);
        }

        [HttpDelete("{workspaceId:Guid}")]
        [Authorize]
        public async Task<IActionResult> RemoveSelfMember([FromRoute] Guid workspaceId)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await workspaceMemberService.RemoveSelfMemberAsync(workspaceId, CurrentUserId);
            return response.Succeeded ? Ok(response.Data) : HandleResult(response);
        }
    }
}