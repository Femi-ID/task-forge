using System;
using api.Enums;

namespace api.Dtos.Projects
{
    /// <summary>Lightweight pointer to a task: enough to render a list and link to the full task.</summary>
    public class TaskSummaryDto
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public TaskItemStatus Status { get; set; }
        public TaskPriority Priority { get; set; }
    }
}
