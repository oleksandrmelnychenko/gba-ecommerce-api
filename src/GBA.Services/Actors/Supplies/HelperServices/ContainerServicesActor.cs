using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Messages.Auditing;
using GBA.Domain.Messages.Supplies.HelperServices.Containers;
using GBA.Domain.Messages.Supplies.Invoices;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Supplies.HelperServices;

public sealed class ContainerServicesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;

    public ContainerServicesActor(
        IDbConnectionFactory connectionFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;

        Receive<GetAllContainersRangedMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.From == null) message.From = DateTime.Now.Date;

            message.To = message.To?.Date.AddHours(23).AddMinutes(59).AddSeconds(59) ?? DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            Sender.Tell(_supplyRepositoriesFactory
                .NewContainerServiceRepository(connection)
                .GetAllRanged(message.From.Value, message.To.Value)
            );
        });

        Receive<GetAllAvailableContainerServicesMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_supplyRepositoriesFactory.NewContainerServiceRepository(connection).GetAllAvailable());
        });

        Receive<UpdateContainerServiceDocumentsMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.ContainerService.BillOfLadingDocument != null && !message.ContainerService.BillOfLadingDocument.IsNew())
                _supplyRepositoriesFactory
                    .NewBillOfLadingDocumentRepository(connection)
                    .Update(message.ContainerService.BillOfLadingDocument);
            if (message.ContainerService.InvoiceDocuments.Any(d => !d.IsNew()))
                _supplyRepositoriesFactory
                    .NewInvoiceDocumentRepository(connection)
                    .Update(message.ContainerService.InvoiceDocuments.Where(d => !d.IsNew()));
            if (message.ContainerService.ActProvidingServiceDocument != null && !message.ContainerService.ActProvidingServiceDocument.Id.Equals(0))
                _supplyRepositoriesFactory
                    .NewActProvidingServiceDocumentRepository(connection)
                    .Update(message.ContainerService.ActProvidingServiceDocument);
            if (message.ContainerService.SupplyServiceAccountDocument != null && !message.ContainerService.SupplyServiceAccountDocument.Id.Equals(0))
                _supplyRepositoriesFactory
                    .NewSupplyServiceAccountDocumentRepository(connection)
                    .Update(message.ContainerService.SupplyServiceAccountDocument);

            Sender.Tell(_supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetByNetId(message.NetId));
        });

        Receive<UpdateDeliveryTermMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IContainerServiceRepository containerServiceRepository = _supplyRepositoriesFactory.NewContainerServiceRepository(connection);

            if (message.ContainerService != null) {
                string oldTermDeliveryInDays = containerServiceRepository.GetTermDeliveryInDaysById(message.ContainerService.Id);

                if (oldTermDeliveryInDays != null && !oldTermDeliveryInDays.Equals(message.TermDeliveryInDays)) {
                    containerServiceRepository.UpdateDeliveryTerms(message.ContainerService.Id, message.TermDeliveryInDays);

                    ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(new RetrieveAndStoreAuditDataMessage(
                        message.UpdatedByNetId,
                        message.NetId,
                        "SupplyOrder.ContainerService",
                        new ContainerService { TermDeliveryInDays = message.TermDeliveryInDays },
                        new ContainerService { TermDeliveryInDays = oldTermDeliveryInDays }
                    ));
                }

                Sender.Tell(_supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetByNetId(message.NetId));
            } else {
                Sender.Tell(null);
            }
        });

        Receive<UpdateContainerExtraChargeMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IContainerServiceRepository containerServiceRepository = _supplyRepositoriesFactory.NewContainerServiceRepository(connection);

            ContainerService containerService = containerServiceRepository.GetBySupplyOrderContainerServiceNetIdWithoutIncludes(message.NetId);
            IPackingListRepository packingListRepository = _supplyRepositoriesFactory.NewPackingListRepository(connection);

            if (containerService == null || containerService.IsNew()) {
                Sender.Tell(new Exception());
                return;
            }

            List<PackingList> assignedPackingLists = packingListRepository.GetAllAssignedToContainerByContainerNetId(message.NetId);

            if (assignedPackingLists.Any()) {
                switch (message.SupplyExtraChargeType) {
                    case SupplyExtraChargeType.Price:
                        decimal totalPrice = assignedPackingLists.Sum(p => p.TotalPrice);

                        assignedPackingLists.ForEach(list => {
                            decimal percent = list.TotalPrice * 100 / totalPrice;

                            list.ExtraCharge = decimal.Round(percent * containerService.NetPrice / 100, 2, MidpointRounding.AwayFromZero);
                            list.AccountingExtraCharge = decimal.Round(percent * containerService.AccountingNetPrice / 100, 2, MidpointRounding.AwayFromZero);
                        });

                        break;
                    case SupplyExtraChargeType.Weight:
                        double totalWeight = assignedPackingLists.Sum(p => p.TotalNetWeight);

                        assignedPackingLists.ForEach(list => {
                            double percent = list.TotalNetWeight * 100 / totalWeight;

                            list.ExtraCharge = decimal.Round(Convert.ToDecimal(percent) * containerService.NetPrice / 100, 2, MidpointRounding.AwayFromZero);
                            list.AccountingExtraCharge = decimal.Round(Convert.ToDecimal(percent) * containerService.AccountingNetPrice / 100, 2,
                                MidpointRounding.AwayFromZero);
                        });

                        break;
                    case SupplyExtraChargeType.Volume:
                        double totalCBM = assignedPackingLists.Sum(p => p.TotalCBM);

                        assignedPackingLists.ForEach(list => {
                            double percent =
                                totalCBM.Equals(0)
                                    ? 100d / assignedPackingLists.Count
                                    : list.TotalCBM * 100 / totalCBM;

                            list.ExtraCharge = decimal.Round(Convert.ToDecimal(percent) * containerService.NetPrice / 100, 2, MidpointRounding.AwayFromZero);
                            list.AccountingExtraCharge = decimal.Round(Convert.ToDecimal(percent) * containerService.AccountingNetPrice / 100, 2,
                                MidpointRounding.AwayFromZero);
                        });

                        break;
                }

                packingListRepository.UpdateExtraCharge(assignedPackingLists);
            }

            containerServiceRepository.SetIsExtraChargeCalculatedByNetId(message.NetId, message.SupplyExtraChargeType);

            Sender.Tell(
                _supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetByNetId(
                    _supplyRepositoriesFactory.NewContainerServiceRepository(connection).GetSupplyOrderNetIdBySupplyOrderContainerServiceNetId(message.NetId)
                )
            );

            ActorReferenceManager.Instance.Get(SupplyActorNames.SUPPLY_INVOICE_ACTOR).Tell(new UpdateSupplyInvoiceItemGrossPriceMessage(
                _supplyRepositoriesFactory
                    .NewSupplyInvoiceRepository(connection)
                    .GetIdByContainerServiceId(containerService.Id),
                message.UserNetId
            ));
        });

        Receive<GetWithSupplyInvoicesByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_supplyRepositoriesFactory
                .NewContainerServiceRepository(connection)
                .GetByNetId(message.NetId)
            );
        });

        Receive<UnassigningContainerServiceBeforeCalculatedExtraChargeMessage>(ProcessUnassigningContainerBeforeCalculatedExtraChargeService);

        Receive<RemoveContainerServiceBeforeCalculatedExtraChargeMessage>(ProcessRemoveContainerServiceBeforeCalculatedExtraCharge);
    }

    private void ProcessUnassigningContainerBeforeCalculatedExtraChargeService(UnassigningContainerServiceBeforeCalculatedExtraChargeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            if (_supplyRepositoriesFactory.NewContainerServiceRepository(connection).IsContainerInOrder(message.SupplyOrderNetId, message.ContainerServiceNetId)) {
                long supplyOrderId = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetIdByNetId(message.SupplyOrderNetId);
                long containerServiceId = _supplyRepositoriesFactory.NewContainerServiceRepository(connection).GetIdByNetId(message.ContainerServiceNetId);

                _supplyRepositoriesFactory
                    .NewSupplyOrderContainerServiceRepository(connection)
                    .RemoveAllBySupplyOrderAndContainerServiceId(supplyOrderId, containerServiceId);

                _supplyRepositoriesFactory
                    .NewPackingListRepository(connection)
                    .UnassigningAllByContainerAndSupplyOrderId(supplyOrderId, containerServiceId);
            }

            Sender.Tell(_supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetByNetId(message.SupplyOrderNetId));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessRemoveContainerServiceBeforeCalculatedExtraCharge(RemoveContainerServiceBeforeCalculatedExtraChargeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            long containerServiceId = _supplyRepositoriesFactory.NewContainerServiceRepository(connection).GetIdByNetId(message.ContainerServiceNetId);

            _supplyRepositoriesFactory
                .NewSupplyOrderContainerServiceRepository(connection)
                .RemoveAllByContainerServiceId(containerServiceId);

            _supplyRepositoriesFactory
                .NewPackingListRepository(connection)
                .UnassigninAllByContainerServiceId(containerServiceId);

            _supplyRepositoriesFactory
                .NewContainerServiceRepository(connection)
                .Remove(containerServiceId);

            Sender.Tell(_supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetByNetId(message.SupplyOrderNetId));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}