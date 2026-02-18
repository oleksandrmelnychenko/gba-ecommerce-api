using System;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Messages.PaymentOrders.OutcomePaymentOrders;
using GBA.Domain.Repositories.Consumables.Contracts;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using static GBA.Common.Helpers.DateTimeHelper;

namespace GBA.Services.Actors.PaymentOrders.OutcomePaymentOrderGetActors;

public sealed class BaseOutcomePaymentOrderGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConsumablesRepositoriesFactory _consumablesRepositoriesFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;

    public BaseOutcomePaymentOrderGetActor(
        IDbConnectionFactory connectionFactory,
        IConsumablesRepositoriesFactory consumablesRepositoriesFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _consumablesRepositoriesFactory = consumablesRepositoriesFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;

        Receive<CalculateOutcomePaymentOrdersMessage>(ProcessCalculateOutcomePaymentOrdersMessage);

        Receive<GetAllOutcomePaymentOrdersMessage>(ProcessGetAllOutcomePaymentOrdersMessage);

        Receive<GetAllOutcomePaymentOrdersByColleagueNetIdMessage>(ProcessGetAllOutcomePaymentOrdersByColleagueNetIdMessage);

        Receive<GetOutcomePaymentOrderByNetIdMessage>(ProcessGetOutcomePaymentOrderByNetIdMessage);

        Receive<GetAllOutcomePaymentOrdersByCompanyCarNetIdMessage>(ProcessGetAllOutcomePaymentOrdersByCompanyCarNetIdMessage);

        Receive<GetAllUnderReportOutcomePaymentOrdersFilteredMessage>(ProcessGetAllUnderReportOutcomePaymentOrdersFilteredMessage);
    }

    private void ProcessCalculateOutcomePaymentOrdersMessage(CalculateOutcomePaymentOrdersMessage message) {
        using (IDbConnection connection = _connectionFactory.NewSqlConnection()) {
            IConsumablesOrderRepository consumablesOrderRepository = _consumablesRepositoriesFactory.NewConsumablesOrderRepository(connection);

            message.OutcomePaymentOrder.Amount = decimal.Zero;

            if (message.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(j => j.ConsumablesOrder != null))
                foreach (OutcomePaymentOrderConsumablesOrder junction in
                         message
                             .OutcomePaymentOrder
                             .OutcomePaymentOrderConsumablesOrders
                             .Where(j => j.ConsumablesOrder != null)) {
                    foreach (ConsumablesOrderItem orderItem in junction.ConsumablesOrder.ConsumablesOrderItems.Where(i => !i.Deleted)) {
                        if (!orderItem.TotalPriceWithVAT.Equals(decimal.Zero))
                            orderItem.PricePerItem = Math.Round(orderItem.TotalPriceWithVAT / Convert.ToDecimal(orderItem.Qty), 2);
                        else
                            orderItem.TotalPriceWithVAT = Math.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2);
                        if (orderItem.VatPercent > 0)
                            orderItem.VAT = Math.Round(orderItem.TotalPriceWithVAT * Convert.ToDecimal(orderItem.VatPercent) / (100 + Convert.ToDecimal(orderItem.VatPercent)), 2);
                        else if (orderItem.VAT > decimal.Zero)
                            orderItem.VatPercent = Math.Round(Convert.ToDouble(orderItem.VAT / (orderItem.TotalPriceWithVAT - orderItem.VAT)), 2);

                        orderItem.TotalPrice = Math.Round(orderItem.TotalPriceWithVAT - orderItem.VAT, 2);

                        message.OutcomePaymentOrder.Amount = Math.Round(message.OutcomePaymentOrder.Amount + orderItem.TotalPrice + orderItem.VAT, 2);

                        junction.ConsumablesOrder.TotalAmount = Math.Round(junction.ConsumablesOrder.TotalAmount + orderItem.TotalPrice + orderItem.VAT, 2);

                        junction.ConsumablesOrder.TotalAmountWithoutVAT = Math.Round(junction.ConsumablesOrder.TotalAmountWithoutVAT + orderItem.TotalPrice, 2);
                    }

                    if (junction.ConsumablesOrder.IsNew() || message.OutcomePaymentOrder.IsUnderReport || junction.ConsumablesOrder.IsPayed) continue;

                    decimal paidAmount = consumablesOrderRepository.GetPaidAmountByOrderId(junction.ConsumablesOrder.Id);

                    message.OutcomePaymentOrder.Amount = Math.Round(message.OutcomePaymentOrder.Amount - paidAmount, 2);
                }

            if (message.OutcomePaymentOrder.CompanyCarFuelings.Any())
                foreach (CompanyCarFueling fueling in message.OutcomePaymentOrder.CompanyCarFuelings) {
                    if (fueling.PricePerLiter > decimal.Zero)
                        fueling.TotalPriceWithVat = Math.Round(fueling.PricePerLiter * Convert.ToDecimal(fueling.FuelAmount), 2);
                    else
                        fueling.PricePerLiter = Math.Round(fueling.TotalPriceWithVat / Convert.ToDecimal(fueling.FuelAmount), 2);

                    if (fueling.VatPercent > 0) {
                        fueling.VatAmount = Math.Round(fueling.TotalPriceWithVat * Convert.ToDecimal(fueling.VatPercent) / (100m + Convert.ToDecimal(fueling.VatPercent)), 2);

                        fueling.TotalPrice = Math.Round(fueling.TotalPriceWithVat - fueling.VatAmount, 2);
                    } else if (fueling.VatAmount > decimal.Zero) {
                        fueling.VatPercent = Math.Round(Convert.ToDouble(fueling.VatAmount / (fueling.TotalPriceWithVat - fueling.VatAmount) * 100), 2);

                        fueling.TotalPrice = Math.Round(fueling.TotalPriceWithVat - fueling.VatAmount, 2);
                    } else {
                        fueling.TotalPrice = fueling.TotalPriceWithVat;
                    }

                    message.OutcomePaymentOrder.Amount = Math.Round(message.OutcomePaymentOrder.Amount + fueling.TotalPrice + fueling.VatAmount, 2);
                }
        }

        Sender.Tell(message.OutcomePaymentOrder);
    }

    private void ProcessGetAllOutcomePaymentOrdersMessage(GetAllOutcomePaymentOrdersMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.Limit <= 0) message.Limit = 20;
        if (message.Offset < 0) message.Offset = 0;
        if (message.From.Year.Equals(1)) message.From = DateTime.UtcNow;
        if (message.To.Year.Equals(1)) message.To = DateTime.UtcNow;
        if (string.IsNullOrEmpty(message.Value)) message.Value = string.Empty;

        message.To = ConvertDateTimeToUtcInUkraineTimeZone(message.To.AddHours(23).AddMinutes(59).AddSeconds(59));
        message.From = ConvertDateTimeToUtcInUkraineTimeZone(message.From.Date);

        Sender.Tell(
            _paymentOrderRepositoriesFactory
                .NewOutcomePaymentOrderRepository(connection)
                .GetAll(
                    message.Limit,
                    message.Offset,
                    message.From,
                    message.To,
                    message.Value,
                    message.CurrencyNetId,
                    message.RegisterNetId,
                    message.PaymentMovementNetId,
                    message.OrganizationIds
                )
        );
    }

    private void ProcessGetAllOutcomePaymentOrdersByColleagueNetIdMessage(GetAllOutcomePaymentOrdersByColleagueNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_paymentOrderRepositoriesFactory.NewOutcomePaymentOrderRepository(connection).GetAllByColleagueNetId(message.ColleagueNetId));
    }

    private void ProcessGetOutcomePaymentOrderByNetIdMessage(GetOutcomePaymentOrderByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_paymentOrderRepositoriesFactory.NewOutcomePaymentOrderRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessGetAllOutcomePaymentOrdersByCompanyCarNetIdMessage(GetAllOutcomePaymentOrdersByCompanyCarNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_paymentOrderRepositoriesFactory.NewOutcomePaymentOrderRepository(connection).GetCurrentOutcomesByCompanyCarNetId(message.NetId));
    }

    private void ProcessGetAllUnderReportOutcomePaymentOrdersFilteredMessage(GetAllUnderReportOutcomePaymentOrdersFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.Limit <= 0) message.Limit = 20;
        if (message.Offset < 0) message.Offset = 0;
        if (message.From.Year.Equals(1)) message.From = DateTime.UtcNow;
        if (message.To.Year.Equals(1)) message.To = DateTime.UtcNow;
        if (string.IsNullOrEmpty(message.Value)) message.Value = string.Empty;

        message.To = ConvertDateTimeToUtcInUkraineTimeZone(message.To.AddHours(23).AddMinutes(59).AddSeconds(59));
        message.From = ConvertDateTimeToUtcInUkraineTimeZone(message.From.Date);

        Sender.Tell(
            _paymentOrderRepositoriesFactory
                .NewOutcomePaymentOrderRepository(connection)
                .GetAllUnderReport(
                    message.Limit,
                    message.Offset,
                    message.From,
                    message.To,
                    message.Value,
                    message.CurrencyNetId,
                    message.RegisterNetId,
                    message.PaymentMovementNetId
                )
        );
    }
}