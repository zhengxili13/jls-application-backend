using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using JLSDataAccess.Interfaces;
using JLSDataModel.AdminViewModel;
using JLSDataModel.Models;
using JLSDataModel.Models.Order;
using JLSDataModel.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JLSDataAccess.Repositories;

public class OrderRepository(JlsDbContext context, ILogger<OrderRepository> logger) : IOrderRepository
{
    /*
     * Mobile Zoom
     */
    public async Task<long> SaveOrder(List<OrderProductViewModelMobile> References, long ShippingAdressId,
        long FacturationAdressId, int UserId, long? ClientRemarkId, long CutomerInfoId)
    {
        /* Step1: get progressing status ri */
        var status = await (from ri in context.ReferenceItem
            join rc in context.ReferenceCategory on ri.ReferenceCategoryId equals rc.Id
            where rc.ShortLabel == "OrderStatus" && ri.Code == "OrderStatus_Progressing"
            select ri).FirstOrDefaultAsync();

        var orderType = await (from ri in context.ReferenceItem
            join rc in context.ReferenceCategory on ri.ReferenceCategoryId equals rc.Id
            where rc.ShortLabel == "OrderType" && ri.Code == "OrderType_External"
            select ri).FirstOrDefaultAsync();


        var TaxRate = await (from ri in context.ReferenceItem
            join rc in context.ReferenceCategory on ri.ReferenceCategoryId equals rc.Id
            where rc.ShortLabel == "TaxRate" && ri.Validity == true
            orderby ri.Order descending
            select ri).FirstOrDefaultAsync();

        /* Step2: construct the orderInfo object */
        var Order = new OrderInfo();
        Order.FacturationAdressId = FacturationAdressId;
        Order.ShippingAdressId = ShippingAdressId;
        Order.UserId = UserId;
        Order.StatusReferenceItemId = status.Id;
        Order.OrderTypeId = orderType.Id;
        Order.TaxRateId = TaxRate.Id;

        Order.CreatedBy = UserId;
        Order.CreatedOn = DateTime.Now;

        Order.ClientRemarkId = ClientRemarkId;
        Order.CustomerId = CutomerInfoId;

        await context.AddAsync(Order);
        await context.SaveChangesAsync();

        /* Step3: add OrderInfoStatusLog*/

        var orderInfoStatusLog = new OrderInfoStatusLog();

        orderInfoStatusLog.OrderInfoId = Order.Id;

        orderInfoStatusLog.NewStatusId = Order.StatusReferenceItemId;
        orderInfoStatusLog.UserId = UserId;
        orderInfoStatusLog.ActionTime = DateTime.Now;
        context.OrderInfoStatusLog.Add(orderInfoStatusLog);

        await context.SaveChangesAsync();

        /* Step4: Add product */
        float TotalPrice = 0;
        var OrderProducts = new List<OrderProduct>();
        foreach (var r in References)
        {
            var product = await context.Product.Where(p => p.ReferenceItemId == r.ReferenceId).FirstOrDefaultAsync();
            var op = new OrderProduct
            {
                OrderId = Order.Id,
                Quantity = r.Quantity,
                ReferenceId = r.ReferenceId,
                UnitPrice = double.Parse(r.Price.Value.ToString("0.00")),
                Colissage = r.QuantityPerBox != 0
                    ? r.QuantityPerBox
                    : (int)product.QuantityPerBox // todo check if QuantityPerBox exists or not
            };
            OrderProducts.Add(op);
            TotalPrice = r.Quantity * r.Price.Value * r.UnityQuantity + TotalPrice;
        }

        await context.AddRangeAsync(OrderProducts);
        await context.SaveChangesAsync();

        var taxRate = context.ReferenceItem.Where(p => p.Code == "TaxRate_20%").Select(p => p.Value).FirstOrDefault();
        var tax = float.Parse(taxRate) * 0.01;
        Order.TotalPrice = (float?)(TotalPrice * (1 + (taxRate != null ? tax : 0)));
        Order.TotalPriceHT = TotalPrice;
        context.Update(Order);
        await context.SaveChangesAsync();
        // Return new orderId
        return Order.Id;
    }


    public async Task<long> SaveAdminOrder(OrderInfo order, List<OrderProductViewModelMobile> References,
        int CreatedOrUpdatedBy)
    {
        try
        {
            OrderInfo orderToUpdate = null;
            if (order.Id == 0)
            {
                orderToUpdate = new OrderInfo();

                orderToUpdate.CreatedBy = CreatedOrUpdatedBy;
                orderToUpdate.CreatedOn = DateTime.Now;

                orderToUpdate.UserId = CreatedOrUpdatedBy;
            }
            else
            {
                orderToUpdate = await context.OrderInfo.AsNoTracking().Where(x => x.Id == order.Id)
                    .FirstOrDefaultAsync();
                var oldOrder = await context.OrderInfo.FindAsync(order.Id);
                orderToUpdate.UpdatedBy = CreatedOrUpdatedBy;

                if (oldOrder.StatusReferenceItemId != order.StatusReferenceItemId)
                {
                    var orderInfoStatusLog = new OrderInfoStatusLog();

                    orderInfoStatusLog.OrderInfoId = order.Id;
                    orderInfoStatusLog.OldStatusId = oldOrder.StatusReferenceItemId;
                    orderInfoStatusLog.NewStatusId = order.StatusReferenceItemId;
                    orderInfoStatusLog.UserId = CreatedOrUpdatedBy;
                    orderInfoStatusLog.ActionTime = DateTime.Now;

                    context.OrderInfoStatusLog.Add(orderInfoStatusLog);
                    await context.SaveChangesAsync();
                }
            }

            orderToUpdate.AdminRemarkId = order.AdminRemarkId;
            orderToUpdate.ClientRemarkId = order.ClientRemarkId;
            orderToUpdate.FacturationAdressId = order.FacturationAdressId;
            orderToUpdate.ShipmentInfoId = order.ShipmentInfoId;
            orderToUpdate.StatusReferenceItemId = order.StatusReferenceItemId;
            orderToUpdate.TaxRateId = order.TaxRateId;
            orderToUpdate.CustomerId = order.CustomerId;
            orderToUpdate.UserId = order.UserId;

            if (order.Id > 0)
            {
                context.Update(order);

                await context.SaveChangesAsync();
            }
            else
            {
                await context.OrderInfo.AddAsync(order);
                await context.SaveChangesAsync();

                var orderInfoStatusLog = new OrderInfoStatusLog();

                orderInfoStatusLog.OrderInfoId = order.Id;

                orderInfoStatusLog.NewStatusId = order.StatusReferenceItemId;
                orderInfoStatusLog.UserId = CreatedOrUpdatedBy;
                orderInfoStatusLog.ActionTime = DateTime.Now;

                context.OrderInfoStatusLog.Add(orderInfoStatusLog);

                await context.SaveChangesAsync();
            }


            /* Step 1: remove all the product in order */
            var PreviousOrderProducts = await context.OrderProduct.Where(p => p.OrderId == order.Id).ToListAsync();
            context.RemoveRange(PreviousOrderProducts);

            float TotalPrice = 0;
            var products = new List<OrderProduct>();
            /* Step 2: Add product in order */
            if (References.Count() > 0)
                foreach (var item in References)
                {
                    var reference = new OrderProduct();
                    reference.ReferenceId = item.ReferenceId;
                    reference.Quantity = item.Quantity;
                    reference.UnitPrice = double.Parse(item.Price.Value.ToString("0.00"));
                    reference.OrderId = order.Id;
                    reference.Colissage = item.QuantityPerBox; // todo: add check here 
                    reference.TotalPrice =
                        reference.Quantity * reference.UnitPrice * reference.Colissage; // todo add check here 

                    TotalPrice = TotalPrice + item.Price.Value * item.Quantity * item.UnityQuantity;

                    products.Add(reference);
                }


            await context.AddRangeAsync(products);
            var taxRate = context.ReferenceItem.Where(p => p.Code == "TaxRate_20%").Select(p => p.Value)
                .FirstOrDefault();
            var tax = float.Parse(taxRate) * 0.01;
            order.TotalPrice = (float?)(TotalPrice * (1 + (taxRate != null ? tax : 0)));
            order.TotalPriceHT = TotalPrice;

            context.Update(order);

            await context.SaveChangesAsync();

            // Return new orderId
            return order.Id;
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    public async Task<List<OrderListViewModelMobile>> GetOrdersListByUserId(int UserId, string StatusCode, string Lang)
    {
        var result = await (from o in context.OrderInfo
            join riStatus in context.ReferenceItem on o.StatusReferenceItemId equals riStatus.Id
            where o.UserId == UserId && (StatusCode == "All" || riStatus.Code == StatusCode)
            orderby o.CreatedOn descending
            select new OrderListViewModelMobile
            {
                Id = o.Id,
                CreatedOn = o.CreatedOn,
                TotalPrice = o.TotalPrice,
                NumberOfProduct = context.OrderProduct.Where(p => p.OrderId == o.Id).Sum(p => p.Quantity),
                ShippingAdressId = o.ShippingAdressId,
                ShippingAdress = (from a in context.Adress
                    where a.Id == o.ShippingAdressId
                    select a).FirstOrDefault(),
                StatusCode = (from ri in context.ReferenceItem
                    where ri.Id == o.StatusReferenceItemId
                    select ri.Code).FirstOrDefault(),
                StatusLabel = (from rl in context.ReferenceLabel
                    where rl.ReferenceItemId == o.StatusReferenceItemId && rl.Lang == Lang
                    select rl.Label).FirstOrDefault()
            }).ToListAsync();
        return result;
    }


    public async Task<dynamic> GetOrdersListByOrderId(long OrderId, string Lang)
    {
        var result = await (from o in context.OrderInfo
            where o.Id == OrderId
            select new
            {
                OrderInfo = new OrderInfo
                {
                    Id = o.Id,
                    UserId = o.UserId,
                    User = (from u in context.Users
                        where u.Id == o.UserId
                        select u).FirstOrDefault(),
                    TaxRateId = o.TaxRateId,
                    TotalPriceHT = o.TotalPriceHT,
                    TotalPrice = o.TotalPrice,
                    ClientRemarkId = o.ClientRemarkId,
                    AdminRemarkId = o.AdminRemarkId,
                    PaymentInfo = o.PaymentInfo,
                    CreatedOn = o.CreatedOn,
                    UpdatedOn = o.UpdatedOn,
                    OrderTypeId = o.OrderTypeId,
                    ShipmentInfoId = o.ShipmentInfoId
                },
                ShippingMessage = (from riShippingMessage in context.ReferenceItem
                    join rlShippingMessage in context.ReferenceLabel on riShippingMessage.Id equals rlShippingMessage
                        .ReferenceItemId
                    where rlShippingMessage.Lang == Lang && riShippingMessage.Code == "ShippingMessage"
                    select rlShippingMessage.Label).FirstOrDefault(),
                ClientRemark = (from clientRemark in context.Remark
                    where clientRemark.Id == o.ClientRemarkId
                    select clientRemark).FirstOrDefault(),
                AdminRemark = (from adminRemark in context.Remark
                    where adminRemark.Id == o.AdminRemarkId
                    select adminRemark).FirstOrDefault(),
                ShipmentInfo = (from shipmentInfo in context.ShipmentInfo
                    where shipmentInfo.Id == o.ShipmentInfoId
                    select shipmentInfo).FirstOrDefault(),
                TaxRate = (from taxRateRi in context.ReferenceItem
                    where taxRateRi.Id == o.TaxRateId
                    select new
                    {
                        taxRateRi.Id,
                        taxRateRi.Code,
                        taxRateRi.Value
                    }).FirstOrDefault(),

                CustomerInfo = (from customer in context.CustomerInfo
                    where customer.Id == o.CustomerId
                    select customer).FirstOrDefault(),
                OrderType = (from riOrderType in context.ReferenceItem
                    join rlOrderType in context.ReferenceLabel on riOrderType.Id equals rlOrderType.ReferenceItemId
                    where rlOrderType.Lang == Lang && riOrderType.Id == o.OrderTypeId
                    select new
                    {
                        riOrderType.Id,
                        riOrderType.Code,
                        rlOrderType.Label
                    }).FirstOrDefault(),
                Status = (from ri in context.ReferenceItem
                    join rl in context.ReferenceLabel on ri.Id equals rl.ReferenceItemId
                    where rl.Lang == Lang && ri.Id == o.StatusReferenceItemId
                    select new ReferenceItemViewModel
                    {
                        Id = ri.Id,
                        Label = rl.Label,
                        Code = ri.Code
                    }).FirstOrDefault(),
                StatusInfo = (from statusInfo in context.OrderInfoStatusLog
                    where statusInfo.OrderInfoId == o.Id
                    orderby statusInfo.ActionTime descending
                    select new
                    {
                        statusInfo.Id,
                        OldStatus = (from rlOld in context.ReferenceLabel
                            join riOld in context.ReferenceItem on rlOld.ReferenceItemId equals riOld.Id
                            where rlOld.ReferenceItemId == statusInfo.OldStatusId && rlOld.Lang == Lang
                            select new
                            {
                                ReferenceId = rlOld.ReferenceItemId,
                                riOld.Code,
                                rlOld.Label
                            }).FirstOrDefault(),
                        NewStatus = (from rlNew in context.ReferenceLabel
                            join riNew in context.ReferenceItem on rlNew.ReferenceItemId equals riNew.Id
                            where rlNew.ReferenceItemId == statusInfo.NewStatusId && rlNew.Lang == Lang
                            select new
                            {
                                ReferenceId = rlNew.ReferenceItemId,
                                riNew.Code,
                                rlNew.Label
                            }).FirstOrDefault(),
                        statusInfo.ActionTime,
                        statusInfo.UserId,
                        UserName = (from u in context.Users
                            where u.Id == statusInfo.UserId
                            select u.UserName).FirstOrDefault()
                    }).ToList(),
                FacturationAdress = context.Adress.Where(p => p.Id == o.FacturationAdressId).FirstOrDefault(),
                ShippingAdress = context.Adress.Where(p => p.Id == o.ShippingAdressId).FirstOrDefault(),
                ProductList = (from op in context.OrderProduct
                    join p in context.Product on op.ReferenceId equals p.ReferenceItemId
                    join riProduct in context.ReferenceItem on p.ReferenceItemId equals riProduct.Id
                    join rc in context.ReferenceCategory on riProduct.ReferenceCategoryId equals rc.Id
                    join rl in context.ReferenceLabel on riProduct.Id equals rl.ReferenceItemId
                    where op.OrderId == o.Id && rc.ShortLabel == "Product" && rl.Lang == Lang
                    select new
                    {
                        UnityQuantity = op.Colissage,
                        op.Quantity,
                        ProductId = p.Id,
                        ReferenceId = riProduct.Id,
                        riProduct.Code,
                        riProduct.ParentId,
                        riProduct.Value,
                        riProduct.Order,
                        rl.Label,
                        Price = op.UnitPrice,
                        op.TotalPrice,
                        IsModifiedPriceOrBox =
                            !(op.Colissage == p.QuantityPerBox && Math.Abs(op.UnitPrice.Value - p.Price.Value) < 0.001)
                                ? true
                                : false,
                        QuantityPerBox = op.Colissage != 0 ? op.Colissage : p.QuantityPerBox,
                        p.QuantityPerParcel,
                        p.MinQuantity,
                        p.Size,
                        p.Color,
                        p.Material,
                        DefaultPhotoPath = (from path in context.ProductPhotoPath
                            where path.ProductId == p.Id
                            select path.Path).FirstOrDefault(),
                        PhotoPath = (from path in context.ProductPhotoPath
                            where path.ProductId == p.Id
                            select new ProductListPhotoPath { Path = path.Path }).ToList()
                    }).ToList()
            }).FirstOrDefaultAsync();
        return result;
    }


    /*
     * Admin Zoom
     */

    public async Task<List<dynamic>> AdvancedOrderSearchByCriteria(string Lang, int? UserId, DateTime? FromDate,
        DateTime? ToDate, string OrderId, long? StatusId)
    {
        var result = await (from order in context.OrderInfo
            from statusRi in context.ReferenceItem.Where(p => p.Id == order.StatusReferenceItemId).DefaultIfEmpty()
            where (StatusId == null || statusRi.Id == StatusId)
                  && (UserId == null || order.UserId == UserId)
                  && (OrderId == null || order.Id.ToString().Contains(OrderId))
                  && (FromDate == null || order.CreatedOn >= FromDate)
                  && (ToDate == null || order.CreatedOn <= ToDate)
            orderby order.CreatedOn descending
            select new
            {
                order.Id,
                User = (from u in context.Users
                    where u.Id == order.UserId
                    select u).FirstOrDefault(),
                order.UpdatedBy,
                UpdatedByUser = (from u in context.Users
                    where u.Id == order.UpdatedBy
                    select u).FirstOrDefault(),
                CustomerInfo = (from customer in context.CustomerInfo
                    where customer.Id == order.CustomerId
                    select customer).FirstOrDefault(),
                StatusId = statusRi.Id,
                Status = (from statusLabel in context.ReferenceLabel
                    where statusLabel.ReferenceItemId == statusRi.Id && statusLabel.Lang == Lang
                    select new
                    {
                        statusRi.Id,
                        statusLabel.Label,
                        statusRi.Code
                    }).FirstOrDefault(),
                ShippingAddress = context.Adress.Where(p => p.Id == order.ShippingAdressId).FirstOrDefault(),
                order.CreatedOn,
                order.UpdatedOn,
                order.TotalPrice,
                order.TotalPriceHT,
                OrderType = (from orderTypeRi in context.ReferenceItem
                    join orderTypeRl in context.ReferenceLabel on orderTypeRi.Id equals orderTypeRl.ReferenceItemId
                    where orderTypeRi.Id == order.OrderTypeId && orderTypeRl.Lang == Lang
                    select new
                    {
                        orderTypeRi.Id,
                        orderTypeRi.Code,
                        orderTypeRl.Label
                    }).FirstOrDefault(),

                UpdatedUser = (from uu in context.Users
                    where uu.Id == order.UpdatedBy
                    select uu).FirstOrDefault()
            }).ToListAsync<dynamic>();
        return result;
    }

    public async Task<long> SaveOrderRemark(Remark remark, int? CreatedOrUpadatedBy)
    {
        if (remark.Id != 0)
        {
            remark.UpdatedBy = CreatedOrUpadatedBy;
            context.Remark.Update(remark);
        }
        else
        {
            remark.CreatedBy = CreatedOrUpadatedBy;
            remark.CreatedOn = DateTime.Now;
            await context.Remark.AddAsync(remark);
        }

        await context.SaveChangesAsync();
        return remark.Id;
    }

    public async Task<long> SaveCustomerInfo(CustomerInfo customer, int? CreatedOrUpadatedBy)
    {
        var previousCustomerInfo = context.CustomerInfo.Where(p => p.UserId == CreatedOrUpadatedBy).FirstOrDefault();
        if (previousCustomerInfo != null)
        {
            context.Entry(previousCustomerInfo).State = EntityState.Detached;
            customer.Id = previousCustomerInfo.Id;
        }

        if (customer.Id != 0)
        {
            customer.UpdatedBy = CreatedOrUpadatedBy;
            context.CustomerInfo.Update(customer);
        }
        else
        {
            customer.CreatedBy = CreatedOrUpadatedBy;
            customer.CreatedOn = DateTime.Now;
            await context.CustomerInfo.AddAsync(customer);
        }

        await context.SaveChangesAsync();
        return customer.Id;
    }

    public async Task<long> SaveOrderShipmentInfo(ShipmentInfo shipment, int? CreatedOrUpadatedBy)
    {
        if (shipment.Id != 0)
        {
            shipment.UpdatedBy = CreatedOrUpadatedBy;

            context.ShipmentInfo.Update(shipment);
        }
        else
        {
            shipment.CreatedBy = CreatedOrUpadatedBy;
            shipment.CreatedOn = DateTime.Now;

            await context.ShipmentInfo.AddAsync(shipment);
        }

        await context.SaveChangesAsync();

        return shipment.Id;
    }

    public async Task<List<OrdersListViewModel>> GetAllOrdersWithInterval(string lang, int intervalCount, int size,
        string orderActive, string orderDirection)
    {
        var request = from order in context.OrderInfo
            join user in context.Users on order.UserId equals user.Id
            select new OrdersListViewModel
            {
                Id = order.Id,
                OrderReferenceCode = order.OrderReferenceCode,
                EntrepriseName = user.EntrepriseName,
                UserName = user.UserName,
                TotalPrice = order.TotalPrice,
                Date = order.CreatedOn,
                StatusReferenceItemLabel = (from ri in context.ReferenceItem
                    where ri.Id == order.StatusReferenceItemId
                    from rl in context.ReferenceLabel
                        .Where(rl => rl.ReferenceItemId == ri.Id && rl.Lang == lang).DefaultIfEmpty()
                    select rl.Label).FirstOrDefault()
            };

        if (orderActive == "null" || orderActive == "undefined" || orderDirection == "null")
            return await request.Skip(intervalCount * size).Take(size).ToListAsync();

        Expression<Func<OrdersListViewModel, object>> funcOrder; // TOdo: check !!

        switch (orderActive)
        {
            case "id":
                funcOrder = order => order.Id;
                break;
            case "reference":
                funcOrder = order => order.OrderReferenceCode;
                break;
            case "name":
                funcOrder = order => order.UserName;
                break;
            case "entrepriseName":
                funcOrder = order => order.EntrepriseName;
                break;
            case "total":
                funcOrder = order => order.TotalPrice;
                break;
            case "status":
                funcOrder = order => order.StatusReferenceItemLabel;
                break;
            case "date":
                funcOrder = order => order.Date;
                break;
            default:
                funcOrder = order => order.Id;
                break;
        }

        if (orderDirection == "asc")
            request = request.OrderBy(funcOrder);
        else
            request = request.OrderByDescending(funcOrder);

        var result = await request.Skip(intervalCount * size).Take(size).ToListAsync();


        return result;
    }

    public async Task<OrderViewModel> GetOrderById(long id, string lang)
    {
        var result = await (from order in context.OrderInfo
            where order.Id == id
            join sa in context.Adress on order.ShippingAdressId equals sa.Id
            join fa in context.Adress on order.FacturationAdressId equals fa.Id
            join user in context.Users on order.UserId equals user.Id
            join ris in context.ReferenceItem on order.StatusReferenceItemId equals ris.Id
            from rls in context.ReferenceLabel.Where(rls => rls.ReferenceItemId == ris.Id
                                                            && rls.Lang == lang).DefaultIfEmpty()
            select new OrderViewModel
            {
                OrderReferenceCode = order.OrderReferenceCode,
                PaymentInfo = order.PaymentInfo,
                // TaxRateId = order.TaxRateId,
                TotalPrice = order.TotalPrice,
                // AdminRemarkId = order.AdminRemarkId,
                //  ClientRemark = order.ClientRemarkId,
                StatusLabel = rls.Label,
                StatusReferenceItem = ris,
                User = new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    EntrepriseName = user.EntrepriseName,
                    Name = user.UserName,
                    Telephone = user.PhoneNumber
                },
                FacturationAdress = fa,
                ShippingAdress = sa,
                Products = (from po in context.OrderProduct
                    where po.OrderId == order.Id
                    join rip in context.ReferenceItem on po.ReferenceId equals rip.Id
                    join pi in context.Product on rip.Id equals pi.ReferenceItemId
                    from img in context.ProductPhotoPath.Where(img => img.ProductId == pi.Id)
                        .Take(1).DefaultIfEmpty()
                    from rlp in context.ReferenceLabel.Where(rlp => rlp.ReferenceItemId == rip.Id
                                                                    && rlp.Lang == lang).Take(1).DefaultIfEmpty()
                    select new OrderProductViewModel
                    {
                        Id = pi.Id,
                        Image = img.Path,
                        Name = rlp.Label,
                        Price = pi.Price,
                        Quantity = po.Quantity,
                        ReferenceCode = rip.Code
                    }).ToList()
            }).FirstOrDefaultAsync();

        return result;
    }

    public async Task<dynamic> GetCustomerInfoList()
    {
        var result = await (from c in context.CustomerInfo
                join o in context.OrderInfo on c.Id equals o.CustomerId
                orderby c.EntrepriseName
                select new
                {
                    c.Id,
                    c.Email,
                    c.PhoneNumber,
                    c.EntrepriseName,
                    c.Siret,
                    PreviousShippingAddress = (from a in context.Adress
                        join o in context.OrderInfo on a.Id equals o.ShippingAdressId
                        where o.CustomerId == c.Id
                        select a).FirstOrDefault(),
                    PreviousFacturationAddress = (from a in context.Adress
                        join o in context.OrderInfo on a.Id equals o.FacturationAdressId
                        where o.CustomerId == c.Id
                        select a).FirstOrDefault()
                }
            ).ToListAsync<dynamic>();
        return result.Distinct().ToList();
    }
}