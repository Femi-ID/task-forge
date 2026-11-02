using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos;
using api.Dtos.WorkspaceMember;
using api.Enums;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class WorkspaceMemberService(
        AppDbContext context,
        IWorkspaceAuthorization workspaceAuth,
        ITokenService tokenService) : IWorkspaceMemberService
    {
        public async Task<Result<WorkspaceMemberDto>> AddWorkspaceMemberAsync(
            Guid workspaceId, Guid requestingUserId, CreateWorkspaceMemberDto dto)
        {
            // confirm if the requesting user is an admin in that workspace
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, requestingUserId);
            if (!requesterIsAdmin) return Result<WorkspaceMemberDto>.Failure("Only workspace admins can add members", ResultError.Forbidden);

            // confirm if the target user exists
            var targetUserExists = await context.Users.AnyAsync(u => u.Id == dto.AppUserId);
            if (!targetUserExists) return Result<WorkspaceMemberDto>.Failure("User not found", ResultError.NotFound);

            // check if the target user is already a member of the workspace
            var alreadyMember = await context.WorkspaceMembers.AnyAsync(m =>
                m.AppUserId == dto.AppUserId && m.WorkspaceId == workspaceId);
            if (alreadyMember) return Result<WorkspaceMemberDto>.Failure("User is already a member of this workspace", ResultError.Conflict);

            // add the target user to the workspace 
            var newWorkspaceMemberModel = WorkspaceMemberMapper.ToWorkspaceMemberModelFromCreateDto(dto.AppUserId, workspaceId, dto.Role);
            await context.WorkspaceMembers.AddAsync(newWorkspaceMemberModel);
            await context.SaveChangesAsync();

            // abstract data with dto 
            var workspaceMemberDto = WorkspaceMemberMapper.ToWorkspaceMemberDto(newWorkspaceMemberModel);
            return Result<WorkspaceMemberDto>.Success(workspaceMemberDto);
        }

        public async Task<Result<WorkspaceMemberDto>> UpdateWorkspaceMemberRoleAsync(
                Guid workspaceId,
                Guid requestingUserId,
                Guid targetUserId,
                WorkspaceRole newRole)
        {
            // confirm if the requesting user is an admin in that workspace
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, requestingUserId);
            if (!requesterIsAdmin) return Result<WorkspaceMemberDto>.Failure("Only workspace admins can change member role", ResultError.Forbidden);

            // check if the target user is already a member of the workspace
            var workspaceMember = await GetWorkspaceMember(targetUserId, workspaceId);
            if (workspaceMember is null) return Result<WorkspaceMemberDto>.Failure("Target user is not a member of the workspace", ResultError.Validation);

            // get workspace
            var workspace = await workspaceAuth.GetTrackedWorkspaceAsync(workspaceId);
            if (workspace is null) return Result<WorkspaceMemberDto>.Failure("Workspace does not exist", ResultError.NotFound);

            // ensure the user(user's role) being changed isn't the owner
            if (workspaceMember.AppUserId == workspace.OwnerId)
                return Result<WorkspaceMemberDto>.Failure("Owner must always be an admin", ResultError.Forbidden);

            workspaceMember.Role = newRole;
            await context.SaveChangesAsync();

            // abstract data with dto
            var workspaceMemberDto = WorkspaceMemberMapper.ToWorkspaceMemberDto(workspaceMember);
            return Result<WorkspaceMemberDto>.Success(workspaceMemberDto);
        }

        public async Task<Result<bool>> RemoveWorkspaceMemberAsync(Guid workspaceId, Guid requestingUserId, Guid targetUserId)
        {
            // confirm if the requesting user is an admin in that workspace
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, requestingUserId);
            if (!requesterIsAdmin) return Result<bool>.Failure("Only workspace admins can remove members", ResultError.Forbidden);

            // check if the target user is a member of the workspace
            var workspaceMember = await GetWorkspaceMember(targetUserId, workspaceId);
            if (workspaceMember is null) return Result<bool>.Failure("Target user is not a member of the workspace", ResultError.Validation);

            var workspace = await workspaceAuth.GetTrackedWorkspaceAsync(workspaceId);
            if (workspace is null) return Result<bool>.Failure("Workspace does not exist", ResultError.NotFound);

            // ensure the user being removed isn't the owner
            if (workspaceMember.AppUserId == workspace.OwnerId)
                return Result<bool>.Failure("Owner must transfer ownership before being removed", ResultError.Conflict);

            // confirm that the targetUser is not the last admin and number of admins left is at least 2
            if (workspaceMember.Role == WorkspaceRole.Admin && await IsLastAdminAsync(workspaceId))
                return Result<bool>.Failure("A workspace must have at least 1 admin", ResultError.Conflict);

            context.WorkspaceMembers.Remove(workspaceMember);
            await context.SaveChangesAsync();
            return Result<bool>.Success(true); // user successfully removed from the workspace
        }

        public async Task<Result<bool>> RemoveSelfMemberAsync(Guid workspaceId, Guid userId)
        {
            // check if the user is a member of the workspace
            var workspaceMember = await GetWorkspaceMember(userId, workspaceId);
            if (workspaceMember is null) return Result<bool>.Failure("Target user is not a member of the workspace", ResultError.Validation);

            // get the workspace
            var workspace = await workspaceAuth.GetTrackedWorkspaceAsync(workspaceId);
            if (workspace is null) return Result<bool>.Failure("Workspace does not exist", ResultError.NotFound);

            // ensure the user being removed is neither the owner nor the last admin
            if (workspaceMember.AppUserId == workspace.OwnerId)
                return Result<bool>.Failure("Owner must transfer ownership before being removed", ResultError.Conflict);

             if (workspaceMember.Role == WorkspaceRole.Admin && await IsLastAdminAsync(workspaceId))
                return Result<bool>.Failure("You are the last admin. Promote another member to admin first", ResultError.Conflict);

            context.WorkspaceMembers.Remove(workspaceMember);
            await context.SaveChangesAsync();
            return Result<bool>.Success(true); // user successfully removed from the workspace
        }

        public async Task<Result<WorkspaceMemberDto>> GetWorkspaceMemberByIdAsync(Guid targetUserId, Guid workspaceId, Guid requestingUserId)
        {
            // without this, anyone logged in could read any user's role in any workspace just by guessing ids
            var isMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, requestingUserId);
            if (!isMember) return Result<WorkspaceMemberDto>.Failure("Only workspace members can view members", ResultError.Forbidden);

            var workspaceMember = await context.WorkspaceMembers.FirstOrDefaultAsync(m =>
                m.AppUserId == targetUserId && m.WorkspaceId == workspaceId);
            if (workspaceMember is null) return Result<WorkspaceMemberDto>.Failure("Member not found", ResultError.NotFound);

            return Result<WorkspaceMemberDto>.Success(WorkspaceMemberMapper.ToWorkspaceMemberDto(workspaceMember));
        }

        public async Task<Result<List<WorkspaceMemberDto>>> GetWorkspaceMembersAsync(Guid workspaceId, Guid requestingUserId)
        {
            var isMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, requestingUserId);
            if (!isMember) return Result<List<WorkspaceMemberDto>>.Failure("Only workspace members can view members", ResultError.Forbidden);

            var members = await context.WorkspaceMembers.AsNoTracking()
                .Where(m => m.WorkspaceId == workspaceId)
                .OrderBy(m => m.Role == WorkspaceRole.Admin ? 0 : 1)
                .ThenBy(m => m.AppUser.FullName)
                .Select(m => WorkspaceMemberMapper.ToWorkspaceMemberDto(m))
                .ToListAsync();

            return Result<List<WorkspaceMemberDto>>.Success(members);
        }

        public async Task<Result<WorkspaceMember>> AcceptWorkspaceInviteByTokenAsync(string rawToken, Guid acceptingUserId)
        {
            // confirm the hashToken exists
            var tokenHash = tokenService.HashToken(rawToken);
            var invite = await context.WorkspaceInvites.FirstOrDefaultAsync(i => i.TokenHash == tokenHash);
            if (invite is null) return Result<WorkspaceMember>.Failure("Invalid invite", ResultError.NotFound);
            return await VerifyAndAcceptInviteAsync(invite, acceptingUserId);
        }

        public async Task<Result<WorkspaceMember>> AcceptWorkspaceInviteByIdAsync(Guid inviteId, Guid acceptingUserId)
        {
            // confirm the invite exists
            var invite = await context.WorkspaceInvites.FindAsync(inviteId);
            if (invite is null) return Result<WorkspaceMember>.Failure("Invite not found", ResultError.NotFound);
            return await VerifyAndAcceptInviteAsync(invite, acceptingUserId);
        }

        // private helpers
        private async Task<Result<WorkspaceMember>> VerifyAndAcceptInviteAsync(WorkspaceInvite invite, Guid acceptingUserId)
        {
            // confirm the invite is still valid
            if (invite.Status != WorkspaceInviteStatus.Pending)
                return Result<WorkspaceMember>.Failure("This invite is no longer valid", ResultError.Conflict);

            if (invite.ExpireAt <= DateTime.UtcNow)
            {
                invite.Status = WorkspaceInviteStatus.Expired;
                await context.SaveChangesAsync();
                return Result<WorkspaceMember>.Failure("This invite has expired", ResultError.Conflict);
            }

            // fetch user
            var acceptingUser = await context.Users.FindAsync(acceptingUserId);
            if (acceptingUser is null) return Result<WorkspaceMember>.Failure("User not found", ResultError.NotFound);

            // confirm the user is the expected receiving user
            if (!string.Equals(acceptingUser.Email, invite.InvitedEmail, StringComparison.OrdinalIgnoreCase))
                return Result<WorkspaceMember>.Failure("You are not permitted to use this invite!", ResultError.Forbidden);

            //  verify the user is not yet a member
            var alreadyMember = await context.WorkspaceMembers.AnyAsync(m =>
                m.AppUserId == acceptingUserId && m.WorkspaceId == invite.WorkspaceId);
            if (alreadyMember)
            {
                invite.Status = WorkspaceInviteStatus.Accepted;
                await context.SaveChangesAsync();
                return Result<WorkspaceMember>.Failure("You are already a member of this workspace", ResultError.Conflict);
            }

            //  add user to the workspace
            var member = WorkspaceMemberMapper.ToWorkspaceMemberModelFromCreateDto(acceptingUserId, invite.WorkspaceId, invite.Role);
            await context.WorkspaceMembers.AddAsync(member);
            invite.Status = WorkspaceInviteStatus.Accepted;
            await context.SaveChangesAsync();

            return Result<WorkspaceMember>.Success(member);
        }

         private async Task<WorkspaceMember?> GetWorkspaceMember(Guid targetUserId, Guid workspaceId) =>
            await context.WorkspaceMembers.FirstOrDefaultAsync(m => m.AppUserId == targetUserId && m.WorkspaceId == workspaceId);

        private async Task<bool> IsLastAdminAsync(Guid workspaceId)
        {
            return await context.WorkspaceMembers.CountAsync(m => m.WorkspaceId == workspaceId && m.Role == WorkspaceRole.Admin) <= 1;
        }
    }
}