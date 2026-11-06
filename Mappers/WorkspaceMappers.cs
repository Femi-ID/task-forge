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
                Name = workspaceDto.Name.Trim(),
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
                Name = workspaceModel.Name.Trim(),
                Description = workspaceModel.Description,
                OwnerId = workspaceModel.OwnerId,
                OwnerFullName = workspaceModel.Owner.FullName,
                CreatedAt = workspaceModel.CreatedAt,
                UpdatedAt = workspaceModel.UpdatedAt,
            };
        }

        public static readonly Expression<Func<Workspace, WorkspaceDetailDto>> ToWorkspaceDetailDto = w => new WorkspaceDetailDto
        {
            Id = w.Id,
            Name = w.Name,
            Description = w.Description,
            OwnerId = w.OwnerId,
            OwnerFullName = w.Owner.FullName,
            MemberCount = w.WorkspaceMembers.Count,
            CreatedAt = w.CreatedAt,
            UpdatedAt = w.UpdatedAt,
            Projects = w.Projects
                .Where(p => p.Status != ProjectStatus.Archived && p.WorkspaceId == w.Id)
                .OrderBy(p => p.Name)
                .Select(p => new WorkspaceProjectSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Status = p.Status,
                    TaskCount = p.Tasks.Count
                })
                .ToList(),
            Labels = w.Labels
                .OrderBy(l => l.Name)
                .Select(l => new WorkspaceLabelDto
                {
                    Id = l.Id,
                    Colour = l.Colour,
                    Name = l.Name
                })
                .ToList(),
        };

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