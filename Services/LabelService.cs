using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos;
using api.Dtos.Labels;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class LabelService(AppDbContext context, IWorkspaceAuthorization workspaceAuth) : ILabelService
    {
        public async Task<Result<LabelResponseDto>> CreateLabelAsync(CreateLabelDto dto, Guid workspaceId, Guid userId)
        {
            var isAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, userId);
            if (!isAdmin) return Result<LabelResponseDto>.Failure("Only workspace admins can create labels.", ResultError.Forbidden);

            var label = LabelMapper.ToLabelModelFromCreateDto(dto, workspaceId);
            await context.Labels.AddAsync(label);
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
            {
                return Result<LabelResponseDto>.Failure("A label with this name already exists in this workspace.", ResultError.Conflict);
            }

            return Result<LabelResponseDto>.Success(LabelMapper.ToDto(label));
        }

        public async Task<Result<bool>> DeleteLabelAsync(Guid workspaceId, Guid labelId, Guid userId)
        {
            var isAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, userId);
            if (!isAdmin) return Result<bool>.Failure("Only workspace admins can delete labels.", ResultError.Forbidden);

            var label = await context.Labels.FirstOrDefaultAsync(l => l.Id == labelId && l.WorkspaceId == workspaceId);
            if (label is null) return Result<bool>.Failure("Label not found", ResultError.NotFound);

            // The TaskLabel->Label FK is NoAction (see AppDbContext comment), so the database itself won't
            // cascade this. EF's own SaveChanges still orders deletes correctly within one transaction once
            // both removals are tracked — NoAction only means the DATABASE won't do it silently; we still can,
            // explicitly, in one atomic call.
            // so what you're basically saying is deleting from the tasklabel many-to-many table instantly deletes the label from the label model itself 
            // and from other task instances that have this label as one their labels in their list
            var attachedLabels = context.TaskLabels.Where(tl => tl.LabelId == labelId);
            context.TaskLabels.RemoveRange(attachedLabels);
            context.Labels.Remove(label);
            await context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }

        public async Task<Result<List<LabelResponseDto>>> GetAllLabelsAsync(Guid workspaceId, Guid userId)
        {
            var isMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, userId);
            if (!isMember) return Result<List<LabelResponseDto>>.Failure("Only workspace members can view labels.", ResultError.Forbidden);

            var labelsDto = await context.Labels.AsNoTracking()
                .Where(l => l.WorkspaceId == workspaceId)
                .OrderBy(l => l.Name)
                .Select(l => new LabelResponseDto { Id = l.Id, Name = l.Name, Colour = l.Colour, WorkspaceId = l.WorkspaceId })
                .ToListAsync();

            return Result<List<LabelResponseDto>>.Success(labelsDto);
        }

        public async Task<Result<LabelResponseDto>> UpdateLabelAsync(UpdateLabelDto dto, Guid workspaceId, Guid labelId, Guid userId)
        {
            var isAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, userId);
            if (!isAdmin) return Result<LabelResponseDto>.Failure("Only workspace admins can delete labels.", ResultError.Forbidden);

            var label = await context.Labels.FirstOrDefaultAsync(l => l.Id == labelId && l.WorkspaceId == workspaceId);
            if (label is null) return Result<LabelResponseDto>.Failure("Label not found", ResultError.NotFound);

            if (!string.IsNullOrWhiteSpace(dto.Name)) label.Name = dto.Name.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Colour)) label.Colour = dto.Colour.Trim();

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
            {
                return Result<LabelResponseDto>.Failure("A label with this name already exists in this workspace.", ResultError.Conflict);
            }

            return Result<LabelResponseDto>.Success(LabelMapper.ToDto(label));
        }
    }
}