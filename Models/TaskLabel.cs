using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class TaskLabel
    {
        public Guid TaskItemId { get; set; }
        public TaskItem TaskItem { get; set; } = null!;
        public Guid LabelId { get; set; }
        public Label Label { get; set; } = null!;
    }
}