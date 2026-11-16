using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos;
using api.Dtos.Comments;
using api.Helpers;

namespace api.Interfaces
{
    public interface ICommentService
    {
        Task<Result<CommentResponseDto>> CreateCommentAsync(CreateCommentDto dto, Guid workspaceId, Guid projectId, Guid taskId, Guid userId);
        Task<Result<PagedResult<CommentResponseDto>>> GetAllCommentsAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId, CommentQueryObject query);
        Task<Result<CommentResponseDto>> GetCommentByIdAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid commentId, Guid userId);
        Task<Result<EditCommentResponseDto>> UpdateCommentAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid commentId, Guid userId, UpdateCommentDto dto);
        Task<Result<bool>> DeleteCommentAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid commentId, Guid userId);
    
    }
}