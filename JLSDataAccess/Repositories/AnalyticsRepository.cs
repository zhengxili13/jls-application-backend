using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JLSDataAccess.Interfaces;
using JLSDataModel.Models.Analytics;
using Microsoft.EntityFrameworkCore;

namespace JLSDataAccess.Repositories;

public class AnalyticsRepository : IAnalyticsReporsitory
{
    private readonly JlsDbContext db;

    public AnalyticsRepository(JlsDbContext context)
    {
        db = context;
    }

    public async Task<List<dynamic>> GetTeamMemberSalesPerformance()
    {
        var adminRoleIds = await db.Roles.Where(p => p.Name == "Admin" || p.Name == "SuperAdmin").Select(p => p.Id)
            .ToListAsync();
        var salesPerformance = await (from p in db.OrderInfo
            join ur in db.UserRoles on p.UserId equals ur.UserId
            where adminRoleIds.Contains(ur.RoleId) //&& p.CreatedOn>= DateTime.Now.AddYears(-1) // last 1 year 
            group p by new { p.UserId, p.CreatedOn.Value.Month, p.CreatedOn.Value.Year }
            into g
            orderby g.Key.Year, g.Key.Month descending
            select new TeamMemberSalesPerformance
            {
                UserId = g.Key.UserId,
                Year = g.Key.Year,
                Month = g.Key.Month,
                Sum = g.Sum(p => p.TotalPrice)
            }).ToListAsync();
        var userList = await (from u in db.Users
            join ur in db.UserRoles on u.Id equals ur.UserId
            where adminRoleIds.Contains(ur.RoleId)
            select new
            {
                UserId = u.Id,
                Performance = from s in salesPerformance
                    where s.UserId == u.Id
                    select s,
                Username = u.UserName,
                u.CreatedOn
            }).ToListAsync<dynamic>();

        return userList;
    }


    public async Task<List<dynamic>> GetInternalExternalSalesPerformance(string Lang)
    {
        var riValidAndProgressing = await db.ReferenceItem
            .Where(p => p.Code == "OrderStatus_Valid" || p.Code == "OrderStatus_Progressing").Select(p => p.Id)
            .ToListAsync();
        var result = await (from ri in db.ReferenceItem
            join rl in db.ReferenceLabel on ri.Id equals rl.ReferenceItemId
            where (ri.Code == "OrderType_Internal" || ri.Code == "OrderType_External") && rl.Lang == Lang
            select new
            {
                ri.Id,
                ri.Code,
                rl.Label,
                OrderCount = db.OrderInfo.Where(p =>
                    p.OrderTypeId == ri.Id && riValidAndProgressing.Contains(p.StatusReferenceItemId)).Count(),
                OrderSum = db.OrderInfo
                    .Where(p => p.OrderTypeId == ri.Id && riValidAndProgressing.Contains(p.StatusReferenceItemId))
                    .Sum(p => p.TotalPrice)
            }).ToListAsync<dynamic>();

        return result;
    }

    public async Task<dynamic> GetVisitorAndClientInfo()
    {
        var visitorData = await db.VisitorCounter.ToListAsync();
        var UserInfo = await db.Users.ToListAsync();
        return new
        {
            VisitorData = visitorData, UserInfo
        };
    }

    public async Task<List<dynamic>> GetSalesPerformanceByStatus(string Lang)
    {
        var statusCategoryId = db.ReferenceCategory.Where(p => p.ShortLabel == "OrderStatus").Select(p => p.Id)
            .FirstOrDefault();
        var result = await (from ri in db.ReferenceItem
            join rl in db.ReferenceLabel on ri.Id equals rl.ReferenceItemId
            where ri.ReferenceCategoryId == statusCategoryId && rl.Lang == Lang
            select new
            {
                ri.Id,
                ri.Code,
                rl.Label,
                OrderCount = db.OrderInfo.Where(p => p.StatusReferenceItemId == ri.Id).Count()
            }).ToListAsync<dynamic>();

        return result;
    }

    public async Task<List<dynamic>> GetTopSaleProduct(string Lang, int? Limit)
    {
        var orderRefuseStatusId = db.ReferenceItem.Where(p => p.Code == "OrderStatus_Refus").FirstOrDefault().Id;
        var rcProductId = db.ReferenceCategory.Where(p => p.ShortLabel == "Product").FirstOrDefault().Id;
        var result = await (from ri in db.ReferenceItem
            join rl in db.ReferenceLabel on ri.Id equals rl.ReferenceItemId
            from op in db.OrderProduct.Where(x => ri.Id == x.ReferenceId).DefaultIfEmpty()
            from o in db.OrderInfo.Where(x => op.OrderId == x.Id).DefaultIfEmpty()
            where o.StatusReferenceItemId != orderRefuseStatusId && rl.Lang == Lang &&
                  ri.ReferenceCategoryId == rcProductId
            group op by new { ri.Id, rl.Label, ri.Code }
            into g
            select new
            {
                id = g.Key.Id,
                name = g.Key.Label,
                totalQuantity = g.Sum(p => p.Quantity),
                code = g.Key.Code
            }).ToListAsync<dynamic>();

        result = (from r in result
            orderby r.totalQuantity descending
            select r).ToList();
        if (Limit != null && Limit > 0) result = result.Take((int)Limit).ToList();

        return result;
    }

    public List<BestClientWidget> GetBestClientWidget(int Limit)
    {
        var result = db.BestClientWidget.FromSqlRaw("SP_WidgetBestClient").AsNoTracking().Take(Limit).ToList();
        return result;
    }

    public async Task<List<dynamic>> GetBestSalesSubCategory(int Limit, string Lang)
    {
        var riValidAndProgressing =
            await db.ReferenceItem.Where(p => p.Code != "OrderStatus_Refus").Select(p => p.Id).ToListAsync();
        var result = await (from o in db.OrderInfo
            join op in db.OrderProduct on o.Id equals op.OrderId
            join riProduct in db.ReferenceItem on op.ReferenceId equals riProduct.Id
            join rlSecondCategory in db.ReferenceLabel on riProduct.ParentId equals rlSecondCategory.ReferenceItemId
            where riValidAndProgressing.Contains(o.StatusReferenceItemId) && rlSecondCategory.Lang == Lang
            group op by new { rlSecondCategory.ReferenceItemId, rlSecondCategory.Label }
            into g
            select new
            {
                SecondCategoryLabel = g.Key.Label,
                SecondCategoryId = g.Key.ReferenceItemId,
                Sum = g.Sum(x => x.Quantity)
            }).ToListAsync<dynamic>();

        result = result.OrderByDescending(p => p.Sum).Take(Limit).ToList();

        return result;
    }

    public async Task<List<dynamic>> GetSalesPerformanceByYearMonth()
    {
        var riValidAndProgressing =
            await db.ReferenceItem.Where(p => p.Code != "OrderStatus_Refus").Select(p => p.Id).ToListAsync();
        var result = await (from o in db.OrderInfo
            where riValidAndProgressing.Contains(o.StatusReferenceItemId)
            group o by new { o.CreatedOn.Value.Year, o.CreatedOn.Value.Month }
            into g
            select new
            {
                g.Key.Year,
                g.Key.Month,
                Sum = g.Sum(p => p.TotalPrice)
            }).ToListAsync<dynamic>();


        return result;
    }

    public async Task<List<dynamic>> GetAdminSalesPerformanceDashboard(string Lang)
    {
        // var riStatusId = db.ReferenceItem.Where(p => p.Code == "OrderStatus_Valid").Select(p => p.Id).FirstOrDefault();
        var result = await (from riYear in db.ReferenceItem
            join rcYear in db.ReferenceCategory on riYear.ReferenceCategoryId equals rcYear.Id
            where rcYear.ShortLabel == "Year"
            orderby riYear.Value descending
            select new
            {
                Year = riYear.Value,

                Dashboard = (from riMonth in db.ReferenceItem
                    join rcMonth in db.ReferenceCategory on riMonth.ReferenceCategoryId equals rcMonth.Id
                    where rcMonth.ShortLabel == "Month"
                    select new
                    {
                        ProductCommentCount = (from pc in db.ProductComment
                            where pc.CreatedOn.Value.Year == int.Parse(riYear.Value) &&
                                  pc.CreatedOn.Value.Month == int.Parse(riMonth.Value)
                            select pc.Id).Count(),
                        Month = riMonth.Value,
                        Order = (from o in db.OrderInfo
                            where o.CreatedOn.Value.Year == int.Parse(riYear.Value) &&
                                  o.CreatedOn.Value.Month == int.Parse(riMonth.Value)
                            select new
                            {
                                o.Id,
                                o.UserId,
                                o.TotalPrice,
                                o.CreatedOn,
                                OrderStatusId = o.StatusReferenceItemId,
                                OrderStatusCode = db.ReferenceItem.Where(p => p.Id == o.StatusReferenceItemId)
                                    .Select(p => p.Code).FirstOrDefault(),
                                o.OrderTypeId,
                                OrderTypeCode = db.ReferenceItem.Where(p => p.Id == o.OrderTypeId).Select(p => p.Code)
                                    .FirstOrDefault()
                            }).ToList()
                    }).ToList()
            }).ToListAsync<dynamic>();
        return result;
    }

    public class TeamMemberSalesPerformance
    {
        public int UserId { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public float? Sum { get; set; }
    }
}