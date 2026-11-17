using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Tasks
{
    public class TaskLabelDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Colour { get; set; }
    }
}