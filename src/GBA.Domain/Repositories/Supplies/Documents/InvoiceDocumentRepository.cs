using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.Documents;

public sealed class InvoiceDocumentRepository : IInvoiceDocumentRepository {
    private readonly IDbConnection _connection;

    public InvoiceDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<InvoiceDocument> invoiceDocuments) {
        _connection.Execute(
            "INSERT INTO InvoiceDocument (DocumentUrl, ContainerServiceID, CustomServiceID, PortWorkServiceID, TransportationServiceID, SupplyInvoiceID, FileName, ContentType, GeneratedName, CustomAgencyServiceID, PlaneDeliveryServiceID, PortCustomAgencyServiceID, VehicleDeliveryServiceID, SupplyOrderPolandPaymentDeliveryProtocolID, " +
            "PackingListId, MergedServiceId, Updated, VehicleServiceID, [Type]) " +
            "VALUES(@DocumentUrl, @ContainerServiceID, @CustomServiceID, @PortWorkServiceID, @TransportationServiceID, @SupplyInvoiceID, @FileName, @ContentType, @GeneratedName, @CustomAgencyServiceId, @PlaneDeliveryServiceId, @PortCustomAgencyServiceId, @VehicleDeliveryServiceId, @SupplyOrderPolandPaymentDeliveryProtocolID, " +
            "@PackingListId, @MergedServiceId, getutcdate(), @VehicleServiceID, @Type)",
            invoiceDocuments
        );
    }

    public void Add(InvoiceDocument invoiceDocument) {
        _connection.Execute(
            "INSERT INTO InvoiceDocument (DocumentUrl, ContainerServiceID, CustomServiceID, PortWorkServiceID, TransportationServiceID, SupplyInvoiceID, FileName, ContentType, GeneratedName, CustomAgencyServiceID, PlaneDeliveryServiceID, PortCustomAgencyServiceID, VehicleDeliveryServiceID, SupplyOrderPolandPaymentDeliveryProtocolID, " +
            "PackingListId, MergedServiceId, Updated, VehicleServiceID, [Type]) " +
            "VALUES(@DocumentUrl, @ContainerServiceID, @CustomServiceID, @PortWorkServiceID, @TransportationServiceID, @SupplyInvoiceID, @FileName, @ContentType, @GeneratedName, @CustomAgencyServiceId, @PlaneDeliveryServiceId, @PortCustomAgencyServiceId, @VehicleDeliveryServiceId, @SupplyOrderPolandPaymentDeliveryProtocolID, " +
            "@PackingListId, @MergedServiceId, getutcdate(), @VehicleServiceID, @Type)",
            invoiceDocument
        );
    }

    public void RemoveAll(Guid supplyInvoiceNetId) {
        _connection.Execute(
            "UPDATE InvoiceDocument SET Deleted = 1 " +
            "WHERE SupplyInvoiceID = (SELECT ID FROM SupplyInvoice " +
            "WHERE NetUID = @SupplyInvoiceNetId)",
            new { SupplyInvoiceNetId = supplyInvoiceNetId }
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE InvoiceDocument SET Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void Remove(IEnumerable<InvoiceDocument> invoiceDocuments) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET [Deleted] = 1" +
            ",[Updated] = getutcdate() " +
            "WHERE [ID] = @Id; ",
            invoiceDocuments);
    }

    public void RemoveAllByVehicleServiceIdExceptProvided(long vehicleServiceId, IEnumerable<long> notRemoveIds) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].VehicleServiceID = @Id " +
            "AND [InvoiceDocument].ID NOT IN @NotRemoveIds",
            new { Id = vehicleServiceId, NotRemoveIds = notRemoveIds }
        );
    }

    public void RemoveAllByVehicleServiceId(long vehicleServiceId) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].VehicleServiceID = @Id",
            new { Id = vehicleServiceId }
        );
    }

    public void Update(IEnumerable<InvoiceDocument> invoiceDocuments) {
        _connection.Execute(
            "UPDATE InvoiceDocument " +
            "SET DocumentUrl = @DocumentUrl, ContainerServiceID = @ContainerServiceID, CustomServiceID = @CustomServiceID, PortWorkServiceID = @PortWorkServiceID, " +
            "TransportationServiceID = @TransportationServiceID, SupplyInvoiceID = @SupplyInvoiceID, PackingListId = @PackingListId, " +
            "FileName = @FileName, ContentType = @ContentType, GeneratedName = @GeneratedName, CustomAgencyServiceId = @CustomAgencyServiceId, " +
            "PlaneDeliveryServiceId = @PlaneDeliveryServiceId, PortCustomAgencyServiceId = @PortCustomAgencyServiceId, " +
            "VehicleDeliveryServiceId = @VehicleDeliveryServiceId, SupplyOrderPolandPaymentDeliveryProtocolID = @SupplyOrderPolandPaymentDeliveryProtocolID, Updated = getutcdate()," +
            "VehicleServiceID = @VehicleServiceID, [Type] = @Type " +
            "WHERE NetUID = @NetUID",
            invoiceDocuments
        );
    }

    public void UpdateSupplyInvoiceId(long fromInvoiceId, long toInvoiceId) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET SupplyInvoiceID = @ToInvoiceId, Updated = GETUTCDATE() " +
            "WHERE SupplyInvoiceID = @FromInvoiceId",
            new { FromInvoiceId = fromInvoiceId, ToInvoiceId = toInvoiceId }
        );
    }

    public void RemoveAllBySupplyInvoiceId(long id) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE SupplyInvoiceID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllBySupplyInvoiceIdExceptProvided(long invoiceId, IEnumerable<long> notRemoveIds) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].SupplyInvoiceID = @Id " +
            "AND [InvoiceDocument].ID NOT IN @NotRemoveIds",
            new { Id = invoiceId, NotRemoveIds = notRemoveIds }
        );
    }

    public void RemoveAllByMergedServiceId(long id) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].MergedServiceID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByMergedServiceIdExceptProvided(long id, IEnumerable<long> notRemoveIds) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].MergedServiceID = @Id " +
            "AND [InvoiceDocument].ID NOT IN @NotRemoveIds",
            new { Id = id, NotRemoveIds = notRemoveIds }
        );
    }

    public void RemoveAllByCustomServiceId(long id) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].CustomServiceID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByCustomServiceIdExceptProvided(long id, IEnumerable<long> notRemoveIds) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].CustomServiceID = @Id " +
            "AND [InvoiceDocument].ID NOT IN @NotRemoveIds",
            new { Id = id, NotRemoveIds = notRemoveIds }
        );
    }

    public void RemoveAllByPortWorkServiceId(long id) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].PortWorkServiceID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByPortWorkServiceIdExceptProvided(long id, IEnumerable<long> notRemoveIds) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].PortWorkServiceID = @Id " +
            "AND [InvoiceDocument].ID NOT IN @NotRemoveIds",
            new { Id = id, NotRemoveIds = notRemoveIds }
        );
    }

    public void RemoveAllByPortCustomAgencyServiceId(long id) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].PortCustomAgencyServiceID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByPortCustomAgencyServiceIdExceptProvided(long id, IEnumerable<long> notRemoveIds) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].PortCustomAgencyServiceID = @Id " +
            "AND [InvoiceDocument].ID NOT IN @NotRemoveIds",
            new { Id = id, NotRemoveIds = notRemoveIds }
        );
    }

    public void RemoveAllByTransportationServiceId(long id) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].TransportationServiceID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByTransportationServiceIdExceptProvided(long id, IEnumerable<long> notRemoveIds) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].TransportationServiceID = @Id " +
            "AND [InvoiceDocument].ID NOT IN @NotRemoveIds",
            new { Id = id, NotRemoveIds = notRemoveIds }
        );
    }

    public void RemoveAllByCustomAgencyServiceId(long id) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].CustomAgencyServiceID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByCustomAgencyServiceIdExceptProvided(long id, IEnumerable<long> notRemoveIds) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].CustomAgencyServiceID = @Id " +
            "AND [InvoiceDocument].ID NOT IN @NotRemoveIds",
            new { Id = id, NotRemoveIds = notRemoveIds }
        );
    }

    public void RemoveAllByContainerServiceId(long id) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].ContainerServiceID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByContainerServiceIdExceptProvided(long id, IEnumerable<long> notRemoveIds) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].ContainerServiceID = @Id " +
            "AND [InvoiceDocument].ID NOT IN @NotRemoveIds",
            new { Id = id, NotRemoveIds = notRemoveIds }
        );
    }

    public void RemoveAllByPlaneDeliveryServiceId(long id) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].PlaneDeliveryServiceID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByPlaneDeliveryServiceIdExceptProvided(long id, IEnumerable<long> notRemoveIds) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].PlaneDeliveryServiceID = @Id " +
            "AND [InvoiceDocument].ID NOT IN @NotRemoveIds",
            new { Id = id, NotRemoveIds = notRemoveIds }
        );
    }

    public void RemoveAllByVehicleDeliveryServiceId(long id) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].VehicleDeliveryServiceID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByVehicleDeliveryServiceIdExceptProvided(long id, IEnumerable<long> notRemoveIds) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].VehicleDeliveryServiceID = @Id " +
            "AND [InvoiceDocument].ID NOT IN @NotRemoveIds",
            new { Id = id, NotRemoveIds = notRemoveIds }
        );
    }

    public void RemoveAllByPackingListId(long id) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].PackingListID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByPackingListIdExceptProvided(long id, IEnumerable<long> notRemoveIds) {
        _connection.Execute(
            "UPDATE [InvoiceDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [InvoiceDocument].PackingListID = @Id " +
            "AND [InvoiceDocument].ID NOT IN @NotRemoveIds",
            new { Id = id, NotRemoveIds = notRemoveIds }
        );
    }
}