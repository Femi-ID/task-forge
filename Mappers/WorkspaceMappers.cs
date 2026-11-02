using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using api.Dtos.Workspace;
using api.Dtos.WorkspaceMember;
using api.Enums;
using api.Models;

namespace api.Mappers
{
    public static class WorkspaceMappers
    {
        public static Workspace ToWorkspaceModelFromCreateDto(CreateWorkspaceDto workspaceDto, Guid ownerId)
        {
            return new Workspace
            {
                Name = workspaceDto.Name,
                Description = workspaceDto.Description,
                OwnerId = ownerId
            };
        }

        public static List<WorkspaceDto> ToWorkspaceDtoList(List<Workspace> workspaces) =>
        workspaces.Select(w => new WorkspaceDto
        {
            Id = w.Id,
            Name = w.Name,
            Description = w.Description,
            OwnerId = w.OwnerId
        }).ToList();

        public static WorkspaceDto ToWorkspaceDto(Workspace workspaceModel)
        {
            return new WorkspaceDto
            {
                Id = workspaceModel.Id,
                Name = workspaceModel.Name,
                Description = workspaceModel.Description,
                OwnerId = workspaceModel.OwnerId
            };
        }

        // public static readonly Expression<Func<Workspace, WorkspaceDto>> ToWorkspaceDetailDto = w => new WorkspaceDto
        // {
        //     Id = w.Id,
        //     Name = w.Name,
        //     Description = w.Description,
        //     OwnerId = w.OwnerId,
        //     OwnerFullName = w.Owner.FullName,
        //     WorkspaceMembers = w.WorkspaceMembers
        //     .Where(w => w.WorkspaceId == )
        //         .OrderBy(m => m.Role ==WorkspaceRole.Admin ? 0: 1)
        //         .Select(m => new WorkspaceMemberDto
        //         {
        //             FullName = m.AppUser.FullName,
        //             Email = m.AppUser.Email!,
        //             Role = m.
        //         })
        //         .ToList(),
        //     CreatedAt = p.CreatedAt,
        //     UpdatedAt = p.UpdatedAt,
        // };

        // public static Workspace ToWorkspaceModelFromUpdateDto(UpdateWorkspaceDto updateDto)
        // {
        //     if (string.IsNullOrWhiteSpace(updateDto.Name) && string.IsNullOrWhiteSpace(updateDto.Description))
        //     {
        //         return null!;
        //     }
        //     ;
        //     //  write code that confirms update.Name and .Description are not null
        //     return new Workspace
        //     {
        //         Name = updateDto.Name!,
        //         Description = updateDto.Description
        //     };
        // }

        // public static WorkspaceDto ToWorkspaceDtoFromGetAll(Workspace workspaceModel, Guid userId)
        // {
        //     return new WorkspaceDto
        //     {
        //         Id = workspaceModel.Id,
        //         Name = workspaceModel.Name,
        //         Description = workspaceModel.Description,
        //         OwnerId = userId
        //     };
        // }
    }
}