using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos;
using api.Dtos.Labels;

namespace api.Interfaces
{
    public interface ILabelService
    {
        Task<Result<LabelResponseDto>> CreateLabelAsync(CreateLabelDto dto, Guid workspaceId, Guid userId);
        Task<Result<List<LabelResponseDto>>> GetAllLabelsAsync(Guid workspaceId, Guid userId);
        Task<Result<LabelResponseDto>> UpdateLabelAsync(UpdateLabelDto dto, Guid workspaceId, Guid labelId, Guid userId);
        Task<Result<bool>> DeleteLabelAsync(Guid workspaceId, Guid labelId, Guid userId);
    }
}