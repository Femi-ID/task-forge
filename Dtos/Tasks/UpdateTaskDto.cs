using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using api.Enums;

namespace api.Dtos.Tasks
{
    /// <summary>
    /// Partial update, same convention as UpdateProjectDto: null = leave unchanged.
    /// Description "" clears it. AssigneeId is the one new wrinkle — see UpdateTaskAsync for why
    /// Guid.Empty, not null, means "unassign."
    /// </summary>
    public class UpdateTaskDto
    {
        [MinLength(3), MaxLength(200)]
        public string? Title { get; set; }

        [MaxLength(4000)]
        public string? Description { get; set; }

        public Guid? AssigneeId { get; set; }

        public TaskPriority? Priority { get; set; }

        public DateTime? DueDate { get; set; }
    }

    public class UpdateTaskStatusDto
    {
        [Required]
        public TaskItemStatus Status { get; set; }
    }
}