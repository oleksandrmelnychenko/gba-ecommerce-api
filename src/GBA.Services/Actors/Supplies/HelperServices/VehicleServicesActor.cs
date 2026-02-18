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
using GBA.Domain.Messages.Supplies.HelperServices.Vehicles;
using GBA.Domain.Messages.Supplies.Invoices;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Supplies.HelperServices;

public sealed class VehicleServicesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;

    public VehicleServicesActor(
        IDbConnectionFactory connectionFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;

        Receive<GetWithSupplyInvoicesByNetIdMessage>(ProcessGetWithSupplyInvoicesByNetIdMessage);

        Receive<UpdateVehicleExtraChargeMessage>(ProcessUpdateVehicleExtraChargeMessage);

        Receive<GetAllVehiclesRangedMessage>(ProcessGetAllVehiclesRangedMessage);

        Receive<GetAllAvailableVehicleServicesMessage>(ProcessGetAllAvailableVehicleServicesMessage);

        Receive<UpdateVehicleServiceDocumentsMessage>(ProcessUpdateVehicleServiceDocumentsMessage);

        Receive<UpdateDeliveryTermMessage>(ProcessUpdateDeliveryTermMessage);

        Receive<UnassigningVehicleServiceBeforeCalculatedExtraChargeMessage>(ProccessUnassigningVehicleBeforeCalculatedExtraChargeService);

        Receive<RemoveVehicleServiceBeforeCalculatedExtraChargeMessage>(ProccessRemoveVehicleServiceBeforeCalculatedExtraCharge);
    }

    private void ProcessGetWithSupplyInvoicesByNetIdMessage(GetWithSupplyInvoicesByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory
            .NewVehicleServiceRepository(connection)
            .GetByNetId(message.NetId)
        );
    }

    private void ProcessUpdateVehicleExtraChargeMessage(UpdateVehicleExtraChargeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IVehicleServiceRepository vehicleServiceRepository = _supplyRepositoriesFactory.NewVehicleServiceRepository(connection);

        VehicleService vehicleService = vehicleServiceRepository.GetBySupplyOrderVehiclesServiceNetIdWithoutIncludes(message.NetId);
        IPackingListRepository packingListRepository = _supplyRepositoriesFactory.NewPackingListRepository(connection);


        if (vehicleService == null || vehicleService.IsNew()) {
            Sender.Tell(new Exception());
            return;
        }

        List<PackingList> assignedPackingLists = packingListRepository.GetAllAssignedToVehicleByVehicleNetId(message.NetId);

        if (assignedPackingLists.Any()) {
            switch (message.SupplyExtraChargeType) {
                case SupplyExtraChargeType.Price:
                    decimal totalPrice = assignedPackingLists.Sum(p => p.TotalPrice);

                    assignedPackingLists.ForEach(list => {
                        decimal percent = list.TotalPrice * 100 / totalPrice;

                        list.ExtraCharge = decimal.Round(percent * vehicleService.NetPrice / 100, 2, MidpointRounding.AwayFromZero);
                        list.AccountingExtraCharge = decimal.Round(percent * vehicleService.AccountingNetPrice / 100, 2, MidpointRounding.AwayFromZero);
                    });

                    break;
                case SupplyExtraChargeType.Weight:
                    double totalWeight = assignedPackingLists.Sum(p => p.TotalNetWeight);

                    assignedPackingLists.ForEach(list => {
                        double percent = list.TotalNetWeight * 100 / totalWeight;

                        list.ExtraCharge = decimal.Round(Convert.ToDecimal(percent) * vehicleService.NetPrice / 100, 2, MidpointRounding.AwayFromZero);
                        list.AccountingExtraCharge = decimal.Round(Convert.ToDecimal(percent) * vehicleService.AccountingNetPrice / 100, 2,
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

                        list.ExtraCharge = decimal.Round(Convert.ToDecimal(percent) * vehicleService.NetPrice / 100, 2, MidpointRounding.AwayFromZero);
                        list.AccountingExtraCharge = decimal.Round(Convert.ToDecimal(percent) * vehicleService.AccountingNetPrice / 100, 2,
                            MidpointRounding.AwayFromZero);
                    });

                    break;
            }

            packingListRepository.UpdateExtraCharge(assignedPackingLists);
        }

        vehicleServiceRepository.SetIsExtraChargeCalculatedByNetId(message.NetId, message.SupplyExtraChargeType);

        Sender.Tell(
            _supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetByNetId(
                _supplyRepositoriesFactory.NewVehicleServiceRepository(connection).GetSupplyOrderNetIdBySupplyOrderVehicleServiceNetId(message.NetId)
            )
        );

        ActorReferenceManager.Instance.Get(SupplyActorNames.SUPPLY_INVOICE_ACTOR).Tell(new UpdateSupplyInvoiceItemGrossPriceMessage(
            _supplyRepositoriesFactory
                .NewSupplyInvoiceRepository(connection)
                .GetIdByVehicleServiceId(vehicleService.Id),
            message.UserNetId
        ));
    }

    private void ProcessGetAllVehiclesRangedMessage(GetAllVehiclesRangedMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.From == null) message.From = DateTime.Now.Date;

        message.To = message.To?.Date.AddHours(23).AddMinutes(59).AddSeconds(59) ?? DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        Sender.Tell(_supplyRepositoriesFactory
            .NewVehicleServiceRepository(connection)
            .GetAllRanged(message.From.Value, message.To.Value)
        );
    }

    private void ProcessGetAllAvailableVehicleServicesMessage(GetAllAvailableVehicleServicesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory.NewVehicleServiceRepository(connection).GetAllAvailable());
    }

    private void ProcessUpdateVehicleServiceDocumentsMessage(UpdateVehicleServiceDocumentsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.VehicleService.BillOfLadingDocument != null && !message.VehicleService.BillOfLadingDocument.IsNew())
            _supplyRepositoriesFactory
                .NewBillOfLadingDocumentRepository(connection)
                .Update(message.VehicleService.BillOfLadingDocument);
        if (message.VehicleService.InvoiceDocuments.Any(d => !d.IsNew()))
            _supplyRepositoriesFactory
                .NewInvoiceDocumentRepository(connection)
                .Update(message.VehicleService.InvoiceDocuments.Where(d => !d.IsNew()));
        if (message.VehicleService.ActProvidingServiceDocument != null && !message.VehicleService.ActProvidingServiceDocument.Id.Equals(0))
            _supplyRepositoriesFactory
                .NewActProvidingServiceDocumentRepository(connection)
                .Update(message.VehicleService.ActProvidingServiceDocument);
        if (message.VehicleService.SupplyServiceAccountDocument != null && !message.VehicleService.SupplyServiceAccountDocument.Id.Equals(0))
            _supplyRepositoriesFactory
                .NewSupplyServiceAccountDocumentRepository(connection)
                .Update(message.VehicleService.SupplyServiceAccountDocument);

        Sender.Tell(_supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessUpdateDeliveryTermMessage(UpdateDeliveryTermMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IVehicleServiceRepository vehicleServiceRepository = _supplyRepositoriesFactory.NewVehicleServiceRepository(connection);

        if (message.VehicleService != null) {
            string oldTermDeliveryInDays = vehicleServiceRepository.GetTermDeliveryInDaysById(message.VehicleService.Id);

            if (oldTermDeliveryInDays != null && !oldTermDeliveryInDays.Equals(message.TermDeliveryInDays)) {
                vehicleServiceRepository.UpdateDeliveryTerms(message.VehicleService.Id, message.TermDeliveryInDays);

                ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(new RetrieveAndStoreAuditDataMessage(
                    message.UpdatedByNetId,
                    message.NetId,
                    "SupplyOrder.VehicleService",
                    new VehicleService { TermDeliveryInDays = message.TermDeliveryInDays },
                    new VehicleService { TermDeliveryInDays = oldTermDeliveryInDays }
                ));
            }

            Sender.Tell(_supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetByNetId(message.NetId));
        } else {
            Sender.Tell(null);
        }
    }

    private void ProccessUnassigningVehicleBeforeCalculatedExtraChargeService(UnassigningVehicleServiceBeforeCalculatedExtraChargeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            if (_supplyRepositoriesFactory.NewVehicleServiceRepository(connection).IsVehicleInOrder(message.SupplyOrderNetId, message.VehicleServiceNetId)) {
                long supplyOrderId = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetIdByNetId(message.SupplyOrderNetId);
                long vehicleServiceId = _supplyRepositoriesFactory.NewVehicleServiceRepository(connection).GetIdByNetId(message.VehicleServiceNetId);

                _supplyRepositoriesFactory
                    .NewSupplyOrderVehicleServiceRepository(connection)
                    .RemoveAllBySupplyOrderAndVehicleServiceId(supplyOrderId, vehicleServiceId);

                _supplyRepositoriesFactory
                    .NewPackingListRepository(connection)
                    .UnassigningAllByVehicleAndSupplyOrderId(supplyOrderId, vehicleServiceId);
            }

            Sender.Tell(_supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetByNetId(message.SupplyOrderNetId));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProccessRemoveVehicleServiceBeforeCalculatedExtraCharge(RemoveVehicleServiceBeforeCalculatedExtraChargeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            long vehicleServiceId = _supplyRepositoriesFactory.NewVehicleServiceRepository(connection).GetIdByNetId(message.VehicleServiceNetId);

            _supplyRepositoriesFactory
                .NewSupplyOrderVehicleServiceRepository(connection)
                .RemoveAllByVehicleServiceId(vehicleServiceId);

            _supplyRepositoriesFactory
                .NewPackingListRepository(connection)
                .UnassigninAllByVehicleServiceId(vehicleServiceId);

            _supplyRepositoriesFactory
                .NewVehicleServiceRepository(connection)
                .Remove(vehicleServiceId);

            Sender.Tell(_supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetByNetId(message.SupplyOrderNetId));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}