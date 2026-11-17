using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Labels
{
    public class UpdateLabelDto
    {
        [MinLength(1), MaxLength(50)]
        public string? Name { get; set; }
        [MaxLength(9)]
        public string? Colour { get; set; }
    }
}