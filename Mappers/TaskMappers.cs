using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using api.Dtos.Tasks;
using api.Models;

namespace api.Mappers
{
    public class TaskMappers
    {
        public static TaskItem ToTaskModelFromCreateDto(CreateTaskDto dto, Guid projectId)
        {
            return new TaskItem
            {
                Title = dto.Title.Trim(),
                Description = dto.Description,
                ProjectId = projectId,
                AssigneeId = dto.AssigneeId,
                Priority = dto.Priority,
                DueDate = dto.DueDate
            };
        }

        // Expressions: these read t.Assignee.FullName and t.TaskLabels/t.Comments, which only resolve
        // correctly when EF translates the whole expression into SQL. A plain method here would hit the
        // exact NullReferenceException we just fixed on WorkspaceMappers.ToWorkspaceDto — never map an
        // in-memory tracked TaskItem through these; always query through them.
        public static readonly Expression<Func<TaskItem, TaskResponseDtoList>> ToListDto = taskItem => new TaskResponseDtoList
        {
            Id = taskItem.Id,
            Title = taskItem.Title,
            Status = taskItem.Status,
            Priority = taskItem.Priority,
            DueDate = taskItem.DueDate,
            AssigneeId = taskItem.AssigneeId,
            AssigneeFullName = taskItem.Assignee != null ? taskItem.Assignee.FullName : null,
            CommentCount = taskItem.Comments.Count(c => !c.IsDeleted),
            CreatedAt = taskItem.CreatedAt,
            UpdatedAt = taskItem.UpdatedAt
        };

        public static readonly Expression<Func<TaskItem, TaskResponseDto>> ToDetailDto = t => new TaskResponseDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            ProjectId = t.ProjectId,
            Status = t.Status,
            Priority = t.Priority,
            DueDate = t.DueDate,
            AssigneeId = t.AssigneeId,
            AssigneeFullName = t.Assignee != null ? t.Assignee.FullName : null,
            Labels = t.TaskLabels
                .Select(tl => new TaskLabelDto { Id = tl.Label.Id, Name = tl.Label.Name, Colour = tl.Label.Colour })
                .ToList(),
            CommentCount = t.Comments.Count(c => !c.IsDeleted),
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }
}