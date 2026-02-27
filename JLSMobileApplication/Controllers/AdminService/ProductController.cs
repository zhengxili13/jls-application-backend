using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using JLSApplicationBackend.Heplers;
using JLSDataAccess;
using JLSDataAccess.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using JLSApplicationBackend.Services;

namespace JLSMobileApplication.Controllers.AdminService;

[Authorize]
[Route("admin/[controller]/{action}")]
[ApiController]
public class ProductController(
    IOptions<AppSettings> appSettings,
    IImageService imageService,
    IProductRepository productRepository,
    IReferenceRepository referenceRepository,
    JlsDbContext context,
    ILogger<ProductController> logger)
    : Controller
{
    private readonly AppSettings _appSettings = appSettings.Value;

    [HttpPost]
    public async Task<long> RemoveImageById([FromBody] long Id)
    {
        var image = await context.ProductPhotoPath.FindAsync(Id);

        if (image == null) return 0;

        try
        {
            // Removed local file deletion, calling ImageService to delete from Cloudflare R2
            await imageService.DeleteImageAsync(image.Path);

            var PhotoId = image.Id;
            context.ProductPhotoPath.Remove(image);

            await context.SaveChangesAsync();

            return PhotoId;
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }


    [HttpPost]
    public async Task<long> SetDefaultImageById([FromBody] long Id)
    {
        var image = await context.ProductPhotoPath.FindAsync(Id);

        if (image == null) return 0;

        try
        {
            var productImageList =
                await context.ProductPhotoPath.Where(p => p.ProductId == image.ProductId).ToListAsync();

            foreach (var item in productImageList)
                if (item.Id == Id)
                    item.IsDefault = true;
                else
                    item.IsDefault = false;

            context.UpdateRange(productImageList);
            await context.SaveChangesAsync();

            return image.Id;
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }


    [HttpGet]
    public async Task<long> RemoveProductCommentById(long CommentId)
    {
        try
        {
            var productCommentId = await productRepository.RemoveProductCommentById(CommentId);
            return productCommentId;
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    [HttpPost]
    public async Task<JsonResult> AdvancedProductSearchByCriteria(AdvancedProductSearchCriteria criteria)
    {
        try
        {
            var result = await productRepository.AdvancedProductSearchByCriteria(criteria.ProductLabel,
                criteria.MainCategoryReferenceId, criteria.SecondCategoryReferenceId, criteria.Validity, criteria.Lang);
            var list = result.Skip(criteria.begin * criteria.step).Take(criteria.step);

            return Json(new
            {
                ProductList = list,
                TotalCount = result.Count()
            });
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    [HttpPost]
    public async Task<long> UpdateOrCreateProduct(UpdateOrCreateProductCriteria criteria)
    {
        try
        {
            var ProductReferenceCategory = await referenceRepository.GetReferenceCategoryByShortLabel("product");
            if (ProductReferenceCategory != null)
            {
                var ReferenceId = await referenceRepository.SaveReferenceItem(criteria.ReferenceId,
                    ProductReferenceCategory.Id, criteria.ReferenceCode, criteria.SecondCategoryId, criteria.Validity,
                    null, criteria.CreatedOrUpdatedBy);

                if (ReferenceId != 0)
                {
                    var ProductId = await productRepository.SaveProductInfo(criteria.ProductId, ReferenceId,
                        criteria.QuantityPerBox, criteria.QuantityPerParcel, criteria.MinQuantity, criteria.Price,
                        criteria.TaxRateId, criteria.Description, criteria.Color, criteria.Material, criteria.Size,
                        criteria.Forme, criteria.CreatedOrUpdatedBy);
                    // todo change : SaveReferenceLabel take an list of param and save one time all the three translation
                    var ReferenceLabelFrId =
                        await referenceRepository.SaveReferenceLabel(ReferenceId, criteria.Labelfr, "fr");
                    var ReferenceLabelEnId =
                        await referenceRepository.SaveReferenceLabel(ReferenceId, criteria.Labelen, "en");
                    var ReferenceLabelCnId =
                        await referenceRepository.SaveReferenceLabel(ReferenceId, criteria.Labelcn, "cn");
                    if (ProductId != 0 && ReferenceLabelFrId != 0 && ReferenceLabelEnId != 0 && ReferenceLabelCnId != 0)
                        return ProductId;
                }
            }

            return 0;
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    [HttpPost]
    public async Task<JsonResult> GetProductInfoByReferenceIds([FromBody] GetProductInfoByReferenceIdsCriteria criteria)
    {
        try
        {
            return Json(await productRepository.GetProductInfoByReferenceIds(criteria.ReferenceIds, criteria.Lang));
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }


    [HttpPost]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadPhoto()
    {
        try
        {
            var file = Request.Form.Files[0];
            Request.Form.TryGetValue("ProductId", out var productIdString);

            if (file.Length > 0 && long.TryParse(productIdString, out long productId))
            {
                // Upload image directly to Cloudflare R2 without keeping it on local disk
                var dbPath = await imageService.UploadProductImageAsync(productId, file);

                // Save db path (folder/filename) to db as usual
                await productRepository.SavePhotoPath(productId, dbPath);
                return Ok(new { dbPath });
            }

            return BadRequest();
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetProductPhotoPathById(long ProductId)
    {
        var data = await productRepository.GetProductPhotoPathById(ProductId);
        return Json(data);
    }


    [HttpGet]
    public async Task<JsonResult> GetProductById(long Id)
    {
        try
        {
            var data = await productRepository.GetProductById(Id, "", null);
            return Json(data);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    public class AdvancedProductSearchCriteria
    {
        public AdvancedProductSearchCriteria()
        {
            SecondCategoryReferenceId = new List<long>();
        }

        public string ProductLabel { get; set; }
        public long MainCategoryReferenceId { get; set; }
        public List<long> SecondCategoryReferenceId { get; set; }

        public bool? Validity { get; set; }
        public string Lang { get; set; }

        public int begin { get; set; }

        public int step { get; set; }
    }

    public class UpdateOrCreateProductCriteria
    {
        public int? CreatedOrUpdatedBy { get; set; }
        public string Labelfr { get; set; }
        public string Labelen { get; set; }
        public string Labelcn { get; set; }

        public string ReferenceCode { get; set; }

        public string Description { get; set; }

        public long SecondCategoryId { get; set; }

        public long ProductId { get; set; }

        public long ReferenceCategoryId { get; set; }

        public long ReferenceId { get; set; }

        public int? QuantityPerBox { get; set; }

        public int? QuantityPerParcel { get; set; }

        public int? MinQuantity { get; set; }

        public float? Price { get; set; }

        public long? TaxRateId { get; set; }

        public bool Validity { get; set; }

        public string Color { get; set; }

        public string Material { get; set; }

        public string Size { get; set; }

        public string Forme { get; set; }
    }

    public class GetProductInfoByReferenceIdsCriteria
    {
        public GetProductInfoByReferenceIdsCriteria()
        {
            ReferenceIds = new List<long>();
        }

        public List<long> ReferenceIds { get; set; }
        public string Lang { get; set; }
    }
}