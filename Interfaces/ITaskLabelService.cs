using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos;
using api.Dtos.Labels;

namespace api.Interfaces
{
    public interface ITaskLabelService
    {
        Task<Result<List<LabelResponseDto>>> GetTaskLabelsAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId);
        Task<Result<LabelResponseDto>> AttachLabelAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid labelId, Guid userId);
        Task<Result<bool>> DetachLabelAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid labelId, Guid userId);

    }
}