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

        public static Workspace ToWorkspaceModelFromUpdateDto(UpdateWorkspaceDto updateDto)
        {
            if (string.IsNullOrWhiteSpace(updateDto.Name) && string.IsNullOrWhiteSpace(updateDto.Description))
            {
                return null!;
            };
            //  write code that confirms update.Name and .Description are not null
            return new Workspace
            {
                Name = updateDto.Name!,
                Description = updateDto.Description
            };
        }
    }
}