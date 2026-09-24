using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Workspace
{
    public class CreateWorkspaceDto
    {
        [Required]
        [MaxLength(100)]
        public required string Name { get; set; } = null!;

        [MaxLength(1000)]
        [DataType(DataType.Text)]
        public string? Description { get; set; }
    }
}