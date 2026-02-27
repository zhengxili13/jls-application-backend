using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;

namespace JLSApplicationBackend.Services
{
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly ILogger<GoogleDriveService> _logger;
        private readonly DriveService _driveService;

        // Assuming you have configured the relative or absolute path of the json key
        // in appsettings.json or environment variables, or it can be injected.
        private const string CredentialsFilePath = "google-credentials.json";

        public GoogleDriveService(ILogger<GoogleDriveService> logger)
        {
            _logger = logger;
            _driveService = InitializeDriveService();
        }

        private DriveService InitializeDriveService()
        {
            try
            {
                GoogleCredential credential;

                using (var stream = new FileStream(CredentialsFilePath, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream)
                        .CreateScoped(DriveService.ScopeConstants.Drive); // Full access required for uploads
                }

                var service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "JLSApplicationBackend"
                });

                return service;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Google Drive Service. Please check if {CredentialsFilePath} exists and is valid.", CredentialsFilePath);
                throw;
            }
        }

        public async Task<(string FileName, MemoryStream FileStream)> DownloadFileAsync(string fileId)
        {
            try
            {
                var request = _driveService.Files.Get(fileId);
                
                // To get the file name, we need to specify the fields to return
                request.Fields = "name";
                var fileMetadata = await request.ExecuteAsync();
                string fileName = fileMetadata.Name;

                var memoryStream = new MemoryStream();
                
                // Execute download
                request = _driveService.Files.Get(fileId);
                await request.DownloadAsync(memoryStream);
                
                // Reset stream position to the beginning for subsequent reading
                memoryStream.Position = 0;

                return (fileName, memoryStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download file with ID {FileId} from Google Drive.", fileId);
                throw;
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string mimeType, string parentFolderId = null)
        {
            try
            {
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = fileName
                };

                if (!string.IsNullOrEmpty(parentFolderId))
                {
                    fileMetadata.Parents = new List<string> { parentFolderId };
                }

                var request = _driveService.Files.Create(fileMetadata, fileStream, mimeType);
                request.Fields = "id";

                var response = await request.UploadAsync();

                if (response.Status != Google.Apis.Upload.UploadStatus.Completed)
                {
                    throw new Exception($"Upload failed: {response.Exception?.Message}");
                }

                var file = request.ResponseBody;
                return file.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file {FileName} to Google Drive.", fileName);
                throw;
            }
        }
    }
}
