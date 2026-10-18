using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.SalesModels.Models;
using GBA.Services.Infrastructure.Checkout;

namespace GBA.Services.Services.Orders.Contracts;

public interface IOrderService {
    Task<CheckoutResult> GenerateNewOrderAndSaleFromClientShoppingCart(Guid clientNetId, bool withVat, CheckoutAttempt attempt);

    Task<CheckoutResult> GenerateNewSaleWithInvoice(
        Sale sale,
        Guid clientNetId,
        bool isWorkplace,
        bool withVatRequested,
        CheckoutAttempt attempt);

    CheckoutResult FindInvoiceCheckoutReplay(Guid clientNetId, bool withVat, CheckoutAttempt attempt);

    Task<Order> DynamicallyCalculateTotalPrices(Order order, Guid clientNetId);

    Task<string> GenerateNewQuickSaleWithInvoice(Sale sale, Guid retailClientNetId, bool fullPayment);

    Task<string> GenerateNewRetailSale(Sale sale, Guid retailClientNetId, bool fullPayment);

    Task<List<OrderItem>> RemoveUnavailableProducts(List<OrderItem> orderItems, long retailClientId);

    Task SendPaymentImageToCrm(Guid saleNetId, Guid clientNetId, PaymentConfirmationImageModel paymentImage);

    Task<SaleStatistic> GetSaleByNetId(Guid netId);
}