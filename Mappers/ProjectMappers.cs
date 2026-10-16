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
        public static Project ToProjectModelFromCreateProjectDto(CreateProjectDto dto)
        {
            return new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                Status = dto.Status,
                WorkspaceId = dto.WorkspaceId
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
                Tasks = p.Tasks,
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
                CreatedAt = projectModel.CreatedAt
            };
        }
    }
}