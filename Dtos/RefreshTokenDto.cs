using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using api.Models;

namespace api.Dtos
{
    public class RefreshTokenDto
    {
        // public required AppUser User{ get; set; }
        [Required]
        public required string RefreshToken { get; set; } 
    }
}