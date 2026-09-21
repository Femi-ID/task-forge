using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class Comment
    {
        public Guid Id { get; set; }
        public required string Content { get; set; }
        public required string TaskId { get; set; }
        public required Task Task { get; set; }
        public required string AuthorId { get; set; }
        public required AppUser AppUser { get; set; }
        public required string ParentCommentId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; }
    }
}