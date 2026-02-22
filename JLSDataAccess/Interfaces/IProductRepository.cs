using System.Collections.Generic;
using System.Threading.Tasks;
using JLSDataModel.Models.Product;
using JLSDataModel.ViewModels;

namespace JLSDataAccess.Interfaces;

public interface IProductRepository
{
    Task<List<ProductCategoryViewModel>> GetProductMainCategory(string Lang);
    Task<List<dynamic>> GetProductByPrice(string Lang, long? MainCategoryId);
    Task<List<ProductCategoryViewModel>> GetProductSecondCategory(long MainCategoryReferenceId, string Lang);
    Task<List<dynamic>> GetPromotionProduct(string Lang);

    Task<ProductListViewModel> GetProductListBySecondCategory(long SecondCategoryReferenceId, string Lang, int begin,
        int step);

    Task<List<ProductListData>> GetProductInfoByReferenceIds(List<long> ReferenceIds, string Lang);

    Task<dynamic> GetProductListBySalesPerformance(string Lang, int begin, int step);

    Task<dynamic> GetProductPhotoPathById(long ProductId);

    Task<long> SavePhotoPath(long ProductId, string Path);

    Task<ProductListViewModel> GetProductListByPublishDate(string Lang, long? MainCategoryId, int begin, int step);

    Task<List<dynamic>> GetProductListByNote(string Lang);

    Task<List<ProductComment>> GetAllProductCommentList(int begin, int step);
    Task<List<ProductCommentViewModel>> GetProductCommentListByCriteria(long? ProductId, long? UserId, string Lang);

    Task<long> RemoveProductCommentById(long ProductCommentId);

    Task<List<dynamic>> SimpleProductSearch(string SearchText, string Lang);

    Task<List<dynamic>> AdvancedProductSearchClient(string SearchText, long? MainCategory, long? SecondCategory,
        int? PriceIntervalLower, int? PriceIntervalUpper, int? MinQuantity, string OrderBy, string Lang);

    Task<long> SaveProductComment(long ProductId, string Title, string Body, int Level, int UserId);

    /*
     *  Admin zoom
     */
    Task<List<dynamic>> AdvancedProductSearchByCriteria(string ProductLabel, long MainCategoryReferenceId,
        List<long> SecondCategoryReferenceId, bool? Validity, string Lang);

    Task<long> SaveProductInfo(long ProductId, long ReferenceId, int? QuantityPerBox, int? QuantityPerParcel,
        int? MinQuantity, float? Price, long? TaxRate, string Description, string Color, string Material, string Size,
        string Forme, int? CreatedOrUpdatedBy);

    Task<dynamic> GetProductById(long id, string Lang, int? UserId);
    Task<int> RemoveImageById(long id);


    Task<List<dynamic>> GetFavoriteListByUserId(int UserId, string Lang);
    Task<long> AddIntoProductFavoriteList(int UserId, long ProductId, bool? IsFavorite);


    Task<List<dynamic>> GetCategoryForWebSite(string Lang);
}