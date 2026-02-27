using System.IO;
using System.Threading.Tasks;

namespace JLSApplicationBackend.Services
{
    public interface ICloudflareR2Service
    {
        /// <summary>
        /// Gets a file from Cloudflare R2 by its file ID (Key) and returns it as a MemoryStream.
        /// </summary>
        /// <param name="fileId">The ID (key) of the file in R2.</param>
        /// <param name="folder">Optional: specify folder like "Images" or "Exports" if not included in the ID.</param>
        /// <returns>A tuple containing the file name and the file stream.</returns>
        Task<(string FileName, MemoryStream FileStream)> DownloadFileAsync(string fileId, string folder = null);

        /// <summary>
        /// Uploads a file to Cloudflare R2.
        /// </summary>
        /// <param name="fileStream">The stream of the file to upload.</param>
        /// <param name="fileName">The name of the file to save as.</param>
        /// <param name="mimeType">The MIME type of the file.</param>
        /// <param name="folder">The folder to upload to, e.g., "Images" or "Exports".</param>
        /// <returns>The ID (key) of the uploaded file.</returns>
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string mimeType, string folder = "Exports");

        /// <summary>
        /// Gets a file from Cloudflare R2 by its file name and returns it as a MemoryStream.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="folder">The folder where the file resides, e.g., "Exports".</param>
        /// <returns>A tuple containing the file name and the file stream.</returns>
        Task<(string FileName, MemoryStream FileStream)> DownloadFileByNameAsync(string fileName, string folder = "Exports");

        /// <summary>
        /// Deletes a file from Cloudflare R2 by its key (id).
        /// </summary>
        /// <param name="fileId">The full key or ID of the file.</param>
        /// <param name="folder">Optional folder name.</param>
        Task DeleteFileAsync(string fileId, string folder = null);
    }
}
