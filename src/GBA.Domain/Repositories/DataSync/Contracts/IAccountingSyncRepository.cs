using System;
using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.DataSync.Contracts;

public interface IAccountingSyncRepository {
    IEnumerable<Currency> GetAllCurrencies();

    void Update(Currency currency);

    IEnumerable<SyncExchangeRate> GetAllSyncExchangeRates();

    IEnumerable<SyncExchangeRate> GetAmgAllSyncExchangeRates();

    IEnumerable<SyncCrossExchangeRate> GetAllSyncCrossExchangeRates();

    IEnumerable<SyncCrossExchangeRate> GetAmgAllSyncCrossExchangeRates();

    IEnumerable<ExchangeRate> GetAllUahExchangeRates();

    IEnumerable<GovExchangeRate> GetAllGovExchangeRates();

    void CleanExchangeRateHistory();

    void CleanGovExchangeRateHistory();

    void CleanCrossExchangeRateHistory();

    void CleanGovCrossExchangeRateHistory();

    void Add(ExchangeRateHistory history);

    void Add(GovExchangeRateHistory history);

    void Add(ExchangeRate history);

    void Update(IEnumerable<ExchangeRate> exchangeRates);

    void Update(IEnumerable<GovExchangeRate> exchangeRates);

    IEnumerable<SyncAccounting> GetSyncAccountingFiltered(long clientCode, string agreementName, string organizationName, string currencyCode);

    IEnumerable<SyncAccounting> GetAmgSyncAccountingFiltered(long clientCode, string agreementName, string organizationName, string currencyCode);

    IEnumerable<Client> GetAllClients();

    long Add(BaseLifeCycleStatus status);

    long Add(BaseSalePaymentStatus status);

    long Add(SaleNumber number);

    long Add(DeliveryRecipient deliveryRecipient);

    long Add(DeliveryRecipientAddress address);

    long Add(Debt entity);

    void Add(ClientInDebt entity);

    long Add(Order entity);

    long Add(Sale sale);

    long Add(SupplyOrder supplyOrder);

    long Add(SupplyInvoice supplyInvoice);

    long Add(PackingList packingList);

    long Add(SupplyOrderNumber supplyOrderNumber);

    long Add(SupplyProForm supplyProform);

    long Add(SupplyOrderItem supplyOrderItem);

    long Add(SupplyInvoiceOrderItem supplyInvoiceOrderItem);

    long Add(PackingListPackageOrderItem packingListPackageOrderItem);

    long Add(SupplyOrderPaymentDeliveryProtocol protocol);

    long Add(SupplyOrderPaymentDeliveryProtocolKey key);

    long Add(SupplyPaymentTask supplyPaymentTask);

    decimal GetExchangeRateAmountToEuroByDate(long fromCurrencyId, DateTime fromDate);

    void Update(ClientAgreement agreement);

    void CleanDebtsAndBalances();

    Product GetDevProduct();

    MeasureUnit GetMeasureUnit();

    long Add(MeasureUnit measureUnit);

    void Add(MeasureUnitTranslation translation);

    long Add(Product product);

    void Add(OrderItem orderItem);

    long Add(SupplyInformationDeliveryProtocolKey key);

    long Add(SupplyInformationDeliveryProtocolKeyTranslation keyTranslation);

    long Add(SupplyInformationDeliveryProtocol protocol);

    GovExchangeRate GetGovByCurrencyIdAndCode(long id, string code, DateTime fromDate);

    ExchangeRate GetByCurrencyIdAndCode(long id, string code, DateTime fromDate);

    SupplyOrderPaymentDeliveryProtocolKey GetProtocolPaymentByKey(string debtsFromOneCKey);

    SupplyInformationDeliveryProtocolKey GetInformationProtocolByKey(string debtsFromOneCUkKey);

    IEnumerable<SupplyOrganization> GetAllSupplyOrganizations();

    ConsumablesStorage GetConsumablesStorageByKey(string debtsFromOneCUkKey);

    Organization GetByName(string defaultOrganizationAmg);

    ConsumableProductCategory GetSupplyServiceConsumablesProductCategory();

    ConsumableProduct GetConsumablesProductByKey(string debtsFromOneCUkKey);

    long Add(ConsumablesStorage consumablesStorage);

    long Add(ConsumableProductCategory consumableProductCategory);

    long Add(ConsumableProduct consumableProduct);

    void Update(SupplyOrganizationAgreement agreement);

    PaymentCostMovement GetPaymentCostMovementByKey(string debtsFromOneCUkKey);

    long Add(PaymentCostMovement paymentCostMovement);

    long Add(ConsumablesOrder consumablesOrder);

    long Add(ConsumablesOrderItem consumablesOrderItem);

    void Add(PaymentCostMovementOperation paymentCostMovementOperation);

    Storage GetDevStorage(string name);

    long Add(Storage storage);

    long Add(ProductIncome productIncome);

    long Add(ProductIncomeItem item);

    long Add(ProductSpecification productSpecification);

    long Add(ConsignmentItem consignmentItem);

    void Add(ConsignmentItemMovement movement);

    long Add(Consignment consignment);

    void CleanDebtsAndBalancesForSupplier();

    void CleanDebtsAndBalancesForSupplyOrganizations();

    PaymentRegister GetDevPaymentRegister(string name);

    long Add(PaymentRegister paymentRegister);

    long Add(PaymentCurrencyRegister paymentCurrencyRegister);

    long Add(IncomePaymentOrder incomePaymentOrder);

    PaymentMovement GetDevPaymentMovement(string name);

    long Add(PaymentMovement paymentMovement);

    void Add(PaymentMovementOperation paymentMovementOperation);

    void Add(ClientBalanceMovement movement);

    CrossExchangeRate GetByCurrenciesIds(long currencyFromId, long currencyToId, DateTime fromDate);

    long Add(OutcomePaymentOrder outcomePaymentOrder);

    CrossExchangeRate GetByCurrenciesIds(long currencyFromId, long currencyToId);

    ExchangeRate GetByCurrencyIdAndCode(long id, string code);

    void Add(OutcomePaymentOrderSupplyPaymentTask task);

    IncomePaymentOrder GetLastIncomePaymentOrder();

    OutcomePaymentOrder GetLastOutcomePaymentOrder();
}