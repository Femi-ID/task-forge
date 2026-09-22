using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class Label
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Colour { get; set; }
        public Guid WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;
        public List<TaskLabel> TaskLabels { get; set; } = new List<TaskLabel>();
    }
}