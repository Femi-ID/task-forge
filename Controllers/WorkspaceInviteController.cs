using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos;
using api.Helpers;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace api.Controllers
{
    [Route("api/v1/workspaces/{workspaceId:Guid}/invites")]
    [ApiController, Authorize]
    public class WorkspaceInviteController(IWorkspaceInvite workspaceInviteService) : ApiControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> GenerateInviteAsync(Guid workspaceId, [FromBody] GenerateWorkspaceInviteDto dto)
        {
            if (dto.WorkspaceId != workspaceId) return BadRequest("Workspace mismatch");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var workspaceInvite = await workspaceInviteService.GenerateWorkspaceInviteAsync(dto, CurrentUserId);
            return HandleResult(workspaceInvite);
            // var inviteLink = $"https://taskforge.app/invites/accept?token={RawToken}";
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSentWorkspaceInvitesAsync(Guid workspaceId)
        {
            return HandleResult(await workspaceInviteService.GetAllSentWorkspaceInvitesAsync(workspaceId, CurrentUserId));
        }

        [HttpDelete("{inviteId:Guid}")]
        public async Task<IActionResult> RevokeInviteAsync(Guid inviteId)
        {
            return HandleResult(await workspaceInviteService.RevokeWorkspaceInviteAsync(inviteId, CurrentUserId));
        }

        [HttpGet("{inviteId:Guid}")]
        public async Task<IActionResult> GetWorkspaceInviteByIdAsync([FromRoute] Guid workspaceId, Guid inviteId)
        {
            var response = await workspaceInviteService.GetWorkspaceInviteByIdAsync(workspaceId, inviteId, CurrentUserId);
            return HandleResult(response);
        }
    }


    // my invites
    [Route("api/v1/invites")]
    [ApiController, Authorize]
    public class MyInvitesController(IWorkspaceInvite inviteService, IWorkspaceMemberService memberService) : ApiControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetMyInvitesAsync()
        {
            return HandleResult(await inviteService.GetMyInvitesAsync(CurrentUserId));
        }

        public record AcceptInviteDto(string Token);

        [HttpPost("token/accept")]
        public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteDto dto) =>
        HandleResult(await memberService.AcceptWorkspaceInviteByTokenAsync(dto.Token, CurrentUserId));

        [HttpPost("{inviteId:Guid}/accept")]
        public async Task<IActionResult> AcceptInviteWorkspaceById([FromRoute] Guid inviteId)
        {
            var response = await memberService.AcceptWorkspaceInviteByIdAsync(inviteId, CurrentUserId);
            return HandleResult(response);
        }
    }
}