using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Enums;
using api.Models;

namespace api.Dtos.Projects
{
    public class ProjectResponseDto
    {
        public Guid Id { get; set; }
         public required string Name { get; set; }
        public string? Description { get; set; }
        public ProjectStatus Status { get; set; }
         public Guid WorkspaceId { get; set; }
         public DateTime CreatedAt { get; set; }
         public List<TaskItem> Tasks { get; set; } = [];
    }
    public class ProjectResponseDtoList
    {
        public Guid Id { get; set; }
         public required string Name { get; set; }
        public string? Description { get; set; }
        public ProjectStatus Status { get; set; }
         public Guid WorkspaceId { get; set; }
         public int TaskCount { get; set; }
         public DateTime CreatedAt { get; set; }
    }
}