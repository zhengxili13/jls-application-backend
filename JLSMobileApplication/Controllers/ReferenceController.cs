using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JLSDataAccess.Interfaces;
using JLSDataModel.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JLSConsoleApplication.Controllers;

[Route("api/[controller]/{action}")]
[ApiController]
public class ReferenceController(IReferenceRepository referenceRepository, ILogger<ReferenceController> looger)
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
            looger.LogError(e.Message);
            throw;
        }
    }


    [HttpGet]
    public async Task<JsonResult> GetWbesiteslides()
    {
        try
        {
            var result = await referenceRepository.GetWbesiteslides();

            return Json(result);
        }
        catch (Exception e)
        {
            looger.LogError(e.Message);
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
            looger.LogError(e.Message);
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
}