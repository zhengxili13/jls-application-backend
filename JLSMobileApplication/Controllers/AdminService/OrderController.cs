using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JLSApplicationBackend.Services;
using JLSDataAccess;
using JLSDataAccess.Interfaces;
using JLSDataModel.Models;
using JLSDataModel.Models.Adress;
using JLSDataModel.Models.Order;
using JLSDataModel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JLSApplicationBackend.Controllers.AdminService;

[Authorize]
[Route("admin/[controller]/{action}")]
[ApiController]
public class OrderController(
    IOrderRepository orderRepository,
    IAdressRepository adressRepository,
    JlsDbContext context,
    ISendEmailAndMessageService sendEmailAndMessageService,
    IWebHostEnvironment env,
    ILogger<OrderController> logger)
    : Controller
{
    [HttpPost]
    public async Task<JsonResult> AdvancedOrderSearchByCriteria(AdvancedOrderSearchCriteria criteria)
    {
        try
        {
            var result = await orderRepository.AdvancedOrderSearchByCriteria(criteria.Lang, criteria.UserId,
                criteria.FromDate, criteria.ToDate, criteria.OrderId, criteria.StatusId);
            var list = result.Skip(criteria.begin * criteria.step).Take(criteria.step);

            return Json(new
            {
                OrderList = list,
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
    public async Task<JsonResult> SaveAdminOrder(SaveAdminOrderCriteria criteria)
    {
        try
        {
            var orderInfo = criteria.Orderinfo;

            /* Step1 : save shipping address */
            orderInfo.ShippingAdressId = await SaveAddress(criteria.ShippingAddress, criteria.CreatedOrUpdatedBy);

            /* Step2 : save facturation address */
            orderInfo.FacturationAdressId = await SaveAddress(criteria.ShippingAddress, criteria.CreatedOrUpdatedBy);

            /* Step3: save shipment info */
            if (criteria.ShipmentInfo != null)
            {
                orderInfo.ShipmentInfoId = await orderRepository.SaveOrderShipmentInfo(criteria.ShipmentInfo, criteria.CreatedOrUpdatedBy);
            }

            /* Step4: save Admin remark info */
            if (criteria.AdminRemark != null)
            {
                orderInfo.AdminRemarkId = await orderRepository.SaveOrderRemark(criteria.AdminRemark, criteria.CreatedOrUpdatedBy);
            }

            if (criteria.CustomerInfo != null)
            {
                orderInfo.CustomerId = await orderRepository.SaveCustomerInfo(criteria.CustomerInfo, criteria.CreatedOrUpdatedBy);
            }

            /* Step5: save Admin remark info */
            if (criteria.ClientRemark != null)
            {
                orderInfo.ClientRemarkId = await orderRepository.SaveOrderRemark(criteria.ClientRemark, criteria.CreatedOrUpdatedBy);
            }


            OrderInfo orderToUpdate = null;
            if (orderInfo.Id == 0)
            {
                orderToUpdate = new OrderInfo
                {
                    CreatedBy = criteria.CreatedOrUpdatedBy,
                    CreatedOn = DateTime.Now,
                    UserId = criteria.CreatedOrUpdatedBy
                };
            }
            else
            {
                orderToUpdate = await context.OrderInfo.FindAsync(orderInfo.Id);
                var oldOrder = await context.OrderInfo.FindAsync(orderInfo.Id);
                orderToUpdate.UpdatedBy = criteria.CreatedOrUpdatedBy;

                if (IsOrderStatusChanged(oldOrder, orderInfo))
                {
                    await UpdateOrderStatus(criteria, orderInfo, oldOrder);
                }
            }

            orderToUpdate.AdminRemarkId = orderInfo.AdminRemarkId;
            orderToUpdate.ClientRemarkId = orderInfo.ClientRemarkId;
            orderToUpdate.FacturationAdressId = orderInfo.FacturationAdressId;
            orderToUpdate.ShippingAdressId = orderInfo.ShippingAdressId;
            orderToUpdate.OrderTypeId = orderInfo.OrderTypeId != null && orderInfo.OrderTypeId > 0
                ? orderInfo.OrderTypeId
                : await context.ReferenceItem.Where(p => p.Code == "OrderType_Internal").Select(p => p.Id)
                    .FirstOrDefaultAsync();
            orderToUpdate.ShipmentInfoId = orderInfo.ShipmentInfoId;
            orderToUpdate.StatusReferenceItemId = orderInfo.StatusReferenceItemId;
            orderToUpdate.TaxRateId = orderInfo.TaxRateId;
            orderToUpdate.CustomerId = orderInfo.CustomerId;

            if (orderInfo.Id == 0 && !env.IsDevelopment())
            {
                await context.AddAsync(orderToUpdate);
                await context.SaveChangesAsync();

                var orderInfoStatusLog = new OrderInfoStatusLog();

                orderInfoStatusLog.OrderInfoId = orderToUpdate.Id;

                orderInfoStatusLog.NewStatusId = orderToUpdate.StatusReferenceItemId;
                orderInfoStatusLog.UserId = criteria.CreatedOrUpdatedBy;
                orderInfoStatusLog.ActionTime = DateTime.Now;

                context.OrderInfoStatusLog.Add(orderInfoStatusLog);

                await context.SaveChangesAsync();
            }
            else
            {
                context.Update(orderToUpdate);
                await context.SaveChangesAsync();
            }

            /* Step 1: remove all the product in order */
            var PreviousOrderProducts =
                await context.OrderProduct.Where(p => p.OrderId == orderToUpdate.Id).ToListAsync();
            context.RemoveRange(PreviousOrderProducts);

            float TotalPrice = 0;
            var OrderProducts = new List<OrderProduct>();
            foreach (var r in criteria.References)
            {
                var product = await context.Product.Where(p => p.ReferenceItemId == r.ReferenceId)
                    .FirstOrDefaultAsync();
                var op = new OrderProduct
                {
                    OrderId = orderToUpdate.Id,
                    Quantity = r.Quantity,
                    ReferenceId = r.ReferenceId,
                    UnitPrice = double.Parse(r.Price.Value.ToString("0.00")),
                    Colissage = r.QuantityPerBox != 0
                        ? r.QuantityPerBox
                        : (int)product.QuantityPerBox, // todo check if QuantityPerBox exists or not
                    TotalPrice = r.Quantity * r.Price.Value * r.QuantityPerBox
                };
                OrderProducts.Add(op);
                TotalPrice = r.Quantity * r.Price.Value * op.Colissage + TotalPrice;
            }

            await context.AddRangeAsync(OrderProducts);
            await context.SaveChangesAsync();

            var taxRate = context.ReferenceItem.Where(p => p.Code == "TaxRate_20%").Select(p => p.Value)
                .FirstOrDefault();
            var tax = float.Parse(taxRate) * 0.01;
            orderToUpdate.TotalPrice = (float?)(TotalPrice * (1 + (taxRate != null ? tax : 0)));
            orderToUpdate.TotalPriceHT = TotalPrice;

            context.Update(orderToUpdate);

            await context.SaveChangesAsync();
            if (orderInfo.Id == 0)
                // JLS ask for sending email only for new order, the order update not need to send email for jls.
                // TODO: place a better method to send email according to updated information 
                await sendEmailAndMessageService.CreateOrUpdateOrderAsync(orderToUpdate.Id,
                    orderInfo.Id == 0 ? "CreateNewOrder" : "UpdateOrder");
            return Json(orderToUpdate.Id);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    private static bool IsOrderStatusChanged(OrderInfo oldOrder, OrderInfo orderInfo)
    {
        return oldOrder.StatusReferenceItemId != orderInfo.StatusReferenceItemId;
    }

    private async Task UpdateOrderStatus(SaveAdminOrderCriteria criteria, OrderInfo orderInfo, OrderInfo oldOrder)
    {
        var orderInfoStatusLog = new OrderInfoStatusLog
        {
            OrderInfoId = orderInfo.Id,
            OldStatusId = oldOrder.StatusReferenceItemId,
            NewStatusId = orderInfo.StatusReferenceItemId,
            UserId = criteria.CreatedOrUpdatedBy,
            ActionTime = DateTime.Now
        };

        context.OrderInfoStatusLog.Add(orderInfoStatusLog);
        await context.SaveChangesAsync();
    }

    private async Task<long> SaveAddress(Adress address, long createdOrUpdatedBy)
    {
        if (address is { Id: 0 })
        {
            address.CreatedBy = createdOrUpdatedBy;
            address.CreatedOn = DateTime.Now;
        }
        else
        {
            address.UpdatedBy = createdOrUpdatedBy;
        }
        return await adressRepository.CreateOrUpdateAdress(address);
    }

    [HttpGet]
    public async Task<JsonResult> GetCustomerInfoList()
    {
        try
        {
            return Json(await orderRepository.GetCustomerInfoList());
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    public class AdvancedOrderSearchCriteria
    {
        public string Lang { get; set; }
        public int? UserId { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public long? StatusId { get; set; }

        public string OrderId { get; set; }

        public int begin { get; set; }

        public int step { get; set; }
    }


    public class SaveAdminOrderCriteria
    {
        public CustomerInfo CustomerInfo { get; set; }
        public Remark AdminRemark { get; set; }

        public Remark ClientRemark { get; set; }

        public ShipmentInfo ShipmentInfo { get; set; }
        public Adress ShippingAddress { get; set; }

        public Adress FacturationAddress { get; set; }

        public List<OrderProductViewModelMobile> References { get; set; }

        public OrderInfo Orderinfo { get; set; }

        public int CreatedOrUpdatedBy { get; set; }
    }
}