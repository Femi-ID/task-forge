using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos;
using api.Dtos.Attachments;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Services
{
    public enum AttachmentOwnerType
    {
        Project,
        Task,
        Comment
    }

    public class AttachmentService(
        AppDbContext context,
        IWorkspaceAuthorization workspaceAuth,
        IFileStorage storage) : IAttachmentService
    {
        public async Task<Result<AttachmentResponseDto>> UploadAsync(
            Guid workspaceId, AttachmentOwnerType ownerType, Guid ownerId, Guid userId,
            IFormFile file, CancellationToken ct)
        {
            // any workspace member can upload.
            var isMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, userId);
            if (!isMember) return Result<AttachmentResponseDto>.Failure("Only workspace members can upload attachments.", ResultError.Forbidden);

            //  Validation
            if (file is null || file.Length == 0)
                return Result<AttachmentResponseDto>.Failure("No file uploaded.", ResultError.Validation);
            if (file.Length > GlobalConstants.StorageLimits.MaxFileSizeBytes)
                return Result<AttachmentResponseDto>.Failure(
                    $"File is too large. Max {GlobalConstants.StorageLimits.MaxFileSizeBytes / (1024 * 1024)} MB.", ResultError.Validation);
            if (!GlobalConstants.StorageLimits.AllowedContentTypes.Contains(file.ContentType ?? string.Empty))
                return Result<AttachmentResponseDto>.Failure($"File type '{file.ContentType}' is not allowed.", ResultError.Validation);

            // Verify the parent exists AND is in the claimed workspace. Without this, a user could upload
            // an attachment "to" a task that belongs to a different workspace entirely by mismatching the
            // route IDs.
            var parentValid = ownerType switch
            {
                AttachmentOwnerType.Project => await context.Projects.AnyAsync(p => p.Id == ownerId && p.WorkspaceId == workspaceId, ct),
                AttachmentOwnerType.Task => await context.Tasks.AnyAsync(t => t.Id == ownerId && t.Project.WorkspaceId == workspaceId, ct),
                AttachmentOwnerType.Comment => await context.Comments.AnyAsync(c => c.Id == ownerId && c.TaskItem.Project.WorkspaceId == workspaceId, ct),
                _ => false
            };
            if (!parentValid) return Result<AttachmentResponseDto>.Failure("Parent not found in this workspace.", ResultError.NotFound);

            // Build the storage key. A GUID guarantees uniqueness and is path-traversal-proof by construction.
            var storageKey = $"workspaces/{workspaceId}/{Guid.NewGuid()}";

            // Order of operations matters here. Upload the bytes FIRST, then commit the DB row.
            // If we did it the other way (DB first, S3 second) and S3 failed, we'd have an orphan row
            // pointing at a file that doesn't exist. This way, if the DB insert fails after S3 succeeded,
            // we have an orphan BLOB with no row — which the Hangfire sweep job can find and clean up
            // (unlike an orphan row pointing at nothing, which is harder to detect). Orphan BLOB = recoverable, orphan row = broken.

            // upload the bytes
            using (var stream = file.OpenReadStream()) // Opens the request stream for reading the uploaded file.
            {
                await storage.SaveAsync(storageKey, stream, file.ContentType!, ct);
            }

            // create new attachment instance
            var attachment = new Attachment
            {
                WorkspaceId = workspaceId,
                ProjectId = ownerType == AttachmentOwnerType.Project ? ownerId : null,
                TaskItemId = ownerType == AttachmentOwnerType.Task ? ownerId : null,
                CommentId = ownerType == AttachmentOwnerType.Comment ? ownerId : null,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType!,
                SizeBytes = file.Length,
                StorageKey = storageKey,
                UploadedByUserId = userId
            };

            try
            {
                await context.Attachments.AddAsync(attachment, ct);
                await context.SaveChangesAsync(ct);
            }
            catch
            {
                // Best-effort cleanup of the S3 blob if the DB insert failed. This can itself fail (network) 
                // that's what the Hangfire sweep job is for, as a backstop. But doing it inline
                // handles the common case quickly.
                try
                {
                    await storage.DeleteAsync(storageKey, CancellationToken.None);
                    context.Attachments.Remove(attachment);
                    await context.SaveChangesAsync(ct);
                }
                catch { /* sweep job will get it */ }
                throw;
            }

            // get the user's fullname as the uploaderName
            var uploaderFullName = await context.Users.Where(u => u.Id == userId).Select(u => u.FullName).FirstAsync(ct);
            return Result<AttachmentResponseDto>.Success(new AttachmentResponseDto
            {
                Id = attachment.Id,
                OriginalFileName = attachment.OriginalFileName,
                ContentType = attachment.ContentType,
                SizeBytes = attachment.SizeBytes,
                UploadedByUserId = attachment.UploadedByUserId,
                UploadedByFullName = uploaderFullName,
                CreatedAt = attachment.CreatedAt
            });
        }

        public async Task<Result<Uri>> GetDownloadUrlAsync(Guid workspaceId, Guid attachmentId, Guid userId, CancellationToken ct)
        {
            var isMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, userId);
            if (!isMember) return Result<Uri>.Failure("Only workspace members can download attachments.", ResultError.Forbidden);

            // Filter on BOTH ids
            var attachment = await context.Attachments.AsNoTracking()
                .Where(a => a.Id == attachmentId && a.WorkspaceId == workspaceId)
                .Select(a => new { a.StorageKey, a.OriginalFileName })
                .FirstOrDefaultAsync(ct);
            if (attachment is null) return Result<Uri>.Failure("Attachment not found", ResultError.NotFound);

            var url = await storage.GetPresignedDownloadURLAsync(
                attachment.StorageKey,
                attachment.OriginalFileName,
                TimeSpan.FromMinutes(GlobalConstants.StorageLimits.PreSignedURLValidMinutes),
                ct);
            return Result<Uri>.Success(url);
        }

        public async Task<Result<List<AttachmentResponseDto>>> ListAsync(
            Guid workspaceId, AttachmentOwnerType ownerType, Guid ownerId, Guid userId, CancellationToken ct)
        {
            var isMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, userId);
            if (!isMember) return Result<List<AttachmentResponseDto>>.Failure("Only workspace members can view attachments.", ResultError.Forbidden);

            var query = context.Attachments.AsNoTracking().Where(a => a.WorkspaceId == workspaceId);
            query = ownerType switch
            {
                AttachmentOwnerType.Project => query.Where(a => a.ProjectId == ownerId),
                AttachmentOwnerType.Task => query.Where(a => a.TaskItemId == ownerId),
                AttachmentOwnerType.Comment => query.Where(a => a.CommentId == ownerId),
                _ => query.Where(_ => false)
            };

            // Expression projection — reads a.UploadedByUser.FullName as a navigation, so EF needs to
            // translate the whole Select into SQL joins. Same pattern as CommentMapper.ToDto.
            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AttachmentResponseDto
                {
                    Id = a.Id,
                    OriginalFileName = a.OriginalFileName,
                    ContentType = a.ContentType,
                    SizeBytes = a.SizeBytes,
                    UploadedByUserId = a.UploadedByUserId,
                    UploadedByFullName = a.UploadedByUser.FullName,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync(ct);

            return Result<List<AttachmentResponseDto>>.Success(items);
        }

        public async Task<Result<bool>> DeleteAsync(Guid workspaceId, Guid attachmentId, Guid userId, CancellationToken ct)
        {
            var attachment = await context.Attachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.WorkspaceId == workspaceId, ct);
            if (attachment is null) return Result<bool>.Failure("Attachment not found", ResultError.NotFound);

            // Same authorization shape as Comment deletion: uploader OR workspace admin. An admin
            // needs moderation power; a user shouldn't be stuck with a file they uploaded by mistake.
            var isAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, userId);
            if (attachment.UploadedByUserId != userId && !isAdmin)
                return Result<bool>.Failure("Only the uploader or a workspace admin can delete this attachment.", ResultError.Forbidden);

            // Delete the DB row FIRST, then the blob. Opposite order from upload, for the same reason:
            // if the blob delete fails after the row delete succeeds, we've got an orphan blob (recoverable
            // via sweep), not an orphan row (which would make the UI show a "download" button for a file
            // that no longer exists on S3).
            context.Attachments.Remove(attachment);
            await context.SaveChangesAsync(ct);
            try { await storage.DeleteAsync(attachment.StorageKey, ct); }
            catch { /* hangfire sweep job */ }

            return Result<bool>.Success(true);
        }
    }
}