using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Interfaces
{
    public interface IWorkspaceAuthorization
    {
        Task<bool> IsWorkspaceAdminMemberAsync(Guid workspaceId, Guid userId);
    }
}