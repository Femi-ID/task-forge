using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using api.Dtos.Comments;
using api.Models;

namespace api.Mappers
{
    public class CommentMapper
    {
        public static Comment ToCommentModelFromCreateCommentDto(CreateCommentDto dto, Guid taskId, Guid authorId)
        {
            return new Comment
            {
                Content = dto.Content.Trim(),
                TaskItemId = taskId,
                AuthorId = authorId,
                ParentCommentId = dto.ParentCommentId
            };
        }

        // Expression: reads c.Author.FullName and c.Replies, both navigations EF must translate into the SQL
        // joins itself. Inlined one level deep instead than self-referencing.
        public static readonly Expression<Func<Comment, CommentResponseDto>> ToDto = c => new CommentResponseDto
        {
            Id = c.Id,
            Content = c.IsDeleted ? "[deleted]" : c.Content,
            TaskItemId = c.TaskItemId,
            AuthorId = c.AuthorId,
            AuthorFullName = c.Author.FullName,
            ParentCommentId = c.ParentCommentId,
            IsDeleted = c.IsDeleted,
            IsEdited = c.UpdatedAt.HasValue,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            Replies = c.Replies
                .OrderBy(r => r.CreatedAt)
                .Select(r => new CommentResponseDto
                {
                    Id = c.Id,
                    Content = c.IsDeleted ? "[deleted]" : c.Content,
                    TaskItemId = c.TaskItemId,
                    AuthorId = c.AuthorId,
                    AuthorFullName = c.Author.FullName,
                    ParentCommentId = c.ParentCommentId,
                    IsDeleted = c.IsDeleted,
                    IsEdited = c.UpdatedAt.HasValue,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    Replies = new List<CommentResponseDto>()
                }).ToList()
        };
    }
}