using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Interfaces
{
    public interface IFileStorage
    {
        Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct);
        Task<Uri> GetPresignedDownloadURLAsync(string key, string originalFilename, TimeSpan validFor, CancellationToken ct);
        Task DeleteAsync(string key, CancellationToken ct);
    }
}