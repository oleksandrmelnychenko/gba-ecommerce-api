using System.Collections.Generic;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Messages.Products.ProductPlacementMovements;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.SaleReturns.Contracts;

namespace GBA.Services.Actors.Products;

public sealed class ProductPlacementActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISaleReturnRepositoriesFactory _saleReturnRepositoriesFactory;

    public ProductPlacementActor(IDbConnectionFactory connectionFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        ISaleReturnRepositoriesFactory saleReturnRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _saleReturnRepositoriesFactory = saleReturnRepositoriesFactory;

        Receive<MoveProductPlacementFromSaleReturnMessage>(ProcessMoveProductPlacementFromSaleReturnMessage);
    }

    private void ProcessMoveProductPlacementFromSaleReturnMessage(MoveProductPlacementFromSaleReturnMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();

        IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
        IProductLocationRepository productLocationRepository = _productRepositoriesFactory.NewProductLocationRepository(connection);
        IProductLocationHistoryRepository productLocationHistoryRepository = _productRepositoriesFactory.NewProductLocationHistoryRepository(connection);
        ISaleReturnItemProductPlacementRepository saleReturnItemProductPlacementRepository =
            _saleReturnRepositoriesFactory.NewSaleReturnItemProductPlacementRepository(connection);

        double toRestoreAmount = message.SaleReturnItem.Qty;

        IEnumerable<ProductLocation> productLocations = productLocationRepository.GetAllByOrderItemId(message.SaleReturnItem.OrderItem.Id);

        foreach (ProductLocation location in productLocations) {
            if (location.Qty.Equals(0)) continue;

            if (toRestoreAmount.Equals(0)) break;

            ProductPlacement productPlacement = productPlacementRepository.GetByIdDeleted(location.ProductPlacementId);

            if (productPlacement != null) {
                if (toRestoreAmount > location.Qty) {
                    long productPlacementId = productPlacementRepository.AddWithId(new ProductPlacement {
                        Qty = location.Qty,
                        CellNumber = productPlacement.CellNumber,
                        RowNumber = productPlacement.RowNumber,
                        StorageNumber = productPlacement.StorageNumber,
                        ProductId = message.SaleReturnItem.OrderItem.ProductId,
                        StorageId = message.SaleReturnItem.StorageId
                    });
                    saleReturnItemProductPlacementRepository.Add(new SaleReturnItemProductPlacement {
                        ProductPlacementId = productPlacement.Id,
                        SaleReturnItemId = message.SaleReturnItem.Id,
                        Qty = location.Qty
                    });
                    productLocationHistoryRepository.Add(new ProductLocationHistory {
                        StorageId = message.SaleReturnItem.StorageId,
                        Qty = location.Qty,
                        ProductPlacementId = location.ProductPlacementId,
                        OrderItemId = location.OrderItemId,
                        TypeOfMovement = TypeOfMovement.Return
                    });
                    toRestoreAmount -= location.Qty;
                    location.Qty = 0;
                    productLocationRepository.Remove(location);
                } else {
                    long productPlacementId = productPlacementRepository.AddWithId(new ProductPlacement {
                        Qty = toRestoreAmount,
                        CellNumber = productPlacement.CellNumber,
                        RowNumber = productPlacement.RowNumber,
                        StorageNumber = productPlacement.StorageNumber,
                        ProductId = message.SaleReturnItem.OrderItem.ProductId,
                        StorageId = message.SaleReturnItem.StorageId
                    });
                    saleReturnItemProductPlacementRepository.Add(new SaleReturnItemProductPlacement {
                        ProductPlacementId = productPlacement.Id,
                        SaleReturnItemId = message.SaleReturnItem.Id,
                        Qty = toRestoreAmount
                    });
                    productLocationHistoryRepository.Add(new ProductLocationHistory {
                        StorageId = message.SaleReturnItem.StorageId,
                        Qty = toRestoreAmount,
                        ProductPlacementId = location.ProductPlacementId,
                        OrderItemId = location.OrderItemId,
                        TypeOfMovement = TypeOfMovement.Return
                    });
                    location.Qty -= toRestoreAmount;

                    if (location.Qty > 0)
                        productLocationRepository.Update(location);
                    else
                        productLocationRepository.Remove(location);
                    toRestoreAmount = 0;
                }
            } else {
                if (toRestoreAmount > location.Qty) {
                    long productPlacementId = productPlacementRepository.AddWithId(new ProductPlacement {
                        Qty = location.Qty,
                        CellNumber = "N",
                        RowNumber = "N",
                        StorageNumber = "N",
                        ProductId = message.SaleReturnItem.OrderItem.ProductId,
                        StorageId = message.SaleReturnItem.StorageId
                    });
                    saleReturnItemProductPlacementRepository.Add(new SaleReturnItemProductPlacement {
                        ProductPlacementId = productPlacement.Id,
                        SaleReturnItemId = message.SaleReturnItem.Id,
                        Qty = location.Qty
                    });
                    productLocationHistoryRepository.Add(new ProductLocationHistory {
                        StorageId = message.SaleReturnItem.StorageId,
                        Qty = location.Qty,
                        ProductPlacementId = location.ProductPlacementId,
                        OrderItemId = location.OrderItemId,
                        TypeOfMovement = TypeOfMovement.Return
                    });
                    toRestoreAmount -= location.Qty;
                    location.Qty = 0;
                    productLocationRepository.Update(location);
                    productLocationRepository.Remove(location);
                } else {
                    long productPlacementId = productPlacementRepository.AddWithId(new ProductPlacement {
                        Qty = toRestoreAmount,
                        CellNumber = "N",
                        RowNumber = "N",
                        StorageNumber = "N",
                        ProductId = message.SaleReturnItem.OrderItem.ProductId,
                        StorageId = message.SaleReturnItem.StorageId
                    });
                    saleReturnItemProductPlacementRepository.Add(new SaleReturnItemProductPlacement {
                        ProductPlacementId = productPlacement.Id,
                        SaleReturnItemId = message.SaleReturnItem.Id,
                        Qty = toRestoreAmount
                    });
                    productLocationHistoryRepository.Add(new ProductLocationHistory {
                        StorageId = message.SaleReturnItem.StorageId,
                        Qty = toRestoreAmount,
                        ProductPlacementId = location.ProductPlacementId,
                        OrderItemId = location.OrderItemId,
                        TypeOfMovement = TypeOfMovement.Return
                    });
                    location.Qty -= toRestoreAmount;

                    if (location.Qty > 0)
                        productLocationRepository.Update(location);
                    else
                        productLocationRepository.Remove(location);
                    toRestoreAmount = 0;
                }
            }
        }
    }
}