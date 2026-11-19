using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos;
using api.Dtos.Attachments;
using api.Services;

namespace api.Interfaces
{
    public interface IAttachmentService
    {
        Task<Result<AttachmentResponseDto>> UploadAsync(Guid workspaceId, AttachmentOwnerType ownerType, Guid ownerId, Guid userId, IFormFile file, CancellationToken ct);
        Task<Result<Uri>> GetDownloadUrlAsync(Guid workspaceId, Guid attachmentId, Guid userId, CancellationToken ct);
        Task<Result<List<AttachmentResponseDto>>> ListAsync(Guid workspaceId, AttachmentOwnerType ownerType, Guid ownerId, Guid userId, CancellationToken ct);
        Task<Result<bool>> DeleteAsync(Guid workspaceId, Guid attachmentId, Guid userId, CancellationToken ct);
    }
}