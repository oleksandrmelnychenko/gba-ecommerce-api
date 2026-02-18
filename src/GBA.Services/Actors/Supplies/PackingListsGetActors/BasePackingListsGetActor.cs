using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Messages.Supplies.PackingLists;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Services.Actors.Supplies.PackingListsGetActors;

public sealed class BasePackingListsGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IOrganizationRepositoriesFactory _organizationRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public BasePackingListsGetActor(
        IDbConnectionFactory connectionFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IXlsFactoryManager xlsFactoryManager,
        IOrganizationRepositoriesFactory organizationRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _xlsFactoryManager = xlsFactoryManager;
        _organizationRepositoriesFactory = organizationRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;

        Receive<GetPackingListSpecificationDocumentUrlMessage>(ProcessGetPackingListSpecificationDocumentUrlMessage);

        Receive<GetProductsSpecificationByPackingListNetIdMessage>(ProcessGetProductsSpecificationByPackingListNetIdMessage);

        Receive<GetAllUnshippedPackingListsMessage>(ProcessGetAllUnshippedPackingListsMessage);
    }

    private void ProcessGetPackingListSpecificationDocumentUrlMessage(GetPackingListSpecificationDocumentUrlMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPackingListRepository packingListRepository = _supplyRepositoriesFactory.NewPackingListRepository(connection);

        PackingList packingList = packingListRepository.GetByNetIdWithOrderInfo(message.NetId);

        if (packingList.SupplyInvoice.SupplyOrder.Organization.Culture == "uk")
            Sender.Tell(
                _xlsFactoryManager
                    .NewInvoiceXlsManager()
                    .ExportUkInvoiceProductSpecification(
                        NoltFolderManager.GetSpecificationsFolderPath(),
                        _supplyRepositoriesFactory
                            .NewSupplyInvoiceRepository(connection)
                            .GetByNetIdWithItemsAndSpecificationForExport(packingList.SupplyInvoice.NetUid)
                    )
            );
        else
            Sender.Tell(
                _xlsFactoryManager
                    .NewInvoiceXlsManager()
                    .ExportSpecificationToXlsx(
                        NoltFolderManager.GetSpecificationsFolderPath(),
                        packingListRepository.GetByNetIdForSpecification(message.NetId),
                        packingListRepository.GetGroupedSpecificationForDocumentByPackingListNetId(message.NetId)
                    )
            );
    }

    private void ProcessGetProductsSpecificationByPackingListNetIdMessage(GetProductsSpecificationByPackingListNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ICurrencyRepository currencyRepository =
            _currencyRepositoriesFactory.NewCurrencyRepository(connection);

        Currency uah = currencyRepository.GetUAHCurrencyIfExists();
        Currency eur = currencyRepository.GetEURCurrencyIfExists();

        SupplyInvoice invoice =
            _supplyRepositoriesFactory
                .NewSupplyInvoiceRepository(connection)
                .GetSupplyInvoiceByPackingListNetId(message.NetId);

        GovExchangeRate govExhangeRate =
            _exchangeRateRepositoriesFactory
                .NewGovExchangeRateRepository(connection)
                .GetByCurrencyIdAndCode(uah.Id, eur.Code,
                    invoice.DateCustomDeclaration.HasValue ? invoice.DateCustomDeclaration.Value : invoice.Created);

        PackingList toReturn = _supplyRepositoriesFactory
            .NewPackingListRepository(connection)
            .GetByNetIdWithProductSpecification(message.NetId, govExhangeRate.Amount);

        List<PackingListPackageOrderItemSupplyService> services =
            toReturn.PackingListPackageOrderItems
                .SelectMany(x => x.PackingListPackageOrderItemSupplyServices)
                .ToList();

        IEnumerable<IGrouping<long, PackingListPackageOrderItemSupplyService>> groupedServices =
            services.Where(x => x.MergedServiceId.HasValue).GroupBy(x => x.MergedServiceId.Value);

        foreach (IGrouping<long, PackingListPackageOrderItemSupplyService> groupedService in groupedServices) {
            decimal totalGeneralPriceForServiceEur = groupedService.Sum(x => x.TotalGeneralPriceForServiceEur);
            decimal totalGeneralPriceForServiceUah = groupedService.Sum(x => x.TotalGeneralPriceForServiceUah);
            decimal totalNetPriceForServiceEur = groupedService.Sum(x => x.TotalNetPriceForServiceEur);
            decimal totalNetPriceForServiceUah = groupedService.Sum(x => x.TotalNetPriceForServiceUah);
            decimal totalManagementPriceForServiceEur = groupedService.Sum(x => x.TotalManagementPriceForServiceEur);
            decimal totalManagementPriceForServiceUah = groupedService.Sum(x => x.TotalManagementPriceForServiceUah);

            foreach (PackingListPackageOrderItemSupplyService service in groupedService) {
                service.TotalGeneralPriceForServiceEur = totalGeneralPriceForServiceEur;
                service.TotalGeneralPriceForServiceUah = totalGeneralPriceForServiceUah;
                service.TotalNetPriceForServiceEur = totalNetPriceForServiceEur;
                service.TotalNetPriceForServiceUah = totalNetPriceForServiceUah;
                service.TotalManagementPriceForServiceEur = totalManagementPriceForServiceEur;
                service.TotalManagementPriceForServiceUah = totalManagementPriceForServiceUah;
            }
        }

        Sender.Tell(toReturn);
    }

    private void ProcessGetAllUnshippedPackingListsMessage(GetAllUnshippedPackingListsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        string organizationCulture =
            _organizationRepositoriesFactory
                .NewOrganizationRepository(connection)
                .GetCultureByNetId(message.OrganizationNetId);

        Sender.Tell(
            _supplyRepositoriesFactory.NewPackingListRepository(connection)
                .GetAllUnshipped(message.SupplyTransportationType, organizationCulture));
    }
}