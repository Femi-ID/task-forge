using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using api.Enums;

namespace api.Dtos.Workspace
{
    public class WorkspaceDetailDto
    {
        public Guid Id { get; set; }

        [Required]
        public required string Name { get; set; } = null!;
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }
        public string OwnerFullName { get; set; } = null!;
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<WorkspaceProjectSummaryDto> Projects { get; set; } = [];
        public List<WorkspaceLabelDto> Labels { get; set; } = [];
    }

    public class WorkspaceProjectSummaryDto
    {
        public Guid Id { get; set; } 
        public required string Name { get; set; }
        public ProjectStatus Status { get; set; }
        public int TaskCount { get; set; }
    }

    public class WorkspaceLabelDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Colour { get; set; }
    }
}