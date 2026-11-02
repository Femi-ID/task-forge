using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using api.Dtos.Projects;
using api.Models;

namespace api.Mappers
{
    public class ProjectMappers
    {
        public static Project ToProjectModelFromCreateProjectDto(CreateProjectDto dto, Guid workspaceId)
        {
            return new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                Status = dto.Status,
                WorkspaceId = workspaceId
            };
        }

        public static List<ProjectResponseDtoList> ToProjectResponseDtoList(List<Project> projects)
        {
            return projects.Select(p => new ProjectResponseDtoList
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status,
                WorkspaceId = p.WorkspaceId,
                TaskCount = p.Tasks.Count,
                CreatedAt = p.CreatedAt
            }).ToList();
        }

        // For a project we already hold in memory (after create/update). Tasks aren't loaded there, so it's empty.
        public static ProjectResponseDto ToProjectDto(Project projectModel)
        {
            return new ProjectResponseDto
            {
                Id = projectModel.Id,
                Name = projectModel.Name,
                Description = projectModel.Description,
                Status = projectModel.Status,
                WorkspaceId = projectModel.WorkspaceId,
                CreatedAt = projectModel.CreatedAt,
                UpdatedAt = projectModel.UpdatedAt
            };
        }

        // The two mappers below are Expressions (not methods) so EF translates them into the SQL SELECT:
        // no entities are materialized, tasks are never loaded for the list, and no entity can leak into a response.
        public static readonly Expression<Func<Project, ProjectResponseDtoList>> ToListDto = p => new ProjectResponseDtoList
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Status = p.Status,
            WorkspaceId = p.WorkspaceId,
            TaskCount = p.Tasks.Count,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };

        public static readonly Expression<Func<Project, ProjectResponseDto>> ToDetailDto = p => new ProjectResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Status = p.Status,
            WorkspaceId = p.WorkspaceId,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Tasks = p.Tasks
                .OrderBy(t => t.CreatedAt)
                .Select(t => new TaskSummaryDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.Status,
                    Priority = t.Priority
                }).ToList()
        };
    }
}