using System.IO;
using System.Threading.Tasks;

namespace JLSApplicationBackend.Services
{
    public interface IGoogleDriveService
    {
        /// <summary>
        /// Gets a file from Google Drive by its file ID and returns it as a MemoryStream.
        /// </summary>
        /// <param name="fileId">The ID of the Google Drive file.</param>
        /// <returns>A tuple containing the file name and the file stream.</returns>
        Task<(string FileName, MemoryStream FileStream)> DownloadFileAsync(string fileId);

        /// <summary>
        /// Uploads a file to Google Drive.
        /// </summary>
        /// <param name="fileStream">The stream of the file to upload.</param>
        /// <param name="fileName">The name of the file to save as.</param>
        /// <param name="mimeType">The MIME type of the file.</param>
        /// <param name="parentFolderId">Optional ID of the folder to upload to.</param>
        /// <returns>The ID of the uploaded file.</returns>
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string mimeType, string parentFolderId = null);
    }
}
