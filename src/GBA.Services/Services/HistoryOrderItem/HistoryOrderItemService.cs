using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using GBA.Common.Helpers.StockStateStorage;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.DepreciatedOrders.Contracts;
using GBA.Domain.Repositories.DocumentMonths.Contracts;
using GBA.Domain.Repositories.History.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Services.Services.HistoryOrderItem.Contracts;

namespace GBA.Services.Services.HistoryOrderItem;

public sealed class HistoryOrderItemService : IHistoryOrderItemService {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDepreciatedRepositoriesFactory _depreciatedRepositoriesFactory;
    private readonly IDocumentMonthRepositoryFactory _documentMonthRepositoryFactory;
    private readonly IHistoryRepositoryFactory _historyRepositoryFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public HistoryOrderItemService(
        IDbConnectionFactory connectionFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory,
        IHistoryRepositoryFactory historyRepositoryFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IDepreciatedRepositoriesFactory depreciatedRepositoriesFactory,
        IXlsFactoryManager xlsFactoryManager,
        IDocumentMonthRepositoryFactory documentMonthRepositoryFactory
    ) {
        _productRepositoriesFactory = productRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;
        _connectionFactory = connectionFactory;
        _historyRepositoryFactory = historyRepositoryFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _depreciatedRepositoriesFactory = depreciatedRepositoriesFactory;
        _xlsFactoryManager = xlsFactoryManager;
        _documentMonthRepositoryFactory = documentMonthRepositoryFactory;
    }

    public Task AddNewFromSupplyOrderUkraineDynamicPlacements(SupplyOrderUkraine supplyOrderUkraine) {
        return Task.Run(() => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);
            IProductAvailabilityDataHistoryRepository productAvailabilityDataHistoryRepository =
                _historyRepositoryFactory.NewIProductAvailabilityDataHistoryRepository(connectionDataAnalitic);
            IProductPlacementDataHistoryRepository productPlacementDataHistoryRepository =
                _historyRepositoryFactory.NewIProductPlacementDataHistoryRepository(connectionDataAnalitic);
            IOrderRepository orderRepository = _saleRepositoriesFactory.NewOrderRepository(connection);
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
            foreach (SupplyOrderUkraineItem supplyOrderUkraineItem in supplyOrderUkraine.SupplyOrderUkraineItems) {
                Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(supplyOrderUkraineItem.Product.NetUid);
                Product productPlacement = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetId(supplyOrderUkraineItem.Product.NetUid);
                Reservation reservation = new();

                if (product != null) {
                    IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

                    reservation.ProductReservationsUK =
                        productReservationRepository
                            .GetAllCurrentReservationsByProductNetIdAndCulture(
                                product.NetUid,
                                "uk"
                            );

                    reservation.CartProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.ClientShoppingCart != null).ToList();
                    reservation.ProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.Order != null).ToList();

                    reservation.TotalReservedUK = reservation.ProductReservationsUK.Sum(r => r.Qty);
                    reservation.TotalCartReservedUK = reservation.CartProductReservationsUK.Sum(r => r.Qty);

                    reservation.SupplyOrderUkraineCartItem =
                        _supplyUkraineRepositoriesFactory
                            .NewSupplyOrderUkraineCartItemRepository(connection)
                            .GetByProductIdIfReserved(
                                product.Id
                            );

                    reservation.DefectiveAvailabilities =
                        _productRepositoriesFactory
                            .NewProductAvailabilityRepository(connection)
                            .GetAllOnDefectiveStoragesByProductId(
                                product.Id
                            );
                }

                long StockStateStorageId = stockStateStorageRepository.Add(new StockStateStorage {
                    ChangeTypeOrderItem = ChangeTypeOrderItem.AddNewFromSupplyOrderUkraineDynamicPlacements,
                    TotalCartReservedUK = reservation.TotalCartReservedUK,
                    TotalReservedUK = reservation.TotalReservedUK,
                    ProductId = product.Id,
                    UserId = supplyOrderUkraine.ResponsibleId,
                    QtyHistory = supplyOrderUkraineItem.Qty
                });

                foreach (ProductAvailability productAvailabilities in productPlacement.ProductAvailabilities) {
                    long ProductAvailabilityDataHistoryId = productAvailabilityDataHistoryRepository.Add(new ProductAvailabilityDataHistory {
                        StorageId = productAvailabilities.StorageId,
                        StockStateStorageID = StockStateStorageId,
                        Amount = productAvailabilities.Amount
                    });
                    foreach (ProductPlacement placement in productAvailabilities.Storage.ProductPlacements) {
                        Product productBd = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetById(placement.ProductId);

                        ProductPlacementDataHistory productPlacementDataHistory = new() {
                            CellNumber = placement.CellNumber,
                            RowNumber = placement.RowNumber,
                            StorageNumber = placement.StorageNumber,
                            StorageId = placement.StorageId,
                            Qty = placement.Qty,
                            ConsignmentItemId = placement.ConsignmentItemId,
                            ProductId = placement.ProductId,
                            NameUA = productBd.NameUA,
                            VendorCode = productBd.VendorCode,
                            MainOriginalNumber = productBd.MainOriginalNumber,
                            ProductAvailabilityDataHistoryID = ProductAvailabilityDataHistoryId
                        };
                        if (placement.ConsignmentItem != null) productPlacementDataHistory.ConsignmentNumber = placement.ConsignmentItem.Consignment.ProductIncome.Number;

                        productPlacementDataHistoryRepository.Add(productPlacementDataHistory);
                    }
                }
            }
        });
    }

    public Task AddProductCapitalization(ProductCapitalization productCapitalization) {
        return Task.Run(() => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);
            IProductAvailabilityDataHistoryRepository productAvailabilityDataHistoryRepository =
                _historyRepositoryFactory.NewIProductAvailabilityDataHistoryRepository(connectionDataAnalitic);
            IProductPlacementDataHistoryRepository productPlacementDataHistoryRepository =
                _historyRepositoryFactory.NewIProductPlacementDataHistoryRepository(connectionDataAnalitic);
            IOrderRepository orderRepository = _saleRepositoriesFactory.NewOrderRepository(connection);
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            foreach (ProductCapitalizationItem productCapitalizationItem in productCapitalization.ProductCapitalizationItems) {
                Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(productCapitalizationItem.Product.NetUid);
                Product productPlacement = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetId(productCapitalizationItem.Product.NetUid);
                Reservation reservation = new();

                if (product != null) {
                    IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

                    reservation.ProductReservationsUK =
                        productReservationRepository
                            .GetAllCurrentReservationsByProductNetIdAndCulture(
                                product.NetUid,
                                "uk"
                            );

                    reservation.CartProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.ClientShoppingCart != null).ToList();
                    reservation.ProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.Order != null).ToList();

                    reservation.TotalReservedUK = reservation.ProductReservationsUK.Sum(r => r.Qty);
                    reservation.TotalCartReservedUK = reservation.CartProductReservationsUK.Sum(r => r.Qty);

                    reservation.SupplyOrderUkraineCartItem =
                        _supplyUkraineRepositoriesFactory
                            .NewSupplyOrderUkraineCartItemRepository(connection)
                            .GetByProductIdIfReserved(
                                product.Id
                            );

                    reservation.DefectiveAvailabilities =
                        _productRepositoriesFactory
                            .NewProductAvailabilityRepository(connection)
                            .GetAllOnDefectiveStoragesByProductId(
                                product.Id
                            );
                }

                long StockStateStorageId = stockStateStorageRepository.Add(new StockStateStorage {
                    ChangeTypeOrderItem = ChangeTypeOrderItem.AddProductCapitalization,
                    TotalCartReservedUK = reservation.TotalCartReservedUK,
                    TotalReservedUK = reservation.TotalReservedUK,
                    ProductId = product.Id,
                    UserId = productCapitalization.ResponsibleId,
                    QtyHistory = productCapitalizationItem.Qty
                });

                foreach (ProductAvailability productAvailabilities in productPlacement.ProductAvailabilities) {
                    long ProductAvailabilityDataHistoryId = productAvailabilityDataHistoryRepository.Add(new ProductAvailabilityDataHistory {
                        StorageId = productAvailabilities.StorageId,
                        StockStateStorageID = StockStateStorageId,
                        Amount = productAvailabilities.Amount
                    });
                    foreach (ProductPlacement placement in productAvailabilities.Storage.ProductPlacements) {
                        Product productBd = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetById(placement.ProductId);

                        ProductPlacementDataHistory productPlacementDataHistory = new() {
                            CellNumber = placement.CellNumber,
                            RowNumber = placement.RowNumber,
                            StorageNumber = placement.StorageNumber,
                            StorageId = placement.StorageId,
                            Qty = placement.Qty,
                            ConsignmentItemId = placement.ConsignmentItemId,
                            ProductId = placement.ProductId,
                            NameUA = productBd.NameUA,
                            VendorCode = productBd.VendorCode,
                            MainOriginalNumber = productBd.MainOriginalNumber,
                            ProductAvailabilityDataHistoryID = ProductAvailabilityDataHistoryId
                        };
                        if (placement.ConsignmentItem != null) productPlacementDataHistory.ConsignmentNumber = placement.ConsignmentItem.Consignment.ProductIncome.Number;

                        productPlacementDataHistoryRepository.Add(productPlacementDataHistory);
                    }
                }
            }
        });
    }

    public Task<List<StockStateStorage>> GetStockStateStorage(long[] storageId, string value, DateTime from, DateTime to, long limit, long offset) {
        return Task.Run(() => {
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();
            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);
            return stockStateStorageRepository.GetAllFiltered(storageId, from, to, limit, offset, value);
        });
    }

    public Task<List<ProductPlacementDataHistory>> GetStockStateStorageVerification(long[] storageId, string value, DateTime from, DateTime to, long limit, long offset) {
        return Task.Run(() => {
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();
            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);
            return stockStateStorageRepository.GetVerificationAllFilteredProductPlacementHistory(storageId, from, to, limit, offset, value);
        });
    }

    public Task UpdateClientsShoppingCartItems(OrderItem orderItem) {
        return Task.Run(() => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);
            IProductAvailabilityDataHistoryRepository productAvailabilityDataHistoryRepository =
                _historyRepositoryFactory.NewIProductAvailabilityDataHistoryRepository(connectionDataAnalitic);
            IProductPlacementDataHistoryRepository productPlacementDataHistoryRepository =
                _historyRepositoryFactory.NewIProductPlacementDataHistoryRepository(connectionDataAnalitic);
            IOrderRepository orderRepository = _saleRepositoriesFactory.NewOrderRepository(connection);
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(orderItem.Product.NetUid);
            Product productPlacement = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetId(orderItem.Product.NetUid);
            Reservation reservation = new();

            if (product != null) {
                IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

                reservation.ProductReservationsUK =
                    productReservationRepository
                        .GetAllCurrentReservationsByProductNetIdAndCulture(
                            product.NetUid,
                            "uk"
                        );

                reservation.CartProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.ClientShoppingCart != null).ToList();
                reservation.ProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.Order != null).ToList();

                reservation.TotalReservedUK = reservation.ProductReservationsUK.Sum(r => r.Qty);
                reservation.TotalCartReservedUK = reservation.CartProductReservationsUK.Sum(r => r.Qty);

                reservation.SupplyOrderUkraineCartItem =
                    _supplyUkraineRepositoriesFactory
                        .NewSupplyOrderUkraineCartItemRepository(connection)
                        .GetByProductIdIfReserved(
                            product.Id
                        );

                reservation.DefectiveAvailabilities =
                    _productRepositoriesFactory
                        .NewProductAvailabilityRepository(connection)
                        .GetAllOnDefectiveStoragesByProductId(
                            product.Id
                        );
            }

            long StockStateStorageId = stockStateStorageRepository.Add(new StockStateStorage {
                ChangeTypeOrderItem = ChangeTypeOrderItem.UpdateClientsShoppingCartItems,
                TotalCartReservedUK = reservation.TotalCartReservedUK,
                TotalReservedUK = reservation.TotalReservedUK,
                ProductId = product.Id,
                UserId = orderItem.UserId,
                QtyHistory = orderItem.Qty
            });

            foreach (ProductAvailability productAvailabilities in productPlacement.ProductAvailabilities) {
                long ProductAvailabilityDataHistoryId = productAvailabilityDataHistoryRepository.Add(new ProductAvailabilityDataHistory {
                    StorageId = productAvailabilities.StorageId,
                    StockStateStorageID = StockStateStorageId,
                    Amount = productAvailabilities.Amount
                });
                foreach (ProductPlacement placement in productAvailabilities.Storage.ProductPlacements) {
                    Product productBd = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetById(placement.ProductId);

                    ProductPlacementDataHistory productPlacementDataHistory = new() {
                        CellNumber = placement.CellNumber,
                        RowNumber = placement.RowNumber,
                        StorageNumber = placement.StorageNumber,
                        StorageId = placement.StorageId,
                        Qty = placement.Qty,
                        ConsignmentItemId = placement.ConsignmentItemId,
                        ProductId = placement.ProductId,
                        NameUA = productBd.NameUA,
                        VendorCode = productBd.VendorCode,
                        MainOriginalNumber = productBd.MainOriginalNumber,
                        ProductAvailabilityDataHistoryID = ProductAvailabilityDataHistoryId
                    };
                    if (placement.ConsignmentItem != null) productPlacementDataHistory.ConsignmentNumber = placement.ConsignmentItem.Consignment.ProductIncome.Number;

                    productPlacementDataHistoryRepository.Add(productPlacementDataHistory);
                }
            }
        });
    }

    public Task DeleteClientsShoppingCartItems(Guid netId) {
        return Task.Run(() => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);
            IProductAvailabilityDataHistoryRepository productAvailabilityDataHistoryRepository =
                _historyRepositoryFactory.NewIProductAvailabilityDataHistoryRepository(connectionDataAnalitic);
            IProductPlacementDataHistoryRepository productPlacementDataHistoryRepository =
                _historyRepositoryFactory.NewIProductPlacementDataHistoryRepository(connectionDataAnalitic);
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            IOrderRepository orderRepository = _saleRepositoriesFactory.NewOrderRepository(connection);
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            OrderItem orderItem = orderItemRepository.GetByNetIdWithProduct(netId);
            Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(orderItem.Product.NetUid);
            Product productPlacement = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetId(orderItem.Product.NetUid);
            Reservation reservation = new();

            if (product != null) {
                IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

                reservation.ProductReservationsUK =
                    productReservationRepository
                        .GetAllCurrentReservationsByProductNetIdAndCulture(
                            product.NetUid,
                            "uk"
                        );

                reservation.CartProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.ClientShoppingCart != null).ToList();
                reservation.ProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.Order != null).ToList();

                reservation.TotalReservedUK = reservation.ProductReservationsUK.Sum(r => r.Qty);
                reservation.TotalCartReservedUK = reservation.CartProductReservationsUK.Sum(r => r.Qty);

                reservation.SupplyOrderUkraineCartItem =
                    _supplyUkraineRepositoriesFactory
                        .NewSupplyOrderUkraineCartItemRepository(connection)
                        .GetByProductIdIfReserved(
                            product.Id
                        );

                reservation.DefectiveAvailabilities =
                    _productRepositoriesFactory
                        .NewProductAvailabilityRepository(connection)
                        .GetAllOnDefectiveStoragesByProductId(
                            product.Id
                        );
            }

            long StockStateStorageId = stockStateStorageRepository.Add(new StockStateStorage {
                ChangeTypeOrderItem = ChangeTypeOrderItem.DeleteClientsShoppingCartItems,
                TotalCartReservedUK = reservation.TotalCartReservedUK,
                TotalReservedUK = reservation.TotalReservedUK,
                ProductId = product.Id,
                UserId = orderItem.UserId,
                QtyHistory = orderItem.Qty
            });

            foreach (ProductAvailability productAvailabilities in productPlacement.ProductAvailabilities) {
                long ProductAvailabilityDataHistoryId = productAvailabilityDataHistoryRepository.Add(new ProductAvailabilityDataHistory {
                    StorageId = productAvailabilities.StorageId,
                    StockStateStorageID = StockStateStorageId,
                    Amount = productAvailabilities.Amount
                });
                foreach (ProductPlacement placement in productAvailabilities.Storage.ProductPlacements) {
                    Product productBd = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetById(placement.ProductId);

                    ProductPlacementDataHistory productPlacementDataHistory = new() {
                        CellNumber = placement.CellNumber,
                        RowNumber = placement.RowNumber,
                        StorageNumber = placement.StorageNumber,
                        StorageId = placement.StorageId,
                        Qty = placement.Qty,
                        ConsignmentItemId = placement.ConsignmentItemId,
                        ProductId = placement.ProductId,
                        NameUA = productBd.NameUA,
                        VendorCode = productBd.VendorCode,
                        MainOriginalNumber = productBd.MainOriginalNumber,
                        ProductAvailabilityDataHistoryID = ProductAvailabilityDataHistoryId
                    };
                    if (placement.ConsignmentItem != null) productPlacementDataHistory.ConsignmentNumber = placement.ConsignmentItem.Consignment.ProductIncome.Number;

                    productPlacementDataHistoryRepository.Add(productPlacementDataHistory);
                }
            }
        });
    }

    public Task NewClientsShoppingCartItems(OrderItem orderItem) {
        return Task.Run(() => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);
            IProductAvailabilityDataHistoryRepository productAvailabilityDataHistoryRepository =
                _historyRepositoryFactory.NewIProductAvailabilityDataHistoryRepository(connectionDataAnalitic);
            IProductPlacementDataHistoryRepository productPlacementDataHistoryRepository =
                _historyRepositoryFactory.NewIProductPlacementDataHistoryRepository(connectionDataAnalitic);
            IOrderRepository orderRepository = _saleRepositoriesFactory.NewOrderRepository(connection);
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(orderItem.Product.NetUid);
            Product productPlacement = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetId(orderItem.Product.NetUid);
            Reservation reservation = new();

            if (product != null) {
                IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

                reservation.ProductReservationsUK =
                    productReservationRepository
                        .GetAllCurrentReservationsByProductNetIdAndCulture(
                            product.NetUid,
                            "uk"
                        );

                reservation.CartProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.ClientShoppingCart != null).ToList();
                reservation.ProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.Order != null).ToList();

                reservation.TotalReservedUK = reservation.ProductReservationsUK.Sum(r => r.Qty);
                reservation.TotalCartReservedUK = reservation.CartProductReservationsUK.Sum(r => r.Qty);

                reservation.SupplyOrderUkraineCartItem =
                    _supplyUkraineRepositoriesFactory
                        .NewSupplyOrderUkraineCartItemRepository(connection)
                        .GetByProductIdIfReserved(
                            product.Id
                        );

                reservation.DefectiveAvailabilities =
                    _productRepositoriesFactory
                        .NewProductAvailabilityRepository(connection)
                        .GetAllOnDefectiveStoragesByProductId(
                            product.Id
                        );
            }

            long StockStateStorageId = stockStateStorageRepository.Add(new StockStateStorage {
                ChangeTypeOrderItem = ChangeTypeOrderItem.NewClientsShoppingCartItems,
                TotalCartReservedUK = reservation.TotalCartReservedUK,
                TotalReservedUK = reservation.TotalReservedUK,
                ProductId = product.Id,
                UserId = orderItem.UserId,
                QtyHistory = orderItem.Qty
            });

            foreach (ProductAvailability productAvailabilities in productPlacement.ProductAvailabilities) {
                long ProductAvailabilityDataHistoryId = productAvailabilityDataHistoryRepository.Add(new ProductAvailabilityDataHistory {
                    StorageId = productAvailabilities.StorageId,
                    StockStateStorageID = StockStateStorageId,
                    Amount = productAvailabilities.Amount
                });
                foreach (ProductPlacement placement in productAvailabilities.Storage.ProductPlacements) {
                    Product productBd = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetById(placement.ProductId);

                    ProductPlacementDataHistory productPlacementDataHistory = new() {
                        CellNumber = placement.CellNumber,
                        RowNumber = placement.RowNumber,
                        StorageNumber = placement.StorageNumber,
                        StorageId = placement.StorageId,
                        Qty = placement.Qty,
                        ConsignmentItemId = placement.ConsignmentItemId,
                        ProductId = placement.ProductId,
                        NameUA = productBd.NameUA,
                        VendorCode = productBd.VendorCode,
                        MainOriginalNumber = productBd.MainOriginalNumber,
                        ProductAvailabilityDataHistoryID = ProductAvailabilityDataHistoryId
                    };
                    if (placement.ConsignmentItem != null) productPlacementDataHistory.ConsignmentNumber = placement.ConsignmentItem.Consignment.ProductIncome.Number;

                    productPlacementDataHistoryRepository.Add(productPlacementDataHistory);
                }
            }
        });
    }

    public Task NewPackingListDynamic(PackingList packingList) {
        return Task.Run(() => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);
            IProductAvailabilityDataHistoryRepository productAvailabilityDataHistoryRepository =
                _historyRepositoryFactory.NewIProductAvailabilityDataHistoryRepository(connectionDataAnalitic);
            IProductPlacementDataHistoryRepository productPlacementDataHistoryRepository =
                _historyRepositoryFactory.NewIProductPlacementDataHistoryRepository(connectionDataAnalitic);
            IOrderRepository orderRepository = _saleRepositoriesFactory.NewOrderRepository(connection);
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            foreach (PackingListPackageOrderItem PackingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection)
                    .GetByNetIdWithoutIncludes(PackingListPackageOrderItem.SupplyInvoiceOrderItem.Product.NetUid);
                Product productPlacement = _productRepositoriesFactory.NewGetSingleProductRepository(connection)
                    .GetByNetId(PackingListPackageOrderItem.SupplyInvoiceOrderItem.Product.NetUid);
                Reservation reservation = new();

                if (product != null) {
                    IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

                    reservation.ProductReservationsUK =
                        productReservationRepository
                            .GetAllCurrentReservationsByProductNetIdAndCulture(
                                product.NetUid,
                                "uk"
                            );

                    reservation.CartProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.ClientShoppingCart != null).ToList();
                    reservation.ProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.Order != null).ToList();

                    reservation.TotalReservedUK = reservation.ProductReservationsUK.Sum(r => r.Qty);
                    reservation.TotalCartReservedUK = reservation.CartProductReservationsUK.Sum(r => r.Qty);

                    reservation.SupplyOrderUkraineCartItem =
                        _supplyUkraineRepositoriesFactory
                            .NewSupplyOrderUkraineCartItemRepository(connection)
                            .GetByProductIdIfReserved(
                                product.Id
                            );

                    reservation.DefectiveAvailabilities =
                        _productRepositoriesFactory
                            .NewProductAvailabilityRepository(connection)
                            .GetAllOnDefectiveStoragesByProductId(
                                product.Id
                            );
                }

                long StockStateStorageId = stockStateStorageRepository.Add(new StockStateStorage {
                    ChangeTypeOrderItem = ChangeTypeOrderItem.NewPackingListDynamic,
                    TotalCartReservedUK = reservation.TotalCartReservedUK,
                    TotalReservedUK = reservation.TotalReservedUK,
                    ProductId = product.Id,
                    //UserId = productCapitalization.ResponsibleId,
                    QtyHistory = PackingListPackageOrderItem.Qty
                });

                foreach (ProductAvailability productAvailabilities in productPlacement.ProductAvailabilities) {
                    long ProductAvailabilityDataHistoryId = productAvailabilityDataHistoryRepository.Add(new ProductAvailabilityDataHistory {
                        StorageId = productAvailabilities.StorageId,
                        StockStateStorageID = StockStateStorageId,
                        Amount = productAvailabilities.Amount
                    });
                    foreach (ProductPlacement placement in productAvailabilities.Storage.ProductPlacements) {
                        Product productBd = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetById(placement.ProductId);

                        ProductPlacementDataHistory productPlacementDataHistory = new() {
                            CellNumber = placement.CellNumber,
                            RowNumber = placement.RowNumber,
                            StorageNumber = placement.StorageNumber,
                            StorageId = placement.StorageId,
                            Qty = placement.Qty,
                            ConsignmentItemId = placement.ConsignmentItemId,
                            ProductId = placement.ProductId,
                            NameUA = productBd.NameUA,
                            VendorCode = productBd.VendorCode,
                            MainOriginalNumber = productBd.MainOriginalNumber,
                            ProductAvailabilityDataHistoryID = ProductAvailabilityDataHistoryId
                        };
                        if (placement.ConsignmentItem != null) productPlacementDataHistory.ConsignmentNumber = placement.ConsignmentItem.Consignment.ProductIncome.Number;

                        productPlacementDataHistoryRepository.Add(productPlacementDataHistory);
                    }
                }
            }
        });
    }

    public Task ReturnNew(SaleReturn saleReturn) {
        return Task.Run(() => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);
            IProductAvailabilityDataHistoryRepository productAvailabilityDataHistoryRepository =
                _historyRepositoryFactory.NewIProductAvailabilityDataHistoryRepository(connectionDataAnalitic);
            IProductPlacementDataHistoryRepository productPlacementDataHistoryRepository =
                _historyRepositoryFactory.NewIProductPlacementDataHistoryRepository(connectionDataAnalitic);
            IOrderRepository orderRepository = _saleRepositoriesFactory.NewOrderRepository(connection);
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            foreach (SaleReturnItem SaleReturnItem in saleReturn.SaleReturnItems) {
                Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(SaleReturnItem.OrderItem.Product.NetUid);
                Product productPlacement = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetId(SaleReturnItem.OrderItem.Product.NetUid);
                Order order = orderRepository.GetById((long)SaleReturnItem.OrderItem.OrderId);
                Sale saleFromDb = saleRepository.GetByOrderId(order.Id);
                Reservation reservation = new();

                if (product != null) {
                    IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

                    reservation.ProductReservationsUK =
                        productReservationRepository
                            .GetAllCurrentReservationsByProductNetIdAndCulture(
                                product.NetUid,
                                "uk"
                            );

                    reservation.CartProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.ClientShoppingCart != null).ToList();
                    reservation.ProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.Order != null).ToList();

                    reservation.TotalReservedUK = reservation.ProductReservationsUK.Sum(r => r.Qty);
                    reservation.TotalCartReservedUK = reservation.CartProductReservationsUK.Sum(r => r.Qty);

                    reservation.SupplyOrderUkraineCartItem =
                        _supplyUkraineRepositoriesFactory
                            .NewSupplyOrderUkraineCartItemRepository(connection)
                            .GetByProductIdIfReserved(
                                product.Id
                            );

                    reservation.DefectiveAvailabilities =
                        _productRepositoriesFactory
                            .NewProductAvailabilityRepository(connection)
                            .GetAllOnDefectiveStoragesByProductId(
                                product.Id
                            );
                }

                long StockStateStorageId = stockStateStorageRepository.Add(new StockStateStorage {
                    ChangeTypeOrderItem = ChangeTypeOrderItem.Return,
                    TotalCartReservedUK = reservation.TotalCartReservedUK,
                    TotalReservedUK = reservation.TotalReservedUK,
                    ProductId = product.Id,
                    SaleId = saleFromDb.Id,
                    SaleNumberId = saleFromDb.SaleNumberId,
                    UserId = SaleReturnItem.OrderItem.UserId,
                    QtyHistory = SaleReturnItem.Qty
                });

                foreach (ProductAvailability productAvailabilities in productPlacement.ProductAvailabilities) {
                    long ProductAvailabilityDataHistoryId = productAvailabilityDataHistoryRepository.Add(new ProductAvailabilityDataHistory {
                        StorageId = productAvailabilities.StorageId,
                        StockStateStorageID = StockStateStorageId,
                        Amount = productAvailabilities.Amount
                    });
                    foreach (ProductPlacement placement in productAvailabilities.Storage.ProductPlacements) {
                        Product productBd = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetById(placement.ProductId);

                        ProductPlacementDataHistory productPlacementDataHistory = new() {
                            CellNumber = placement.CellNumber,
                            RowNumber = placement.RowNumber,
                            StorageNumber = placement.StorageNumber,
                            StorageId = placement.StorageId,
                            Qty = placement.Qty,
                            ConsignmentItemId = placement.ConsignmentItemId,
                            ProductId = placement.ProductId,
                            NameUA = productBd.NameUA,
                            VendorCode = productBd.VendorCode,
                            MainOriginalNumber = productBd.MainOriginalNumber,
                            ProductAvailabilityDataHistoryID = ProductAvailabilityDataHistoryId
                        };
                        if (placement.ConsignmentItem != null) productPlacementDataHistory.ConsignmentNumber = placement.ConsignmentItem.Consignment.ProductIncome.Number;

                        productPlacementDataHistoryRepository.Add(productPlacementDataHistory);
                    }
                }
            }
        });
    }

    public Task SetAllProducts() {
        return Task.Run(() => {
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);
            IProductAvailabilityDataHistoryRepository productAvailabilityDataHistoryRepository =
                _historyRepositoryFactory.NewIProductAvailabilityDataHistoryRepository(connectionDataAnalitic);
            IProductPlacementDataHistoryRepository productPlacementDataHistoryRepository =
                _historyRepositoryFactory.NewIProductPlacementDataHistoryRepository(connectionDataAnalitic);
            List<Product> productFromDb = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetAll();
            foreach (Product product in productFromDb) {
                Product productPlacement = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetId(product.NetUid);
                Reservation reservation = new();

                if (product != null) {
                    IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

                    reservation.ProductReservationsUK =
                        productReservationRepository
                            .GetAllCurrentReservationsByProductNetIdAndCulture(
                                product.NetUid,
                                "uk"
                            );

                    reservation.CartProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.ClientShoppingCart != null).ToList();
                    reservation.ProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.Order != null).ToList();

                    reservation.TotalReservedUK = reservation.ProductReservationsUK.Sum(r => r.Qty);
                    reservation.TotalCartReservedUK = reservation.CartProductReservationsUK.Sum(r => r.Qty);

                    reservation.SupplyOrderUkraineCartItem =
                        _supplyUkraineRepositoriesFactory
                            .NewSupplyOrderUkraineCartItemRepository(connection)
                            .GetByProductIdIfReserved(
                                product.Id
                            );

                    reservation.DefectiveAvailabilities =
                        _productRepositoriesFactory
                            .NewProductAvailabilityRepository(connection)
                            .GetAllOnDefectiveStoragesByProductId(
                                product.Id
                            );
                }

                long StockStateStorageId = stockStateStorageRepository.Add(new StockStateStorage {
                    ChangeTypeOrderItem = ChangeTypeOrderItem.Set,
                    TotalCartReservedUK = reservation.TotalCartReservedUK,
                    TotalReservedUK = reservation.TotalReservedUK,
                    ProductId = product.Id
                });

                foreach (ProductAvailability productAvailabilities in productPlacement.ProductAvailabilities) {
                    long ProductAvailabilityDataHistoryId = productAvailabilityDataHistoryRepository.Add(new ProductAvailabilityDataHistory {
                        StorageId = productAvailabilities.StorageId,
                        StockStateStorageID = StockStateStorageId,
                        Amount = productAvailabilities.Amount
                    });
                    foreach (ProductPlacement placement in productAvailabilities.Storage.ProductPlacements) {
                        ProductPlacementDataHistory productPlacementDataHistory = new() {
                            CellNumber = placement.CellNumber,
                            RowNumber = placement.RowNumber,
                            StorageNumber = placement.StorageNumber,
                            StorageId = placement.StorageId,
                            Qty = placement.Qty,
                            ConsignmentItemId = placement.ConsignmentItemId,
                            ProductId = placement.ProductId,
                            NameUA = productPlacement.NameUA,
                            VendorCode = productPlacement.VendorCode,
                            MainOriginalNumber = productPlacement.MainOriginalNumber,
                            ProductAvailabilityDataHistoryID = ProductAvailabilityDataHistoryId
                        };
                        if (placement.ConsignmentItem != null) productPlacementDataHistory.ConsignmentNumber = placement.ConsignmentItem.Consignment.ProductIncome.Number;

                        productPlacementDataHistoryRepository.Add(productPlacementDataHistory);
                    }
                }
            }
        });
    }

    public Task OrderNewIvoice(Sale sale) {
        return Task.Run(() => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);
            IProductAvailabilityDataHistoryRepository productAvailabilityDataHistoryRepository =
                _historyRepositoryFactory.NewIProductAvailabilityDataHistoryRepository(connectionDataAnalitic);
            IProductPlacementDataHistoryRepository productPlacementDataHistoryRepository =
                _historyRepositoryFactory.NewIProductPlacementDataHistoryRepository(connectionDataAnalitic);
            IOrderRepository orderRepository = _saleRepositoriesFactory.NewOrderRepository(connection);
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            foreach (OrderItem orderItem in sale.Order.OrderItems) {
                Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(orderItem.Product.NetUid);
                Product productPlacement = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetId(orderItem.Product.NetUid);
                Order order = orderRepository.GetById((long)orderItem.OrderId);
                Sale saleFromDb = saleRepository.GetByOrderId(order.Id);
                Reservation reservation = new();

                if (product != null) {
                    IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

                    reservation.ProductReservationsUK =
                        productReservationRepository
                            .GetAllCurrentReservationsByProductNetIdAndCulture(
                                product.NetUid,
                                "uk"
                            );

                    reservation.CartProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.ClientShoppingCart != null).ToList();
                    reservation.ProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.Order != null).ToList();

                    reservation.TotalReservedUK = reservation.ProductReservationsUK.Sum(r => r.Qty);
                    reservation.TotalCartReservedUK = reservation.CartProductReservationsUK.Sum(r => r.Qty);

                    reservation.SupplyOrderUkraineCartItem =
                        _supplyUkraineRepositoriesFactory
                            .NewSupplyOrderUkraineCartItemRepository(connection)
                            .GetByProductIdIfReserved(
                                product.Id
                            );

                    reservation.DefectiveAvailabilities =
                        _productRepositoriesFactory
                            .NewProductAvailabilityRepository(connection)
                            .GetAllOnDefectiveStoragesByProductId(
                                product.Id
                            );
                }

                long StockStateStorageId = stockStateStorageRepository.Add(new StockStateStorage {
                    ChangeTypeOrderItem = ChangeTypeOrderItem.OrderNewIvoice,
                    TotalCartReservedUK = reservation.TotalCartReservedUK,
                    TotalReservedUK = reservation.TotalReservedUK,
                    ProductId = product.Id,
                    SaleId = sale.Id,
                    SaleNumberId = sale.SaleNumberId,
                    UserId = orderItem.UserId,
                    QtyHistory = orderItem.Qty
                });

                foreach (ProductAvailability productAvailabilities in productPlacement.ProductAvailabilities) {
                    long ProductAvailabilityDataHistoryId = productAvailabilityDataHistoryRepository.Add(new ProductAvailabilityDataHistory {
                        StorageId = productAvailabilities.StorageId,
                        StockStateStorageID = StockStateStorageId,
                        Amount = productAvailabilities.Amount
                    });
                    foreach (ProductPlacement placement in productAvailabilities.Storage.ProductPlacements) {
                        Product productBd = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetById(placement.ProductId);

                        ProductPlacementDataHistory productPlacementDataHistory = new() {
                            CellNumber = placement.CellNumber,
                            RowNumber = placement.RowNumber,
                            StorageNumber = placement.StorageNumber,
                            StorageId = placement.StorageId,
                            Qty = placement.Qty,
                            ConsignmentItemId = placement.ConsignmentItemId,
                            ProductId = placement.ProductId,
                            NameUA = productBd.NameUA,
                            VendorCode = productBd.VendorCode,
                            MainOriginalNumber = productBd.MainOriginalNumber,
                            ProductAvailabilityDataHistoryID = ProductAvailabilityDataHistoryId
                        };
                        if (placement.ConsignmentItem != null) productPlacementDataHistory.ConsignmentNumber = placement.ConsignmentItem.Consignment.ProductIncome.Number;

                        productPlacementDataHistoryRepository.Add(productPlacementDataHistory);
                    }
                }
            }
        });
    }

    public Task SetLastStep(Sale sale) {
        return Task.Run(() => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);
            IProductAvailabilityDataHistoryRepository productAvailabilityDataHistoryRepository =
                _historyRepositoryFactory.NewIProductAvailabilityDataHistoryRepository(connectionDataAnalitic);
            IProductPlacementDataHistoryRepository productPlacementDataHistoryRepository =
                _historyRepositoryFactory.NewIProductPlacementDataHistoryRepository(connectionDataAnalitic);
            IOrderRepository orderRepository = _saleRepositoriesFactory.NewOrderRepository(connection);
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            foreach (OrderItem orderItem in sale.Order.OrderItems) {
                Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(orderItem.Product.NetUid);
                Product productPlacement = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetId(orderItem.Product.NetUid);
                Order order = orderRepository.GetById((long)orderItem.OrderId);
                Sale saleFromDb = saleRepository.GetByOrderId(order.Id);
                Reservation reservation = new();

                if (product != null) {
                    IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

                    reservation.ProductReservationsUK =
                        productReservationRepository
                            .GetAllCurrentReservationsByProductNetIdAndCulture(
                                product.NetUid,
                                "uk"
                            );

                    reservation.CartProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.ClientShoppingCart != null).ToList();
                    reservation.ProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.Order != null).ToList();

                    reservation.TotalReservedUK = reservation.ProductReservationsUK.Sum(r => r.Qty);
                    reservation.TotalCartReservedUK = reservation.CartProductReservationsUK.Sum(r => r.Qty);

                    reservation.SupplyOrderUkraineCartItem =
                        _supplyUkraineRepositoriesFactory
                            .NewSupplyOrderUkraineCartItemRepository(connection)
                            .GetByProductIdIfReserved(
                                product.Id
                            );

                    reservation.DefectiveAvailabilities =
                        _productRepositoriesFactory
                            .NewProductAvailabilityRepository(connection)
                            .GetAllOnDefectiveStoragesByProductId(
                                product.Id
                            );
                }

                long StockStateStorageId = stockStateStorageRepository.Add(new StockStateStorage {
                    ChangeTypeOrderItem = ChangeTypeOrderItem.SetLastStep,
                    TotalCartReservedUK = reservation.TotalCartReservedUK,
                    TotalReservedUK = reservation.TotalReservedUK,
                    ProductId = product.Id,
                    SaleId = sale.Id,
                    SaleNumberId = sale.SaleNumberId,
                    UserId = orderItem.UserId,
                    QtyHistory = orderItem.Qty
                });

                foreach (ProductAvailability productAvailabilities in productPlacement.ProductAvailabilities) {
                    long ProductAvailabilityDataHistoryId = productAvailabilityDataHistoryRepository.Add(new ProductAvailabilityDataHistory {
                        StorageId = productAvailabilities.StorageId,
                        StockStateStorageID = StockStateStorageId,
                        Amount = productAvailabilities.Amount
                    });
                    IEnumerable<ProductPlacement> historyProductPlacements =
                        productPlacementRepository.GetIsHistorySet(productAvailabilities.ProductId, productAvailabilities.StorageId);
                    foreach (ProductPlacement item in historyProductPlacements) {
                        productAvailabilities.Storage.ProductPlacements.Add(item);
                        productPlacementRepository.RemoveIsHistorySet(item);
                    }

                    foreach (ProductPlacement placement in productAvailabilities.Storage.ProductPlacements) {
                        Product productBd = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetById(placement.ProductId);

                        ProductPlacementDataHistory productPlacementDataHistory = new() {
                            CellNumber = placement.CellNumber,
                            RowNumber = placement.RowNumber,
                            StorageNumber = placement.StorageNumber,
                            StorageId = placement.StorageId,
                            Qty = placement.Qty,
                            ConsignmentItemId = placement.ConsignmentItemId,
                            ProductId = placement.ProductId,
                            NameUA = productBd.NameUA,
                            VendorCode = productBd.VendorCode,
                            MainOriginalNumber = productBd.MainOriginalNumber,
                            ProductAvailabilityDataHistoryID = ProductAvailabilityDataHistoryId
                        };
                        if (placement.ConsignmentItem != null) productPlacementDataHistory.ConsignmentNumber = placement.ConsignmentItem.Consignment.ProductIncome.Number;

                        productPlacementDataHistoryRepository.Add(productPlacementDataHistory);
                    }
                }
            }
        });
    }

    public Task SetReserve(OrderItem orderItem) {
        return Task.Run(() => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);
            IProductAvailabilityDataHistoryRepository productAvailabilityDataHistoryRepository =
                _historyRepositoryFactory.NewIProductAvailabilityDataHistoryRepository(connectionDataAnalitic);
            IProductPlacementDataHistoryRepository productPlacementDataHistoryRepository =
                _historyRepositoryFactory.NewIProductPlacementDataHistoryRepository(connectionDataAnalitic);
            IOrderRepository orderRepository = _saleRepositoriesFactory.NewOrderRepository(connection);
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(orderItem.Product.NetUid);
            Product productPlacement = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetId(orderItem.Product.NetUid);
            Order order = orderRepository.GetById((long)orderItem.OrderId);
            Sale sale = saleRepository.GetByOrderId(order.Id);
            Reservation reservation = new();

            if (product != null) {
                IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

                reservation.ProductReservationsUK =
                    productReservationRepository
                        .GetAllCurrentReservationsByProductNetIdAndCulture(
                            product.NetUid,
                            "uk"
                        );

                reservation.CartProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.ClientShoppingCart != null).ToList();
                reservation.ProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.Order != null).ToList();

                reservation.TotalReservedUK = reservation.ProductReservationsUK.Sum(r => r.Qty);
                reservation.TotalCartReservedUK = reservation.CartProductReservationsUK.Sum(r => r.Qty);

                reservation.SupplyOrderUkraineCartItem =
                    _supplyUkraineRepositoriesFactory
                        .NewSupplyOrderUkraineCartItemRepository(connection)
                        .GetByProductIdIfReserved(
                            product.Id
                        );

                reservation.DefectiveAvailabilities =
                    _productRepositoriesFactory
                        .NewProductAvailabilityRepository(connection)
                        .GetAllOnDefectiveStoragesByProductId(
                            product.Id
                        );
            }

            long StockStateStorageId = stockStateStorageRepository.Add(new StockStateStorage {
                ChangeTypeOrderItem = ChangeTypeOrderItem.Reserve,
                TotalCartReservedUK = reservation.TotalCartReservedUK,
                TotalReservedUK = reservation.TotalReservedUK,
                ProductId = product.Id,
                SaleId = sale.Id,
                SaleNumberId = sale.SaleNumberId,
                UserId = orderItem.UserId,
                QtyHistory = orderItem.Qty
            });

            foreach (ProductAvailability productAvailabilities in productPlacement.ProductAvailabilities) {
                long ProductAvailabilityDataHistoryId = productAvailabilityDataHistoryRepository.Add(new ProductAvailabilityDataHistory {
                    StorageId = productAvailabilities.StorageId,
                    StockStateStorageID = StockStateStorageId,
                    Amount = productAvailabilities.Amount
                });
                foreach (ProductPlacement placement in productAvailabilities.Storage.ProductPlacements) {
                    Product productBd = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetById(placement.ProductId);

                    ProductPlacementDataHistory productPlacementDataHistory = new() {
                        CellNumber = placement.CellNumber,
                        RowNumber = placement.RowNumber,
                        StorageNumber = placement.StorageNumber,
                        StorageId = placement.StorageId,
                        Qty = placement.Qty,
                        ConsignmentItemId = placement.ConsignmentItemId,
                        ProductId = placement.ProductId,
                        NameUA = productBd.NameUA,
                        VendorCode = productBd.VendorCode,
                        MainOriginalNumber = productBd.MainOriginalNumber,
                        ProductAvailabilityDataHistoryID = ProductAvailabilityDataHistoryId
                    };
                    if (placement.ConsignmentItem != null) productPlacementDataHistory.ConsignmentNumber = placement.ConsignmentItem.Consignment.ProductIncome.Number;

                    productPlacementDataHistoryRepository.Add(productPlacementDataHistory);
                }
            }
        });
    }

    public Task ShiftCurrent(Sale sale) {
        return Task.Run(() => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);
            IProductAvailabilityDataHistoryRepository productAvailabilityDataHistoryRepository =
                _historyRepositoryFactory.NewIProductAvailabilityDataHistoryRepository(connectionDataAnalitic);
            IProductPlacementDataHistoryRepository productPlacementDataHistoryRepository =
                _historyRepositoryFactory.NewIProductPlacementDataHistoryRepository(connectionDataAnalitic);
            IOrderRepository orderRepository = _saleRepositoriesFactory.NewOrderRepository(connection);
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);

            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            HistoryInvoiceEdit history = sale.HistoryInvoiceEdit.LastOrDefault();
            foreach (OrderItemBaseShiftStatus item in history.OrderItemBaseShiftStatuses.Where(y => y.Qty != 0)) {
                OrderItem orderItem = sale.Order.OrderItems.FirstOrDefault(x => item.OrderItemId == x.Id);
                if (orderItem == null) orderItem = orderItemRepository.GetById(item.OrderItemId);

                Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(orderItem.Product.NetUid);
                Product productPlacement = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetId(orderItem.Product.NetUid);
                Order order = orderRepository.GetById((long)orderItem.OrderId);

                Sale saleFromDb = saleRepository.GetByOrderId(order.Id);
                Reservation reservation = new();

                if (product != null) {
                    IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

                    reservation.ProductReservationsUK =
                        productReservationRepository
                            .GetAllCurrentReservationsByProductNetIdAndCulture(
                                product.NetUid,
                                "uk"
                            );

                    reservation.CartProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.ClientShoppingCart != null).ToList();
                    reservation.ProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.Order != null).ToList();

                    reservation.TotalReservedUK = reservation.ProductReservationsUK.Sum(r => r.Qty);
                    reservation.TotalCartReservedUK = reservation.CartProductReservationsUK.Sum(r => r.Qty);

                    reservation.SupplyOrderUkraineCartItem =
                        _supplyUkraineRepositoriesFactory
                            .NewSupplyOrderUkraineCartItemRepository(connection)
                            .GetByProductIdIfReserved(
                                product.Id
                            );

                    reservation.DefectiveAvailabilities =
                        _productRepositoriesFactory
                            .NewProductAvailabilityRepository(connection)
                            .GetAllOnDefectiveStoragesByProductId(
                                product.Id
                            );
                }

                long StockStateStorageId = stockStateStorageRepository.Add(new StockStateStorage {
                    ChangeTypeOrderItem = ChangeTypeOrderItem.ActEditTheInvoice,
                    TotalCartReservedUK = reservation.TotalCartReservedUK,
                    TotalReservedUK = reservation.TotalReservedUK,
                    ProductId = product.Id,
                    SaleId = sale.Id,
                    SaleNumberId = sale.SaleNumberId,
                    UserId = orderItem.UserId,
                    QtyHistory = item.Qty
                });

                foreach (ProductAvailability productAvailabilities in productPlacement.ProductAvailabilities) {
                    long ProductAvailabilityDataHistoryId = productAvailabilityDataHistoryRepository.Add(new ProductAvailabilityDataHistory {
                        StorageId = productAvailabilities.StorageId,
                        StockStateStorageID = StockStateStorageId,
                        Amount = productAvailabilities.Amount
                    });
                    foreach (ProductPlacement placement in productAvailabilities.Storage.ProductPlacements) {
                        Product productBd = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetById(placement.ProductId);

                        ProductPlacementDataHistory productPlacementDataHistory = new() {
                            CellNumber = placement.CellNumber,
                            RowNumber = placement.RowNumber,
                            StorageNumber = placement.StorageNumber,
                            StorageId = placement.StorageId,
                            Qty = placement.Qty,
                            ConsignmentItemId = placement.ConsignmentItemId,
                            ProductId = placement.ProductId,
                            NameUA = productBd.NameUA,
                            VendorCode = productBd.VendorCode,
                            MainOriginalNumber = productBd.MainOriginalNumber,
                            ProductAvailabilityDataHistoryID = ProductAvailabilityDataHistoryId
                        };
                        if (placement.ConsignmentItem != null) productPlacementDataHistory.ConsignmentNumber = placement.ConsignmentItem.Consignment.ProductIncome.Number;

                        productPlacementDataHistoryRepository.Add(productPlacementDataHistory);
                    }
                }
            }
        });
    }

    public Task<(string xlsxFile, string pdfFile)> GetCreateDocumentProductPlacementStorage(string saleInvoicesFolderPath, long[] storageId, string value, DateTime to) {
        return Task.Run(() => {
            string xlsxFile = string.Empty;
            string pdfFile = string.Empty;
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();

            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);

            List<StockStateStorage> productsPlacementStorage = stockStateStorageRepository.GetAll(storageId, to, value);
            (xlsxFile, pdfFile) =
                _xlsFactoryManager
                    .NewProductPlacementStorageManagerManager()
                    .ExportStockStateStorageToXlsx(
                        saleInvoicesFolderPath,
                        productsPlacementStorage,
                        _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk"));

            return (xlsxFile, pdfFile);
        });
    }

    public Task SetProductPlacementUpdate(List<ProductPlacement> productPlacements) {
        return Task.Run(() => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);
            IProductAvailabilityDataHistoryRepository productAvailabilityDataHistoryRepository =
                _historyRepositoryFactory.NewIProductAvailabilityDataHistoryRepository(connectionDataAnalitic);
            IProductPlacementDataHistoryRepository productPlacementDataHistoryRepository =
                _historyRepositoryFactory.NewIProductPlacementDataHistoryRepository(connectionDataAnalitic);
            IOrderRepository orderRepository = _saleRepositoriesFactory.NewOrderRepository(connection);
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);

            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
            Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetById(productPlacements.First().ProductId);
            Product productPlacement = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetId(product.NetUid);
            Reservation reservation = new();

            if (product != null) {
                IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

                reservation.ProductReservationsUK =
                    productReservationRepository
                        .GetAllCurrentReservationsByProductNetIdAndCulture(
                            product.NetUid,
                            "uk"
                        );

                reservation.CartProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.ClientShoppingCart != null).ToList();
                reservation.ProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.Order != null).ToList();

                reservation.TotalReservedUK = reservation.ProductReservationsUK.Sum(r => r.Qty);
                reservation.TotalCartReservedUK = reservation.CartProductReservationsUK.Sum(r => r.Qty);

                reservation.SupplyOrderUkraineCartItem =
                    _supplyUkraineRepositoriesFactory
                        .NewSupplyOrderUkraineCartItemRepository(connection)
                        .GetByProductIdIfReserved(
                            product.Id
                        );

                reservation.DefectiveAvailabilities =
                    _productRepositoriesFactory
                        .NewProductAvailabilityRepository(connection)
                        .GetAllOnDefectiveStoragesByProductId(
                            product.Id
                        );
            }

            long StockStateStorageId = stockStateStorageRepository.Add(new StockStateStorage {
                ChangeTypeOrderItem = ChangeTypeOrderItem.ProductPlacementUpdate,
                TotalCartReservedUK = reservation.TotalCartReservedUK,
                TotalReservedUK = reservation.TotalReservedUK,
                ProductId = product.Id
            });

            foreach (ProductAvailability productAvailabilities in productPlacement.ProductAvailabilities) {
                long ProductAvailabilityDataHistoryId = productAvailabilityDataHistoryRepository.Add(new ProductAvailabilityDataHistory {
                    StorageId = productAvailabilities.StorageId,
                    StockStateStorageID = StockStateStorageId,
                    Amount = productAvailabilities.Amount
                });
                foreach (ProductPlacement placement in productAvailabilities.Storage.ProductPlacements) {
                    Product productBd = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetById(placement.ProductId);

                    ProductPlacementDataHistory productPlacementDataHistory = new() {
                        CellNumber = placement.CellNumber,
                        RowNumber = placement.RowNumber,
                        StorageNumber = placement.StorageNumber,
                        StorageId = placement.StorageId,
                        Qty = placement.Qty,
                        ConsignmentItemId = placement.ConsignmentItemId,
                        ProductId = placement.ProductId,
                        NameUA = productBd.NameUA,
                        VendorCode = productBd.VendorCode,
                        MainOriginalNumber = productBd.MainOriginalNumber,
                        ProductAvailabilityDataHistoryID = ProductAvailabilityDataHistoryId
                    };
                    if (placement.ConsignmentItem != null) productPlacementDataHistory.ConsignmentNumber = placement.ConsignmentItem.Consignment.ProductIncome.Number;

                    productPlacementDataHistoryRepository.Add(productPlacementDataHistory);
                }
            }
        });
    }

    public Task SetFastClient(Guid netId) {
        return Task.Run(() => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);
            IProductAvailabilityDataHistoryRepository productAvailabilityDataHistoryRepository =
                _historyRepositoryFactory.NewIProductAvailabilityDataHistoryRepository(connectionDataAnalitic);
            IProductPlacementDataHistoryRepository productPlacementDataHistoryRepository =
                _historyRepositoryFactory.NewIProductPlacementDataHistoryRepository(connectionDataAnalitic);
            IOrderRepository orderRepository = _saleRepositoriesFactory.NewOrderRepository(connection);
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);

            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
            Sale saleFromDb = saleRepository.GetByNetId(netId);

            foreach (OrderItem orderItem in saleFromDb.Order.OrderItems) {
                Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(orderItem.Product.NetUid);
                Product productPlacement = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetId(orderItem.Product.NetUid);
                Order order = orderRepository.GetById((long)orderItem.OrderId);
                Reservation reservation = new();

                if (product != null) {
                    IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

                    reservation.ProductReservationsUK =
                        productReservationRepository
                            .GetAllCurrentReservationsByProductNetIdAndCulture(
                                product.NetUid,
                                "uk"
                            );

                    reservation.CartProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.ClientShoppingCart != null).ToList();
                    reservation.ProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.Order != null).ToList();

                    reservation.TotalReservedUK = reservation.ProductReservationsUK.Sum(r => r.Qty);
                    reservation.TotalCartReservedUK = reservation.CartProductReservationsUK.Sum(r => r.Qty);

                    reservation.SupplyOrderUkraineCartItem =
                        _supplyUkraineRepositoriesFactory
                            .NewSupplyOrderUkraineCartItemRepository(connection)
                            .GetByProductIdIfReserved(
                                product.Id
                            );

                    reservation.DefectiveAvailabilities =
                        _productRepositoriesFactory
                            .NewProductAvailabilityRepository(connection)
                            .GetAllOnDefectiveStoragesByProductId(
                                product.Id
                            );
                }

                long StockStateStorageId = stockStateStorageRepository.Add(new StockStateStorage {
                    ChangeTypeOrderItem = ChangeTypeOrderItem.SetLastStep,
                    TotalCartReservedUK = reservation.TotalCartReservedUK,
                    TotalReservedUK = reservation.TotalReservedUK,
                    ProductId = product.Id,
                    SaleId = saleFromDb.Id,
                    SaleNumberId = saleFromDb.SaleNumberId,
                    UserId = orderItem.UserId,
                    QtyHistory = orderItem.Qty
                });

                foreach (ProductAvailability productAvailabilities in productPlacement.ProductAvailabilities) {
                    long ProductAvailabilityDataHistoryId = productAvailabilityDataHistoryRepository.Add(new ProductAvailabilityDataHistory {
                        StorageId = productAvailabilities.StorageId,
                        StockStateStorageID = StockStateStorageId,
                        Amount = productAvailabilities.Amount
                    });
                    foreach (ProductPlacement placement in productAvailabilities.Storage.ProductPlacements) {
                        Product productBd = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetById(placement.ProductId);

                        ProductPlacementDataHistory productPlacementDataHistory = new() {
                            CellNumber = placement.CellNumber,
                            RowNumber = placement.RowNumber,
                            StorageNumber = placement.StorageNumber,
                            StorageId = placement.StorageId,
                            Qty = placement.Qty,
                            ConsignmentItemId = placement.ConsignmentItemId,
                            ProductId = placement.ProductId,
                            NameUA = productBd.NameUA,
                            VendorCode = productBd.VendorCode,
                            MainOriginalNumber = productBd.MainOriginalNumber,
                            ProductAvailabilityDataHistoryID = ProductAvailabilityDataHistoryId
                        };
                        if (placement.ConsignmentItem != null) productPlacementDataHistory.ConsignmentNumber = placement.ConsignmentItem.Consignment.ProductIncome.Number;

                        productPlacementDataHistoryRepository.Add(productPlacementDataHistory);
                    }
                }
            }
        });
    }

    public Task DepreciatedOrder(DepreciatedOrder depreciatedOrder) {
        return Task.Run(() => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);
            IProductAvailabilityDataHistoryRepository productAvailabilityDataHistoryRepository =
                _historyRepositoryFactory.NewIProductAvailabilityDataHistoryRepository(connectionDataAnalitic);
            IProductPlacementDataHistoryRepository productPlacementDataHistoryRepository =
                _historyRepositoryFactory.NewIProductPlacementDataHistoryRepository(connectionDataAnalitic);
            IOrderRepository orderRepository = _saleRepositoriesFactory.NewOrderRepository(connection);
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);

            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            foreach (DepreciatedOrderItem DepreciatedOrderItem in depreciatedOrder.DepreciatedOrderItems) {
                Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(DepreciatedOrderItem.Product.NetUid);
                Product productPlacement = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetId(DepreciatedOrderItem.Product.NetUid);
                Reservation reservation = new();

                if (product != null) {
                    IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

                    reservation.ProductReservationsUK =
                        productReservationRepository
                            .GetAllCurrentReservationsByProductNetIdAndCulture(
                                product.NetUid,
                                "uk"
                            );

                    reservation.CartProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.ClientShoppingCart != null).ToList();
                    reservation.ProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.Order != null).ToList();

                    reservation.TotalReservedUK = reservation.ProductReservationsUK.Sum(r => r.Qty);
                    reservation.TotalCartReservedUK = reservation.CartProductReservationsUK.Sum(r => r.Qty);

                    reservation.SupplyOrderUkraineCartItem =
                        _supplyUkraineRepositoriesFactory
                            .NewSupplyOrderUkraineCartItemRepository(connection)
                            .GetByProductIdIfReserved(
                                product.Id
                            );

                    reservation.DefectiveAvailabilities =
                        _productRepositoriesFactory
                            .NewProductAvailabilityRepository(connection)
                            .GetAllOnDefectiveStoragesByProductId(
                                product.Id
                            );
                }

                long StockStateStorageId = default;
                if (depreciatedOrder.IsManagement)
                    StockStateStorageId = stockStateStorageRepository.Add(new StockStateStorage {
                        ChangeTypeOrderItem = ChangeTypeOrderItem.DepreciatedOrderManagement,
                        TotalCartReservedUK = reservation.TotalCartReservedUK,
                        TotalReservedUK = reservation.TotalReservedUK,
                        ProductId = product.Id,
                        UserId = depreciatedOrder.ResponsibleId,
                        QtyHistory = DepreciatedOrderItem.Qty
                    });
                else
                    StockStateStorageId = stockStateStorageRepository.Add(new StockStateStorage {
                        ChangeTypeOrderItem = ChangeTypeOrderItem.DepreciatedOrder,
                        TotalCartReservedUK = reservation.TotalCartReservedUK,
                        TotalReservedUK = reservation.TotalReservedUK,
                        ProductId = product.Id,
                        UserId = depreciatedOrder.ResponsibleId,
                        QtyHistory = DepreciatedOrderItem.Qty
                    });


                foreach (ProductAvailability productAvailabilities in productPlacement.ProductAvailabilities) {
                    long ProductAvailabilityDataHistoryId = productAvailabilityDataHistoryRepository.Add(new ProductAvailabilityDataHistory {
                        StorageId = productAvailabilities.StorageId,
                        StockStateStorageID = StockStateStorageId,
                        Amount = productAvailabilities.Amount
                    });
                    foreach (ProductPlacement placement in productAvailabilities.Storage.ProductPlacements) {
                        Product productBd = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetById(placement.ProductId);

                        ProductPlacementDataHistory productPlacementDataHistory = new() {
                            CellNumber = placement.CellNumber,
                            RowNumber = placement.RowNumber,
                            StorageNumber = placement.StorageNumber,
                            StorageId = placement.StorageId,
                            Qty = placement.Qty,
                            ConsignmentItemId = placement.ConsignmentItemId,
                            ProductId = placement.ProductId,
                            NameUA = productBd.NameUA,
                            VendorCode = productBd.VendorCode,
                            MainOriginalNumber = productBd.MainOriginalNumber,
                            ProductAvailabilityDataHistoryID = ProductAvailabilityDataHistoryId
                        };
                        if (placement.ConsignmentItem != null) productPlacementDataHistory.ConsignmentNumber = placement.ConsignmentItem.Consignment.ProductIncome.Number;

                        productPlacementDataHistoryRepository.Add(productPlacementDataHistory);
                    }
                }
            }
        });
    }

    public Task<(string xlsxFile, string pdfFile)> GetVerificationCreateDocumentProductPlacementStorage(string saleInvoicesFolderPath, long[] storageId, string value,
        DateTime from, DateTime to) {
        return Task.Run(() => {
            string xlsxFile = string.Empty;
            string pdfFile = string.Empty;
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();

            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);

            List<ProductPlacementDataHistory> productsPlacementStorage =
                stockStateStorageRepository.GetVerificationAllFilteredProductPlacementHistory(storageId, from, to, 0, 0, value);
            (xlsxFile, pdfFile) =
                _xlsFactoryManager
                    .NewProductPlacementStorageManagerManager()
                    .ExportVerificationStockStatesStorageToXlsxTest(
                        saleInvoicesFolderPath,
                        from,
                        to,
                        productsPlacementStorage,
                        _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk"));

            return (xlsxFile, pdfFile);
        });
    }

    public Task DepreciatedOrder(long depreciatedOrderId) {
        return Task.Run(() => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            using IDbConnection connectionDataAnalitic = _connectionFactory.NewDataAnalyticSqlConnection();
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IStockStateStorageRepository stockStateStorageRepository = _historyRepositoryFactory.NewStockStateStorageRepository(connectionDataAnalitic);
            IProductAvailabilityDataHistoryRepository productAvailabilityDataHistoryRepository =
                _historyRepositoryFactory.NewIProductAvailabilityDataHistoryRepository(connectionDataAnalitic);
            IProductPlacementDataHistoryRepository productPlacementDataHistoryRepository =
                _historyRepositoryFactory.NewIProductPlacementDataHistoryRepository(connectionDataAnalitic);
            IOrderRepository orderRepository = _saleRepositoriesFactory.NewOrderRepository(connection);
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
            IDepreciatedOrderRepository depreciatedOrderRepository = _depreciatedRepositoriesFactory.NewDepreciatedOrderRepository(connection);
            IDepreciatedOrderItemRepository depreciatedOrderItemRepository = _depreciatedRepositoriesFactory.NewDepreciatedOrderItemRepository(connection);
            DepreciatedOrder depreciatedOrder = depreciatedOrderRepository.GetById(depreciatedOrderId);
            foreach (DepreciatedOrderItem orderItem in depreciatedOrder.DepreciatedOrderItems) {
                Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(orderItem.Product.NetUid);
                Product productPlacement = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetId(orderItem.Product.NetUid);
                Reservation reservation = new();

                if (product != null) {
                    IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

                    reservation.ProductReservationsUK =
                        productReservationRepository
                            .GetAllCurrentReservationsByProductNetIdAndCulture(
                                product.NetUid,
                                "uk"
                            );

                    reservation.CartProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.ClientShoppingCart != null).ToList();
                    reservation.ProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.Order != null).ToList();

                    reservation.TotalReservedUK = reservation.ProductReservationsUK.Sum(r => r.Qty);
                    reservation.TotalCartReservedUK = reservation.CartProductReservationsUK.Sum(r => r.Qty);

                    reservation.SupplyOrderUkraineCartItem =
                        _supplyUkraineRepositoriesFactory
                            .NewSupplyOrderUkraineCartItemRepository(connection)
                            .GetByProductIdIfReserved(
                                product.Id
                            );

                    reservation.DefectiveAvailabilities =
                        _productRepositoriesFactory
                            .NewProductAvailabilityRepository(connection)
                            .GetAllOnDefectiveStoragesByProductId(
                                product.Id
                            );
                }

                long StockStateStorageId = stockStateStorageRepository.Add(new StockStateStorage {
                    ChangeTypeOrderItem = ChangeTypeOrderItem.DepreciatedOrderFile,
                    TotalCartReservedUK = reservation.TotalCartReservedUK,
                    TotalReservedUK = reservation.TotalReservedUK,
                    ProductId = product.Id,
                    QtyHistory = orderItem.Qty
                });

                foreach (ProductAvailability productAvailabilities in productPlacement.ProductAvailabilities) {
                    long ProductAvailabilityDataHistoryId = productAvailabilityDataHistoryRepository.Add(new ProductAvailabilityDataHistory {
                        StorageId = productAvailabilities.StorageId,
                        StockStateStorageID = StockStateStorageId,
                        Amount = productAvailabilities.Amount
                    });
                    foreach (ProductPlacement placement in productAvailabilities.Storage.ProductPlacements) {
                        Product productBd = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetById(placement.ProductId);

                        ProductPlacementDataHistory productPlacementDataHistory = new() {
                            CellNumber = placement.CellNumber,
                            RowNumber = placement.RowNumber,
                            StorageNumber = placement.StorageNumber,
                            StorageId = placement.StorageId,
                            Qty = placement.Qty,
                            ConsignmentItemId = placement.ConsignmentItemId,
                            ProductId = placement.ProductId,
                            NameUA = productBd.NameUA,
                            VendorCode = productBd.VendorCode,
                            MainOriginalNumber = productBd.MainOriginalNumber,
                            ProductAvailabilityDataHistoryID = ProductAvailabilityDataHistoryId
                        };
                        if (placement.ConsignmentItem != null) productPlacementDataHistory.ConsignmentNumber = placement.ConsignmentItem.Consignment.ProductIncome.Number;

                        productPlacementDataHistoryRepository.Add(productPlacementDataHistory);
                    }
                }
            }
        });
    }
}