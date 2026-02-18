using System;
using System.Collections.Generic;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Messages.Supplies;
using GBA.Domain.Messages.Supplies.Invoices;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.HelperServices.Contracts;

namespace GBA.Services.Actors.Supplies.SupplyInvoicesGetActors;

public sealed class BaseSupplyInvoicesGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IOrganizationRepositoriesFactory _organizationRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public BaseSupplyInvoicesGetActor(
        IDbConnectionFactory connectionFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IOrganizationRepositoriesFactory organizationRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IXlsFactoryManager xlsFactoryManager) {
        _connectionFactory = connectionFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _organizationRepositoriesFactory = organizationRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _xlsFactoryManager = xlsFactoryManager;

        Receive<GetInvoiceWithSupplyInvoiceOrderItemsByNetIdMessage>(ProcessGetInvoiceWithSupplyInvoiceOrderItemsByNetIdMessage);

        Receive<ExportPZDocumentBySupplyInvoiceNetIdMessage>(ProcessExportPZDocumentBySupplyInvoiceNetIdMessage);

        Receive<GetAllInvoicesFromApprovedSupplyOrderMessage>(ProcessGetAllInvoicesFromApprovedSupplyOrder);

        Receive<GetByServicesNetIdMessage>(ProcessGetByServicesNetId);

        Receive<GetAllSpendingOnServicesByNetIdMessage>(ProcessGetAllSpendingOnServicesByNetId);

        Receive<GetSupplyInvoiceByNetIdMessage>(ProcessGetSupplyInvoiceByNetIdMessage);

        Receive<GetAllSupplyInvoicesByContainerNetId>(ProcessGetAllSupplyInvoicesByContainerNetId);

        Receive<GetCountSupplyInvoicesByContainerNetId>(ProcessGetCountSupplyInvoicesByContainerNetId);
    }

    private void ProcessGetAllInvoicesFromApprovedSupplyOrder(GetAllInvoicesFromApprovedSupplyOrderMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            string organizationCulture =
                _organizationRepositoriesFactory
                    .NewOrganizationRepository(connection)
                    .GetCultureByNetId(message.OrganizationNetId);

            long protocolId = _supplyRepositoriesFactory
                .NewDeliveryProductProtocolRepository(connection)
                .GetIdByNetId(message.NetId);

            Sender.Tell(
                _supplyRepositoriesFactory.NewSupplyInvoiceRepository(connection)
                    .GetAllInvoicesFromApprovedSupplyOrder(message.SupplyTransportationType, organizationCulture, protocolId));
        } catch (Exception) {
            Sender.Tell(new List<SupplyInvoice>());
        }
    }

    private void ProcessGetByServicesNetId(GetByServicesNetIdMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyInvoiceRepository supplyInvoiceRepository =
                _supplyRepositoriesFactory.NewSupplyInvoiceRepository(connection);
            IBillOfLadingServiceRepository billOfLadingServiceRepository =
                _supplyRepositoriesFactory.NewBillOfLadingServiceRepository(connection);
            IMergedServiceRepository mergedServiceRepository =
                _supplyRepositoriesFactory.NewMergedServiceRepository(connection);

            BillOfLadingService billOfLadingService = billOfLadingServiceRepository
                .GetWithoutIncludesByNetId(message.ServiceNetId);

            long deliveryProductProtocolId;

            List<SupplyInvoice> supplyInvoices = new();
            if (billOfLadingService != null) {
                deliveryProductProtocolId = billOfLadingServiceRepository.GetDeliveryProductProtocolIdByNetId(billOfLadingService.NetUid);

                supplyInvoices = supplyInvoiceRepository.GetByBillOfLadingServiceId(billOfLadingService.Id, deliveryProductProtocolId);
            } else {
                MergedService mergedService = mergedServiceRepository
                    .GetWithoutIncludesByNetId(message.ServiceNetId);

                if (mergedService != null) {
                    deliveryProductProtocolId = mergedServiceRepository.GetDeliveryProductProtocolIdByNetId(mergedService.NetUid);

                    supplyInvoices = supplyInvoiceRepository.GetByMergedServiceId(mergedService.Id, deliveryProductProtocolId);
                }
            }

            Sender.Tell(supplyInvoices);
        } catch {
            Sender.Tell(new List<SupplyInvoice>());
        }
    }

    private void ProcessGetAllSpendingOnServicesByNetId(GetAllSpendingOnServicesByNetIdMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _supplyRepositoriesFactory
                    .NewSupplyInvoiceRepository(connection)
                    .GetAllSpendingOnServicesByNetId(message.NetId));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetInvoiceWithSupplyInvoiceOrderItemsByNetIdMessage(GetInvoiceWithSupplyInvoiceOrderItemsByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supplyRepositoriesFactory
                .NewSupplyInvoiceRepository(connection)
                .GetByNetIdWithAllIncludes(
                    message.NetId
                )
        );
    }

    private void ProcessExportPZDocumentBySupplyInvoiceNetIdMessage(ExportPZDocumentBySupplyInvoiceNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyInvoiceRepository supplyInvoiceRepository = _supplyRepositoriesFactory.NewSupplyInvoiceRepository(connection);

        SupplyInvoice supplyInvoice = supplyInvoiceRepository.GetByNetIdWithoutIncludes(message.NetId);

        if (supplyInvoice != null) {
            supplyInvoice =
                supplyInvoiceRepository
                    .GetByNetIdAndCultureWithAllIncludes(
                        message.NetId,
                        "pl"
                    );

            Currency pln =
                _currencyRepositoriesFactory
                    .NewCurrencyRepository(connection)
                    .GetPLNCurrencyIfExists();

            ExchangeRate exchangeRate =
                _exchangeRateRepositoriesFactory
                    .NewExchangeRateRepository(connection)
                    .GetByCurrencyIdAndCode(
                        pln.Id,
                        supplyInvoice.SupplyOrder.ClientAgreement.Agreement.Currency.Code,
                        supplyInvoice.DateFrom?.AddDays(-1) ?? supplyInvoice.Created.AddDays(-1)
                    );

            supplyInvoice.ExchangeRate = exchangeRate?.Amount ?? 1m;

            Sender.Tell(
                _xlsFactoryManager
                    .NewInvoiceXlsManager()
                    .ExportSupplyInvoicePzDocument(
                        message.PathToFolder,
                        supplyInvoice
                    )
            );
        } else {
            Sender.Tell(
                (string.Empty, string.Empty)
            );
        }
    }

    private void ProcessGetSupplyInvoiceByNetIdMessage(GetSupplyInvoiceByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory
            .NewSupplyInvoiceRepository(connection)
            .GetByNetId(message.NetId)
        );
    }

    private void ProcessGetAllSupplyInvoicesByContainerNetId(GetAllSupplyInvoicesByContainerNetId message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory
            .NewSupplyInvoiceRepository(connection)
            .GetAllByContainerNetId(message.ContainerNetId)
        );
    }

    private void ProcessGetCountSupplyInvoicesByContainerNetId(GetCountSupplyInvoicesByContainerNetId message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory
            .NewSupplyInvoiceRepository(connection)
            .GetCountByContainerNetId(message.ContainerNetId)
        );
    }
}