using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using api.Enums;

namespace api.Dtos.Projects
{
    public class UpdateProjectDto
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(2000)]
        [DataType(DataType.Text)]
        public string? Description { get; set; }

        public ProjectStatus? Status { get; set; }
    }
}