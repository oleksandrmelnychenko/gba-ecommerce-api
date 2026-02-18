using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Messages.Consignments;
using GBA.Domain.Messages.Products.Incomes;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Products;

public sealed class ProductIncomesActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public ProductIncomesActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IStorageRepositoryFactory storageRepositoryFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _storageRepositoryFactory = storageRepositoryFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;

        Receive<AddNewProductIncomeFromPackingListMessage>(ProcessAddNewProductIncomeFromPackingListMessage);

        Receive<AddNewProductIncomeFromSupplyOrderUkraineMessage>(ProcessAddNewProductIncomeFromSupplyOrderUkraineMessage);

        Receive<AddNewProductIncomeFromSupplyOrderUkraineDynamicPlacementsMessage>(ProcessAddNewProductIncomeFromSupplyOrderUkraineDynamicPlacementsMessage);

        Receive<AddNewProductIncomeFromPackingListDynamicPlacementsMessage>(ProcessAddNewProductIncomeFromPackingListDynamicPlacementsMessage);

        Receive<AddNewProductIncomeFromActReconciliationItemsMessage>(ProcessAddNewProductIncomeFromActReconciliationItemsMessage);

        Receive<AddNewProductIncomeFromActReconciliationItemMessage>(ProcessAddNewProductIncomeFromActReconciliationItemMessage);
    }

    private void ProcessAddNewProductIncomeFromPackingListMessage(AddNewProductIncomeFromPackingListMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
        IPackingListRepository packingListRepository = _supplyRepositoriesFactory.NewPackingListRepository(connection);
        ISupplyInvoiceRepository supplyInvoiceRepository = _supplyRepositoriesFactory.NewSupplyInvoiceRepository(connection);
        IPackingListPackageOrderItemRepository packingListPackageOrderItemRepository =
            _supplyRepositoriesFactory.NewPackingListPackageOrderItemRepository(connection);

        Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetByNetId(message.StorageNetId);

        if (storage == null) {
            Sender.Tell(new Tuple<ProductIncome, string>(null, ProductIncomeResourceNames.STORAGE_NOT_EXISTS));

            return;
        }

        PackingList packingList = packingListRepository.GetByNetId(message.PackingListNetId);

        if (packingList == null) {
            Sender.Tell(new Tuple<ProductIncome, string>(null, ProductIncomeResourceNames.ENTITY_NOT_EXISTS));

            return;
        }

        if (packingList.PackingListPackageOrderItems.Any(i => i.IsPlaced) ||
            !packingList.PackingListPallets.All(p => p.PackingListPackageOrderItems.All(i => !i.IsPlaced)) ||
            !packingList.PackingListBoxes.All(b => b.PackingListPackageOrderItems.All(i => !i.IsPlaced))) {
            Sender.Tell(new Tuple<ProductIncome, string>(null, ProductIncomeResourceNames.ALREADY_PLACED));

            return;
        }

        if (!packingList.PackingListPackageOrderItems.All(i => i.IsReadyToPlaced) ||
            !packingList.PackingListPallets.All(p => p.PackingListPackageOrderItems.All(i => i.IsReadyToPlaced)) ||
            !packingList.PackingListBoxes.All(b => b.PackingListPackageOrderItems.All(i => i.IsReadyToPlaced))) {
            Sender.Tell(new Tuple<ProductIncome, string>(null, ProductIncomeResourceNames.SHOULD_BE_READY_TO_BE_PLACED));

            return;
        }

        packingListRepository.SetPlaced(packingList.Id, true);

        SupplyOrder supplyOrder = supplyOrderRepository.GetByPackingListId(packingList.Id);

        bool isFullyPlaced = true;

        foreach (SupplyInvoice invoice in supplyOrder.SupplyInvoices) {
            SupplyInvoice supplyInvoice = supplyInvoiceRepository.GetByNetIdWithAllIncludes(invoice.NetUid);

            if (!supplyInvoice.PackingLists.All(p => p.IsPlaced)) isFullyPlaced = false;
        }

        supplyOrderRepository.SetPartiallyPlaced(supplyOrder.Id, true);
        supplyOrderRepository.SetFullyPlaced(supplyOrder.Id, isFullyPlaced);

        IProductIncomeRepository productIncomeRepository = _productRepositoriesFactory.NewProductIncomeRepository(connection);
        IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
        IProductIncomeItemRepository productIncomeItemRepository = _productRepositoriesFactory.NewProductIncomeItemRepository(connection);
        IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
        IActReconciliationItemRepository actReconciliationItemRepository =
            _supplyUkraineRepositoriesFactory.NewActReconciliationItemRepository(connection);
        ISupplyInvoiceOrderItemRepository supplyInvoiceOrderItemRepository = _supplyRepositoriesFactory.NewSupplyInvoiceOrderItemRepository(connection);

        User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

        ProductIncome productIncome = new() {
            StorageId = storage.Id,
            UserId = user.Id,
            FromDate =
                message.FromDate.Year.Equals(1)
                    ? DateTime.UtcNow
                    : TimeZoneInfo.ConvertTimeToUtc(message.FromDate),
            ProductIncomeType = ProductIncomeType.IncomePl
        };

        const string incomeLocale = "P";

        ProductIncome lastProductIncome = productIncomeRepository.GetLastByTypeAndPrefix(ProductIncomeType.IncomePl, incomeLocale);

        if (lastProductIncome != null && DateTime.Now.Year.Equals(lastProductIncome.Created.Year))
            productIncome.Number =
                $"{incomeLocale}{string.Format("{0:D10}", Convert.ToInt32(lastProductIncome.Number.Substring(incomeLocale.Length, 10)) + 1)}";
        else
            productIncome.Number =
                $"{incomeLocale}{string.Format("{0:D10}", 1)}";

        productIncome.Id = productIncomeRepository.Add(productIncome);

        foreach (PackingListPackageOrderItem item in packingList.PackingListPackageOrderItems) {
            ProductAvailability productAvailability =
                productAvailabilityRepository
                    .GetByProductAndStorageIds(
                        item.SupplyInvoiceOrderItem.ProductId,
                        storage.Id
                    );

            if (productAvailability == null) {
                productAvailability = new ProductAvailability {
                    Amount = item.Qty,
                    StorageId = storage.Id,
                    ProductId = item.SupplyInvoiceOrderItem.ProductId
                };

                productAvailability.Id = productAvailabilityRepository.AddWithId(productAvailability);
            } else {
                productAvailability.Amount += item.Qty;

                productAvailabilityRepository.Update(productAvailability);
            }

            item.PlacedQty += item.Qty;

            packingListPackageOrderItemRepository.UpdatePlacementInformation(item);

            if (item.SupplyInvoiceOrderItem == null) continue;

            SupplyInvoiceOrderItem invoiceItemFromDb = supplyInvoiceOrderItemRepository.GetById(item.SupplyInvoiceOrderItem.Id);

            if (invoiceItemFromDb == null) continue;

            ActReconciliationItem reconciliationItem =
                actReconciliationItemRepository
                    .GetBySupplyInvoiceOrderItemId(
                        invoiceItemFromDb.Id
                    );

            if (reconciliationItem == null) continue;

            reconciliationItem.HasDifference =
                !invoiceItemFromDb.Qty.Equals(invoiceItemFromDb.PackingListPackageOrderItems.Sum(p => p.PlacedQty));
            reconciliationItem.NegativeDifference = reconciliationItem.HasDifference;
            reconciliationItem.QtyDifference = invoiceItemFromDb.Qty - invoiceItemFromDb.PackingListPackageOrderItems.Sum(p => p.PlacedQty);
            reconciliationItem.ActualQty = invoiceItemFromDb.PackingListPackageOrderItems.Sum(p => p.PlacedQty);
            reconciliationItem.OrderedQty = invoiceItemFromDb.Qty;

            actReconciliationItemRepository.Update(reconciliationItem);

            ProductIncomeItem incomeItem = new() {
                PackingListPackageOrderItemId = item.Id,
                ProductIncomeId = productIncome.Id,
                Qty = item.Qty,
                RemainingQty = item.Qty
            };

            incomeItem.Id = productIncomeItemRepository.Add(incomeItem);

            productPlacementRepository.Add(new ProductPlacement {
                StorageNumber = "N",
                RowNumber = "N",
                CellNumber = "N",
                Qty = item.Qty,
                StorageId = storage.Id,
                ProductIncomeItemId = incomeItem.Id,
                ProductId = item.SupplyInvoiceOrderItem.ProductId
            });
        }

        packingListPackageOrderItemRepository
            .SetIsPlacedByIds(
                packingList
                    .PackingListPackageOrderItems
                    .Select(item => item.Id),
                true
            );

        foreach (PackingListPackage box in packingList.PackingListBoxes) {
            productIncomeItemRepository
                .Add(
                    box
                        .PackingListPackageOrderItems
                        .Select(item => {
                            ProductAvailability productAvailability =
                                productAvailabilityRepository
                                    .GetByProductAndStorageIds(
                                        item.SupplyInvoiceOrderItem.ProductId,
                                        storage.Id
                                    );

                            if (productAvailability == null) {
                                productAvailability = new ProductAvailability {
                                    Amount = item.Qty,
                                    StorageId = storage.Id,
                                    ProductId = item.SupplyInvoiceOrderItem.ProductId
                                };

                                productAvailabilityRepository.Add(productAvailability);
                            } else {
                                productAvailability.Amount += item.Qty;

                                productAvailabilityRepository.Update(productAvailability);
                            }

                            return new ProductIncomeItem {
                                ProductIncomeId = productIncome.Id,
                                PackingListPackageOrderItemId = item.Id,
                                Qty = item.Qty
                            };
                        })
                );

            packingListPackageOrderItemRepository
                .SetIsPlacedByIds(
                    box
                        .PackingListPackageOrderItems
                        .Select(item => item.Id),
                    true
                );
        }

        foreach (PackingListPackage pallet in packingList.PackingListPallets) {
            productIncomeItemRepository
                .Add(
                    pallet
                        .PackingListPackageOrderItems
                        .Select(item => {
                            ProductAvailability productAvailability =
                                productAvailabilityRepository
                                    .GetByProductAndStorageIds(
                                        item.SupplyInvoiceOrderItem.ProductId,
                                        storage.Id
                                    );

                            if (productAvailability == null) {
                                productAvailability = new ProductAvailability {
                                    Amount = item.Qty,
                                    StorageId = storage.Id,
                                    ProductId = item.SupplyInvoiceOrderItem.ProductId
                                };

                                productAvailabilityRepository.Add(productAvailability);
                            } else {
                                productAvailability.Amount += item.Qty;

                                productAvailabilityRepository.Update(productAvailability);
                            }

                            return new ProductIncomeItem {
                                ProductIncomeId = productIncome.Id,
                                PackingListPackageOrderItemId = item.Id,
                                Qty = item.Qty
                            };
                        })
                );

            packingListPackageOrderItemRepository
                .SetIsPlacedByIds(
                    pallet
                        .PackingListPackageOrderItems
                        .Select(item => item.Id),
                    true
                );
        }

        packingList = packingListRepository.GetById(packingList.Id);

        if (packingList?.SupplyInvoice?.SupplyOrder != null) {
            supplyOrderRepository.SetPartiallyPlaced(packingList.SupplyInvoice.SupplyOrder.Id, true);

            List<SupplyInvoice> invoices =
                supplyInvoiceRepository
                    .GetAllBySupplyOrderIdWithPackingLists(
                        packingList.SupplyInvoice.SupplyOrder.Id
                    );

            foreach (SupplyInvoice invoice in invoices) {
                invoice.IsPartiallyPlaced = invoice.PackingLists.All(p => p.IsPlaced || p.PackingListPackageOrderItems.Any(i => i.PlacedQty > 0));
                invoice.IsFullyPlaced = invoice.PackingLists.All(p => p.IsPlaced);
            }

            supplyInvoiceRepository.UpdatePlacementInfo(invoices);

            packingList.SupplyInvoice.SupplyOrder.IsPartiallyPlaced = invoices.Any(i => i.IsPartiallyPlaced);
            packingList.SupplyInvoice.SupplyOrder.IsFullyPlaced = invoices.All(i => i.IsFullyPlaced);

            if (packingList.SupplyInvoice.SupplyOrder.IsFullyPlaced)
                supplyOrderRepository.SetFullyPlaced(packingList.SupplyInvoice.SupplyOrder.Id, true);

            if (packingList.SupplyInvoice.DeliveryProductProtocol != null) {
                packingList.SupplyInvoice.DeliveryProductProtocol.IsPartiallyPlaced = invoices.Any(i => i.IsPartiallyPlaced);
                packingList.SupplyInvoice.DeliveryProductProtocol.IsPlaced = invoices.All(i => i.IsFullyPlaced);

                _supplyRepositoriesFactory
                    .NewDeliveryProductProtocolRepository(connection)
                    .SetFullyAndPartialPlacedPlaced(packingList.SupplyInvoice.DeliveryProductProtocol);
            }
        }

        ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new AddNewConsignmentMessage(productIncome.Id, false));

        Sender.Tell(new Tuple<ProductIncome, string>(productIncomeRepository.GetById(productIncome.Id), string.Empty));
    }

    private void ProcessAddNewProductIncomeFromSupplyOrderUkraineMessage(AddNewProductIncomeFromSupplyOrderUkraineMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetByNetId(message.StorageNetId);

            if (storage == null) {
                Sender.Tell(new Tuple<SupplyOrderUkraine, string>(null, ProductIncomeResourceNames.STORAGE_NOT_EXISTS));

                return;
            }

            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            IProductIncomeRepository productIncomeRepository = _productRepositoriesFactory.NewProductIncomeRepository(connection);
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IProductIncomeItemRepository productIncomeItemRepository = _productRepositoriesFactory.NewProductIncomeItemRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            ISupplyOrderUkraineItemRepository supplyOrderUkraineItemRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineItemRepository(connection);

            if (message.SupplyOrderUkraine.IsPlaced) {
                Sender.Tell(new Tuple<SupplyOrderUkraine, string>(null, ProductIncomeResourceNames.ALREADY_PLACED));

                return;
            }

            if (message.SupplyOrderUkraine.IsNew()) {
                Sender.Tell(new Tuple<SupplyOrderUkraine, string>(message.SupplyOrderUkraine, string.Empty));

                return;
            }

            User user = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

            if (message
                .SupplyOrderUkraine
                .SupplyOrderUkraineItems
                .Any(i => !i.IsNew() && i.ProductPlacements.Any(p => p.IsNew()) && !i.NotOrdered)) {
                foreach (SupplyOrderUkraineItem item in message
                             .SupplyOrderUkraine
                             .SupplyOrderUkraineItems
                             .Where(i => !i.IsNew() && i.ProductPlacements.Any(p => p.IsNew()) && !i.NotOrdered))
                    if (!item
                            .ProductPlacements
                            .Where(p => p.IsNew())
                            .All(p => !string.IsNullOrEmpty(p.StorageNumber) && !string.IsNullOrEmpty(p.CellNumber))) {
                        throw new LocalizedException(
                            ProductIncomeResourceNames.PRODUCT_PLACEMENT_INFO,
                            new object[] { item.Product?.VendorCode ?? "" });
                    } else {
                        SupplyOrderUkraineItem fromDbItem = supplyOrderUkraineItemRepository.GetById(item.Id);

                        fromDbItem.QtyDifferent = fromDbItem.Qty - fromDbItem.PlacedQty;

                        item.ToIncomeQty = item.ProductPlacements.Where(p => p.IsNew()).Sum(p => p.Qty);

                        if (item.ToIncomeQty > fromDbItem.QtyDifferent)
                            throw new LocalizedException(
                                ProductIncomeResourceNames.SPECIFY_QTY_MORE_PRODUCT_AVAILABILITY,
                                new object[] { item.Product?.VendorCode, fromDbItem.QtyDifferent, item.ToIncomeQty }
                            );
                    }

                ProductIncome productIncome = new() {
                    StorageId = storage.Id,
                    UserId = user.Id,
                    FromDate = message.FromDate.Year.Equals(1) ? DateTime.UtcNow : TimeZoneInfo.ConvertTimeToUtc(message.FromDate),
                    ProductIncomeType = ProductIncomeType.IncomeUk
                };

                string prefix = message.SupplyOrderUkraine?.Organization?.Code ?? string.Empty;

                ProductIncome lastProductIncome = productIncomeRepository.GetLastByTypeAndPrefix(ProductIncomeType.IncomeUk, prefix);

                if (lastProductIncome != null && DateTime.Now.Year.Equals(lastProductIncome.Created.Year))
                    productIncome.Number =
                        $"{prefix}{string.Format("{0:D10}", Convert.ToInt32(lastProductIncome.Number.Substring(prefix.Length, 10)) + 1)}";
                else
                    productIncome.Number =
                        $"{prefix}{string.Format("{0:D10}", 1)}";

                productIncome.Id = productIncomeRepository.Add(productIncome);

                foreach (SupplyOrderUkraineItem item in message
                             .SupplyOrderUkraine
                             .SupplyOrderUkraineItems
                             .Where(i => !i.IsNew() && i.ProductPlacements.Any(p => p.IsNew()) && !i.NotOrdered)) {
                    SupplyOrderUkraineItem fromDbItem = supplyOrderUkraineItemRepository.GetById(item.Id);

                    if (fromDbItem == null) continue;

                    fromDbItem.QtyDifferent = fromDbItem.Qty - fromDbItem.PlacedQty;

                    if (fromDbItem.QtyDifferent <= 0) continue;

                    item.ToIncomeQty = item.ProductPlacements.Where(p => p.IsNew()).Sum(p => p.Qty);

                    fromDbItem.PlacedQty += item.ToIncomeQty;

                    fromDbItem.RemainingQty += item.ToIncomeQty;

                    fromDbItem.IsFullyPlaced = fromDbItem.Qty.Equals(fromDbItem.PlacedQty);

                    supplyOrderUkraineItemRepository.UpdatePlacementInformation(fromDbItem);

                    ProductIncomeItem incomeItem = new() {
                        ProductIncomeId = productIncome.Id,
                        SupplyOrderUkraineItemId = fromDbItem.Id,
                        Qty = item.ToIncomeQty,
                        RemainingQty = item.ToIncomeQty
                    };

                    incomeItem.Id = productIncomeItemRepository.Add(incomeItem);

                    productPlacementRepository
                        .Add(
                            item
                                .ProductPlacements
                                .Where(p => p.IsNew())
                                .Select(placement => {
                                    productPlacementRepository
                                        .Add(new ProductPlacement {
                                            ProductId = placement.ProductId,
                                            StorageId = placement.StorageId,
                                            Qty = placement.Qty,
                                            ProductIncomeItemId = incomeItem.Id,
                                            StorageNumber = placement.StorageNumber,
                                            CellNumber = placement.CellNumber,
                                            RowNumber = placement.RowNumber
                                        });

                                    placement.ProductId = fromDbItem.ProductId;
                                    placement.StorageId = storage.Id;
                                    placement.SupplyOrderUkraineItemId = fromDbItem.Id;

                                    return placement;
                                })
                        );

                    ProductAvailability productAvailability =
                        productAvailabilityRepository
                            .GetByProductAndStorageIds(
                                fromDbItem.ProductId,
                                storage.Id
                            );

                    if (productAvailability == null) {
                        productAvailability = new ProductAvailability {
                            Amount = item.ToIncomeQty,
                            StorageId = storage.Id,
                            ProductId = fromDbItem.ProductId
                        };

                        productAvailabilityRepository.Add(productAvailability);
                    } else {
                        productAvailability.Amount += item.ToIncomeQty;

                        productAvailabilityRepository.Update(productAvailability);
                    }
                }

                ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new AddNewConsignmentMessage(productIncome.Id, false));
            }

            supplyOrderUkraineItemRepository
                .RemoveAllByIds(
                    message
                        .SupplyOrderUkraine
                        .SupplyOrderUkraineItems
                        .Where(i => !i.IsNew() && i.Deleted && i.NotOrdered)
                        .Select(i => i.Id)
                );

            SupplyOrderUkraine orderFromDb =
                _supplyUkraineRepositoriesFactory
                    .NewSupplyOrderUkraineRepository(connection)
                    .GetByNetId(
                        message.SupplyOrderUkraine.NetUid
                    );

            foreach (SupplyOrderUkraineItem item in message
                         .SupplyOrderUkraine
                         .SupplyOrderUkraineItems
                         .Where(i => i.IsNew() && i.Product != null && !i.Product.IsNew() && !i.Deleted && i.Qty > 0)) {
                item.NotOrdered = true;
                item.ProductId = item.Product.Id;
                item.SupplyOrderUkraineId = message.SupplyOrderUkraine.Id;

                if (orderFromDb.SupplyOrderUkraineItems.Any(i => !i.IsNew() && i.NotOrdered && i.ProductId.Equals(item.ProductId))) {
                    SupplyOrderUkraineItem existingItem =
                        orderFromDb.SupplyOrderUkraineItems.First(i => !i.IsNew() && i.NotOrdered && i.ProductId.Equals(item.ProductId));

                    existingItem.Qty += item.Qty;

                    supplyOrderUkraineItemRepository.Update(existingItem);

                    item.Id = existingItem.Id;
                } else {
                    item.Id = supplyOrderUkraineItemRepository.Add(item);
                }

                Product product = getSingleProductRepository.GetByIdAndRuleLocaleWithProductGroupAndWriteOffRules(item.ProductId, "pl");

                ProductAvailability productAvailability =
                    productAvailabilityRepository
                        .GetByProductIdForCulture(
                            product.Id,
                            "pl"
                        );

                if (productAvailability == null || productAvailability.Amount <= 0) continue;

                if (productAvailability.Amount >= item.Qty)
                    productAvailability.Amount = Math.Round(productAvailability.Amount - item.Qty, 2, MidpointRounding.AwayFromZero);
                else
                    productAvailability.Amount = 0;

                productAvailabilityRepository.Update(productAvailability);
            }

            message.SupplyOrderUkraine =
                _supplyUkraineRepositoriesFactory
                    .NewSupplyOrderUkraineRepository(connection)
                    .GetByNetId(
                        message.SupplyOrderUkraine.NetUid
                    );

            if (message.SupplyOrderUkraine.SupplyOrderUkraineItems.Where(i => !i.NotOrdered).All(i => i.IsFullyPlaced)) {
                message.SupplyOrderUkraine.IsPlaced = true;

                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineRepository(connection).UpdateIsPlaced(message.SupplyOrderUkraine);
            }

            Sender.Tell(
                new Tuple<SupplyOrderUkraine, string>(
                    message.SupplyOrderUkraine,
                    string.Empty
                )
            );
        } catch (LocalizedException ex) {
            Sender.Tell(ex);
        }
    }

    private void ProcessAddNewProductIncomeFromSupplyOrderUkraineDynamicPlacementsMessage(AddNewProductIncomeFromSupplyOrderUkraineDynamicPlacementsMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.SupplyOrderUkraine == null) throw new Exception(ProductIncomeResourceNames.EMPTY_ENTITY);

            if (message.SupplyOrderUkraine.IsNew()) throw new Exception(ProductIncomeResourceNames.NEW_ENTITY);

            ISupplyOrderUkraineRepository supplyOrderUkraineRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineRepository(connection);

            SupplyOrderUkraine fromDb = supplyOrderUkraineRepository.GetById(message.SupplyOrderUkraine.Id);

            if (fromDb == null) throw new Exception(ProductIncomeResourceNames.ENTITY_NOT_EXISTS);

            if (fromDb.IsPlaced) throw new Exception(ProductIncomeResourceNames.ALREADY_PLACED);

            if (!message.SupplyOrderUkraine.IsPlaced) {
                if (!message.SupplyOrderUkraine.DynamicProductPlacementColumns.Any()) throw new Exception(ProductIncomeResourceNames.NO_COLUMNS_FOR_PLACEMENT);

                if (!message
                        .SupplyOrderUkraine
                        .DynamicProductPlacementColumns
                        .Any(c => c.DynamicProductPlacementRows
                            .Any(r => r.DynamicProductPlacements
                                .Any(p => !p.IsNew() && !p.IsApplied))) && !message.SupplyOrderUkraine.IsPlaced)
                    throw new Exception(ProductIncomeResourceNames.NO_VALUE_FOR_PLACEMENT);
            }

            Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetByNetId(message.StorageNetId);

            if (storage == null) throw new Exception(ProductIncomeResourceNames.STORAGE_NOT_EXISTS);

            foreach (DynamicProductPlacementColumn column in message
                         .SupplyOrderUkraine
                         .DynamicProductPlacementColumns
                         .Where(c => c.DynamicProductPlacementRows
                             .Any(r => r.DynamicProductPlacements
                                 .Any(p => !p.IsNew() && !p.IsApplied))))
            foreach (DynamicProductPlacementRow row in column
                         .DynamicProductPlacementRows
                         .Where(r => r.DynamicProductPlacements
                             .Any(p => !p.IsNew() && !p.IsApplied)))
            foreach (DynamicProductPlacement placement in row.DynamicProductPlacements.Where(p => !p.IsNew() && !p.IsApplied)) {
                SupplyOrderUkraineItem itemFromDb =
                    fromDb.SupplyOrderUkraineItems.First(i => i.Id.Equals(row.SupplyOrderUkraineItemId));

                if (itemFromDb == null || itemFromDb.NotOrdered) continue;

                if (string.IsNullOrEmpty(placement.StorageNumber) || string.IsNullOrEmpty(placement.CellNumber))
                    throw new LocalizedException(
                        ProductIncomeResourceNames.SPECIFY_PLACEMENT_INFO_FOR_VENDOR_CODE,
                        itemFromDb.Product?.VendorCode ?? string.Empty
                    );

                itemFromDb.QtyDifferent = itemFromDb.Qty - itemFromDb.PlacedQty;

                if (itemFromDb.QtyDifferent < placement.Qty)
                    throw new LocalizedException(
                        ProductIncomeResourceNames.SPECIFIED_MORE_QTY_THAN_AVAILABLE,
                        itemFromDb.Product?.VendorCode ?? string.Empty
                    );
            }

            if (message.SupplyOrderUkraine.Organization != null && !fromDb.OrganizationId.Equals(message.SupplyOrderUkraine.Organization.Id)) {
                fromDb.OrganizationId = message.SupplyOrderUkraine.Organization.Id;

                supplyOrderUkraineRepository.UpdateOrganization(fromDb);
            }

            IProductIncomeRepository productIncomeRepository = _productRepositoriesFactory.NewProductIncomeRepository(connection);
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IProductIncomeItemRepository productIncomeItemRepository = _productRepositoriesFactory.NewProductIncomeItemRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IActReconciliationItemRepository actReconciliationItemRepository = _supplyUkraineRepositoriesFactory.NewActReconciliationItemRepository(connection);
            ISupplyOrderUkraineItemRepository supplyOrderUkraineItemRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineItemRepository(connection);
            IDynamicProductPlacementRepository dynamicProductPlacementRepository = _supplyUkraineRepositoriesFactory.NewDynamicProductPlacementRepository(connection);

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            if (message
                .SupplyOrderUkraine
                .DynamicProductPlacementColumns
                .Any(c => c.DynamicProductPlacementRows
                    .Any(r => r.DynamicProductPlacements
                        .Any(p => !p.IsNew() && !p.IsApplied)))) {
                ProductIncome productIncome = new() {
                    StorageId = storage.Id,
                    UserId = user.Id,
                    FromDate = message.FromDate.Year.Equals(1) ? DateTime.UtcNow : TimeZoneInfo.ConvertTimeToUtc(message.FromDate),
                    ProductIncomeType = ProductIncomeType.IncomeUk
                };

                string prefix = message.SupplyOrderUkraine?.Organization?.Code ?? string.Empty;

                ProductIncome lastProductIncome = productIncomeRepository.GetLastByTypeAndPrefix(ProductIncomeType.IncomeUk, prefix);

                if (lastProductIncome != null && DateTime.Now.Year.Equals(lastProductIncome.Created.Year))
                    productIncome.Number =
                        $"{prefix}{string.Format("{0:D10}", Convert.ToInt32(lastProductIncome.Number.Substring(prefix.Length, 10)) + 1)}";
                else
                    productIncome.Number =
                        $"{prefix}{string.Format("{0:D10}", 1)}";

                productIncome.Id = productIncomeRepository.Add(productIncome);

                foreach (DynamicProductPlacementColumn column in message
                             .SupplyOrderUkraine
                             .DynamicProductPlacementColumns
                             .Where(c => c.DynamicProductPlacementRows
                                 .Any(r => r.DynamicProductPlacements
                                     .Any(p => !p.IsNew() && !p.IsApplied))))
                foreach (DynamicProductPlacementRow row in column
                             .DynamicProductPlacementRows
                             .Where(r => r.DynamicProductPlacements
                                 .Any(p => !p.IsNew() && !p.IsApplied)))
                foreach (DynamicProductPlacement placement in row.DynamicProductPlacements.Where(p => !p.IsNew() && !p.IsApplied)) {
                    if (!row.SupplyOrderUkraineItemId.HasValue) continue;

                    SupplyOrderUkraineItem itemFromDb = supplyOrderUkraineItemRepository.GetById(row.SupplyOrderUkraineItemId.Value);

                    if (itemFromDb == null || itemFromDb.NotOrdered) continue;

                    if (fromDb.ClientAgreement != null)
                        fromDb.ClientAgreement.CurrentAmount -= Convert.ToDecimal(placement.Qty) * itemFromDb.UnitPriceLocal;

                    itemFromDb.PlacedQty += placement.Qty;
                    itemFromDb.RemainingQty += placement.Qty;
                    itemFromDb.IsFullyPlaced = itemFromDb.Qty.Equals(itemFromDb.PlacedQty);

                    supplyOrderUkraineItemRepository.UpdatePlacementInformation(itemFromDb);

                    dynamicProductPlacementRepository.SetIsAppliedById(placement.Id);

                    ProductAvailability productAvailability =
                        productAvailabilityRepository
                            .GetByProductAndStorageIds(
                                itemFromDb.ProductId,
                                storage.Id
                            );

                    if (productAvailability == null) {
                        productAvailability = new ProductAvailability {
                            Amount = placement.Qty,
                            StorageId = storage.Id,
                            ProductId = itemFromDb.ProductId
                        };

                        productAvailability.Id = productAvailabilityRepository.AddWithId(productAvailability);
                    } else {
                        productAvailability.Amount += placement.Qty;

                        productAvailabilityRepository.Update(productAvailability);
                    }

                    ActReconciliationItem reconciliationItem =
                        actReconciliationItemRepository
                            .GetBySupplyOrderUkraineItemId(
                                itemFromDb.Id
                            );

                    if (reconciliationItem != null) {
                        reconciliationItem.HasDifference = !itemFromDb.IsFullyPlaced;
                        reconciliationItem.NegativeDifference = !itemFromDb.NotOrdered && itemFromDb.Qty > itemFromDb.PlacedQty;
                        reconciliationItem.QtyDifference = itemFromDb.Qty - itemFromDb.PlacedQty;
                        reconciliationItem.ActualQty = itemFromDb.NotOrdered ? itemFromDb.Qty : itemFromDb.PlacedQty;
                        reconciliationItem.OrderedQty = itemFromDb.NotOrdered ? 0d : itemFromDb.Qty;

                        actReconciliationItemRepository.Update(reconciliationItem);
                    }

                    ProductIncomeItem existingIncomeItem =
                        productIncomeItemRepository
                            .GetByProductIncomeAndSupplyOrderUkraineItemIdsIfExists(
                                productIncome.Id,
                                itemFromDb.Id,
                                itemFromDb.PackingListPackageOrderItemId
                            );

                    if (existingIncomeItem != null) {
                        existingIncomeItem.Qty += placement.Qty;
                        existingIncomeItem.RemainingQty += placement.Qty;

                        productIncomeItemRepository.UpdateQtyFields(existingIncomeItem);

                        ProductPlacement productPlacement =
                            productPlacementRepository
                                .GetIfExists(
                                    placement.RowNumber,
                                    placement.CellNumber,
                                    placement.StorageNumber,
                                    itemFromDb.ProductId,
                                    storage.Id,
                                    existingIncomeItem.Id
                                );

                        if (productPlacement == null) {
                            productPlacementRepository
                                .Add(new ProductPlacement {
                                    ProductId = itemFromDb.ProductId,
                                    StorageId = storage.Id,
                                    Qty = placement.Qty,
                                    ProductIncomeItemId = existingIncomeItem.Id,
                                    StorageNumber = placement.StorageNumber,
                                    CellNumber = placement.CellNumber,
                                    RowNumber = placement.RowNumber
                                });
                        } else {
                            productPlacement.Qty += placement.Qty;

                            productPlacementRepository.UpdateQty(productPlacement);
                        }
                    } else {
                        ProductIncomeItem productIncomeItem = new() {
                            ProductIncomeId = productIncome.Id,
                            SupplyOrderUkraineItemId = itemFromDb.Id,
                            Qty = placement.Qty,
                            RemainingQty = placement.Qty
                        };

                        productIncomeItem.Id = productIncomeItemRepository.Add(productIncomeItem);

                        productPlacementRepository
                            .Add(new ProductPlacement {
                                ProductId = itemFromDb.ProductId,
                                StorageId = storage.Id,
                                Qty = placement.Qty,
                                ProductIncomeItemId = productIncomeItem.Id,
                                StorageNumber = placement.StorageNumber,
                                CellNumber = placement.CellNumber,
                                RowNumber = placement.RowNumber
                            });
                    }
                }

                ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new AddNewConsignmentMessage(productIncome.Id, false));
            }

            if (fromDb.ClientAgreement != null)
                _clientRepositoriesFactory.NewClientAgreementRepository(connection).UpdateAmountByNetId(
                    fromDb.ClientAgreement.NetUid, fromDb.ClientAgreement.CurrentAmount);

            fromDb =
                _supplyUkraineRepositoriesFactory
                    .NewSupplyOrderUkraineRepository(connection)
                    .GetByNetId(
                        fromDb.NetUid
                    );

            if (fromDb
                    .SupplyOrderUkraineItems
                    .Where(i => !i.NotOrdered)
                    .All(i => i.IsFullyPlaced) || message.SupplyOrderUkraine.IsPlaced) {
                fromDb = supplyOrderUkraineRepository.GetById(message.SupplyOrderUkraine.Id);

                fromDb.IsPlaced = true;

                supplyOrderUkraineRepository.UpdateIsPlaced(fromDb);
            }

            if (fromDb
                    .SupplyOrderUkraineItems
                    .Where(i => !i.NotOrdered)
                    .Any(i => i.PlacedQty > 0) || message.SupplyOrderUkraine.IsPlaced) {
                fromDb = supplyOrderUkraineRepository.GetById(message.SupplyOrderUkraine.Id);

                fromDb.IsPartialPlaced = true;

                supplyOrderUkraineRepository.UpdateIsPartialPlaced(fromDb);
            }

            Sender.Tell(
                supplyOrderUkraineRepository
                    .GetByNetId(
                        fromDb.NetUid
                    )
            );
        } catch (LocalizedException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessAddNewProductIncomeFromPackingListDynamicPlacementsMessage(AddNewProductIncomeFromPackingListDynamicPlacementsMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.PackingList == null) throw new Exception(ProductIncomeResourceNames.EMPTY_ENTITY);

            if (message.PackingList.IsNew()) throw new Exception(ProductIncomeResourceNames.NEW_ENTITY);

            IPackingListRepository packingListRepository = _supplyRepositoriesFactory.NewPackingListRepository(connection);

            PackingList fromDb = packingListRepository.GetById(message.PackingList.Id);

            if (fromDb == null) throw new Exception(ProductIncomeResourceNames.ENTITY_NOT_EXISTS);

            if (fromDb.IsPlaced) throw new Exception(ProductIncomeResourceNames.ALREADY_PLACED);

            if (!message.PackingList.DynamicProductPlacementColumns.Any()) throw new Exception(ProductIncomeResourceNames.NO_COLUMNS_FOR_PLACEMENT);

            if (!message
                    .PackingList
                    .DynamicProductPlacementColumns
                    .Any(c => c.DynamicProductPlacementRows
                        .Any(r => r.DynamicProductPlacements
                            .Any(p => !p.IsNew() && !p.IsApplied))) && !message.PackingList.IsPlaced)
                throw new Exception(ProductIncomeResourceNames.NO_VALUE_FOR_PLACEMENT);

            Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetByNetId(message.StorageNetId);

            if (storage == null) throw new Exception(ProductIncomeResourceNames.STORAGE_NOT_EXISTS);

            foreach (DynamicProductPlacementColumn column in message
                         .PackingList
                         .DynamicProductPlacementColumns
                         .Where(c => c.DynamicProductPlacementRows
                             .Any(r => r.DynamicProductPlacements
                                 .Any(p => !p.IsNew() && !p.IsApplied))))
            foreach (DynamicProductPlacementRow row in column
                         .DynamicProductPlacementRows
                         .Where(r => r.DynamicProductPlacements
                             .Any(p => !p.IsNew() && !p.IsApplied)))
            foreach (DynamicProductPlacement placement in row.DynamicProductPlacements.Where(p => !p.IsNew() && !p.IsApplied)) {
                PackingListPackageOrderItem itemFromDb =
                    fromDb.PackingListPackageOrderItems.First(i => i.Id.Equals(row.PackingListPackageOrderItemId));

                if (itemFromDb == null) continue;

                if (string.IsNullOrEmpty(placement.StorageNumber) || string.IsNullOrEmpty(placement.CellNumber))
                    throw new LocalizedException(
                        ProductIncomeResourceNames.SPECIFY_PLACEMENT_INFO_FOR_VENDOR_CODE,
                        itemFromDb.SupplyInvoiceOrderItem?.SupplyOrderItem?.Product?.VendorCode ?? string.Empty
                    );

                itemFromDb.QtyDifferent = itemFromDb.Qty - itemFromDb.PlacedQty;

                if (itemFromDb.QtyDifferent < placement.Qty)
                    throw new LocalizedException(
                        ProductIncomeResourceNames.SPECIFIED_MORE_QTY_THAN_AVAILABLE,
                        itemFromDb.SupplyInvoiceOrderItem?.SupplyOrderItem?.Product?.VendorCode ?? string.Empty
                    );
            }

            IProductIncomeRepository productIncomeRepository = _productRepositoriesFactory.NewProductIncomeRepository(connection);
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IProductIncomeItemRepository productIncomeItemRepository = _productRepositoriesFactory.NewProductIncomeItemRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IActReconciliationItemRepository actReconciliationItemRepository = _supplyUkraineRepositoriesFactory.NewActReconciliationItemRepository(connection);
            ISupplyInvoiceOrderItemRepository supplyInvoiceOrderItemRepository = _supplyRepositoriesFactory.NewSupplyInvoiceOrderItemRepository(connection);
            IDynamicProductPlacementRepository dynamicProductPlacementRepository = _supplyUkraineRepositoriesFactory.NewDynamicProductPlacementRepository(connection);
            IProductPlacementHistoryRepository productPlacementHistoryRepository = _productRepositoriesFactory.NewProductPlacementHistoryRepository(connection);
            IPackingListPackageOrderItemRepository packingListPackageOrderItemRepository =
                _supplyRepositoriesFactory.NewPackingListPackageOrderItemRepository(connection);
            IClientAgreementRepository clientAgreementRepository =
                _clientRepositoriesFactory.NewClientAgreementRepository(connection);

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            ClientAgreement supplierClientAgreement =
                clientAgreementRepository
                    .GetClientAgreementWithAgreementByPackingListId(message.PackingList.Id);

            if (message
                .PackingList
                .DynamicProductPlacementColumns
                .Any(c => c.DynamicProductPlacementRows
                    .Any(r => r.DynamicProductPlacements
                        .Any(p => !p.IsNew() && !p.IsApplied)))) {
                ProductIncome productIncome = new() {
                    StorageId = storage.Id,
                    UserId = user.Id,
                    FromDate = message.FromDate.Year.Equals(1) ? DateTime.UtcNow : TimeZoneInfo.ConvertTimeToUtc(message.FromDate),
                    ProductIncomeType = ProductIncomeType.IncomeUk
                };

                string prefix = fromDb.SupplyInvoice?.SupplyOrder?.Organization?.Code ?? string.Empty;

                ProductIncome lastProductIncome = productIncomeRepository.GetLastByTypeAndPrefix(ProductIncomeType.IncomeUk, prefix);

                if (lastProductIncome != null && DateTime.Now.Year.Equals(lastProductIncome.Created.Year))
                    productIncome.Number =
                        $"{prefix}{string.Format("{0:D10}", Convert.ToInt32(lastProductIncome.Number.Substring(prefix.Length, 10)) + 1)}";
                else
                    productIncome.Number =
                        $"{prefix}{string.Format("{0:D10}", 1)}";

                productIncome.Id = productIncomeRepository.Add(productIncome);

                foreach (DynamicProductPlacementColumn column in message
                             .PackingList
                             .DynamicProductPlacementColumns
                             .Where(c => c.DynamicProductPlacementRows
                                 .Any(r => r.DynamicProductPlacements
                                     .Any(p => !p.IsNew() && !p.IsApplied))))
                foreach (DynamicProductPlacementRow row in column
                             .DynamicProductPlacementRows
                             .Where(r => r.DynamicProductPlacements
                                 .Any(p => !p.IsNew() && !p.IsApplied)))
                foreach (DynamicProductPlacement placement in row.DynamicProductPlacements.Where(p => !p.IsNew() && !p.IsApplied)) {
                    if (!row.PackingListPackageOrderItemId.HasValue) continue;

                    PackingListPackageOrderItem itemFromDb =
                        packingListPackageOrderItemRepository.GetByIdWithIncludesForProduct(row.PackingListPackageOrderItemId ?? 0);

                    if (itemFromDb == null) continue;

                    if (supplierClientAgreement != null)
                        supplierClientAgreement.CurrentAmount -= Convert.ToDecimal(placement.Qty) * itemFromDb.UnitPrice;

                    itemFromDb.PlacedQty += placement.Qty;
                    itemFromDb.RemainingQty = itemFromDb.Qty - itemFromDb.PlacedQty;

                    packingListPackageOrderItemRepository.UpdatePlacementInformation(itemFromDb);

                    dynamicProductPlacementRepository.SetIsAppliedById(placement.Id);

                    ProductAvailability productAvailability =
                        productAvailabilityRepository
                            .GetByProductAndStorageIds(
                                itemFromDb.SupplyInvoiceOrderItem.ProductId,
                                storage.Id
                            );

                    if (productAvailability == null) {
                        productAvailability = new ProductAvailability {
                            Amount = placement.Qty,
                            StorageId = storage.Id,
                            ProductId = itemFromDb.SupplyInvoiceOrderItem.ProductId
                        };

                        productAvailability.Id = productAvailabilityRepository.AddWithId(productAvailability);
                    } else {
                        productAvailability.Amount += placement.Qty;

                        productAvailabilityRepository.Update(productAvailability);
                    }

                    ProductIncomeItem existingIncomeItem =
                        productIncomeItemRepository
                            .GetByProductIncomeAndPackingListPackageOrderItemIdsIfExists(
                                productIncome.Id,
                                itemFromDb.Id
                            );

                    if (existingIncomeItem != null) {
                        existingIncomeItem.Qty += placement.Qty;
                        existingIncomeItem.RemainingQty += placement.Qty;

                        productIncomeItemRepository.UpdateQtyFields(existingIncomeItem);

                        ProductPlacement productPlacement =
                            productPlacementRepository
                                .GetIfExists(
                                    placement.RowNumber,
                                    placement.CellNumber,
                                    placement.StorageNumber,
                                    itemFromDb.SupplyInvoiceOrderItem.ProductId,
                                    storage.Id,
                                    existingIncomeItem.Id
                                );

                        if (productPlacement == null) {
                            productPlacementRepository
                                .Add(new ProductPlacement {
                                    ProductId = itemFromDb.SupplyInvoiceOrderItem.ProductId,
                                    StorageId = storage.Id,
                                    Qty = placement.Qty,
                                    ProductIncomeItemId = existingIncomeItem.Id,
                                    StorageNumber = placement.StorageNumber,
                                    CellNumber = placement.CellNumber,
                                    RowNumber = placement.RowNumber
                                });

                            productPlacementHistoryRepository.Add(new ProductPlacementHistory {
                                Placement = placement.StorageNumber + "-" + placement.RowNumber + "-" + placement.CellNumber,
                                StorageId = storage.Id,
                                ProductId = itemFromDb.SupplyInvoiceOrderItem.ProductId,
                                Qty = placement.Qty,
                                StorageLocationType = StorageLocationType.SupplyOrder,
                                AdditionType = AdditionType.Add,
                                UserId = user.Id
                            });
                        } else {
                            // TODO Find out correct history values for update
                            // productPlacementHistoryRepository.Add(new ProductPlacementHistory {
                            //     Placement = placement.StorageNumber + "-" + placement.RowNumber + "-" + placement.CellNumber,
                            //     StorageId = storage.Id,
                            //     ProductId = itemFromDb.SupplyInvoiceOrderItem.ProductId,
                            //     Qty = placement.Qty,
                            //     StorageLocationType = StorageLocationType.SupplyOrder,
                            //     AdditionType = AdditionType.Add,
                            //     UserId = user.Id
                            // });

                            productPlacement.Qty += placement.Qty;

                            productPlacementRepository.UpdateQty(productPlacement);
                        }
                    } else {
                        ProductIncomeItem productIncomeItem = new() {
                            ProductIncomeId = productIncome.Id,
                            PackingListPackageOrderItemId = itemFromDb.Id,
                            Qty = placement.Qty,
                            RemainingQty = placement.Qty
                        };

                        productIncomeItem.Id = productIncomeItemRepository.Add(productIncomeItem);

                        productPlacementRepository
                            .Add(new ProductPlacement {
                                ProductId = itemFromDb.SupplyInvoiceOrderItem.ProductId,
                                StorageId = storage.Id,
                                Qty = placement.Qty,
                                ProductIncomeItemId = productIncomeItem.Id,
                                StorageNumber = placement.StorageNumber,
                                CellNumber = placement.CellNumber,
                                RowNumber = placement.RowNumber
                            });

                        productPlacementHistoryRepository.Add(new ProductPlacementHistory {
                            Placement = placement.StorageNumber + "-" + placement.RowNumber + "-" + placement.CellNumber,
                            StorageId = storage.Id,
                            ProductId = itemFromDb.SupplyInvoiceOrderItem.ProductId,
                            Qty = placement.Qty,
                            StorageLocationType = StorageLocationType.SupplyOrder,
                            AdditionType = AdditionType.Add,
                            UserId = user.Id
                        });
                    }

                    if (itemFromDb.SupplyInvoiceOrderItem == null) continue;

                    SupplyInvoiceOrderItem invoiceItemFromDb = supplyInvoiceOrderItemRepository.GetById(itemFromDb.SupplyInvoiceOrderItem.Id);

                    if (invoiceItemFromDb == null) continue;

                    ActReconciliationItem reconciliationItem =
                        actReconciliationItemRepository
                            .GetBySupplyInvoiceOrderItemId(
                                invoiceItemFromDb.Id
                            );

                    if (reconciliationItem == null) continue;

                    reconciliationItem.HasDifference =
                        !invoiceItemFromDb.Qty.Equals(invoiceItemFromDb.PackingListPackageOrderItems.Sum(p => p.PlacedQty));
                    reconciliationItem.NegativeDifference = reconciliationItem.HasDifference;
                    reconciliationItem.QtyDifference =
                        invoiceItemFromDb.Qty - invoiceItemFromDb.PackingListPackageOrderItems.Sum(p => p.PlacedQty);
                    reconciliationItem.ActualQty = invoiceItemFromDb.PackingListPackageOrderItems.Sum(p => p.PlacedQty);
                    reconciliationItem.OrderedQty = invoiceItemFromDb.Qty;

                    actReconciliationItemRepository.Update(reconciliationItem);
                }

                ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new AddNewConsignmentMessage(productIncome.Id, false));
            }

            if (supplierClientAgreement != null)
                clientAgreementRepository.UpdateAmountByNetId(
                    supplierClientAgreement.NetUid, supplierClientAgreement.CurrentAmount);

            fromDb =
                packingListRepository
                    .GetById(
                        fromDb.Id
                    );

            packingListPackageOrderItemRepository
                .SetIsPlacedOnlyByIds(
                    fromDb
                        .PackingListPackageOrderItems
                        .Where(i => i.PlacedQty >= i.Qty)
                        .Select(i => i.Id),
                    true
                );

            if (message.PackingList.IsPlaced || fromDb.PackingListPackageOrderItems.All(i => i.Qty.Equals(i.PlacedQty))) {
                fromDb.IsPlaced = true;

                packingListRepository.UpdateIsPlaced(fromDb);
            }

            if (fromDb.SupplyInvoice?.SupplyOrder != null) {
                ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
                ISupplyInvoiceRepository supplyInvoiceRepository = _supplyRepositoriesFactory.NewSupplyInvoiceRepository(connection);

                supplyOrderRepository.SetPartiallyPlaced(fromDb.SupplyInvoice.SupplyOrder.Id, true);

                List<SupplyInvoice> invoices =
                    supplyInvoiceRepository
                        .GetAllBySupplyOrderIdWithPackingLists(
                            fromDb.SupplyInvoice.SupplyOrder.Id
                        );

                foreach (SupplyInvoice invoice in invoices) {
                    invoice.IsPartiallyPlaced = invoice.PackingLists.All(p => p.IsPlaced || p.PackingListPackageOrderItems.Any(i => i.PlacedQty > 0));
                    invoice.IsFullyPlaced = invoice.PackingLists.All(p => p.IsPlaced);
                }

                supplyInvoiceRepository.UpdatePlacementInfo(invoices);

                fromDb.SupplyInvoice.SupplyOrder.IsPartiallyPlaced = invoices.Any(i => i.IsPartiallyPlaced);
                fromDb.SupplyInvoice.SupplyOrder.IsFullyPlaced = invoices.All(i => i.IsFullyPlaced);

                if (fromDb.SupplyInvoice.SupplyOrder.IsFullyPlaced) supplyOrderRepository.SetFullyPlaced(fromDb.SupplyInvoice.SupplyOrder.Id, true);
            }

            Sender.Tell(
                packingListRepository
                    .GetById(
                        fromDb.Id
                    )
            );
        } catch (LocalizedException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessAddNewProductIncomeFromActReconciliationItemsMessage(AddNewProductIncomeFromActReconciliationItemsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetByNetId(message.StorageNetId);

            if (storage == null) throw new Exception(ProductIncomeResourceNames.STORAGE_NOT_EXISTS);

            if (!message.Items.Any()) throw new Exception(ProductIncomeResourceNames.SPECIFY_ACT_RECONCILIATION_ITEMS);

            if (!message.Items.Any(i => i.ToOperationQty > 0)) throw new Exception(ProductIncomeResourceNames.NO_VALUE_FOR_PLACEMENT);

            IProductIncomeRepository productIncomeRepository = _productRepositoriesFactory.NewProductIncomeRepository(connection);
            IProductIncomeItemRepository productIncomeItemRepository = _productRepositoriesFactory.NewProductIncomeItemRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IActReconciliationItemRepository actReconciliationItemRepository = _supplyUkraineRepositoriesFactory.NewActReconciliationItemRepository(connection);
            IPackingListPackageOrderItemRepository packingListPackageOrderItemRepository =
                _supplyRepositoriesFactory.NewPackingListPackageOrderItemRepository(connection);
            ISupplyOrderUkraineItemRepository supplyOrderUkraineItemRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineItemRepository(connection);

            ActReconciliation actReconciliation =
                _supplyUkraineRepositoriesFactory
                    .NewActReconciliationRepository(connection)
                    .GetByIdIfExists(
                        message.Items.First().ActReconciliationId
                    );

            ProductIncome productIncome =
                new() {
                    StorageId = storage.Id,
                    UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id,
                    FromDate =
                        message.FromDate.Year.Equals(1)
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.FromDate),
                    ProductIncomeType = ProductIncomeType.IncomeUk,
                    Comment = message.Comment
                };

            string prefix = storage.Organization?.Code ?? string.Empty;

            ProductIncome lastProductIncome = productIncomeRepository.GetLastByTypeAndPrefix(ProductIncomeType.IncomeUk, prefix);

            if (lastProductIncome != null && DateTime.Now.Year.Equals(lastProductIncome.Created.Year))
                productIncome.Number =
                    $"{prefix}{string.Format("{0:D10}", Convert.ToInt32(lastProductIncome.Number.Substring(prefix.Length, 10)) + 1)}";
            else
                productIncome.Number =
                    $"{prefix}{string.Format("{0:D10}", 1)}";

            productIncome.Id = productIncomeRepository.Add(productIncome);

            productIncome = productIncomeRepository.GetById(productIncome.Id);

            if (actReconciliation == null || actReconciliation.SupplyOrderUkraineId.HasValue) {
                foreach (ActReconciliationItem listItem in message.Items) {
                    ActReconciliationItem item = actReconciliationItemRepository.GetById(listItem.Id);

                    if (item?.SupplyOrderUkraineItemId == null) continue;

                    if (item.QtyDifference < listItem.ToOperationQty) listItem.ToOperationQty = item.QtyDifference;

                    if (listItem.ToOperationQty.Equals(0d)) continue;

                    ProductIncomeItem incomeItem = new() {
                        ActReconciliationItemId = item.Id,
                        ProductIncomeId = productIncome.Id,
                        Qty = listItem.ToOperationQty,
                        RemainingQty = listItem.ToOperationQty,
                        SupplyOrderUkraineItemId = item.SupplyOrderUkraineItemId.Value
                    };

                    incomeItem.Id =
                        productIncomeItemRepository
                            .Add(
                                incomeItem
                            );

                    supplyOrderUkraineItemRepository
                        .IncreasePlacementInfoById(
                            item.SupplyOrderUkraineItemId.Value,
                            listItem.ToOperationQty
                        );

                    ProductAvailability productAvailability =
                        productAvailabilityRepository
                            .GetByProductAndStorageIds(
                                item.ProductId,
                                storage.Id
                            );

                    if (productAvailability == null) {
                        productAvailability = new ProductAvailability {
                            Amount = listItem.ToOperationQty,
                            StorageId = storage.Id,
                            ProductId = item.ProductId
                        };

                        productAvailability.Id = productAvailabilityRepository.AddWithId(productAvailability);
                    } else {
                        productAvailability.Amount += listItem.ToOperationQty;

                        productAvailabilityRepository.Update(productAvailability);
                    }

                    if (!storage.ForDefective) {
                        IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);

                        productPlacementRepository
                            .Add(new ProductPlacement {
                                ProductId = item.ProductId,
                                StorageId = storage.Id,
                                Qty = listItem.ToOperationQty,
                                ProductIncomeItemId = incomeItem.Id,
                                StorageNumber = "N",
                                CellNumber = "N",
                                RowNumber = "N"
                            });
                    }

                    if (!item.NegativeDifference) {
                        SupplyOrderUkraineItem notOrderedItem =
                            supplyOrderUkraineItemRepository
                                .GetNotOrderedItemByActReconciliationItemIdIfExists(
                                    item.Id
                                );

                        if (notOrderedItem != null) {
                            notOrderedItem.RemainingQty += listItem.ToOperationQty;

                            supplyOrderUkraineItemRepository
                                .UpdatePlacementInformation(
                                    notOrderedItem
                                );
                        }
                    }

                    item.QtyDifference -= listItem.ToOperationQty;

                    if (item.QtyDifference.Equals(0d)) {
                        item.HasDifference = false;
                        item.NegativeDifference = false;
                    }

                    actReconciliationItemRepository.Update(item);
                }
            } else {
                IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);

                foreach (ActReconciliationItem listItem in message.Items) {
                    ActReconciliationItem item = actReconciliationItemRepository.GetById(listItem.Id);

                    if (item?.SupplyInvoiceOrderItemId == null) continue;

                    if (item.QtyDifference < listItem.ToOperationQty) listItem.ToOperationQty = item.QtyDifference;

                    if (listItem.ToOperationQty.Equals(0d)) continue;

                    ProductAvailability productAvailability =
                        productAvailabilityRepository
                            .GetByProductAndStorageIds(
                                item.ProductId,
                                storage.Id
                            );

                    if (productAvailability == null) {
                        productAvailability = new ProductAvailability {
                            Amount = listItem.ToOperationQty,
                            StorageId = storage.Id,
                            ProductId = item.ProductId
                        };

                        productAvailability.Id = productAvailabilityRepository.AddWithId(productAvailability);
                    } else {
                        productAvailability.Amount += listItem.ToOperationQty;

                        productAvailabilityRepository.Update(productAvailability);
                    }

                    IEnumerable<PackingListPackageOrderItem> packListItems =
                        packingListPackageOrderItemRepository.GetAllNotPlacedBySupplyInvoiceOrderItemId(item.SupplyInvoiceOrderItemId.Value);

                    foreach (PackingListPackageOrderItem packListItem in packListItems) {
                        double unplacedQty = packListItem.Qty - packListItem.PlacedQty;

                        ProductIncomeItem existingIncomeItem =
                            productIncomeItemRepository
                                .GetByProductIncomeAndPackingListPackageOrderItemIdsIfExists(
                                    productIncome.Id,
                                    packListItem.Id
                                );

                        if (unplacedQty >= listItem.ToOperationQty) {
                            if (existingIncomeItem != null) {
                                existingIncomeItem.Qty += listItem.ToOperationQty;
                                existingIncomeItem.RemainingQty += listItem.ToOperationQty;

                                productIncomeItemRepository.UpdateQtyFields(existingIncomeItem);
                            } else {
                                productIncomeItemRepository
                                    .Add(
                                        new ProductIncomeItem {
                                            ActReconciliationItemId = item.Id,
                                            ProductIncomeId = productIncome.Id,
                                            PackingListPackageOrderItemId = packListItem.Id,
                                            Qty = listItem.ToOperationQty,
                                            RemainingQty = listItem.ToOperationQty
                                        }
                                    );
                            }

                            packingListPackageOrderItemRepository
                                .UpdatePlacementInformation(
                                    packListItem.Id,
                                    listItem.ToOperationQty
                                );

                            break;
                        }

                        if (existingIncomeItem != null) {
                            existingIncomeItem.Qty += unplacedQty;
                            existingIncomeItem.RemainingQty += unplacedQty;

                            productIncomeItemRepository.UpdateQtyFields(existingIncomeItem);

                            ProductPlacement productPlacement =
                                productPlacementRepository
                                    .GetIfExists(
                                        "N",
                                        "N",
                                        "N",
                                        listItem.ProductId,
                                        storage.Id,
                                        existingIncomeItem.Id
                                    );

                            if (productPlacement == null) {
                                productPlacementRepository
                                    .Add(new ProductPlacement {
                                        ProductId = listItem.ProductId,
                                        StorageId = storage.Id,
                                        Qty = unplacedQty,
                                        ProductIncomeItemId = existingIncomeItem.Id,
                                        StorageNumber = "N",
                                        CellNumber = "N",
                                        RowNumber = "N"
                                    });
                            } else {
                                productPlacement.Qty += unplacedQty;

                                productPlacementRepository.UpdateQty(productPlacement);
                            }
                        } else {
                            ProductIncomeItem productIncomeItem = new() {
                                ProductIncomeId = productIncome.Id,
                                PackingListPackageOrderItemId = packListItem.Id,
                                Qty = unplacedQty,
                                RemainingQty = unplacedQty
                            };

                            productIncomeItem.Id = productIncomeItemRepository.Add(productIncomeItem);

                            productPlacementRepository
                                .Add(new ProductPlacement {
                                    ProductId = listItem.ProductId,
                                    StorageId = storage.Id,
                                    Qty = unplacedQty,
                                    ProductIncomeItemId = productIncomeItem.Id,
                                    StorageNumber = "N",
                                    CellNumber = "N",
                                    RowNumber = "N"
                                });
                        }
                    }

                    if (!item.NegativeDifference) {
                        SupplyOrderUkraineItem notOrderedItem =
                            supplyOrderUkraineItemRepository
                                .GetNotOrderedItemByActReconciliationItemIdIfExists(
                                    item.Id
                                );

                        if (notOrderedItem != null) {
                            notOrderedItem.RemainingQty += listItem.ToOperationQty;

                            supplyOrderUkraineItemRepository
                                .UpdatePlacementInformation(
                                    notOrderedItem
                                );
                        }
                    }

                    item.QtyDifference -= listItem.ToOperationQty;

                    if (item.QtyDifference.Equals(0d)) {
                        item.HasDifference = false;
                        item.NegativeDifference = false;
                    }

                    actReconciliationItemRepository.Update(item);
                }
            }

            productIncome =
                productIncomeRepository
                    .GetByNetId(
                        productIncome.NetUid
                    );

            ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new AddNewConsignmentMessage(productIncome.Id, false));

            Sender.Tell(
                new {
                    ProductIncome = productIncome,
                    ActReconciliationItems =
                        actReconciliationItemRepository
                            .GetByIds(
                                message.Items.Select(i => i.Id)
                            )
                }
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessAddNewProductIncomeFromActReconciliationItemMessage(AddNewProductIncomeFromActReconciliationItemMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetByNetId(message.StorageNetId);

            if (storage == null)
                throw new Exception(ProductIncomeResourceNames.STORAGE_NOT_EXISTS);

            IActReconciliationItemRepository actReconciliationItemRepository = _supplyUkraineRepositoriesFactory.NewActReconciliationItemRepository(connection);

            ActReconciliationItem item = actReconciliationItemRepository.GetByNetId(message.ItemNetId);

            if (item == null)
                throw new Exception(ProductIncomeResourceNames.ENTITY_NOT_EXISTS);

            if (message.Qty <= 0)
                throw new Exception(ProductIncomeResourceNames.QTY_LESS_THAN_ZERO);

            if (item.QtyDifference < message.Qty)
                throw new Exception(ProductIncomeResourceNames.QTY_MORE_THAN_DIFFERENCE_QTY);

            if (!storage.ForDefective && (string.IsNullOrEmpty(message.StorageNumber) || string.IsNullOrEmpty(message.CellNumber)))
                throw new Exception(ProductIncomeResourceNames.SPECIFY_PLACEMENT_INFO);

            ActReconciliation actReconciliation =
                _supplyUkraineRepositoriesFactory
                    .NewActReconciliationRepository(connection)
                    .GetByIdIfExists(
                        item.ActReconciliationId
                    );

            IProductIncomeRepository productIncomeRepository = _productRepositoriesFactory.NewProductIncomeRepository(connection);
            IProductIncomeItemRepository productIncomeItemRepository = _productRepositoriesFactory.NewProductIncomeItemRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IPackingListPackageOrderItemRepository packingListPackageOrderItemRepository =
                _supplyRepositoriesFactory.NewPackingListPackageOrderItemRepository(connection);
            ISupplyOrderUkraineItemRepository supplyOrderUkraineItemRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineItemRepository(connection);

            ProductIncome productIncome = new() {
                StorageId = storage.Id,
                UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id,
                FromDate = message.FromDate.Year.Equals(1) ? DateTime.UtcNow : TimeZoneInfo.ConvertTimeToUtc(message.FromDate),
                ProductIncomeType = ProductIncomeType.IncomeUk,
                Comment = message.Comment
            };

            string prefix = storage.Organization?.Code ?? string.Empty;

            ProductIncome lastProductIncome = productIncomeRepository.GetLastByTypeAndPrefix(ProductIncomeType.IncomeUk, prefix);

            if (lastProductIncome != null && DateTime.Now.Year.Equals(lastProductIncome.Created.Year))
                productIncome.Number =
                    $"{prefix}{string.Format("{0:D10}", Convert.ToInt32(lastProductIncome.Number.Substring(prefix.Length, 10)) + 1)}";
            else
                productIncome.Number =
                    $"{prefix}{string.Format("{0:D10}", 1)}";

            productIncome.Id = productIncomeRepository.Add(productIncome);

            if (actReconciliation == null || actReconciliation.SupplyOrderUkraineId.HasValue) {
                ProductIncomeItem incomeItem = new() {
                    ActReconciliationItemId = item.Id,
                    ProductIncomeId = productIncome.Id,
                    Qty = message.Qty,
                    RemainingQty = message.Qty,
                    SupplyOrderUkraineItemId = item.SupplyOrderUkraineItemId
                };

                incomeItem.Id =
                    productIncomeItemRepository
                        .Add(
                            incomeItem
                        );

                if (item.SupplyOrderUkraineItemId.HasValue)
                    supplyOrderUkraineItemRepository
                        .IncreasePlacementInfoById(
                            item.SupplyOrderUkraineItemId.Value,
                            message.Qty
                        );

                ProductAvailability productAvailability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            item.ProductId,
                            storage.Id
                        );

                if (productAvailability == null) {
                    productAvailability = new ProductAvailability {
                        Amount = message.Qty,
                        StorageId = storage.Id,
                        ProductId = item.ProductId
                    };

                    productAvailability.Id = productAvailabilityRepository.AddWithId(productAvailability);
                } else {
                    productAvailability.Amount += message.Qty;

                    productAvailabilityRepository.Update(productAvailability);
                }

                if (!storage.ForDefective) {
                    IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);

                    productPlacementRepository
                        .Add(new ProductPlacement {
                            ProductId = item.ProductId,
                            StorageId = storage.Id,
                            Qty = message.Qty,
                            ProductIncomeItemId = incomeItem.Id,
                            StorageNumber = "N",
                            CellNumber = "N",
                            RowNumber = "N"
                        });
                }

                if (!item.NegativeDifference) {
                    SupplyOrderUkraineItem notOrderedItem =
                        supplyOrderUkraineItemRepository
                            .GetNotOrderedItemByActReconciliationItemIdIfExists(
                                item.Id
                            );

                    if (notOrderedItem != null) {
                        notOrderedItem.RemainingQty += message.Qty;

                        supplyOrderUkraineItemRepository
                            .UpdatePlacementInformation(
                                notOrderedItem
                            );
                    }
                }

                item.QtyDifference -= message.Qty;

                if (item.QtyDifference.Equals(0d)) {
                    item.HasDifference = false;
                    item.NegativeDifference = false;
                }

                actReconciliationItemRepository.Update(item);
            } else {
                ProductAvailability productAvailability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            item.ProductId,
                            storage.Id
                        );

                if (productAvailability == null) {
                    productAvailability = new ProductAvailability {
                        Amount = message.Qty,
                        StorageId = storage.Id,
                        ProductId = item.ProductId
                    };

                    productAvailability.Id = productAvailabilityRepository.AddWithId(productAvailability);
                } else {
                    productAvailability.Amount += message.Qty;

                    productAvailabilityRepository.Update(productAvailability);
                }

                if (item.SupplyInvoiceOrderItemId != null) {
                    IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);

                    IEnumerable<PackingListPackageOrderItem> packListItems =
                        packingListPackageOrderItemRepository.GetAllNotPlacedBySupplyInvoiceOrderItemId(item.SupplyInvoiceOrderItemId.Value);

                    foreach (PackingListPackageOrderItem packListItem in packListItems) {
                        double unplacedQty = packListItem.Qty - packListItem.PlacedQty;

                        ProductIncomeItem existingIncomeItem =
                            productIncomeItemRepository
                                .GetByProductIncomeAndPackingListPackageOrderItemIdsIfExists(
                                    productIncome.Id,
                                    packListItem.Id
                                );

                        if (unplacedQty >= message.Qty) {
                            if (existingIncomeItem != null) {
                                existingIncomeItem.Qty += message.Qty;
                                existingIncomeItem.RemainingQty += message.Qty;

                                productIncomeItemRepository.UpdateQtyFields(existingIncomeItem);
                            } else {
                                productIncomeItemRepository
                                    .Add(
                                        new ProductIncomeItem {
                                            ActReconciliationItemId = item.Id,
                                            ProductIncomeId = productIncome.Id,
                                            PackingListPackageOrderItemId = packListItem.Id,
                                            Qty = message.Qty,
                                            RemainingQty = message.Qty
                                        }
                                    );
                            }

                            packingListPackageOrderItemRepository
                                .UpdatePlacementInformation(
                                    packListItem.Id,
                                    message.Qty
                                );

                            break;
                        }

                        if (existingIncomeItem != null) {
                            existingIncomeItem.Qty += unplacedQty;
                            existingIncomeItem.RemainingQty += unplacedQty;

                            productIncomeItemRepository.UpdateQtyFields(existingIncomeItem);

                            ProductPlacement productPlacement =
                                productPlacementRepository
                                    .GetIfExists(
                                        "N",
                                        "N",
                                        "N",
                                        item.ProductId,
                                        storage.Id,
                                        existingIncomeItem.Id
                                    );

                            if (productPlacement == null) {
                                productPlacementRepository
                                    .Add(new ProductPlacement {
                                        ProductId = item.ProductId,
                                        StorageId = storage.Id,
                                        Qty = unplacedQty,
                                        ProductIncomeItemId = existingIncomeItem.Id,
                                        StorageNumber = "N",
                                        CellNumber = "N",
                                        RowNumber = "N"
                                    });
                            } else {
                                productPlacement.Qty += unplacedQty;

                                productPlacementRepository.UpdateQty(productPlacement);
                            }
                        } else {
                            ProductIncomeItem productIncomeItem = new() {
                                ProductIncomeId = productIncome.Id,
                                PackingListPackageOrderItemId = packListItem.Id,
                                Qty = unplacedQty,
                                RemainingQty = unplacedQty
                            };

                            productIncomeItem.Id = productIncomeItemRepository.Add(productIncomeItem);

                            productPlacementRepository
                                .Add(new ProductPlacement {
                                    ProductId = item.ProductId,
                                    StorageId = storage.Id,
                                    Qty = unplacedQty,
                                    ProductIncomeItemId = productIncomeItem.Id,
                                    StorageNumber = "N",
                                    CellNumber = "N",
                                    RowNumber = "N"
                                });
                        }
                    }
                }

                if (!item.NegativeDifference) {
                    SupplyOrderUkraineItem notOrderedItem =
                        supplyOrderUkraineItemRepository
                            .GetNotOrderedItemByActReconciliationItemIdIfExists(
                                item.Id
                            );

                    if (notOrderedItem != null) {
                        notOrderedItem.RemainingQty += message.Qty;

                        supplyOrderUkraineItemRepository
                            .UpdatePlacementInformation(
                                notOrderedItem
                            );
                    }
                }

                item.QtyDifference -= message.Qty;

                if (item.QtyDifference.Equals(0d)) {
                    item.HasDifference = false;
                    item.NegativeDifference = false;
                }

                actReconciliationItemRepository.Update(item);
            }

            productIncome =
                productIncomeRepository
                    .GetById(
                        productIncome.Id
                    );

            item =
                actReconciliationItemRepository
                    .GetByNetId(
                        message.ItemNetId
                    );

            ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new AddNewConsignmentMessage(productIncome.Id, false));

            Sender.Tell(
                new {
                    ProductIncome = productIncome,
                    ActReconciliationItem = item
                }
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}