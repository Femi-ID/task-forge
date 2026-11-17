using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Enums;

namespace api.Dtos.Tasks
{
    public class TaskResponseDto
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public Guid ProjectId { get; set; }
        public TaskItemStatus Status { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime DueDate { get; set; }
        public Guid? AssigneeId { get; set; }
        public string? AssigneeFullName { get; set; }
        public List<TaskLabelDto> Labels { get; set; } = [];
        public int CommentCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class TaskResponseDtoList
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public TaskItemStatus Status { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime DueDate { get; set; }
        public Guid? AssigneeId { get; set; }
        public string? AssigneeFullName { get; set; }
        public int CommentCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class TaskStatusResponseDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public TaskItemStatus Status { get; set; }
}
}