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
        public async Task<IActionResult> GenerateInvite(Guid workspaceId, [FromBody] GenerateWorkspaceInviteDto dto)
        {
            if (dto.WorkspaceId != workspaceId) return BadRequest("Workspace mismatch");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var workspaceInvite = await workspaceInviteService.GenerateWorkspaceInviteAsync(dto, CurrentUserId);
            if (workspaceInvite.Data!.InvitedEmail is null) return HandleResult(workspaceInvite);

            return HandleResult(workspaceInvite);
            // var inviteLink = $"taskforge.com/workspace-member/add/{response.Data.WorkspaceId}/{response.Data.}";
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSentWorkspaceInvites(Guid workspaceId)
        {
            return HandleResult(await workspaceInviteService.GetAllSentWorkspaceInvitesAsync(workspaceId, CurrentUserId));
        }

        [HttpDelete("{inviteId:Guid}")]
        public async Task<IActionResult> RevokeInvite(Guid inviteId)
        {
            return HandleResult(await workspaceInviteService.RevokeWorkspaceInviteAsync(inviteId, CurrentUserId));
        }
    }


    // my invites
    [Route("api/v1/invites")]
    [ApiController, Authorize]
    public class MyInvitesController(IWorkspaceInvite inviteService, IWorkspaceMemberService memberService) : ApiControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetMyInvites()
        {
            return HandleResult(await inviteService.GetMyInvitesAsync(CurrentUserId));
        }

        public record AcceptInviteDto(string Token);

        [HttpPost("accept")]
        public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteDto dto) =>
        HandleResult(await memberService.AcceptWorkspaceInviteAsync(dto.Token, CurrentUserId));

    }
}