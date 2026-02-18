using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;
using GBA.Domain.Entities.Sales.Shipments;
using GBA.Domain.Entities.Transporters;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales.Shipments;

public sealed class ShipmentListRepository : IShipmentListRepository {
    private readonly IDbConnection _connection;

    public ShipmentListRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ShipmentList shipmentList) {
        return _connection.Query<long>(
                "INSERT INTO [ShipmentList] (Number, Comment, FromDate, IsSent, TransporterId, ResponsibleId, Updated) " +
                "VALUES (@Number, @Comment, @FromDate, @IsSent, @TransporterId, @ResponsibleId, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                shipmentList
            )
            .Single();
    }

    public void Update(ShipmentList shipmentList) {
        _connection.Execute(
            "UPDATE [ShipmentList] " +
            "SET Number = @Number, Comment = @Comment, FromDate = @FromDate, IsSent = @IsSent, ResponsibleId = @ResponsibleId, Updated = GETUTCDATE() " +
            "WHERE [ShipmentList].ID = @Id",
            shipmentList
        );
    }

    public ShipmentList GetLastRecord() {
        return _connection.Query<ShipmentList>(
                "SELECT TOP(1) * " +
                "FROM [ShipmentList] " +
                "ORDER BY [ShipmentList].ID DESC"
            )
            .SingleOrDefault();
    }

    public ShipmentList GetById(long id) {
        ShipmentList toReturn =
            _connection.Query<ShipmentList, Transporter, User, ShipmentList>(
                    "SELECT * " +
                    "FROM [ShipmentList] " +
                    "LEFT JOIN [Transporter] " +
                    "ON [Transporter].ID = [ShipmentList].TransporterID " +
                    "LEFT JOIN [User] AS [Responsible] " +
                    "ON [Responsible].ID = [ShipmentList].ResponsibleID " +
                    "WHERE [ShipmentList].ID = @Id",
                    (shipmentList, transporter, responsible) => {
                        shipmentList.Transporter = transporter;
                        shipmentList.Responsible = responsible;

                        return shipmentList;
                    },
                    new { Id = id }
                )
                .SingleOrDefault();

        if (toReturn != null)
            toReturn.ShipmentListItems =
                _connection.Query<ShipmentListItem, Sale, BaseLifeCycleStatus, SaleNumber, DeliveryRecipient, DeliveryRecipientAddress, ShipmentListItem>(
                    "SELECT * " +
                    "FROM [ShipmentListItem] " +
                    "LEFT JOIN [Sale] " +
                    "ON [Sale].ID = [ShipmentListItem].SaleID " +
                    "LEFT JOIN [BaseLifeCycleStatus] " +
                    "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
                    "LEFT JOIN [SaleNumber] " +
                    "ON [SaleNumber].ID = [Sale].SaleNumberID " +
                    "LEFT JOIN [DeliveryRecipient] " +
                    "ON [DeliveryRecipient].ID = [Sale].DeliveryRecipientID " +
                    "LEFT JOIN [DeliveryRecipientAddress] " +
                    "ON [DeliveryRecipientAddress].ID = [Sale].DeliveryRecipientAddressID " +
                    "WHERE [ShipmentListItem].ShipmentListID = @Id " +
                    "AND [ShipmentListItem].Deleted = 0 ",
                    (item, sale, status, number, deliveryRecipient, deliveryRecipientAddress) => {
                        sale.BaseLifeCycleStatus = status;
                        sale.SaleNumber = number;
                        sale.DeliveryRecipient = deliveryRecipient;
                        sale.DeliveryRecipientAddress = deliveryRecipientAddress;

                        item.Sale = sale;

                        return item;
                    },
                    new { toReturn.Id }
                ).ToList();

        return toReturn;
    }

    public ShipmentList GetByNetId(Guid netId) {
        ShipmentList toReturn =
            _connection.Query<ShipmentList, Transporter, User, ShipmentList>(
                    "SELECT * " +
                    "FROM [ShipmentList] " +
                    "LEFT JOIN [Transporter] " +
                    "ON [Transporter].ID = [ShipmentList].TransporterID " +
                    "LEFT JOIN [User] AS [Responsible] " +
                    "ON [Responsible].ID = [ShipmentList].ResponsibleID " +
                    "WHERE [ShipmentList].NetUID = @NetId",
                    (shipmentList, transporter, responsible) => {
                        shipmentList.Transporter = transporter;
                        shipmentList.Responsible = responsible;

                        return shipmentList;
                    },
                    new { NetId = netId }
                )
                .SingleOrDefault();

        if (toReturn == null) return null;

        Type[] types = {
            typeof(ShipmentListItem),
            typeof(Sale),
            typeof(WarehousesShipment),
            typeof(BaseLifeCycleStatus),
            typeof(SaleNumber),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(ClientAgreement),
            typeof(Client)
        };

        Func<object[], ShipmentListItem> mapper = objects => {
            ShipmentListItem item = (ShipmentListItem)objects[0];
            Sale sale = (Sale)objects[1];
            WarehousesShipment warehousesShipment = (WarehousesShipment)objects[2];
            BaseLifeCycleStatus status = (BaseLifeCycleStatus)objects[3];
            SaleNumber number = (SaleNumber)objects[4];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[5];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[6];
            ClientAgreement clientAgreement = (ClientAgreement)objects[7];
            Client client = (Client)objects[8];

            clientAgreement.Client = client;
            sale.WarehousesShipment = warehousesShipment;
            sale.ClientAgreement = clientAgreement;
            sale.BaseLifeCycleStatus = status;
            sale.SaleNumber = number;
            sale.DeliveryRecipient = deliveryRecipient;
            sale.DeliveryRecipientAddress = deliveryRecipientAddress;

            item.Sale = sale;

            return item;
        };

        toReturn.ShipmentListItems =
            _connection.Query(
                "SELECT * " +
                "FROM [ShipmentListItem] " +
                "LEFT JOIN [Sale] " +
                "ON [Sale].ID = [ShipmentListItem].SaleID " +
                "LEFT JOIN [WarehousesShipment] " +
                "ON [WarehousesShipment].SaleID = [Sale].ID " +
                "LEFT JOIN [BaseLifeCycleStatus] " +
                "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
                "LEFT JOIN [SaleNumber] " +
                "ON [SaleNumber].ID = [Sale].SaleNumberID " +
                "LEFT JOIN [DeliveryRecipient] " +
                "ON [DeliveryRecipient].ID = [Sale].DeliveryRecipientID " +
                "LEFT JOIN [DeliveryRecipientAddress] " +
                "ON [DeliveryRecipientAddress].ID = [Sale].DeliveryRecipientAddressID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [ClientAgreement].ClientID " +
                "WHERE [ShipmentListItem].ShipmentListID = @Id " +
                "AND [ShipmentListItem].Deleted = 0",
                types,
                mapper,
                new { toReturn.Id }
            ).ToList();

        return toReturn;
    }

    public ShipmentList GetByFillteredNetId(DateTime from, DateTime to, Guid netId) {
        ShipmentList toReturn =
            _connection.Query<ShipmentList, Transporter, User, ShipmentList>(
                    "SELECT * " +
                    "FROM [ShipmentList] " +
                    "LEFT JOIN [Transporter] " +
                    "ON [Transporter].ID = [ShipmentList].TransporterID " +
                    "LEFT JOIN [User] AS [Responsible] " +
                    "ON [Responsible].ID = [ShipmentList].ResponsibleID " +
                    "WHERE [ShipmentList].NetUID = @NetId",
                    (shipmentList, transporter, responsible) => {
                        shipmentList.Transporter = transporter;
                        shipmentList.Responsible = responsible;

                        return shipmentList;
                    },
                    new { NetId = netId }
                )
                .SingleOrDefault();

        if (toReturn == null) return null;

        Type[] types = {
            typeof(ShipmentListItem),
            typeof(Sale),
            typeof(WarehousesShipment),
            typeof(BaseLifeCycleStatus),
            typeof(SaleNumber),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(ClientAgreement),
            typeof(Client)
        };

        Func<object[], ShipmentListItem> mapper = objects => {
            ShipmentListItem item = (ShipmentListItem)objects[0];
            Sale sale = (Sale)objects[1];
            WarehousesShipment warehousesShipment = (WarehousesShipment)objects[2];
            BaseLifeCycleStatus status = (BaseLifeCycleStatus)objects[3];
            SaleNumber number = (SaleNumber)objects[4];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[5];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[6];
            ClientAgreement clientAgreement = (ClientAgreement)objects[7];
            Client client = (Client)objects[8];

            clientAgreement.Client = client;
            sale.WarehousesShipment = warehousesShipment;
            sale.ClientAgreement = clientAgreement;
            sale.BaseLifeCycleStatus = status;
            sale.SaleNumber = number;
            sale.DeliveryRecipient = deliveryRecipient;
            sale.DeliveryRecipientAddress = deliveryRecipientAddress;

            item.Sale = sale;

            return item;
        };

        toReturn.ShipmentListItems =
            _connection.Query(
                "SELECT * " +
                "FROM [ShipmentListItem] " +
                "LEFT JOIN [Sale] " +
                "ON [Sale].ID = [ShipmentListItem].SaleID " +
                "LEFT JOIN [WarehousesShipment] " +
                "ON [WarehousesShipment].SaleID = [Sale].ID " +
                "LEFT JOIN [BaseLifeCycleStatus] " +
                "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
                "LEFT JOIN [SaleNumber] " +
                "ON [SaleNumber].ID = [Sale].SaleNumberID " +
                "LEFT JOIN [DeliveryRecipient] " +
                "ON [DeliveryRecipient].ID = [Sale].DeliveryRecipientID " +
                "LEFT JOIN [DeliveryRecipientAddress] " +
                "ON [DeliveryRecipientAddress].ID = [Sale].DeliveryRecipientAddressID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [ClientAgreement].ClientID " +
                "WHERE [ShipmentListItem].ShipmentListID = @Id " +
                "AND [ShipmentListItem].Deleted = 0 " +
                "AND [Sale].ChangedToInvoice >= @From " +
                "AND [Sale].ChangedToInvoice <= @To ",
                types,
                mapper,
                new {
                    toReturn.Id, From = from, To = to
                }
            ).ToList();

        return toReturn;
    }

    public ShipmentList GetByTransporterFilteredNetId(DateTime from, DateTime to, Guid netId) {
        ShipmentList toReturn =
            _connection.Query<ShipmentList, Transporter, User, ShipmentList>(
                    "SELECT TOP(1) * " +
                    "FROM [ShipmentList] " +
                    "LEFT JOIN [Transporter] " +
                    "ON [Transporter].ID = [ShipmentList].TransporterID " +
                    "LEFT JOIN [User] AS [Responsible] " +
                    "ON [Responsible].ID = [ShipmentList].ResponsibleID " +
                    "WHERE [Transporter].NetUID = @NetId " +
                    "AND [ShipmentList].IsSent = 0 " +
                    "ORDER BY [ShipmentList].ID DESC",
                    (shipmentList, transporter, responsible) => {
                        shipmentList.Transporter = transporter;
                        shipmentList.Responsible = responsible;

                        return shipmentList;
                    },
                    new { NetId = netId }
                )
                .SingleOrDefault();

        if (toReturn != null) {
            toReturn.ShipmentListItems =
                _connection.Query<ShipmentListItem, Sale, BaseLifeCycleStatus, SaleNumber, DeliveryRecipient, DeliveryRecipientAddress, ShipmentListItem>(
                    "SELECT * " +
                    "FROM [ShipmentListItem] " +
                    "LEFT JOIN [Sale] " +
                    "ON [Sale].ID = [ShipmentListItem].SaleID " +
                    "LEFT JOIN [BaseLifeCycleStatus] " +
                    "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
                    "LEFT JOIN [SaleNumber] " +
                    "ON [SaleNumber].ID = [Sale].SaleNumberID " +
                    "LEFT JOIN [DeliveryRecipient] " +
                    "ON [DeliveryRecipient].ID = [Sale].DeliveryRecipientID " +
                    "LEFT JOIN [DeliveryRecipientAddress] " +
                    "ON [DeliveryRecipientAddress].ID = [Sale].DeliveryRecipientAddressID " +
                    "WHERE [ShipmentListItem].ShipmentListID = @Id " +
                    "AND [ShipmentListItem].Deleted = 0 " +
                    "AND [Sale].ChangedToInvoice >= @From " +
                    "AND [Sale].ChangedToInvoice <= @To ",
                    (item, sale, status, number, deliveryRecipient, deliveryRecipientAddress) => {
                        sale.BaseLifeCycleStatus = status;
                        sale.SaleNumber = number;
                        sale.DeliveryRecipient = deliveryRecipient;
                        sale.DeliveryRecipientAddress = deliveryRecipientAddress;

                        item.Sale = sale;

                        return item;
                    },
                    new {
                        toReturn.Id, From = from, To = to
                    }
                ).ToList();
            if (!toReturn.ShipmentListItems.Any()) return null;
        }

        return toReturn;
    }

    public ShipmentList GetByTransporterNetId(Guid netId) {
        ShipmentList toReturn =
            _connection.Query<ShipmentList, Transporter, User, ShipmentList>(
                    "SELECT TOP(1) * " +
                    "FROM [ShipmentList] " +
                    "LEFT JOIN [Transporter] " +
                    "ON [Transporter].ID = [ShipmentList].TransporterID " +
                    "LEFT JOIN [User] AS [Responsible] " +
                    "ON [Responsible].ID = [ShipmentList].ResponsibleID " +
                    "WHERE [Transporter].NetUID = @NetId " +
                    "AND [ShipmentList].IsSent = 0 " +
                    "ORDER BY [ShipmentList].ID DESC",
                    (shipmentList, transporter, responsible) => {
                        shipmentList.Transporter = transporter;
                        shipmentList.Responsible = responsible;

                        return shipmentList;
                    },
                    new { NetId = netId }
                )
                .SingleOrDefault();

        if (toReturn != null)
            toReturn.ShipmentListItems =
                _connection.Query<ShipmentListItem, Sale, BaseLifeCycleStatus, SaleNumber, DeliveryRecipient, DeliveryRecipientAddress, ShipmentListItem>(
                    "SELECT * " +
                    "FROM [ShipmentListItem] " +
                    "LEFT JOIN [Sale] " +
                    "ON [Sale].ID = [ShipmentListItem].SaleID " +
                    "LEFT JOIN [BaseLifeCycleStatus] " +
                    "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
                    "LEFT JOIN [SaleNumber] " +
                    "ON [SaleNumber].ID = [Sale].SaleNumberID " +
                    "LEFT JOIN [DeliveryRecipient] " +
                    "ON [DeliveryRecipient].ID = [Sale].DeliveryRecipientID " +
                    "LEFT JOIN [DeliveryRecipientAddress] " +
                    "ON [DeliveryRecipientAddress].ID = [Sale].DeliveryRecipientAddressID " +
                    "WHERE [ShipmentListItem].ShipmentListID = @Id " +
                    "AND [ShipmentListItem].Deleted = 0",
                    (item, sale, status, number, deliveryRecipient, deliveryRecipientAddress) => {
                        sale.BaseLifeCycleStatus = status;
                        sale.SaleNumber = number;
                        sale.DeliveryRecipient = deliveryRecipient;
                        sale.DeliveryRecipientAddress = deliveryRecipientAddress;

                        item.Sale = sale;

                        return item;
                    },
                    new { toReturn.Id }
                ).ToList();

        return toReturn;
    }

    public IEnumerable<ShipmentList> GetAllFiltered(DateTime from, DateTime to, Guid netId, long limit, long offset) {
        string sqlExpression =
            "; WITH [Rowed_CTE] " +
            "AS (" +
            "SELECT [ShipmentList].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [ShipmentList].FromDate DESC) AS [RowNumber] " +
            "FROM [ShipmentList] " +
            "LEFT JOIN [Transporter] " +
            "ON [Transporter].ID = [ShipmentList].TransporterID " +
            "WHERE [ShipmentList].Deleted = 0 " +
            "AND [ShipmentList].FromDate >= @From " +
            "AND [ShipmentList].FromDate <= @To ";

        if (!netId.Equals(Guid.Empty)) sqlExpression += "AND [Transporter].NetUID = @NetId";

        sqlExpression +=
            ") " +
            "SELECT * " +
            "FROM [ShipmentList] " +
            "LEFT JOIN [Transporter] " +
            "ON [Transporter].ID = [ShipmentList].TransporterID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [ShipmentList].ResponsibleID " +
            "WHERE [ShipmentList].ID IN (" +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            "WHERE [Rowed_CTE].RowNumber > @Offset " +
            "AND [Rowed_CTE].RowNumber <= @Limit + @Offset" +
            ")";

        IEnumerable<ShipmentList> shipments =
            _connection.Query<ShipmentList, Transporter, User, ShipmentList>(
                sqlExpression,
                (shipmentList, transporter, responsible) => {
                    shipmentList.Transporter = transporter;
                    shipmentList.Responsible = responsible;

                    return shipmentList;
                },
                new { From = from, To = to, NetId = netId, Limit = limit, Offset = offset }
            );

        if (!shipments.Any()) return shipments;

        Type[] types = {
            typeof(ShipmentListItem),
            typeof(Sale),
            typeof(BaseLifeCycleStatus),
            typeof(SaleNumber),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(CustomersOwnTtn),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(Agreement),
            typeof(Currency)
        };

        Func<object[], ShipmentListItem> mapper = objects => {
            ShipmentListItem item = (ShipmentListItem)objects[0];
            Sale sale = (Sale)objects[1];
            BaseLifeCycleStatus status = (BaseLifeCycleStatus)objects[2];
            SaleNumber number = (SaleNumber)objects[3];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[4];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[5];
            CustomersOwnTtn customersOwnTtn = (CustomersOwnTtn)objects[6];
            ClientAgreement clientAgreement = (ClientAgreement)objects[7];
            Client client = (Client)objects[8];
            Agreement agreement = (Agreement)objects[9];
            Currency currency = (Currency)objects[10];

            agreement.Currency = currency;

            clientAgreement.Agreement = agreement;
            clientAgreement.Client = client;

            sale.BaseLifeCycleStatus = status;
            sale.SaleNumber = number;
            sale.ClientAgreement = clientAgreement;
            sale.DeliveryRecipient = deliveryRecipient;
            sale.DeliveryRecipientAddress = deliveryRecipientAddress;
            sale.CustomersOwnTtn = customersOwnTtn;

            item.Sale = sale;

            shipments.First(s => s.Id.Equals(item.ShipmentListId)).ShipmentListItems.Add(item);

            return item;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [ShipmentListItem] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [ShipmentListItem].SaleID " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [DeliveryRecipient] " +
            "ON [DeliveryRecipient].ID = [Sale].DeliveryRecipientID " +
            "LEFT JOIN [DeliveryRecipientAddress] " +
            "ON [DeliveryRecipientAddress].ID = [Sale].DeliveryRecipientAddressID " +
            "LEFT JOIN [CustomersOwnTtn] " +
            "ON [CustomersOwnTtn].ID = [Sale].CustomersOwnTtnID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [Sale].ClientAgreementID = [ClientAgreement].ID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "WHERE [ShipmentListItem].ShipmentListID IN @Ids " +
            "AND [ShipmentListItem].Deleted = 0",
            types,
            mapper,
            new { Ids = shipments.Select(s => s.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );


        foreach (ShipmentList shipment in shipments) {
            Type[] typesUpdateDataCarrier = {
                typeof(UpdateDataCarrier),
                typeof(User),
                typeof(Transporter)
            };

            Func<object[], UpdateDataCarrier> mapperUpdateDataCarrier = objects => {
                UpdateDataCarrier updateDataCarrier = (UpdateDataCarrier)objects[0];
                User user = (User)objects[1];
                Transporter transporter = (Transporter)objects[2];
                ShipmentListItem shipmentListItem = shipment.ShipmentListItems.First(x => x.SaleId.Equals(updateDataCarrier.SaleId));

                if (user != null) updateDataCarrier.User = user;
                if (transporter != null) updateDataCarrier.Transporter = transporter;
                shipmentListItem.Sale.UpdateDataCarrier.Add(updateDataCarrier);
                return updateDataCarrier;
            };

            _connection.Query(
                "SELECT " +
                "[UpdateDataCarrier].* " +
                ",[User].* " +
                ",[Transporter].* " +
                "From [UpdateDataCarrier] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [UpdateDataCarrier].UserId " +
                "LEFT JOIN [Transporter] " +
                "ON [Transporter].ID = [UpdateDataCarrier].TransporterId " +
                "WHERE [UpdateDataCarrier].SaleId IN @Ids " +
                "AND [UpdateDataCarrier].IsEditTransporter = 0 ",
                typesUpdateDataCarrier,
                mapperUpdateDataCarrier,
                new {
                    Ids = shipment.ShipmentListItems.Select(x => x.SaleId)
                });
        }


        foreach (ShipmentList shipment in shipments) {
            Type[] typesWarehousesShipment = {
                typeof(WarehousesShipment),
                typeof(User),
                typeof(Transporter)
            };

            Func<object[], WarehousesShipment> mapperWarehousesShipment = objects => {
                WarehousesShipment warehousesShipment = (WarehousesShipment)objects[0];
                User user = (User)objects[1];
                Transporter transporter = (Transporter)objects[2];
                ShipmentListItem shipmentListItem = shipment.ShipmentListItems.First(x => x.SaleId.Equals(warehousesShipment.SaleId));

                if (user != null) warehousesShipment.User = user;
                if (transporter != null) warehousesShipment.Transporter = transporter;
                shipmentListItem.Sale.WarehousesShipment = warehousesShipment;
                return warehousesShipment;
            };

            _connection.Query(
                "SELECT " +
                "[WarehousesShipment].* " +
                ",[User].* " +
                ",[Transporter].* " +
                "From [WarehousesShipment] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [WarehousesShipment].UserId " +
                "LEFT JOIN [Transporter] " +
                "ON [Transporter].ID = [WarehousesShipment].TransporterId " +
                "WHERE [WarehousesShipment].SaleId IN @Ids ",
                typesWarehousesShipment,
                mapperWarehousesShipment,
                new {
                    Ids = shipment.ShipmentListItems.Select(x => x.SaleId)
                });
        }

        Type[] typesHistoryInvoiceEdit = {
            typeof(HistoryInvoiceEdit),
            typeof(OrderItemBaseShiftStatus)
        };

        Func<object[], HistoryInvoiceEdit> mapperHistoryInvoiceEdit = objects => {
            HistoryInvoiceEdit historyInvoice = (HistoryInvoiceEdit)objects[0];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[1];

            ShipmentListItem saleFromList = shipments.SelectMany(x => x.ShipmentListItems.Where(y => y.Sale.Id.Equals(historyInvoice.SaleId))).FirstOrDefault();

            if (!saleFromList.Sale.HistoryInvoiceEdit.Any(p => p.Id.Equals(historyInvoice.Id))) {
                historyInvoice.OrderItemBaseShiftStatuses.Add(orderItemBaseShiftStatus);
                saleFromList.Sale.HistoryInvoiceEdit.Add(historyInvoice);
                if (!historyInvoice.IsDevelopment)
                    saleFromList.Sale.IsDevelopment = false;
                else
                    saleFromList.Sale.IsDevelopment = true;
            } else {
                HistoryInvoiceEdit historyinvoiceEditFromList = saleFromList.Sale.HistoryInvoiceEdit.First(s => s.Id.Equals(historyInvoice.Id));

                historyinvoiceEditFromList.OrderItemBaseShiftStatuses.Add(orderItemBaseShiftStatus);
                if (!historyinvoiceEditFromList.IsDevelopment)
                    saleFromList.Sale.IsDevelopment = false;
                else
                    saleFromList.Sale.IsDevelopment = true;
            }

            return historyInvoice;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [HistoryInvoiceEdit] " +
            "LEFT JOIN [OrderItemBaseShiftStatus] " +
            "ON [OrderItemBaseShiftStatus].HistoryInvoiceEditID = HistoryInvoiceEdit.ID " +
            "WHERE HistoryInvoiceEdit.SaleID IN @Ids " +
            "AND HistoryInvoiceEdit.Deleted = 0 ",
            typesHistoryInvoiceEdit,
            mapperHistoryInvoiceEdit,
            new { Ids = shipments.SelectMany(x => x.ShipmentListItems.Select(x => x.Sale.Id)) }
        );

        return shipments;
    }

    public IEnumerable<ShipmentList> GetDocumentFiltered() {
        string sqlExpression =
            "; WITH [Rowed_CTE] " +
            "AS (" +
            "SELECT [ShipmentList].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [ShipmentList].FromDate DESC) AS [RowNumber] " +
            "FROM [ShipmentList] " +
            "LEFT JOIN [Transporter] " +
            "ON [Transporter].ID = [ShipmentList].TransporterID " +
            "WHERE [ShipmentList].Deleted = 0 ";
        sqlExpression +=
            ") " +
            "SELECT * " +
            "FROM [ShipmentList] " +
            "LEFT JOIN [Transporter] " +
            "ON [Transporter].ID = [ShipmentList].TransporterID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [ShipmentList].ResponsibleID " +
            "WHERE [ShipmentList].ID IN (" +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            ")";

        IEnumerable<ShipmentList> shipments =
            _connection.Query<ShipmentList, Transporter, User, ShipmentList>(
                sqlExpression,
                (shipmentList, transporter, responsible) => {
                    shipmentList.Transporter = transporter;
                    shipmentList.Responsible = responsible;
                    return shipmentList;
                },
                new { }
            );

        if (!shipments.Any()) return shipments;

        Type[] types = {
            typeof(ShipmentListItem),
            typeof(Sale),
            typeof(BaseLifeCycleStatus),
            typeof(SaleNumber),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(Agreement),
            typeof(Currency)
        };

        Func<object[], ShipmentListItem> mapper = objects => {
            ShipmentListItem item = (ShipmentListItem)objects[0];
            Sale sale = (Sale)objects[1];
            BaseLifeCycleStatus status = (BaseLifeCycleStatus)objects[2];
            SaleNumber number = (SaleNumber)objects[3];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[4];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[5];
            ClientAgreement clientAgreement = (ClientAgreement)objects[6];
            Client client = (Client)objects[7];
            Agreement agreement = (Agreement)objects[8];
            Currency currency = (Currency)objects[9];

            agreement.Currency = currency;

            clientAgreement.Agreement = agreement;
            clientAgreement.Client = client;

            sale.BaseLifeCycleStatus = status;
            sale.SaleNumber = number;
            sale.ClientAgreement = clientAgreement;
            sale.DeliveryRecipient = deliveryRecipient;
            sale.DeliveryRecipientAddress = deliveryRecipientAddress;

            item.Sale = sale;

            shipments.First(s => s.Id.Equals(item.ShipmentListId)).ShipmentListItems.Add(item);

            return item;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [ShipmentListItem] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [ShipmentListItem].SaleID " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [DeliveryRecipient] " +
            "ON [DeliveryRecipient].ID = [Sale].DeliveryRecipientID " +
            "LEFT JOIN [DeliveryRecipientAddress] " +
            "ON [DeliveryRecipientAddress].ID = [Sale].DeliveryRecipientAddressID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [Sale].ClientAgreementID = [ClientAgreement].ID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "WHERE [ShipmentListItem].ShipmentListID IN @Ids " +
            "AND [ShipmentListItem].Deleted = 0",
            types,
            mapper,
            new { Ids = shipments.Select(s => s.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return shipments;
    }
}