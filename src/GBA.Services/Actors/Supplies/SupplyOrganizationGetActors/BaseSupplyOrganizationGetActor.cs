using System;
using System.Collections.Generic;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Messages.Supplies.Organizations;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.DocumentMonths.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Services.Actors.Supplies.SupplyOrganizationGetActors;

public sealed class BaseSupplyOrganizationGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IDocumentMonthRepositoryFactory _documentMonthRepositoryFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public BaseSupplyOrganizationGetActor(
        IDbConnectionFactory connectionFactory,
        IXlsFactoryManager xlsFactoryManager,
        IDocumentMonthRepositoryFactory documentMonthRepositoryFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _xlsFactoryManager = xlsFactoryManager;
        _documentMonthRepositoryFactory = documentMonthRepositoryFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;

        Receive<GetSupplyOrganizationByNetIdMessage>(ProcessGetSupplyOrganizationByNetIdMessage);

        Receive<GetAllSupplyOrganizationsMessage>(ProcessGetAllSupplyOrganizationsMessage);

        Receive<GetAllSupplyOrganizationsDocumentMessage>(ProcessGetAllSupplyOrganizationsDocumentMessage);

        Receive<GetAllSupplyOrganizationsFromSearchMessage>(ProcessGetAllSupplyOrganizationsFromSearchMessage);

        Receive<GetAllSupplyOrganizationAgreementsBySupplyOrganizationIdMessage>(ProcessGetAllSupplyOrganizationAgreementsBySupplyOrganizationIdMessage);
    }

    private void ProcessGetAllSupplyOrganizationsDocumentMessage(GetAllSupplyOrganizationsDocumentMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();

        try {
            string xlsxFile = string.Empty;
            string pdfFile = string.Empty;
            List<SupplyOrganization> supplyOrganization = _supplyRepositoriesFactory.NewSupplyOrganizationRepository(connection)
                .GetAllFromSearch(message.Value, message.OrganizationNetId);
            (xlsxFile, pdfFile) =
                _xlsFactoryManager
                    .NewSynchronizationXlsManager()
                    .ExportUkSupplyOrganizationToXlsx(
                        message.SaleInvoicesFolderPath,
                        supplyOrganization,
                        _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk"));

            Sender.Tell((xlsxFile, pdfFile));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetAllSupplyOrganizationAgreementsBySupplyOrganizationIdMessage(GetAllSupplyOrganizationAgreementsBySupplyOrganizationIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection)
            .GetAllBySupplyOrganizationId(message.Id));
    }

    private void ProcessGetSupplyOrganizationByNetIdMessage(GetSupplyOrganizationByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        SupplyOrganization supplyOrganization = _supplyRepositoriesFactory.NewSupplyOrganizationRepository(connection).GetByNetId(message.NetId);

        if (supplyOrganization != null)
            foreach (SupplyOrganizationAgreement agreement in supplyOrganization.SupplyOrganizationAgreements)
                if (!agreement.Currency.Code.ToLower().Equals("eur")) {
                    Currency euroCurrency = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

                    supplyOrganization.TotalAgreementsCurrentAmount += agreement.CurrentAmount + agreement.AccountingCurrentAmount;

                    if (euroCurrency == null) continue;

                    if (agreement.Currency.Code.ToLower().Equals("uah") || agreement.Currency.Code.ToLower().Equals("pln")) {
                        ExchangeRate exchangeRate =
                            _exchangeRateRepositoriesFactory
                                .NewExchangeRateRepository(connection)
                                .GetByCurrencyIdAndCode(
                                    agreement.Currency.Id,
                                    euroCurrency.Code
                                );

                        decimal exchangeRateAmount = exchangeRate?.Amount ?? 1;

                        agreement.CurrentEuroAmount = Math.Round(agreement.CurrentAmount / exchangeRateAmount, 2);

                        supplyOrganization.TotalAgreementsCurrentEuroAmount +=
                            agreement.CurrentEuroAmount + Math.Round(agreement.AccountingCurrentAmount / exchangeRateAmount, 2);
                    } else {
                        ICrossExchangeRateRepository crossExchangeRateRepository = _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection);

                        CrossExchangeRate crossExchangeRate = crossExchangeRateRepository.GetByCurrenciesIds(agreement.Currency.Id, euroCurrency.Id);

                        if (crossExchangeRate != null) {
                            decimal exchangeRateAmount = crossExchangeRate.Amount;

                            agreement.CurrentEuroAmount = Math.Round(agreement.CurrentAmount / exchangeRateAmount, 2);

                            supplyOrganization.TotalAgreementsCurrentEuroAmount +=
                                agreement.CurrentEuroAmount + Math.Round(agreement.AccountingCurrentAmount / exchangeRateAmount, 2);
                        } else {
                            crossExchangeRate = crossExchangeRateRepository.GetByCurrenciesIds(euroCurrency.Id, agreement.Currency.Id);

                            decimal exchangeRateAmount = crossExchangeRate?.Amount ?? 1;

                            agreement.CurrentEuroAmount = Math.Round(agreement.CurrentAmount / exchangeRateAmount, 2);

                            supplyOrganization.TotalAgreementsCurrentEuroAmount +=
                                agreement.CurrentEuroAmount + Math.Round(agreement.AccountingCurrentAmount / exchangeRateAmount, 2);
                        }
                    }
                } else {
                    supplyOrganization.TotalAgreementsCurrentAmount += agreement.CurrentAmount + agreement.AccountingCurrentAmount;

                    supplyOrganization.TotalAgreementsCurrentEuroAmount = supplyOrganization.TotalAgreementsCurrentAmount;

                    agreement.CurrentEuroAmount = agreement.CurrentAmount;
                }

        Sender.Tell(supplyOrganization);
    }

    private void ProcessGetAllSupplyOrganizationsMessage(GetAllSupplyOrganizationsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory.NewSupplyOrganizationRepository(connection)
            .GetAll(message.OrganizationNetId));
    }

    private void ProcessGetAllSupplyOrganizationsFromSearchMessage(GetAllSupplyOrganizationsFromSearchMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory.NewSupplyOrganizationRepository(connection)
            .GetAllFromSearch(message.Value, message.OrganizationNetId));
    }
}