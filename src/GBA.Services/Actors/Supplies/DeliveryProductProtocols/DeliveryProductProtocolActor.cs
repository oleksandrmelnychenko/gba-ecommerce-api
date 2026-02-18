using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers.PrintingDocuments;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Messages.Supplies.DeliveryProductProtocols;
using GBA.Domain.Messages.Supplies.HelperServices.BillOfLadings;
using GBA.Domain.Messages.Supplies.HelperServices.Mergeds;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.DeliveryProductProtocols.Contracts;
using GBA.Domain.Repositories.Supplies.HelperServices.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Supplies.DeliveryProductProtocols;

public class DeliveryProductProtocolActor : ReceiveActor {
    private const string DEFAULT_COMMENT = "Ввід залишків з 1С.";
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public DeliveryProductProtocolActor(
        IDbConnectionFactory connectionFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IXlsFactoryManager xlsFactoryManager) {
        _connectionFactory = connectionFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _xlsFactoryManager = xlsFactoryManager;

        Receive<AddNewDeliveryProductProtocolMessage>(ProcessAddNewDeliveryProductProtocolMessage);

        Receive<GetAllFilteredDeliveryProductProtocolMessage>(ProcessGetAllFilteredDeliveryProductProtocol);

        Receive<RemoveDeliveryProductProtocolMessage>(ProcessRemoveDeliveryProductProtocol);

        Receive<AddSupplyInvoicesToDeliverProductProtocolMessage>(ProcessAddSupplyInvoicesToDeliverProductProtocol);

        Receive<GetByNetIdDeliveryProductProtocolMessage>(ProcessGetByNetIdDeliveryProductProtocol);

        Receive<UpdateProtocolStatusMessage>(ProcessUpdateProtocolStatus);

        Receive<ResetGrossPriceInProtocolMessage>(ProcessResetGrossPriceInProtocol);

        Receive<GetAllFilteredDeliveryProductProtocolForPrintingMessage>(ProcessGetAllFilteredDeliveryProductProtocolForPrinting);

        Receive<MergeInvoicesMessage>(ProcessMergeInvoicesMessage);
    }

    private void ProcessAddNewDeliveryProductProtocolMessage(AddNewDeliveryProductProtocolMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IDeliveryProductProtocolDocumentRepository deliveryProductProtocolDocumentRepository =
                _supplyRepositoriesFactory.NewDeliveryProductProtocolDocumentRepository(connection);
            IDeliveryProductProtocolNumberRepository deliveryProductProtocolNumberRepository =
                _supplyRepositoriesFactory.NewDeliveryProductProtocolNumberRepository(connection);
            IDeliveryProductProtocolRepository deliveryProductProtocolRepository =
                _supplyRepositoriesFactory.NewDeliveryProductProtocolRepository(connection);

            message.DeliveryProductProtocol.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetUserIdByNetId(message.UserNtUId);

            DeliveryProductProtocolNumber lastNumber = deliveryProductProtocolNumberRepository.GetLastNumber(DEFAULT_COMMENT);

            string newNumber;

            if (lastNumber != null && lastNumber.Created.Year.Equals(DateTime.Now.Year))
                newNumber = string.Format("P{0:D10}", int.Parse(lastNumber.Number.Substring(1)) + 1);
            else
                newNumber = string.Format("P{0:D10}", 1);

            if (string.IsNullOrEmpty(newNumber)) {
                Sender.Tell(null);
                return;
            }

            message.DeliveryProductProtocol.DeliveryProductProtocolNumberId =
                deliveryProductProtocolNumberRepository.Add(new DeliveryProductProtocolNumber { Number = newNumber });

            message.DeliveryProductProtocol.OrganizationId = message.DeliveryProductProtocol.Organization.Id;

            long deliveryProductProtocolId = _supplyRepositoriesFactory.NewDeliveryProductProtocolRepository(connection).AddNew(message.DeliveryProductProtocol);

            foreach (DeliveryProductProtocolDocument document in message.DeliveryProductProtocol.DeliveryProductProtocolDocuments) {
                document.DeliveryProductProtocolId = deliveryProductProtocolId;
                document.Number = newNumber;

                deliveryProductProtocolDocumentRepository.Add(document);
            }

            Guid deliveryProductProtocolNetId = deliveryProductProtocolRepository.GetNetIdById(deliveryProductProtocolId);

            Sender.Tell(deliveryProductProtocolRepository.GetByNetId(deliveryProductProtocolNetId));
        } catch {
            Sender.Tell(new Exception(DeliveryProductProtocolResourceNames.SERVER_EXCEPTION));
        }
    }

    private void ProcessGetAllFilteredDeliveryProductProtocol(GetAllFilteredDeliveryProductProtocolMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_supplyRepositoriesFactory
                .NewDeliveryProductProtocolRepository(connection)
                .AllFiltered(message.From, message.To, message.Organization, message.Supplier, message.Limit, message.Offset));
        } catch {
            Sender.Tell(new Exception(DeliveryProductProtocolResourceNames.SERVER_EXCEPTION));
        }
    }

    private void ProcessRemoveDeliveryProductProtocol(RemoveDeliveryProductProtocolMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IDeliveryProductProtocolRepository deliveryProductProtocolRepository =
                _supplyRepositoriesFactory.NewDeliveryProductProtocolRepository(connection);

            long protocolId = deliveryProductProtocolRepository.GetIdByNetId(message.NetId);

            if (protocolId.Equals(0)) {
                Sender.Tell(false);
                return;
            }

            deliveryProductProtocolRepository.RemoveById(protocolId);

            Sender.Tell(true);
        } catch {
            Sender.Tell(false);
        }
    }

    private void ProcessAddSupplyInvoicesToDeliverProductProtocol(AddSupplyInvoicesToDeliverProductProtocolMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyInvoiceRepository supplyInvoiceRepository =
                _supplyRepositoriesFactory.NewSupplyInvoiceRepository(connection);

            if (message.Protocol.SupplyInvoices.Any()) {
                IEnumerable<long> ids = message.Protocol.SupplyInvoices.Where(s => !s.IsNew()).Select(s => s.Id);

                supplyInvoiceRepository.UnassignAllByDeliveryProductProtocolIdExceptProvided(message.Protocol.Id, ids);

                supplyInvoiceRepository.AssignProvidedToDeliveryProductProtocol(message.Protocol.Id, ids);
            } else {
                supplyInvoiceRepository.RemoveAllSupplyInvoiceFromDeliveryProductProtocolById(message.Protocol.Id);
            }

            Sender.Tell(
                _supplyRepositoriesFactory
                    .NewDeliveryProductProtocolRepository(connection)
                    .GetByNetId(message.Protocol.NetUid)
            );
        } catch {
            Sender.Tell(new Exception(DeliveryProductProtocolResourceNames.SERVER_EXCEPTION));
        }
    }

    private void ProcessGetByNetIdDeliveryProductProtocol(GetByNetIdDeliveryProductProtocolMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_supplyRepositoriesFactory
                .NewDeliveryProductProtocolRepository(connection)
                .GetByNetId(message.NetId));
        } catch {
            Sender.Tell(new Exception(DeliveryProductProtocolResourceNames.SERVER_EXCEPTION));
        }
    }

    private void ProcessUpdateProtocolStatus(UpdateProtocolStatusMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IDeliveryProductProtocolRepository deliveryProductProtocolRepository =
                _supplyRepositoriesFactory
                    .NewDeliveryProductProtocolRepository(connection);

            DeliveryProductProtocol protocol =
                deliveryProductProtocolRepository.GetWithoutIncludesByNetId(message.NetId);

            if (protocol == null) {
                Sender.Tell(new Exception());
                return;
            }

            if (protocol.IsShipped) {
                _supplyRepositoriesFactory
                    .NewDeliveryProductProtocolRepository(connection)
                    .UpdateIsCompletedByNetId(message.NetId);
            } else {
                _supplyRepositoriesFactory
                    .NewDeliveryProductProtocolRepository(connection)
                    .UpdateIsShippedByNetId(message.NetId);

                _supplyRepositoriesFactory
                    .NewBillOfLadingServiceRepository(connection)
                    .UpdateIsShippedByDeliveryProductProtocolId(protocol.Id);

                _supplyRepositoriesFactory
                    .NewSupplyInvoiceRepository(connection)
                    .UpdateIsShippedByDeliveryProductProtocolId(protocol.Id);
            }

            Sender.Tell(
                _supplyRepositoriesFactory
                    .NewDeliveryProductProtocolRepository(connection)
                    .GetByNetId(message.NetId)
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessResetGrossPriceInProtocol(ResetGrossPriceInProtocolMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            DeliveryProductProtocol protocol =
                _supplyRepositoriesFactory
                    .NewDeliveryProductProtocolRepository(connection)
                    .GetByNetId(message.ProtocolNetId);

            if (protocol.BillOfLadingService != null)
                ActorReferenceManager.Instance.Get(HelperServiceActorNames.BILL_OF_LADING_SERVICE).Tell(new ResetValueBillOfLadingServiceMessage(
                    protocol.BillOfLadingService.Id,
                    message.UserNetId
                ));

            ActorReferenceManager.Instance.Get(HelperServiceActorNames.MERGED_SERVICES_ACTOR).Tell(new ResetValueMergedServiceMessage(
                protocol.MergedServices.Select(x => x.Id),
                message.UserNetId
            ));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetAllFilteredDeliveryProductProtocolForPrinting(GetAllFilteredDeliveryProductProtocolForPrintingMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            object forPrint =
                _supplyRepositoriesFactory
                    .NewDeliveryProductProtocolRepository(connection)
                    .GetAllFilteredForPrinting(message.From, message.To);

            if (forPrint == null) {
                Sender.Tell((string.Empty, string.Empty));
                return;
            }

            PrintDocumentsHelper printDocumentsHelper = new(forPrint, message.DataForPrintings);

            List<Dictionary<string, string>> rows = printDocumentsHelper.GetRowsForPrintDocument();

            (string pathXls, string pathPdf) =
                _xlsFactoryManager
                    .NewPrintDocumentsManager()
                    .GetPrintDocument(
                        message.PathToFolder,
                        message.DataForPrintings,
                        rows);

            Sender.Tell((pathXls, pathPdf));
        } catch {
            Sender.Tell((string.Empty, string.Empty));
        }
    }

    private void ProcessMergeInvoicesMessage(MergeInvoicesMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyInvoiceRepository supplyInvoiceRepository = _supplyRepositoriesFactory.NewSupplyInvoiceRepository(connection);
            ISupplyInvoiceOrderItemRepository supplyInvoiceOrderItemRepository = _supplyRepositoriesFactory.NewSupplyInvoiceOrderItemRepository(connection);
            IPackingListRepository packingListRepository = _supplyRepositoriesFactory.NewPackingListRepository(connection);
            IPackingListPackageOrderItemRepository packingListPackageOrderItemRepository = _supplyRepositoriesFactory.NewPackingListPackageOrderItemRepository(connection);
            IInvoiceDocumentRepository invoiceDocumentRepository = _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection);
            ISupplyOrderPaymentDeliveryProtocolRepository paymentDeliveryProtocolRepository =
                _supplyRepositoriesFactory.NewSupplyOrderPaymentDeliveryProtocolRepository(connection);
            ISupplyInformationDeliveryProtocolRepository informationDeliveryProtocolRepository =
                _supplyRepositoriesFactory.NewSupplyInformationDeliveryProtocolRepository(connection);
            ISupplyInvoiceMergedServiceRepository invoiceMergedServiceRepository = _supplyRepositoriesFactory.NewSupplyInvoiceMergedServiceRepository(connection);
            ISupplyInvoiceBillOfLadingServiceRepository billOfLadingServiceRepository = _supplyRepositoriesFactory.NewSupplyInvoiceBillOfLadingServiceRepository(connection);
            ISupplyInvoiceDeliveryDocumentRepository invoiceDeliveryDocumentRepository = _supplyRepositoriesFactory.NewSupplyInvoiceDeliveryDocumentRepository(connection);

            DeliveryProductProtocol protocol = _supplyRepositoriesFactory.NewDeliveryProductProtocolRepository(connection).GetWithoutIncludesByNetId(message.NetId);
            IEnumerable<SupplyInvoice> invoices = supplyInvoiceRepository.GetAllByDeliveryProductProtocolId(protocol.Id, message.InvoiceNetIds);

            if (invoices.Count() <= 1) {
                Sender.Tell(new { IsSuccess = true });
                return;
            }

            SupplyInvoice mainInvoice = invoices.First();
            PackingList mainInvoicePackingList = mainInvoice.PackingLists.First();

            if (mainInvoice.PackingLists.Count > 1) {
                foreach (PackingList packingList in mainInvoice.PackingLists.Where(x => x.Id != mainInvoicePackingList.Id)) {
                    packingList.SupplyInvoiceId = mainInvoice.Id;
                    packingList.RootPackingListId = mainInvoicePackingList.Id;
                }

                packingListRepository.UpdateSupplyInvoiceIdAndRootId(mainInvoice.PackingLists.Where(x => x.Id != mainInvoicePackingList.Id));
                packingListPackageOrderItemRepository.UpdatePackingListId(mainInvoice.PackingLists.Where(x => x.Id != mainInvoicePackingList.Id).Select(x => x.Id),
                    mainInvoicePackingList.Id);
            }

            for (int i = 1; i < invoices.Count(); i++) {
                SupplyInvoice mergedInvoice = invoices.ElementAt(i);

                if (mergedInvoice.PackingLists.Any()) {
                    foreach (PackingList packingList in mergedInvoice.PackingLists) {
                        packingList.SupplyInvoiceId = mainInvoice.Id;
                        packingList.RootPackingListId = mainInvoicePackingList.Id;
                    }

                    packingListRepository.UpdateSupplyInvoiceIdAndRootId(mergedInvoice.PackingLists);
                    packingListPackageOrderItemRepository.UpdatePackingListId(mergedInvoice.PackingLists.Select(x => x.Id), mainInvoicePackingList.Id);
                }

                if (mergedInvoice.SupplyInvoiceOrderItems.Any()) {
                    foreach (SupplyInvoiceOrderItem invoiceOrderItem in mergedInvoice.SupplyInvoiceOrderItems) invoiceOrderItem.SupplyInvoiceId = mainInvoice.Id;

                    supplyInvoiceOrderItemRepository.UpdateSupplyInvoiceId(mergedInvoice.SupplyInvoiceOrderItems);
                }

                invoiceDocumentRepository.UpdateSupplyInvoiceId(mergedInvoice.Id, mainInvoice.Id);
                paymentDeliveryProtocolRepository.UpdateSupplyInvoiceId(mergedInvoice.Id, mainInvoice.Id);
                informationDeliveryProtocolRepository.UpdateSupplyInvoiceId(mergedInvoice.Id, mainInvoice.Id);
                invoiceMergedServiceRepository.UpdateSupplyInvoiceId(mergedInvoice.Id, mainInvoice.Id);
                billOfLadingServiceRepository.UpdateSupplyInvoiceId(mergedInvoice.Id, mainInvoice.Id);
                invoiceDeliveryDocumentRepository.UpdateSupplyInvoiceId(mergedInvoice.Id, mainInvoice.Id);

                supplyInvoiceRepository.Merge(mergedInvoice.NetUid, mainInvoice.Id);
                Sender.Tell(new { IsSuccess = true });
            }
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}