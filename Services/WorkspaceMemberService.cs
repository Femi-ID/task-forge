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
        ITokenService tokenService): IWorkspaceMemberService
    {
        public async Task<Result<WorkspaceMember>> AddWorkspaceMemberAsync(
            Guid workspaceId, Guid requestingUserId, CreateWorkspaceMemberDto dto)
        {
            // confirm if the requesting user is an admin in that workspace
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, requestingUserId);
            if (!requesterIsAdmin) return Result<WorkspaceMember>.Failure("Only workspace admins can add members", ResultError.Forbidden);

            // confirm if the target user exists
            var targetUserExists = await context.Users.AnyAsync(u => u.Id == dto.AppUserId);
            if (!targetUserExists) return Result<WorkspaceMember>.Failure("User not found", ResultError.NotFound);

            // check if the target user is already a member of the workspace
            var alreadyMember = await context.WorkspaceMembers.AnyAsync(m =>
                m.AppUserId == dto.AppUserId && m.WorkspaceId == workspaceId);
            if (alreadyMember) return Result<WorkspaceMember>.Failure("User is already a member of this workspace", ResultError.Conflict);

            // add the target user to the workspace 
            var newWorkspaceMemberModel = WorkspaceMemberMapper.ToWorkspaceMemberModelFromCreateDto(dto.AppUserId, workspaceId, dto.Role);
            await context.WorkspaceMembers.AddAsync(newWorkspaceMemberModel);
            await context.SaveChangesAsync();
            return Result<WorkspaceMember>.Success(newWorkspaceMemberModel);
        }

        public async Task<Result<WorkspaceMember?>> UpdateWorkspaceMemberRoleAsync(
                Guid workspaceId,
                Guid requestingUserId,
                Guid targetUserId,
                WorkspaceRole newRole)
        {
            // do i need to: confirm if the requesting user is the same as the target user,allow but check if they're the last admin

            // confirm if the requesting user is an admin in that workspace
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, requestingUserId);
            if (!requesterIsAdmin) return Result<WorkspaceMember?>.Failure("Only workspace admins can remove members", ResultError.Forbidden);

            // check if the target user is already a member of the workspace
            var workspaceMember = await GetWorkspaceMember(targetUserId, workspaceId);
            if (workspaceMember is null) return Result<WorkspaceMember?>.Failure("Target user is not a member of the workspace", ResultError.Validation);

            // get workspace
            var workspace = await workspaceAuth.GetTrackedWorkspaceAsync(workspaceId);
            if (workspace is null) return Result<WorkspaceMember?>.Failure("Workspace does not exist", ResultError.NotFound);

            // ensure the user(user's role) being changed isn't the owner
            if (workspaceMember.AppUserId == workspace.OwnerId)
                return Result<WorkspaceMember?>.Failure("Owner must always be an admin", ResultError.Forbidden);

            workspaceMember.Role = newRole;
            await context.SaveChangesAsync();
            return Result<WorkspaceMember?>.Success(workspaceMember);
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

            // confirm that number of admins left is at least 1, although you need to be an admin to even remove another admin
            var numberOfAdmins = workspace.WorkspaceMembers.Count(w => w.Role == WorkspaceRole.Admin);
            if (numberOfAdmins == 1) return Result<bool>.Failure("A workspace must have at least 1 admin!", ResultError.Conflict);

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
            
            var numberOfAdmins = workspace.WorkspaceMembers.Count(w => w.Role == WorkspaceRole.Admin);
            if (numberOfAdmins == 1)
            {
                var lastAdmin = await context.WorkspaceMembers.FirstOrDefaultAsync(w => w.Role == WorkspaceRole.Admin);
                var isUserLastAdmin = lastAdmin?.AppUserId == userId;
                if (isUserLastAdmin)
                    return Result<bool>.
                        Failure("User cannot be removed cause they are the last admin. Add/promote another user as admin first", ResultError.Conflict);
            }

            context.WorkspaceMembers.Remove(workspaceMember);
            await context.SaveChangesAsync();
            return Result<bool>.Success(true); // user successfully removed from the workspace
        }

        public async Task<WorkspaceMember?> GetWorkspaceMember(Guid targetUserId, Guid workspaceId)
        {
            var workspaceMember = await context.WorkspaceMembers.FirstOrDefaultAsync(m =>
                m.AppUserId == targetUserId && m.WorkspaceId == workspaceId);
            if (workspaceMember is null) return null;

            return workspaceMember;
        }

        public async Task<Result<WorkspaceMember>> AcceptWorkspaceInviteAsync(string rawToken, Guid acceptingUserId)
        {
            // confirm the hashToken exists
            var tokenHash = tokenService.HashToken(rawToken);
            var invite = await context.WorkspaceInvites.FirstOrDefaultAsync(i => i.TokenHash == tokenHash);
            if (invite is null) return Result<WorkspaceMember>.Failure("Invalid invite", ResultError.NotFound);

            // confirm the invite is still valid
            if (invite.Status != WorkspaceInviteStatus.Pending)
                return Result<WorkspaceMember>.Failure("This invite is no longer valid", ResultError.Conflict);

            if (invite.ExpireAt <= DateTime.UtcNow)
            {
                invite.Status = WorkspaceInviteStatus.Expired;
                await context.SaveChangesAsync();
                return Result<WorkspaceMember>.Failure("This invite has expired", ResultError.Conflict);
            }

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

            var member = WorkspaceMemberMapper.ToWorkspaceMemberModelFromCreateDto(acceptingUserId, invite.WorkspaceId, invite.Role);
            await context.WorkspaceMembers.AddAsync(member);
            invite.Status = WorkspaceInviteStatus.Accepted;
            await context.SaveChangesAsync();

            return Result<WorkspaceMember>.Success(member);
        }
    }
}