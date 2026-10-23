using System;
using System.Collections.Generic;
using System.Linq;
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
                Tasks = projectModel.Tasks // i want this to be a list of the tasks (the task title and the id acting as a pointer to the task)
            };
        }
    }
}