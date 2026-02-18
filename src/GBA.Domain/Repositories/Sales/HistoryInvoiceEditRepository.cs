using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class HistoryInvoiceEditRepository : IHistoryInvoiceEditRepository {
    private readonly IDbConnection _connection;

    public HistoryInvoiceEditRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(HistoryInvoiceEdit HistoryInvoiceEdit) {
        return _connection.Query<long>(
            "INSERT INTO HistoryInvoiceEdit ( SaleId, ApproveUpdate, IsDevelopment, IsPrinted, Updated) " +
            "VALUES ( @SaleId, @ApproveUpdate, @IsDevelopment, @IsPrinted, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            HistoryInvoiceEdit
        ).Single();
    }

    public List<HistoryInvoiceEdit> GetByIdSale(long saleId) {
        List<HistoryInvoiceEdit> toReturn = new();
        Type[] types = {
            typeof(HistoryInvoiceEdit),
            typeof(OrderItemBaseShiftStatus)
        };

        Func<object[], HistoryInvoiceEdit> mapper = objects => {
            HistoryInvoiceEdit historyInvoice = (HistoryInvoiceEdit)objects[0];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[2];

            if (!toReturn.Any(p => p.Id.Equals(historyInvoice.Id))) {
                historyInvoice.OrderItemBaseShiftStatuses.Add(orderItemBaseShiftStatus);
                toReturn.Add(historyInvoice);
            } else {
                HistoryInvoiceEdit historyInvoiceEditFromList = toReturn.First(s => s.Id.Equals(historyInvoice.Id));

                historyInvoiceEditFromList.OrderItemBaseShiftStatuses.Add(orderItemBaseShiftStatus);
            }

            return historyInvoice;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [HistoryInvoiceEdit] " +
            "Left Join [OrderItemBaseShiftStatus] " +
            "ON [OrderItemBaseShiftStatus].HistoryInvoiceEditId = HistoryInvoiceEdit.ID " +
            "Where HistoryInvoiceEdit.SaleID = @saleID " +
            "And HistoryInvoiceEdit.Deleted = 0 ",
            types,
            mapper,
            new { saleID = saleId }
        );

        return toReturn;
    }

    public void UpdateIsDevelopment(Guid netId) {
        _connection.Execute(
            "UPDATE [HistoryInvoiceEdit] " +
            "SET IsDevelopment = 1, Updated = getutcdate() " +
            "WHERE [HistoryInvoiceEdit].[NetUID] = @NetId",
            new { NetId = netId });
    }

    public HistoryInvoiceEdit GetByNetId(Guid netId) {
        HistoryInvoiceEdit historyInvoiceEdit = new();
        Type[] typesHistoryInvoiceEdit = {
            typeof(HistoryInvoiceEdit),
            typeof(OrderItemBaseShiftStatus),
            typeof(User),
            typeof(Sale)
        };

        Func<object[], HistoryInvoiceEdit> mapperHistoryInvoiceEdit = objects => {
            HistoryInvoiceEdit historyInvoice = (HistoryInvoiceEdit)objects[0];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[1];
            User user = (User)objects[2];
            Sale sale = (Sale)objects[3];

            historyInvoice.Sale = sale;
            orderItemBaseShiftStatus.User = user;
            if (historyInvoiceEdit.Id.Equals(historyInvoice.Id)) {
                historyInvoiceEdit.OrderItemBaseShiftStatuses.Add(orderItemBaseShiftStatus);
            } else {
                historyInvoiceEdit = historyInvoice;

                historyInvoiceEdit.OrderItemBaseShiftStatuses.Add(orderItemBaseShiftStatus);
            }

            return historyInvoice;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [HistoryInvoiceEdit] " +
            "LEFT JOIN [OrderItemBaseShiftStatus] " +
            "ON [OrderItemBaseShiftStatus].HistoryInvoiceEditId = HistoryInvoiceEdit.ID " +
            "LEFT JOIN [User] " +
            "ON [User].Id = OrderItemBaseShiftStatus.UserId " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].Id = [HistoryInvoiceEdit].SaleId " +
            "WHERE HistoryInvoiceEdit.NetUID = @NetUId " +
            "AND HistoryInvoiceEdit.Deleted = 0 ",
            typesHistoryInvoiceEdit,
            mapperHistoryInvoiceEdit,
            new { NetUId = netId }
        );
        return historyInvoiceEdit;
    }

    public HistoryInvoiceEdit GetById(long Id) {
        HistoryInvoiceEdit historyInvoiceEdit = new();
        Type[] typesHistoryInvoiceEdit = {
            typeof(HistoryInvoiceEdit),
            typeof(OrderItemBaseShiftStatus),
            typeof(User),
            typeof(Sale)
        };

        Func<object[], HistoryInvoiceEdit> mapperHistoryInvoiceEdit = objects => {
            HistoryInvoiceEdit historyInvoice = (HistoryInvoiceEdit)objects[0];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[1];
            User user = (User)objects[2];
            Sale sale = (Sale)objects[3];

            historyInvoice.Sale = sale;
            orderItemBaseShiftStatus.User = user;
            if (historyInvoiceEdit.Id.Equals(historyInvoice.Id)) {
                historyInvoiceEdit.OrderItemBaseShiftStatuses.Add(orderItemBaseShiftStatus);
            } else {
                historyInvoiceEdit = historyInvoice;

                historyInvoiceEdit.OrderItemBaseShiftStatuses.Add(orderItemBaseShiftStatus);
            }

            return historyInvoice;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [HistoryInvoiceEdit] " +
            "LEFT JOIN [OrderItemBaseShiftStatus] " +
            "ON [OrderItemBaseShiftStatus].HistoryInvoiceEditId = HistoryInvoiceEdit.ID " +
            "LEFT JOIN [User] " +
            "ON [User].Id = OrderItemBaseShiftStatus.UserId " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].Id = [HistoryInvoiceEdit].SaleId " +
            "WHERE HistoryInvoiceEdit.ID = @Id " +
            "AND HistoryInvoiceEdit.Deleted = 0 ",
            typesHistoryInvoiceEdit,
            mapperHistoryInvoiceEdit,
            new {
                Id
            }
        );
        return historyInvoiceEdit;
    }

    public void UpdateApproveUpdateFalse(Guid netId) {
        _connection.Execute(
            "UPDATE [HistoryInvoiceEdit] " +
            "SET ApproveUpdate = 0, Updated = getutcdate() " +
            "WHERE [HistoryInvoiceEdit].[NetUID] = @NetId",
            new { NetId = netId });
    }

    public void UpdateApproveUpdate(Guid netId) {
        _connection.Execute(
            "UPDATE [HistoryInvoiceEdit] " +
            "SET ApproveUpdate = 1, Updated = getutcdate() " +
            "WHERE [HistoryInvoiceEdit].[NetUID] = @NetId",
            new { NetId = netId });
    }
}