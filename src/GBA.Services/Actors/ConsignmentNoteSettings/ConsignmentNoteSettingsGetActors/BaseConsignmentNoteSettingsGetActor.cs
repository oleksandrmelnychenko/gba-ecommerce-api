using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.ReSaleModels;
using GBA.Domain.Messages.ConsignmentNoteSettings;
using GBA.Domain.Repositories.ConsignmentNoteSettings.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.ReSales.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.VatRates.Contracts;

namespace GBA.Services.Actors.ConsignmentNoteSettings.ConsignmentNoteSettingsGetActors;

public sealed class BaseConsignmentNoteSettingsGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConsignmentNoteSettingRepositoriesFactory _consignmentNoteSettingRepositoriesFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IReSaleRepositoriesFactory _reSaleRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly IVatRateRepositoriesFactory _vatRateRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public BaseConsignmentNoteSettingsGetActor(
        IDbConnectionFactory connectionFactory,
        IConsignmentNoteSettingRepositoriesFactory consignmentNoteSettingRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IXlsFactoryManager xlsFactoryManager,
        IReSaleRepositoriesFactory reSaleRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        IVatRateRepositoriesFactory vatRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _consignmentNoteSettingRepositoriesFactory = consignmentNoteSettingRepositoriesFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _xlsFactoryManager = xlsFactoryManager;
        _reSaleRepositoriesFactory = reSaleRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _vatRateRepositoriesFactory = vatRateRepositoriesFactory;

        Receive<GetByNetIdConsignmentNoteSettingMessage>(ProcessGetByNetIdConsignmentNoteSetting);

        Receive<GetAllConsignmentNoteSettingsMessage>(ProcessGetAllConsignmentNoteSettings);

        Receive<ExportConsignmentNoteDocumentMessage>(ProcessExportConsignmentNoteDocument);
    }

    private void ProcessGetByNetIdConsignmentNoteSetting(GetByNetIdConsignmentNoteSettingMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _consignmentNoteSettingRepositoriesFactory
                    .NewConsignmentNoteSettingRepository(connection)
                    .GetByNetId(message.NetId)
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetAllConsignmentNoteSettings(GetAllConsignmentNoteSettingsMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _consignmentNoteSettingRepositoriesFactory
                    .NewConsignmentNoteSettingRepository(connection)
                    .GetAll(message.ForReSale)
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessExportConsignmentNoteDocument(ExportConsignmentNoteDocumentMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ICurrencyRepository currencyRepository =
                _currencyRepositoriesFactory.NewCurrencyRepository(connection);
            IExchangeRateRepository exchangeRateRepository =
                _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);

            string xlsDoc;
            string pdfDoc;

            if (message.ForReSale) {
                UpdatedReSaleModel reSale = _reSaleRepositoriesFactory.NewReSaleRepository(connection).GetUpdatedByNetId(message.NetId);

                reSale = GetReSaleModelForUpdate(reSale);

                if (reSale.ReSale.Organization.VatRateId.HasValue)
                    reSale.ReSale.Organization.VatRate =
                        _vatRateRepositoriesFactory
                            .NewVatRateRepository(connection)
                            .GetById(reSale.ReSale.Organization.VatRateId.Value);

                if (reSale.ReSale.ClientAgreementId.HasValue) {
                    Currency uah = currencyRepository.GetUAHCurrencyIfExists();

                    if (reSale.ReSale.ClientAgreement.Agreement.CurrencyId.HasValue &&
                        !reSale.ReSale.ClientAgreement.Agreement.CurrencyId.Value.Equals(uah.Id)) {
                        ExchangeRate uahToAgreement =
                            exchangeRateRepository
                                .GetByCurrencyIdAndCode(uah.Id, reSale.ReSale.ClientAgreement.Agreement.Currency.Code,
                                    reSale.ReSale.ChangedToInvoice ?? reSale.ReSale.Created);

                        foreach (UpdatedReSaleItemModel reSaleItem in reSale.ReSaleItemModels) {
                            reSaleItem.Price /= uahToAgreement.Amount;
                            reSaleItem.SalePrice /= uahToAgreement.Amount;
                            reSaleItem.Amount /= uahToAgreement.Amount;
                        }
                    }
                }

                (xlsDoc, pdfDoc) =
                    _xlsFactoryManager
                        .NewConsignmentNoteDocumentManager()
                        .GetPrintReSaleConsignmentNoteDocument(message.PathToFolder, reSale, message.Setting);
            } else {
                Sale sale = _saleRepositoriesFactory.NewSaleRepository(connection).GetByNetId(message.NetId);

                (xlsDoc, pdfDoc) =
                    _xlsFactoryManager
                        .NewConsignmentNoteDocumentManager()
                        .GetPrintSaleConsignmentNoteDocument(message.PathToFolder, sale, message.Setting);
            }

            Sender.Tell(new Tuple<string, string>(xlsDoc, pdfDoc));
        } catch {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }

    private static UpdatedReSaleModel GetReSaleModelForUpdate(UpdatedReSaleModel toReturn) {
        decimal vatPercent = Convert.ToDecimal(toReturn.ReSale.Organization.VatRate?.Value ?? 0);

        foreach (ReSaleItem reSaleItem in toReturn.ReSale.ReSaleItems)
            if (!toReturn.ReSaleItemModels.Any(x => x.ConsignmentItem.Id.Equals(reSaleItem.ReSaleAvailability.ConsignmentItem.Id))) {
                UpdatedReSaleItemModel item = new() {
                    ReSaleItems = new List<ReSaleItem> { reSaleItem },
                    Price = reSaleItem.ReSaleAvailability.PricePerItem,
                    Qty = reSaleItem.ReSaleAvailability.RemainingQty + reSaleItem.Qty,
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