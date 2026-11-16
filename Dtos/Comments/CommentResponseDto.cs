using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Comments
{
    public class CommentResponseDto
    {
        public Guid Id { get; set; }
        public required string Content { get; set; }
        public Guid TaskItemId { get; set; }
        public Guid AuthorId { get; set; }
        public required string AuthorFullName { get; set; }
        public Guid? ParentCommentId { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsEdited { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<CommentResponseDto> Replies { get; set; } = [];
    }

    public class EditCommentResponseDto
    {
        public Guid Id { get; set; }
        public required string Content { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}