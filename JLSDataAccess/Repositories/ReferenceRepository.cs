using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JLSDataAccess.Interfaces;
using JLSDataModel.AdminViewModel;
using JLSDataModel.Models;
using JLSDataModel.Models.Website;
using Microsoft.EntityFrameworkCore;

namespace JLSDataAccess.Repositories;

public class ReferenceRepository : IReferenceRepository
{
    private readonly JlsDbContext db;


    public ReferenceRepository(JlsDbContext context)
    {
        db = context;
    }


    public async Task<ReferenceCategory> GetReferenceCategoryByShortLabel(string ShortLabel)
    {
        return await db.ReferenceCategory.Where(p => p.ShortLabel == ShortLabel).FirstOrDefaultAsync();
    }

    public async Task<List<dynamic>> GetReferenceItemsByCategoryLabels(List<string> shortLabels, string lang)
    {
        var result = from ri in db.ReferenceItem
            join rc in db.ReferenceCategory on ri.ReferenceCategoryId equals rc.Id
            where shortLabels.Contains(rc.ShortLabel)
            orderby ri.Order
            select new
            {
                ri.Id,
                ri.Code,
                ReferenceCategoryId = rc.Id,
                ReferenceCategoryLabel = rc.ShortLabel,
                Label = (from rl in db.ReferenceLabel
                    where rl.ReferenceItemId == ri.Id && rl.Lang == lang
                    select rl.Label).FirstOrDefault(),
                ri.ParentId,
                ri.Validity,
                ri.Value
            };
        return await result.ToListAsync<dynamic>();
    }

    public async Task<long> SaveReferenceItem(long ReferenceId, long CategoryId, string Code, long? ParentId,
        bool Validity, string Value, int? CreatedOrUpdatedBy)
    {
        // TODO Add createdBy / createdOn / UpdatedBy
        ReferenceItem ReferenceToUpdateOrCreate = null;
        if (ReferenceId == 0)
        {
            ReferenceToUpdateOrCreate = new ReferenceItem();
            ReferenceToUpdateOrCreate.CreatedBy = CreatedOrUpdatedBy;
            ReferenceToUpdateOrCreate.CreatedOn = DateTime.Now;
        }
        else
        {
            ReferenceToUpdateOrCreate = db.ReferenceItem.Where(p => p.Id == ReferenceId).FirstOrDefault();
            ReferenceToUpdateOrCreate.UpdatedBy = CreatedOrUpdatedBy;
        }

        if (ReferenceToUpdateOrCreate != null)
        {
            ReferenceToUpdateOrCreate.ReferenceCategoryId = CategoryId;
            ReferenceToUpdateOrCreate.Code = Code;
            ReferenceToUpdateOrCreate.ParentId = ParentId;
            ReferenceToUpdateOrCreate.Validity = Validity;
            ReferenceToUpdateOrCreate.Value = Value;

            if (ReferenceId == 0)
                await db.ReferenceItem.AddAsync(ReferenceToUpdateOrCreate);
            else
                db.ReferenceItem.Update(ReferenceToUpdateOrCreate);
            await db.SaveChangesAsync();
            return ReferenceToUpdateOrCreate.Id;
        }

        return 0;
    }

    public async Task<long> SaveReferenceLabel(long ReferenceId, string Label, string Lang)
    {
        // TODO Add createdBy / createdOn / UpdatedBy

        var ReferenceLabelToUpdateOrCreate = db.ReferenceLabel
            .Where(p => p.ReferenceItemId == ReferenceId && p.Lang == Lang).FirstOrDefault();

        if (ReferenceLabelToUpdateOrCreate == null)
        {
            ReferenceLabelToUpdateOrCreate = new ReferenceLabel();
            ReferenceLabelToUpdateOrCreate.ReferenceItemId = ReferenceId;
        }

        ReferenceLabelToUpdateOrCreate.Label = Label;
        ReferenceLabelToUpdateOrCreate.Lang = Lang;

        if (ReferenceId == 0)
            await db.ReferenceLabel.AddAsync(ReferenceLabelToUpdateOrCreate);
        else
            db.ReferenceLabel.Update(ReferenceLabelToUpdateOrCreate);
        await db.SaveChangesAsync();
        return ReferenceLabelToUpdateOrCreate.Id;
    }

    public async Task<List<ReferenceCategory>> GetAllCategoryList()
    {
        var result = await db.ReferenceCategory.ToListAsync();
        return result;
    }

    public async Task<List<dynamic>> AdvancedSearchReferenceItem(string SearchText, long? ReferenceCategoryId,
        bool? Validity, long? ParentId, string Lang, bool? IgnoreProduct)
    {
        var result = await (from ri in db.ReferenceItem
            from rl in db.ReferenceLabel.Where(p => p.ReferenceItemId == ri.Id).DefaultIfEmpty()
            join rc in db.ReferenceCategory on ri.ReferenceCategoryId equals rc.Id
            where (rl == null || rl.Lang == Lang) &&
                  (SearchText == "" || ri.Code.Contains(SearchText) || rl.Label.Contains(SearchText)) &&
                  (ReferenceCategoryId == null || ri.ReferenceCategoryId == ReferenceCategoryId) &&
                  (Validity == null || ri.Validity == Validity) &&
                  (ParentId == null || ri.ParentId == ParentId) &&
                  (IgnoreProduct == null || IgnoreProduct == false ||
                   (IgnoreProduct == true && rc.ShortLabel != "Product"))
            select new
            {
                ri.Id,
                rl.Label,
                ri.Value,
                ri.Validity,
                ri.Code,
                CategoryId = ri.ReferenceCategoryId,
                Category = (from category in db.ReferenceCategory
                    where category.Id == ri.ReferenceCategoryId
                    select new
                    {
                        category.Id, category.ShortLabel
                    }).FirstOrDefault(),
                Labels = (from l in db.ReferenceLabel
                    where l.ReferenceItemId == ri.Id
                    select new
                    {
                        l.Id,
                        l.Lang,
                        l.Label
                    }).ToList(),
                ri.ParentId,
                ParentReferenceItem = (from rip in db.ReferenceItem
                    join rlp in db.ReferenceLabel on rip.Id equals rlp.ReferenceItemId
                    where rip.Id == ri.ParentId && rlp.Lang == Lang
                    select new
                    {
                        rip.Id,
                        rip.Code,
                        rlp.Label,
                        CategoryId = rip.ReferenceCategoryId
                    }).FirstOrDefault()
            }).ToListAsync<dynamic>();

        return result;
    }

    public async Task<List<dynamic>> GetAllReferenceItemWithChildren(string Lang)
    {
        var result = await (from ri in db.ReferenceItem
            join rl in db.ReferenceLabel on ri.Id equals rl.ReferenceItemId
            join rc in db.ReferenceCategory on ri.ReferenceCategoryId equals rc.Id
            where rl.Lang == Lang && rc.ShortLabel != "Product" // todo place all into an  configuration table
            select new
            {
                ri.Id,
                ri.Code,
                rl.Label,
                CategoryId = ri.ReferenceCategoryId
            }).ToListAsync<dynamic>();
        return result.Distinct().ToList();
    }

    /*
     * Admin Zoom
     */
    public async Task<List<ReferenceItemViewModel>> GetReferenceItemsByCategoryLabelsAdmin(string shortLabels,
        string lang)
    {
        var referenceLabelList =
            new List<string>(shortLabels.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries));

        var result = from ri in db.ReferenceItem
            join rc in db.ReferenceCategory on ri.ReferenceCategoryId equals rc.Id
            from rl in db.ReferenceLabel.Where(p => p.ReferenceItemId == ri.Id && p.Lang == lang).DefaultIfEmpty()
            where referenceLabelList.Contains(rc.ShortLabel)
            select new ReferenceItemViewModel
            {
                Id = ri.Id,
                Code = ri.Code,
                Value = ri.Value,
                Order = ri.Order,
                ParentId = ri.ParentId,
                Label = rl.Label,
                Lang = rl.Lang,
                Category = rc.ShortLabel,
                Validity = ri.Validity
            };
        return await result.ToListAsync();
    }

    public async Task<List<ReferenceCategory>> GetAllReferenceCategory()
    {
        var result = await (from rc in db.ReferenceCategory
            select rc).ToListAsync();

        return result;
    }


    /*
     * Check code of referenceitem is not used yet (todo: add a constraint sql server side)
     */
    public async Task<bool> CheckReferenceCodeExists(string Code)
    {
        var result = await db.ReferenceItem.Where(p => p.Code == Code).FirstOrDefaultAsync();

        return result != null ? true : false;
    }

    /*
     * Get website and mobile slide from db
     */
    public async Task<List<WebsiteSlide>> GetWbesiteslides()
    {
        var result = await db.WebsiteSlide.ToListAsync();

        return result;
    }
}