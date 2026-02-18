using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.EntityHelpers.ProductModels;
using GBA.Domain.EntityHelpers.ReSaleModels;
using GBA.Domain.Messages.ReSales;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Consignments.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.DocumentMonths.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using GBA.Domain.Repositories.ReSales.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Domain.Repositories.VatRates.Contracts;

namespace GBA.Services.Actors.ReSales;

public sealed class ReSaleActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConsignmentRepositoriesFactory _consignmentRepositoriesFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IDocumentMonthRepositoryFactory _documentMonthRepositoryFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;
    private readonly IReSaleRepositoriesFactory _reSaleRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IVatRateRepositoriesFactory _vatRateRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public ReSaleActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IReSaleRepositoriesFactory reSaleRepositoriesFactory,
        IXlsFactoryManager xlsFactoryManager,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IVatRateRepositoriesFactory vatRateRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IDocumentMonthRepositoryFactory documentMonthRepositoryFactory,
        IConsignmentRepositoriesFactory consignmentRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _reSaleRepositoriesFactory = reSaleRepositoriesFactory;
        _xlsFactoryManager = xlsFactoryManager;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _vatRateRepositoriesFactory = vatRateRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _documentMonthRepositoryFactory = documentMonthRepositoryFactory;
        _consignmentRepositoriesFactory = consignmentRepositoriesFactory;

        Receive<AddReSaleMessage>(ProcessAddReSaleMessage);

        Receive<GetAllReSalesMessage>(ProcessGetAllReSalesMessage);

        Receive<GetUpdatedReSaleByNetIdMessage>(ProcessGetUpdatedReSaleByNetIdMessage);

        Receive<ExportReSaleDocumentMessage>(ProcessExportReSaleDocument);

        Receive<UpdateReSaleMessage>(ProcessUpdateReSale);

        Receive<ChangeToInvoiceReSaleMessage>(ProcessChangeToInvoiceReSale);

        Receive<RemoveReSaleMessage>(ProcessRemoveReSale);

        Receive<ChangeIsCompletedMessage>(ProcessChangeIsCompleted);
    }

    private void ProcessAddReSaleMessage(AddReSaleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
        IReSaleRepository reSaleRepository = _reSaleRepositoriesFactory.NewReSaleRepository(connection);
        IReSaleItemRepository reSaleItemRepository = _reSaleRepositoriesFactory.NewReSaleItemRepository(connection);
        ISaleNumberRepository saleNumberRepository = _saleRepositoriesFactory.NewSaleNumberRepository(connection);
        IReSaleAvailabilityRepository reSaleAvailabilityRepository = _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection);

        if (HasUnavailableProducts(message.ReSale, reSaleAvailabilityRepository, out UnavailableProductsForReSaleModel unavailableProductNames)) {
            Sender.Tell(unavailableProductNames);
            return;
        }

        User user = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

        ReSale reSale = new() { UserId = user.Id };

        if (message.ReSale.ClientAgreement != null)
            reSale.ClientAgreementId = message.ReSale.ClientAgreement.Id;

        reSale.FromStorageId = message.ReSale.FromStorageId;

        reSale.OrganizationId = message.ReSale.Organization.Id;
        reSale.Comment = message.ReSale.Comment;

        SaleNumber lastSaleNumber = saleNumberRepository.GetLastRecordByOrganizationNetId(message.ReSale.Organization.NetUid);
        SaleNumber saleNumber;

        Organization organization = message.ReSale.Organization;

        string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

        if (lastSaleNumber != null && DateTime.Now.Year.Equals(lastSaleNumber.Created.Year))
            saleNumber = new SaleNumber {
                OrganizationId = organization.Id,
                Value =
                    string.Format(
                        "{0}{1}{2}",
                        organization.Code,
                        currentMonth,
                        string.Format("{0:D8}",
                            Convert.ToInt32(
                                lastSaleNumber.Value.Substring(
                                    lastSaleNumber.Organization.Code.Length + currentMonth.Length,
                                    lastSaleNumber.Value.Length - (lastSaleNumber.Organization.Code.Length + currentMonth.Length))) + 1)
                    )
            };
        else
            saleNumber = new SaleNumber {
                OrganizationId = organization.Id,
                Value = $"{organization.Code}{currentMonth}{string.Format("{0:D8}", 1)}"
            };

        reSale.SaleNumberId = saleNumberRepository.Add(saleNumber);
        reSale.SaleNumber = saleNumber;
        reSale.BaseSalePaymentStatusId =
            _saleRepositoriesFactory
                .NewBaseSalePaymentStatusRepository(connection)
                .Add(new BaseSalePaymentStatus());

        reSale.BaseLifeCycleStatusId =
            _saleRepositoriesFactory
                .NewBaseLifeCycleStatusRepository(connection)
                .Add(new BaseLifeCycleStatus());

        reSale.Id = reSaleRepository.Add(reSale);

        foreach (ReSaleAvailabilityItemModel item in message.ReSale.ReSaleAvailabilityModels)
            reSaleItemRepository.Add(new ReSaleItem {
                Qty = item.QtyToReSale,
                ReSaleId = reSale.Id,
                PricePerItem = item.SalePrice,
                ExchangeRate = item.ExchangeRate,
                ExtraCharge = item.SalePrice.Equals(0) || item.Price.Equals(0) ? 0 : item.SalePrice / item.Price,
                ProductId = item.ProductId
            });

        Sender.Tell(null);
    }

    private static bool HasUnavailableProducts(ReSaleWithReSaleAvailabilityModel reSale, IReSaleAvailabilityRepository reSaleAvailabilityRepository,
        out UnavailableProductsForReSaleModel absentProductNames) {
        List<ProductWithAvailableQty> products = new();

        foreach (ReSaleAvailabilityItemModel item in reSale.ReSaleAvailabilityModels) {
            ReSaleAvailabilityWithTotalsModel actualAvailability = reSaleAvailabilityRepository.GetActualReSaleAvailabilityByProductId(item.ProductId);

            if (actualAvailability.TotalQty + item.OldValue.QtyToReSale < item.QtyToReSale)
                products.Add(new ProductWithAvailableQty(item.ProductId, item.VendorCode, actualAvailability.TotalQty + item.OldValue.QtyToReSale));
        }

        absentProductNames = new UnavailableProductsForReSaleModel("ֳ ������ ���������� � �������� �������:", products);

        return products.Any();
    }

    private static bool HasUnavailableProducts(UpdatedReSaleModel reSale, IReSaleAvailabilityRepository reSaleAvailabilityRepository,
        out UnavailableProductsForReSaleModel absentProductNames) {
        List<ProductWithAvailableQty> products = new();

        foreach (UpdatedReSaleItemModel item in reSale.ReSaleItemModels) {
            ReSaleAvailabilityWithTotalsModel actualAvailability = reSaleAvailabilityRepository.GetActualReSaleAvailabilityByProductId(item.ReSaleItems[0].ProductId);

            if (actualAvailability.TotalQty + item.OldValue.QtyToReSale < item.QtyToReSale)
                products.Add(new ProductWithAvailableQty(item.ReSaleItems[0].ProductId, item.ReSaleItems[0].Product.VendorCode,
                    actualAvailability.TotalQty + item.OldValue.QtyToReSale));
        }

        absentProductNames = new UnavailableProductsForReSaleModel("ֳ ������ ���������� � �������� �������:", products);

        return products.Any();
    }

    private void ProcessGetAllReSalesMessage(GetAllReSalesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _reSaleRepositoriesFactory
                .NewReSaleRepository(connection)
                .GetAll(message.From, message.To, message.Limit, message.Offset, message.Status));
    }

    private void ProcessGetUpdatedReSaleByNetIdMessage(GetUpdatedReSaleByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IReSaleAvailabilityRepository reSaleAvailabilityRepository = _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection);
        UpdatedReSaleModel toReturn;

        if (message.UpdatedReSaleModel == null) {
            toReturn = _reSaleRepositoriesFactory.NewReSaleRepository(connection).GetUpdatedByNetId(message.NetId);

            toReturn = GetReSaleModelForUpdate(toReturn, _reSaleRepositoriesFactory.NewReSaleItemRepository(connection));

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
            toReturn = message.UpdatedReSaleModel;

            if (HasUnavailableProducts(message.UpdatedReSaleModel, reSaleAvailabilityRepository, out UnavailableProductsForReSaleModel unavailableProductNames)) {
                Sender.Tell(unavailableProductNames);
                return;
            }

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

        if (!toReturn.ReSale.ChangedToInvoice.HasValue)
            toReturn.ReSale.TotalPaymentAmount = toReturn.TotalAmount;

        toReturn.ReSale.DifferencePaymentAndInvoiceAmount = toReturn.TotalAmount - toReturn.ReSale.TotalPaymentAmount;

        Sender.Tell(toReturn);
    }

    private void ProcessExportReSaleDocument(ExportReSaleDocumentMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IReSaleRepository reSaleRepository =
                _reSaleRepositoriesFactory.NewReSaleRepository(connection);
            IPaymentRegisterRepository paymentRegisterRepository =
                _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection);
            ICurrencyRepository currencyRepository =
                _currencyRepositoriesFactory.NewCurrencyRepository(connection);
            IExchangeRateRepository exchangeRateRepository =
                _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
            IVatRateRepository vatRateRepository =
                _vatRateRepositoriesFactory.NewVatRateRepository(connection);


            string excelFilePath = string.Empty;
            string pdfFilePath = string.Empty;

            ReSale reSaleByNetId = reSaleRepository.GetByNetIdWithoutInfo(message.NetId);

            if (reSaleByNetId == null) {
                Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
                return;
            }

            if (message.Type.Equals(ReSaleDownloadDocumentType.PaymentDocument)) {
                if (!reSaleByNetId.ChangedToInvoice.HasValue) {
                    ReSale reSale = reSaleRepository.GetByNetId(message.NetId);

                    GetReSaleInfoForExportDocument(reSale, vatRateRepository, paymentRegisterRepository);

                    if (reSale.ClientAgreementId.HasValue) {
                        Currency uah = currencyRepository.GetUAHCurrencyIfExists();

                        if (reSale.ClientAgreement.Agreement.CurrencyId.HasValue &&
                            !reSale.ClientAgreement.Agreement.CurrencyId.Value.Equals(uah.Id)) {
                            ExchangeRate uahToAgreement =
                                exchangeRateRepository
                                    .GetByCurrencyIdAndCode(uah.Id, reSale.ClientAgreement.Agreement.Currency.Code, reSale.ChangedToInvoice ?? reSale.Created);

                            foreach (ReSaleItem reSaleItem in reSale.ReSaleItems) {
                                reSaleItem.PricePerItem /= uahToAgreement.Amount;
                                reSaleItem.TotalPrice /= uahToAgreement.Amount;
                            }
                        }
                    }

                    (excelFilePath, pdfFilePath) =
                        _xlsFactoryManager
                            .NewReSaleXlsManager()
                            .ExportReSalePaymentDocumentToXlsx(
                                message.FolderPath,
                                reSale
                            );
                } else {
                    UpdatedReSaleModel reSale = reSaleRepository.GetUpdatedByNetId(message.NetId);

                    GetReSaleInfoForExportDocument(reSale.ReSale, vatRateRepository, paymentRegisterRepository);

                    reSale = GetReSaleModelForUpdate(reSale, _reSaleRepositoriesFactory.NewReSaleItemRepository(connection));

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

                    (excelFilePath, pdfFilePath) =
                        _xlsFactoryManager
                            .NewReSaleXlsManager()
                            .ExportReSaleInvoicePaymentDocumentToXlsx(
                                message.FolderPath,
                                reSale
                            );
                }
            } else if (message.Type.Equals(ReSaleDownloadDocumentType.SalesInvoice)) {
                UpdatedReSaleModel reSale = reSaleRepository.GetUpdatedByNetId(message.NetId);

                GetReSaleInfoForExportDocument(reSale.ReSale, vatRateRepository, paymentRegisterRepository);

                reSale = GetReSaleModelForUpdate(reSale, _reSaleRepositoriesFactory.NewReSaleItemRepository(connection));

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

                (excelFilePath, pdfFilePath) =
                    _xlsFactoryManager
                        .NewReSaleXlsManager()
                        .ExportReSaleSalesInvoiceDocumentToXlsx(
                            message.FolderPath,
                            reSale,
                            _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
                        );
            }

            Sender.Tell(new Tuple<string, string>(excelFilePath, pdfFilePath));
        } catch (Exception) {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }

    private void ProcessChangeIsCompleted(ChangeIsCompletedMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            IReSaleRepository reSaleRepository = _reSaleRepositoriesFactory.NewReSaleRepository(connection);

            ReSale reSale = reSaleRepository.GetByNetIdWithItemsInfo(message.NetId);

            reSaleRepository.ChangeIsCompleted(reSale.NetUid, true);

            SetDebtToClient(
                reSale,
                _clientRepositoriesFactory.NewClientAgreementRepository(connection),
                _clientRepositoriesFactory.NewClientInDebtRepository(connection),
                _saleRepositoriesFactory.NewBaseSalePaymentStatusRepository(connection),
                _saleRepositoriesFactory.NewDebtRepository(connection),
                _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
                _currencyRepositoriesFactory.NewCurrencyRepository(connection),
                _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection),
                _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection));

            UpdatedReSaleModel toReturn = reSaleRepository.GetUpdatedByNetId(message.NetId);

            Sender.Tell(GetReSaleModelForUpdate(toReturn, _reSaleRepositoriesFactory.NewReSaleItemRepository(connection)));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdateReSale(UpdateReSaleMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IReSaleRepository reSaleRepository = _reSaleRepositoriesFactory.NewReSaleRepository(connection);
            IReSaleAvailabilityRepository reSaleAvailabilityRepository = _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection);

            bool isNotInvoice = !message.UpdatedReSale.ReSale.ChangedToInvoice.HasValue;

            if (isNotInvoice && HasUnavailableProducts(message.UpdatedReSale, reSaleAvailabilityRepository, out UnavailableProductsForReSaleModel unavailableProductNames)) {
                Sender.Tell(unavailableProductNames);
                return;
            }

            ReSale reSale = reSaleRepository.GetByNetIdWithItemsInfo(message.UpdatedReSale.ReSale.NetUid);
            reSale.OrganizationId = message.UpdatedReSale.ReSale.Organization.Id;
            if (message.UpdatedReSale.ReSale.ClientAgreement != null)
                reSale.ClientAgreementId = message.UpdatedReSale.ReSale.ClientAgreement.Id;
            else
                reSale.ClientAgreementId = null;
            reSale.Comment = message.UpdatedReSale.ReSale.Comment;

            reSaleRepository.Update(reSale);

            foreach (UpdatedReSaleItemModel item in message.UpdatedReSale.ReSaleItemModels) {
                double totalExistQty = item.ReSaleItems.Sum(x => x.Qty);

                double diffQty = item.QtyToReSale - totalExistQty;

                foreach (ReSaleItem reSaleItem in item.ReSaleItems) {
                    reSaleItem.Qty += diffQty;
                    reSaleItem.PricePerItem = item.SalePrice;
                }
            }

            _reSaleRepositoriesFactory
                .NewReSaleItemRepository(connection)
                .UpdateMany(message.UpdatedReSale.ReSaleItemModels.SelectMany(x => x.ReSaleItems));

            UpdatedReSaleModel toReturn = _reSaleRepositoriesFactory.NewReSaleRepository(connection).GetUpdatedByNetId(message.UpdatedReSale.ReSale.NetUid);

            Sender.Tell(GetReSaleModelForUpdate(toReturn, _reSaleRepositoriesFactory.NewReSaleItemRepository(connection)));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessChangeToInvoiceReSale(ChangeToInvoiceReSaleMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IReSaleRepository reSaleRepository = _reSaleRepositoriesFactory.NewReSaleRepository(connection);
            IReSaleAvailabilityRepository reSaleAvailabilityRepository = _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection);
            IReSaleItemRepository reSaleItemRepository = _reSaleRepositoriesFactory.NewReSaleItemRepository(connection);

            // if (HasUnavailableProducts(message.UpdatedReSale, reSaleAvailabilityRepository, out UnavailableProductsForReSaleModel unavailableProductNames)) {
            //     Sender.Tell(unavailableProductNames);
            //     return;
            // }

            ReSale reSale = reSaleRepository.GetByNetIdWithItemsInfo(message.NetId);

            if (reSale == null)
                throw new Exception("ReSale is not empty");

            if (!reSale.ChangedToInvoiceById.HasValue) {
                reSale.ChangedToInvoiceById = _userRepositoriesFactory.NewUserRepository(connection).GetIdByNetId(message.UserNetId);
                reSale.ChangedToInvoice = DateTime.UtcNow;

                reSale.BaseLifeCycleStatus.SaleLifeCycleType = SaleLifeCycleType.Packaging;

                _saleRepositoriesFactory
                    .NewBaseLifeCycleStatusRepository(connection)
                    .Update(reSale.BaseLifeCycleStatus);

                reSale.TotalPaymentAmount = reSale.ReSaleItems.Sum(item => Convert.ToDecimal(item.Qty) * item.PricePerItem);

                reSaleRepository.UpdateChangeToInvoice(reSale);
            }

            IConsignmentItemMovementRepository consignmentItemMovementRepository = _consignmentRepositoriesFactory.NewConsignmentItemMovementRepository(connection);

            foreach (ReSaleItem reSaleItem in reSale.ReSaleItems) {
                IEnumerable<ReSaleAvailability> reSaleAvailabilities =
                    reSaleAvailabilityRepository.GetExistByProductId(reSaleItem.ProductId);

                double storeQty = reSaleItem.Qty;

                foreach (ReSaleAvailability reSaleAvailability in reSaleAvailabilities) {
                    if (storeQty.Equals(0)) break;

                    ReSaleItem newReSaleItem = new() {
                        ExchangeRate = reSaleAvailability.ExchangeRate,
                        ExtraCharge = reSaleItem.PricePerItem.Equals(0) || reSaleAvailability.PricePerItem.Equals(0)
                            ? 0
                            : reSaleItem.PricePerItem / reSaleAvailability.PricePerItem,
                        ProductId = reSaleAvailability.ConsignmentItem.ProductId,
                        ReSaleId = reSale.Id,
                        PricePerItem = reSaleItem.PricePerItem,
                        ReSaleAvailabilityId = reSaleAvailability.Id
                    };

                    if (reSaleAvailability.RemainingQty > storeQty) {
                        newReSaleItem.Qty = storeQty;
                        reSaleAvailability.RemainingQty -= storeQty;
                        reSaleAvailability.InvoiceQty += storeQty;
                        reSaleAvailabilityRepository.UpdateRemainingQty(reSaleAvailability);
                        storeQty = 0;
                    } else {
                        newReSaleItem.Qty = reSaleAvailability.RemainingQty;
                        storeQty -= reSaleAvailability.RemainingQty;
                        reSaleAvailability.InvoiceQty += reSaleAvailability.RemainingQty;
                        reSaleAvailability.RemainingQty = 0;
                        reSaleAvailabilityRepository.UpdateRemainingQty(reSaleAvailability);
                    }

                    long newReSaleItemId = reSaleItemRepository.Add(newReSaleItem);

                    consignmentItemMovementRepository.Add(
                        new ConsignmentItemMovement {
                            IsIncomeMovement = false,
                            Qty = newReSaleItem.Qty,
                            RemainingQty = reSaleAvailability.RemainingQty,
                            MovementType = ConsignmentItemMovementType.Sale,
                            ConsignmentItemId = reSaleAvailability.ConsignmentItemId,
                            ReSaleItemId = newReSaleItemId
                        });
                }

                reSaleItemRepository.Delete(reSaleItem.Id);
            }

            UpdatedReSaleModel toReturn = _reSaleRepositoriesFactory.NewReSaleRepository(connection).GetUpdatedByNetId(message.NetId);

            Sender.Tell(GetReSaleModelForUpdate(toReturn, _reSaleRepositoriesFactory.NewReSaleItemRepository(connection)));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessRemoveReSale(RemoveReSaleMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IReSaleRepository reSaleRepository = _reSaleRepositoriesFactory.NewReSaleRepository(connection);

            ReSale reSale = reSaleRepository.GetByNetIdWithItemsInfo(message.NetId);

            if (!reSale.ChangedToInvoiceById.HasValue) {
                _reSaleRepositoriesFactory
                    .NewReSaleItemRepository(connection)
                    .DeleteByReSale(reSale.Id);

                reSaleRepository.Remove(reSale.Id);
            }

            Sender.Tell(reSale);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private static UpdatedReSaleModel GetReSaleModelForUpdate(UpdatedReSaleModel toReturn, IReSaleItemRepository reSaleItemRepository) {
        decimal vatPercent = Convert.ToDecimal(toReturn.ReSale.Organization.VatRate?.Value ?? 0);

        if (toReturn.ReSale.ChangedToInvoice.HasValue)
            foreach (ReSaleItem reSaleItem in toReturn.ReSale.ReSaleItems)
                if (!toReturn.ReSaleItemModels.Any(x => x.ConsignmentItem.Id.Equals(reSaleItem.ReSaleAvailability.ConsignmentItem.Id))) {
                    UpdatedReSaleItemModel item = new() {
                        ReSaleItems = new List<ReSaleItem> { reSaleItem },
                        Price = reSaleItem.ReSaleAvailability.PricePerItem,
                        Qty = reSaleItem.RemainingQty + reSaleItem.Qty - reSaleItemRepository.GetSoldInReSaleByProductIdAndReSaleId(reSaleItem.ProductId, reSaleItem.ReSaleId),
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

                    existItem.Qty += reSaleItem.Qty - reSaleItemRepository.GetSoldInReSaleByProductIdAndReSaleId(reSaleItem.ProductId, reSaleItem.ReSaleId);

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
        else
            foreach (ReSaleItem reSaleItem in toReturn.ReSale.ReSaleItems) {
                UpdatedReSaleItemModel item = new() {
                    ReSaleItems = new List<ReSaleItem> { reSaleItem },
                    Price = reSaleItem.ReSaleAvailability.PricePerItem,
                    Qty = reSaleItem.RemainingQty - reSaleItemRepository.GetSoldInReSaleByProductIdAndReSaleId(reSaleItem.ProductId, reSaleItem.ReSaleId),
                    QtyToReSale = reSaleItem.Qty,
                    SalePrice = reSaleItem.PricePerItem,
                    ConsignmentItem = reSaleItem.ReSaleAvailability.ConsignmentItem,
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
            }

        if (!toReturn.ReSale.ChangedToInvoice.HasValue)
            toReturn.ReSale.TotalPaymentAmount = toReturn.TotalAmount;

        toReturn.ReSale.DifferencePaymentAndInvoiceAmount = toReturn.TotalAmount - toReturn.ReSale.TotalPaymentAmount;

        return toReturn;
    }

    private static void SetDebtToClient(
        ReSale reSale,
        IClientAgreementRepository clientAgreementRepository,
        IClientInDebtRepository clientInDebtRepository,
        IBaseSalePaymentStatusRepository baseSalePaymentStatusRepository,
        IDebtRepository debtRepository,
        IExchangeRateRepository exchangeRateRepository,
        ICurrencyRepository currencyRepository,
        IClientBalanceMovementRepository clientBalanceMovementRepository,
        ICrossExchangeRateRepository crossExchangeRateRepository) {
        ClientAgreement clientAgreement = clientAgreementRepository.GetById(reSale.ClientAgreement.Id);
        ClientInDebt clientInDebtFromDb = clientInDebtRepository.GetByReSaleAndClientAgreementIds(reSale.Id, clientAgreement.Id);

        if (!reSale.ChangedToInvoice.HasValue) return;

        Currency uah = currencyRepository.GetUAHCurrencyIfExists();
        Currency eur = currencyRepository.GetEURCurrencyIfExists();

        ExchangeRate govExchangeRateUahToEur =
            exchangeRateRepository
                .GetByCurrencyIdAndCode(uah.Id, eur.Code, reSale.ChangedToInvoice.Value);

        decimal totalEuro =
            decimal.Round(reSale.ReSaleItems.Sum(reSaleItem => reSaleItem.PricePerItem * Convert.ToDecimal(reSaleItem.Qty)) / govExchangeRateUahToEur.Amount,
                14,
                MidpointRounding.AwayFromZero);

        if (clientAgreement.CurrentAmount >= totalEuro) {
            clientAgreement.CurrentAmount = Math.Round(clientAgreement.CurrentAmount - totalEuro, 14);

            clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

            clientBalanceMovementRepository
                .AddOutMovement(
                    new ClientBalanceMovement {
                        ClientAgreementId = clientAgreement.Id,
                        Amount = totalEuro,
                        ExchangeRateAmount = govExchangeRateUahToEur.Amount
                    }
                );

            baseSalePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Paid, reSale.BaseSalePaymentStatusId);
        } else {
            if (clientAgreement.CurrentAmount > decimal.Zero) {
                totalEuro = decimal.Round(totalEuro - clientAgreement.CurrentAmount, 14, MidpointRounding.AwayFromZero);

                clientBalanceMovementRepository
                    .AddOutMovement(
                        new ClientBalanceMovement {
                            ClientAgreementId = clientAgreement.Id,
                            Amount = clientAgreement.CurrentAmount,
                            ExchangeRateAmount = govExchangeRateUahToEur.Amount
                        }
                    );

                clientAgreement.CurrentAmount = decimal.Zero;

                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                baseSalePaymentStatusRepository
                    .SetSalePaymentStatusTypeById(SalePaymentStatusType.PartialPaid, reSale.BaseSalePaymentStatusId);
            }

            decimal total;

            if (!clientAgreement.Agreement.Currency.Code.Equals("EUR")) {
                decimal exchangeRateAmount = govExchangeRateUahToEur.Amount;

                ExchangeRate exchangeRate =
                    exchangeRateRepository
                        .GetByCurrencyIdAndCode(clientAgreement.Agreement.Currency.Id, eur.Code, reSale.ChangedToInvoice.Value);

                if (exchangeRate != null) {
                    exchangeRateAmount = exchangeRate.Amount;
                } else {
                    CrossExchangeRate crossExchangeRate =
                        crossExchangeRateRepository
                            .GetByCurrenciesIds(eur.Id, clientAgreement.Agreement.Currency.Id, reSale.ChangedToInvoice.Value);

                    if (crossExchangeRate != null)
                        exchangeRateAmount = crossExchangeRate.Amount;
                }


                total = totalEuro * exchangeRateAmount;
            } else {
                total = totalEuro;
            }

            if (clientInDebtFromDb != null) {
                clientInDebtFromDb.Debt.Total = total;

                debtRepository.Update(clientInDebtFromDb.Debt);
            } else {
                Debt debt = new() {
                    Days = 0,
                    Total = total
                };

                ClientInDebt clientInDebt = new() {
                    AgreementId = clientAgreement.AgreementId,
                    ClientId = clientAgreement.ClientId,
                    DebtId = debtRepository.Add(debt),
                    ReSaleId = reSale.Id
                };

                clientInDebtRepository.Add(clientInDebt);
            }
        }
    }

    private void GetReSaleInfoForExportDocument(
        ReSale reSale,
        IVatRateRepository vatRateRepository,
        IPaymentRegisterRepository paymentRegisterRepository) {
        if (reSale.Organization.VatRateId.HasValue)
            reSale.Organization.VatRate =
                vatRateRepository
                    .GetById(reSale.Organization.VatRateId.Value);

        if (reSale.ClientAgreementId.HasValue && reSale.ClientAgreement.Agreement.OrganizationId.HasValue)
            reSale.Organization.MainPaymentRegister =
                paymentRegisterRepository.GetMainPaymentRegisterByOrganization(reSale.ClientAgreement.Agreement.OrganizationId.Value);

        if (reSale.ClientAgreementId.HasValue &&
            reSale.ClientAgreement.Agreement.CurrencyId.HasValue &&
            reSale.ClientAgreement.Agreement.OrganizationId.HasValue) {
            PaymentRegister paymentRegister =
                paymentRegisterRepository
                    .GetActiveBankAccountByCurrencyAndOrganizationIds(
                        reSale.ClientAgreement.Agreement.CurrencyId.Value,
                        reSale.ClientAgreement.Agreement.OrganizationId.Value
                    );

            if (paymentRegister != null) reSale.Organization.PaymentRegisters.Add(paymentRegister);
        }
    }
}