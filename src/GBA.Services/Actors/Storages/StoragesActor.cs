using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.ReSaleModels;
using GBA.Domain.Messages.Storages;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.ReSales.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Services.Actors.Storages;

public sealed class StoragesActor : ReceiveActor {
    private static readonly Regex SpecialCharactersReplace = new("[$&+,:;=?@#|/\\\\'\"�<>. ^*()%!\\-]", RegexOptions.Compiled);
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IReSaleRepositoriesFactory _reSaleRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public StoragesActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IStorageRepositoryFactory storageRepositoryFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IReSaleRepositoriesFactory reSaleRepositoriesFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _storageRepositoryFactory = storageRepositoryFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _reSaleRepositoriesFactory = reSaleRepositoriesFactory;

        Receive<GetAllStoragesMessage>(ProcessGetAllStoragesMessage);

        Receive<AddStorageMessage>(ProcessAddStorageMessage);

        Receive<GetStorageByNetIdMessage>(ProcessGetStorageByNetIdMessage);

        Receive<UpdateStorageMessage>(ProcessUpdateStorageMessage);

        Receive<GetAvailabileProductsByStorageNetIdFilteredMessage>(ProcessGetAvailableProductsByStorageNetIdFilteredMessage);

        Receive<GetIncomedProductsByStorageNetIdFilteredMessage>(ProcessGetIncomeProductsByStorageNetIdFilteredMessage);

        Receive<GetProductSupplyInfoByStorageFilteredMessage>(ProcessGetProductSupplyInfoByStorageFilteredMessage);

        Receive<GetAllNonDefectiveStoragesByLocaleMessage>(ProcessGetAllNonDefectiveStoragesByLocaleMessage);

        Receive<GetAllDefectiveStoragesByLocaleMessage>(ProcessGetAllDefectiveStoragesByLocaleMessage);

        Receive<DeleteStorageMessage>(ProcessDeleteStorageMessage);

        Receive<GetAllStoragesForReturnsMessage>(ProcessGetAllStoragesForReturnsMessage);

        Receive<GetAllStoragesForReturnsFilteredMessage>(ProcessGetAllStoragesForReturnsFilteredMessage);

        Receive<GetTotalProductsCountByStorageNetIdMessage>(ProcessGetTotalProductsCountByStorageNetIdMessage);

        Receive<ExportAllProductsByStorageDocumentMessage>(ProcessExportAllProductsByStorageDocumentMessage);

        Receive<GetAllStoragesWithOrganizationsMessage>(ProcessGetAllStoragesWithOrganizationsMessage);

        Receive<GetAllFilteredByOrganizationNetIdMessage>(ProcessGetAllFilteredByOrganizationNetIdMessage);

        Receive<GetAllForEcommerceMessage>(ProcessGetAllFroEcommerceMessage);

        Receive<SetStorageForEcommerceMessage>(ProcessSetStorageForEcommerceMessage);

        Receive<UnselectStorageForEcommerceMessage>(ProcessRemoveStorageForEcommerceMessage);

        Receive<SetStoragePriorityMessage>(ProcessSetStoragePriorityMessage);
    }

    private void ProcessSetStoragePriorityMessage(SetStoragePriorityMessage message) {
        try {
            if (message.Priority < 0) throw new Exception("InvalidPriorityValue");
            if (message.StorageId.Equals(0)) throw new Exception("Such storage doesn't exists in database");

            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);
            storageRepository.SetStoragePriority(message.StorageId, message.Priority);

            Sender.Tell(storageRepository.GetAllForEcommerce());
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessRemoveStorageForEcommerceMessage(UnselectStorageForEcommerceMessage message) {
        try {
            if (message.StorageNetId.Equals(Guid.Empty)) throw new Exception("Such storage doesn't exists in database");

            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);
            storageRepository.UnselectStorageForEcommerce(message.StorageNetId);

            Sender.Tell(storageRepository.GetAllForEcommerce().OrderBy(s => s.RetailPriority));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessSetStorageForEcommerceMessage(SetStorageForEcommerceMessage message) {
        try {
            if (message.StorageNetId.Equals(Guid.Empty)) throw new Exception("Such storage doesn't exists in database");

            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);

            IEnumerable<Storage> allForEcommerce = storageRepository.GetAllForEcommerce().ToList().OrderByDescending(s => s.RetailPriority);

            if (!allForEcommerce.Any()) {
                storageRepository.SetStorageForEcommerce(message.StorageNetId);
                Sender.Tell(storageRepository.GetAllForEcommerce());
            } else if (allForEcommerce.Any(s => s.NetUid.Equals(message.StorageNetId))) {
                Sender.Tell(allForEcommerce.OrderBy(s => s.RetailPriority));
            } else {
                Storage storage = storageRepository.GetByNetId(message.StorageNetId);
                storage.RetailPriority = allForEcommerce.First().RetailPriority + 1;
                storage.ForEcommerce = true;
                storageRepository.Update(storage);

                Sender.Tell(allForEcommerce.Append(storage).OrderBy(s => s.RetailPriority));
            }
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetAllFroEcommerceMessage(GetAllForEcommerceMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_storageRepositoryFactory.NewStorageRepository(connection).GetAllForEcommerce().OrderBy(s => s.RetailPriority));
    }

    private void ProcessGetAllStoragesMessage(GetAllStoragesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_storageRepositoryFactory.NewStorageRepository(connection).GetAll());
    }

    private void ProcessAddStorageMessage(AddStorageMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);

        message.Storage.OrganizationId = message.Storage?.Organization?.Id;

        message.Storage.Id = storageRepository.Add(message.Storage);

        Sender.Tell(storageRepository.GetById(message.Storage.Id));
    }

    private void ProcessGetStorageByNetIdMessage(GetStorageByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_storageRepositoryFactory.NewStorageRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessUpdateStorageMessage(UpdateStorageMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);

        message.Storage.OrganizationId = message.Storage?.Organization?.Id;

        if (!message.Storage.ForVatProducts)
            message.Storage.AvailableForReSale = false;

        storageRepository.Update(message.Storage);

        Sender.Tell(storageRepository.GetByNetId(message.Storage.NetUid));
    }

    private void ProcessGetAvailableProductsByStorageNetIdFilteredMessage(GetAvailabileProductsByStorageNetIdFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductAvailabilityRepository(connection)
                .GetAllByStorageNetIdFiltered(
                    message.StorageNetId,
                    message.Limit,
                    message.Offset,
                    SpecialCharactersReplace.Replace(message.Value, string.Empty)
                )
        );
    }

    private void ProcessGetIncomeProductsByStorageNetIdFilteredMessage(GetIncomedProductsByStorageNetIdFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetByNetId(message.StorageNetId);

        List<ProductStorageInfo> storageInfos = new();

        decimal totalAmount = decimal.Zero;
        decimal totalAmountLocal = decimal.Zero;
        decimal totalAmountFiltered = decimal.Zero;
        decimal totalAmountLocalFiltered = decimal.Zero;

        if (storage != null) {
            if (storage.Locale.ToLower().Equals("pl")) {
                Currency pln = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetPLNCurrencyIfExists();

                ExchangeRate euroExchangeRate =
                    _exchangeRateRepositoriesFactory
                        .NewExchangeRateRepository(connection)
                        .GetByCurrencyIdAndCode(
                            pln.Id,
                            "EUR",
                            DateTime.UtcNow.AddDays(-1)
                        );

                decimal exchangeRateAmount = euroExchangeRate?.Amount ?? 1m;

                IPackingListPackageOrderItemRepository packingListPackageOrderItemRepository =
                    _supplyRepositoriesFactory
                        .NewPackingListPackageOrderItemRepository(connection);

                List<PackingListPackageOrderItem> items =
                    packingListPackageOrderItemRepository
                        .GetAllPlacedItemsFiltered(
                            message.StorageNetId,
                            message.SupplierNetId,
                            message.Limit,
                            message.Offset,
                            SpecialCharactersReplace.Replace(message.Value, string.Empty),
                            message.From,
                            message.To
                        );

                totalAmountFiltered =
                    packingListPackageOrderItemRepository
                        .GetTotalEuroAmountForPlacedItemsFiltered(
                            message.StorageNetId,
                            message.SupplierNetId,
                            SpecialCharactersReplace.Replace(message.Value, string.Empty),
                            message.From,
                            message.To
                        );

                totalAmount =
                    packingListPackageOrderItemRepository
                        .GetTotalEuroAmountForPlacedItemsByStorage(message.StorageNetId);

                totalAmountLocalFiltered = decimal.Round(totalAmountFiltered * exchangeRateAmount, 2, MidpointRounding.AwayFromZero);

                totalAmountLocal = decimal.Round(totalAmount * exchangeRateAmount, 2, MidpointRounding.AwayFromZero);

                storageInfos.AddRange(items.Select(item => new ProductStorageInfo {
                    Product = item.SupplyInvoiceOrderItem.Product,
                    PackingListPackageOrderItem = item,
                    Supplier = item.Supplier,
                    ProductPlacements = item.SupplyInvoiceOrderItem.Product.ProductPlacements.ToList(),
                    Qty = item.Qty,
                    FromDate = item.PackingList.SupplyInvoice.DateFrom ?? item.PackingList.SupplyInvoice.Created,
                    RemainingQty = item.RemainingQty,
                    GrossWeight = Math.Round(item.GrossWeight, 3),
                    TotalGrossWeight = Math.Round(item.GrossWeight * item.Qty, 2),
                    UnitPrice = decimal.Round(item.GrossUnitPriceEur, 2, MidpointRounding.AwayFromZero),
                    TotalAmount = decimal.Round(item.GrossUnitPriceEur * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero),
                    UnitPriceLocal = decimal.Round(item.GrossUnitPriceEur * exchangeRateAmount, 2, MidpointRounding.AwayFromZero),
                    TotalAmountLocal = decimal.Round(item.GrossUnitPriceEur * exchangeRateAmount * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero)
                }));
            } else {
                ISupplyOrderUkraineItemRepository supplyOrderUkraineItemRepository =
                    _supplyUkraineRepositoriesFactory
                        .NewSupplyOrderUkraineItemRepository(connection);

                IEnumerable<SupplyOrderUkraineItem> items =
                    supplyOrderUkraineItemRepository
                        .GetAllPlacedItemsFiltered(
                            message.StorageNetId,
                            message.SupplierNetId,
                            message.Limit,
                            message.Offset,
                            SpecialCharactersReplace.Replace(message.Value, string.Empty),
                            message.From,
                            message.To
                        );

                totalAmountFiltered = totalAmountLocalFiltered =
                    supplyOrderUkraineItemRepository
                        .GetTotalEuroAmountForPlacedItemsFiltered(
                            message.StorageNetId,
                            message.SupplierNetId,
                            SpecialCharactersReplace.Replace(message.Value, string.Empty),
                            message.From,
                            message.To
                        );

                totalAmount = totalAmountLocal =
                    supplyOrderUkraineItemRepository
                        .GetTotalEuroAmountForPlacedItemsByStorage(message.StorageNetId);

                foreach (SupplyOrderUkraineItem item in items) {
                    item.Qty = item.ProductIncomeItems.Sum(i => i.Qty);

                    storageInfos.Add(new ProductStorageInfo {
                        Product = item.Product,
                        PackingListPackageOrderItem = item.PackingListPackageOrderItem,
                        SupplyOrderUkraineItem = item,
                        Supplier = item.Supplier,
                        FromDate = item.SupplyOrderUkraine.FromDate,
                        ProductPlacements = item.Product.ProductPlacements.ToList(),
                        Qty = item.Qty,
                        RemainingQty = item.Qty,
                        GrossWeight = Math.Round(item.NetWeight, 3),
                        TotalGrossWeight = Math.Round(item.NetWeight * item.Qty, 2),
                        UnitPrice = decimal.Round(item.GrossUnitPrice, 2, MidpointRounding.AwayFromZero),
                        TotalAmount = decimal.Round(item.GrossUnitPrice * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero),
                        UnitPriceLocal = decimal.Round(item.GrossUnitPrice, 2, MidpointRounding.AwayFromZero),
                        TotalAmountLocal = decimal.Round(item.GrossUnitPrice * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero)
                    });
                }
            }
        }

        Sender.Tell(
            new {
                Collection = storageInfos,
                TotalAmount = totalAmount,
                TotalAmountLocal = totalAmountLocal,
                TotalAmountFiltered = totalAmountFiltered,
                TotalAmountLocalFiltered = totalAmountLocalFiltered
            }
        );
    }

    private void ProcessGetProductSupplyInfoByStorageFilteredMessage(GetProductSupplyInfoByStorageFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        List<ProductSupplyInfo> infos = new();

        Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetByNetId(message.StorageNetId);

        decimal totalAmount = decimal.Zero;
        decimal totalAmountLocal = decimal.Zero;
        decimal totalAmountFiltered = decimal.Zero;
        decimal totalAmountLocalFiltered = decimal.Zero;

        if (storage != null) {
            if (storage.Locale.ToLower().Equals("pl")) {
                ISupplyInvoiceRepository supplyInvoiceRepository = _supplyRepositoriesFactory.NewSupplyInvoiceRepository(connection);

                Currency pln = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetPLNCurrencyIfExists();

                ExchangeRate euroExchangeRate =
                    _exchangeRateRepositoriesFactory
                        .NewExchangeRateRepository(connection)
                        .GetByCurrencyIdAndCode(
                            pln.Id,
                            "EUR",
                            DateTime.UtcNow.AddDays(-1)
                        );

                decimal exchangeRateAmount = euroExchangeRate?.Amount ?? 1m;

                List<SupplyInvoice> supplyInvoices =
                    supplyInvoiceRepository
                        .GetAllIncomedInvoicesFiltered(
                            message.From,
                            message.To,
                            message.StorageNetId,
                            message.SupplierNetId,
                            message.Value,
                            message.Limit,
                            message.Offset
                        );

                infos.AddRange(supplyInvoices.Select(invoice => new ProductSupplyInfo {
                    FromDate = invoice.DateFrom ?? invoice.Created,
                    Supplier = invoice.SupplyOrder.Client,
                    SupplyInvoice = invoice,
                    TotalAmount = invoice.TotalGrossPrice,
                    TotalAmountLocal = decimal.Round(exchangeRateAmount * invoice.TotalGrossPrice, 2, MidpointRounding.AwayFromZero),
                    TotalGrossWeight = invoice.TotalGrossWeight
                }));

                totalAmountFiltered =
                    supplyInvoiceRepository
                        .GetTotalEuroAmountForPlacedItemsFiltered(
                            message.StorageNetId,
                            message.SupplierNetId,
                            message.Value,
                            message.From,
                            message.To
                        );

                totalAmount =
                    _supplyRepositoriesFactory
                        .NewPackingListPackageOrderItemRepository(connection)
                        .GetTotalEuroAmountForPlacedItemsByStorage(message.StorageNetId);

                totalAmountLocalFiltered = decimal.Round(totalAmountFiltered * exchangeRateAmount, 2, MidpointRounding.AwayFromZero);

                totalAmountLocal = decimal.Round(totalAmount * exchangeRateAmount, 2, MidpointRounding.AwayFromZero);
            } else {
                ISupplyOrderUkraineRepository supplyOrderUkraineRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineRepository(connection);

                List<SupplyOrderUkraine> ukraineOrders =
                    supplyOrderUkraineRepository
                        .GetAllIncomeOrdersFiltered(
                            message.From,
                            message.To,
                            message.StorageNetId,
                            message.SupplierNetId,
                            message.Value,
                            message.Limit,
                            message.Offset
                        );

                foreach (SupplyOrderUkraine order in ukraineOrders) {
                    foreach (SupplyOrderUkraineItem item in order.SupplyOrderUkraineItems) {
                        item.Qty = item.RemainingQty = item.ProductIncomeItems.Sum(i => i.RemainingQty);

                        item.TotalNetWeight = Math.Round(item.NetWeight * item.Qty, 3);

                        item.GrossPrice = decimal.Round(item.GrossUnitPrice * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero);

                        order.TotalNetWeight = Math.Round(order.TotalNetWeight + item.TotalNetWeight, 3);

                        order.TotalGrossPrice = decimal.Round(order.TotalGrossPrice + item.GrossPrice, 2, MidpointRounding.AwayFromZero);
                    }

                    infos.Add(new ProductSupplyInfo {
                        FromDate = order.FromDate,
                        Supplier = order.Supplier,
                        SupplyOrderUkraine = order,
                        TotalAmount = order.TotalGrossPrice,
                        TotalAmountLocal = order.TotalGrossPrice,
                        TotalGrossWeight = order.TotalNetWeight
                    });
                }

                totalAmountFiltered = totalAmountLocalFiltered =
                    supplyOrderUkraineRepository
                        .GetTotalEuroAmountForPlacedItemsFiltered(
                            message.StorageNetId,
                            message.SupplierNetId,
                            message.Value,
                            message.From,
                            message.To
                        );

                totalAmount = totalAmountLocal =
                    _supplyUkraineRepositoriesFactory
                        .NewSupplyOrderUkraineItemRepository(connection)
                        .GetTotalEuroAmountForPlacedItemsByStorage(message.StorageNetId);
            }
        }

        Sender.Tell(
            new {
                Collection = infos,
                TotalAmount = totalAmount,
                TotalAmountLocal = totalAmountLocal,
                TotalAmountFiltered = totalAmountFiltered,
                TotalAmountLocalFiltered = totalAmountLocalFiltered
            }
        );
    }

    private void ProcessGetAllNonDefectiveStoragesByLocaleMessage(GetAllNonDefectiveStoragesByLocaleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _storageRepositoryFactory
                .NewStorageRepository(connection)
                .GetAllNonDefectiveByCurrentLocale()
        );
    }

    private void ProcessGetAllDefectiveStoragesByLocaleMessage(GetAllDefectiveStoragesByLocaleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _storageRepositoryFactory
                .NewStorageRepository(connection)
                .GetAllDefectiveByCurrentLocale()
        );
    }

    private void ProcessDeleteStorageMessage(DeleteStorageMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _storageRepositoryFactory.NewStorageRepository(connection).Remove(message.NetId);
    }

    private void ProcessGetAllStoragesForReturnsMessage(GetAllStoragesForReturnsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_storageRepositoryFactory.NewStorageRepository(connection).GetAllForReturns(message.Status.Equals(SaleReturnItemStatus.Defect)));
    }

    private void ProcessGetAllStoragesForReturnsFilteredMessage(GetAllStoragesForReturnsFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();

        IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);
        IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

        if (message.OrderItemNetId.HasValue) {
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            OrderItem orderItem = orderItemRepository.GetByNetId(message.OrderItemNetId.Value);

            IEnumerable<ProductReservation> productReservations = productReservationRepository.GetAllByOrderItemIdWithAvailabilityAndReSaleAvailabilities(orderItem.Id);

            if (productReservations.First().IsReSaleReservation) {
                IReSaleAvailabilityRepository reSaleRepository = _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection);
                ReSaleAvailabilityWithTotalsModel reSaleAvailability = reSaleRepository.GetActualReSaleAvailabilityByProductId(orderItem.ProductId);

                double availableQty = reSaleAvailability.TotalQty;

                IEnumerable<Storage> storages = availableQty == 0
                    ? new[] { storageRepository.GetReSale() }
                    : storageRepository.GetAllForReturnsFiltered(
                        message.OrganizationNetId,
                        message.OrderItemNetId,
                        message.Status == SaleReturnItemStatus.Defect
                    );

                Sender.Tell(storages);
                return;
            }
        }

        IEnumerable<Storage> filteredStorages = storageRepository.GetAllForReturnsFiltered(
            message.OrganizationNetId,
            message.OrderItemNetId,
            message.Status == SaleReturnItemStatus.Defect
        );

        Sender.Tell(filteredStorages);
    }

    private void ProcessGetTotalProductsCountByStorageNetIdMessage(GetTotalProductsCountByStorageNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_storageRepositoryFactory.NewStorageRepository(connection).GetTotalProductsCountByStorageNetId(message.NetId));
    }

    private void ProcessExportAllProductsByStorageDocumentMessage(ExportAllProductsByStorageDocumentMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            List<ProductAvailability> productAvailabilities =
                _productRepositoriesFactory
                    .NewProductAvailabilityRepository(connection)
                    .GetAllProductsByStorageNetId(message.NetId);

            if (productAvailabilities == null)
                Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));

            (string excelFilePath, string pdfFilePath) =
                _xlsFactoryManager
                    .NewProductsXlsManager()
                    .ExportAllProductsByStorageToXlsx(
                        message.PathToFolder,
                        productAvailabilities
                    );

            Sender.Tell(new Tuple<string, string>(excelFilePath, pdfFilePath));
        } catch (Exception) {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }

    private void ProcessGetAllStoragesWithOrganizationsMessage(GetAllStoragesWithOrganizationsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _storageRepositoryFactory
                .NewStorageRepository(connection)
                .GetAllWithOrganizations()
        );
    }

    private void ProcessGetAllFilteredByOrganizationNetIdMessage(GetAllFilteredByOrganizationNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _storageRepositoryFactory
                .NewStorageRepository(connection)
                .GetAllFilteredByOrganizationNetId(
                    message.OrganizationNetId,
                    message.SkipDefective
                )
        );
    }
}