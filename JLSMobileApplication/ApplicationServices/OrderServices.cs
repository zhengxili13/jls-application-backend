using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JLSApplicationBackend.Services;
using JLSDataAccess.Interfaces;
using JLSDataModel.Models;
using JLSDataModel.Models.Order;
using JLSDataModel.Models.User;
using JLSDataModel.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;

namespace JLSApplicationBackend.ApplicationServices;

public interface IOrderServices
{
    Task<long> CreateOrder(SaveOrderCriteria criteria);
}

public class OrderServices(IOrderRepository order,
    IAdressRepository adressRepository,
    UserManager<User> userManager,
    IProductRepository product,
    ISendEmailAndMessageService sendEmailAndMessageService,
    IWebHostEnvironment env): IOrderServices
{
    public async Task<long> CreateOrder(SaveOrderCriteria criteria)
    {
        var shippingAddressId = await UpdateAdress(criteria.ShippingAdressId, true);
        await UpdateAdress(criteria.FacturationAdressId);

        /* Step3: Customer info */
        var user = await userManager.FindByIdAsync(criteria.UserId.ToString());
        long customerId = 0;
        if (user != null)
        {
            var customer = CustomerInfo.FromUserInfo(user);
            customerId = await order.SaveCustomerInfo(customer, criteria.UserId);
        }

        long clientRemarkId = 0;
        /* Step4: save Admin remark info */
        if (criteria.ClientRemark != "")
        {
            clientRemarkId = await order.SaveOrderRemark(criteria.GetRemark(), criteria.UserId);
        }

        /* Step5: reforme the productlist */
        var referenceList = criteria.References.Select(p => p.ReferenceId).ToList();
        var productList = await product.GetProductInfoByReferenceIds(referenceList, "fr");
        if (productList == null) throw new InvalidOperationException("Not able to save order: product cannot be empty");
        var formatedReferenceList = MapProducts(criteria.References, productList);
        var orderId = await order.SaveOrder(formatedReferenceList, shippingAddressId,
            criteria.FacturationAdressId, criteria.UserId, clientRemarkId, customerId);
        await SendEmail(orderId);
        return orderId;

    }

    private Task SendEmail(long orderId)
    {
        return !env.IsDevelopment() ? sendEmailAndMessageService.CreateOrUpdateOrderAsync(orderId, "CreateNewOrder") : Task.CompletedTask;
    }

    private static List<OrderProductViewModelMobile> MapProducts(List<OrderProductViewModelMobile> references, List<ProductListData> productList)
    {
        var formatedReferenceList = (from p in productList
            join ri in references on p.ReferenceId equals ri.ReferenceId
            select new OrderProductViewModelMobile
            {
                Price = p.Price, // Modify accroding to client specification 
                UnityQuantity = (int)p.QuantityPerBox,
                Quantity = ri.Quantity,
                ReferenceId = ri.ReferenceId
            }).ToList();
        return formatedReferenceList;
    }

    private async Task<long> UpdateAdress(long adressId, bool isDefaultAdress = false)
    {
        var adress = await adressRepository.GetAdressByIdAsync(adressId);
        var addressToInsert = adress.Clone();
        addressToInsert.IsDefaultAdress = isDefaultAdress;
        return await adressRepository.CreateOrUpdateAdress(addressToInsert);
    }

}

public record SaveOrderCriteria
{
    public long ShippingAdressId { get; init; }
    public long FacturationAdressId { get; init; }
    public int UserId { get; init; }
    public List<OrderProductViewModelMobile> References { get; init; } = new();
    public string ClientRemark { get; init; }

    public Remark GetRemark()
    {
        return new Remark
        {
            Text = ClientRemark,
            UserId = UserId,
            CreatedBy = UserId,
            CreatedOn = DateTime.Now
        };
    }
}