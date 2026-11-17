using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class ActivityLog
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!; // the only real FK — Cascade: log is meaningless once the workspace is gone
        public Guid ActorUserId { get; set; }
        public required string ActorFullName { get; set; } // snapshot at write time — survives the user being renamed or removed
        public required string Action { get; set; }         // "task.created" — free-form string, not an enum (see below)
        public required string Description { get; set; }    // pre-rendered, human-readable, frozen at write time
        public Guid? TaskItemId { get; set; }                // deep-link hint only — no FK, survives the task being deleted
        public Guid? ProjectId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}