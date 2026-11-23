using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos;
using api.Dtos.Workspace;
using api.Enums;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class WorkspaceService(AppDbContext context, IFileStorage storage) : IWorkspace, IWorkspaceAuthorization
    {
        private const string NameTakenMessage = "You already have a workspace with this name";
        public async Task<Result<WorkspaceDto>> CreateWorkspaceAsync(CreateWorkspaceDto dto, Guid userId)
        {
            // confirm if this workspace name doesn't exist for the user
            if (await WorkspaceNameTakenAsync(userId, dto.Name.Trim()))
                return Result<WorkspaceDto>.Failure(NameTakenMessage, ResultError.Conflict);

            var workspaceModel = WorkspaceMappers.ToWorkspaceModelFromCreateDto(dto, userId);
            await context.Workspaces.AddAsync(workspaceModel);

            // create workspaceMember instance and save
            var workspaceMemberModel = WorkspaceMemberMapper.ToWorkspaceMemberModelFromCreateDto(userId, workspaceModel.Id, WorkspaceRole.Admin);
            await context.WorkspaceMembers.AddAsync(workspaceMemberModel);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
            {
                return Result<WorkspaceDto>.Failure(NameTakenMessage, ResultError.Conflict);
            }

            var workspaceDto = await context.Workspaces.AsNoTracking()
                .Where(w => w.Id == workspaceModel.Id)
                .Select(WorkspaceMappers.ToWorkspaceDto)
                .FirstAsync();
            return Result<WorkspaceDto>.Success(workspaceDto);
        }

        public async Task<Result<bool>> DeleteWorkspaceAsync(Guid id, Guid userId)
        {
            var workspaceModel = await context.Workspaces.FirstOrDefaultAsync(w => w.Id == id);
            if (workspaceModel is null) return Result<bool>.Failure("Workspace does not exist", ResultError.NotFound);

            // To confirm that the user has an admin role in the workspace they want to delete
            var isAdmin = await IsWorkspaceAdminMemberAsync(id, userId);
            if (!isAdmin) return Result<bool>.Failure("You have no permission for this request", ResultError.Forbidden);

            // Projects are only ever soft-deleted (Archived), so "has any project" would block deletion forever.
            // Only live projects block it; archived ones are removed with the workspace by the database cascade.
            var hasActiveProjects = await context.Projects.AnyAsync(p => p.WorkspaceId == id && p.Status != ProjectStatus.Archived);
            if (hasActiveProjects)
                return Result<bool>.Failure("Archive all of this workspace's projects before deleting it", ResultError.Validation);

            // Confirms that the user is the owner of the project
            if (workspaceModel.OwnerId == userId)
            {
                context.Workspaces.Remove(workspaceModel);
                await context.SaveChangesAsync();
                return Result<bool>.Success(true);
            }
            return Result<bool>.Failure("Only the workspace owner can delete it. Ask the owner to transfer ownership to you first", ResultError.Validation);
        }

        public async Task<Result<PagedResult<WorkspaceDto>>> GetAllWorkspacesAsync(WorkspaceQueryObject query, Guid userId)
        {
            var workspaces = context.Workspaces.AsNoTracking()
                .Where(w => w.WorkspaceMembers.Any(m => m.AppUserId == userId))
                // .Include(w => w.Projects)
                // .Include(w => w.Labels)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(query.Name))
            {
                foreach (var term in query.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var loweredTerm = term.ToLower();
                    workspaces = workspaces.Where(w => w.Name.ToLower().Contains(loweredTerm));
                }
            }
            if (query.BeforeCreatedAt.HasValue)
            {
                workspaces = workspaces.Where(w => w.CreatedAt <= query.BeforeCreatedAt);
            }
            if (query.AfterCreatedAt.HasValue)
            {
                workspaces = workspaces.Where(w => w.CreatedAt >= query.AfterCreatedAt);
            }
            if (query.BeforeUpdatedAt.HasValue)
            {
                workspaces = workspaces.Where(w => w.UpdatedAt <= query.BeforeUpdatedAt);
            }
            if (query.AfterUpdatedAt.HasValue)
            {
                workspaces = workspaces.Where(w => w.UpdatedAt >= query.AfterUpdatedAt);
            }
            if (query.ExactDate.HasValue)
            {
                var day = query.ExactDate.Value.Date;
                workspaces = workspaces.Where(w => w.CreatedAt >= day && w.CreatedAt < day.AddDays(1));
            }

            var totalCount = await workspaces.CountAsync();

            // sorting
            IOrderedQueryable<Workspace> ordered = (query.SortBy ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "name" => query.IsDescending ? workspaces.OrderByDescending(w => w.Name) : workspaces.OrderBy(w => w.Name),
                "updatedat" => query.IsDescending ? workspaces.OrderByDescending(w => w.UpdatedAt) : workspaces.OrderBy(w => w.UpdatedAt),
                "createdat" => query.IsDescending ? workspaces.OrderByDescending(w => w.CreatedAt) : workspaces.OrderBy(w => w.CreatedAt),
                _ => workspaces.OrderByDescending(w => w.CreatedAt)
            };
            var orderedWorkspaces = ordered.ThenBy(w => w.Id);

            //  sorting
            // if (!string.IsNullOrWhiteSpace(query.SortBy))
            // {
            //     query.SortBy = query.SortBy.Trim().ToLowerInvariant();
            //     if (query.SortBy.Equals("name"))
            //         workspaces = query.IsDescending ? workspaces.OrderByDescending(x => x.Name) : workspaces.OrderBy(x => x.Name);
            //     if (query.SortBy.Equals("updatedat"))
            //         workspaces = query.IsDescending ? workspaces.OrderByDescending(x => x.UpdatedAt) : workspaces.OrderBy(x => x.UpdatedAt);
            //     if (query.SortBy.Equals("createdat"))
            //         workspaces = query.IsDescending ? workspaces.OrderByDescending(x => x.CreatedAt) : workspaces.OrderBy(x => x.CreatedAt);
            // }
            // workspaces.OrderByDescending(w => w.Id); // tie-breaker: without it, two rows with the same CreatedAt can swap pages

            var (page, size) = PaginationHelper.Normalize(query.PageNumber, query.PageSize);
            var items = await orderedWorkspaces
                .Skip((page - 1) * size)
                .Take(size)
                .Select(w => new WorkspaceDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    Description = w.Description,
                    OwnerId = w.OwnerId,
                    OwnerFullName = w.Owner.FullName,
                    CreatedAt = w.CreatedAt,
                    UpdatedAt = w.UpdatedAt,
                })
                .ToListAsync();

            return Result<PagedResult<WorkspaceDto>>.Success(new PagedResult<WorkspaceDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = size
            });
            // return Result<List<Workspace>>.Success(await workspaces.Skip(skipNumber).Take(query.PageSize).ToListAsync());
        }

        public async Task<Result<WorkspaceDetailDto>> GetWorkspaceByIdAsync(Guid workspaceId, Guid userId)
        {
            var workspaceDetail = await context.Workspaces
                .Where(w => w.Id == workspaceId && w.WorkspaceMembers.Any(m => m.AppUserId == userId))
                .Select(WorkspaceMappers.ToWorkspaceDetailDto)
                .AsSplitQuery()
                .FirstOrDefaultAsync();
            if (workspaceDetail is null) return Result<WorkspaceDetailDto>.Failure("Workspace does not exist", ResultError.NotFound);

            return Result<WorkspaceDetailDto>.Success(workspaceDetail);
        }

        public async Task<Result<WorkspaceDto>> UpdateWorkspaceAsync(Guid id, UpdateWorkspaceDto updateDto, Guid userId)
        {
            var existingWorkspaceModel = await GetTrackedWorkspaceAsync(id);
            if (existingWorkspaceModel is null) return Result<WorkspaceDto>.Failure("Workspace does not exist", ResultError.NotFound);

            // To confirm first that the user is the owner or an admin member of the workspace
            var isAdmin = await IsWorkspaceAdminMemberAsync(id, userId);
            if (!isAdmin) return Result<WorkspaceDto>.Failure("You are not permitted to make this request", ResultError.Forbidden);

            if (!string.IsNullOrWhiteSpace(updateDto.Name?.Trim()))
            {
                var newName = updateDto.Name.Trim();
                // names are unique per OWNER (not per requesting admin), and renaming to your own current name is fine
                if (await WorkspaceNameTakenAsync(existingWorkspaceModel.OwnerId, newName, excludeWorkspaceId: id))
                    return Result<WorkspaceDto>.Failure("The owner already has a workspace with this name", ResultError.Conflict);
                existingWorkspaceModel.Name = newName;
            }
            if (updateDto.Description is not null)
                existingWorkspaceModel.Description = updateDto.Description.Length == 0 ? null : updateDto.Description;

            try
            {
                await context.SaveChangesAsync(); // AppDbContext stamps UpdatedAt
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
            {
                return Result<WorkspaceDto>.Failure("The owner already has a workspace with this name", ResultError.Conflict);
            }

            var workspaceDto = await context.Workspaces.AsNoTracking()
                .Where(w => w.Id == existingWorkspaceModel.Id)
                .Include(w => w.Owner.FullName)
                .Select(WorkspaceMappers.ToWorkspaceDto)
                .FirstAsync();
            return Result<WorkspaceDto>.Success(workspaceDto);
        }

        public async Task<Workspace?> GetTrackedWorkspaceAsync(Guid workspaceId)
        {
            var workspaceModel = await context.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId);
            if (workspaceModel is null) return null;
            return workspaceModel;
        }

        public async Task<bool> IsWorkspaceAdminMemberAsync(Guid workspaceId, Guid userId)
        {
            var isAdminMember = await context.WorkspaceMembers.AnyAsync(m =>
                m.WorkspaceId == workspaceId && m.AppUserId == userId && m.Role == WorkspaceRole.Admin);
            if (!isAdminMember) return false;
            return true;
        }

        public async Task<Result<bool>> TransferWorkspaceOwnershipAsync(Guid workspaceId, Guid currentOwnerId, Guid newOwnerId)
        {
            //  get workspace
            var workspace = await GetTrackedWorkspaceAsync(workspaceId);
            if (workspace is null) return Result<bool>.Failure("Workspace does not exist", ResultError.NotFound);

            // confirm the requesting user is the workspace owner
            if (workspace.OwnerId != currentOwnerId)
                return Result<bool>.Failure("Only the current owner can transfer ownership", ResultError.Forbidden);

            // confirm new owner is already a member
            var newOwnerMember = await context.WorkspaceMembers.FirstOrDefaultAsync(m =>
                m.WorkspaceId == workspaceId && m.AppUserId == newOwnerId);
            if (newOwnerMember is null)
                return Result<bool>.Failure("The new owner must already be a member of this workspace", ResultError.Validation);

            if (await WorkspaceNameTakenAsync(newOwnerId, workspace.Name, excludeWorkspaceId: workspaceId))
                return Result<bool>.Failure("The new owner already has a workspace with this name. Rename one of them first", ResultError.Conflict);

            newOwnerMember.Role = WorkspaceRole.Admin;
            workspace.OwnerId = newOwnerId;
            await context.SaveChangesAsync();
            return Result<bool>.Success(true);
        }

        public async Task<bool> IsWorkspaceMemberAsync(Guid workspaceId, Guid userId)
        {
            return await context.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == workspaceId && m.AppUserId == userId);
        }

        private Task<bool> WorkspaceNameTakenAsync(Guid ownerId, string name, Guid excludeWorkspaceId = default)
        {
            var loweredName = name.ToLower();
            return context.Workspaces.AnyAsync(w =>
                w.OwnerId == ownerId && w.Id != excludeWorkspaceId && w.Name.ToLower() == loweredName);
        }

        public async Task<Result<Uri>> GetWorkspaceLogoUrlAsync(Guid workspaceId, Guid userId, CancellationToken ct)
        {
            var isMember = await IsWorkspaceMemberAsync(workspaceId, userId);
            if (!isMember) return Result<Uri>.Failure("Only workspace members can download attachments.", ResultError.Forbidden);

            var workspace = await context.Workspaces
                .Where(w => w.Id == workspaceId && w.LogoStorageKey != null)
                .Select(w => new { w.LogoStorageKey, w.OriginalFileName })
                .FirstOrDefaultAsync(ct);
            if (workspace is null) return Result<Uri>.Failure("Workspace not found", ResultError.NotFound);
            if (workspace.LogoStorageKey is null) return Result<Uri>.Failure("No logo found for this workspace", ResultError.Validation);

            // var workspaceLogoUrl = await context.Attachments.AsNoTracking()
            //     .Where(a => a.WorkspaceId == workspaceId && a.StorageKey == workspace.LogoStorageKey)
            //     .Select(a => new { a.StorageKey, a.OriginalFileName })
            //     .FirstOrDefaultAsync(ct); // or should i use firstAsync();
            // if (workspaceLogoUrl is null) return Result<Uri>.Failure("WorkspaceLogo attachment not found", ResultError.NotFound);

            var url = await storage.GetPresignedDownloadURLAsync(
                workspace.LogoStorageKey,
                workspace.OriginalFileName!,
                TimeSpan.FromMinutes(GlobalConstants.StorageLimits.PreSignedURLValidMinutes),
                ct);

            return Result<Uri>.Success(url);
        }
        public async Task<Result<string>> SetWorkspaceLogoAsync(Guid workspaceId, Guid userId, IFormFile file, CancellationToken ct)
        {
            var isAdminMember = await IsWorkspaceAdminMemberAsync(workspaceId, userId);
            if (!isAdminMember) return Result<string>.Failure("Only workspace admin can set its logo.", ResultError.Forbidden);

            var workspace = await context.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId);
            if (workspace is null) return Result<string>.Failure("Workspace not found", ResultError.NotFound);

            //  Validation
            if (file is null || file.Length == 0)
                return Result<string>.Failure("No file uploaded.", ResultError.Validation);
            if (file.Length > GlobalConstants.StorageLimits.MaxWorkspaceLogoFileSizeBytes)
                return Result<string>.Failure(
                    $"File is too large. Max {GlobalConstants.StorageLimits.MaxWorkspaceLogoFileSizeBytes / (1024 * 1024)} MB.", ResultError.Validation);
            if (!GlobalConstants.StorageLimits.AllowedWorkspaceLogoContentTypes.Contains(file.ContentType ?? string.Empty))
                return Result<string>.Failure($"File type '{file.ContentType}' is not allowed.", ResultError.Validation);

            // Build the storage key. A GUID guarantees uniqueness and is path-traversal-proof by construction.
            var storageKey = $"workspaces/{workspaceId}/logo";

            // upload the image bytes
            using (var stream = file.OpenReadStream())
            {
                await storage.SaveAsync(storageKey, stream, file.ContentType!, ct);
            }

            // create new attachment instance
            // var attachment = new Attachment
            // {
            //     WorkspaceId = workspaceId,
            //     OriginalFileName = file.FileName,
            //     ContentType = file.ContentType!,
            //     SizeBytes = file.Length,
            //     StorageKey = storageKey,
            //     UploadedByUserId = userId
            // };
            // await context.Attachments.AddAsync(attachment, ct);

            try
            {
                // assign the attachment to the workspace instance
                workspace.LogoStorageKey = storageKey;
                workspace.OriginalFileName = file.FileName;
                await context.SaveChangesAsync(ct);
            }
            catch
            {
                try
                {
                    await storage.DeleteAsync(storageKey, CancellationToken.None);
                    workspace.LogoStorageKey = null;
                    await context.SaveChangesAsync(ct);
                }
                catch { /* sweep job will get it */ }
                Result<string>.Failure("Could not successfully set the workspace logo.", ResultError.None);
            }

            var url = await storage.GetPresignedDownloadURLAsync(
                storageKey,
                file.FileName,
                TimeSpan.FromMinutes(GlobalConstants.StorageLimits.PreSignedURLValidMinutes),
                ct);
            return Result<string>.Success(url.ToString());

            // SetLogoAsync writes to an identifiable key like "workspaces/{id}/logo" — same key every time, 
            // so uploading a new logo overwrites the old one in S3 automatically (no sweep job needed for logos). 
            // Same IFileStorage interface, same validation (restrict to image/png, image/jpeg, image/webp only, 
            // smaller size cap — 2MB is plenty for a logo). Admin-only, since it affects how the workspace appears to everyone in it.
        }
        public async Task<Result<bool>> DeleteWorkspaceLogoAsync(Guid workspaceId, Guid userId, CancellationToken ct)
        {
            var isAdminMember = await IsWorkspaceAdminMemberAsync(workspaceId, userId);
            if (!isAdminMember) return Result<bool>.Failure("Only workspace admin can remove its logo.", ResultError.Forbidden);

            var workspace = await context.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId);
            if (workspace is null) return Result<bool>.Failure("Workspace not found", ResultError.NotFound);
            if (workspace.LogoStorageKey is null) return Result<bool>.Failure("No logo found for this workspace", ResultError.Validation);

            // Delete the DB row FIRST, then the blob. Opposite order from upload
            var storageKey = workspace.LogoStorageKey;
            workspace.LogoStorageKey = null;
            await context.SaveChangesAsync(ct);
            
            try { await storage.DeleteAsync(storageKey, ct); }
            catch { /* hangfire sweep job */ }

            return Result<bool>.Success(true);
        }
    }
}