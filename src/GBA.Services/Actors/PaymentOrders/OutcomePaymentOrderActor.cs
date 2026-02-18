using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.Accounting;
using GBA.Domain.Helpers.PaymentOrders;
using GBA.Domain.Messages.PaymentOrders.OutcomePaymentOrders;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Consumables.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.PaymentOrders;

public sealed class OutcomePaymentOrderActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConsumablesRepositoriesFactory _consumablesRepositoriesFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoryFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public OutcomePaymentOrderActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoryFactory,
        IConsumablesRepositoriesFactory consumablesRepositoriesFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoryFactory = exchangeRateRepositoryFactory;
        _consumablesRepositoriesFactory = consumablesRepositoriesFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;

        Receive<AddNewOutcomePaymentOrderMessage>(ProcessAddNewOutcomePaymentOrderMessage);

        Receive<AddNewOutcomePaymentOrderForSuppliesMessage>(ProcessAddNewOutcomePaymentOrderForSuppliesMessage);

        Receive<AddNewOutcomePaymentOrderFromTaxFreeMessage>(ProcessAddNewOutcomePaymentOrderFromTaxFreeMessage);

        Receive<AddNewOutcomePaymentOrderFromSadMessage>(ProcessAddNewOutcomePaymentOrderFromSadMessage);

        Receive<UpdateOutcomePaymentOrderMessage>(ProcessUpdateOutcomePaymentOrderMessage);

        Receive<CancelOutcomePaymentOrderByNetIdMessage>(ProcessCancelOutcomePaymentOrderByNetIdMessage);
    }

    private void ProcessAddNewOutcomePaymentOrderMessage(AddNewOutcomePaymentOrderMessage message) {
        if (message.OutcomePaymentOrder == null) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.INCORRECT_OUTCOME_PAYMENT_ORDER));

            return;
        }

        if (!message.OutcomePaymentOrder.IsNew()) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.INCORRECT_OUTCOME_PAYMENT_ORDER));

            return;
        }

        if (message.OutcomePaymentOrder.Amount <= decimal.Zero) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.AMOUNT_NOT_SPECIFIED));

            return;
        }

        if ((message.OutcomePaymentOrder.Organization == null || message.OutcomePaymentOrder.Organization.IsNew()) &&
            message.OutcomePaymentOrder.OrganizationId.Equals(0)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.ORGANIZATION_NOT_SPECIFIED));

            return;
        }

        if (!message.OutcomePaymentOrder.IsUnderReport
            && (message.OutcomePaymentOrder.ConsumableProductOrganization == null || message.OutcomePaymentOrder.ConsumableProductOrganization.IsNew())
            && (message.OutcomePaymentOrder.ClientAgreement == null || message.OutcomePaymentOrder.ClientAgreement.IsNew())
            && (message.OutcomePaymentOrder.SupplyOrganizationAgreement == null || message.OutcomePaymentOrder.SupplyOrganizationAgreement.IsNew())
            && (message.OutcomePaymentOrder.Colleague == null || message.OutcomePaymentOrder.Colleague.IsNew())
            && (message.OutcomePaymentOrder.Client == null || message.OutcomePaymentOrder.Client.IsNew())) {
            Sender.Tell(
                new Tuple<OutcomePaymentOrder, string>(
                    null,
                    OutcomePaymentOrderResourceNames.CONSUMABLE_PRODUCT_ORGANIZATION_AND_CLIENT_AGREEMENT_NOT_SPECIFIED
                )
            );

            return;
        }

        if (message.OutcomePaymentOrder.PaymentMovementOperation?.PaymentMovement == null ||
            message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovement.IsNew()) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.PAYMENT_MOVEMENT_NOT_SPECIFIED));

            return;
        }

        if ((message.OutcomePaymentOrder.PaymentCurrencyRegister == null || message.OutcomePaymentOrder.PaymentCurrencyRegister.IsNew()) &&
            message.OutcomePaymentOrder.PaymentCurrencyRegisterId.Equals(0)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.PAYMENT_CURRENCY_REGISTER_NOT_SPECIFIED));

            return;
        }

        if (message.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(o => o.ConsumablesOrder != null) &&
            (message
                 .OutcomePaymentOrder
                 .OutcomePaymentOrderConsumablesOrders
                 .Any(o => !o.ConsumablesOrder.ConsumablesOrderItems.Any()) ||
             message
                 .OutcomePaymentOrder
                 .OutcomePaymentOrderConsumablesOrders
                 .Any(o =>
                     o
                         .ConsumablesOrder
                         .ConsumablesOrderItems
                         .Any(i => i.ConsumableProductCategory == null && i.ConsumableProductCategoryId.Equals(0))))) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.CONSUMABLE_ORDER_ITEMS_EMPTY));

            return;
        }

        if (message.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(o => o.ConsumablesOrder != null) &&
            message.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(o =>
                o.ConsumablesOrder.ConsumablesOrderItems.Any(i =>
                    i.PaymentCostMovementOperation?.PaymentCostMovement == null))) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.PAYMENT_COST_MOVEMENT_NOT_SPECIFIED));

            return;
        }

        if (message.OutcomePaymentOrder.CompanyCarFuelings.Any(f => f.CompanyCar == null)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.COMPANY_CAR_NOT_SPECIFIED));

            return;
        }

        if (message.OutcomePaymentOrder.CompanyCarFuelings.Any(f => f.ConsumableProductOrganization == null)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.FUELING_CONSUMABLE_ORGANIZATION_NOT_SPECIFIED));

            return;
        }

        if (message.OutcomePaymentOrder.CompanyCarFuelings.Any(f =>
                f.PaymentCostMovementOperation?.PaymentCostMovement == null)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.FUELING_PAYMENT_COST_MOVEMENT_NOT_SPECIFIED));

            return;
        }

        if (message.OutcomePaymentOrder.CompanyCarFuelings.Any(f => f.FuelAmount <= 0)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.FUEL_AMOUNT_NOT_SPECIFIED));

            return;
        }

        if (message.OutcomePaymentOrder.CompanyCarFuelings.Any(f => f.PricePerLiter <= decimal.Zero && f.TotalPrice <= decimal.Zero)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.FUEL_PRICE_NOT_SPECIFIED));

            return;
        }

        if (message.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(o => o.ConsumablesOrder != null) &&
            message.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(o =>
                o.ConsumablesOrder.ConsumablesOrderItems.Any(i => i.TotalPrice <= decimal.Zero && i.PricePerItem <= decimal.Zero))) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.CONSUMABLE_PRODUCT_SHOULD_HAVE_PRICE));

            return;
        }

        if (message.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(o => o.ConsumablesOrder != null) &&
            message
                .OutcomePaymentOrder
                .OutcomePaymentOrderConsumablesOrders
                .Any(o => o.ConsumablesOrder.ConsumablesOrderItems.Any(i => i.Qty <= 0))) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.CONSUMABLE_PRODUCT_SHOULD_HAVE_QTY));

            return;
        }

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IOutcomePaymentOrderRepository outcomePaymentOrderRepository = _paymentOrderRepositoriesFactory.NewOutcomePaymentOrderRepository(connection);
        IPaymentCurrencyRegisterRepository paymentCurrencyRegisterRepository = _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection);

        if (message.OutcomePaymentOrder.Organization != null) message.OutcomePaymentOrder.OrganizationId = message.OutcomePaymentOrder.Organization.Id;
        if (message.OutcomePaymentOrder.PaymentCurrencyRegister != null)
            message.OutcomePaymentOrder.PaymentCurrencyRegisterId = message.OutcomePaymentOrder.PaymentCurrencyRegister.Id;
        if (message.OutcomePaymentOrder.Colleague != null) message.OutcomePaymentOrder.ColleagueId = message.OutcomePaymentOrder.Colleague.Id;
        if (message.OutcomePaymentOrder.FromDate.Year.Equals(1)) message.OutcomePaymentOrder.FromDate = DateTime.UtcNow;
        if (message.OutcomePaymentOrder.Client != null) message.OutcomePaymentOrder.ClientId = message.OutcomePaymentOrder.Client.Id;

        if (message.OutcomePaymentOrder.ClientAgreement != null && message.OutcomePaymentOrder.ClientAgreement.Id > 0) {
            message.OutcomePaymentOrder.ClientAgreementId = message.OutcomePaymentOrder.ClientAgreement.Id;
            message.OutcomePaymentOrder.ConsumableProductOrganization = null;
            message.OutcomePaymentOrder.SupplyOrganizationAgreement = null;
        } else if (message.OutcomePaymentOrder.SupplyOrganizationAgreement != null && message.OutcomePaymentOrder.SupplyOrganizationAgreement.Id > 0) {
            message.OutcomePaymentOrder.ClientAgreement = null;
        }

        PaymentCurrencyRegister paymentCurrencyRegister = paymentCurrencyRegisterRepository.GetById(message.OutcomePaymentOrder.PaymentCurrencyRegisterId);

        if (paymentCurrencyRegister.Amount < message.OutcomePaymentOrder.Amount) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.INSUFFICIENT_OF_FUNDS));

            return;
        }

        if (string.IsNullOrEmpty(message.OutcomePaymentOrder.Number)) {
            OutcomePaymentOrder
                lastRecord = outcomePaymentOrderRepository.GetLastRecord(message.OutcomePaymentOrder.PaymentCurrencyRegister!.PaymentRegister
                    .Type);

            if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                message.OutcomePaymentOrder.Number = message.OutcomePaymentOrder.Organization!.Code + string.Format("{0:D10}", 1);
            else
                message.OutcomePaymentOrder.Number =
                    message.OutcomePaymentOrder.Organization!.Code +
                    string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastRecord.Number, @"(\d+)").Value) + 1);

            if (message.OutcomePaymentOrder.IsUnderReport) {
                OutcomePaymentOrder lastAdvanceRecord = outcomePaymentOrderRepository.GetLastAdvanceRecord();

                if (lastAdvanceRecord == null || !lastAdvanceRecord.Created.Year.Equals(DateTime.Now.Year))
                    message.OutcomePaymentOrder.AdvanceNumber = message.OutcomePaymentOrder.Organization!.Code + string.Format("{0:D10}", 1);
                else
                    message.OutcomePaymentOrder.AdvanceNumber =
                        message.OutcomePaymentOrder.Organization!.Code +
                        string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastAdvanceRecord.Number, @"(\d+)").Value) + 1);
            }
        }

        message.OutcomePaymentOrder.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.CurrentUserNetId).Id;

        bool clientAgreementNegativeBalance = false;

        if (!message.OutcomePaymentOrder.IsUnderReport) {
            if (message.OutcomePaymentOrder.ConsumableProductOrganization != null) {
                message.OutcomePaymentOrder.ConsumableProductOrganizationId = message.OutcomePaymentOrder.ConsumableProductOrganization.Id;
                message.OutcomePaymentOrder.SupplyOrganizationAgreementId = message.OutcomePaymentOrder.SupplyOrganizationAgreement?.Id;

                ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
                    _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);

                if (message.OutcomePaymentOrder.SupplyOrganizationAgreementId.HasValue)
                    message.OutcomePaymentOrder.SupplyOrganizationAgreement =
                        supplyOrganizationAgreementRepository
                            .GetById(message.OutcomePaymentOrder.SupplyOrganizationAgreementId ?? 0);

                if (message.OutcomePaymentOrder.SupplyOrganizationAgreement != null) {
                    Currency euroCurrency = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

                    PaymentOrdersCurrencyConvertor paymentOrdersCurrencyConvertor = new(
                        euroCurrency,
                        paymentCurrencyRegister.Currency,
                        message.OutcomePaymentOrder.FromDate,
                        _exchangeRateRepositoryFactory.NewExchangeRateRepository(connection),
                        _exchangeRateRepositoryFactory.NewCrossExchangeRateRepository(connection));

                    message.OutcomePaymentOrder.EuroAmount = paymentOrdersCurrencyConvertor.ConvertAmountToEuro(message.OutcomePaymentOrder.Amount);

                    AgreementConversionResult agreementConversionResult = paymentOrdersCurrencyConvertor.GetConvertedAmountToAgreementCurrency(
                        message.OutcomePaymentOrder.Amount,
                        message.OutcomePaymentOrder.ExchangeRate,
                        message.OutcomePaymentOrder.SupplyOrganizationAgreement.Currency);

                    message.OutcomePaymentOrder.AfterExchangeAmount = agreementConversionResult.InAgreementCurrencyAmount;
                    message.OutcomePaymentOrder.ExchangeRate = agreementConversionResult.ExchangeRate;

                    message.OutcomePaymentOrder.SupplyOrganizationAgreement.CurrentEuroAmount =
                        Math.Round(
                            message.OutcomePaymentOrder.SupplyOrganizationAgreement.CurrentEuroAmount
                            +
                            message.OutcomePaymentOrder.EuroAmount,
                            2
                        );
                    message.OutcomePaymentOrder.SupplyOrganizationAgreement.CurrentAmount =
                        Math.Round(
                            message.OutcomePaymentOrder.SupplyOrganizationAgreement.CurrentAmount
                            +
                            message.OutcomePaymentOrder.AfterExchangeAmount,
                            2
                        );

                    supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.OutcomePaymentOrder.SupplyOrganizationAgreement);
                }
            } else if (message.OutcomePaymentOrder.ClientAgreement != null) {
                message.OutcomePaymentOrder.ClientAgreementId = message.OutcomePaymentOrder.ClientAgreement.Id;

                IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

                message.OutcomePaymentOrder.ClientAgreement =
                    clientAgreementRepository.GetById(message.OutcomePaymentOrder.ClientAgreementId.Value);

                Currency euro = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

                PaymentOrdersCurrencyConvertor paymentOrdersCurrencyConvertor = new(
                    euro,
                    paymentCurrencyRegister.Currency,
                    message.OutcomePaymentOrder.FromDate,
                    _exchangeRateRepositoryFactory.NewExchangeRateRepository(connection),
                    _exchangeRateRepositoryFactory.NewCrossExchangeRateRepository(connection));

                message.OutcomePaymentOrder.EuroAmount = paymentOrdersCurrencyConvertor.ConvertAmountToEuro(message.OutcomePaymentOrder.Amount);

                AgreementConversionResult agreementConversionResult = paymentOrdersCurrencyConvertor.GetConvertedAmountToAgreementCurrency(
                    message.OutcomePaymentOrder.Amount,
                    message.OutcomePaymentOrder.ExchangeRate,
                    message.OutcomePaymentOrder.ClientAgreement.Agreement.Currency);

                message.OutcomePaymentOrder.AfterExchangeAmount = agreementConversionResult.InAgreementCurrencyAmount;
                message.OutcomePaymentOrder.ExchangeRate = agreementConversionResult.ExchangeRate;

                message.OutcomePaymentOrder.ClientAgreement.CurrentAmount =
                    Math.Round(
                        message.OutcomePaymentOrder.ClientAgreement.CurrentAmount
                        -
                        message.OutcomePaymentOrder.EuroAmount,
                        2
                    );

                _clientRepositoriesFactory
                    .NewClientBalanceMovementRepository(connection)
                    .AddOutMovement(
                        new ClientBalanceMovement {
                            ClientAgreementId = message.OutcomePaymentOrder.ClientAgreement.Id,
                            Amount = message.OutcomePaymentOrder.EuroAmount,
                            ExchangeRateAmount = message.OutcomePaymentOrder.ExchangeRate
                        }
                    );

                clientAgreementRepository.UpdateAmountByNetId(
                    message.OutcomePaymentOrder.ClientAgreement.NetUid,
                    message.OutcomePaymentOrder.ClientAgreement.CurrentAmount
                );
            }
        }

        if (clientAgreementNegativeBalance) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.CLIENT_AGREEMENT_NEGATIVE_BALANCE));
        } else {
            if (!message.OutcomePaymentOrder.IsUnderReport && message.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any()) {
                IConsumablesOrderRepository consumablesOrderRepository = _consumablesRepositoriesFactory.NewConsumablesOrderRepository(connection);
                IConsumablesOrderItemRepository consumablesOrderItemRepository = _consumablesRepositoriesFactory.NewConsumablesOrderItemRepository(connection);
                IPaymentCostMovementOperationRepository paymentCostMovementOperationRepository =
                    _paymentOrderRepositoriesFactory.NewPaymentCostMovementOperationRepository(connection);

                if (message.OutcomePaymentOrder.IsUnderReport) message.OutcomePaymentOrder.Amount = decimal.Zero;

                foreach (OutcomePaymentOrderConsumablesOrder junction in
                         message
                             .OutcomePaymentOrder
                             .OutcomePaymentOrderConsumablesOrders
                             .Where(j => j.IsNew() && j.ConsumablesOrder != null)) {
                    if (junction.ConsumablesOrder.ConsumablesStorage != null)
                        junction.ConsumablesOrder.ConsumablesStorageId = junction.ConsumablesOrder.ConsumablesStorage.Id;

                    if (junction.ConsumablesOrder.OrganizationFromDate.Year.Equals(1)) junction.ConsumablesOrder.OrganizationFromDate = DateTime.UtcNow;

                    junction.ConsumablesOrder.UserId = message.OutcomePaymentOrder.UserId;

                    if (junction.ConsumablesOrder.IsNew()) {
                        ConsumablesOrder lastConsumablesOrder = consumablesOrderRepository.GetLastRecord();

                        if (lastConsumablesOrder == null || !lastConsumablesOrder.Created.Year.Equals(DateTime.Now.Year))
                            junction.ConsumablesOrder.Number = string.Format("{0:D10}", 1);
                        else
                            junction.ConsumablesOrder.Number = string.Format("{0:D10}", Convert.ToInt32(lastConsumablesOrder.Number) + 1);

                        junction.ConsumablesOrder.Id = consumablesOrderRepository.Add(junction.ConsumablesOrder);

                        junction.ConsumablesOrderId = junction.ConsumablesOrder.Id;

                        foreach (ConsumablesOrderItem orderItem in junction
                                     .ConsumablesOrder
                                     .ConsumablesOrderItems
                                     .Where(i => i.ConsumableProductCategory != null || !i.ConsumableProductCategoryId.Equals(0))) {
                            if (orderItem.ConsumableProductCategory != null) orderItem.ConsumableProductCategoryId = orderItem.ConsumableProductCategory.Id;
                            if (orderItem.ConsumableProduct != null && !orderItem.ConsumableProduct.IsNew())
                                orderItem.ConsumableProductId = orderItem.ConsumableProduct.Id;
                            else
                                orderItem.ConsumableProductId = null;
                            if (orderItem.ConsumableProductOrganization != null && !orderItem.ConsumableProductOrganization.IsNew())
                                orderItem.ConsumableProductOrganizationId = orderItem.ConsumableProductOrganization.Id;
                            else
                                orderItem.ConsumableProductOrganizationId = null;
                            if (orderItem.SupplyOrganizationAgreement != null && !orderItem.SupplyOrganizationAgreement.IsNew())
                                orderItem.SupplyOrganizationAgreementId = orderItem.SupplyOrganizationAgreement.Id;
                            else
                                orderItem.SupplyOrganizationAgreementId = null;
                            if (!orderItem.TotalPriceWithVAT.Equals(decimal.Zero))
                                orderItem.PricePerItem = Math.Round(orderItem.TotalPriceWithVAT / Convert.ToDecimal(orderItem.Qty), 2);
                            else
                                orderItem.TotalPriceWithVAT = Math.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2);
                            if (orderItem.VatPercent > 0)
                                orderItem.VAT = Math.Round(orderItem.TotalPriceWithVAT * Convert.ToDecimal(orderItem.VatPercent) / (100 + Convert.ToDecimal(orderItem.VatPercent)),
                                    2);
                            else if (orderItem.VAT > decimal.Zero)
                                orderItem.VatPercent = Math.Round(Convert.ToDouble(orderItem.VAT / (orderItem.TotalPriceWithVAT - orderItem.VAT)), 2);

                            orderItem.TotalPrice = Math.Round(orderItem.TotalPriceWithVAT - orderItem.VAT, 2);

                            if (message.OutcomePaymentOrder.IsUnderReport)
                                message.OutcomePaymentOrder.Amount = Math.Round(message.OutcomePaymentOrder.Amount + orderItem.TotalPrice + orderItem.VAT, 2);

                            orderItem.ConsumablesOrderId = junction.ConsumablesOrderId;

                            orderItem.Id = consumablesOrderItemRepository.Add(orderItem);

                            orderItem.PaymentCostMovementOperation.PaymentCostMovementId = orderItem.PaymentCostMovementOperation.PaymentCostMovement.Id;
                            orderItem.PaymentCostMovementOperation.ConsumablesOrderItemId = orderItem.Id;

                            paymentCostMovementOperationRepository.Add(orderItem.PaymentCostMovementOperation);
                        }
                    } else {
                        junction.ConsumablesOrderId = junction.ConsumablesOrder.Id;

                        consumablesOrderRepository.Update(junction.ConsumablesOrder);

                        foreach (ConsumablesOrderItem orderItem in junction
                                     .ConsumablesOrder
                                     .ConsumablesOrderItems
                                     .Where(i => i.IsNew() && (i.ConsumableProductCategory != null || !i.ConsumableProductCategoryId.Equals(0)))) {
                            if (orderItem.ConsumableProductCategory != null) orderItem.ConsumableProductCategoryId = orderItem.ConsumableProductCategory.Id;
                            if (orderItem.ConsumableProduct != null && !orderItem.ConsumableProduct.IsNew())
                                orderItem.ConsumableProductId = orderItem.ConsumableProduct.Id;
                            else
                                orderItem.ConsumableProductId = null;
                            if (orderItem.ConsumableProductOrganization != null && !orderItem.ConsumableProductOrganization.IsNew())
                                orderItem.ConsumableProductOrganizationId = orderItem.ConsumableProductOrganization.Id;
                            else
                                orderItem.ConsumableProductOrganizationId = null;
                            if (orderItem.SupplyOrganizationAgreement != null && !orderItem.SupplyOrganizationAgreement.IsNew())
                                orderItem.SupplyOrganizationAgreementId = orderItem.SupplyOrganizationAgreement.Id;
                            else
                                orderItem.SupplyOrganizationAgreementId = null;
                            if (!orderItem.TotalPriceWithVAT.Equals(decimal.Zero))
                                orderItem.PricePerItem = Math.Round(orderItem.TotalPriceWithVAT / Convert.ToDecimal(orderItem.Qty), 2);
                            else
                                orderItem.TotalPriceWithVAT = Math.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2);
                            if (orderItem.VatPercent > 0)
                                orderItem.VAT = Math.Round(orderItem.TotalPriceWithVAT * Convert.ToDecimal(orderItem.VatPercent) / (100 + Convert.ToDecimal(orderItem.VatPercent)),
                                    2);
                            else if (orderItem.VAT > decimal.Zero)
                                orderItem.VatPercent = Math.Round(Convert.ToDouble(orderItem.VAT / (orderItem.TotalPriceWithVAT - orderItem.VAT)), 2);

                            orderItem.TotalPrice = Math.Round(orderItem.TotalPriceWithVAT - orderItem.VAT, 2);

                            if (message.OutcomePaymentOrder.IsUnderReport)
                                message.OutcomePaymentOrder.Amount = Math.Round(message.OutcomePaymentOrder.Amount + orderItem.TotalPrice + orderItem.VAT, 2);

                            orderItem.ConsumablesOrderId = junction.ConsumablesOrderId;

                            orderItem.Id = consumablesOrderItemRepository.Add(orderItem);

                            orderItem.PaymentCostMovementOperation.PaymentCostMovementId = orderItem.PaymentCostMovementOperation.PaymentCostMovement.Id;
                            orderItem.PaymentCostMovementOperation.ConsumablesOrderItemId = orderItem.Id;

                            paymentCostMovementOperationRepository.Add(orderItem.PaymentCostMovementOperation);
                        }

                        consumablesOrderItemRepository
                            .Update(
                                junction
                                    .ConsumablesOrder
                                    .ConsumablesOrderItems
                                    .Where(i => !i.IsNew() &&
                                                (i.ConsumableProductCategory != null || !i.ConsumableProductCategoryId.Equals(0)))
                                    .Select(item => {
                                        if (item.ConsumableProductCategory != null) item.ConsumableProductCategoryId = item.ConsumableProductCategory.Id;
                                        if (item.ConsumableProduct != null) item.ConsumableProductId = item.ConsumableProduct.Id;
                                        if (item.ConsumableProductOrganization != null && !item.ConsumableProductOrganization.IsNew())
                                            item.ConsumableProductOrganizationId = item.ConsumableProductOrganization.Id;
                                        if (!item.TotalPriceWithVAT.Equals(decimal.Zero))
                                            item.PricePerItem = Math.Round(item.TotalPriceWithVAT / Convert.ToDecimal(item.Qty), 2);
                                        else
                                            item.TotalPriceWithVAT = Math.Round(item.PricePerItem * Convert.ToDecimal(item.Qty), 2);
                                        if (item.VatPercent > 0)
                                            item.VAT = Math.Round(item.TotalPriceWithVAT * Convert.ToDecimal(item.VatPercent) / (100 + Convert.ToDecimal(item.VatPercent)), 2);
                                        else if (item.VAT > decimal.Zero) item.VatPercent = Math.Round(Convert.ToDouble(item.VAT / (item.TotalPriceWithVAT - item.VAT)), 2);

                                        item.TotalPrice = Math.Round(item.TotalPriceWithVAT - item.VAT);

                                        if (message.OutcomePaymentOrder.IsUnderReport)
                                            message.OutcomePaymentOrder.Amount = Math.Round(message.OutcomePaymentOrder.Amount + item.TotalPrice + item.VAT, 2);

                                        item.PaymentCostMovementOperation.PaymentCostMovementId = item.PaymentCostMovementOperation.PaymentCostMovement.Id;
                                        item.PaymentCostMovementOperation.ConsumablesOrderItemId = item.Id;

                                        paymentCostMovementOperationRepository.Update(item.PaymentCostMovementOperation);

                                        return item;
                                    })
                            );
                    }
                }
            }

            if (message.OutcomePaymentOrder.IsUnderReport && message.OutcomePaymentOrder.CompanyCarFuelings.Any())
                foreach (CompanyCarFueling fueling in message.OutcomePaymentOrder.CompanyCarFuelings) {
                    if (fueling.CompanyCar != null) fueling.CompanyCarId = fueling.CompanyCar.Id;
                    if (fueling.ConsumableProductOrganization != null) fueling.ConsumableProductOrganizationId = fueling.ConsumableProductOrganization.Id;

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

                    fueling.UserId = message.OutcomePaymentOrder.UserId;

                    message.OutcomePaymentOrder.Amount = Math.Round(message.OutcomePaymentOrder.Amount + fueling.TotalPrice + fueling.VatAmount, 2);
                }

            if (paymentCurrencyRegister.Amount < message.OutcomePaymentOrder.Amount) {
                Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.INSUFFICIENT_OF_FUNDS));
            } else {
                paymentCurrencyRegister.Amount = Math.Round(paymentCurrencyRegister.Amount - message.OutcomePaymentOrder.Amount, 2);

                paymentCurrencyRegisterRepository.UpdateAmount(paymentCurrencyRegister);

                if (message.OutcomePaymentOrder.EuroAmount.Equals(decimal.Zero)) {
                    Currency euroCurrency = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

                    PaymentOrdersCurrencyConvertor paymentOrdersCurrencyConvertor = new(
                        euroCurrency,
                        paymentCurrencyRegister.Currency,
                        message.OutcomePaymentOrder.FromDate,
                        _exchangeRateRepositoryFactory.NewExchangeRateRepository(connection),
                        _exchangeRateRepositoryFactory.NewCrossExchangeRateRepository(connection));

                    message.OutcomePaymentOrder.EuroAmount = paymentOrdersCurrencyConvertor.ConvertAmountToEuro(message.OutcomePaymentOrder.Amount);
                }

                message.OutcomePaymentOrder.Id = outcomePaymentOrderRepository.Add(message.OutcomePaymentOrder);

                if (message.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any()) {
                    foreach (OutcomePaymentOrderConsumablesOrder junction in
                             message.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Where(j =>
                                 j.IsNew() && j.ConsumablesOrder != null))
                        junction.OutcomePaymentOrderId = message.OutcomePaymentOrder.Id;

                    _paymentOrderRepositoriesFactory
                        .NewOutcomePaymentOrderConsumablesOrderRepository(connection)
                        .Add(
                            message
                                .OutcomePaymentOrder
                                .OutcomePaymentOrderConsumablesOrders
                                .Where(j => j.IsNew() && j.ConsumablesOrder != null)
                        );
                }

                if (message.OutcomePaymentOrder.CompanyCarFuelings.Any()) {
                    ICompanyCarRepository companyCarRepository = _consumablesRepositoriesFactory.NewCompanyCarRepository(connection);
                    ICompanyCarFuelingRepository companyCarFuelingRepository = _consumablesRepositoriesFactory.NewCompanyCarFuelingRepository(connection);
                    IPaymentCostMovementOperationRepository paymentCostMovementOperationRepository =
                        _paymentOrderRepositoriesFactory.NewPaymentCostMovementOperationRepository(connection);

                    foreach (CompanyCarFueling fueling in message.OutcomePaymentOrder.CompanyCarFuelings) {
                        fueling.OutcomePaymentOrderId = message.OutcomePaymentOrder.Id;

                        fueling.Id = companyCarFuelingRepository.Add(fueling);

                        companyCarRepository.UpdateFuelAmountByCarId(fueling.CompanyCarId, fueling.FuelAmount);

                        fueling.PaymentCostMovementOperation.PaymentCostMovementId = fueling.PaymentCostMovementOperation.PaymentCostMovement.Id;
                        fueling.PaymentCostMovementOperation.CompanyCarFuelingId = fueling.Id;

                        paymentCostMovementOperationRepository.Add(fueling.PaymentCostMovementOperation);
                    }
                }

                if (message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
                    message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovementId =
                        message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovement.Id;

                message.OutcomePaymentOrder.PaymentMovementOperation.OutcomePaymentOrderId = message.OutcomePaymentOrder.Id;

                _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection).Add(message.OutcomePaymentOrder.PaymentMovementOperation);

                Sender.Tell(new Tuple<OutcomePaymentOrder, string>(outcomePaymentOrderRepository.GetById(message.OutcomePaymentOrder.Id), string.Empty));
            }
        }
    }

    private void ProcessAddNewOutcomePaymentOrderForSuppliesMessage(AddNewOutcomePaymentOrderForSuppliesMessage message) {
        if (message.OutcomePaymentOrder == null) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.INCORRECT_OUTCOME_PAYMENT_ORDER));

            return;
        }

        if (!message.OutcomePaymentOrder.IsNew()) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.INCORRECT_OUTCOME_PAYMENT_ORDER));

            return;
        }

        if (message.OutcomePaymentOrder.Amount <= decimal.Zero) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.AMOUNT_NOT_SPECIFIED));

            return;
        }

        if ((message.OutcomePaymentOrder.Organization == null || message.OutcomePaymentOrder.Organization.IsNew()) &&
            message.OutcomePaymentOrder.OrganizationId.Equals(0)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.ORGANIZATION_NOT_SPECIFIED));

            return;
        }

        if (!message
                .OutcomePaymentOrder
                .OutcomePaymentOrderSupplyPaymentTasks
                .Any(t => t.SupplyPaymentTask != null && !t.SupplyPaymentTask.IsNew())) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.SUPPLY_PAYMENT_TASK_NOT_SPECIFIED));

            return;
        }

        if (message.OutcomePaymentOrder.PaymentMovementOperation?.PaymentMovement == null ||
            message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovement.IsNew()) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.PAYMENT_MOVEMENT_NOT_SPECIFIED));

            return;
        }

        if ((message.OutcomePaymentOrder.PaymentCurrencyRegister == null || message.OutcomePaymentOrder.PaymentCurrencyRegister.IsNew())
            && message.OutcomePaymentOrder.PaymentCurrencyRegisterId.Equals(0)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.PAYMENT_CURRENCY_REGISTER_NOT_SPECIFIED));

            return;
        }

        if (message.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Any(x =>
                !x.SupplyPaymentTask.IsAccounting.Equals(message.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.FirstOrDefault()?.SupplyPaymentTask
                    ?.IsAccounting))) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.PAYMENT_MOVEMENT_NOT_VALID));

            return;
        }

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyPaymentTaskRepository supplyPaymentTaskRepository = _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);
        IOutcomePaymentOrderRepository outcomePaymentOrderRepository = _paymentOrderRepositoriesFactory.NewOutcomePaymentOrderRepository(connection);
        IPaymentCurrencyRegisterRepository paymentCurrencyRegisterRepository = _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection);

        if (message.OutcomePaymentOrder.Organization != null) message.OutcomePaymentOrder.OrganizationId = message.OutcomePaymentOrder.Organization.Id;
        if (message.OutcomePaymentOrder.PaymentCurrencyRegister != null)
            message.OutcomePaymentOrder.PaymentCurrencyRegisterId = message.OutcomePaymentOrder.PaymentCurrencyRegister.Id;
        if (message.OutcomePaymentOrder.Colleague != null) message.OutcomePaymentOrder.ColleagueId = message.OutcomePaymentOrder.Colleague.Id;
        if (message.OutcomePaymentOrder.FromDate.Year.Equals(1)) message.OutcomePaymentOrder.FromDate = DateTime.UtcNow;
        if (message.OutcomePaymentOrder.ConsumableProductOrganization != null && !message.OutcomePaymentOrder.ConsumableProductOrganization.IsNew())
            message.OutcomePaymentOrder.ConsumableProductOrganizationId = message.OutcomePaymentOrder.ConsumableProductOrganization.Id;

        PaymentCurrencyRegister paymentCurrencyRegister = paymentCurrencyRegisterRepository.GetById(message.OutcomePaymentOrder.PaymentCurrencyRegisterId);

        if (paymentCurrencyRegister.Amount < message.OutcomePaymentOrder.Amount) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.INSUFFICIENT_OF_FUNDS));
        } else {
            // TODO to find out if organization exists
            OutcomePaymentOrder lastRecord = outcomePaymentOrderRepository.GetLastRecord(message.OutcomePaymentOrder.PaymentCurrencyRegister!.PaymentRegister.Type);

            if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                message.OutcomePaymentOrder.Number = message.OutcomePaymentOrder.Organization!.Code + string.Format("{0:D10}", 1);
            else
                message.OutcomePaymentOrder.Number =
                    message.OutcomePaymentOrder.Organization!.Code +
                    string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastRecord.Number, @"(\d+)").Value) + 1);

            message.OutcomePaymentOrder.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.CurrentUserNetId).Id;

            bool isSameSupplier;
            bool isSameSupplierAgreement = true;

            List<SupplyPaymentTaskDocument> toAddDocuments = new();

            //Determine what kind of Supplier is used for payment
            OutcomePaymentOrderSupplyPaymentTask task =
                message
                    .OutcomePaymentOrder
                    .OutcomePaymentOrderSupplyPaymentTasks
                    .First(j => j.SupplyPaymentTask != null && !j.SupplyPaymentTask.IsNew());

            message.OutcomePaymentOrder.SupplyOrderPolandPaymentDeliveryProtocol = null;
            message.OutcomePaymentOrder.ConsumableProductOrganization = null;
            message.OutcomePaymentOrder.SupplyOrganizationAgreement = null;
            message.OutcomePaymentOrder.ClientAgreement = null;

            if (task.SupplyPaymentTask.PaymentDeliveryProtocols.Any()) {
                SupplyOrderPaymentDeliveryProtocol protocol = task.SupplyPaymentTask.PaymentDeliveryProtocols.First();

                if (protocol.SupplyProForm != null) {
                    if (protocol.SupplyProForm.SupplyOrders.Any()) {
                        SupplyOrder order = protocol.SupplyProForm.SupplyOrders.First();

                        if (order.ClientAgreement != null)
                            message.OutcomePaymentOrder.ClientAgreement = order.ClientAgreement;
                    }
                } else {
                    if (protocol.SupplyInvoice?.SupplyOrder?.ClientAgreement != null)
                        message.OutcomePaymentOrder.ClientAgreement = protocol.SupplyInvoice.SupplyOrder.ClientAgreement;
                    else if (protocol.SupplyInvoice?.SupplyOrder != null)
                        message.OutcomePaymentOrder.ClientAgreement = _clientRepositoriesFactory.NewClientAgreementRepository(connection)
                            .GetById(protocol.SupplyInvoice.SupplyOrder.ClientAgreementId);
                }
            } else if (task.SupplyPaymentTask.SupplyOrderUkrainePaymentDeliveryProtocols.Any()) {
                SupplyOrderUkrainePaymentDeliveryProtocol protocol = task.SupplyPaymentTask.SupplyOrderUkrainePaymentDeliveryProtocols.First();

                message.OutcomePaymentOrder.ClientAgreement = protocol.SupplyOrderUkraine?.ClientAgreement;
            } else if (task.SupplyPaymentTask.SupplyOrderPolandPaymentDeliveryProtocols.Any(p => !p.IsNew())) {
                message.OutcomePaymentOrder.SupplyOrderPolandPaymentDeliveryProtocol =
                    task.SupplyPaymentTask.SupplyOrderPolandPaymentDeliveryProtocols.First();

                message.OutcomePaymentOrder.SupplyOrderPolandPaymentDeliveryProtocolId = message.OutcomePaymentOrder.SupplyOrderPolandPaymentDeliveryProtocol.Id;
            } else if (task.SupplyPaymentTask.ContainerServices.Any()) {
                ContainerService containerService = task.SupplyPaymentTask.ContainerServices.First();

                message.OutcomePaymentOrder.ConsumableProductOrganization = containerService.ContainerOrganization;
                message.OutcomePaymentOrder.SupplyOrganizationAgreement = containerService.SupplyOrganizationAgreement;
            } else if (task.SupplyPaymentTask.BrokerServices.Any()) {
                CustomService customService = task.SupplyPaymentTask.BrokerServices.First();

                message.OutcomePaymentOrder.ConsumableProductOrganization = customService.CustomOrganization ?? customService.ExciseDutyOrganization;
                message.OutcomePaymentOrder.SupplyOrganizationAgreement = customService.SupplyOrganizationAgreement;
            } else if (task.SupplyPaymentTask.PortWorkServices.Any()) {
                PortWorkService portWorkService = task.SupplyPaymentTask.PortWorkServices.First();

                message.OutcomePaymentOrder.ConsumableProductOrganization = portWorkService.PortWorkOrganization;
                message.OutcomePaymentOrder.SupplyOrganizationAgreement = portWorkService.SupplyOrganizationAgreement;
            } else if (task.SupplyPaymentTask.TransportationServices.Any()) {
                TransportationService transportationService = task.SupplyPaymentTask.TransportationServices.First();

                message.OutcomePaymentOrder.ConsumableProductOrganization = transportationService.TransportationOrganization;
                message.OutcomePaymentOrder.SupplyOrganizationAgreement = transportationService.SupplyOrganizationAgreement;
            } else if (task.SupplyPaymentTask.PortCustomAgencyServices.Any()) {
                PortCustomAgencyService portCustomAgencyService = task.SupplyPaymentTask.PortCustomAgencyServices.First();

                message.OutcomePaymentOrder.ConsumableProductOrganization = portCustomAgencyService.PortCustomAgencyOrganization;
                message.OutcomePaymentOrder.SupplyOrganizationAgreement = portCustomAgencyService.SupplyOrganizationAgreement;
            } else if (task.SupplyPaymentTask.CustomAgencyServices.Any()) {
                CustomAgencyService customAgencyService = task.SupplyPaymentTask.CustomAgencyServices.First();

                message.OutcomePaymentOrder.ConsumableProductOrganization = customAgencyService.CustomAgencyOrganization;
                message.OutcomePaymentOrder.SupplyOrganizationAgreement = customAgencyService.SupplyOrganizationAgreement;
            } else if (task.SupplyPaymentTask.PlaneDeliveryServices.Any()) {
                PlaneDeliveryService planeDeliveryService = task.SupplyPaymentTask.PlaneDeliveryServices.First();

                message.OutcomePaymentOrder.ConsumableProductOrganization = planeDeliveryService.PlaneDeliveryOrganization;
                message.OutcomePaymentOrder.SupplyOrganizationAgreement = planeDeliveryService.SupplyOrganizationAgreement;
            } else if (task.SupplyPaymentTask.VehicleDeliveryServices.Any()) {
                VehicleDeliveryService vehicleDeliveryService = task.SupplyPaymentTask.VehicleDeliveryServices.First();

                message.OutcomePaymentOrder.ConsumableProductOrganization = vehicleDeliveryService.VehicleDeliveryOrganization;
                message.OutcomePaymentOrder.SupplyOrganizationAgreement = vehicleDeliveryService.SupplyOrganizationAgreement;
            } else if (task.SupplyPaymentTask.MergedServices.Any()) {
                MergedService mergedService = task.SupplyPaymentTask.MergedServices.First();

                message.OutcomePaymentOrder.ConsumableProductOrganization = mergedService.SupplyOrganization;
                message.OutcomePaymentOrder.SupplyOrganizationAgreement = mergedService.SupplyOrganizationAgreement;
            } else if (task.SupplyPaymentTask.ConsumablesOrder != null) {
                ConsumablesOrder consumablesOrder = task.SupplyPaymentTask.ConsumablesOrder;

                message.OutcomePaymentOrder.ConsumableProductOrganization = consumablesOrder.ConsumableProductOrganization;
                message.OutcomePaymentOrder.SupplyOrganizationAgreement = consumablesOrder.SupplyOrganizationAgreement;
            } else if (task.SupplyPaymentTask.VehicleServices.Any()) {
                VehicleService vehicleService = task.SupplyPaymentTask.VehicleServices.First();

                message.OutcomePaymentOrder.ConsumableProductOrganization = vehicleService.VehicleOrganization;
                message.OutcomePaymentOrder.SupplyOrganizationAgreement = vehicleService.SupplyOrganizationAgreement;
            } else if (task.SupplyPaymentTask.BillOfLadingServices.Any()) {
                BillOfLadingService billOfLadingService = task.SupplyPaymentTask.BillOfLadingServices.First();

                message.OutcomePaymentOrder.ConsumableProductOrganization = billOfLadingService.SupplyOrganization;
                message.OutcomePaymentOrder.SupplyOrganizationAgreement = billOfLadingService.SupplyOrganizationAgreement;
            }

            //Check is all SupplyPaymentTask's use same Supplier
            if (message.OutcomePaymentOrder.ConsumableProductOrganization != null) {
                isSameSupplier =
                    message
                        .OutcomePaymentOrder
                        .OutcomePaymentOrderSupplyPaymentTasks
                        .Where(j => j.SupplyPaymentTask != null && !j.SupplyPaymentTask.IsNew())
                        .All(junction => {
                            if (junction.SupplyPaymentTask.ContainerServices.Any())
                                return junction
                                    .SupplyPaymentTask
                                    .ContainerServices
                                    .First()
                                    .ContainerOrganization.Id.Equals(message.OutcomePaymentOrder.ConsumableProductOrganization.Id);

                            if (junction.SupplyPaymentTask.BrokerServices.Any()) {
                                CustomService customService = junction.SupplyPaymentTask.BrokerServices.First();

                                return customService.CustomOrganization?.Id.Equals(message.OutcomePaymentOrder.ConsumableProductOrganization.Id) ??
                                       customService.ExciseDutyOrganization.Id.Equals(message.OutcomePaymentOrder.ConsumableProductOrganization.Id);
                            }

                            if (junction.SupplyPaymentTask.PortWorkServices.Any())
                                return junction
                                    .SupplyPaymentTask
                                    .PortWorkServices
                                    .First()
                                    .PortWorkOrganization.Id.Equals(message.OutcomePaymentOrder.ConsumableProductOrganization.Id);

                            if (junction.SupplyPaymentTask.TransportationServices.Any())
                                return junction
                                    .SupplyPaymentTask
                                    .TransportationServices
                                    .First()
                                    .TransportationOrganization.Id.Equals(message.OutcomePaymentOrder.ConsumableProductOrganization.Id);

                            if (junction.SupplyPaymentTask.PortCustomAgencyServices.Any())
                                return junction
                                    .SupplyPaymentTask
                                    .PortCustomAgencyServices
                                    .First()
                                    .PortCustomAgencyOrganization.Id.Equals(message.OutcomePaymentOrder.ConsumableProductOrganization.Id);

                            if (junction.SupplyPaymentTask.CustomAgencyServices.Any())
                                return junction
                                    .SupplyPaymentTask
                                    .CustomAgencyServices
                                    .First()
                                    .CustomAgencyOrganization.Id.Equals(message.OutcomePaymentOrder.ConsumableProductOrganization.Id);

                            if (junction.SupplyPaymentTask.PlaneDeliveryServices.Any())
                                return junction
                                    .SupplyPaymentTask
                                    .PlaneDeliveryServices
                                    .First()
                                    .PlaneDeliveryOrganization.Id.Equals(message.OutcomePaymentOrder.ConsumableProductOrganization.Id);

                            if (junction.SupplyPaymentTask.VehicleDeliveryServices.Any())
                                return junction
                                    .SupplyPaymentTask
                                    .VehicleDeliveryServices
                                    .First()
                                    .VehicleDeliveryOrganization.Id.Equals(message.OutcomePaymentOrder.ConsumableProductOrganization.Id);

                            if (junction.SupplyPaymentTask.MergedServices.Any())
                                return junction
                                    .SupplyPaymentTask
                                    .MergedServices
                                    .First()
                                    .SupplyOrganization.Id.Equals(message.OutcomePaymentOrder.ConsumableProductOrganization.Id);

                            if (junction.SupplyPaymentTask.VehicleServices.Any())
                                return junction
                                    .SupplyPaymentTask
                                    .VehicleServices
                                    .First()
                                    .VehicleOrganization.Id.Equals(message.OutcomePaymentOrder.ConsumableProductOrganization.Id);

                            if (junction.SupplyPaymentTask.BillOfLadingServices.Any())
                                return junction
                                    .SupplyPaymentTask
                                    .BillOfLadingServices
                                    .First()
                                    .SupplyOrganization.Id.Equals(message.OutcomePaymentOrder.ConsumableProductOrganization.Id);

                            if (junction.SupplyPaymentTask.ConsumablesOrder != null)
                                return junction
                                    .SupplyPaymentTask
                                    .ConsumablesOrder
                                    .ConsumableProductOrganization.Id.Equals(message.OutcomePaymentOrder.ConsumableProductOrganization.Id);

                            return false;
                        });

                //Check is all SupplyOrganization's as Supplier's use same SupplyOrganizationAgreement
                if (isSameSupplier)
                    isSameSupplierAgreement =
                        message
                            .OutcomePaymentOrder
                            .OutcomePaymentOrderSupplyPaymentTasks
                            .Where(j => j.SupplyPaymentTask != null && !j.SupplyPaymentTask.IsNew())
                            .All(junction => {
                                if (junction.SupplyPaymentTask.ContainerServices.Any())
                                    return junction
                                        .SupplyPaymentTask
                                        .ContainerServices
                                        .First()
                                        .SupplyOrganizationAgreement.Id.Equals(message.OutcomePaymentOrder.SupplyOrganizationAgreement.Id);
                                if (junction.SupplyPaymentTask.BrokerServices.Any())
                                    return junction
                                        .SupplyPaymentTask
                                        .BrokerServices
                                        .First()
                                        .SupplyOrganizationAgreement.Id.Equals(message.OutcomePaymentOrder.SupplyOrganizationAgreement.Id);
                                if (junction.SupplyPaymentTask.PortWorkServices.Any())
                                    return junction
                                        .SupplyPaymentTask
                                        .PortWorkServices
                                        .First()
                                        .SupplyOrganizationAgreement.Id.Equals(message.OutcomePaymentOrder.SupplyOrganizationAgreement.Id);
                                if (junction.SupplyPaymentTask.TransportationServices.Any())
                                    return junction
                                        .SupplyPaymentTask
                                        .TransportationServices
                                        .First()
                                        .SupplyOrganizationAgreement.Id.Equals(message.OutcomePaymentOrder.SupplyOrganizationAgreement.Id);
                                if (junction.SupplyPaymentTask.PortCustomAgencyServices.Any())
                                    return junction
                                        .SupplyPaymentTask
                                        .PortCustomAgencyServices
                                        .First()
                                        .SupplyOrganizationAgreement.Id.Equals(message.OutcomePaymentOrder.SupplyOrganizationAgreement.Id);
                                if (junction.SupplyPaymentTask.CustomAgencyServices.Any())
                                    return junction
                                        .SupplyPaymentTask
                                        .CustomAgencyServices
                                        .First()
                                        .SupplyOrganizationAgreement.Id.Equals(message.OutcomePaymentOrder.SupplyOrganizationAgreement.Id);
                                if (junction.SupplyPaymentTask.PlaneDeliveryServices.Any())
                                    return junction
                                        .SupplyPaymentTask
                                        .PlaneDeliveryServices
                                        .First()
                                        .SupplyOrganizationAgreement.Id.Equals(message.OutcomePaymentOrder.SupplyOrganizationAgreement.Id);
                                if (junction.SupplyPaymentTask.VehicleDeliveryServices.Any())
                                    return junction
                                        .SupplyPaymentTask
                                        .VehicleDeliveryServices
                                        .First()
                                        .SupplyOrganizationAgreement.Id.Equals(message.OutcomePaymentOrder.SupplyOrganizationAgreement.Id);
                                if (junction.SupplyPaymentTask.MergedServices.Any())
                                    return junction
                                        .SupplyPaymentTask
                                        .MergedServices
                                        .First()
                                        .SupplyOrganizationAgreement.Id.Equals(message.OutcomePaymentOrder.SupplyOrganizationAgreement.Id);
                                if (junction.SupplyPaymentTask.VehicleServices.Any())
                                    return junction
                                        .SupplyPaymentTask
                                        .VehicleServices
                                        .First()
                                        .SupplyOrganizationAgreement.Id.Equals(message.OutcomePaymentOrder.SupplyOrganizationAgreement.Id);
                                if (junction.SupplyPaymentTask.BillOfLadingServices.Any())
                                    return junction
                                        .SupplyPaymentTask
                                        .BillOfLadingServices
                                        .First()
                                        .SupplyOrganizationAgreement.Id.Equals(message.OutcomePaymentOrder.SupplyOrganizationAgreement.Id);
                                if (junction.SupplyPaymentTask.ConsumablesOrder != null)
                                    return junction
                                        .SupplyPaymentTask
                                        .ConsumablesOrder
                                        .SupplyOrganizationAgreement.Id.Equals(message.OutcomePaymentOrder.SupplyOrganizationAgreement.Id);

                                return false;
                            });
            } else if (message.OutcomePaymentOrder.ClientAgreement != null) {
                isSameSupplier =
                    message
                        .OutcomePaymentOrder
                        .OutcomePaymentOrderSupplyPaymentTasks
                        .Where(j => j.SupplyPaymentTask != null && !j.SupplyPaymentTask.IsNew())
                        .All(junction => {
                            if (task.SupplyPaymentTask.PaymentDeliveryProtocols.Any()) {
                                SupplyOrderPaymentDeliveryProtocol protocol = task.SupplyPaymentTask.PaymentDeliveryProtocols.First();

                                if (protocol.SupplyProForm != null) {
                                    if (!protocol.SupplyProForm.SupplyOrders.Any()) return false;

                                    SupplyOrder order = protocol.SupplyProForm.SupplyOrders.First();

                                    if (order.Client != null && order.Client.ClientAgreements.Any())
                                        return order
                                            .Client
                                            .ClientAgreements
                                            .First()
                                            .Id.Equals(message.OutcomePaymentOrder.ClientAgreement.Id);
                                } else {
                                    if (protocol.SupplyInvoice?.SupplyOrder?.ClientAgreement != null)
                                        return protocol
                                            .SupplyInvoice
                                            .SupplyOrder
                                            .ClientAgreement
                                            .Id.Equals(message.OutcomePaymentOrder.ClientAgreement.Id);
                                }
                            } else if (task.SupplyPaymentTask.SupplyOrderUkrainePaymentDeliveryProtocols.Any()) {
                                SupplyOrderUkrainePaymentDeliveryProtocol protocol = task.SupplyPaymentTask.SupplyOrderUkrainePaymentDeliveryProtocols.First();

                                if (protocol.SupplyOrderUkraine?.ClientAgreement != null)
                                    return protocol
                                        .SupplyOrderUkraine
                                        .ClientAgreement
                                        .Id.Equals(message.OutcomePaymentOrder.ClientAgreement.Id);
                            }

                            return false;
                        });
            } else if (message.OutcomePaymentOrder.SupplyOrderPolandPaymentDeliveryProtocol != null) {
                isSameSupplier =
                    message
                        .OutcomePaymentOrder
                        .OutcomePaymentOrderSupplyPaymentTasks
                        .Where(j => j.SupplyPaymentTask != null && !j.SupplyPaymentTask.IsNew())
                        .All(j => j
                            .SupplyPaymentTask
                            .SupplyOrderPolandPaymentDeliveryProtocols.Any(p => !p.IsNew())
                        );
            } else {
                isSameSupplier = false;
            }

            if (!isSameSupplier)
                Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.ALL_TASKS_SHOULD_HAVE_SAME_SUPPLIER));
            else if (!isSameSupplierAgreement)
                Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.ALL_TASKS_SHOULD_HAVE_SAME_SUPPLIER_AGREEMENT));
            else
                try {
                    if (message.OutcomePaymentOrder.ClientAgreement == null && message.OutcomePaymentOrder.ConsumableProductOrganization == null) {
                        message.OutcomePaymentOrder.ClientAgreementId = null;
                        message.OutcomePaymentOrder.ConsumableProductOrganizationId = null;
                        message.OutcomePaymentOrder.SupplyOrganizationAgreementId = null;
                    } else if (message.OutcomePaymentOrder.ConsumableProductOrganization != null) {
                        message.OutcomePaymentOrder.ClientAgreementId = null;
                        message.OutcomePaymentOrder.SupplyOrderPolandPaymentDeliveryProtocolId = null;

                        message.OutcomePaymentOrder.ConsumableProductOrganizationId = message.OutcomePaymentOrder.ConsumableProductOrganization.Id;
                        message.OutcomePaymentOrder.SupplyOrganizationAgreementId = message.OutcomePaymentOrder.SupplyOrganizationAgreement.Id;

                        ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
                            _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);

                        message.OutcomePaymentOrder.SupplyOrganizationAgreement =
                            supplyOrganizationAgreementRepository
                                .GetById(message.OutcomePaymentOrder.SupplyOrganizationAgreementId.Value);

                        Currency euroCurrency = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

                        PaymentOrdersCurrencyConvertor paymentOrdersCurrencyConvertor = new(
                            euroCurrency,
                            paymentCurrencyRegister.Currency,
                            message.OutcomePaymentOrder.FromDate,
                            _exchangeRateRepositoryFactory.NewExchangeRateRepository(connection),
                            _exchangeRateRepositoryFactory.NewCrossExchangeRateRepository(connection));

                        message.OutcomePaymentOrder.EuroAmount = paymentOrdersCurrencyConvertor.ConvertAmountToEuro(message.OutcomePaymentOrder.Amount);

                        AgreementConversionResult agreementConversionResult = paymentOrdersCurrencyConvertor.GetConvertedAmountToAgreementCurrency(
                            message.OutcomePaymentOrder.Amount,
                            message.OutcomePaymentOrder.ExchangeRate,
                            message.OutcomePaymentOrder.SupplyOrganizationAgreement.Currency);

                        message.OutcomePaymentOrder.AfterExchangeAmount = agreementConversionResult.InAgreementCurrencyAmount;
                        message.OutcomePaymentOrder.ExchangeRate = agreementConversionResult.ExchangeRate;

                        message.OutcomePaymentOrder.SupplyOrganizationAgreement.CurrentEuroAmount =
                            Math.Round(
                                message.OutcomePaymentOrder.SupplyOrganizationAgreement.CurrentEuroAmount
                                +
                                message.OutcomePaymentOrder.EuroAmount,
                                2
                            );
                        message.OutcomePaymentOrder.SupplyOrganizationAgreement.CurrentAmount =
                            Math.Round(
                                message.OutcomePaymentOrder.SupplyOrganizationAgreement.CurrentAmount
                                +
                                message.OutcomePaymentOrder.AfterExchangeAmount,
                                2
                            );

                        supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.OutcomePaymentOrder.SupplyOrganizationAgreement);
                    } else if (message.OutcomePaymentOrder.ClientAgreement != null) {
                        message.OutcomePaymentOrder.SupplyOrganizationAgreementId = null;
                        message.OutcomePaymentOrder.ConsumableProductOrganizationId = null;
                        message.OutcomePaymentOrder.SupplyOrderPolandPaymentDeliveryProtocolId = null;

                        message.OutcomePaymentOrder.ClientAgreementId = message.OutcomePaymentOrder.ClientAgreement.Id;

                        Currency euro = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

                        PaymentOrdersCurrencyConvertor paymentOrdersCurrencyConvertor = new(
                            euro,
                            paymentCurrencyRegister.Currency,
                            message.OutcomePaymentOrder.FromDate,
                            _exchangeRateRepositoryFactory.NewExchangeRateRepository(connection),
                            _exchangeRateRepositoryFactory.NewCrossExchangeRateRepository(connection));

                        message.OutcomePaymentOrder.EuroAmount = paymentOrdersCurrencyConvertor.ConvertAmountToEuro(message.OutcomePaymentOrder.Amount);

                        AgreementConversionResult agreementConversionResult = paymentOrdersCurrencyConvertor.GetConvertedAmountToAgreementCurrency(
                            message.OutcomePaymentOrder.Amount,
                            message.OutcomePaymentOrder.ExchangeRate,
                            message.OutcomePaymentOrder.ClientAgreement.Agreement.Currency);

                        message.OutcomePaymentOrder.AfterExchangeAmount = agreementConversionResult.InAgreementCurrencyAmount;
                        message.OutcomePaymentOrder.ExchangeRate = agreementConversionResult.ExchangeRate;

                        message.OutcomePaymentOrder.ClientAgreement.CurrentAmount =
                            Math.Round(
                                message.OutcomePaymentOrder.ClientAgreement.CurrentAmount
                                -
                                message.OutcomePaymentOrder.EuroAmount,
                                2
                            );

                        _clientRepositoriesFactory
                            .NewClientBalanceMovementRepository(connection)
                            .AddOutMovement(
                                new ClientBalanceMovement {
                                    ClientAgreementId = message.OutcomePaymentOrder.ClientAgreement.Id,
                                    Amount = message.OutcomePaymentOrder.EuroAmount,
                                    ExchangeRateAmount = message.OutcomePaymentOrder.ExchangeRate
                                }
                            );

                        _clientRepositoriesFactory
                            .NewClientAgreementRepository(connection)
                            .UpdateAmountByNetId(
                                message.OutcomePaymentOrder.ClientAgreement.NetUid,
                                message.OutcomePaymentOrder.ClientAgreement.CurrentAmount
                            );
                    }

                    decimal toPayAmount = decimal.Zero + message.OutcomePaymentOrder.AfterExchangeAmount;

                    List<OutcomePaymentOrderSupplyPaymentTask> junctionsToAdd = new();

                    foreach (OutcomePaymentOrderSupplyPaymentTask junction in message
                                 .OutcomePaymentOrder
                                 .OutcomePaymentOrderSupplyPaymentTasks
                                 .Where(j => j.SupplyPaymentTask != null && !j.SupplyPaymentTask.IsNew())) {
                        if (junction.SupplyPaymentTask.SupplyPaymentTaskDocuments.Any(d => d.IsNew()))
                            toAddDocuments
                                .AddRange(
                                    junction
                                        .SupplyPaymentTask
                                        .SupplyPaymentTaskDocuments
                                        .Where(d => d.IsNew())
                                        .Select(document => {
                                            document.SupplyPaymentTaskId = junction.SupplyPaymentTask.Id;

                                            return document;
                                        })
                                );

                        junction.SupplyPaymentTask = supplyPaymentTaskRepository.GetByIdWithCalculatedGrossPrice(junction.SupplyPaymentTask.Id);

                        junction.SupplyPaymentTaskId = junction.SupplyPaymentTask.Id;

                        if (toPayAmount >= junction.SupplyPaymentTask.GrossPrice) {
                            junction.SupplyPaymentTask.TaskStatus = TaskStatus.Done;

                            supplyPaymentTaskRepository.UpdateTaskStatus(junction.SupplyPaymentTask);

                            toPayAmount = Math.Round(toPayAmount - junction.SupplyPaymentTask.GrossPrice, 2);

                            junction.Amount = junction.SupplyPaymentTask.GrossPrice;

                            junctionsToAdd.Add(junction);
                        } else {
                            junction.SupplyPaymentTask.TaskStatus = TaskStatus.PartialyDone;

                            supplyPaymentTaskRepository.UpdateTaskStatus(junction.SupplyPaymentTask);

                            junction.Amount = toPayAmount;

                            junctionsToAdd.Add(junction);

                            break;
                        }
                    }

                    _supplyRepositoriesFactory
                        .NewSupplyPaymentTaskDocumentRepository(connection)
                        .Add(toAddDocuments);

                    paymentCurrencyRegister.Amount = Math.Round(paymentCurrencyRegister.Amount - message.OutcomePaymentOrder.Amount, 2);

                    paymentCurrencyRegisterRepository.UpdateAmount(paymentCurrencyRegister);

                    message.OutcomePaymentOrder.Id = outcomePaymentOrderRepository.Add(message.OutcomePaymentOrder);

                    _paymentOrderRepositoriesFactory
                        .NewOutcomePaymentOrderSupplyPaymentTaskRepository(connection)
                        .Add(
                            junctionsToAdd
                                .Select(j => {
                                    j.OutcomePaymentOrderId = message.OutcomePaymentOrder.Id;

                                    return j;
                                })
                        );

                    if (message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
                        message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovementId =
                            message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovement.Id;

                    message.OutcomePaymentOrder.PaymentMovementOperation.OutcomePaymentOrderId = message.OutcomePaymentOrder.Id;

                    _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection).Add(message.OutcomePaymentOrder.PaymentMovementOperation);

                    Sender.Tell(
                        new Tuple<OutcomePaymentOrder, string>(
                            outcomePaymentOrderRepository.GetByIdForSupplies(message.OutcomePaymentOrder.Id),
                            string.Empty
                        )
                    );
                } catch (Exception e) {
                    Console.WriteLine(e);
                }
        }
    }

    private void ProcessAddNewOutcomePaymentOrderFromTaxFreeMessage(AddNewOutcomePaymentOrderFromTaxFreeMessage message) {
        if (message.OutcomePaymentOrder == null) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.INCORRECT_OUTCOME_PAYMENT_ORDER));
            return;
        }

        if (!message.OutcomePaymentOrder.IsNew()) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.INCORRECT_OUTCOME_PAYMENT_ORDER));
            return;
        }

        if (message.OutcomePaymentOrder.Amount <= decimal.Zero) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.AMOUNT_NOT_SPECIFIED));
            return;
        }

        if ((message.OutcomePaymentOrder.Organization == null || message.OutcomePaymentOrder.Organization.IsNew()) &&
            message.OutcomePaymentOrder.OrganizationId.Equals(0)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.ORGANIZATION_NOT_SPECIFIED));
            return;
        }

        if (message.OutcomePaymentOrder.PaymentMovementOperation?.PaymentMovement == null ||
            message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovement.IsNew()) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.PAYMENT_MOVEMENT_NOT_SPECIFIED));
            return;
        }

        if ((message.OutcomePaymentOrder.PaymentCurrencyRegister == null || message.OutcomePaymentOrder.PaymentCurrencyRegister.IsNew()) &&
            message.OutcomePaymentOrder.PaymentCurrencyRegisterId.Equals(0)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.PAYMENT_CURRENCY_REGISTER_NOT_SPECIFIED));
            return;
        }

        if ((message.OutcomePaymentOrder.ClientAgreement == null || message.OutcomePaymentOrder.ClientAgreement.IsNew()) &&
            (message.OutcomePaymentOrder.OrganizationClientAgreement == null || message.OutcomePaymentOrder.OrganizationClientAgreement.IsNew())) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.PARTNER_AGREEMENT_NOT_SPECIFIED));
            return;
        }

        if (message.TaxFreeNetId.Equals(Guid.Empty)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.TAX_FREE_INVALID));
            return;
        }

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
        IPaymentCurrencyRegisterRepository paymentCurrencyRegisterRepository = _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection);

        if (message.OutcomePaymentOrder.ClientAgreement != null && !message.OutcomePaymentOrder.ClientAgreement.IsNew())
            message.OutcomePaymentOrder.ClientAgreementId = message.OutcomePaymentOrder.ClientAgreement.Id;

        if (message.OutcomePaymentOrder.OrganizationClientAgreement != null && !message.OutcomePaymentOrder.OrganizationClientAgreement.IsNew()) {
            message.OutcomePaymentOrder.OrganizationClientAgreementId = message.OutcomePaymentOrder.OrganizationClientAgreement.Id;
            message.OutcomePaymentOrder.OrganizationClientId = message.OutcomePaymentOrder.OrganizationClientAgreement.OrganizationClientId;
        }

        if (message.OutcomePaymentOrder.Organization != null && !message.OutcomePaymentOrder.Organization.IsNew())
            message.OutcomePaymentOrder.OrganizationId = message.OutcomePaymentOrder.Organization.Id;

        if (message.OutcomePaymentOrder.PaymentCurrencyRegister != null)
            message.OutcomePaymentOrder.PaymentCurrencyRegisterId = message.OutcomePaymentOrder.PaymentCurrencyRegister.Id;

        if (message.OutcomePaymentOrder.FromDate.Year.Equals(1)) message.OutcomePaymentOrder.FromDate = DateTime.UtcNow;

        PaymentCurrencyRegister paymentCurrencyRegister = paymentCurrencyRegisterRepository.GetById(message.OutcomePaymentOrder.PaymentCurrencyRegisterId);

        if (paymentCurrencyRegister == null) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.INVALID_PAYMENT_CURRENCY_REGISTER));
            return;
        }

        if (paymentCurrencyRegister.Currency.Code.ToUpper() != "PLN") {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.OPERATION_AVAILABLE_ONLY_FOR_PLN_CURRENCY));
            return;
        }

        if (paymentCurrencyRegister.Amount < message.OutcomePaymentOrder.Amount) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.INSUFFICIENT_OF_FUNDS));
            return;
        }

        ITaxFreeRepository taxFreeRepository = _supplyUkraineRepositoriesFactory.NewTaxFreeRepository(connection, exchangeRateConnection);

        TaxFree taxFree = taxFreeRepository.GetByNetId(message.TaxFreeNetId);

        if (taxFree == null) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.TAX_FREE_INVALID));
            return;
        }

        if (taxFree.TaxFreePackList == null || !taxFree.TaxFreePackList.IsSent) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.TAX_FREE_PACK_LIST_SHOULD_BE_SENT));
            return;
        }

        double daysDifference = (message.OutcomePaymentOrder.FromDate - (taxFree.FormedDate ?? taxFree.Created)).TotalDays;

        if (daysDifference < 0) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.FROM_DATE_SHOULD_BE_GREATER_THAN_TAX_FREE_CREATED));
            return;
        }

        if (daysDifference > 180) {
            Sender.Tell(
                new Tuple<OutcomePaymentOrder, string>(
                    null,
                    OutcomePaymentOrderResourceNames.FROM_DATE_SHOULD_BE_LESS_THAN_TAX_FREE_CREATED_PLUS_HALF_YEAR
                )
            );
            return;
        }

        if (message.OutcomePaymentOrder.Amount != taxFree.VatAmountPl) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.AMOUNT_SHOULD_BE_EQUAL_TO_VAT_AMOUNT_OF_TAX_FREE));
            return;
        }

        IOutcomePaymentOrderRepository outcomePaymentOrderRepository = _paymentOrderRepositoriesFactory.NewOutcomePaymentOrderRepository(connection);

        User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

        message.OutcomePaymentOrder.TaxFreeId = taxFree.Id;
        message.OutcomePaymentOrder.UserId = user.Id;
        message.OutcomePaymentOrder.ExchangeRate = 1m;
        message.OutcomePaymentOrder.AfterExchangeAmount = message.OutcomePaymentOrder.Amount;

        paymentCurrencyRegister.Amount = Math.Round(paymentCurrencyRegister.Amount - message.OutcomePaymentOrder.Amount, 2);

        paymentCurrencyRegisterRepository.UpdateAmount(paymentCurrencyRegister);

        OutcomePaymentOrder lastRecord = outcomePaymentOrderRepository.GetLastRecord(message.OutcomePaymentOrder.PaymentCurrencyRegister!.PaymentRegister.Type);

        if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
            message.OutcomePaymentOrder.Number = message.OutcomePaymentOrder.Organization!.Code + string.Format("{0:D10}", 1);
        else
            message.OutcomePaymentOrder.Number =
                message.OutcomePaymentOrder.Organization!.Code +
                string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastRecord.Number, @"(\d+)").Value) + 1);

        message.OutcomePaymentOrder.Id = outcomePaymentOrderRepository.Add(message.OutcomePaymentOrder);

        if (message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
            message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovementId =
                message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovement.Id;

        message.OutcomePaymentOrder.PaymentMovementOperation.OutcomePaymentOrderId = message.OutcomePaymentOrder.Id;

        _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection).Add(message.OutcomePaymentOrder.PaymentMovementOperation);

        Sender.Tell(new Tuple<OutcomePaymentOrder, string>(outcomePaymentOrderRepository.GetById(message.OutcomePaymentOrder.Id), string.Empty));
    }

    private void ProcessAddNewOutcomePaymentOrderFromSadMessage(AddNewOutcomePaymentOrderFromSadMessage message) {
        if (message.OutcomePaymentOrder == null) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.INCORRECT_OUTCOME_PAYMENT_ORDER));
            return;
        }

        if (!message.OutcomePaymentOrder.IsNew()) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.INCORRECT_OUTCOME_PAYMENT_ORDER));
            return;
        }

        if (message.OutcomePaymentOrder.Amount <= decimal.Zero) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.AMOUNT_NOT_SPECIFIED));
            return;
        }

        if ((message.OutcomePaymentOrder.Organization == null || message.OutcomePaymentOrder.Organization.IsNew()) &&
            message.OutcomePaymentOrder.OrganizationId.Equals(0)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.ORGANIZATION_NOT_SPECIFIED));
            return;
        }

        if (message.OutcomePaymentOrder.PaymentMovementOperation?.PaymentMovement == null ||
            message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovement.IsNew()) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.PAYMENT_MOVEMENT_NOT_SPECIFIED));
            return;
        }

        if ((message.OutcomePaymentOrder.PaymentCurrencyRegister == null || message.OutcomePaymentOrder.PaymentCurrencyRegister.IsNew()) &&
            message.OutcomePaymentOrder.PaymentCurrencyRegisterId.Equals(0)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.PAYMENT_CURRENCY_REGISTER_NOT_SPECIFIED));
            return;
        }

        if ((message.OutcomePaymentOrder.ClientAgreement == null || message.OutcomePaymentOrder.ClientAgreement.IsNew()) &&
            (message.OutcomePaymentOrder.OrganizationClientAgreement == null || message.OutcomePaymentOrder.OrganizationClientAgreement.IsNew())) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.PARTNER_AGREEMENT_NOT_SPECIFIED));
            return;
        }

        if (message.SadNetId.Equals(Guid.Empty)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.TAX_FREE_INVALID));
            return;
        }

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
        IPaymentCurrencyRegisterRepository paymentCurrencyRegisterRepository = _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection);

        if (message.OutcomePaymentOrder.ClientAgreement != null && !message.OutcomePaymentOrder.ClientAgreement.IsNew())
            message.OutcomePaymentOrder.ClientAgreementId = message.OutcomePaymentOrder.ClientAgreement.Id;

        if (message.OutcomePaymentOrder.OrganizationClientAgreement != null && !message.OutcomePaymentOrder.OrganizationClientAgreement.IsNew()) {
            message.OutcomePaymentOrder.OrganizationClientAgreementId = message.OutcomePaymentOrder.OrganizationClientAgreement.Id;
            message.OutcomePaymentOrder.OrganizationClientId = message.OutcomePaymentOrder.OrganizationClientAgreement.OrganizationClientId;
        }

        if (message.OutcomePaymentOrder.Organization != null && !message.OutcomePaymentOrder.Organization.IsNew())
            message.OutcomePaymentOrder.OrganizationId = message.OutcomePaymentOrder.Organization.Id;

        if (message.OutcomePaymentOrder.PaymentCurrencyRegister != null)
            message.OutcomePaymentOrder.PaymentCurrencyRegisterId = message.OutcomePaymentOrder.PaymentCurrencyRegister.Id;

        if (message.OutcomePaymentOrder.FromDate.Year.Equals(1)) message.OutcomePaymentOrder.FromDate = DateTime.UtcNow;

        PaymentCurrencyRegister paymentCurrencyRegister = paymentCurrencyRegisterRepository.GetById(message.OutcomePaymentOrder.PaymentCurrencyRegisterId);

        if (paymentCurrencyRegister == null) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.INVALID_PAYMENT_CURRENCY_REGISTER));
            return;
        }

        if (paymentCurrencyRegister.Currency.Code.ToUpper() != "PLN") {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.OPERATION_AVAILABLE_ONLY_FOR_PLN_CURRENCY));
            return;
        }

        if (paymentCurrencyRegister.Amount < message.OutcomePaymentOrder.Amount) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.INSUFFICIENT_OF_FUNDS));
            return;
        }

        ISadRepository sadRepository = _supplyUkraineRepositoriesFactory.NewSadRepository(connection, exchangeRateConnection);

        Sad sad = sadRepository.GetByNetId(message.SadNetId);

        if (sad == null) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.SAD_INVALID));
            return;
        }

        if (!sad.IsSend) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.SAD_SHOULD_BE_SENT));
            return;
        }

        double daysDifference = (message.OutcomePaymentOrder.FromDate - sad.FromDate).TotalDays;

        if (daysDifference < 0) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.FROM_DATE_SHOULD_BE_GREATER_THAN_SAD_CREATED));
            return;
        }

        if (daysDifference > 180) {
            Sender.Tell(
                new Tuple<OutcomePaymentOrder, string>(
                    null,
                    OutcomePaymentOrderResourceNames.FROM_DATE_SHOULD_BE_LESS_THAN_SAD_CREATED_PLUS_HALF_YEAR
                )
            );
            return;
        }

        if (message.OutcomePaymentOrder.Amount != sad.TotalVatAmountWithMargin) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.AMOUNT_SHOULD_BE_EQUAL_TO_VAT_AMOUNT_OF_SAD));
            return;
        }

        IOutcomePaymentOrderRepository outcomePaymentOrderRepository = _paymentOrderRepositoriesFactory.NewOutcomePaymentOrderRepository(connection);

        User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

        message.OutcomePaymentOrder.SadId = sad.Id;
        message.OutcomePaymentOrder.UserId = user.Id;
        message.OutcomePaymentOrder.ExchangeRate = 1m;
        message.OutcomePaymentOrder.AfterExchangeAmount = message.OutcomePaymentOrder.Amount;

        paymentCurrencyRegister.Amount = Math.Round(paymentCurrencyRegister.Amount - message.OutcomePaymentOrder.Amount, 2);

        paymentCurrencyRegisterRepository.UpdateAmount(paymentCurrencyRegister);

        OutcomePaymentOrder lastRecord = outcomePaymentOrderRepository.GetLastRecord(message.OutcomePaymentOrder.PaymentCurrencyRegister!.PaymentRegister.Type);

        if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
            message.OutcomePaymentOrder.Number = message.OutcomePaymentOrder.Organization!.Code + string.Format("{0:D10}", 1);
        else
            message.OutcomePaymentOrder.Number =
                message.OutcomePaymentOrder.Organization!.Code +
                string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastRecord.Number, @"(\d+)").Value) + 1);

        message.OutcomePaymentOrder.Id = outcomePaymentOrderRepository.Add(message.OutcomePaymentOrder);

        if (message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
            message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovementId =
                message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovement.Id;

        message.OutcomePaymentOrder.PaymentMovementOperation.OutcomePaymentOrderId = message.OutcomePaymentOrder.Id;

        _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection).Add(message.OutcomePaymentOrder.PaymentMovementOperation);

        Sender.Tell(new Tuple<OutcomePaymentOrder, string>(outcomePaymentOrderRepository.GetById(message.OutcomePaymentOrder.Id), string.Empty));
    }

    private void ProcessUpdateOutcomePaymentOrderMessage(UpdateOutcomePaymentOrderMessage message) {
        if (message.OutcomePaymentOrder == null) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.INCORRECT_OUTCOME_PAYMENT_ORDER));

            return;
        }

        if (message.OutcomePaymentOrder.IsNew()) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.INCORRECT_OUTCOME_PAYMENT_ORDER));

            return;
        }

        if (message.OutcomePaymentOrder.Amount <= decimal.Zero) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.AMOUNT_NOT_SPECIFIED));

            return;
        }

        if (message.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(o => o.ConsumablesOrder != null) &&
            message.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(o =>
                o.ConsumablesOrder.ConsumablesOrderItems.Any(i =>
                    i.PaymentCostMovementOperation?.PaymentCostMovement == null))) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.PAYMENT_COST_MOVEMENT_NOT_SPECIFIED));

            return;
        }

        if (message.OutcomePaymentOrder.CompanyCarFuelings.Any(f => f.CompanyCar == null)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.COMPANY_CAR_NOT_SPECIFIED));

            return;
        }

        if (message.OutcomePaymentOrder.CompanyCarFuelings.Any(f => f.ConsumableProductOrganization == null)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.FUELING_CONSUMABLE_ORGANIZATION_NOT_SPECIFIED));

            return;
        }

        if (message.OutcomePaymentOrder.CompanyCarFuelings.Any(f =>
                f.PaymentCostMovementOperation?.PaymentCostMovement == null)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.FUELING_PAYMENT_COST_MOVEMENT_NOT_SPECIFIED));

            return;
        }

        if (message.OutcomePaymentOrder.CompanyCarFuelings.Any(f => f.FuelAmount <= 0)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.FUEL_AMOUNT_NOT_SPECIFIED));

            return;
        }

        if (message.OutcomePaymentOrder.CompanyCarFuelings.Any(f => f.PricePerLiter <= decimal.Zero && f.TotalPrice <= decimal.Zero)) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.FUEL_PRICE_NOT_SPECIFIED));

            return;
        }

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IOutcomePaymentOrderRepository outcomePaymentOrderRepository = _paymentOrderRepositoriesFactory.NewOutcomePaymentOrderRepository(connection);
        IPaymentCurrencyRegisterRepository paymentCurrencyRegisterRepository = _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection);

        if (message.OutcomePaymentOrder.Organization != null) message.OutcomePaymentOrder.OrganizationId = message.OutcomePaymentOrder.Organization.Id;
        if (message.OutcomePaymentOrder.Colleague != null) message.OutcomePaymentOrder.ColleagueId = message.OutcomePaymentOrder.Colleague.Id;
        if (message.OutcomePaymentOrder.FromDate.Year.Equals(1)) message.OutcomePaymentOrder.FromDate = DateTime.UtcNow;
        if (message.OutcomePaymentOrder.PaymentCurrencyRegister != null)
            message.OutcomePaymentOrder.PaymentCurrencyRegisterId = message.OutcomePaymentOrder.PaymentCurrencyRegister.Id;
        if (message.OutcomePaymentOrder.PaymentMovementOperation != null) {
            if (message.OutcomePaymentOrder.PaymentMovementOperation.IsNew()) {
                if (message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
                    message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovementId =
                        message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovement.Id;

                message.OutcomePaymentOrder.PaymentMovementOperation.OutcomePaymentOrderId = message.OutcomePaymentOrder.Id;

                _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection).Add(message.OutcomePaymentOrder.PaymentMovementOperation);
            } else {
                if (message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
                    message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovementId =
                        message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovement.Id;

                _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection).Update(message.OutcomePaymentOrder.PaymentMovementOperation);
            }
        }

        long currentUserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.CurrentUserNetId).Id;

        if (message.Auto) {
            OutcomePaymentOrder outcomeFromDb = outcomePaymentOrderRepository.GetByIdWithCalculatedAmount(message.OutcomePaymentOrder.Id);

            if (outcomeFromDb != null) {
                if (outcomeFromDb.RootAssignedPaymentOrder != null) {
                    if (outcomeFromDb.RootAssignedPaymentOrder.RootOutcomePaymentOrderId.HasValue) {
                        outcomeFromDb =
                            outcomePaymentOrderRepository
                                .GetByIdWithCalculatedAmount(
                                    outcomeFromDb.RootAssignedPaymentOrder.RootOutcomePaymentOrderId ?? 0
                                );
                    } else {
                        Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.NOT_MODIFIED_OUTCOME));
                        return;
                    }
                }

                if (message.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any()) {
                    IConsumablesOrderRepository consumablesOrderRepository = _consumablesRepositoriesFactory.NewConsumablesOrderRepository(connection);
                    IConsumablesOrderItemRepository consumablesOrderItemRepository = _consumablesRepositoriesFactory.NewConsumablesOrderItemRepository(connection);
                    IPaymentCostMovementOperationRepository paymentCostMovementOperationRepository =
                        _paymentOrderRepositoriesFactory.NewPaymentCostMovementOperationRepository(connection);

                    message.OutcomePaymentOrder.Amount = decimal.Zero;

                    foreach (OutcomePaymentOrderConsumablesOrder junction in
                             message
                                 .OutcomePaymentOrder
                                 .OutcomePaymentOrderConsumablesOrders
                                 .Where(j => j.ConsumablesOrder != null)) {
                        if (junction.ConsumablesOrder.ConsumablesStorage != null)
                            junction.ConsumablesOrder.ConsumablesStorageId = junction.ConsumablesOrder.ConsumablesStorage.Id;

                        if (junction.ConsumablesOrder.OrganizationFromDate.Year.Equals(1)) junction.ConsumablesOrder.OrganizationFromDate = DateTime.UtcNow;

                        junction.ConsumablesOrder.UserId = message.OutcomePaymentOrder.UserId;
                        junction.OutcomePaymentOrderId = message.OutcomePaymentOrder.Id;

                        if (junction.ConsumablesOrder.IsNew()) {
                            ConsumablesOrder lastConsumablesOrder = consumablesOrderRepository.GetLastRecord();

                            if (lastConsumablesOrder == null || !lastConsumablesOrder.Created.Year.Equals(DateTime.Now.Year))
                                junction.ConsumablesOrder.Number = string.Format("{0:D10}", 1);
                            else
                                junction.ConsumablesOrder.Number = string.Format("{0:D10}", Convert.ToInt32(lastConsumablesOrder.Number) + 1);

                            junction.ConsumablesOrder.Id = consumablesOrderRepository.Add(junction.ConsumablesOrder);

                            junction.ConsumablesOrderId = junction.ConsumablesOrder.Id;

                            foreach (ConsumablesOrderItem orderItem in junction
                                         .ConsumablesOrder
                                         .ConsumablesOrderItems
                                         .Where(i => i.ConsumableProductCategory != null || !i.ConsumableProductCategoryId.Equals(0))) {
                                if (orderItem.ConsumableProductCategory != null) orderItem.ConsumableProductCategoryId = orderItem.ConsumableProductCategory.Id;
                                if (orderItem.ConsumableProduct != null && !orderItem.ConsumableProduct.IsNew())
                                    orderItem.ConsumableProductId = orderItem.ConsumableProduct.Id;
                                else
                                    orderItem.ConsumableProductId = null;
                                if (orderItem.ConsumableProductOrganization != null && !orderItem.ConsumableProductOrganization.IsNew())
                                    orderItem.ConsumableProductOrganizationId = orderItem.ConsumableProductOrganization.Id;
                                else
                                    orderItem.ConsumableProductOrganizationId = null;
                                if (orderItem.SupplyOrganizationAgreement != null && !orderItem.SupplyOrganizationAgreement.IsNew())
                                    orderItem.SupplyOrganizationAgreementId = orderItem.SupplyOrganizationAgreement.Id;
                                else
                                    orderItem.SupplyOrganizationAgreementId = null;

                                if (junction.ConsumablesOrder.SupplyOrganizationAgreement != null)
                                    orderItem.SupplyOrganizationAgreementId = junction.ConsumablesOrder.SupplyOrganizationAgreement.Id;

                                message.OutcomePaymentOrder.Amount = Math.Round(message.OutcomePaymentOrder.Amount + orderItem.TotalPriceWithVAT, 2);

                                orderItem.ConsumablesOrderId = junction.ConsumablesOrderId;

                                orderItem.Id = consumablesOrderItemRepository.Add(orderItem);

                                orderItem.PaymentCostMovementOperation.PaymentCostMovementId = orderItem.PaymentCostMovementOperation.PaymentCostMovement.Id;
                                orderItem.PaymentCostMovementOperation.ConsumablesOrderItemId = orderItem.Id;

                                paymentCostMovementOperationRepository.Add(orderItem.PaymentCostMovementOperation);
                            }
                        } else {
                            junction.ConsumablesOrderId = junction.ConsumablesOrder.Id;

                            consumablesOrderRepository.Update(junction.ConsumablesOrder);

                            foreach (ConsumablesOrderItem orderItem in junction
                                         .ConsumablesOrder
                                         .ConsumablesOrderItems
                                         .Where(i => i.IsNew() && (i.ConsumableProductCategory != null || !i.ConsumableProductCategoryId.Equals(0)))) {
                                if (orderItem.ConsumableProductCategory != null) orderItem.ConsumableProductCategoryId = orderItem.ConsumableProductCategory.Id;
                                if (orderItem.ConsumableProduct != null && !orderItem.ConsumableProduct.IsNew())
                                    orderItem.ConsumableProductId = orderItem.ConsumableProduct.Id;
                                else
                                    orderItem.ConsumableProductId = null;
                                if (orderItem.ConsumableProductOrganization != null && !orderItem.ConsumableProductOrganization.IsNew())
                                    orderItem.ConsumableProductOrganizationId = orderItem.ConsumableProductOrganization.Id;
                                else
                                    orderItem.ConsumableProductOrganizationId = null;
                                if (orderItem.SupplyOrganizationAgreement != null && !orderItem.SupplyOrganizationAgreement.IsNew())
                                    orderItem.SupplyOrganizationAgreementId = orderItem.SupplyOrganizationAgreement.Id;
                                else
                                    orderItem.SupplyOrganizationAgreementId = null;

                                if (junction.ConsumablesOrder.SupplyOrganizationAgreement != null)
                                    orderItem.SupplyOrganizationAgreementId = junction.ConsumablesOrder.SupplyOrganizationAgreement.Id;

                                message.OutcomePaymentOrder.Amount = Math.Round(message.OutcomePaymentOrder.Amount + orderItem.TotalPriceWithVAT, 2);

                                orderItem.ConsumablesOrderId = junction.ConsumablesOrderId;

                                orderItem.Id = consumablesOrderItemRepository.Add(orderItem);

                                orderItem.PaymentCostMovementOperation.PaymentCostMovementId = orderItem.PaymentCostMovementOperation.PaymentCostMovement.Id;
                                orderItem.PaymentCostMovementOperation.ConsumablesOrderItemId = orderItem.Id;

                                paymentCostMovementOperationRepository.Add(orderItem.PaymentCostMovementOperation);
                            }

                            consumablesOrderItemRepository
                                .Update(
                                    junction
                                        .ConsumablesOrder
                                        .ConsumablesOrderItems
                                        .Where(i => !i.IsNew() &&
                                                    (i.ConsumableProductCategory != null || !i.ConsumableProductCategoryId.Equals(0)))
                                        .Select(item => {
                                            if (item.ConsumableProductCategory != null) item.ConsumableProductCategoryId = item.ConsumableProductCategory.Id;
                                            if (item.ConsumableProduct != null) item.ConsumableProductId = item.ConsumableProduct.Id;
                                            if (item.ConsumableProductOrganization != null)
                                                item.ConsumableProductOrganizationId = item.ConsumableProductOrganization.Id;

                                            message.OutcomePaymentOrder.Amount = Math.Round(message.OutcomePaymentOrder.Amount + item.TotalPriceWithVAT, 2);

                                            item.PaymentCostMovementOperation.PaymentCostMovementId = item.PaymentCostMovementOperation.PaymentCostMovement.Id;
                                            item.PaymentCostMovementOperation.ConsumablesOrderItemId = item.Id;

                                            paymentCostMovementOperationRepository.Update(item.PaymentCostMovementOperation);

                                            return item;
                                        })
                                );
                        }
                    }

                    _paymentOrderRepositoriesFactory
                        .NewOutcomePaymentOrderConsumablesOrderRepository(connection)
                        .Add(
                            message
                                .OutcomePaymentOrder
                                .OutcomePaymentOrderConsumablesOrders
                                .Where(j => j.IsNew() && j.ConsumablesOrder != null)
                        );
                }

                if (message.OutcomePaymentOrder.CompanyCarFuelings.Any()) {
                    ICompanyCarRepository companyCarRepository = _consumablesRepositoriesFactory.NewCompanyCarRepository(connection);
                    ICompanyCarFuelingRepository companyCarFuelingRepository = _consumablesRepositoriesFactory.NewCompanyCarFuelingRepository(connection);
                    IPaymentCostMovementOperationRepository paymentCostMovementOperationRepository =
                        _paymentOrderRepositoriesFactory.NewPaymentCostMovementOperationRepository(connection);

                    if (!message.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any()) message.OutcomePaymentOrder.Amount = decimal.Zero;

                    foreach (CompanyCarFueling fueling in message.OutcomePaymentOrder.CompanyCarFuelings.Where(f => !f.Deleted))
                        if (fueling.IsNew()) {
                            if (fueling.CompanyCar != null) fueling.CompanyCarId = fueling.CompanyCar.Id;
                            if (fueling.ConsumableProductOrganization != null) fueling.ConsumableProductOrganizationId = fueling.ConsumableProductOrganization.Id;

                            if (fueling.SupplyOrganizationAgreement != null)
                                fueling.SupplyOrganizationAgreementId = fueling.SupplyOrganizationAgreement.Id;

                            fueling.UserId = message.OutcomePaymentOrder.UserId;

                            message.OutcomePaymentOrder.Amount = Math.Round(message.OutcomePaymentOrder.Amount + fueling.TotalPriceWithVat, 2);

                            fueling.OutcomePaymentOrderId = message.OutcomePaymentOrder.Id;

                            fueling.Id = companyCarFuelingRepository.Add(fueling);

                            companyCarRepository.UpdateFuelAmountByCarId(fueling.CompanyCarId, fueling.FuelAmount);

                            fueling.PaymentCostMovementOperation.PaymentCostMovementId = fueling.PaymentCostMovementOperation.PaymentCostMovement.Id;
                            fueling.PaymentCostMovementOperation.CompanyCarFuelingId = fueling.Id;

                            paymentCostMovementOperationRepository.Add(fueling.PaymentCostMovementOperation);
                        } else {
                            if (fueling.CompanyCar != null) fueling.CompanyCarId = fueling.CompanyCar.Id;
                            if (fueling.ConsumableProductOrganization != null) fueling.ConsumableProductOrganizationId = fueling.ConsumableProductOrganization.Id;

                            if (fueling.SupplyOrganizationAgreement != null)
                                fueling.SupplyOrganizationAgreementId = fueling.SupplyOrganizationAgreement.Id;

                            fueling.UserId = message.OutcomePaymentOrder.UserId;
                            fueling.TotalPrice = Math.Round(fueling.TotalPriceWithVat - fueling.VatAmount, 2);

                            message.OutcomePaymentOrder.Amount = Math.Round(message.OutcomePaymentOrder.Amount + fueling.TotalPriceWithVat, 2);

                            fueling.OutcomePaymentOrderId = message.OutcomePaymentOrder.Id;

                            CompanyCarFueling fuelingFromDb = companyCarFuelingRepository.GetById(fueling.Id);

                            companyCarFuelingRepository.Update(fueling);

                            companyCarRepository.UpdateFuelAmountByCarId(fueling.CompanyCarId, fueling.FuelAmount - fuelingFromDb.FuelAmount);

                            if (fueling.PaymentCostMovementOperation.IsNew()) {
                                fueling.PaymentCostMovementOperation.PaymentCostMovementId = fueling.PaymentCostMovementOperation.PaymentCostMovement.Id;
                                fueling.PaymentCostMovementOperation.CompanyCarFuelingId = fueling.Id;

                                paymentCostMovementOperationRepository.Add(fueling.PaymentCostMovementOperation);
                            } else {
                                fueling.PaymentCostMovementOperation.PaymentCostMovementId = fueling.PaymentCostMovementOperation.PaymentCostMovement.Id;

                                paymentCostMovementOperationRepository.Update(fueling.PaymentCostMovementOperation);
                            }
                        }

                    companyCarFuelingRepository
                        .RemoveAllByIds(
                            message
                                .OutcomePaymentOrder
                                .CompanyCarFuelings
                                .Where(f => f.Deleted).Select(f => f.Id)
                        );
                }

                if (!outcomeFromDb.Amount.Equals(message.OutcomePaymentOrder.Amount)) {
                    PaymentCurrencyRegister paymentCurrencyRegister = paymentCurrencyRegisterRepository.GetById(outcomeFromDb.PaymentCurrencyRegisterId);

                    if (outcomeFromDb.Amount > message.OutcomePaymentOrder.Amount) {
                        decimal amountDifference = outcomeFromDb.Amount - message.OutcomePaymentOrder.Amount;

                        paymentCurrencyRegister.Amount = Math.Round(paymentCurrencyRegister.Amount + amountDifference, 2);

                        paymentCurrencyRegisterRepository.UpdateAmount(paymentCurrencyRegister);

                        IIncomePaymentOrderRepository incomeCashOrderRepository = _paymentOrderRepositoriesFactory.NewIncomePaymentOrderRepository(connection);
                        IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoryFactory.NewExchangeRateRepository(connection);
                        ICrossExchangeRateRepository crossExchangeRateRepository = _exchangeRateRepositoryFactory.NewCrossExchangeRateRepository(connection);
                        ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(connection);

                        Currency euroCurrency = currencyRepository.GetEURCurrencyIfExists();
                        Currency paymentCurrency = currencyRepository.GetById(paymentCurrencyRegister.CurrencyId);

                        PaymentOrdersCurrencyConvertor paymentOrdersCurrencyConvertor = new(
                            euroCurrency,
                            paymentCurrency,
                            DateTime.UtcNow,
                            exchangeRateRepository,
                            crossExchangeRateRepository);

                        IncomePaymentOrder newIncome = new() {
                            Amount = amountDifference,
                            EuroAmount = paymentOrdersCurrencyConvertor.ConvertAmountToEuro(amountDifference),
                            CurrencyId = paymentCurrencyRegister.CurrencyId,
                            PaymentRegisterId = paymentCurrencyRegister.PaymentRegisterId,
                            FromDate = DateTime.UtcNow,
                            UserId = currentUserId,
                            ColleagueId = outcomeFromDb.ColleagueId,
                            OrganizationId = outcomeFromDb.OrganizationId,
                            OperationType = OperationType.ReturnFromColleague
                        };

                        IncomePaymentOrder lastRecord = incomeCashOrderRepository.GetLastRecord(message.OutcomePaymentOrder.PaymentCurrencyRegister.PaymentRegister.Type);

                        if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                            newIncome.Number = message.OutcomePaymentOrder.Organization!.Code + string.Format("{0:D10}", 1);
                        else
                            newIncome.Number =
                                message.OutcomePaymentOrder.Organization!.Code +
                                string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastRecord.Number, @"(\d+)").Value) + 1);

                        newIncome.Id = incomeCashOrderRepository.Add(newIncome);

                        if (message.OutcomePaymentOrder?.PaymentMovementOperation?.PaymentMovementId != null)
                            _paymentOrderRepositoriesFactory
                                .NewPaymentMovementOperationRepository(connection)
                                .Add(new PaymentMovementOperation {
                                    IncomePaymentOrderId = newIncome.Id,
                                    PaymentMovementId = message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovementId
                                });

                        _paymentOrderRepositoriesFactory
                            .NewAssignedPaymentOrderRepository(connection)
                            .Add(new AssignedPaymentOrder {
                                RootOutcomePaymentOrderId = outcomeFromDb.Id,
                                AssignedIncomePaymentOrderId = newIncome.Id
                            });

                        message.OutcomePaymentOrder.UserId = currentUserId;

                        outcomePaymentOrderRepository.Update(message.OutcomePaymentOrder);

                        Sender.Tell(
                            new Tuple<OutcomePaymentOrder, string>(
                                outcomePaymentOrderRepository.GetById(message.OutcomePaymentOrder.Id),
                                string.Empty
                            )
                        );
                    } else {
                        decimal amountDifference = Math.Round(message.OutcomePaymentOrder.Amount - outcomeFromDb.Amount, 2);

                        if (paymentCurrencyRegister.Amount < amountDifference) {
                            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.INSUFFICIENT_OF_FUNDS));
                        } else {
                            paymentCurrencyRegister.Amount = Math.Round(paymentCurrencyRegister.Amount - amountDifference, 2);

                            paymentCurrencyRegisterRepository.UpdateAmount(paymentCurrencyRegister);

                            OutcomePaymentOrder lastRecord =
                                outcomePaymentOrderRepository.GetLastRecord(message.OutcomePaymentOrder.PaymentCurrencyRegister.PaymentRegister.Type);

                            OutcomePaymentOrder newOutcome = new() {
                                Amount = amountDifference,
                                FromDate = DateTime.UtcNow,
                                OrganizationId = outcomeFromDb.OrganizationId,
                                PaymentCurrencyRegisterId = paymentCurrencyRegister.Id,
                                UserId = currentUserId,
                                ColleagueId = outcomeFromDb.ColleagueId,
                                OperationType = OperationType.TransferToColleague
                            };

                            if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                                newOutcome.Number = message.OutcomePaymentOrder.Organization!.Code + string.Format("{0:D10}", 1);
                            else
                                newOutcome.Number =
                                    message.OutcomePaymentOrder.Organization!.Code +
                                    string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastRecord.Number, @"(\d+)").Value) + 1);

                            newOutcome.Id = outcomePaymentOrderRepository.Add(newOutcome);

                            if (message.OutcomePaymentOrder?.PaymentMovementOperation?.PaymentMovementId != null)
                                _paymentOrderRepositoriesFactory
                                    .NewPaymentMovementOperationRepository(connection)
                                    .Add(new PaymentMovementOperation {
                                        OutcomePaymentOrderId = newOutcome.Id,
                                        PaymentMovementId = message.OutcomePaymentOrder.PaymentMovementOperation.PaymentMovementId
                                    });

                            _paymentOrderRepositoriesFactory
                                .NewAssignedPaymentOrderRepository(connection)
                                .Add(new AssignedPaymentOrder {
                                    RootOutcomePaymentOrderId = outcomeFromDb.Id,
                                    AssignedOutcomePaymentOrderId = newOutcome.Id
                                });

                            message.OutcomePaymentOrder.UserId = currentUserId;

                            outcomePaymentOrderRepository.Update(message.OutcomePaymentOrder);

                            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(outcomePaymentOrderRepository.GetById(message.OutcomePaymentOrder.Id),
                                string.Empty));
                        }
                    }
                } else {
                    message.OutcomePaymentOrder.UserId = currentUserId;

                    outcomePaymentOrderRepository.Update(message.OutcomePaymentOrder);

                    Sender.Tell(new Tuple<OutcomePaymentOrder, string>(outcomePaymentOrderRepository.GetById(message.OutcomePaymentOrder.Id), string.Empty));
                }
            } else {
                Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, OutcomePaymentOrderResourceNames.INCORRECT_OUTCOME_PAYMENT_ORDER));
            }
        } else {
            if (message.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any()) {
                IConsumablesOrderRepository consumablesOrderRepository = _consumablesRepositoriesFactory.NewConsumablesOrderRepository(connection);
                IConsumablesOrderItemRepository consumablesOrderItemRepository = _consumablesRepositoriesFactory.NewConsumablesOrderItemRepository(connection);
                IPaymentCostMovementOperationRepository paymentCostMovementOperationRepository =
                    _paymentOrderRepositoriesFactory.NewPaymentCostMovementOperationRepository(connection);

                foreach (OutcomePaymentOrderConsumablesOrder junction in
                         message
                             .OutcomePaymentOrder
                             .OutcomePaymentOrderConsumablesOrders
                             .Where(j => j.ConsumablesOrder != null)) {
                    if (junction.ConsumablesOrder.ConsumablesStorage != null)
                        junction.ConsumablesOrder.ConsumablesStorageId = junction.ConsumablesOrder.ConsumablesStorage.Id;

                    if (junction.ConsumablesOrder.OrganizationFromDate.Year.Equals(1)) junction.ConsumablesOrder.OrganizationFromDate = DateTime.UtcNow;

                    junction.ConsumablesOrder.UserId = message.OutcomePaymentOrder.UserId;
                    junction.OutcomePaymentOrderId = message.OutcomePaymentOrder.Id;

                    if (junction.ConsumablesOrder.IsNew()) {
                        ConsumablesOrder lastConsumablesOrder = consumablesOrderRepository.GetLastRecord();

                        if (lastConsumablesOrder == null || !lastConsumablesOrder.Created.Year.Equals(DateTime.Now.Year))
                            junction.ConsumablesOrder.Number = string.Format("{0:D10}", 1);
                        else
                            junction.ConsumablesOrder.Number = string.Format("{0:D10}", Convert.ToInt32(lastConsumablesOrder.Number) + 1);

                        junction.ConsumablesOrder.Id = consumablesOrderRepository.Add(junction.ConsumablesOrder);

                        junction.ConsumablesOrderId = junction.ConsumablesOrder.Id;

                        foreach (ConsumablesOrderItem orderItem in junction
                                     .ConsumablesOrder
                                     .ConsumablesOrderItems
                                     .Where(i => i.ConsumableProductCategory != null || !i.ConsumableProductCategoryId.Equals(0))) {
                            if (orderItem.ConsumableProductCategory != null) orderItem.ConsumableProductCategoryId = orderItem.ConsumableProductCategory.Id;
                            if (orderItem.ConsumableProduct != null && !orderItem.ConsumableProduct.IsNew())
                                orderItem.ConsumableProductId = orderItem.ConsumableProduct.Id;
                            else
                                orderItem.ConsumableProductId = null;
                            if (orderItem.ConsumableProductOrganization != null && !orderItem.ConsumableProductOrganization.IsNew())
                                orderItem.ConsumableProductOrganizationId = orderItem.ConsumableProductOrganization.Id;
                            else
                                orderItem.ConsumableProductOrganizationId = null;
                            if (orderItem.SupplyOrganizationAgreement != null && !orderItem.SupplyOrganizationAgreement.IsNew())
                                orderItem.SupplyOrganizationAgreementId = orderItem.SupplyOrganizationAgreement.Id;
                            else
                                orderItem.SupplyOrganizationAgreementId = null;

                            if (junction.ConsumablesOrder.SupplyOrganizationAgreement != null)
                                orderItem.SupplyOrganizationAgreementId = junction.ConsumablesOrder.SupplyOrganizationAgreement.Id;

                            message.OutcomePaymentOrder.Amount = Math.Round(message.OutcomePaymentOrder.Amount + orderItem.TotalPriceWithVAT, 2);

                            orderItem.ConsumablesOrderId = junction.ConsumablesOrderId;

                            orderItem.Id = consumablesOrderItemRepository.Add(orderItem);

                            orderItem.PaymentCostMovementOperation.PaymentCostMovementId = orderItem.PaymentCostMovementOperation.PaymentCostMovement.Id;
                            orderItem.PaymentCostMovementOperation.ConsumablesOrderItemId = orderItem.Id;

                            paymentCostMovementOperationRepository.Add(orderItem.PaymentCostMovementOperation);
                        }
                    } else {
                        junction.ConsumablesOrderId = junction.ConsumablesOrder.Id;

                        consumablesOrderRepository.Update(junction.ConsumablesOrder);

                        foreach (ConsumablesOrderItem orderItem in junction
                                     .ConsumablesOrder
                                     .ConsumablesOrderItems
                                     .Where(i => i.IsNew() && (i.ConsumableProductCategory != null || !i.ConsumableProductCategoryId.Equals(0)))) {
                            if (orderItem.ConsumableProductCategory != null) orderItem.ConsumableProductCategoryId = orderItem.ConsumableProductCategory.Id;
                            if (orderItem.ConsumableProduct != null && !orderItem.ConsumableProduct.IsNew())
                                orderItem.ConsumableProductId = orderItem.ConsumableProduct.Id;
                            else
                                orderItem.ConsumableProductId = null;
                            if (orderItem.ConsumableProductOrganization != null && !orderItem.ConsumableProductOrganization.IsNew())
                                orderItem.ConsumableProductOrganizationId = orderItem.ConsumableProductOrganization.Id;
                            else
                                orderItem.ConsumableProductOrganizationId = null;
                            if (orderItem.SupplyOrganizationAgreement != null && !orderItem.SupplyOrganizationAgreement.IsNew())
                                orderItem.SupplyOrganizationAgreementId = orderItem.SupplyOrganizationAgreement.Id;
                            else
                                orderItem.SupplyOrganizationAgreementId = null;

                            if (junction.ConsumablesOrder.SupplyOrganizationAgreement != null)
                                orderItem.SupplyOrganizationAgreementId = junction.ConsumablesOrder.SupplyOrganizationAgreement.Id;

                            message.OutcomePaymentOrder.Amount = Math.Round(message.OutcomePaymentOrder.Amount + orderItem.TotalPriceWithVAT, 2);

                            orderItem.ConsumablesOrderId = junction.ConsumablesOrderId;

                            orderItem.Id = consumablesOrderItemRepository.Add(orderItem);

                            orderItem.PaymentCostMovementOperation.PaymentCostMovementId = orderItem.PaymentCostMovementOperation.PaymentCostMovement.Id;
                            orderItem.PaymentCostMovementOperation.ConsumablesOrderItemId = orderItem.Id;

                            paymentCostMovementOperationRepository.Add(orderItem.PaymentCostMovementOperation);
                        }

                        consumablesOrderItemRepository
                            .Update(
                                junction
                                    .ConsumablesOrder
                                    .ConsumablesOrderItems
                                    .Where(i => !i.IsNew() &&
                                                (i.ConsumableProductCategory != null || !i.ConsumableProductCategoryId.Equals(0)))
                                    .Select(item => {
                                        if (item.ConsumableProductCategory != null) item.ConsumableProductCategoryId = item.ConsumableProductCategory.Id;
                                        if (item.ConsumableProduct != null) item.ConsumableProductId = item.ConsumableProduct.Id;
                                        if (item.ConsumableProductOrganization != null)
                                            item.ConsumableProductOrganizationId = item.ConsumableProductOrganization.Id;

                                        message.OutcomePaymentOrder.Amount = Math.Round(message.OutcomePaymentOrder.Amount + item.TotalPriceWithVAT, 2);

                                        item.PaymentCostMovementOperation.PaymentCostMovementId = item.PaymentCostMovementOperation.PaymentCostMovement.Id;
                                        item.PaymentCostMovementOperation.ConsumablesOrderItemId = item.Id;

                                        paymentCostMovementOperationRepository.Update(item.PaymentCostMovementOperation);

                                        return item;
                                    })
                            );
                    }
                }

                _paymentOrderRepositoriesFactory
                    .NewOutcomePaymentOrderConsumablesOrderRepository(connection)
                    .Add(
                        message
                            .OutcomePaymentOrder
                            .OutcomePaymentOrderConsumablesOrders
                            .Where(j => j.IsNew() && j.ConsumablesOrder != null)
                    );
            }

            if (message.OutcomePaymentOrder.CompanyCarFuelings.Any()) {
                ICompanyCarRepository companyCarRepository = _consumablesRepositoriesFactory.NewCompanyCarRepository(connection);
                ICompanyCarFuelingRepository companyCarFuelingRepository = _consumablesRepositoriesFactory.NewCompanyCarFuelingRepository(connection);
                IPaymentCostMovementOperationRepository paymentCostMovementOperationRepository =
                    _paymentOrderRepositoriesFactory.NewPaymentCostMovementOperationRepository(connection);

                foreach (CompanyCarFueling fueling in message.OutcomePaymentOrder.CompanyCarFuelings.Where(f => !f.Deleted))
                    if (fueling.IsNew()) {
                        if (fueling.CompanyCar != null) fueling.CompanyCarId = fueling.CompanyCar.Id;
                        if (fueling.ConsumableProductOrganization != null) fueling.ConsumableProductOrganizationId = fueling.ConsumableProductOrganization.Id;

                        if (fueling.SupplyOrganizationAgreement != null)
                            fueling.SupplyOrganizationAgreementId = fueling.SupplyOrganizationAgreement.Id;

                        fueling.UserId = message.OutcomePaymentOrder.UserId;

                        message.OutcomePaymentOrder.Amount = Math.Round(message.OutcomePaymentOrder.Amount + fueling.TotalPriceWithVat, 2);

                        fueling.OutcomePaymentOrderId = message.OutcomePaymentOrder.Id;

                        fueling.Id = companyCarFuelingRepository.Add(fueling);

                        companyCarRepository.UpdateFuelAmountByCarId(fueling.CompanyCarId, fueling.FuelAmount);

                        fueling.PaymentCostMovementOperation.PaymentCostMovementId = fueling.PaymentCostMovementOperation.PaymentCostMovement.Id;
                        fueling.PaymentCostMovementOperation.CompanyCarFuelingId = fueling.Id;

                        paymentCostMovementOperationRepository.Add(fueling.PaymentCostMovementOperation);
                    } else {
                        if (fueling.CompanyCar != null) fueling.CompanyCarId = fueling.CompanyCar.Id;
                        if (fueling.ConsumableProductOrganization != null) fueling.ConsumableProductOrganizationId = fueling.ConsumableProductOrganization.Id;

                        if (fueling.CompanyCar != null) fueling.CompanyCarId = fueling.CompanyCar.Id;
                        if (fueling.ConsumableProductOrganization != null) fueling.ConsumableProductOrganizationId = fueling.ConsumableProductOrganization.Id;

                        if (fueling.SupplyOrganizationAgreement != null)
                            fueling.SupplyOrganizationAgreementId = fueling.SupplyOrganizationAgreement.Id;

                        fueling.UserId = message.OutcomePaymentOrder.UserId;

                        message.OutcomePaymentOrder.Amount = Math.Round(message.OutcomePaymentOrder.Amount + fueling.TotalPriceWithVat, 2);

                        fueling.OutcomePaymentOrderId = message.OutcomePaymentOrder.Id;

                        CompanyCarFueling fuelingFromDb = companyCarFuelingRepository.GetById(fueling.Id);

                        companyCarFuelingRepository.Update(fueling);

                        companyCarRepository.UpdateFuelAmountByCarId(fueling.CompanyCarId, fueling.FuelAmount - fuelingFromDb.FuelAmount);

                        if (fueling.PaymentCostMovementOperation.IsNew()) {
                            fueling.PaymentCostMovementOperation.PaymentCostMovementId = fueling.PaymentCostMovementOperation.PaymentCostMovement.Id;
                            fueling.PaymentCostMovementOperation.CompanyCarFuelingId = fueling.Id;

                            paymentCostMovementOperationRepository.Add(fueling.PaymentCostMovementOperation);
                        } else {
                            fueling.PaymentCostMovementOperation.PaymentCostMovementId = fueling.PaymentCostMovementOperation.PaymentCostMovement.Id;

                            paymentCostMovementOperationRepository.Update(fueling.PaymentCostMovementOperation);
                        }
                    }

                companyCarFuelingRepository
                    .RemoveAllByIds(
                        message.OutcomePaymentOrder.CompanyCarFuelings.Where(f => f.Deleted).Select(f => f.Id)
                    );
            }

            message.OutcomePaymentOrder.UserId = currentUserId;

            outcomePaymentOrderRepository.Update(message.OutcomePaymentOrder);

            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(outcomePaymentOrderRepository.GetById(message.OutcomePaymentOrder.Id), string.Empty));
        }
    }

    private void ProcessCancelOutcomePaymentOrderByNetIdMessage(CancelOutcomePaymentOrderByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();

        IOutcomePaymentOrderRepository outcomePaymentOrderRepository = _paymentOrderRepositoriesFactory.NewOutcomePaymentOrderRepository(connection);

        OutcomePaymentOrder outcomeFromDb = outcomePaymentOrderRepository.GetByNetId(message.NetId);

        if (outcomeFromDb == null) {
            Sender.Tell(
                new Tuple<bool, string, OutcomePaymentOrder, string>(
                    false,
                    OutcomePaymentOrderResourceNames.OUTCOME_PAYMENT_ORDERS_NOT_EXIST,
                    null,
                    string.Empty));
            return;
        }

        if (outcomeFromDb.IsCanceled) {
            Sender.Tell(
                new Tuple<bool, string, OutcomePaymentOrder, string>(
                    false,
                    OutcomePaymentOrderResourceNames.OUTCOME_ALREADY_CANCELED,
                    null,
                    string.Empty));

            return;
        }

        IPaymentRegisterRepository paymentRegisterRepository = _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection);

        PaymentRegister register = paymentRegisterRepository.GetById(outcomeFromDb.PaymentCurrencyRegister.PaymentRegisterId);

        PaymentCurrencyRegister paymentCurrencyRegister =
            register.PaymentCurrencyRegisters.First(r => r.CurrencyId.Equals(outcomeFromDb.PaymentCurrencyRegister.CurrencyId));

        paymentCurrencyRegister.Amount = decimal.Round(paymentCurrencyRegister.Amount + outcomeFromDb.Amount, 2, MidpointRounding.AwayFromZero);

        _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection).UpdateAmount(paymentCurrencyRegister);

        outcomeFromDb.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

        outcomeFromDb.IsCanceled = true;

        outcomePaymentOrderRepository.Update(outcomeFromDb);

        Sender.Tell(
            new Tuple<bool, string, OutcomePaymentOrder, string>(
                true,
                OutcomePaymentOrderResourceNames.OUTCOME_CANCELED,
                outcomeFromDb,
                string.Empty));
    }
}