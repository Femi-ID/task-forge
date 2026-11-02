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

            var invitedEmail = dto.InvitedEmail.Trim().ToLowerInvariant();

            // confirm if the invited user is not a member of the workspace
            var alreadyMember = await context.WorkspaceMembers.AnyAsync(m =>
                m.AppUser.NormalizedEmail == invitedEmail.ToUpperInvariant() && m.WorkspaceId == dto.WorkspaceId);
            if (alreadyMember) return Result<WorkspaceInviteResponseDto>.Failure("User is already a member of this workspace", ResultError.Conflict);

            // avoid re-creating another invite for a user who currently has a valid invite link
            var duplicatePending = await context.WorkspaceInvites.AnyAsync(i =>
                i.WorkspaceId == dto.WorkspaceId &&
                i.InvitedEmail == dto.InvitedEmail &&
                i.Status == WorkspaceInviteStatus.Pending &&
                i.ExpireAt > DateTime.UtcNow);
            if (duplicatePending) return Result<WorkspaceInviteResponseDto>.Failure("An invite to this workspace is already pending for this email", ResultError.Conflict);

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
                RawToken = rawToken // it's only present in this response not stored, not logged
            };
            return Result<WorkspaceInviteResponseDto>.Success(response);
        }

        public async Task<Result<List<WorkspaceInviteDto>>> GetMyInvitesAsync(Guid invitedUserId)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == invitedUserId);
            if (user?.Email is null) return Result<List<WorkspaceInviteDto>>.Failure("User not found", ResultError.NotFound);

            var email = user.Email.ToLowerInvariant();
            var invitations = await context.WorkspaceInvites.AsNoTracking()
                .Include(i => i.Workspace).Include(i => i.InvitedByUser)
                .Where(i =>
                        i.InvitedEmail == user.Email &&
                        i.Status == WorkspaceInviteStatus.Pending &&
                        i.ExpireAt > DateTime.UtcNow)
                .OrderByDescending(i => i.ExpireAt)
                .ToListAsync();
            if (invitations is null) return Result<List<WorkspaceInviteDto>>.Failure("No invites found", ResultError.NotFound);

            var invitationsDto = invitations.Select(WorkspaceInviteMapper.ToWorkspaceInviteDto).ToList();
            return Result<List<WorkspaceInviteDto>>.Success(invitationsDto);
        }

        public async Task<Result<List<WorkspaceInviteDto>>> GetAllSentWorkspaceInvitesAsync(Guid workspaceId, Guid requestingUserId)
        {
            // confirm the requesting user is an admin
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, requestingUserId);
            if (!requesterIsAdmin) return Result<List<WorkspaceInviteDto>>.Failure("Only workspace admins can view invites", ResultError.Forbidden);

            var invites = await context.WorkspaceInvites
                .AsNoTracking()
                .Include(i => i.Workspace).Include(i => i.InvitedByUser)
                .Where(i => i.WorkspaceId == workspaceId)
                .ToListAsync();

            return Result<List<WorkspaceInviteDto>>.Success(invites.Select(WorkspaceInviteMapper.ToWorkspaceInviteDto).ToList());
        }

        public async Task<Result<WorkspaceInviteDto>> GetWorkspaceInviteByIdAsync(Guid workspaceId, Guid inviteId, Guid requestingUserId)
        {
            // confirm the requesting user is an admin
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, requestingUserId);
            if (!requesterIsAdmin) return Result<WorkspaceInviteDto>.Failure("You don't have access to this invite", ResultError.Forbidden);

            var invite = await context.WorkspaceInvites.AsNoTracking()
                .Include(i => i.Workspace).Include(i => i.InvitedByUser)
                .FirstOrDefaultAsync(i => i.Id == inviteId && i.WorkspaceId == workspaceId);
            if (invite is null) return Result<WorkspaceInviteDto>.Failure("Invite not found", ResultError.NotFound);

            return Result<WorkspaceInviteDto>.Success(WorkspaceInviteMapper.ToWorkspaceInviteDto(invite));
        }

        public async Task<Result<bool>> RevokeWorkspaceInviteAsync(Guid workspaceId, Guid inviteId, Guid requestingUserId)
        {
            var requesterIsAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, requestingUserId);
            if (!requesterIsAdmin) return Result<bool>.Failure("Only workspace admins can revoke invites", ResultError.Forbidden);

            var invite = await context.WorkspaceInvites.FirstOrDefaultAsync(i => i.Id == inviteId && i.WorkspaceId == workspaceId);
            if (invite is null) return Result<bool>.Failure("Invite not found", ResultError.NotFound);

            if (invite.Status != WorkspaceInviteStatus.Pending)
                return Result<bool>.Failure("This invite is no longer pending", ResultError.Conflict);

            invite.Status = WorkspaceInviteStatus.Revoked;
            await context.SaveChangesAsync();
            return Result<bool>.Success(true);
        }
    };
}