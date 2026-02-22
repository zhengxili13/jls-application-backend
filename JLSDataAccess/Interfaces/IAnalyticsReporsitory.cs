using System.Collections.Generic;
using System.Threading.Tasks;
using JLSDataModel.Models.Analytics;

namespace JLSDataAccess.Interfaces;

public interface IAnalyticsReporsitory
{
    Task<List<dynamic>> GetAdminSalesPerformanceDashboard(string Lang);

    Task<List<dynamic>> GetTeamMemberSalesPerformance();

    Task<List<dynamic>> GetInternalExternalSalesPerformance(string Lang);

    Task<List<dynamic>> GetSalesPerformanceByStatus(string Lang);

    Task<List<dynamic>> GetSalesPerformanceByYearMonth();


    Task<List<dynamic>> GetTopSaleProduct(string Lang, int? Limit);

    Task<List<dynamic>> GetBestSalesSubCategory(int Limit, string Lang);

    List<BestClientWidget> GetBestClientWidget(int Limit);

    Task<dynamic> GetVisitorAndClientInfo();
}