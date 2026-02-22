using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JLSDataModel.AdminViewModel;
using JLSDataModel.Models;
using JLSDataModel.Models.Order;
using JLSDataModel.ViewModels;

namespace JLSDataAccess.Interfaces;

public interface IOrderRepository
{
    Task<long> SaveOrder(List<OrderProductViewModelMobile> References, long ShippingAdressId, long FacturationAdressId,
        int UserId, long? ClientRemarkId, long CutomerInfoId);

    Task<List<OrderListViewModelMobile>> GetOrdersListByUserId(int UserId, string StatusCode, string Lang);

    Task<dynamic> GetOrdersListByOrderId(long OrderId, string Lang);


    Task<List<dynamic>> AdvancedOrderSearchByCriteria(string Lang, int? UserId, DateTime? FromDate, DateTime? ToDate,
        string OrderId, long? StatusId);

    Task<List<OrdersListViewModel>> GetAllOrdersWithInterval(string lang, int intervalCount, int size,
        string orderActive, string orderDirection);

    Task<OrderViewModel> GetOrderById(long id, string lang);

    Task<long> SaveAdminOrder(OrderInfo order, List<OrderProductViewModelMobile> References, int CreatedOrUpdatedBy);

    Task<long> SaveOrderRemark(Remark remark, int? CreatedOrUpadatedBy);

    Task<long> SaveOrderShipmentInfo(ShipmentInfo shipment, int? CreatedOrUpadatedBy);

    Task<long> SaveCustomerInfo(CustomerInfo customer, int? CreatedOrUpadatedBy);

    Task<dynamic> GetCustomerInfoList();
}