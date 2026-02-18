using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.Shipments;
using GBA.Domain.Entities.Transporters;
using GBA.Domain.EntityHelpers.SalesModels.Models;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class ProtocolActEditInvoiceRepository : IProtocolActEditInvoiceRepository {
    private readonly IDbConnection _connection;

    public ProtocolActEditInvoiceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public double GetSalesHistoryQtyModel() {
        List<long> idsSalesHistoryModel = _connection.Query<long>(
            "SELECT COUNT(*) AS [TotalRowQty] " +
            "From [UpdateDataCarrier] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [UpdateDataCarrier].SaleId " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [CLient] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [UpdateDataCarrier].IsDevelopment = 0 " +
            "AND [UpdateDataCarrier].IsEditTransporter = 1 " +
            "UNION " +
            "SELECT COUNT(*) AS [TotalRowQty] " +
            "From [HistoryInvoiceEdit] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [HistoryInvoiceEdit].SaleId " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [CLient] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [HistoryInvoiceEdit].IsDevelopment = 0 " +
            "AND [HistoryInvoiceEdit].IsPrinted = 1 " +
            "AND [HistoryInvoiceEdit].Deleted = 0 "
        ).ToList();
        return idsSalesHistoryModel.Sum();
    }

    public double GetEditTransporterQtyModel() {
        List<long> idsSalesHistoryModel = _connection.Query<long>(
            "SELECT COUNT(*) AS [TotalRowQty] " +
            "From [UpdateDataCarrier] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [UpdateDataCarrier].SaleId " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [CLient] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [UpdateDataCarrier].IsDevelopment = 0" +
            "AND [UpdateDataCarrier].Deleted = 0 " +
            "AND [UpdateDataCarrier].IsEditTransporter = 1 "
        ).ToList();
        return idsSalesHistoryModel.Sum();
    }

    public double GetEditActForEditingQtyModel() {
        List<long> idsSalesHistoryModel = _connection.Query<long>(
            "SELECT COUNT(*) AS [TotalRowQty] " +
            "From [HistoryInvoiceEdit] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [HistoryInvoiceEdit].SaleId " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [CLient] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [HistoryInvoiceEdit].IsDevelopment = 0 " +
            "AND [HistoryInvoiceEdit].IsPrinted = 1 " +
            "AND [HistoryInvoiceEdit].Deleted = 0 "
        ).ToList();
        return idsSalesHistoryModel.Sum();
    }

    public List<UpdateDataCarrier> GetEditTransporterModel(DateTime from, DateTime to, long limit, long offset, bool isDevelopment) {
        List<UpdateDataCarrier> updateDataCarrierList = new();

        List<long> idsSalesHistoryModel = _connection.Query<long>(
            "SELECT COUNT(*) AS [TotalRowQty] " +
            "From [UpdateDataCarrier] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [UpdateDataCarrier].SaleId " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [CLient] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [UpdateDataCarrier].IsDevelopment = @IsDevelopment " +
            "AND [UpdateDataCarrier].Created >= @From " +
            "AND [UpdateDataCarrier].Created <= @To " +
            "AND [UpdateDataCarrier].Deleted = 0 " +
            "AND [UpdateDataCarrier].IsEditTransporter = 1 ",
            new {
                From = from,
                To = to,
                IsDevelopment = isDevelopment
            }
        ).ToList();

        string sqlExpression = ";WITH [Search_CTE] " +
                               "AS ( " +
                               "SELECT " +
                               "[UpdateDataCarrier].ID " +
                               "From [UpdateDataCarrier] " +
                               "LEFT JOIN [Sale] " +
                               "ON [Sale].ID = [UpdateDataCarrier].SaleId " +
                               "LEFT JOIN WarehousesShipment " +
                               "ON [WarehousesShipment].ID = [Sale].WarehousesShipmentID " +
                               "LEFT JOIN [Transporter] " +
                               "ON [Transporter].ID = [WarehousesShipment].TransporterId " +
                               "LEFT JOIN SaleNumber " +
                               "ON SaleNumber.ID = [Sale].SaleNumberID " +
                               "LEFT JOIN [ClientAgreement] " +
                               "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
                               "LEFT JOIN [CLient] " +
                               "ON [Client].ID = [ClientAgreement].ClientID " +
                               "WHERE [UpdateDataCarrier].IsDevelopment = @IsDevelopment " +
                               "AND [UpdateDataCarrier].Created >= @From " +
                               "AND [UpdateDataCarrier].Created <= @To " +
                               "AND [UpdateDataCarrier].Deleted = 0 " +
                               "AND [UpdateDataCarrier].IsEditTransporter = 1 " +
                               "), " +
                               "[Rowed_CTE] " +
                               "AS ( " +
                               "SELECT [Search_CTE].ID " +
                               ", ROW_NUMBER() OVER(ORDER BY [Search_CTE].ID DESC) AS [RowNumber] " +
                               "FROM [Search_CTE] " +
                               ") " +
                               "SELECT " +
                               "[UpdateDataCarrier].* , " +
                               "[UpdateDataCarrierTransporter].* , " +
                               "[UpdateDataCarrierUser].* , " +
                               "[Sale].* , " +
                               "[WarehousesShipment].* , " +
                               "[Transporter].* , " +
                               "[User].* , " +
                               "[SaleNumber].* , " +
                               "[ClientAgreement].* , " +
                               "[Client].* " +
                               "From [UpdateDataCarrier] " +
                               "LEFT JOIN [Transporter] as UpdateDataCarrierTransporter " +
                               "ON UpdateDataCarrierTransporter.ID = [UpdateDataCarrier].TransporterId " +
                               "LEFT JOIN [User] as UpdateDataCarrierUser " +
                               "ON UpdateDataCarrierUser.ID = [UpdateDataCarrier].UserId " +
                               "LEFT JOIN [Sale] " +
                               "ON [Sale].ID = [UpdateDataCarrier].SaleId " +
                               "LEFT JOIN WarehousesShipment " +
                               "ON [WarehousesShipment].ID = [Sale].WarehousesShipmentID " +
                               "LEFT JOIN [Transporter] " +
                               "ON [Transporter].ID = [WarehousesShipment].TransporterId " +
                               "LEFT JOIN [User] " +
                               "ON [User].ID = [WarehousesShipment].UserId " +
                               "LEFT JOIN SaleNumber " +
                               "ON SaleNumber.ID = [Sale].SaleNumberID " +
                               "LEFT JOIN [ClientAgreement] " +
                               "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
                               "LEFT JOIN [CLient] " +
                               "ON [Client].ID = [ClientAgreement].ClientID " +
                               "WHERE [UpdateDataCarrier].IsDevelopment = @IsDevelopment " +
                               "AND [UpdateDataCarrier].Created >= @From " +
                               "AND [UpdateDataCarrier].Created <= @To " +
                               "AND [UpdateDataCarrier].Deleted = 0 " +
                               "AND [UpdateDataCarrier].IsEditTransporter = 1 " +
                               "AND [UpdateDataCarrier].ID IN ( " +
                               "SELECT [Rowed_CTE].ID " +
                               "FROM [Rowed_CTE] " +
                               "WHERE [Rowed_CTE].RowNumber > @Offset " +
                               "AND [Rowed_CTE].RowNumber <= @Limit + @Offset " +
                               ") " +
                               "ORDER BY [UpdateDataCarrier].ID DESC; ";

        Type[] types = {
            typeof(UpdateDataCarrier),
            typeof(Transporter),
            typeof(User),
            typeof(Sale),
            typeof(WarehousesShipment),
            typeof(Transporter),
            typeof(User),
            typeof(SaleNumber),
            typeof(ClientAgreement),
            typeof(Client)
        };

        Func<object[], UpdateDataCarrier> mapper = objects => {
            UpdateDataCarrier updateDataCarrier = (UpdateDataCarrier)objects[0];
            Transporter UpdateDataCarrierTransporter = (Transporter)objects[1];
            User updateDataCarrierUser = (User)objects[2];
            Sale sale = (Sale)objects[3];
            WarehousesShipment warehousesShipment = (WarehousesShipment)objects[4];
            Transporter transporter = (Transporter)objects[5];
            User user = (User)objects[6];
            SaleNumber saleNumber = (SaleNumber)objects[7];
            ClientAgreement clientAgreement = (ClientAgreement)objects[8];
            Client client = (Client)objects[9];

            if (warehousesShipment != null && sale != null) {
                warehousesShipment.User = user;
                warehousesShipment.Transporter = transporter;
                sale.WarehousesShipment = warehousesShipment;
            }

            if (clientAgreement != null) {
                clientAgreement.Client = client;
                sale.ClientAgreement = clientAgreement;
            }

            if (sale != null) {
                sale.SaleNumber = saleNumber;
                updateDataCarrier.Sale = sale;
                updateDataCarrier.User = updateDataCarrierUser;
                updateDataCarrier.Transporter = UpdateDataCarrierTransporter;
            }

            updateDataCarrier.TotalRowsQty = idsSalesHistoryModel.Sum();
            updateDataCarrierList.Add(updateDataCarrier);

            return updateDataCarrier;
        };

        var props = new {
            From = from,
            To = to,
            Limit = limit,
            Offset = offset,
            IsDevelopment = isDevelopment
        };

        _connection.Query(sqlExpression, types, mapper, props);
        List<UpdateDataCarrier> groupedBySaleId = updateDataCarrierList
            .GroupBy(item => item.SaleId)
            .SelectMany(group => {
                UpdateDataCarrier oldestItem = group.OrderBy(item => item.Created).FirstOrDefault();

                return group.Select(item => {
                    item.ApproveUpdate = item == oldestItem;
                    return item;
                });
            })
            .ToList();
        return groupedBySaleId;
    }

    public List<HistoryInvoiceEdit> GetEditActForEditingModel(DateTime from, DateTime to, long limit, long offset, bool isDevelopment) {
        List<HistoryInvoiceEdit> histoyInvoiceEditList = new();

        List<long> idsSalesHistoryModel = _connection.Query<long>(
            "SELECT COUNT(*) AS [TotalRowQty] " +
            "FROM [HistoryInvoiceEdit] " +
            "LEFT JOIN [Sale] ON [Sale].ID = [HistoryInvoiceEdit].SaleId " +
            "LEFT JOIN SaleNumber ON SaleNumber.ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Client] ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [HistoryInvoiceEdit].IsDevelopment = @IsDevelopment " +
            "AND [HistoryInvoiceEdit].Created >= @From " +
            "AND [HistoryInvoiceEdit].Created <= @To " +
            "AND [HistoryInvoiceEdit].IsPrinted = 1 " +
            "AND [HistoryInvoiceEdit].Deleted = 0",
            new {
                From = from,
                To = to,
                IsDevelopment = isDevelopment
            }
        ).ToList();


        List<HistoryInvoiceEdit> salesHistoryModel = _connection.Query<HistoryInvoiceEdit, Sale, SaleNumber, ClientAgreement, Client, HistoryInvoiceEdit>(
            ";WITH [Search_CTE] " +
            "AS ( " +
            "Select " +
            "[HistoryInvoiceEdit].ID " +
            "From [HistoryInvoiceEdit] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [HistoryInvoiceEdit].SaleId " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [CLient] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [HistoryInvoiceEdit].IsDevelopment = @IsDevelopment " +
            "AND [HistoryInvoiceEdit].IsPrinted = 1   " +
            "AND [HistoryInvoiceEdit].Deleted = 0 " +
            "), " +
            "[Rowed_CTE] " +
            "AS ( " +
            "SELECT [Search_CTE].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [Search_CTE].ID DESC) AS [RowNumber] " +
            "FROM [Search_CTE] " +
            ") " +
            " " +
            "Select " +
            "[HistoryInvoiceEdit].* , " +
            "[Sale].* , " +
            "[SaleNumber].* , " +
            "[ClientAgreement].* , " +
            "[Client].* " +
            "FROM [HistoryInvoiceEdit] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [HistoryInvoiceEdit].SaleId " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [CLient] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [HistoryInvoiceEdit].IsDevelopment = @IsDevelopment " +
            "AND [HistoryInvoiceEdit].Created >= @From " +
            "AND [HistoryInvoiceEdit].Created <= @To " +
            "AND [HistoryInvoiceEdit].IsPrinted = 1 " +
            "AND [HistoryInvoiceEdit].Deleted = 0 " +
            "AND [HistoryInvoiceEdit].ID IN ( " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            "WHERE [Rowed_CTE].RowNumber > @Offset " +
            "AND [Rowed_CTE].RowNumber <= @Limit + @Offset " +
            ") " +
            "ORDER BY [HistoryInvoiceEdit].ID DESC;",
            (historyInvoiceEdit, sale, saleNumber, ClientAgreement, Client) => {
                if (ClientAgreement != null) {
                    ClientAgreement.Client = Client;
                    sale.ClientAgreement = ClientAgreement;
                }

                if (sale != null) {
                    sale.SaleNumber = saleNumber;
                    historyInvoiceEdit.Sale = sale;
                }

                historyInvoiceEdit.TotalRowsQty = idsSalesHistoryModel.Sum();
                histoyInvoiceEditList.Add(historyInvoiceEdit);
                return historyInvoiceEdit;
            },
            new {
                From = from,
                To = to,
                Limit = limit,
                Offset = offset,
                IsDevelopment = isDevelopment
            }
        ).ToList();
        List<HistoryInvoiceEdit> groupedBySaleId = histoyInvoiceEditList
            .GroupBy(item => item.SaleId)
            .SelectMany(group => {
                HistoryInvoiceEdit historyInvoiceEdit = group.OrderBy(item => item.Created).FirstOrDefault();

                return group.Select(item => {
                    item.ApproveUpdate = item == historyInvoiceEdit;
                    return item;
                });
            })
            .ToList();
        return histoyInvoiceEditList;
    }

    public List<SalesHistoryModel> GetSalesHistoryModel(DateTime from, DateTime to, long limit, long offset, bool isDevelopment) {
        List<long> idsSalesHistoryModel = _connection.Query<long>(
            "SELECT COUNT(*) AS [TotalRowQty] " +
            "From [UpdateDataCarrier] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [UpdateDataCarrier].SaleId " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [CLient] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [UpdateDataCarrier].IsDevelopment = @IsDevelopment " +
            "AND [UpdateDataCarrier].Created >= @From " +
            "AND [UpdateDataCarrier].Created <= @To " +
            "AND [UpdateDataCarrier].Deleted = 0 " +
            "UNION " +
            "SELECT COUNT(*) AS [TotalRowQty] " +
            "From [HistoryInvoiceEdit] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [HistoryInvoiceEdit].SaleId " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [CLient] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [HistoryInvoiceEdit].IsDevelopment = @IsDevelopment " +
            "AND [HistoryInvoiceEdit].Created >= @From " +
            "AND [HistoryInvoiceEdit].Created <= @To " +
            "AND [HistoryInvoiceEdit].IsPrinted = 1 " +
            "AND [HistoryInvoiceEdit].Deleted = 0 ",
            new {
                From = from,
                To = to,
                IsDevelopment = isDevelopment
            }
        ).ToList();


        List<SalesHistoryModel> salesHistoryModel = _connection.Query<SalesHistoryModel>(
            " ;WITH [Search_CTE] " +
            "AS ( " +
            "Select " +
            "[UpdateDataCarrier].ID " +
            "From [UpdateDataCarrier] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [UpdateDataCarrier].SaleId " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [CLient]  " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [UpdateDataCarrier].IsDevelopment = @IsDevelopment " +
            "AND [UpdateDataCarrier].Created >= @From " +
            "AND [UpdateDataCarrier].Created <= @To " +
            "AND [UpdateDataCarrier].Deleted = 0 " +
            "UNION " +
            "Select " +
            "[HistoryInvoiceEdit].ID " +
            "From [HistoryInvoiceEdit] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [HistoryInvoiceEdit].SaleId " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [CLient] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [HistoryInvoiceEdit].IsDevelopment = @IsDevelopment " +
            "AND [HistoryInvoiceEdit].Created >= @From " +
            "AND [HistoryInvoiceEdit].Created <= @To " +
            "AND [HistoryInvoiceEdit].Deleted = 0 " +
            "), " +
            "[Rowed_CTE] " +
            "AS ( " +
            "SELECT [Search_CTE].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [Search_CTE].ID DESC) AS [RowNumber] " +
            "FROM [Search_CTE] " +
            ") " +
            "Select " +
            "[UpdateDataCarrier].[IsDevelopment], " +
            "[UpdateDataCarrier].[Created], " +
            "[SaleNumber].Value AS [Number], " +
            "[Client].FullName, " +
            "[Client].OriginalRegionCode " +
            "From [UpdateDataCarrier] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [UpdateDataCarrier].SaleId " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [CLient] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [UpdateDataCarrier].ID IN ( " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            "WHERE [Rowed_CTE].RowNumber > @Offset " +
            "AND [Rowed_CTE].RowNumber <= @Limit + @Offset " +
            ") " +
            "UNION " +
            "Select " +
            "[HistoryInvoiceEdit].[IsDevelopment], " +
            "[HistoryInvoiceEdit].[Created], " +
            "[SaleNumber].Value AS [Number], " +
            "[Client].FullName, " +
            "[Client].OriginalRegionCode " +
            "FROM [HistoryInvoiceEdit] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [HistoryInvoiceEdit].SaleId " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [CLient] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [HistoryInvoiceEdit].ID IN ( " +
            "SELECT [Rowed_CTE].ID   " +
            "FROM [Rowed_CTE] " +
            "WHERE [Rowed_CTE].RowNumber > @Offset " +
            "AND [Rowed_CTE].RowNumber <= @Limit + @Offset " +
            ") ",
            new {
                From = from,
                To = to,
                Limit = limit,
                Offset = offset,
                IsDevelopment = isDevelopment
            }
        ).OrderByDescending(x => x.Created).ToList();


        if (salesHistoryModel.Any()) salesHistoryModel.FirstOrDefault().TotalRowsQty = idsSalesHistoryModel.Sum();
        return salesHistoryModel;
    }
}