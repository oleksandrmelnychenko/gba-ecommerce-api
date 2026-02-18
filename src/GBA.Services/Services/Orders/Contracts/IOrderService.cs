using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.SalesModels.Models;

namespace GBA.Services.Services.Orders.Contracts;

public interface IOrderService {
    Task<Sale> GenerateNewOrderAndSaleFromClientShoppingCart(Guid clientNetId, bool withVat);

    Task<Sale> GenerateNewSaleWithInvoice(Sale sale, Guid clientNetId, bool isWorkplace);

    Task<Order> DynamicallyCalculateTotalPrices(Order order);

    Task<string> GenerateNewQuickSaleWithInvoice(Sale sale, Guid retailClientNetId, bool fullPayment);

    Task<string> GenerateNewRetailSale(Sale sale, Guid retailClientNetId, bool fullPayment);

    Task<List<OrderItem>> RemoveUnavailableProducts(List<OrderItem> orderItems, long retailClientId);

    Task SendPaymentImageToCrm(Guid saleNetId, Guid clientNetId, PaymentConfirmationImageModel paymentImage);

    Task<SaleStatistic> GetSaleByNetId(Guid netId);
}