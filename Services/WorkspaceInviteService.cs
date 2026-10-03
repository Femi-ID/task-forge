using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos;
using api.Dtos.WorkspaceInvite;
using api.Enums;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class WorkspaceInviteService(
        AppDbContext context,
        IWorkspaceAuthorization workspaceAuth,
        ITokenService tokenService) : IWorkspaceInvite
    {
        public async Task<Result<WorkspaceInviteResponseDto>> GenerateWorkspaceInviteAsync(GenerateWorkspaceInviteDto dto, Guid invitedByUserId)
        {
            // confirm if the invitingUser is an admin of the workspace
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(dto.WorkspaceId, invitedByUserId);
            if (!requesterIsAdmin) return Result<WorkspaceInviteResponseDto>.Failure("Only workspace admins can add members", ResultError.Forbidden);

            // confirm if the invited user is not a member of the workspace
            var alreadyMember = await context.WorkspaceMembers.AnyAsync(m =>
                m.AppUser.Email == dto.InvitedEmail && m.WorkspaceId == dto.WorkspaceId);
            if (alreadyMember) return Result<WorkspaceInviteResponseDto>.Failure("User is already a member of this workspace", ResultError.Conflict);

            // avoid re-creating another invite for a user who currently has a valid invite link
            var duplicatePending = await context.WorkspaceInvites.AnyAsync(i =>
                i.WorkspaceId == dto.WorkspaceId &&
                i.InvitedEmail == dto.InvitedEmail &&
                i.ExpireAt > DateTime.UtcNow);
            if (duplicatePending) return Result<WorkspaceInviteResponseDto>.Failure("An invite is already pending for this email", ResultError.Conflict);

            // generate token
            var (rawToken, hashedToken) = tokenService.GenerateRefreshToken(); // reused: random bytes + hash

            var inviteModel = WorkspaceInviteMapper.ToWorkspaceInviteModelFromGenerateInviteDto(dto, hashedToken, invitedByUserId);

            await context.WorkspaceInvites.AddAsync(inviteModel);
            await context.SaveChangesAsync();
            var response = new WorkspaceInviteResponseDto
            {
                InviteId = inviteModel.Id,
                WorkspaceId = inviteModel.WorkspaceId,
                InvitedEmail = inviteModel.InvitedEmail,
                ExpireAt = inviteModel.ExpireAt,
                RawToken = rawToken // it's only ever present in this response not stored, not logged
            };
            return Result<WorkspaceInviteResponseDto>.Success(response);
        }

        public async Task<Result<List<WorkspaceInviteDto>>> GetMyInvitesAsync(Guid invitedUserId)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == invitedUserId);
            if (user is null) return Result<List<WorkspaceInviteDto>>.Failure("Only workspace admins can view invites", ResultError.Validation);

            var invitations = await context.WorkspaceInvites
                .Include(i => i.Workspace).Include(i => i.InvitedByUser)
                .Where(i =>
                        i.InvitedEmail == user.Email &&
                        i.Status == WorkspaceInviteStatus.Pending &&
                        i.ExpireAt > DateTime.UtcNow)
                .OrderByDescending(i => i.ExpireAt)
                .ToListAsync();
            return Result<List<WorkspaceInviteDto>>.Success(invitations.Select(WorkspaceInviteMapper.ToWorkspaceInviteDto).ToList());
        }

        public async Task<Result<List<WorkspaceInviteDto>>> GetAllSentWorkspaceInvitesAsync(Guid workspaceId, Guid requestingUserId)
        {
            // confirm the requesting user is an admin
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, requestingUserId);
            if (!requesterIsAdmin) return Result<List<WorkspaceInviteDto>>.Failure("Only workspace admins can view invites", ResultError.Forbidden);

            var invites = await context.WorkspaceInvites
                .Include(i => i.Workspace).Include(i => i.InvitedByUser)
                .Where(i => i.WorkspaceId == workspaceId) // do i add "&& i.InvitedByUser == requestingUserId"?
                .ToListAsync();

            return Result<List<WorkspaceInviteDto>>.Success(invites.Select(WorkspaceInviteMapper.ToWorkspaceInviteDto).ToList());
        }

        public async Task<Result<WorkspaceInviteDto>> GetWorkspaceInviteByIdAsync(Guid workspaceId, Guid inviteId, Guid requestingUserId)
        {
            var invite = await context.WorkspaceInvites.Include(i => i.Workspace).Include(i => i.InvitedByUser)
                .FirstOrDefaultAsync(i => i.Id == inviteId && i.WorkspaceId == workspaceId); // added this- && i.WorkspaceId == workspaceId
            if (invite is null) return Result<WorkspaceInviteDto>.Failure("Invite not found", ResultError.NotFound);

            // confirm the requesting user is an admin
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(invite.WorkspaceId, requestingUserId);
            if (!requesterIsAdmin) return Result<WorkspaceInviteDto>.Failure("You don't have access to this invite", ResultError.Forbidden);

            return Result<WorkspaceInviteDto>.Success(WorkspaceInviteMapper.ToWorkspaceInviteDto(invite));
        }

        public async Task<Result<bool>> RevokeWorkspaceInviteAsync(Guid inviteId, Guid requestingUserId)
        {
            var invite = await context.WorkspaceInvites.FindAsync(inviteId);
            if (invite is null) return Result<bool>.Failure("Invite not found", ResultError.NotFound);

            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(invite.WorkspaceId, requestingUserId);
            if (!requesterIsAdmin) return Result<bool>.Failure("Only workspace admins can revoke invites", ResultError.Forbidden);

            if (invite.Status != WorkspaceInviteStatus.Pending)
                return Result<bool>.Failure("This invite is no longer pending", ResultError.Conflict);

            invite.Status = WorkspaceInviteStatus.Revoked;
            await context.SaveChangesAsync();
            return Result<bool>.Success(true);
        }
    };
}