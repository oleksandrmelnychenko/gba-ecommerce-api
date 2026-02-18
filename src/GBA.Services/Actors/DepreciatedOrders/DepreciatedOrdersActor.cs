using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.Helpers.DepreciatedOrders;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.DepreciatedOrderModels;
using GBA.Domain.Messages.Consignments;
using GBA.Domain.Messages.DepreciatedOrders;
using GBA.Domain.Repositories.DepreciatedOrders.Contracts;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.DepreciatedOrders;

public sealed class DepreciatedOrdersActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDepreciatedRepositoriesFactory _depreciatedRepositoriesFactory;
    private readonly IOrganizationRepositoriesFactory _organizationRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public DepreciatedOrdersActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IStorageRepositoryFactory storageRepositoryFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IDepreciatedRepositoriesFactory depreciatedRepositoriesFactory,
        IOrganizationRepositoriesFactory organizationRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _storageRepositoryFactory = storageRepositoryFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _depreciatedRepositoriesFactory = depreciatedRepositoriesFactory;
        _organizationRepositoriesFactory = organizationRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;

        Receive<AddNewDepreciatedOrderMessage>(ProcessAddNewDepreciatedOrderMessage);

        Receive<AddNewDepreciatedOrderFromActReconciliationItemsMessage>(ProcessAddNewDepreciatedOrderFromActReconciliationItemsMessage);

        Receive<AddNewDepreciatedOrderFromActReconciliationItemMessage>(ProcessAddNewDepreciatedOrderFromActReconciliationItemMessage);

        Receive<AddNewDepreciatedOrderFromPackingListMessage>(ProcessAddNewDepreciatedOrderFromPackingListMessage);

        Receive<AddNewDepreciatedOrderFromFileMessage>(ProcessAddNewDepreciatedOrderFromFile);
    }

    private void ProcessAddNewDepreciatedOrderMessage(AddNewDepreciatedOrderMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.DepreciatedOrder != null) {
                if (message.DepreciatedOrder.Storage != null && !message.DepreciatedOrder.Storage.IsNew()) {
                    if (message.DepreciatedOrder.Organization != null && !message.DepreciatedOrder.Organization.IsNew()) {
                        if (message.DepreciatedOrder.DepreciatedOrderItems.Any(i => i.Product != null && !i.Product.IsNew() && i.Qty > 0)) {
                            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
                            IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);
                            IDepreciatedOrderRepository depreciatedOrderRepository = _depreciatedRepositoriesFactory.NewDepreciatedOrderRepository(connection);
                            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
                            IDepreciatedOrderItemRepository depreciatedOrderItemRepository =
                                _depreciatedRepositoriesFactory.NewDepreciatedOrderItemRepository(connection);

                            Storage storage = storageRepository.GetById(message.DepreciatedOrder.Storage.Id);

                            foreach (DepreciatedOrderItem item in message
                                         .DepreciatedOrder
                                         .DepreciatedOrderItems
                                         .Where(i => i.Product != null && !i.Product.IsNew() && i.Qty > 0)) {
                                ProductAvailability availability =
                                    productAvailabilityRepository
                                        .GetByProductAndStorageIds(
                                            item.Product.Id,
                                            storage.Id
                                        );

                                if (availability == null)
                                    throw new LocalizedException(
                                        DepreciatedOrderResourceNames.PRODUCT_NOT_AVAILABLE_ON_STORAGE,
                                        new object[] { item.Product.VendorCode });

                                if (availability.Amount < item.Qty)
                                    throw new LocalizedException(
                                        DepreciatedOrderResourceNames.PRODUCT_AMOUNT_ON_STORAGE,
                                        new object[] { item.Product.VendorCode, availability.Amount });
                            }

                            message.DepreciatedOrder.ResponsibleId =
                                message.DepreciatedOrder.Responsible == null || message.DepreciatedOrder.Responsible.IsNew()
                                    ? userRepository.GetByNetIdWithoutIncludes(message.UserNetId).Id
                                    : message.DepreciatedOrder.Responsible.Id;

                            message.DepreciatedOrder.FromDate =
                                message.DepreciatedOrder.FromDate.Year.Equals(1)
                                    ? DateTime.UtcNow.Date
                                    : message.DepreciatedOrder.FromDate;

                            message.DepreciatedOrder.StorageId = message.DepreciatedOrder.Storage.Id;
                            message.DepreciatedOrder.OrganizationId = message.DepreciatedOrder.Organization.Id;

                            if (string.IsNullOrEmpty(message.DepreciatedOrder.Number)) {
                                DepreciatedOrder lastRecord = depreciatedOrderRepository.GetLastRecord(storage.Locale);

                                string locale =
                                    storage.Locale.ToLower().Equals("pl")
                                        ? "P"
                                        : string.Empty;

                                if (lastRecord != null && DateTime.Now.Year.Equals(lastRecord.Created.Year))
                                    message.DepreciatedOrder.Number =
                                        $"{locale}{string.Format("{0:D11}", Convert.ToInt32(lastRecord.Number.Substring(locale.Length, 11)) + 1)}";
                                else
                                    message.DepreciatedOrder.Number =
                                        $"{locale}{string.Format("{0:D11}", 1)}";
                            }

                            message.DepreciatedOrder.Id = depreciatedOrderRepository.Add(message.DepreciatedOrder);

                            Dictionary<long, long> depreciatedOrderProductAvailabilityIds = new();

                            foreach (DepreciatedOrderItem item in message
                                         .DepreciatedOrder
                                         .DepreciatedOrderItems
                                         .Where(i => i.Product != null && !i.Product.IsNew() && i.Qty > 0)) {
                                ProductAvailability availability =
                                    productAvailabilityRepository
                                        .GetByProductAndStorageIds(
                                            item.Product.Id,
                                            storage.Id
                                        );

                                availability.Amount -= item.Qty;

                                productAvailabilityRepository.Update(availability);

                                item.ProductId = item.Product.Id;
                                item.DepreciatedOrderId = message.DepreciatedOrder.Id;

                                item.Id = depreciatedOrderItemRepository.Add(item);

                                depreciatedOrderProductAvailabilityIds.Add(item.Id, availability.Id);
                            }

                            ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR)
                                .Tell(new StoreConsignmentFromDepreciatedOrderWithReSaleMessage(
                                    message.DepreciatedOrder.Id,
                                    depreciatedOrderProductAvailabilityIds,
                                    message.DepreciatedOrder.IsManagement));

                            Sender.Tell(depreciatedOrderRepository.GetById(message.DepreciatedOrder.Id));
                        } else {
                            throw new Exception(DepreciatedOrderResourceNames.NEED_ADD_ONE_ITEM);
                        }
                    } else {
                        throw new Exception(DepreciatedOrderResourceNames.NEED_SPECIFY_ORGANIZATION);
                    }
                } else {
                    throw new Exception(DepreciatedOrderResourceNames.NEED_SPECIFY_STORAGE);
                }
            } else {
                throw new Exception(DepreciatedOrderResourceNames.ENTITY_CAN_NOT_BE_EMPTY);
            }
        } catch (LocalizedException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessAddNewDepreciatedOrderFromActReconciliationItemsMessage(AddNewDepreciatedOrderFromActReconciliationItemsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            IActReconciliationItemRepository actReconciliationItemRepository = _supplyUkraineRepositoriesFactory.NewActReconciliationItemRepository(connection);

            if (message.Items.Any(i => i.ToOperationQty > 0)) {
                IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);

                Storage storage = storageRepository.GetByNetId(message.StorageNetId);

                Organization organization = _organizationRepositoriesFactory.NewOrganizationRepository(connection).GetByNetId(message.OrganizationNetId);

                if (storage == null) throw new Exception(DepreciatedOrderResourceNames.STORAGE_IS_NOT_EXIST);
                if (organization == null) throw new Exception(DepreciatedOrderResourceNames.ORGANIZATION_IS_NOT_EXIST);

                IDepreciatedOrderRepository depreciatedOrderRepository = _depreciatedRepositoriesFactory.NewDepreciatedOrderRepository(connection);

                DepreciatedOrder order = new() {
                    Comment = message.Comment,
                    FromDate = message.FromDate.Year.Equals(1) ? DateTime.UtcNow.Date : message.FromDate,
                    OrganizationId = organization.Id,
                    ResponsibleId = userRepository.GetByNetIdWithoutIncludes(message.UserNetId).Id,
                    StorageId = storage.Id
                };

                DepreciatedOrder lastRecord = depreciatedOrderRepository.GetLastRecord(storage.Locale);

                string locale =
                    storage.Locale.ToLower().Equals("pl")
                        ? "P"
                        : string.Empty;

                if (lastRecord != null && DateTime.Now.Year.Equals(lastRecord.Created.Year))
                    order.Number =
                        $"{locale}{string.Format("{0:D11}", Convert.ToInt32(lastRecord.Number.Substring(locale.Length, 11)) + 1)}";
                else
                    order.Number =
                        $"{locale}{string.Format("{0:D11}", 1)}";

                order.Id = depreciatedOrderRepository.Add(order);

                foreach (ActReconciliationItem listItem in message.Items.Where(i => i.ToOperationQty > 0)) {
                    ActReconciliationItem item =
                        actReconciliationItemRepository
                            .GetById(
                                listItem.Id
                            );

                    if (item == null) continue;

                    if (item.QtyDifference < listItem.ToOperationQty) listItem.ToOperationQty = item.QtyDifference;

                    if (listItem.ToOperationQty.Equals(0d)) continue;

                    DepreciatedOrderItem depreciatedOrderItem = new() {
                        DepreciatedOrderId = order.Id,
                        ProductId = item.ProductId,
                        ActReconciliationItemId = item.Id,
                        Qty = listItem.ToOperationQty,
                        Reason = listItem.Reason
                    };

                    depreciatedOrderItem.Id =
                        _depreciatedRepositoriesFactory
                            .NewDepreciatedOrderItemRepository(connection)
                            .Add(
                                depreciatedOrderItem
                            );

                    item.QtyDifference -= listItem.ToOperationQty;

                    if (item.QtyDifference.Equals(0d)) {
                        item.HasDifference = false;
                        item.NegativeDifference = false;
                    }

                    actReconciliationItemRepository.Update(item);
                }

                order =
                    depreciatedOrderRepository
                        .GetById(
                            order.Id
                        );

                ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new StoreConsignmentMovementFromDepreciatedOrderMessage(order.Id));

                Sender.Tell(
                    new {
                        ActReconciliationItems =
                            actReconciliationItemRepository
                                .GetByIds(
                                    message.Items.Select(i => i.Id)
                                ),
                        DepreciatedOrder = order
                    }
                );
            } else {
                throw new Exception(DepreciatedOrderResourceNames.NOT_PROVIDE_ACT_RECONCILIATION_ITEMS);
            }
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessAddNewDepreciatedOrderFromActReconciliationItemMessage(AddNewDepreciatedOrderFromActReconciliationItemMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            IActReconciliationItemRepository actReconciliationItemRepository = _supplyUkraineRepositoriesFactory.NewActReconciliationItemRepository(connection);

            ActReconciliationItem item =
                actReconciliationItemRepository
                    .GetByNetId(
                        message.ItemNetId
                    );

            if (item != null) {
                if (item.QtyDifference >= message.Qty) {
                    IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);

                    Storage storage = storageRepository.GetByNetId(message.StorageNetId);

                    if (storage != null) {
                        Organization organization = _organizationRepositoriesFactory.NewOrganizationRepository(connection).GetByNetId(message.OrganizationNetId);

                        if (organization != null) {
                            IDepreciatedOrderRepository depreciatedOrderRepository = _depreciatedRepositoriesFactory.NewDepreciatedOrderRepository(connection);

                            DepreciatedOrder order = new() {
                                Comment = message.Comment,
                                FromDate = message.FromDate.Year.Equals(1) ? DateTime.UtcNow.Date : message.FromDate,
                                OrganizationId = organization.Id,
                                ResponsibleId = userRepository.GetByNetIdWithoutIncludes(message.UserNetId).Id,
                                StorageId = storage.Id
                            };

                            DepreciatedOrder lastRecord = depreciatedOrderRepository.GetLastRecord(storage.Locale);

                            string locale =
                                storage.Locale.ToLower().Equals("pl")
                                    ? "P"
                                    : string.Empty;

                            if (lastRecord != null && DateTime.Now.Year.Equals(lastRecord.Created.Year))
                                order.Number =
                                    $"{locale}{string.Format("{0:D11}", Convert.ToInt32(lastRecord.Number.Substring(locale.Length, 11)) + 1)}";
                            else
                                order.Number =
                                    $"{locale}{string.Format("{0:D11}", 1)}";

                            order.Id = depreciatedOrderRepository.Add(order);

                            DepreciatedOrderItem depreciatedOrderItem = new() {
                                DepreciatedOrderId = order.Id,
                                ProductId = item.ProductId,
                                ActReconciliationItemId = item.Id,
                                Qty = message.Qty,
                                Reason = message.Reason
                            };

                            depreciatedOrderItem.Id =
                                _depreciatedRepositoriesFactory
                                    .NewDepreciatedOrderItemRepository(connection)
                                    .Add(
                                        depreciatedOrderItem
                                    );

                            item.QtyDifference -= message.Qty;

                            if (item.QtyDifference.Equals(0d)) {
                                item.HasDifference = false;
                                item.NegativeDifference = false;
                            }

                            actReconciliationItemRepository.Update(item);

                            order =
                                depreciatedOrderRepository
                                    .GetById(
                                        order.Id
                                    );

                            item =
                                actReconciliationItemRepository
                                    .GetByNetId(
                                        message.ItemNetId
                                    );

                            ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new StoreConsignmentMovementFromDepreciatedOrderMessage(order.Id));

                            Sender.Tell(
                                new {
                                    ActReconciliationItem = item,
                                    DepreciatedOrder = order
                                }
                            );
                        } else {
                            throw new Exception(DepreciatedOrderResourceNames.ORGANIZATION_IS_NOT_EXIST);
                        }
                    } else {
                        throw new Exception(DepreciatedOrderResourceNames.STORAGE_IS_NOT_EXIST);
                    }
                } else {
                    throw new Exception(DepreciatedOrderResourceNames.SPECIFIED_QTY_DIFFERENCE);
                }
            } else {
                throw new Exception(DepreciatedOrderResourceNames.ITEM_ACT_RECONSCILIATION_NOT_EXIST);
            }
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessAddNewDepreciatedOrderFromPackingListMessage(AddNewDepreciatedOrderFromPackingListMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.PackingList == null) throw new Exception(DepreciatedOrderResourceNames.ELEMENT_PACK_LIST_IS_EMPTY);

            IPackingListRepository packingListRepository = _supplyRepositoriesFactory.NewPackingListRepository(connection);

            PackingList packingListFromDb = packingListRepository.GetByIdForPlacement(message.PackingList.Id);

            if (packingListFromDb == null)
                throw new Exception(DepreciatedOrderResourceNames.PACK_LIST_iS_NOT_EXIST_IN_DB);
            if (!packingListFromDb.PackingListPackageOrderItems.All(i => i.IsPlaced))
                throw new Exception(DepreciatedOrderResourceNames.ALL_ELEMENT_PACK_LIST_IS_PLACED);
            if (!packingListFromDb.PackingListBoxes.All(b => b.PackingListPackageOrderItems.All(i => i.IsPlaced)))
                throw new Exception(DepreciatedOrderResourceNames.ALL_ELEMENT_PACK_LIST_IN_BOX_IS_PLACED);
            if (!packingListFromDb.PackingListPallets.All(p => p.PackingListPackageOrderItems.All(i => i.IsPlaced)))
                throw new Exception(DepreciatedOrderResourceNames.ALL_ELEMENT_PACK_LIST_IN_PALLETS_IS_PLACED);

            Organization organization = _organizationRepositoriesFactory.NewOrganizationRepository(connection).GetByNetId(message.OrganizationNetId);

            if (organization == null) throw new Exception(DepreciatedOrderResourceNames.ORGANIZATION_NEED_SPECIFIED);

            long storageId = 0;

            if (packingListFromDb.PackingListPackageOrderItems.Any())
                storageId =
                    packingListFromDb
                        .PackingListPackageOrderItems
                        .First(i => i.ProductIncomeItem != null)
                        .ProductIncomeItem
                        .ProductIncome
                        .StorageId;
            else if (packingListFromDb.PackingListBoxes.Any())
                storageId =
                    packingListFromDb
                        .PackingListBoxes
                        .First()
                        .PackingListPackageOrderItems
                        .First(i => i.ProductIncomeItem != null)
                        .ProductIncomeItem
                        .ProductIncome
                        .StorageId;
            else if (packingListFromDb.PackingListPallets.Any())
                storageId =
                    packingListFromDb
                        .PackingListPallets
                        .First()
                        .PackingListPackageOrderItems
                        .First(i => i.ProductIncomeItem != null)
                        .ProductIncomeItem
                        .ProductIncome
                        .StorageId;

            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IPackingListPackageOrderItemRepository packingListPackageOrderItemRepository =
                _supplyRepositoriesFactory.NewPackingListPackageOrderItemRepository(connection);

            foreach (PackingListPackageOrderItem item in message
                         .PackingList
                         .PackingListPackageOrderItems
                         .Where(i => i.ToOperationQty > 0)) {
                PackingListPackageOrderItem itemFromDb = packingListPackageOrderItemRepository.GetByIdForPlacement(item.Id);

                if (itemFromDb == null) continue;

                if (itemFromDb.RemainingQty < item.ToOperationQty)
                    throw new LocalizedException(
                        DepreciatedOrderResourceNames.SPECIFIED_QTY_MORE_REMAINING_QTY_PRODUCT,
                        new object[] { itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode, itemFromDb.RemainingQty, item.ToOperationQty });

                ProductAvailability availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                            storageId
                        );

                if (availability == null)
                    throw new LocalizedException(
                        DepreciatedOrderResourceNames.PRODUCT_MOVED_FROM_STORAGE,
                        new object[] { itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode });

                if (availability.Amount < item.ToOperationQty)
                    throw new LocalizedException(
                        DepreciatedOrderResourceNames.SPECIFIED_QTY_MORE_REMAINING_QTY_PRODUCT_ON_STORAGE,
                        new object[] { itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode, availability.Amount, item.ToOperationQty });
            }

            foreach (PackingListPackage box in message.PackingList.PackingListBoxes)
            foreach (PackingListPackageOrderItem item in box
                         .PackingList
                         .PackingListPackageOrderItems
                         .Where(i => i.ToOperationQty > 0)) {
                PackingListPackageOrderItem itemFromDb = packingListPackageOrderItemRepository.GetByIdForPlacement(item.Id);

                if (itemFromDb == null) continue;

                if (itemFromDb.RemainingQty < item.ToOperationQty)
                    throw new LocalizedException(
                        DepreciatedOrderResourceNames.SPECIFIED_QTY_MORE_REMAINING_QTY_PRODUCT,
                        new object[] { itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode, itemFromDb.RemainingQty, item.ToOperationQty });

                ProductAvailability availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                            storageId
                        );

                if (availability == null)
                    throw new LocalizedException(
                        DepreciatedOrderResourceNames.PRODUCT_MOVED_FROM_STORAGE,
                        new object[] { itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode });

                if (availability.Amount < item.ToOperationQty)
                    throw new LocalizedException(
                        DepreciatedOrderResourceNames.SPECIFIED_QTY_MORE_REMAINING_QTY_PRODUCT_ON_STORAGE,
                        new object[] { itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode, availability.Amount, item.ToOperationQty });
            }

            foreach (PackingListPackage pallet in message.PackingList.PackingListPallets)
            foreach (PackingListPackageOrderItem item in pallet
                         .PackingList
                         .PackingListPackageOrderItems
                         .Where(i => i.ToOperationQty > 0)) {
                PackingListPackageOrderItem itemFromDb = packingListPackageOrderItemRepository.GetByIdForPlacement(item.Id);

                if (itemFromDb == null) continue;

                if (itemFromDb.RemainingQty < item.ToOperationQty)
                    throw new LocalizedException(
                        DepreciatedOrderResourceNames.SPECIFIED_QTY_MORE_REMAINING_QTY_PRODUCT,
                        new object[] { itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode, itemFromDb.RemainingQty, item.ToOperationQty });

                ProductAvailability availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                            storageId
                        );

                if (availability == null)
                    throw new LocalizedException(
                        DepreciatedOrderResourceNames.PRODUCT_MOVED_FROM_STORAGE,
                        new object[] { itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode });

                if (availability.Amount < item.ToOperationQty)
                    throw new LocalizedException(
                        DepreciatedOrderResourceNames.SPECIFIED_QTY_MORE_REMAINING_QTY_PRODUCT_ON_STORAGE,
                        new object[] { itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode, availability.Amount, item.ToOperationQty });
            }

            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            IDepreciatedOrderRepository depreciatedOrderRepository = _depreciatedRepositoriesFactory.NewDepreciatedOrderRepository(connection);
            IDepreciatedOrderItemRepository depreciatedOrderItemRepository = _depreciatedRepositoriesFactory.NewDepreciatedOrderItemRepository(connection);

            DepreciatedOrder depreciatedOrder = new() {
                ResponsibleId = userRepository.GetByNetIdWithoutIncludes(message.UserNetId).Id,
                OrganizationId = organization.Id,
                StorageId = storageId,
                FromDate =
                    message.FromDate.Year.Equals(1)
                        ? DateTime.UtcNow.Date
                        : message.FromDate
            };

            DepreciatedOrder lastRecord = depreciatedOrderRepository.GetLastRecord("pl");

            if (lastRecord != null && DateTime.Now.Year.Equals(lastRecord.Created.Year))
                depreciatedOrder.Number =
                    $"P{string.Format("{0:D11}", Convert.ToInt32(lastRecord.Number.Substring(1, 11)) + 1)}";
            else
                depreciatedOrder.Number =
                    $"P{string.Format("{0:D11}", 1)}";

            depreciatedOrder.Id = depreciatedOrderRepository.Add(depreciatedOrder);

            foreach (PackingListPackageOrderItem item in message
                         .PackingList
                         .PackingListPackageOrderItems
                         .Where(i => i.ToOperationQty > 0)) {
                PackingListPackageOrderItem itemFromDb = packingListPackageOrderItemRepository.GetByIdForPlacement(item.Id);

                if (itemFromDb == null) continue;

                ProductAvailability availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                            storageId
                        );

                availability.Amount -= item.ToOperationQty;

                productAvailabilityRepository.Update(availability);

                DepreciatedOrderItem depreciatedOrderItem = new() {
                    DepreciatedOrderId = depreciatedOrder.Id,
                    ProductId = itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                    Qty = item.ToOperationQty,
                    Reason = item.Reason
                };

                depreciatedOrderItem.Id = depreciatedOrderItemRepository.Add(depreciatedOrderItem);

                itemFromDb.RemainingQty -= item.ToOperationQty;

                packingListPackageOrderItemRepository.UpdateRemainingQty(itemFromDb);
            }

            foreach (PackingListPackage box in message.PackingList.PackingListBoxes)
            foreach (PackingListPackageOrderItem item in box
                         .PackingList
                         .PackingListPackageOrderItems
                         .Where(i => i.ToOperationQty > 0)) {
                PackingListPackageOrderItem itemFromDb = packingListPackageOrderItemRepository.GetByIdForPlacement(item.Id);

                if (itemFromDb == null) continue;

                ProductAvailability availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                            storageId
                        );

                availability.Amount -= item.ToOperationQty;

                productAvailabilityRepository.Update(availability);

                DepreciatedOrderItem depreciatedOrderItem = new() {
                    DepreciatedOrderId = depreciatedOrder.Id,
                    ProductId = itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                    Qty = item.ToOperationQty,
                    Reason = item.Reason
                };

                depreciatedOrderItem.Id = depreciatedOrderItemRepository.Add(depreciatedOrderItem);

                itemFromDb.RemainingQty -= item.ToOperationQty;

                packingListPackageOrderItemRepository.UpdateRemainingQty(itemFromDb);
            }

            foreach (PackingListPackage pallet in message.PackingList.PackingListPallets)
            foreach (PackingListPackageOrderItem item in pallet
                         .PackingList
                         .PackingListPackageOrderItems
                         .Where(i => i.ToOperationQty > 0)) {
                PackingListPackageOrderItem itemFromDb = packingListPackageOrderItemRepository.GetByIdForPlacement(item.Id);

                if (itemFromDb == null) continue;

                ProductAvailability availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                            storageId
                        );

                availability.Amount -= item.ToOperationQty;

                productAvailabilityRepository.Update(availability);

                DepreciatedOrderItem depreciatedOrderItem = new() {
                    DepreciatedOrderId = depreciatedOrder.Id,
                    ProductId = itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                    Qty = item.ToOperationQty,
                    Reason = item.Reason
                };

                depreciatedOrderItem.Id = depreciatedOrderItemRepository.Add(depreciatedOrderItem);

                itemFromDb.RemainingQty -= item.ToOperationQty;

                packingListPackageOrderItemRepository.UpdateRemainingQty(itemFromDb);
            }

            packingListFromDb =
                packingListRepository
                    .GetByNetIdForPlacement(
                        packingListFromDb.NetUid
                    );

            depreciatedOrder = depreciatedOrderRepository.GetById(depreciatedOrder.Id);

            ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new StoreConsignmentMovementFromDepreciatedOrderMessage(depreciatedOrder.Id));

            Sender.Tell(
                new {
                    DepreciatedOrder = depreciatedOrder,
                    PackingList = packingListFromDb
                }
            );
        } catch (LocalizedException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessAddNewDepreciatedOrderFromFile(AddNewDepreciatedOrderFromFileMessage message) {
        try {
            List<DepreciatedOrderFromFileException> exceptions = new();

            List<ProductMovementItemFromFile> parsedProducts = _xlsFactoryManager
                .NewParseConfigurationXlsManager()
                .GetDepreciatedItemsFromXlsx(message.PathToFile, message.ParseConfig);

            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetByNetId(message.Order.Storage.NetUid);

            foreach (ProductMovementItemFromFile parsedProduct in parsedProducts) {
                Product product = getSingleProductRepository.GetProductByVendorCode(parsedProduct.VendorCode);

                if (product == null) throw new Exception(DepreciatedOrderResourceNames.PRODUCT_WITH_VENDOR_CODE_IS_NULL);
                //1225
                //parsedProduct.IsError = true;
                //exceptions.Add(
                //    new DepreciatedOrderFromFileException(
                //        DepreciatedOrderResourceNames.PRODUCT_WITH_VENDOR_CODE_IS_NULL,
                //        new object[] { parsedProduct.VendorCode }));
                //continue;
                ProductAvailability availabilityFrom =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            product.Id,
                            storage.Id
                        );

                if (availabilityFrom == null) throw new Exception(DepreciatedOrderResourceNames.PRODUCT_NOT_AVAILABLE_ON_STORAGE);
                //1225
                //parsedProduct.IsError = true;
                //exceptions.Add(
                //    new DepreciatedOrderFromFileException(
                //        DepreciatedOrderResourceNames.PRODUCT_NOT_AVAILABLE_ON_STORAGE,
                //        new object[] { parsedProduct.VendorCode }));
                //continue;
                if (availabilityFrom.Amount < parsedProduct.Qty) throw new Exception(DepreciatedOrderResourceNames.SPECIFIED_QTY_MORE_REMAINING_QTY_PRODUCT);
                //parsedProduct.IsError = true;
                //exceptions.Add(
                //    new DepreciatedOrderFromFileException(
                //        DepreciatedOrderResourceNames.SPECIFIED_QTY_MORE_REMAINING_QTY_PRODUCT,
                //        new object[] { parsedProduct.VendorCode, availabilityFrom.Amount, parsedProduct.Qty }));
            }

            if (parsedProducts.All(x => x.IsError))
                throw new Exception(DepreciatedOrderResourceNames.ALL_PRODUCTS_NOT_AVAILABLE_ON_STORAGE);

            IDepreciatedOrderRepository depreciatedOrderRepository = _depreciatedRepositoriesFactory.NewDepreciatedOrderRepository(connection);
            IDepreciatedOrderItemRepository depreciatedOrderItemRepository = _depreciatedRepositoriesFactory.NewDepreciatedOrderItemRepository(connection);

            DepreciatedOrder lastRecord = depreciatedOrderRepository.GetLastRecord(storage.Locale);

            string locale =
                storage.Locale.ToLower().Equals("pl")
                    ? "P"
                    : string.Empty;

            if (lastRecord != null && DateTime.Now.Year.Equals(lastRecord.Created.Year))
                message.Order.Number =
                    $"{locale}{string.Format("{0:D11}", Convert.ToInt32(lastRecord.Number.Substring(locale.Length, 11)) + 1)}";
            else
                message.Order.Number =
                    $"{locale}{string.Format("{0:D11}", 1)}";

            message.Order.StorageId = storage.Id;

            message.Order.OrganizationId = storage.Organization.Id;

            message.Order.ResponsibleId =
                _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

            long depreciatedOrderId = depreciatedOrderRepository.Add(message.Order);
            Dictionary<long, long> depreciatedOrderProductAvailabilityIds = new();
            List<DepreciatedOrderItem> depreciatedOrderItems = new();

            foreach (ProductMovementItemFromFile parsedProduct in parsedProducts.Where(x => x.IsError.Equals(false))) {
                Product product = getSingleProductRepository.GetProductByVendorCodeWithWriteOffRule(parsedProduct.VendorCode);

                ProductAvailability availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            product.Id,
                            storage.Id
                        );

                availability.Amount -= parsedProduct.Qty;

                productAvailabilityRepository.Update(availability);

                DepreciatedOrderItem depreciatedOrderItem = new() {
                    ProductId = product.Id,
                    DepreciatedOrderId = depreciatedOrderId,
                    Reason = message.Order.Comment,
                    Qty = parsedProduct.Qty
                };
                long depreciatedOrderItemId = depreciatedOrderItemRepository.Add(depreciatedOrderItem);
                depreciatedOrderProductAvailabilityIds.Add(depreciatedOrderItemId, availability.Id);
            }


            ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR)
                .Tell(new StoreConsignmentFromDepreciatedOrderWithReSaleMessage(
                    depreciatedOrderId,
                    depreciatedOrderProductAvailabilityIds,
                    message.Order.IsManagement));

            Sender.Tell(depreciatedOrderId);
        } catch (LocalizedException locExc) {
            Sender.Tell(locExc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}