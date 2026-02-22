using System.Collections.Generic;
using System.Threading.Tasks;
using JLSDataModel.AdminViewModel;
using JLSDataModel.Models;
using JLSDataModel.Models.Website;

namespace JLSDataAccess.Interfaces;

public interface IReferenceRepository
{
    Task<List<dynamic>> GetReferenceItemsByCategoryLabels(List<string> shortLabels, string lang);


    Task<ReferenceCategory> GetReferenceCategoryByShortLabel(string ShortLabel);


    Task<List<ReferenceCategory>> GetAllCategoryList();

    Task<List<dynamic>> AdvancedSearchReferenceItem(string SearchText, long? ReferenceCategoryId, bool? Validity,
        long? ParentId, string Lang, bool? IgnoreProduct);

    Task<long> SaveReferenceItem(long ReferenceId, long CategoryId, string Code, long? ParentId, bool Validity,
        string Value, int? CreatedOrUpdatedBy);

    Task<long> SaveReferenceLabel(long ReferenceId, string Label, string Lang);
    /*
     * Admin zoom
     */

    Task<List<dynamic>> GetAllReferenceItemWithChildren(string Lang);

    Task<List<ReferenceItemViewModel>> GetReferenceItemsByCategoryLabelsAdmin(string shortLabels, string lang);

    Task<List<ReferenceCategory>> GetAllReferenceCategory();


    Task<bool> CheckReferenceCodeExists(string Code);
    Task<List<WebsiteSlide>> GetWbesiteslides();
}