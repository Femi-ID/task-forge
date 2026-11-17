using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Labels
{
    public class CreateLabelDto
    {
        [Required, MinLength(1), MaxLength(50)]
        public string Name { get; set; } = null!;
        [Required, MaxLength(9)] // #RRGGBB
        public string Colour { get; set; } = null!;
    }
}