using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.EntityHelpers.Accounting;
using GBA.Domain.EntityHelpers.ReSaleModels;
using GBA.Domain.Repositories.Accounting.Contracts;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.ReSales.Contracts;
using GBA.Services.Services.UserManagement.Contracts;

namespace GBA.Services.Services.UserManagement;

public sealed class AccountingCashFlowService : IAccountingCashFlowService {
    private readonly IAccountingRepositoriesFactory _accountingRepositoriesFactory;
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IReSaleRepositoriesFactory _reSaleRepositoriesFactory;

    public AccountingCashFlowService(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IAccountingRepositoriesFactory accountingRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IReSaleRepositoriesFactory reSaleRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _accountingRepositoriesFactory = accountingRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _reSaleRepositoriesFactory = reSaleRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
    }

    public Task<AccountingCashFlow> GetAccountingCashFlow(Guid netId, DateTime from, DateTime to) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        AccountingCashFlow accountingCashFlow = null;

            Client client = _clientRepositoriesFactory.NewClientRepository(connection).GetByNetIdWithRoleAndType(netId);

            if (client != null) {
                accountingCashFlow = _clientRepositoriesFactory
                    .NewClientCashFlowRepository(connection)
                    .GetRangedByClient(client, from, to, true);
            } else {
                ClientAgreement clientAgreement =
                    _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetByNetIdWithClientRole(netId);

                if (clientAgreement != null)
                    accountingCashFlow = _clientRepositoriesFactory
                        .NewClientCashFlowRepository(connection)
                        .GetRangedByClientAgreement(
                            clientAgreement,
                            from,
                            to,
                            clientAgreement.Agreement.Currency != null && clientAgreement.Agreement.Currency.Code.ToUpper().Equals("EUR"),
                            true
                        );
                else
                    return Task.FromResult(new AccountingCashFlow());
            }

            if (accountingCashFlow != null)
                accountingCashFlow.AccountingCashFlowHeadItems =
                    _accountingRepositoriesFactory
                        .NewAccountingDocumentNameRepository(connection)
                        .GetDocumentNames(accountingCashFlow.AccountingCashFlowHeadItems);
            else
                return Task.FromResult<AccountingCashFlow>(null);

            return Task.FromResult(accountingCashFlow);
    }


    private UpdatedReSaleModel GetUpdatedReSaleByNetId(UpdatedReSaleModel updatedReSaleModel, Guid netId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        UpdatedReSaleModel toReturn;

        if (updatedReSaleModel == null) {
            toReturn = _reSaleRepositoriesFactory.NewReSaleRepository(connection).GetUpdatedByNetId(netId);

            toReturn = GetReSaleModelForUpdate(toReturn);

            if (toReturn.ReSale.ChangedToInvoice.HasValue && toReturn.ReSale.ClientAgreement != null) {
                decimal vatPercent = Convert.ToDecimal(toReturn.ReSale.Organization.VatRate?.Value ?? 0);

                Currency uah = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetUAHCurrencyIfExists();
                Currency eur = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

                ExchangeRate exchangeRateToAgreement =
                    _exchangeRateRepositoriesFactory
                        .NewExchangeRateRepository(connection)
                        .GetByCurrencyIdAndCode(uah.Id, toReturn.ReSale.ClientAgreement.Agreement.Currency.Code, toReturn.ReSale.ChangedToInvoice.Value);

                ExchangeRate exchangeRateToEur =
                    _exchangeRateRepositoriesFactory
                        .NewExchangeRateRepository(connection)
                        .GetByCurrencyIdAndCode(uah.Id, eur.Code, toReturn.ReSale.ChangedToInvoice.Value);

                foreach (UpdatedReSaleItemModel reSaleItem in toReturn.ReSaleItemModels) {
                    if (toReturn.ReSale.ClientAgreement.Agreement.Currency.Code.Equals(eur.Code) ||
                        toReturn.ReSale.ClientAgreement.Agreement.Currency.Code.Equals(uah.Code)) {
                        reSaleItem.TotalAmountEurToUah = reSaleItem.Amount;
                        reSaleItem.TotalAmount = reSaleItem.Amount / exchangeRateToEur.Amount;
                        reSaleItem.TotalVat = reSaleItem.Amount * vatPercent / (100 + vatPercent);

                        if (toReturn.ReSale.ClientAgreement.Agreement.Currency.Code.Equals(uah.Code))
                            reSaleItem.TotalAmountLocal = reSaleItem.Amount;
                        else
                            reSaleItem.TotalAmountLocal = reSaleItem.Amount / exchangeRateToEur.Amount;
                    } else {
                        reSaleItem.TotalAmountLocal = reSaleItem.Amount / exchangeRateToAgreement.Amount;
                        reSaleItem.TotalAmountEurToUah = reSaleItem.Amount;
                        reSaleItem.TotalAmount = reSaleItem.Amount / exchangeRateToEur.Amount;
                        reSaleItem.TotalVat = reSaleItem.Amount * vatPercent / (100 + vatPercent);

                        reSaleItem.TotalVat /= exchangeRateToAgreement.Amount;
                    }

                    toReturn.ReSale.TotalAmountLocal += reSaleItem.TotalAmountLocal;
                    toReturn.ReSale.TotalAmountEurToUah += reSaleItem.TotalAmountEurToUah;
                    toReturn.ReSale.TotalAmount += reSaleItem.TotalAmount;
                    toReturn.ReSale.TotalVat += reSaleItem.TotalVat;
                }
            }
        } else {
            toReturn = updatedReSaleModel;

            decimal vatPercent = Convert.ToDecimal(toReturn.ReSale.Organization.VatRate?.Value ?? 0);

            foreach (UpdatedReSaleItemModel updated in toReturn.ReSaleItemModels) {
                updated.Price = decimal.Round(updated.Price, 2, MidpointRounding.AwayFromZero);
                updated.SalePrice = decimal.Round(updated.SalePrice, 2, MidpointRounding.AwayFromZero);
                updated.Amount = decimal.Round(updated.Amount, 2, MidpointRounding.AwayFromZero);

                updated.OldValue.SalePrice = decimal.Round(updated.OldValue.SalePrice, 2, MidpointRounding.AwayFromZero);
                updated.OldValue.Amount = decimal.Round(updated.OldValue.Amount, 2, MidpointRounding.AwayFromZero);


                if (!updated.Amount.Equals(updated.OldValue.Amount)) {
                    updated.SalePrice = updated.Amount / Convert.ToDecimal(updated.QtyToReSale);
                } else if (!updated.SalePrice.Equals(updated.OldValue.SalePrice)) {
                    updated.Amount = updated.SalePrice * Convert.ToDecimal(updated.QtyToReSale);
                } else if (!updated.QtyToReSale.Equals(updated.OldValue.QtyToReSale)) {
                    if (updated.QtyToReSale > updated.Qty)
                        updated.QtyToReSale = updated.Qty;

                    if (updated.QtyToReSale.Equals(0) || updated.QtyToReSale < 0)
                        updated.QtyToReSale = 1;

                    updated.Amount = updated.SalePrice * Convert.ToDecimal(updated.QtyToReSale);
                }

                decimal amountWithoutExtraCharge = Convert.ToDecimal(updated.QtyToReSale) * updated.Price;

                if (updated.Price.Equals(updated.SalePrice))
                    updated.Profit = 0;
                else
                    updated.Profit = updated.Amount - amountWithoutExtraCharge;

                if (updated.Profit.Equals(0))
                    updated.Profitability = 0;
                else if (amountWithoutExtraCharge.Equals(0))
                    updated.Profitability = 100;
                else
                    updated.Profitability = updated.Amount / amountWithoutExtraCharge * 100 - 100;

                updated.Vat = updated.Amount * vatPercent / (100 + vatPercent);
            }
        }

        return toReturn;
    }

    private static UpdatedReSaleModel GetReSaleModelForUpdate(UpdatedReSaleModel toReturn) {
        decimal vatPercent = Convert.ToDecimal(toReturn.ReSale.Organization.VatRate?.Value ?? 0);

        foreach (ReSaleItem reSaleItem in toReturn.ReSale.ReSaleItems)
            if (!toReturn.ReSaleItemModels.Any(x => x.ConsignmentItem.Id.Equals(reSaleItem.ReSaleAvailability.ConsignmentItem.Id))) {
                UpdatedReSaleItemModel item = new() {
                    ReSaleItems = new List<ReSaleItem> { reSaleItem },
                    Price = reSaleItem.ReSaleAvailability.PricePerItem,
                    Qty = reSaleItem.RemainingQty + reSaleItem.Qty,
                    QtyToReSale = reSaleItem.Qty,
                    ConsignmentItem = reSaleItem.ReSaleAvailability.ConsignmentItem,
                    SalePrice = reSaleItem.PricePerItem,
                    Amount = Convert.ToDecimal(reSaleItem.Qty) * reSaleItem.PricePerItem
                };

                item.Price = decimal.Round(item.Price, 2, MidpointRounding.AwayFromZero);
                item.SalePrice = decimal.Round(item.SalePrice, 2, MidpointRounding.AwayFromZero);
                item.Amount = decimal.Round(item.Amount, 2, MidpointRounding.AwayFromZero);

                item.OldValue.SalePrice = decimal.Round(item.OldValue.SalePrice, 2, MidpointRounding.AwayFromZero);
                item.OldValue.Amount = decimal.Round(item.OldValue.Amount, 2, MidpointRounding.AwayFromZero);

                decimal amountWithoutExtraCharge = Convert.ToDecimal(item.QtyToReSale) * item.Price;

                if (item.Price.Equals(item.SalePrice))
                    item.Profit = 0;
                else
                    item.Profit = item.Amount - amountWithoutExtraCharge;

                if (item.Profit.Equals(0))
                    item.Profitability = 0;
                else if (amountWithoutExtraCharge.Equals(0))
                    item.Profitability = 100;
                else
                    item.Profitability = item.Amount / amountWithoutExtraCharge * 100 - 100;

                item.Vat = item.Amount * vatPercent / (100 + vatPercent);
                toReturn.ReSaleItemModels.Add(item);
            } else {
                UpdatedReSaleItemModel existItem =
                    toReturn.ReSaleItemModels.First(x =>
                        x.ConsignmentItem.Id.Equals(reSaleItem.ReSaleAvailability.ConsignmentItem.Id));

                existItem.Qty += reSaleItem.Qty;

                existItem.Price = decimal.Round(existItem.Price, 2, MidpointRounding.AwayFromZero);
                existItem.SalePrice = decimal.Round(existItem.SalePrice, 2, MidpointRounding.AwayFromZero);
                existItem.QtyToReSale += reSaleItem.Qty;
                existItem.Amount += decimal.Round(Convert.ToDecimal(reSaleItem.Qty) * reSaleItem.PricePerItem, 2, MidpointRounding.AwayFromZero);

                existItem.OldValue.SalePrice = decimal.Round(existItem.OldValue.SalePrice, 2, MidpointRounding.AwayFromZero);
                existItem.OldValue.Amount = decimal.Round(existItem.OldValue.Amount, 2, MidpointRounding.AwayFromZero);

                decimal amountWithoutExtraCharge = Convert.ToDecimal(existItem.QtyToReSale) * existItem.Price;

                if (existItem.Price.Equals(existItem.SalePrice))
                    existItem.Profit = 0;
                else
                    existItem.Profit = existItem.Amount - amountWithoutExtraCharge;

                if (existItem.Profit.Equals(0))
                    existItem.Profitability = 0;
                else if (amountWithoutExtraCharge.Equals(0))
                    existItem.Profitability = 100;
                else
                    existItem.Profitability = existItem.Amount / amountWithoutExtraCharge * 100 - 100;

                existItem.Vat = existItem.Amount * vatPercent / (100 + vatPercent);

                existItem.ReSaleItems.Add(reSaleItem);
            }

        return toReturn;
    }
}
