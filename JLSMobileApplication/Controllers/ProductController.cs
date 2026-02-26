using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using JLSApplicationBackend.Heplers;
using JLSDataAccess.Interfaces;
using JLSDataModel.Models.Product;
using JLSDataModel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JLSApplicationBackend.Controllers;

[Route("api/[controller]/{action}/{id?}")]
[ApiController]
public class ProductController(IMapper mapper, IProductRepository product, ILogger<ProductController> logger)
    : Controller
{
    private readonly IMapper _mapper = mapper;

    [HttpGet]
    public async Task<JsonResult> GetProductMainCategory(string lang)
    {
        try
        {
            return Json(new ApiResult
            {
                Data = await product.GetProductMainCategory(lang),
                Msg = "OK",
                Success = true
            });
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetProductListBySalesPerformance(string lang, int begin, int step)
    {
        try
        {
            return Json(new ApiResult
            {
                Data = await product.GetProductListBySalesPerformance(lang, begin, step),
                Msg = "OK",
                Success = true
            });
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }


    [HttpGet]
    public async Task<JsonResult> GetProductByPrice(string lang, long? mainCategoryId, int begin, int step)
    {
        try
        {
            var result = await product.GetProductByPrice(lang, mainCategoryId, begin, step);

            return Json(new
            {
                TotalCount = result.TotalCount,
                List = result.List
            });
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetProductListByPublishDate(string lang, long? mainCategoryId, int begin, int step)
    {
        try
        {
            return Json(new ApiResult
            {
                Data = await product.GetProductListByPublishDate(lang, mainCategoryId, begin, step),
                Msg = "OK",
                Success = true
            });
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetProductSecondCategory(long mainCategoryReferenceId, string lang)
    {
        try
        {
            return Json(new ApiResult
            {
                Data = await product.GetProductSecondCategory(mainCategoryReferenceId, lang),
                Msg = "OK",
                Success = true
            });
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }


    [HttpGet]
    public async Task<JsonResult> GetProductListBySecondCategory(long secondCategoryReferenceId, string lang, int begin,
        int step)
    {
        try
        {
            var productList =
                await product.GetProductListBySecondCategory(secondCategoryReferenceId, lang, begin, step);

            return Json(new ApiResult
            {
                Data = new
                {
                    productList.ProductListData, productList.TotalCount
                },
                Msg = "OK",
                Success = true
            });
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }


    [HttpGet]
    public async Task<JsonResult> GetProductCommentListByCriteria(long? productId, long? userId, int begin, int step,
        string lang)
    {
        try
        {
            var productComments = await product.GetProductCommentListByCriteria(productId, userId, lang, begin, step);
            return Json(new ApiResult
            {
                Data = new
                {
                    ProductCommentListData = productComments.List,
                    TotalCount = productComments.TotalCount
                },
                Msg = "OK",
                Success = true
            });
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }


    [HttpGet]
    public async Task<JsonResult> GetPromotionProduct(int begin, int step, string lang)
    {
        try
        {
            var productList = await product.GetPromotionProduct(lang);
            var list = new List<dynamic>();
            if (begin != -1 && step != -1)
                list = productList.Skip(begin * step).Take(step).ToList();
            else
                list = productList.ToList();
            return Json(new
                {
                    ProductList = list,
                    TotalCount = productList.Count()
                }
            );
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    [HttpPost]
    public async Task<JsonResult> GetProductInfoByReferenceIds([FromBody] GetProductInfoByReferenceIdsCriteria criteria)
    {
        try
        {
            return Json(await product.GetProductInfoByReferenceIds(criteria.ReferenceIds, criteria.lang));
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> SimpleProductSearch(string searchText, string lang, int begin, int step)
    {
        try
        {
            var result = await product.SimpleProductSearch(searchText, lang, begin, step);
            return Json(new
            {
                TotalCount = result.TotalCount,
                List = result.List
            });
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    [HttpPost]
    public async Task<JsonResult> AdvancedProductSearchClient(AdvancedSearchCriteria criteria)
    {
        try
        {
            var result = await product.AdvancedProductSearchClient(criteria.searchText, criteria.MainCategory,
                criteria.SecondCategory, criteria.PriceIntervalLower, criteria.PriceIntervalUpper, criteria.MinQuantity,
                criteria.OrderBy, criteria.lang, criteria.begin, criteria.step);
            return Json(new
            {
                TotalCount = result.TotalCount,
                List = result.List
            });
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetProductListByNote(string lang, int begin, int step)
    {
        try
        {
            var result = await product.GetProductListByNote(lang, begin, step);
            return Json(new
            {
                TotalCount = result.TotalCount,
                List = result.List
            });
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }


    [HttpGet]
    public async Task<JsonResult> GetProductById(long productId, string lang, int? userId)
    {
        try
        {
            var data = await product.GetProductById(productId, lang, userId);
            return Json(data);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }


    [HttpGet]
    public async Task<JsonResult> GetFavoriteListByUserId(int userId, string lang, int? step, int? begin)
    {
        try
        {
            var result = await product.GetFavoriteListByUserId(userId, lang, begin ?? 0, step ?? 10);
            return Json(new
            {
                TotalCount = result.TotalCount,
                List = result.List
            });
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> AddIntoProductFavoriteList(int userId, long productId, bool? isFavorite)
    {
        try
        {
            var favoriteProductList = await product.AddIntoProductFavoriteList(userId, productId, isFavorite);
            return Json(favoriteProductList);
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }


    /******************************** ZOOM Service with authentification *******************************/
    [Authorize]
    [HttpPost]
    public async Task<JsonResult> SaveProductComment([FromBody] ProductComment comment)
    {
        try
        {
            return Json(new ApiResult
            {
                Data = await product.SaveProductComment(comment.ProductId, comment.Title, comment.Body, comment.Level,
                    comment.UserId),
                Msg = "OK",
                Success = true
            });
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    /* Only for web site */
    [HttpGet]
    public async Task<JsonResult> GetCategoryForWebSite(int numberOfCategory, string lang)
    {
        try
        {
            var result = await product.GetCategoryForWebSite(lang);
            if (numberOfCategory != -1) result = result.Take(numberOfCategory).ToList();
            return Json(result);
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetMainPageForWebSite(string lang)
    {
        var step = 10;
        var begin = 0;
        try
        {
            var productByPublishDate = await product.GetProductListByPublishDate(lang, null, begin, step);
            var productBySalesPerformance = await product.GetProductListBySalesPerformance(lang, begin, step);
            var productByPrice = (await product.GetProductByPrice(lang, null, 0, step)).List;


            return Json(new
            {
                resultByPublishDate = productByPublishDate,
                resultBySalesPerformance = productBySalesPerformance,
                resultByPrice = productByPrice
            });
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    public class GetProductInfoByReferenceIdsCriteria
    {
        public GetProductInfoByReferenceIdsCriteria()
        {
            ReferenceIds = new List<long>();
        }

        public List<long> ReferenceIds { get; set; }
        public string lang { get; set; }
    }


    public class AdvancedSearchCriteria
    {
        public string searchText { get; set; }
        public long? MainCategory { get; set; }
        public long? SecondCategory { get; set; }
        public int? PriceIntervalLower { get; set; }
        public int? PriceIntervalUpper { get; set; }
        public int? MinQuantity { get; set; }
        public string OrderBy { get; set; }
        public string lang { get; set; }

        public int begin { get; set; }
        public int step { get; set; }
    }
}