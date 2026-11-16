using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Comments
{
    public class UpdateCommentDto
    {
        [Required, MinLength(1), MaxLength(4000)]
        public string Content { get; set; } = null!;
    }
}