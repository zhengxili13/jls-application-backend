using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JLSDataAccess.Interfaces;
using JLSDataModel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JLSConsoleApplication.Controllers.AdminService;

[Authorize]
[Route("admin/[controller]/{action}")]
[ApiController]
public class ReferenceController(IReferenceRepository referenceRepository, ILogger<ReferenceController> logger)
    : Controller
{
    [HttpPost]
    public async Task<JsonResult> GetReferenceItemsByCategoryLabels(
        [FromBody] GetReferenceItemsByCategoryLabelsCriteria criteria)
    {
        try
        {
            var result =
                await referenceRepository.GetReferenceItemsByCategoryLabels(criteria.ShortLabels, criteria.Lang);

            return Json(result);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetAllCategoryList(int step, int begin)
    {
        try
        {
            var result = await referenceRepository.GetAllCategoryList();
            var totalCount = result.Count();

            var list = new List<ReferenceCategory>();

            if (step == 0 && begin == 0)
                list = result;
            else
                list = result.Skip(step * begin).Take(step).ToList();


            return Json(new
            {
                ReferenceCategoryList = list,
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
    public async Task<JsonResult> AdvancedSearchReferenceItem(AdvancedSearchReferenceItemCriteria criteria)
    {
        try
        {
            var result = await referenceRepository.AdvancedSearchReferenceItem(criteria.SearchText,
                criteria.ReferenceCategoryId, criteria.Validity, criteria.ParentId, criteria.Lang,
                criteria.IgnoreProduct);
            var totalCount = result.Count();
            var list = result.Skip(criteria.step * criteria.begin).Take(criteria.step);

            return Json(new
            {
                ReferenceItemList = list,
                TotalCount = result.Count()
            });
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetAllReferenceItemWithChildren(string Lang)
    {
        try
        {
            var result = await referenceRepository.GetAllReferenceItemWithChildren(Lang);
            return Json(result);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }


    [HttpGet]
    public async Task<bool> CheckReferenceCodeExists(string Code)
    {
        try
        {
            var result = await referenceRepository.CheckReferenceCodeExists(Code);
            return result;
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    [HttpPost]
    public async Task<JsonResult> SaveReferenceItem([FromBody] SaveReferenceItemCriteria criteria)
    {
        try
        {
            var result = await referenceRepository.SaveReferenceItem(criteria.Id, criteria.CategoryId, criteria.Code,
                criteria.ParentId, criteria.Validity, criteria.Value, criteria.CreatedOrUpdatedBy);

            var ReferenceLabelFrId = await referenceRepository.SaveReferenceLabel(result, criteria.LabelFR, "fr");
            var ReferenceLabelEnId = await referenceRepository.SaveReferenceLabel(result, criteria.LabelEN, "en");
            var ReferenceLabelCnId = await referenceRepository.SaveReferenceLabel(result, criteria.LabelCN, "cn");


            return Json(result);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    public class GetReferenceItemsByCategoryLabelsCriteria
    {
        public GetReferenceItemsByCategoryLabelsCriteria()
        {
            ShortLabels = new List<string>();
        }

        public List<string> ShortLabels { get; set; }
        public string Lang { get; set; }
    }

    public class AdvancedSearchReferenceItemCriteria
    {
        public string SearchText { get; set; }

        public long? ReferenceCategoryId { get; set; }

        public bool? Validity { get; set; }

        public long? ParentId { get; set; }

        public string Lang { get; set; }

        public bool? IgnoreProduct { get; set; }
        public int step { get; set; }

        public int begin { get; set; }
    }

    public class SaveReferenceItemCriteria
    {
        public int? CreatedOrUpdatedBy { get; set; }
        public long Id { get; set; }
        public long CategoryId { get; set; }
        public string Code { get; set; }
        public long? ParentId { get; set; }
        public bool Validity { get; set; }
        public string Value { get; set; }
        public string LabelFR { get; set; }
        public string LabelCN { get; set; }
        public string LabelEN { get; set; }
    }
}