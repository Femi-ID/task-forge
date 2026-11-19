using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api
{
    public class GlobalConstants
    {
        public const string ApiVersion1 = "api/v1/";


        public static class StorageLimits
        {
            public const long MaxFileSizeBytes = 10 * 1024 * 1024; //10MB per file
            public const long MaxWorkspaceLogoFileSizeBytes = 2 * 1024 * 1024; //2MB per file
            public const int PreSignedURLValidMinutes = 5;

            // Allow-list of content types to accept. Anything not on this list is rejected at upload time.
            // Deliberately conservative — opt-in rather than block-list.
            public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
            {
                "image/png", "image/jpeg", "image/webp", "image/gif",
                "application/pdf",
                "text/plain", "text/markdown", "text/csv",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // .docx
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",       // .xlsx
                "application/vnd.openxmlformats-officedocument.presentationml.presentation"// .pptx
            };
            // so no executables(html/js, svg) as they can carry cause possible attacks

            public static readonly HashSet<string> AllowedWorkspaceLogoContentTypes = new(StringComparer.OrdinalIgnoreCase)
            {
                "image/png", 
                "image/jpeg", 
                "image/webp",
            };
        }
    }
}