using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using api.Enums;

namespace api.Dtos
{
    public class GenerateWorkspaceInviteDto
    {
        [Required]
        public Guid WorkspaceId { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string InvitedEmail { get; set; } = null!;

        [Required]
        public WorkspaceRole Role { get; set; }
    }
}