using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Services.Services.HistoryOrderItem.Contracts;

public interface IHistoryOrderItemService {
    Task SetReserve(OrderItem orderItem);
    Task NewClientsShoppingCartItems(OrderItem orderItem);
    Task UpdateClientsShoppingCartItems(OrderItem orderItem);
    Task ShiftCurrent(Sale orderItem);
    Task DepreciatedOrder(DepreciatedOrder orderItem);
    Task ReturnNew(SaleReturn saleReturn);
    Task NewPackingListDynamic(PackingList packingList);
    Task AddProductCapitalization(ProductCapitalization productCapitalization);
    Task AddNewFromSupplyOrderUkraineDynamicPlacements(SupplyOrderUkraine supplyOrderUkraine);
    Task SetLastStep(Sale saleReturn);
    Task DepreciatedOrder(long depreciatedOrderId);
    Task SetProductPlacementUpdate(List<ProductPlacement> productPlacements);
    Task SetFastClient(Guid netId);
    Task<(string xlsxFile, string pdfFile)> GetCreateDocumentProductPlacementStorage(string saleInvoicesFolderPath, long[] storageId, string value, DateTime to);

    Task<(string xlsxFile, string pdfFile)> GetVerificationCreateDocumentProductPlacementStorage(string saleInvoicesFolderPath, long[] storageId, string value, DateTime from,
        DateTime to);

    Task DeleteClientsShoppingCartItems(Guid netId);
    Task OrderNewIvoice(Sale sale);
    Task SetAllProducts();
    Task<List<StockStateStorage>> GetStockStateStorage(long[] storageId, string value, DateTime from, DateTime to, long limit, long offset);
    Task<List<ProductPlacementDataHistory>> GetStockStateStorageVerification(long[] storageId, string value, DateTime from, DateTime to, long limit, long offset);
}