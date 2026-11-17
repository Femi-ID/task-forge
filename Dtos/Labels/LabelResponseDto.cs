using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Labels
{
    public class LabelResponseDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Colour { get; set; }
        public Guid WorkspaceId { get; set; }
    }
}