using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Attachments
{
    public class AttachmentResponseDto
    {
        public Guid Id { get; set; }
        public required string OriginalFileName { get; set; }
        public required string ContentType { get; set; }
        public long SizeBytes { get; set; }
        public Guid UploadedByUserId { get; set; }
        public required string UploadedByFullName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}