using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using api.Interfaces;

namespace api.Services
{
    public class S3FileStorage(IAmazonS3 s3, IConfiguration config) : IFileStorage
    {
        private readonly string bucket = config["Storage:BucketName"]
            ?? throw new InvalidOperationException("Storage:BucketName is not configured.");
        public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct)
        {
            // PutObjectRequest streams the content directly from our incoming HTTP request to S3.
            // No intermediate buffer, no load-whole-file-into-memory. The request body stream is consumed
            // byte-by-byte as the S3 SDK sends it on. This is what makes uploading a 10MB file not cost 10MB of
            // server RAM.
            var request = new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                DisablePayloadSigning = true // required for MinIO over plain HTTP; harmless on real S3 over HTTPS
            };
            await s3.PutObjectAsync(request, ct);
        }

        public Task<Uri> GetPresignedDownloadURLAsync(string key, string originalFileName, TimeSpan validFor, CancellationToken ct)
        {
            // GetPreSignedURL is synchronous in the SDK — no network call is made. It's pure cryptography:
            // it builds a URL with a signature derived from our AccessKey/SecretKey, embedded as query params.
            // The storage server checks that signature when the URL is actually used; it never phones home to us.
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucket,
                Key = key,
                Expires = DateTime.UtcNow.Add(validFor),
                Verb = HttpVerb.GET,
                // ResponseHeaderOverrides lets us dictate response headers for THIS one signed URL.
                // We override Content-Disposition so the browser saves the file with its original name,
                // not the opaque storage key — a user-friendly touch that costs nothing.
                ResponseHeaderOverrides = new ResponseHeaderOverrides
                {
                    ContentDisposition = $"attachment; filename=\"{Uri.EscapeDataString(originalFileName)}\""
                }
            };

            return Task.FromResult(new Uri(s3.GetPreSignedURL(request)));
        }

        public async Task DeleteAsync(string key, CancellationToken ct)
        {
            await s3.DeleteObjectAsync(new DeleteObjectRequest { BucketName = bucket, Key = key }, ct);
        }
    }
}