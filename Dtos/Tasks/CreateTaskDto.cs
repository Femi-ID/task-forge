using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using api.Enums;

namespace api.Dtos.Tasks
{
    public class CreateTaskDto
    {
        [Required, MinLength(3), MaxLength(200)]
        public string Title { get; set; } = null!;
        [MaxLength(4000)]
        public string? Description { get; set; }
        public Guid? AssigneeId { get; set; }
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        [Required]
        public DateTime DueDate { get; set; }
    }
}