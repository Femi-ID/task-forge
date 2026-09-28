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
    public class WorkspaceMemberService(AppDbContext context, WorkspaceService workspaceService): IWorkspaceMemberService
    {
        public async Task<Result<WorkspaceMember>> AddWorkspaceMemberAsync(
            Guid workspaceId, Guid requestingUserId, CreateWorkspaceMemberDto dto)
        {
            // confirm if the requesting user is an admin in that workspace
            var requesterIsAdmin = await workspaceService.IsWorkspaceAdminMemberAsync(workspaceId, requestingUserId);
            if (!requesterIsAdmin) return Result<WorkspaceMember>.Failure("Only workspace admins can add members");

            // confirm if the target user exists
            var targetUserExists = await context.Users.AnyAsync(u => u.Id == dto.AppUserId);
            if (!targetUserExists) return Result<WorkspaceMember>.Failure("User not found");

            // check if the target user is already a member of the workspace
            var alreadyMember = await context.WorkspaceMembers.AnyAsync(m =>
                m.AppUserId == dto.AppUserId && m.WorkspaceId == workspaceId);
            if (alreadyMember) return Result<WorkspaceMember>.Failure("User is already a member of this workspace");

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
            // confirm if the requesting user is an admin in that workspace
            var requesterIsAdmin = await workspaceService.IsWorkspaceAdminMemberAsync(workspaceId, requestingUserId);
            if (!requesterIsAdmin) return Result<WorkspaceMember?>.Failure("Only workspace admins can remove members");

            // check if the target user is already a member of the workspace
            var workspaceMember = await GetWorkspaceMember(targetUserId, workspaceId);
            if (workspaceMember is null) return Result<WorkspaceMember?>.Failure("Target user is not a member of the workspace");

            workspaceMember.Role = newRole;
            await context.SaveChangesAsync();
            return Result<WorkspaceMember?>.Success(workspaceMember);
        }

        public async Task<Result<bool>> RemoveWorkspaceMemberAsync(Guid workspaceId, Guid requestingUserId, Guid targetUserId)
        {
            // confirm if the requesting user is an admin in that workspace
            var requesterIsAdmin = await workspaceService.IsWorkspaceAdminMemberAsync(workspaceId, requestingUserId);
            if (!requesterIsAdmin) return Result<bool>.Failure("Only workspace admins can remove members");

            // check if the target user is a member of the workspace
            var workspaceMember = await GetWorkspaceMember(targetUserId, workspaceId);
            if (workspaceMember is null) return Result<bool>.Failure("Target user is not a member of the workspace");

            var workspace = await workspaceService.GetTrackedWorkspaceAsync(workspaceId);
            if (workspace is null) return Result<bool>.Failure("Workspace does not exist");

            if (workspaceMember.AppUserId == workspace.OwnerId)
                return Result<bool>.Failure("Owner must transfer ownership before being removed");

            // confirm that number of admins left is at least 1, although you need to be an admin to even remove another admin
            var numberOfAdmins = workspace.WorkspaceMembers.Count(w => w.Role == WorkspaceRole.Admin);
            if (numberOfAdmins == 1) return Result<bool>.Failure("A workspace must have at least 1 admin!");

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
    }
}