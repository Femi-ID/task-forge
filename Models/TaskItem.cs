using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Enums;

namespace api.Models
{
    public class TaskItem: IUpdatable
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;
        // Optional: a task can exist unassigned
        public Guid? AssigneeId { get; set; }
        public AppUser? Assignee { get; set; }
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;
        public required DateTime DueDate { get; set; }
        public List<Comment> Comments { get; set; } = new List<Comment>();
        public List<TaskLabel> TaskLabels { get; set; } = new List<TaskLabel>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}