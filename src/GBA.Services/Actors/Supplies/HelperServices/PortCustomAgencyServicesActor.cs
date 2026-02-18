using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Messages.Supplies.HelperServices;
using GBA.Domain.Messages.Supplies.HelperServices.PortCustomAgencies;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Services.Actors.Supplies.HelperServices;

public sealed class PortCustomAgencyServicesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;

    public PortCustomAgencyServicesActor(
        IDbConnectionFactory connectionFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;

        Receive<InsertOrUpdatePortCustomAgencyServiceMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.PortCustomAgencyService.InvoiceDocuments.Any()) {
                IInvoiceDocumentRepository invoiceDocumentRepository = _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection);

                IEnumerable<InvoiceDocument> documentsToAdd = message.PortCustomAgencyService.InvoiceDocuments.Where(d => d.IsNew());
                IEnumerable<InvoiceDocument> documentsToUpdate = message.PortCustomAgencyService.InvoiceDocuments.Where(d => !d.IsNew());

                foreach (InvoiceDocument document in documentsToAdd) document.PortCustomAgencyServiceId = message.PortCustomAgencyService.Id;

                invoiceDocumentRepository.Add(documentsToAdd);
                invoiceDocumentRepository.Update(documentsToUpdate);
            }

            if (message.PortCustomAgencyService.ActProvidingServiceDocument != null && !message.PortCustomAgencyService.ActProvidingServiceDocument.Id.Equals(0))
                _supplyRepositoriesFactory
                    .NewActProvidingServiceDocumentRepository(connection)
                    .Update(message.PortCustomAgencyService.ActProvidingServiceDocument);

            if (message.PortCustomAgencyService.SupplyServiceAccountDocument != null && !message.PortCustomAgencyService.SupplyServiceAccountDocument.Id.Equals(0))
                _supplyRepositoriesFactory
                    .NewSupplyServiceAccountDocumentRepository(connection)
                    .Update(message.PortCustomAgencyService.SupplyServiceAccountDocument);

            Sender.Tell(
                _supplyRepositoriesFactory
                    .NewSupplyOrderRepository(connection)
                    .GetByNetId(message.NetId)
            );
        });

        Receive<GetAllDetailItemsByServiceNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_supplyRepositoriesFactory
                .NewServiceDetailItemRepository(connection)
                .GetAllByNetIdAndType(message.NetId, SupplyServiceType.PortCustomAgency)
            );
        });
    }
}