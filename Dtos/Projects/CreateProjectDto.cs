using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using api.Enums;

namespace api.Dtos.Projects
{
    public class CreateProjectDto
    {
        [Required]
        [MinLength(3)]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MinLength(3)]
        [MaxLength(2000)]
        public string? Description { get; set; } = null!;
        public ProjectStatus Status { get; set; } = ProjectStatus.NotStarted;
    }
}