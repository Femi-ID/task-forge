using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class Attachment
    {
        public Guid Id { get; set; }

        // Every attachment belongs to a workspace, for quota/authorization checks.
        // Direct FK because the service queries by WorkspaceId often; it's cheaper than always
        // joining through Project/Task/Comment to find out which workspace an attachment belongs to.
        public Guid WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;

        // Exactly one of the three below is set — enforced by a CHECK constraint in AppDbContext.
        public Guid? ProjectId { get; set; }
        public Project? Project { get; set; }

        public Guid? TaskItemId { get; set; }
        public TaskItem? TaskItem { get; set; }

        public Guid? CommentId { get; set; }
        public Comment? Comment { get; set; }

        // File metadata
        public required string OriginalFileName { get; set; }
        public required string ContentType { get; set; }
        public long SizeBytes { get; set; }
        public required string StorageKey { get; set; }

        public Guid UploadedByUserId { get; set; }
        public AppUser UploadedByUser { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}