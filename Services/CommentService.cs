using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos;
using api.Dtos.Comments;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class CommentService(AppDbContext context, IWorkspaceAuthorization workspaceAuth) : ICommentService
    {
        public async Task<Result<CommentResponseDto>> CreateCommentAsync(CreateCommentDto dto, Guid workspaceId, Guid projectId, Guid taskId, Guid authorId)
        {
            // confirm the request is from a workspace member
            var isMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, authorId);
            if (!isMember) return Result<CommentResponseDto>.Failure("Only workspace members can comment.", ResultError.Forbidden);

            var taskInScope = await context.Tasks.AnyAsync(t =>
                t.Id == taskId && t.ProjectId == projectId && t.Project.WorkspaceId == workspaceId);
            if (!taskInScope) return Result<CommentResponseDto>.Failure("Task not found", ResultError.NotFound);

            // to confirm if the parent comment (if there's one) has a parent
            if (dto.ParentCommentId.HasValue)
            {
                var parent = await context.Comments.FirstOrDefaultAsync(c => c.Id == dto.ParentCommentId && c.TaskItemId == taskId);
                if (parent is null) return Result<CommentResponseDto>.Failure("Parent comment not found");
                if (parent.ParentCommentId.HasValue)
                    return Result<CommentResponseDto>.Failure(
                        "Replies are limited to one level — reply to the original comment instead.", ResultError.Validation);
            }

            // create and save new comment
            var commentModel = CommentMapper.ToCommentModelFromCreateCommentDto(dto, taskId, authorId);
            await context.AddAsync(commentModel);
            await context.SaveChangesAsync();

            var authorFullName = await context.Users.Where(u => u.Id == authorId).Select(u => u.FullName).FirstAsync();

            return Result<CommentResponseDto>.Success(new CommentResponseDto
            {
                Id = commentModel.Id,
                Content = commentModel.Content,
                TaskItemId = commentModel.TaskItemId,
                AuthorId = commentModel.AuthorId,
                AuthorFullName = authorFullName,
                ParentCommentId = commentModel.ParentCommentId,
                IsDeleted = false,
                IsEdited = false,
                CreatedAt = commentModel.CreatedAt,
                UpdatedAt = commentModel.UpdatedAt,
                Replies = [] // brand new comment, cannot have replies yet
            });
        }

        public async Task<Result<bool>> DeleteCommentAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid commentId, Guid userId)
        {
            var comment = await context.Comments.FirstOrDefaultAsync(c =>
                c.Id == commentId && c.TaskItemId == taskId && c.TaskItem.ProjectId == projectId && c.TaskItem.Project.WorkspaceId == workspaceId);
            if (comment is null) return Result<bool>.Failure("Comment not found", ResultError.NotFound);

            // only the author or an admin can delete a comment
            var isAdmin = await workspaceAuth.IsWorkspaceAdminMemberAsync(workspaceId, userId);
            if (comment.AuthorId != userId && !isAdmin)
                return Result<bool>.Failure("Only the author or a workspace admin can delete this comment.", ResultError.Forbidden);

            // this checks if the comment is a parent to other comments
            var hasReplies = await context.Comments.AnyAsync(c => c.ParentCommentId == commentId);
            if (hasReplies)
            {
                comment.IsDeleted = true;
                comment.Content = string.Empty;
                await context.SaveChangesAsync();
            }
            else
            {
                context.Comments.Remove(comment);
                await context.SaveChangesAsync();
            }

            return Result<bool>.Success(true);
        }

        public async Task<Result<PagedResult<CommentResponseDto>>> GetAllCommentsAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId, CommentQueryObject query)
        {
            var isMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, userId);
            if (!isMember) return Result<PagedResult<CommentResponseDto>>.Failure("Only workspace members can view comments.", ResultError.Forbidden);

            // only top-level comments are paged; each carries its own replies inline (capped at one level)
            var comments = context.Comments.Where(c =>
                c.TaskItemId == taskId && c.TaskItem.ProjectId == projectId && c.TaskItem.Project.WorkspaceId == workspaceId &&
                c.ParentCommentId == null); // this does not this filter the comments that are soft-deleted, this is intentional

            var totalCount = await comments.CountAsync();

            comments = query.IsDescending ? comments.OrderByDescending(c => c.CreatedAt) : comments.OrderBy(c => c.CreatedAt);

            var (page, size) = PaginationHelper.Normalize(query.PageNumber, query.PageSize);
            var items = await comments.Skip((page - 1) * size).Take(size).Select(CommentMapper.ToDto).ToListAsync();

            return Result<PagedResult<CommentResponseDto>>.Success(new PagedResult<CommentResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = size
            });
        }

        public async Task<Result<CommentResponseDto>> GetCommentByIdAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid commentId, Guid userId)
        {
            var isMember = await workspaceAuth.IsWorkspaceMemberAsync(workspaceId, userId);
            if (!isMember) return Result<CommentResponseDto>.Failure("Only workspace members can view comments.", ResultError.Forbidden);

            var commentDto = await context.Comments.AsNoTracking()
                .Where(c => c.Id == commentId && c.TaskItemId == taskId && c.TaskItem.ProjectId == projectId && c.TaskItem.Project.WorkspaceId == workspaceId)
                .Select(CommentMapper.ToDto)
                .FirstOrDefaultAsync();
            
            if (commentDto is null) return Result<CommentResponseDto>.Failure("Comment not found", ResultError.NotFound);
            return Result<CommentResponseDto>.Success(commentDto);
        }

        public async Task<Result<EditCommentResponseDto>> UpdateCommentAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid commentId, Guid userId, UpdateCommentDto dto)
        {
            var comment = await context.Comments.FirstOrDefaultAsync(c =>
                c.Id == commentId && c.TaskItemId == taskId && c.TaskItem.ProjectId == projectId && c.TaskItem.Project.WorkspaceId == workspaceId);
            if (comment is null) return Result<EditCommentResponseDto>.Failure("Comment not found", ResultError.NotFound);

            if (comment.AuthorId != userId)
                return Result<EditCommentResponseDto>.Failure("Only the author can edit this comment", ResultError.Forbidden);
            if (comment.IsDeleted)
                return Result<EditCommentResponseDto>.Failure("Cannot edit a deleted comment.", ResultError.Conflict);

            comment.Content = dto.Content.Trim();
            await context.SaveChangesAsync(); // StampUpdatedAt sets UpdatedAt

            return Result<EditCommentResponseDto>.Success(new EditCommentResponseDto
            {
                Id = comment.Id, Content = comment.Content, UpdatedAt = comment.UpdatedAt
            });
        }
    }
}