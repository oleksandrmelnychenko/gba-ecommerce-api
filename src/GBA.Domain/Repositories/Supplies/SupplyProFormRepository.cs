using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies;

public sealed class SupplyProFormRepository : ISupplyProFormRepository {
    private readonly IDbConnection _connection;

    public SupplyProFormRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SupplyProForm supplyProform) {
        return _connection.Query<long>(
                "INSERT INTO SupplyProForm (NetPrice, Number, IsSkipped, DateFrom, ServiceNumber, Updated) " +
                "VALUES(@NetPrice, @Number, @IsSkipped, @DateFrom, @ServiceNumber, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                supplyProform
            )
            .Single();
    }

    public void Update(SupplyProForm supplyProForm) {
        _connection.Execute(
            "UPDATE SupplyProForm " +
            "SET NetPrice = @NetPrice, Number = @Number, IsSkipped = @IsSkipped, DateFrom = @DateFrom, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            supplyProForm
        );
    }

    public List<SupplyProForm> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId) {
        List<SupplyProForm> toReturn = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [SupplyProForm].ID DESC) AS RowNumber " +
            ", [SupplyProForm].ID " +
            "FROM [SupplyProForm] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyProForm].ID = [SupplyOrder].SupplyProFormID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [Client] " +
            "ON [SupplyOrder].ClientID = [Client].ID " +
            "WHERE [SupplyProForm].Deleted = 0 " +
            "AND [SupplyProForm].Created >= @From " +
            "AND [SupplyProForm].Created <= @To " +
            "AND [SupplyProForm].Number like '%' + @Value + '%' ";

        if (clientNetId.HasValue) sqlExpression += "AND [Client].NetUID = @ClientNetId ";

        sqlExpression +=
            ")" +
            "SELECT * " +
            "FROM [SupplyProForm] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyProForm].ID = [SupplyOrder].SupplyProFormID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [Client] " +
            "ON [SupplyOrder].ClientID = [Client].ID " +
            "WHERE [SupplyProForm].ID IN (" +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ")";

        Type[] types = {
            typeof(SupplyProForm),
            typeof(SupplyOrder),
            typeof(SupplyOrderNumber),
            typeof(Client)
        };

        Func<object[], SupplyProForm> mapper = objects => {
            SupplyProForm supplyProForm = (SupplyProForm)objects[0];
            SupplyOrder supplyOrder = (SupplyOrder)objects[1];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[2];
            Client client = (Client)objects[3];

            if (!toReturn.Any(p => p.Id.Equals(supplyProForm))) {
                if (supplyOrder != null) {
                    supplyOrder.Client = client;
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;

                    supplyProForm.SupplyOrders.Add(supplyOrder);
                }

                toReturn.Add(supplyProForm);
            }

            return supplyProForm;
        };

        var props = new { Value = value, Limit = limit, Offset = offset, From = from, To = to, ClientNetId = clientNetId };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            props
        );

        return toReturn;
    }

    public SupplyProForm GetByNetIdWithAllInclueds(Guid netId) {
        SupplyProForm supplyProFormToReturn = null;

        string sqlStatement = "SELECT * FROM SupplyProForm " +
                              "LEFT OUTER JOIN ProFormDocument " +
                              "ON ProFormDocument.SupplyProFormID = SupplyProForm.ID " +
                              "AND ProFormDocument.Deleted = 0 " +
                              "LEFT JOIN SupplyOrderPaymentDeliveryProtocol " +
                              "ON SupplyOrderPaymentDeliveryProtocol.SupplyProFormID = SupplyProForm.ID " +
                              "AND SupplyOrderPaymentDeliveryProtocol.Deleted = 0 " +
                              "LEFT JOIN SupplyOrderPaymentDeliveryProtocolKey " +
                              "ON SupplyOrderPaymentDeliveryProtocolKey.ID = SupplyOrderPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKeyID " +
                              "LEFT JOIN SupplyPaymentTask " +
                              "ON SupplyPaymentTask.ID = SupplyOrderPaymentDeliveryProtocol.SupplyPaymentTaskID " +
                              "LEFT JOIN [User] AS [PaymentProtocolUser] " +
                              "ON [PaymentProtocolUser].ID = SupplyOrderPaymentDeliveryProtocol.UserID " +
                              "LEFT JOIN [User] AS [SupplyPaymentTaskUser] " +
                              "ON [SupplyPaymentTaskUser].ID = SupplyPaymentTask.UserID " +
                              "LEFT JOIN SupplyInformationDeliveryProtocol " +
                              "ON SupplyInformationDeliveryProtocol.SupplyProFormID = SupplyProForm.ID " +
                              "LEFT JOIN SupplyInformationDeliveryProtocolKey " +
                              "ON SupplyInformationDeliveryProtocolKey.ID = SupplyInformationDeliveryProtocol.SupplyInformationDeliveryProtocolKeyID " +
                              "LEFT JOIN [User] AS [InformationProtocolUser] " +
                              "ON [InformationProtocolUser].ID = SupplyInformationDeliveryProtocol.UserID " +
                              "WHERE SupplyProForm.NetUID = @NetId";

        Type[] types = {
            typeof(SupplyProForm),
            typeof(ProFormDocument),
            typeof(SupplyOrderPaymentDeliveryProtocol),
            typeof(SupplyOrderPaymentDeliveryProtocolKey),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(User),
            typeof(SupplyInformationDeliveryProtocol),
            typeof(SupplyInformationDeliveryProtocolKey),
            typeof(User)
        };

        Func<object[], SupplyProForm> mapper = objects => {
            SupplyProForm supplyProForm = (SupplyProForm)objects[0];
            ProFormDocument proFormDocument = (ProFormDocument)objects[1];
            SupplyOrderPaymentDeliveryProtocol paymentProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[2];
            SupplyOrderPaymentDeliveryProtocolKey paymentProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[3];
            SupplyPaymentTask paymentProtocolSupplyPaymentTask = (SupplyPaymentTask)objects[4];
            User paymentProtocolUser = (User)objects[5];
            User paymentProtocolSupplyPaymentTaskUser = (User)objects[6];
            SupplyInformationDeliveryProtocol informationProtocol = (SupplyInformationDeliveryProtocol)objects[7];
            SupplyInformationDeliveryProtocolKey informationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[8];
            User informationProtocolUser = (User)objects[9];

            if (supplyProFormToReturn != null) {
                if (proFormDocument != null && !supplyProFormToReturn.ProFormDocuments.Any(d => d.Id.Equals(proFormDocument.Id)))
                    supplyProFormToReturn.ProFormDocuments.Add(proFormDocument);
                if (paymentProtocol != null && !supplyProFormToReturn.PaymentDeliveryProtocols.Any(p => p.Id.Equals(paymentProtocol.Id))) {
                    if (paymentProtocolSupplyPaymentTask != null) {
                        paymentProtocolSupplyPaymentTask.User = paymentProtocolSupplyPaymentTaskUser;

                        paymentProtocol.SupplyPaymentTask = paymentProtocolSupplyPaymentTask;
                    }

                    paymentProtocol.SupplyOrderPaymentDeliveryProtocolKey = paymentProtocolKey;
                    paymentProtocol.User = paymentProtocolUser;

                    supplyProFormToReturn.PaymentDeliveryProtocols.Add(paymentProtocol);
                }

                if (informationProtocol != null && !supplyProFormToReturn.InformationDeliveryProtocols.Any(p => p.Id.Equals(informationProtocol.Id))) {
                    informationProtocol.SupplyInformationDeliveryProtocolKey = informationProtocolKey;
                    informationProtocol.User = informationProtocolUser;

                    supplyProFormToReturn.InformationDeliveryProtocols.Add(informationProtocol);
                }
            } else {
                if (proFormDocument != null) supplyProForm.ProFormDocuments.Add(proFormDocument);
                if (paymentProtocol != null) {
                    if (paymentProtocolSupplyPaymentTask != null) {
                        paymentProtocolSupplyPaymentTask.User = paymentProtocolSupplyPaymentTaskUser;

                        paymentProtocol.SupplyPaymentTask = paymentProtocolSupplyPaymentTask;
                    }

                    paymentProtocol.SupplyOrderPaymentDeliveryProtocolKey = paymentProtocolKey;
                    paymentProtocol.User = paymentProtocolUser;

                    supplyProForm.PaymentDeliveryProtocols.Add(paymentProtocol);
                }

                if (informationProtocol != null) {
                    informationProtocol.SupplyInformationDeliveryProtocolKey = informationProtocolKey;
                    informationProtocol.User = informationProtocolUser;

                    supplyProForm.InformationDeliveryProtocols.Add(informationProtocol);
                }

                supplyProFormToReturn = supplyProForm;
            }

            return supplyProForm;
        };

        var props = new { NetId = netId };

        _connection.Query(sqlStatement, types, mapper, props);

        return supplyProFormToReturn;
    }

    public SupplyProForm GetByIdWithAllInclueds(long id) {
        SupplyProForm supplyProFormToReturn = null;

        string sqlStatement = "SELECT * FROM SupplyProForm " +
                              "LEFT OUTER JOIN ProFormDocument " +
                              "ON ProFormDocument.SupplyProFormID = SupplyProForm.ID " +
                              "AND ProFormDocument.Deleted = 0 " +
                              "LEFT JOIN SupplyOrderPaymentDeliveryProtocol " +
                              "ON SupplyOrderPaymentDeliveryProtocol.SupplyProFormID = SupplyProForm.ID " +
                              "AND SupplyOrderPaymentDeliveryProtocol.Deleted = 0 " +
                              "LEFT JOIN SupplyOrderPaymentDeliveryProtocolKey " +
                              "ON SupplyOrderPaymentDeliveryProtocolKey.ID = SupplyOrderPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKeyID " +
                              "LEFT JOIN SupplyPaymentTask " +
                              "ON SupplyPaymentTask.ID = SupplyOrderPaymentDeliveryProtocol.SupplyPaymentTaskID " +
                              "LEFT JOIN [User] AS [PaymentProtocolUser] " +
                              "ON [PaymentProtocolUser].ID = SupplyOrderPaymentDeliveryProtocol.UserID " +
                              "LEFT JOIN [User] AS [SupplyPaymentTaskUser] " +
                              "ON [SupplyPaymentTaskUser].ID = SupplyPaymentTask.UserID " +
                              "LEFT JOIN SupplyInformationDeliveryProtocol " +
                              "ON SupplyInformationDeliveryProtocol.SupplyProFormID = SupplyProForm.ID " +
                              "LEFT JOIN SupplyInformationDeliveryProtocolKey " +
                              "ON SupplyInformationDeliveryProtocolKey.ID = SupplyInformationDeliveryProtocol.SupplyInformationDeliveryProtocolKeyID " +
                              "LEFT JOIN [User] AS [InformationProtocolUser] " +
                              "ON [InformationProtocolUser].ID = SupplyInformationDeliveryProtocol.UserID " +
                              "WHERE SupplyProForm.ID = @Id";

        Type[] types = {
            typeof(SupplyProForm),
            typeof(ProFormDocument),
            typeof(SupplyOrderPaymentDeliveryProtocol),
            typeof(SupplyOrderPaymentDeliveryProtocolKey),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(User),
            typeof(SupplyInformationDeliveryProtocol),
            typeof(SupplyInformationDeliveryProtocolKey),
            typeof(User)
        };

        Func<object[], SupplyProForm> mapper = objects => {
            SupplyProForm supplyProForm = (SupplyProForm)objects[0];
            ProFormDocument proFormDocument = (ProFormDocument)objects[1];
            SupplyOrderPaymentDeliveryProtocol paymentProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[2];
            SupplyOrderPaymentDeliveryProtocolKey paymentProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[3];
            SupplyPaymentTask paymentProtocolSupplyPaymentTask = (SupplyPaymentTask)objects[4];
            User paymentProtocolUser = (User)objects[5];
            User paymentProtocolSupplyPaymentTaskUser = (User)objects[6];
            SupplyInformationDeliveryProtocol informationProtocol = (SupplyInformationDeliveryProtocol)objects[7];
            SupplyInformationDeliveryProtocolKey informationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[8];
            User informationProtocolUser = (User)objects[9];

            if (supplyProFormToReturn != null) {
                if (proFormDocument != null && !supplyProFormToReturn.ProFormDocuments.Any(d => d.Id.Equals(proFormDocument.Id)))
                    supplyProFormToReturn.ProFormDocuments.Add(proFormDocument);
                if (paymentProtocol != null && !supplyProFormToReturn.PaymentDeliveryProtocols.Any(p => p.Id.Equals(paymentProtocol.Id))) {
                    if (paymentProtocolSupplyPaymentTask != null) {
                        paymentProtocolSupplyPaymentTask.User = paymentProtocolSupplyPaymentTaskUser;

                        paymentProtocol.SupplyPaymentTask = paymentProtocolSupplyPaymentTask;
                    }

                    paymentProtocol.SupplyOrderPaymentDeliveryProtocolKey = paymentProtocolKey;
                    paymentProtocol.User = paymentProtocolUser;

                    supplyProFormToReturn.PaymentDeliveryProtocols.Add(paymentProtocol);
                }

                if (informationProtocol != null && !supplyProFormToReturn.InformationDeliveryProtocols.Any(p => p.Id.Equals(informationProtocol.Id))) {
                    informationProtocol.SupplyInformationDeliveryProtocolKey = informationProtocolKey;
                    informationProtocol.User = informationProtocolUser;

                    supplyProFormToReturn.InformationDeliveryProtocols.Add(informationProtocol);
                }
            } else {
                if (proFormDocument != null) supplyProForm.ProFormDocuments.Add(proFormDocument);
                if (paymentProtocol != null) {
                    if (paymentProtocolSupplyPaymentTask != null) {
                        paymentProtocolSupplyPaymentTask.User = paymentProtocolSupplyPaymentTaskUser;

                        paymentProtocol.SupplyPaymentTask = paymentProtocolSupplyPaymentTask;
                    }

                    paymentProtocol.SupplyOrderPaymentDeliveryProtocolKey = paymentProtocolKey;
                    paymentProtocol.User = paymentProtocolUser;

                    supplyProForm.PaymentDeliveryProtocols.Add(paymentProtocol);
                }

                if (informationProtocol != null) {
                    informationProtocol.SupplyInformationDeliveryProtocolKey = informationProtocolKey;
                    informationProtocol.User = informationProtocolUser;

                    supplyProForm.InformationDeliveryProtocols.Add(informationProtocol);
                }

                supplyProFormToReturn = supplyProForm;
            }

            return supplyProForm;
        };

        var props = new { Id = id };

        _connection.Query(sqlStatement, types, mapper, props);

        return supplyProFormToReturn;
    }

    public SupplyProForm GetByNetId(Guid netId) {
        SupplyProForm supplyProFormToReturn = null;

        _connection.Query<SupplyProForm, ProFormDocument, SupplyProForm>(
            "SELECT * FROM SupplyProForm " +
            "LEFT OUTER JOIN ProFormDocument " +
            "ON ProFormDocument.SupplyProFormID = SupplyProForm.ID " +
            "AND ProFormDocument.Deleted = 0 " +
            "WHERE SupplyProForm.NetUID = @NetId",
            (supplyProForm, proFormDocument) => {
                if (proFormDocument != null) supplyProForm.ProFormDocuments.Add(proFormDocument);

                if (supplyProFormToReturn != null) {
                    if (!supplyProFormToReturn.ProFormDocuments.Any(d => d.Id.Equals(proFormDocument.Id))) supplyProFormToReturn.ProFormDocuments.Add(proFormDocument);
                } else {
                    supplyProFormToReturn = supplyProForm;
                }

                return supplyProForm;
            },
            new { NetId = netId }
        );

        return supplyProFormToReturn;
    }

    public SupplyProForm GetByIdWithoutIncludes(long id) {
        return _connection.Query<SupplyProForm>(
                "SELECT * " +
                "FROM [SupplyProForm] " +
                "WHERE [SupplyProForm].ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public SupplyProForm GetById(long id) {
        SupplyProForm supplyProFormToReturn = null;

        _connection.Query<SupplyProForm, ProFormDocument, SupplyProForm>(
            "SELECT * FROM SupplyProForm " +
            "LEFT OUTER JOIN ProFormDocument " +
            "ON ProFormDocument.SupplyProFormID = SupplyProForm.ID AND ProFormDocument.Deleted = 0 " +
            "WHERE SupplyProForm.ID = @Id",
            (supplyProForm, proFormDocument) => {
                if (proFormDocument != null) supplyProForm.ProFormDocuments.Add(proFormDocument);

                if (supplyProFormToReturn != null) {
                    if (!supplyProFormToReturn.ProFormDocuments.Any(d => d.Id.Equals(proFormDocument.Id))) supplyProFormToReturn.ProFormDocuments.Add(proFormDocument);
                } else {
                    supplyProFormToReturn = supplyProForm;
                }

                return supplyProForm;
            },
            new { Id = id }
        );

        return supplyProFormToReturn;
    }
}