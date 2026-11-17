using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Labels;
using api.Models;

namespace api.Mappers
{
    public class LabelMapper
    {
        public static Label ToLabelModelFromCreateDto(CreateLabelDto dto, Guid workspaceId) => new()
        {
            Name = dto.Name.Trim(),
            Colour = dto.Colour.Trim(),
            WorkspaceId = workspaceId
        };
        public static LabelResponseDto ToDto(Label label) => new()
        {
            Id = label.Id,
            Name = label.Name,
            Colour = label.Colour,
            WorkspaceId = label.WorkspaceId
        };
    }
}