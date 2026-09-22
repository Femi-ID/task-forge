using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class Comment : IUpdatable
    {
        public Guid Id { get; set; }
        public required string Content { get; set; }
        public Guid TaskItemId { get; set; }
        public TaskItem TaskItem { get; set; } = null!;
        public Guid AuthorId { get; set; }
        public AppUser Author { get; set; } = null!;

        // Self-reference for threading. NULL = means top-level comment.
        public Guid? ParentCommentId { get; set; }
        public Comment? ParentComment { get; set; }
        public List<Comment> Replies { get; set; } = [];
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}