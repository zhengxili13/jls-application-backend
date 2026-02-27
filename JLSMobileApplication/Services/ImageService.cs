using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace JLSApplicationBackend.Services
{
    public interface IImageService
    {
        Task<string> UploadProductImageAsync(long productId, IFormFile file);
        Task DeleteImageAsync(string imagePath);
    }

    public class ImageService : IImageService
    {
        private readonly ICloudflareR2Service _cloudflareR2Service;
        private readonly ILogger<ImageService> _logger;

        public ImageService(ICloudflareR2Service cloudflareR2Service, ILogger<ImageService> logger)
        {
            _cloudflareR2Service = cloudflareR2Service;
            _logger = logger;
        }

        public async Task<string> UploadProductImageAsync(long productId, IFormFile file)
        {
            try
            {
                var folderName = $"Images/{productId}"; // Base folder
                var fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" +
                               ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');

                var dbPath = $"{folderName}/{fileName}"; // e.g. Images/123/20261111_123456_img.png

                using (var stream = file.OpenReadStream())
                {
                    // Upload to Cloudflare R2
                    // Note: R2 Key will be "folderName/fileName", exactly the dbPath
                    await _cloudflareR2Service.UploadFileAsync(stream, fileName, file.ContentType, folderName);
                }

                // 返回用于保存到数据库的相对路径（与之前格式一致）
                return dbPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload image for product {ProductId}", productId);
                throw;
            }
        }

        public async Task DeleteImageAsync(string imagePath)
        {
            try
            {
                // In R2, the imagePath stored in DB (e.g. "Images/123/xxx.jpg") is exactly the Key.
                await _cloudflareR2Service.DeleteFileAsync(imagePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete image at {ImagePath}", imagePath);
                throw;
            }
        }
    }
}
