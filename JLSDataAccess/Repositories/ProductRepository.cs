using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JLSDataAccess.Interfaces;
using JLSDataModel.Models.Product;
using JLSDataModel.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JLSDataAccess.Repositories;

public class ProductRepository(JlsDbContext context, ILogger<ProductRepository> logger) : IProductRepository
{
    private readonly JlsDbContext db = context;

    /*
     * Mobile Zoom
     */
    // todo adapt mobile and admin
    public async Task<List<ProductListData>> GetProductInfoByReferenceIds(List<long> ReferenceIds, string Lang)
    {
        var result = from ri in db.ReferenceItem
            join rc in db.ReferenceCategory on ri.ReferenceCategoryId equals rc.Id
            join rl in db.ReferenceLabel on ri.Id equals rl.ReferenceItemId
            join product in db.Product on ri.Id equals product.ReferenceItemId
            where rc.ShortLabel == "Product" && ri.Validity == true && rl.Lang == Lang && ReferenceIds.Contains(ri.Id)
            select new ProductListData
            {
                ProductId = product.Id,
                ReferenceId = ri.Id,
                Code = ri.Code,
                ParentId = ri.ParentId,
                Value = ri.Value,
                Order = ri.Order,
                Label = rl.Label,
                Price = product.Price,
                PreviousPrice = product.PreviousPrice,
                QuantityPerBox = product.QuantityPerBox,
                QuantityPerParcel = product.QuantityPerParcel,
                MinQuantity = product.MinQuantity,
                DefaultPhotoPath = (from path in db.ProductPhotoPath
                    orderby path.IsDefault descending
                    where path.ProductId == product.Id
                    select path.Path).FirstOrDefault(),
                PhotoPath = (from path in db.ProductPhotoPath
                    where path.ProductId == product.Id
                    select new ProductListPhotoPath { Path = path.Path }).ToList(),
                IsNew = db.CheckNewProduct(product.Id)
            };
        return await result.ToListAsync();
    }

    public async Task<ProductListViewModel> GetProductListBySecondCategory(long SecondCategoryReferenceId, string Lang,
        int begin, int step)
    {
        var result = from ri in db.ReferenceItem
            join rc in db.ReferenceCategory on ri.ReferenceCategoryId equals rc.Id
            join rl in db.ReferenceLabel on ri.Id equals rl.ReferenceItemId
            join product in db.Product on ri.Id equals product.ReferenceItemId
            where rc.ShortLabel == "Product" && ri.Validity == true && rl.Lang == Lang &&
                  ri.ParentId == SecondCategoryReferenceId
            select new ProductListData
            {
                ProductId = product.Id,
                ReferenceId = ri.Id,
                Code = ri.Code,
                ParentId = ri.ParentId,
                Value = ri.Value,
                Order = ri.Order,
                Label = rl.Label,
                Price = product.Price,
                PreviousPrice = product.PreviousPrice,
                QuantityPerBox = product.QuantityPerBox,
                QuantityPerParcel = product.QuantityPerParcel,
                MinQuantity = product.MinQuantity,
                DefaultPhotoPath = (from path in db.ProductPhotoPath
                    orderby path.IsDefault descending
                    where path.ProductId == product.Id
                    select path.Path).FirstOrDefault(),
                IsNew = db.CheckNewProduct(product.Id)
            };
        var totalCount = result.Count();
        var productList = await result.Skip(begin * step).Take(step).ToListAsync();
        return new ProductListViewModel
        {
            ProductListData = productList,
            TotalCount = totalCount
        };
    }

    public async Task<long> RemoveProductCommentById(long ProductCommentId)
    {
        var ProductComment = db.ProductComment.Find(ProductCommentId);
        if (ProductComment != null)
        {
            db.Remove(ProductComment);
            await db.SaveChangesAsync();

            return ProductComment.Id;
        }

        return 0;
    }

    public async Task<ProductListViewModel> GetProductListByPublishDate(string Lang, long? MainCategoryId, int begin,
        int step)
    {
        var result = from ri in db.ReferenceItem
            join riSecond in db.ReferenceItem on ri.ParentId equals riSecond.Id
            join riMain in db.ReferenceItem on riSecond.ParentId equals riMain.Id
            join rc in db.ReferenceCategory on ri.ReferenceCategoryId equals rc.Id
            join rl in db.ReferenceLabel on ri.Id equals rl.ReferenceItemId
            join product in db.Product on ri.Id equals product.ReferenceItemId
            where rc.ShortLabel == "Product" && ri.Validity == true && rl.Lang == Lang
                  && riSecond.Validity == true && riMain.Validity == true &&
                  (MainCategoryId == null || riMain.Id == MainCategoryId)
            orderby product.CreatedOn descending, rc.Id, rl.Label
            select new ProductListData
            {
                Comments = (from pc in db.ProductComment
                    where pc.ProductId == product.Id
                    select pc).ToList(),
                ProductId = product.Id,
                ReferenceId = ri.Id,
                Code = ri.Code,
                ParentId = ri.ParentId,
                Value = ri.Value,
                Order = ri.Order,
                Label = rl.Label,
                Price = product.Price,
                PreviousPrice = product.PreviousPrice,
                QuantityPerBox = product.QuantityPerBox,
                QuantityPerParcel = product.QuantityPerParcel,
                MinQuantity = product.MinQuantity,
                DefaultPhotoPath = (from path in db.ProductPhotoPath
                    orderby path.IsDefault descending
                    where path.ProductId == product.Id
                    select path.Path).FirstOrDefault(),
                IsNew = db.CheckNewProduct(product.Id)
            };
        var totalCount = result.Count();
        var productList = await result.Skip(begin * step).Take(step).ToListAsync();
        return new ProductListViewModel
        {
            ProductListData = productList,
            TotalCount = totalCount
        };
    }

    // By sales performance // todo: by every month
    public async Task<dynamic> GetProductListBySalesPerformance(string Lang, int begin, int step)
    {
        var result = from ri in db.ReferenceItem
            join riSecond in db.ReferenceItem on ri.ParentId equals riSecond.Id
            join riMain in db.ReferenceItem on riSecond.ParentId equals riMain.Id
            join rc in db.ReferenceCategory on ri.ReferenceCategoryId equals rc.Id
            join rl in db.ReferenceLabel on ri.Id equals rl.ReferenceItemId
            join product in db.Product on ri.Id equals product.ReferenceItemId
            from op in db.OrderProduct.Where(p => p.ReferenceId == ri.Id).DefaultIfEmpty()
            where rc.ShortLabel == "Product" && ri.Validity == true && rl.Lang == Lang
                  && riSecond.Validity == true && riMain.Validity == true
            group op by new
            {
                ri.Id, productId = product.Id, ri.ParentId, ri.Code, ri.Value, rl.Label, product.Price,
                product.QuantityPerBox, product.QuantityPerParcel, product.MinQuantity, product.PreviousPrice
            }
            into g
            orderby g.Sum(x => x.Quantity) descending
            select new
            {
                ReferenceId = g.Key.Id,
                ProductId = g.Key.productId,
                g.Key.Code,
                g.Key.ParentId,
                g.Key.Value,
                g.Key.Label,
                g.Key.Price,
                g.Key.PreviousPrice,
                g.Key.QuantityPerBox,
                g.Key.QuantityPerParcel,
                g.Key.MinQuantity
            };
        var totalCount = result.Count();
        var productList = await result.Skip(begin * step).Take(step).ToListAsync();

        var result1 = (from r in productList
            select new
            {
                Comments = (from pc in db.ProductComment
                    where pc.ProductId == r.ProductId
                    select pc).ToList(),
                r.ReferenceId,
                r.ProductId,
                r.Code,
                r.ParentId,
                r.Value,
                r.Label,
                r.Price,
                r.PreviousPrice,
                r.QuantityPerBox,
                r.QuantityPerParcel,
                r.MinQuantity,
                DefaultPhotoPath = (from pp in db.ProductPhotoPath
                    orderby pp.IsDefault descending
                    where pp.ProductId == r.ProductId
                    select pp.Path).FirstOrDefault(),
                IsNew = db.CheckNewProduct(r.ProductId)
            }).ToList();
        return new
        {
            ProductListData = result1,
            TotalCount = totalCount
        };
    }

    public async Task<List<dynamic>> GetProductListByNote(string Lang)
    {
        var result = from ri in db.ReferenceItem
            join riSecond in db.ReferenceItem on ri.ParentId equals riSecond.Id
            join riMain in db.ReferenceItem on riSecond.ParentId equals riMain.Id
            join rc in db.ReferenceCategory on ri.ReferenceCategoryId equals rc.Id
            join rl in db.ReferenceLabel on ri.Id equals rl.ReferenceItemId
            join product in db.Product on ri.Id equals product.ReferenceItemId
            from pc in db.ProductComment.Where(p => p.ProductId == product.Id).DefaultIfEmpty()
            where rc.ShortLabel == "Product" && ri.Validity == true && rl.Lang == Lang
                  && riSecond.Validity == true && riMain.Validity == true
            group pc by new
            {
                ri.Id, productId = product.Id, ri.ParentId, ri.Code, ri.Value, rl.Label, product.Price,
                product.QuantityPerBox, product.QuantityPerParcel, product.MinQuantity, pc.ProductId,
                product.PreviousPrice
            }
            into g
            orderby g.Sum(x => x.Level) descending
            select new
            {
                ReferenceId = g.Key.Id,
                ProductId = g.Key.productId,
                g.Key.Code,
                g.Key.ParentId,
                g.Key.Value,
                g.Key.Label,
                g.Key.Price,
                g.Key.PreviousPrice,
                g.Key.QuantityPerBox,
                g.Key.QuantityPerParcel,
                g.Key.MinQuantity
            };
        var productList = await result.ToListAsync();

        var result1 = (from r in productList
            select new
            {
                Comments = (from pc in db.ProductComment
                    where pc.ProductId == r.ProductId
                    select pc).ToList(),
                r.ReferenceId,
                r.ProductId,
                r.Code,
                r.ParentId,
                r.Value,
                r.Label,
                r.Price,
                r.PreviousPrice,
                r.QuantityPerBox,
                r.QuantityPerParcel,
                r.MinQuantity,
                DefaultPhotoPath = (from pp in db.ProductPhotoPath
                    orderby pp.IsDefault descending
                    where pp.ProductId == r.ProductId
                    select pp.Path).FirstOrDefault(),
                IsNew = db.CheckNewProduct(r.ProductId)
            }).ToList<dynamic>();
        return result1;
    }

    public async Task<List<dynamic>> GetFavoriteListByUserId(int UserId, string Lang)
    {
        var result = await (from ri in db.ReferenceItem
            join riSecond in db.ReferenceItem on ri.ParentId equals riSecond.Id
            join riMain in db.ReferenceItem on riSecond.ParentId equals riMain.Id
            join rc in db.ReferenceCategory on ri.ReferenceCategoryId equals rc.Id
            join rl in db.ReferenceLabel on ri.Id equals rl.ReferenceItemId
            join product in db.Product on ri.Id equals product.ReferenceItemId
            join favoriteList in db.ProductFavorite on product.Id equals favoriteList.ProductId
            where rc.ShortLabel == "Product" && ri.Validity == true && rl.Lang == Lang
                  && riSecond.Validity == true && riMain.Validity == true && ri.Validity == true &&
                  favoriteList.UserId == UserId
            orderby ri.CreatedOn descending, rc.Id, rl.Label
            select new
            {
                Comments = (from pc in db.ProductComment
                    where pc.ProductId == product.Id
                    select pc).ToList(),
                ProductId = product.Id,
                ReferenceId = ri.Id,
                ri.Code,
                ri.ParentId,
                ri.Value,
                ri.Order,
                rl.Label,
                product.Price,
                product.PreviousPrice,
                product.QuantityPerBox,
                product.QuantityPerParcel,
                product.MinQuantity,
                DefaultPhotoPath = (from path in db.ProductPhotoPath
                    orderby path.IsDefault descending
                    where path.ProductId == product.Id
                    select path.Path).FirstOrDefault(),
                IsNew = db.CheckNewProduct(product.Id)
            }).ToListAsync<dynamic>();
        return result;
    }


    public async Task<long> AddIntoProductFavoriteList(int UserId, long ProductId, bool? IsFavorite)
    {
        var result = db.ProductFavorite.Where(p => p.UserId == UserId && p.ProductId == ProductId).FirstOrDefault();
        if (result == null && IsFavorite == true)
        {
            var FavoriteToInsert = new ProductFavorite();
            FavoriteToInsert.UserId = UserId;
            FavoriteToInsert.ProductId = ProductId;

            await db.AddAsync(FavoriteToInsert);
            await db.SaveChangesAsync();
            return FavoriteToInsert.Id;
        }

        if (result != null && IsFavorite == false)
        {
            db.Remove(result);
            await db.SaveChangesAsync();
            return result.Id;
        }

        return 0;
    }

    public async Task<List<ProductCategoryViewModel>> GetProductMainCategory(string Lang)
    {
        var result = await (from ri in db.ReferenceItem
            join riSecond in db.ReferenceItem on ri.ParentId equals riSecond.Id
            join rip in db.ReferenceItem on riSecond.ParentId equals rip.Id
            join rlp in db.ReferenceLabel on rip.Id equals rlp.ReferenceItemId
            join rcp in db.ReferenceCategory on rip.ReferenceCategoryId equals rcp.Id
            where rcp.ShortLabel == "MainCategory" && rlp.Lang == Lang && rip.Validity == true && ri.Validity == true &&
                  riSecond.Validity == true
            group rip by new { rip.Id, rip.Code, rlp.Label }
            into g
            select new ProductCategoryViewModel
            {
                TotalCount = g.Count(),
                ReferenceId = g.Key.Id,
                Reference = new SubProductCategoryViewModel
                {
                    ReferenceId = g.Key.Id,
                    Label = g.Key.Label,
                    Code = g.Key.Code
                }
            }).ToListAsync();

        return result;
    }

    public async Task<List<ProductCategoryViewModel>> GetProductSecondCategory(long MainCategoryReferenceId,
        string Lang)
    {
        var result = await (from ri in db.ReferenceItem
            join rip in db.ReferenceItem on ri.ParentId equals rip.Id
            join rlp in db.ReferenceLabel on rip.Id equals rlp.ReferenceItemId
            join rcp in db.ReferenceCategory on rip.ReferenceCategoryId equals rcp.Id
            where rcp.ShortLabel == "SecondCategory" && rip.ParentId == MainCategoryReferenceId && rlp.Lang == Lang &&
                  rip.Validity == true && ri.Validity == true
            group rip by new { rip.Id, rip.Code, rlp.Label }
            into g
            select new ProductCategoryViewModel
            {
                TotalCount = g.Count(),
                ReferenceId = g.Key.Id,
                Reference = new SubProductCategoryViewModel
                {
                    ReferenceId = g.Key.Id,
                    Label = g.Key.Label,
                    Code = g.Key.Code
                }
            }).ToListAsync();
        return result;
    }

    public async Task<long> SaveProductComment(long ProductId, string Title, string Body, int Level, int UserId)
    {
        // TODO: change modify
        var ProductComment = new ProductComment();
        ProductComment.Title = Title;
        ProductComment.Body = Body;
        ProductComment.ProductId = ProductId;
        ProductComment.Level = Level;
        ProductComment.CreatedOn = DateTime.Now;
        ProductComment.UserId = UserId;
        ProductComment.CreatedBy = UserId;

        await db.ProductComment.AddAsync(ProductComment);
        await db.SaveChangesAsync();

        return ProductComment.Id;
    }

    public async Task<List<ProductComment>> GetAllProductCommentList(int begin, int step)
    {
        var result = await (from pc in db.ProductComment
            orderby pc.CreatedOn
            select pc).Skip(begin * step).Take(step).ToListAsync();
        return result;
    }

    public async Task<List<ProductCommentViewModel>> GetProductCommentListByCriteria(long? ProductId, long? UserId,
        string Lang)
    {
        var result = await (from pc in db.ProductComment
            where (ProductId == null || pc.ProductId == ProductId)
                  && (UserId == null || pc.UserId == UserId)
            orderby pc.CreatedOn
            select new ProductCommentViewModel
            {
                CreatedOn = pc.CreatedOn,
                UserId = pc.UserId,
                User = (from u in db.Users
                    where u.Id == pc.UserId
                    select u).FirstOrDefault(),
                ProductComment = pc,
                PhotoPath = (from pp in db.ProductPhotoPath
                    orderby pp.IsDefault descending
                    where pp.ProductId == pc.ProductId
                    select pp.Path).FirstOrDefault(),
                Label = (from p in db.Product
                    join rl in db.ReferenceLabel on p.ReferenceItemId equals rl.ReferenceItemId
                    where rl.Lang == Lang && p.Id == pc.ProductId
                    select rl.Label).FirstOrDefault()
            }).ToListAsync();
        return result;
    }


    public async Task<List<dynamic>> AdvancedProductSearchByCriteria(string ProductLabel, long MainCategoryReferenceId,
        List<long> SecondCategoryReferenceId, bool? Validity, string Lang)
    {
        var result = await (from rl in db.ReferenceLabel
            join ri in db.ReferenceItem on rl.ReferenceItemId equals ri.Id
            join rc in db.ReferenceCategory on ri.ReferenceCategoryId equals rc.Id
            join p in db.Product on ri.Id equals p.ReferenceItemId
            join riSecond in db.ReferenceItem on ri.ParentId equals riSecond.Id
            join riMain in db.ReferenceItem on riSecond.ParentId equals riMain.Id
            where (ProductLabel == "" || rl.Label.Contains(ProductLabel) || ri.Code.Contains(ProductLabel))
                  && (MainCategoryReferenceId == 0 || riMain.Id == MainCategoryReferenceId)
                  && (SecondCategoryReferenceId.Count() == 0 || SecondCategoryReferenceId.Contains(riSecond.Id))
                  && (Validity == null || ri.Validity == Validity)
                  && rl.Lang == Lang && rc.ShortLabel == "Product"
            orderby riMain.Id, riSecond.Id
            select new
            {
                Comments = (from pc in db.ProductComment
                    where pc.ProductId == p.Id
                    select pc).ToList(),
                p.CreatedOn,
                ReferenceId = ri.Id,
                ProductId = p.Id,
                rl.Label,
                Translations = from rl1 in db.ReferenceLabel
                    where rl1.ReferenceItemId == ri.Id
                    select rl1,
                ri.Code,
                ri.Validity,
                CategoryId = rc.Id,
                CategoryLabel = rc.ShortLabel,
                p.QuantityPerBox,
                p.BarreCode,
                p.Forme,
                p.Size,
                p.Color,
                p.Material,
                p.Price,
                p.MinQuantity,
                p.Description,
                p.PreviousPrice,
                IsNew = db.CheckNewProduct(p.Id),
                ImagesPath = (from path in db.ProductPhotoPath
                    orderby path.IsDefault descending
                    where path.ProductId == p.Id
                    select new { path.Id, path.Path }).ToList(),
                DefaultPhotoPath = (from path in db.ProductPhotoPath
                    orderby path.IsDefault descending
                    where path.ProductId == p.Id
                    select path.Path).FirstOrDefault(),
                MainCategoryLabel = (from rlMain in db.ReferenceLabel
                    where rlMain.ReferenceItemId == riMain.Id && rlMain.Lang == Lang
                    select rlMain.Label).FirstOrDefault(),
                SecondCategoryLabel = (from rlSecond in db.ReferenceLabel
                    where rlSecond.ReferenceItemId == riSecond.Id && rlSecond.Lang == Lang
                    select rlSecond.Label).FirstOrDefault()
            }).ToListAsync<dynamic>();

        return result.Distinct().ToList();
    }


    public async Task<List<dynamic>> GetPromotionProduct(string Lang)
    {
        var result = await (from rl in db.ReferenceLabel
            join ri in db.ReferenceItem on rl.ReferenceItemId equals ri.Id
            join rc in db.ReferenceCategory on ri.ReferenceCategoryId equals rc.Id
            join p in db.Product on ri.Id equals p.ReferenceItemId
            join riSecond in db.ReferenceItem on ri.ParentId equals riSecond.Id
            join riMain in db.ReferenceItem on riSecond.ParentId equals riMain.Id
            where ri.Validity == true && riMain.Validity == true && riSecond.Validity == true && rl.Lang == Lang &&
                  rc.ShortLabel == "Product"
            orderby p.PreviousPrice == null, p.Price / p.PreviousPrice, rc.Id, rl.Label
            select new
            {
                Comments = (from pc in db.ProductComment
                    where pc.ProductId == p.Id
                    select pc).ToList(),
                p.CreatedOn,
                ReferenceId = ri.Id,
                ProductId = p.Id,
                p.MinQuantity,
                rl.Label,
                ri.Code,
                ri.Validity,
                CategoryId = rc.Id,
                CategoryLabel = rc.ShortLabel,
                p.QuantityPerBox,
                p.QuantityPerParcel,
                p.BarreCode,
                p.Size,
                p.Color,
                p.Material,
                p.Price,
                IsNew = db.CheckNewProduct(p.Id),
                p.PreviousPrice,
                DefaultPhotoPath = (from path in db.ProductPhotoPath
                    orderby path.IsDefault descending
                    where path.ProductId == p.Id
                    select path.Path).FirstOrDefault(),
                MainCategoryLabel = (from rlMain in db.ReferenceLabel
                    where rlMain.ReferenceItemId == riMain.Id && rlMain.Lang == Lang
                    select rlMain.Label).FirstOrDefault(),
                SecondCategoryLabel = (from rlSecond in db.ReferenceLabel
                    where rlSecond.ReferenceItemId == riSecond.Id && rlSecond.Lang == Lang
                    select rlSecond.Label).FirstOrDefault()
            }).ToListAsync<dynamic>();

        return result.Distinct().ToList();
    }


    // For mobile attention : same format as by sales performance... (todo: fix format)
    public async Task<List<dynamic>> SimpleProductSearch(string SearchText, string Lang)
    {
        var result = await (from riProduct in db.ReferenceItem
            join p in db.Product on riProduct.Id equals p.ReferenceItemId
            join rlProduct in db.ReferenceLabel on riProduct.Id equals rlProduct.ReferenceItemId
            join riSecond in db.ReferenceItem on riProduct.ParentId equals riSecond.Id
            join rlSecond in db.ReferenceLabel on riProduct.Id equals rlSecond.ReferenceItemId
            join riMain in db.ReferenceItem on riSecond.ParentId equals riMain.Id
            join rlMain in db.ReferenceLabel on riMain.Id equals rlMain.ReferenceItemId
            where riProduct.Validity == true && riSecond.Validity == true && riMain.Validity == true &&
                  rlProduct.Lang == Lang && rlSecond.Lang == Lang && rlMain.Lang == Lang &&
                  (rlMain.Label.Contains(SearchText) || rlSecond.Label.Contains(SearchText) ||
                   rlProduct.Label.Contains(SearchText) || p.Description.Contains(SearchText) ||
                   riProduct.Code.Contains(SearchText))
            select new
            {
                ReferenceId = p.ReferenceItemId,
                ProductId = p.Id,
                riProduct.Code,
                riProduct.ParentId,
                riProduct.Value,
                rlProduct.Label,
                p.Price,
                p.PreviousPrice,
                p.QuantityPerBox,
                p.QuantityPerParcel,
                p.MinQuantity,
                DefaultPhotoPath = (from pp in db.ProductPhotoPath
                    orderby pp.IsDefault descending
                    where pp.ProductId == p.Id
                    select pp.Path).FirstOrDefault(),
                IsNew = db.CheckNewProduct(p.Id)
            }).ToListAsync<dynamic>();

        return result;
    }

    public async Task<List<dynamic>> AdvancedProductSearchClient(string SearchText, long? MainCategory,
        long? SecondCategory, int? PriceIntervalLower, int? PriceIntervalUpper, int? MinQuantity, string OrderBy,
        string Lang)
    {
        var result = from riProduct in db.ReferenceItem
            join p in db.Product on riProduct.Id equals p.ReferenceItemId
            join rlProduct in db.ReferenceLabel on riProduct.Id equals rlProduct.ReferenceItemId
            join riSecond in db.ReferenceItem on riProduct.ParentId equals riSecond.Id
            join riMain in db.ReferenceItem on riSecond.ParentId equals riMain.Id
            where riProduct.Validity == true && riSecond.Validity == true && riMain.Validity == true &&
                  rlProduct.Lang == Lang
                  && (SearchText == null || SearchText == "" || rlProduct.Label.Contains(SearchText) ||
                      p.Description.Contains(SearchText) || riProduct.Code.Contains(SearchText))
                  && (MainCategory == null || riMain.Id == MainCategory)
                  && (SecondCategory == null || riSecond.Id == SecondCategory)
                  && (PriceIntervalLower == null || p.Price >= PriceIntervalLower)
                  && (PriceIntervalUpper == null || p.Price <= PriceIntervalUpper)
                  && (MinQuantity == null || p.MinQuantity <= MinQuantity)
            select new
            {
                Comments = (from pc in db.ProductComment
                    where pc.ProductId == p.Id
                    select pc).ToList(),
                p.CreatedOn,
                SalesQuantity = (from op in db.OrderProduct
                    where op.ReferenceId == riProduct.Id
                    select op.Id).Count(),
                ReferenceId = p.ReferenceItemId,
                ProductId = p.Id,
                riProduct.Code,
                riProduct.ParentId,
                riProduct.Value,
                rlProduct.Label,
                p.Price,
                p.PreviousPrice,
                p.QuantityPerBox,
                p.QuantityPerParcel,
                p.MinQuantity,
                DefaultPhotoPath = (from pp in db.ProductPhotoPath
                    orderby pp.IsDefault descending
                    where pp.ProductId == p.Id
                    select pp.Path).FirstOrDefault(),
                IsNew = db.CheckNewProduct(p.Id)
            };

        switch (OrderBy)
        {
            case "Default":
                result = result.OrderBy(p => p.Label);
                break;
            case "Price_Increase":
                result = result.OrderBy(p => p.Price);
                break;
            case "Price_Decrease":
                result = result.OrderByDescending(p => p.Price);
                break;
            case "PublishDate_Recent":
                result = result.OrderByDescending(p => p.CreatedOn).ThenBy(p => p.Label);
                break;
            case "Porpularity_More":
                result = result.OrderByDescending(p => p.SalesQuantity);
                break;
            case "Promotion_More":
                result = result.OrderBy(p => p.PreviousPrice == null).ThenBy(p => p.Price / p.PreviousPrice);
                break;
            default:
                result = result.OrderBy(p => p.Label);
                break;
        }

        return await result.ToListAsync<dynamic>();
    }

    public async Task<List<dynamic>> GetProductByPrice(string Lang, long? MainCategoryId)
    {
        var result = from riProduct in db.ReferenceItem
            join p in db.Product on riProduct.Id equals p.ReferenceItemId
            join rlProduct in db.ReferenceLabel on riProduct.Id equals rlProduct.ReferenceItemId
            join riSecond in db.ReferenceItem on riProduct.ParentId equals riSecond.Id
            join riMain in db.ReferenceItem on riSecond.ParentId equals riMain.Id
            where riProduct.Validity == true && riSecond.Validity == true && riMain.Validity == true &&
                  rlProduct.Lang == Lang && (MainCategoryId == null || riMain.Id == MainCategoryId)
            orderby p.Price
            select new
            {
                Comments = (from pc in db.ProductComment
                    where pc.ProductId == p.Id
                    select pc).ToList(),
                ReferenceId = p.ReferenceItemId,
                ProductId = p.Id,
                riProduct.Code,
                riProduct.ParentId,
                riProduct.Value,
                rlProduct.Label,
                p.Price,
                p.PreviousPrice,
                p.QuantityPerBox,
                p.QuantityPerParcel,
                p.MinQuantity,

                IsNew = db.CheckNewProduct(p.Id),
                DefaultPhotoPath = (from pp in db.ProductPhotoPath
                    orderby pp.IsDefault descending
                    where pp.ProductId == p.Id
                    select pp.Path).FirstOrDefault()
            };

        return await result.ToListAsync<dynamic>();
    }


    public async Task<long> SaveProductInfo(long ProductId, long ReferenceId, int? QuantityPerBox,
        int? QuantityPerParcel, int? MinQuantity, float? Price, long? TaxRateId, string Description, string Color,
        string Material, string Size, string Forme, int? CreatedOrUpdatedBy)
    {
        Product ProductToUpdateOrCreate = null;
        if (ProductId == 0)
        {
            ProductToUpdateOrCreate = new Product();
            ProductToUpdateOrCreate.ReferenceItemId = ReferenceId;
            ProductToUpdateOrCreate.CreatedBy = CreatedOrUpdatedBy;
            ProductToUpdateOrCreate.CreatedOn = DateTime.Now;
        }
        else
        {
            ProductToUpdateOrCreate = db.Product.Where(p => p.Id == ProductId).FirstOrDefault();
            ProductToUpdateOrCreate.UpdatedBy = CreatedOrUpdatedBy;
        }

        if (ProductToUpdateOrCreate != null)
        {
            ProductToUpdateOrCreate.QuantityPerBox = QuantityPerBox;
            ProductToUpdateOrCreate.QuantityPerParcel = QuantityPerParcel;
            ProductToUpdateOrCreate.MinQuantity = MinQuantity;
            ProductToUpdateOrCreate.Price = Price;
            ProductToUpdateOrCreate.TaxRateId = TaxRateId;
            ProductToUpdateOrCreate.Description = Description;
            ProductToUpdateOrCreate.Color = Color;
            ProductToUpdateOrCreate.Material = Material;
            ProductToUpdateOrCreate.Size = Size;
            ProductToUpdateOrCreate.Forme = Forme;


            if (ProductId == 0)
                await db.Product.AddAsync(ProductToUpdateOrCreate);
            else
                db.Product.Update(ProductToUpdateOrCreate);
            await db.SaveChangesAsync();
            return ProductToUpdateOrCreate.Id;
        }

        return 0;
    }


    /*
     * Admin Zoom
     */

    public async Task<dynamic> GetProductById(long Id, string Lang, int? UserId)
    {
        var result = await (from ri in db.ReferenceItem
            join p in db.Product on ri.Id equals p.ReferenceItemId
            where p.Id == Id
            select new
            {
                ProductId = p.Id,
                IsFavorite = db.ProductFavorite.Where(p => p.ProductId == Id).FirstOrDefault() != null ? true : false,
                HasBought = (from o in db.OrderInfo
                    join op in db.OrderProduct on o.Id equals op.OrderId
                    join riStatus in db.ReferenceItem on o.StatusReferenceItemId equals riStatus.Id
                    where o.UserId == UserId && riStatus.Code == "OrderStatus_Valid" && op.ReferenceId == ri.Id
                    select o).FirstOrDefault() != null
                    ? true
                    : false,
                MainCategoryId = (from riMain in db.ReferenceItem
                    join riSecond in db.ReferenceItem on riMain.Id equals riSecond.ParentId
                    where riSecond.Id == ri.ParentId
                    select riMain.Id).FirstOrDefault(),
                MainCategoryLabel = (from riMain in db.ReferenceItem
                    join riSecond in db.ReferenceItem on riMain.Id equals riSecond.ParentId
                    join rlMain in db.ReferenceLabel on riMain.Id equals rlMain.ReferenceItemId
                    where riSecond.Id == ri.ParentId && rlMain.Lang == Lang
                    select rlMain.Label).FirstOrDefault(),

                SecondCategoryLabel = (from riSecond in db.ReferenceItem
                    join rlSecond in db.ReferenceLabel on riSecond.Id equals rlSecond.ReferenceItemId
                    where riSecond.Id == ri.ParentId && rlSecond.Lang == Lang
                    select rlSecond.Label).FirstOrDefault(),
                SecondCategoryId = ri.ParentId,
                ReferenceCode = ri.Code,
                p.MinQuantity,
                p.Price,
                p.PreviousPrice,
                p.QuantityPerBox,
                p.QuantityPerParcel,
                p.Description,
                p.Forme,
                ReferenceId = ri.Id,
                p.TaxRateId,
                Label = (from rl in db.ReferenceLabel
                    where rl.ReferenceItemId == ri.Id && rl.Lang == Lang
                    select rl.Label).FirstOrDefault(),
                TaxRate = (from riTaxRate in db.ReferenceItem
                    where riTaxRate.Id == p.TaxRateId
                    select riTaxRate).FirstOrDefault(),
                Translation = (from label in db.ReferenceLabel
                    where label.ReferenceItemId == ri.Id
                    select new
                    {
                        label.Id,
                        label.Label,
                        label.Lang
                    }).ToList(),
                ImagesPath = (from path in db.ProductPhotoPath
                    where path.ProductId == p.Id
                    orderby path.IsDefault descending
                    select new { path.Id, path.Path, path.IsDefault }).ToList(),
                Comments = (from c in db.ProductComment
                    where c.ProductId == p.Id
                    select new
                    {
                        c.Id,
                        c.CreatedOn,
                        c.CreatedBy,
                        Email = db.Users.Where(p => p.Id == c.UserId).Select(p => p.Email).FirstOrDefault(),
                        c.Body,
                        c.Title,
                        c.Level
                    }).ToList(),
                p.Color,
                p.Material,
                p.Size,
                ri.Validity,
                IsNew = db.CheckNewProduct(p.Id),
                DefaultPhotoPath = (from pp in db.ProductPhotoPath
                    where pp.ProductId == p.Id
                    orderby pp.IsDefault descending
                    select pp.Path).FirstOrDefault()
            }).FirstOrDefaultAsync();
        if (result == null) return null;

        return result;
    }


    public async Task<dynamic> GetProductPhotoPathById(long ProductId)
    {
        var result = await (from pt in db.ProductPhotoPath
            orderby pt.IsDefault descending
            where pt.ProductId == ProductId
            select pt).ToListAsync();

        if (result == null) return null;

        return result;
    }

    public async Task<long> SavePhotoPath(long ProductId, string Path)
    {
        var photoPath = db.ProductPhotoPath.Where(p => p.ProductId == ProductId && p.Path == Path).FirstOrDefault();
        if (ProductId > 0 && Path != "" && photoPath == null)
        {
            var path = new ProductPhotoPath();
            path.ProductId = ProductId;
            path.Path = Path;
            await db.ProductPhotoPath.AddAsync(path);
            await db.SaveChangesAsync();

            return path.Id;
        }

        return 0;
    }


    public async Task<int> RemoveImageById(long Id)
    {
        var image = await db.ProductPhotoPath.FindAsync(Id);

        if (image == null) return 0;

        var imagePath = "images/" + image.Path; // todo place into the configuration

        try
        {
            if (File.Exists(imagePath)) File.Delete(imagePath);

            db.ProductPhotoPath.Remove(image);

            await db.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            return 0;
        }

        return 1;
    }


    public async Task<List<dynamic>> GetCategoryForWebSite(string Lang)
    {
        var result = await (from riMain in db.ReferenceItem
            join rlMain in db.ReferenceLabel on riMain.Id equals rlMain.ReferenceItemId
            join rcMain in db.ReferenceCategory on riMain.ReferenceCategoryId equals rcMain.Id
            where riMain.Validity == true && rcMain.ShortLabel == "MainCategory" && rlMain.Lang == Lang
            orderby riMain.Order
            select new
            {
                riMain.Id,
                riMain.Code,
                rlMain.Label,
                CategoryId = rcMain.Id,
                CategoryShortLabel = rcMain.ShortLabel,
                SecondCategory = (from riSecond in db.ReferenceItem
                    join rlSecond in db.ReferenceLabel on riSecond.Id equals rlSecond.ReferenceItemId
                    join rcSecond in db.ReferenceCategory on riSecond.ReferenceCategoryId equals rcSecond.Id
                    where riSecond.ParentId == riMain.Id && riSecond.Validity == true && rlSecond.Lang == Lang &&
                          rcSecond.ShortLabel == "SecondCategory"
                    orderby riSecond.Order
                    select new
                    {
                        NumberOfProduct = (from p in db.Product
                                join riProduct in db.ReferenceItem on p.ReferenceItemId equals riProduct.Id
                                where riProduct.Validity == true && riProduct.ParentId == riSecond.Id
                                select p.Id
                            ).Count(),
                        riSecond.Id,
                        riSecond.Code,
                        rlSecond.Label,
                        CategoryId = rcSecond.Id,
                        CategoryShortLabel = rcSecond.ShortLabel
                    }).ToList()
            }).ToListAsync<dynamic>();

        return result.Distinct().ToList();
    }
}