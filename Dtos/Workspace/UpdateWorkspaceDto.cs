using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Workspace
{
    public class UpdateWorkspaceDto
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(1000)]
        [DataType(DataType.Text)]
        public string? Description { get; set; }
    }
}