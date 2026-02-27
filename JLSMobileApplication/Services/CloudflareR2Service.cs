using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using JLSApplicationBackend.Heplers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JLSApplicationBackend.Services
{
    public class CloudflareR2Service : ICloudflareR2Service
    {
        private readonly ILogger<CloudflareR2Service> _logger;
        private readonly AppSettings _appSettings;
        private readonly AmazonS3Client _s3Client;

        public CloudflareR2Service(ILogger<CloudflareR2Service> logger, IOptions<AppSettings> options)
        {
            _logger = logger;
            _appSettings = options.Value;

            if (string.IsNullOrEmpty(_appSettings.CloudflareR2AccountId) ||
                string.IsNullOrEmpty(_appSettings.CloudflareR2AccessKey) ||
                string.IsNullOrEmpty(_appSettings.CloudflareR2SecretKey))
            {
                _logger.LogWarning("Cloudflare R2 credentials are not configured.");
            }
            else
            {
                var s3Config = new AmazonS3Config
                {
                    ServiceURL = $"https://{_appSettings.CloudflareR2AccountId}.r2.cloudflarestorage.com",
                };

                _s3Client = new AmazonS3Client(_appSettings.CloudflareR2AccessKey, _appSettings.CloudflareR2SecretKey, s3Config);
            }
        }

        public async Task<(string FileName, MemoryStream FileStream)> DownloadFileAsync(string fileId, string folder = null)
        {
            try
            {
                // In S3 API, "fileId" is actually the "Key". If it doesn't already have the folder prefix, we might add it.
                string key = fileId;
                if (!string.IsNullOrEmpty(folder) && !key.StartsWith($"{folder}/"))
                {
                    key = $"{folder}/{fileId}";
                }

                var request = new GetObjectRequest
                {
                    BucketName = _appSettings.CloudflareR2BucketName,
                    Key = key
                };

                using var response = await _s3Client.GetObjectAsync(request);
                var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Return the filename extracted from the key
                var outFileName = Path.GetFileName(key);
                return (outFileName, memoryStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download file {FileId} from Cloudflare R2.", fileId);
                throw;
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string mimeType, string folder = "Exports")
        {
            try
            {
                string key = $"{folder}/{fileName}";

                var request = new PutObjectRequest
                {
                    BucketName = _appSettings.CloudflareR2BucketName,
                    Key = key,
                    InputStream = fileStream,
                    ContentType = mimeType,
                    DisablePayloadSigning = true // Recommended for performance with R2
                };

                var response = await _s3Client.PutObjectAsync(request);
                
                if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"Upload failed with status code: {response.HttpStatusCode}");
                }

                return key; // We use the key (path) as the "file ID"
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file {FileName} to Cloudflare R2 in folder {Folder}.", fileName, folder);
                throw;
            }
        }

        public async Task<(string FileName, MemoryStream FileStream)> DownloadFileByNameAsync(string fileName, string folder = "Exports")
        {
            // In Cloudflare R2 / S3, the folder/fileName is the exact key. 
            // We just construct the key and call the generic download method.
            string key = $"{folder}/{fileName}";
            return await DownloadFileAsync(key);
        }

        public async Task DeleteFileAsync(string fileId, string folder = null)
        {
            try
            {
                string key = fileId;
                if (!string.IsNullOrEmpty(folder) && !key.StartsWith($"{folder}/"))
                {
                    key = $"{folder}/{fileId}";
                }

                var request = new DeleteObjectRequest
                {
                    BucketName = _appSettings.CloudflareR2BucketName,
                    Key = key
                };

                await _s3Client.DeleteObjectAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {FileId} from Cloudflare R2.", fileId);
                throw;
            }
        }
    }
}
