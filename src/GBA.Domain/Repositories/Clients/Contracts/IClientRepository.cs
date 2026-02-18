using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Clients;
using GBA.Domain.EntityHelpers.ClientModels;
using GBA.Domain.EntityHelpers.OrderItemModels;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientRepository {
    long Add(Client client);
    Client GetEmail(string email);
    Client GetEmailDeleted(string email);

    Client GetOriginalRegionCode(string regionCode);

    long GetAllTotalAmount(ClientTypeType type);

    long GetFilteredCount(string sql, string value);

    void Update(Client client);

    void UpdateAbbreviation(Client client);

    void SetTemporaryClientById(long id);

    long DeselectIsForRetailByNetId(Guid netId);

    void Remove(Guid netId);

    void Remove(long id);

    void SetWorkplaceStatus(long id);

    Guid GetClientNetIdByRegionCode(string value);

    Guid GetRootNetIdBySubClientNetId(Guid netId);

    Client GetClientNetIdByMobileNumber(string value);

    Client GetById(long id);

    Client GetRootClientBySubClientNetId(Guid netId);

    Client GetByIdWithoutIncludes(long id);

    Client GetByNetIdWithoutIncludes(Guid netId);

    Client GetByNetId(Guid netId, bool isFromEcommerce = false);

    Client GetByIdWithAllIncludes(long id);

    Client GetByNetIdWithRoleAndType(Guid netId);

    Client GetByIdWithRegionCode(long id);

    Client GetByNetIdWithRegionCode(Guid netId);

    Client GetRetailClient();

    Client SearchClientByMobileNumber(string mobileNumber);

    List<Client> GetAllNewClientsFromECommerce();

    List<Client> GetAllSubClients(Guid clientNetId);

    List<Client> GetAllManufacturerClients();

    List<Client> GetAll(long offset, long limit);

    List<Client> GetAllByManagerId(long managerId);

    List<Client> GetAllRootClientsWithoutIncludes();

    List<Client> GetAllForExport(
        string orderBy,
        string booleanFilter,
        string roleTypeFilter,
        bool searchForClients = true,
        bool? forReSale = null);

    List<Client> GetAllFromSearch(
        long limit,
        long offset,
        string value,
        string orderBy,
        string booleanFilter,
        string roleTypeFilter,
        bool searchForClients = true,
        bool? forReSale = null);

    List<Client> GetAllFiltered(string booleanFilter, string roleTypeFilter);

    List<Client> GetAllShopClients();

    List<dynamic> GetTopBySales();

    List<dynamic> GetTopByOnlineOrders();

    List<Client> GetAllFromSearchByServicePayers(string value, long limit, long offset);

    List<Client> GetAllFromSearchWithDebtInfo(string value, bool allClients, Guid userNetId);

    IEnumerable<Client> GetAllFromSearch(string value);

    IEnumerable<Client> GetAllFromSearchByNameOrRegionCode(string value);

    IEnumerable<Client> GetByOldEcommerceIds(IEnumerable<long> oldEcommerceIds);

    dynamic GetAvgByClientAndProduct(Guid clientNetId, Guid productNetId);

    dynamic GetDebtTotalsForClientStructureByClientNetId(Guid netId);

    dynamic GetDebtTotalsForClientStructureWithRootByClientNetId(Guid netId, bool isFromEcommerce = false);

    List<OrderItemByClientModel> GetOrderItemsByClientNetId(Guid clientNetId);

    List<ClientWithPurchaseActivityModel> GetAllWithPurchaseActivity(long limit, long offset, bool forMyClients, long userId);

    long GetIdByNetId(Guid clientNetId);

    long SetIsForRetailByNetId(Guid netId);

    List<Client> GetClientsNotToBuyAnything(DateTime from, DateTime to, string value);

    void UpdateNumbers(Client client);

    void UpdateOrderExpireDays(Guid clientNetId, long expireDays);

    void UpdateOrderExpireDaysByType(Guid typeNetId, long expireDays);
}