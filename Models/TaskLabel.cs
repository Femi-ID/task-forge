using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class TaskLabel
    {
        public required string TaskId { get; set; }
        public required Task Task { get; set; }
        public required string LabelId { get; set; }
        public required Label Label { get; set; }
    }
}