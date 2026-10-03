using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Workspace;
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

        public static WorkspaceDto ToWorkspaceDto(Workspace workspaceModel, Guid userId)
        {
            return new WorkspaceDto
            {
                Id = workspaceModel.Id,
                Name = workspaceModel.Name,
                Description = workspaceModel.Description,
                OwnerId = userId
            };
        }

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