using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.EntityHelpers.ClientModels;
using GBA.Domain.EntityHelpers.OrderItemModels;
using GBA.Domain.FilterEntities;
using GBA.Domain.Helpers;
using GBA.Domain.Messages.Clients;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using Newtonsoft.Json;

namespace GBA.Services.Actors.Clients.ClientsGetActors;

public sealed class BaseClientsGetActor : ReceiveActor {
    private readonly IClientOneCRepositoriesFactory _clientOneCRepositoriesFactory;
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public BaseClientsGetActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IClientOneCRepositoriesFactory clientOneCRepositoriesFactory,
        IXlsFactoryManager xlsFactoryManager) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _clientOneCRepositoriesFactory = clientOneCRepositoriesFactory;
        _xlsFactoryManager = xlsFactoryManager;

        Receive<GetAllClientsTotalAmountMessage>(ProcessGetAllClientsTotalAmountMessage);

        Receive<GetTotalDebtsByClientStructureMessage>(ProcessGetTotalDebtsByClientStructureMessage);

        Receive<GetTotalDebtsByClientStructureWithRootClientMessage>(ProcessGetTotalDebtsByClientStructureWithRootClientMessage);

        Receive<GetGroupedDebtsByClientNetIdMessage>(ProcessGetGroupedDebtsByClientNetIdMessage);

        Receive<GetAllSubClientClientAgreementsByClientNetIdMessage>(ProcessGetAllSubClientClientAgreementsByClientNetIdMessage);

        Receive<GetAllSubClientsByClientNetIdMessage>(ProcessGetAllSubClientsByClientNetIdMessage);

        Receive<GetAllClientSubClientsByClientNetIdMessage>(ProcessGetAllClientSubClientsByClientNetIdMessage);

        Receive<GetRootClientBySubClientNetIdMessage>(ProcessGetRootClientBySubClientNetIdMessage);

        Receive<GetClientByNetIdMessage>(ProcessGetClientByNetIdMessage);

        Receive<GetAllClientsFromSearchMessage>(ProcessGetAllClientsFromSearchMessage);

        Receive<GetAllClientsWithDebtByManagerNetIdMessage>(ProcessGetAllClientsWithDebtByManagerNetIdMessage);

        Receive<GetTopBySalesMessage>(ProcessGetTopBySalesMessage);

        Receive<GetTopByOnlineOrdersMessage>(ProcessGetTopByOnlineOrdersMessage);

        Receive<GetAvgByClientAndProductMessage>(ProcessGetAvgByClientAndProductMessage);

        Receive<GetDebtInfoMessage>(ProcessGetDebtInfoMessage);

        Receive<GetAllManufacturerClientsMessage>(ProcessGetAllManufacturerClientsMessage);

        Receive<GetAllFromSearchByServicePayersMessage>(ProcessGetAllFromSearchByServicePayersMessage);

        Receive<GetAllNewClientsRegisteredFromECommerceMessage>(ProcessGetAllNewClientsRegisteredFromECommerceMessage);

        Receive<IsSubClientsHasAgreementsMessage>(ProcessIsSubClientsHasAgreementsMessage);

        Receive<GetAllFromSearchByNameAndRegionCodeMessage>(ProcessGetAllFromSearchByNameAndRegionCodeMessage);

        Receive<GetAllClientsFromSearchBySalesMessage>(ProcessGetAllClientsFromSearchBySalesMessage);

        Receive<GetAllOrderItemsByClientMessage>(ProcessGetAllOrderItemsByClientMessage);

        Receive<GetAllShopClientsMessage>(ProcessGetAllShopClientsMessage);

        Receive<GetClientsWithPurchaseActivityMessage>(ProcessGetClientsWithPurchaseActivityMessage);

        Receive<GetClientsNotToBuyAnythingMessage>(ProcessGetClientsNotToBuyAnythingMessage);

        Receive<GetAllClientGroupsByClientNetIdMessage>(ProcessGetAllClientGroupsByClientNetIdMessage);

        Receive<GetWorkplacesByGroupNetIdMessage>(ProcessGetWorkplacesByGroupNetIdMessage);

        Receive<GetWorkplacesByMainClientNetIdMessage>(ProcessGetWorkplacesByMainClientNetIdMessage);

        Receive<GetAllClientsDocumentMessage>(ProcessGetAllClientsDocumentMessage);
    }

    private void ProcessGetAllClientsDocumentMessage(GetAllClientsDocumentMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            Filter filter = JsonConvert.DeserializeObject<Filter>(message.GetQuery.Filter);

            string booleanFilterSql = string.Empty;
            string roleTypeSql = string.Empty;

            if (!string.IsNullOrEmpty(message.GetQuery.BooleanFilter))
                booleanFilterSql =
                    SqlBuilder.GenerateAdditionalBoolSQLWhereStatement(JsonConvert.DeserializeObject<BooleanFilterItem>(message.GetQuery.BooleanFilter),
                        message.GetQuery.Table);
            if (!string.IsNullOrEmpty(message.GetQuery.TypeRoleFilter)) roleTypeSql = SqlBuilder.GenerateClientsSQLInStatement(message.GetQuery.TypeRoleFilter);

            List<Client> clients = _clientRepositoriesFactory
                .NewClientRepository(connection)
                .GetAllFiltered(
                    booleanFilterSql,
                    roleTypeSql
                );
            (string excelFilePath, string pdfFilePath) =
                _xlsFactoryManager
                    .NewClientXlsManager()
                    .ExportAllClientsToXls(
                        message.PathToFolder,
                        clients
                    );

            Sender.Tell(new Tuple<string, string>(excelFilePath, pdfFilePath));
        } catch (Exception) {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }

    private void ProcessGetWorkplacesByMainClientNetIdMessage(GetWorkplacesByMainClientNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory.NewWorkplaceRepository(connection).GetWorkplacesByMainClientNetId(message.NetId));
    }

    private void ProcessGetWorkplacesByGroupNetIdMessage(GetWorkplacesByGroupNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory.NewWorkplaceRepository(connection).GetWorkplacesByClientGroupNetId(message.NetId));
    }

    private void ProcessGetAllClientGroupsByClientNetIdMessage(GetAllClientGroupsByClientNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory.NewClientGroupRepository(connection).GetAllByClientNetId(message.ClientNetId));
    }

    private void ProcessGetAllClientsTotalAmountMessage(GetAllClientsTotalAmountMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory.NewClientRepository(connection).GetAllTotalAmount(message.Type));
    }

    private void ProcessGetTotalDebtsByClientStructureMessage(GetTotalDebtsByClientStructureMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell((object)_clientRepositoriesFactory.NewClientRepository(connection).GetDebtTotalsForClientStructureByClientNetId(message.NetId));
    }

    private void ProcessGetTotalDebtsByClientStructureWithRootClientMessage(GetTotalDebtsByClientStructureWithRootClientMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell((object)_clientRepositoriesFactory.NewClientRepository(connection).GetDebtTotalsForClientStructureWithRootByClientNetId(message.NetId));
    }

    private void ProcessGetGroupedDebtsByClientNetIdMessage(GetGroupedDebtsByClientNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory.NewClientInDebtRepository(connection).GetAllByClientIdGrouped(message.NetId));
    }

    private void ProcessGetAllSubClientClientAgreementsByClientNetIdMessage(GetAllSubClientClientAgreementsByClientNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory.NewClientAgreementRepository(connection).GetAllWithSubClientsByClientNetId(message.ClientNetId));
    }

    private void ProcessGetAllSubClientsByClientNetIdMessage(GetAllSubClientsByClientNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory
            .NewClientRepository(connection)
            .GetAllSubClients(message.ClientNetId)
        );
    }

    private void ProcessGetAllClientSubClientsByClientNetIdMessage(GetAllClientSubClientsByClientNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory
            .NewClientSubClientRepository(connection)
            .GetAllClientSubClients(message.ClientNetId)
        );
    }

    private void ProcessGetRootClientBySubClientNetIdMessage(GetRootClientBySubClientNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory
            .NewClientSubClientRepository(connection)
            .GetRootBySubClientNetId(message.SubClientNetId)
        );
    }

    private void ProcessGetClientByNetIdMessage(GetClientByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Client client = _clientRepositoriesFactory.NewClientRepository(connection).GetByNetId(message.NetId);

        foreach (ClientAgreement clientAgreement in client.ClientAgreements)
            clientAgreement.AccountBalance = _clientRepositoriesFactory.NewClientCashFlowRepository(connection)
                .GetAccountBalanceByClientAgreement(
                    clientAgreement.Id,
                    clientAgreement.Agreement.Currency != null && clientAgreement.Agreement.Currency.Code.ToUpper().Equals("EUR"));

        Sender.Tell(client);
    }

    private void ProcessGetAllClientsFromSearchMessage(GetAllClientsFromSearchMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory.NewClientRepository(connection).GetAllFromSearch(message.Value));
    }

    private void ProcessGetAllClientsWithDebtByManagerNetIdMessage(GetAllClientsWithDebtByManagerNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        User manager = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.NetId);

        Sender.Tell(
            manager == null
                ? new List<Client>()
                : _clientRepositoriesFactory.NewClientRepository(connection).GetAllByManagerId(manager.Id)
        );
    }

    private void ProcessGetTopBySalesMessage(GetTopBySalesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        List<dynamic> topClients = _clientRepositoriesFactory.NewClientRepository(connection).GetTopBySales();

        dynamic[] toReturnData = new dynamic[topClients.Count];

        for (int i = 0; i < topClients.Count; i++) {
            dynamic result = new ExpandoObject();

            result.RegionCode = topClients[i].RegionCode;
            result.TotalSales = topClients[i].TotalSales;
            result.TotalPrice = topClients[i].TotalPrice;
            result.Color = ChartColors.COLORS[i];

            toReturnData[i] = result;
        }


        Sender.Tell(toReturnData);
    }

    private void ProcessGetTopByOnlineOrdersMessage(GetTopByOnlineOrdersMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        List<dynamic> topClients = _clientRepositoriesFactory.NewClientRepository(connection).GetTopByOnlineOrders();

        dynamic[] toReturnData = new dynamic[topClients.Count];

        for (int i = 0; i < topClients.Count; i++) {
            dynamic result = new ExpandoObject();

            result.RegionCode = topClients[i].RegionCode;
            result.TotalOrders = topClients[i].TotalOrders;
            result.TotalPrice = topClients[i].TotalPrice;
            result.Color = ChartColors.COLORS[i];

            toReturnData[i] = result;
        }


        Sender.Tell(toReturnData);
    }

    private void ProcessGetAvgByClientAndProductMessage(GetAvgByClientAndProductMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell((object)_clientRepositoriesFactory.NewClientRepository(connection).GetAvgByClientAndProduct(message.ClientNetId, message.ProductNetId));
    }

    private void ProcessIsSubClientsHasAgreementsMessage(IsSubClientsHasAgreementsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory
            .NewClientAgreementRepository(connection)
            .IsSubClientsHasAgreements(message.NetId)
        );
    }

    private void ProcessGetDebtInfoMessage(GetDebtInfoMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell((object)_clientRepositoriesFactory
            .NewClientInDebtRepository(connection)
            .GetDebtInfo(message.ClientNetId)
        );
    }

    private void ProcessGetAllManufacturerClientsMessage(GetAllManufacturerClientsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory
            .NewClientRepository(connection)
            .GetAllManufacturerClients()
        );
    }

    private void ProcessGetAllFromSearchByServicePayersMessage(GetAllFromSearchByServicePayersMessage message) {
        if (message.Limit <= 0) message.Limit = 20;
        if (message.Offset < 0) message.Offset = 0;
        if (string.IsNullOrEmpty(message.Value)) message.Value = string.Empty;

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _clientRepositoriesFactory
                .NewClientRepository(connection)
                .GetAllFromSearchByServicePayers(message.Value, message.Limit, message.Offset)
        );
    }

    private void ProcessGetAllNewClientsRegisteredFromECommerceMessage(GetAllNewClientsRegisteredFromECommerceMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory.NewClientRepository(connection).GetAllNewClientsFromECommerce());
    }

    private void ProcessGetAllFromSearchByNameAndRegionCodeMessage(GetAllFromSearchByNameAndRegionCodeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _clientRepositoriesFactory
                .NewClientRepository(connection)
                .GetAllFromSearchByNameOrRegionCode(
                    message.Value
                )
        );
    }

    private void ProcessGetAllClientsFromSearchBySalesMessage(GetAllClientsFromSearchBySalesMessage message) {
        if (string.IsNullOrEmpty(message.SearchValue)) {
            Sender.Tell(Enumerable.Empty<Client>());
            return;
        }

        using IDbConnection oneCConnection = _connectionFactory.NewFenixOneCSqlConnection();
        IEnumerable<long> clientOldECommerceIds =
            _clientOneCRepositoriesFactory
                .NewClientOneCRepository(oneCConnection)
                .GetOldEcommerceIdsFromSearchBySales(
                    message.SearchValue
                );

        if (!clientOldECommerceIds.Any()) {
            Sender.Tell(Enumerable.Empty<Client>());
            return;
        }

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _clientRepositoriesFactory
                .NewClientRepository(connection)
                .GetByOldEcommerceIds(
                    clientOldECommerceIds
                )
        );
    }

    private void ProcessGetAllOrderItemsByClientMessage(GetAllOrderItemsByClientMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            List<OrderItemByClientModel> orderItems = _clientRepositoriesFactory
                .NewClientRepository(connection)
                .GetOrderItemsByClientNetId(message.ClientNetId);

            if (!orderItems.Any()) {
                Sender.Tell(Enumerable.Empty<OrderItemByClientModel>());
                return;
            }

            Sender.Tell(orderItems);
        } catch (Exception) {
            Sender.Tell(Enumerable.Empty<OrderItemByClientModel>());
        }
    }

    private void ProcessGetAllShopClientsMessage(GetAllShopClientsMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            List<Client> clients =
                _clientRepositoriesFactory
                    .NewClientRepository(connection)
                    .GetAllShopClients();

            Sender.Tell(clients);
        } catch (Exception) {
            Sender.Tell(Enumerable.Empty<ClientWithPurchaseActivityModel>());
        }
    }

    private void ProcessGetClientsWithPurchaseActivityMessage(GetClientsWithPurchaseActivityMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            long userId = _userRepositoriesFactory.NewUserRepository(connection).GetUserIdByNetId(message.UserNetId);

            List<ClientWithPurchaseActivityModel> clients =
                _clientRepositoriesFactory
                    .NewClientRepository(connection)
                    .GetAllWithPurchaseActivity(message.Limit, message.Offset, message.ForMyClients, userId);

            Sender.Tell(clients);
        } catch (Exception) {
            Sender.Tell(Enumerable.Empty<ClientWithPurchaseActivityModel>());
        }
    }

    private void ProcessGetClientsNotToBuyAnythingMessage(GetClientsNotToBuyAnythingMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            List<Client> clients = _clientRepositoriesFactory.NewClientRepository(connection).GetClientsNotToBuyAnything(message.From, message.To, message.Value);

            Sender.Tell(clients);
        } catch (Exception) {
            Sender.Tell(new List<Client>());
        }
    }
}