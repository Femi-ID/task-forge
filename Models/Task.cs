using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Enums;

namespace api.Models
{
    public class Task
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public required string ProjectId { get; set; }
        public required Project Project { get; set; }
        public required string AssigneeId { get; set; }
        public required AppUser AppUser { get; set; }
        public TaskPriority Priority { get; set; }
        public TaskStatus Status { get; set; }
        public required DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<Comment> Comments { get; set; } = new List<Comment>();
        public List<TaskLabel> TaskLabels { get; set; } = new List<TaskLabel>();
        public DateTime UpdatedAt { get; set; }
    }
}