using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.Helpers.Products;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.DepreciatedOrderModels;
using GBA.Domain.Messages.Consignments;
using GBA.Domain.Messages.Products.Transfers;
using GBA.Domain.Repositories.Consignments.Contracts;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Products;

public sealed class ProductTransfersActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConsignmentRepositoriesFactory _consignmentRepositoriesFactory;
    private readonly IOrganizationRepositoriesFactory _organizationRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public ProductTransfersActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IStorageRepositoryFactory storageRepositoryFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IOrganizationRepositoriesFactory organizationRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory,
        IConsignmentRepositoriesFactory consignmentRepositoriesFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _storageRepositoryFactory = storageRepositoryFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _organizationRepositoriesFactory = organizationRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;
        _consignmentRepositoriesFactory = consignmentRepositoriesFactory;

        Receive<AddNewProductTransferMessage>(ProcessAddNewProductTransferMessage);

        Receive<AddNewProductTransferFromActReconciliationItemsMessage>(ProcessAddNewProductTransferFromActReconciliationItemsMessage);

        Receive<AddNewProductTransferFromActReconciliationItemMessage>(ProcessAddNewProductTransferFromActReconciliationItemMessage);

        Receive<AddNewProductTransferFromPackingListMessage>(ProcessAddNewProductTransferFromPackingListMessage);

        Receive<AddProductTransferFromFileMessage>(ProcessAddProductTransferFromFileMessage);
    }

    private void ProcessAddProductTransferFromFileMessage(AddProductTransferFromFileMessage message) {
        try {
            List<ProductTransferFromFileException> exceptions = new();

            List<ProductMovementItemFromFile> parsedProducts = _xlsFactoryManager
                .NewParseConfigurationXlsManager()
                .GetDepreciatedItemsFromXlsx(message.PathToFile, message.ParseConfig);

            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);

            Storage fromStorage = storageRepository.GetByNetId(message.ProductTransfer.FromStorage.NetUid);
            Storage toStorage = storageRepository.GetByNetId(message.ProductTransfer.ToStorage.NetUid);

            foreach (ProductMovementItemFromFile parsedProduct in parsedProducts) {
                Product product = getSingleProductRepository.GetProductByVendorCode(parsedProduct.VendorCode);

                if (product == null) {
                    parsedProduct.IsError = true;
                    exceptions.Add(
                        new ProductTransferFromFileException(
                            ProductTransferResourceNames.PRODUCT_WITH_VENDOR_CODE_IS_NULL,
                            new object[] { parsedProduct.VendorCode }));

                    continue;
                }

                ProductAvailability availabilityFrom =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            product.Id,
                            fromStorage.Id
                        );

                if (availabilityFrom == null) {
                    parsedProduct.IsError = true;
                    exceptions.Add(
                        new ProductTransferFromFileException(
                            ProductTransferResourceNames.PRODUCT_NOT_AVAILABLE_ON_STORAGE,
                            new object[] { parsedProduct.VendorCode }));

                    continue;
                }

                if (!(availabilityFrom.Amount < parsedProduct.Qty)) continue;

                parsedProduct.IsError = true;
                exceptions.Add(
                    new ProductTransferFromFileException(
                        ProductTransferResourceNames.SPECIFIED_QTY_MORE_REMAINING_QTY_PRODUCT,
                        new object[] { parsedProduct.VendorCode, availabilityFrom.Amount, parsedProduct.Qty }));
            }

            if (parsedProducts.All(x => x.IsError))
                throw new Exception(ProductTransferResourceNames.ALL_PRODUCTS_NOT_AVAILABLE_ON_STORAGE);

            IProductTransferRepository productTransferRepository = _productRepositoriesFactory.NewProductTransferRepository(connection);
            IProductTransferItemRepository productTransferItemRepository = _productRepositoriesFactory.NewProductTransferItemRepository(connection);

            Organization organization = _organizationRepositoriesFactory.NewOrganizationRepository(connection).GetById(fromStorage.Organization.Id);

            ProductTransfer lastRecord = productTransferRepository.GetLastRecord(organization.Id);

            if (lastRecord != null && !string.IsNullOrEmpty(lastRecord.Number) && DateTime.Now.Year.Equals(lastRecord.Created.Year))
                message.ProductTransfer.Number =
                    string.Format(
                        "{0}{1}",
                        organization.Code,
                        string.IsNullOrEmpty(organization.Code)
                            ? 1
                            : Convert.ToInt32(lastRecord.Number.Substring(organization.Code.Length, lastRecord.Number.Length - organization.Code.Length)) + 1
                    );
            else
                message.ProductTransfer.Number =
                    $"{organization.Code}1";

            message.ProductTransfer.FromStorageId = fromStorage.Id;

            message.ProductTransfer.OrganizationId = fromStorage.Organization.Id;

            message.ProductTransfer.ToStorageId = toStorage.Id;

            message.ProductTransfer.ResponsibleId =
                _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

            long productTransferId = productTransferRepository.Add(message.ProductTransfer);

            Dictionary<long, long> productTransferItemProductAvailability = new();

            foreach (ProductMovementItemFromFile parsedProduct in parsedProducts.Where(x => x.IsError.Equals(false))) {
                Product product = getSingleProductRepository.GetProductByVendorCode(parsedProduct.VendorCode);

                ProductAvailability availabilityFrom =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            product.Id,
                            fromStorage.Id
                        );

                availabilityFrom.Amount -= parsedProduct.Qty;

                ProductAvailability availabilityTo =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            product.Id,
                            toStorage.Id
                        );

                if (availabilityTo == null) {
                    availabilityTo = new ProductAvailability {
                        ProductId = product.Id,
                        StorageId = toStorage.Id,
                        Amount = parsedProduct.Qty
                    };

                    productAvailabilityRepository.Add(availabilityTo);
                } else {
                    availabilityTo.Amount += parsedProduct.Qty;
                    productAvailabilityRepository.Update(availabilityTo);
                }

                productAvailabilityRepository.Update(availabilityFrom);

                long itemId = productTransferItemRepository.Add(new ProductTransferItem {
                    ProductId = product.Id,
                    ProductTransferId = productTransferId,
                    Reason = message.ProductTransfer.Comment,
                    Qty = parsedProduct.Qty
                });

                productTransferItemProductAvailability.Add(itemId, availabilityFrom.Id);
            }

            ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR)
                .Tell(new StoreConsignmentFromProductTransferWithReSaleMessage(
                    productTransferId,
                    productTransferItemProductAvailability,
                    message.ProductTransfer.IsManagement,
                    true));

            Sender.Tell(exceptions);
        } catch (LocalizedException locExc) {
            Sender.Tell(locExc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessAddNewProductTransferMessage(AddNewProductTransferMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            if (message.ProductTransfer == null) throw new Exception(ProductTransferResourceNames.ENTITY_CAN_NOT_BE_NULL);
            if (message.ProductTransfer.FromStorage == null || message.ProductTransfer.FromStorage.IsNew())
                throw new Exception(ProductTransferResourceNames.FROM_STORAGE_NEED_SPECIFIED);
            if (message.ProductTransfer.ToStorage == null || message.ProductTransfer.ToStorage.IsNew())
                throw new Exception(ProductTransferResourceNames.TO_STORAGE_NEED_SPECIFIED);
            if (message.ProductTransfer.Organization == null || message.ProductTransfer.Organization.IsNew())
                throw new Exception(ProductTransferResourceNames.ORGANIZATION_NEED_SPECIFIED);
            if (!message.ProductTransfer.ProductTransferItems.Any(i => i.Product != null && !i.Product.IsNew() && i.Qty > 0))
                throw new Exception(ProductTransferResourceNames.NEED_ADD_ONE_ITEM_WITH_SPECIFIED_PRODUCT);

            IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);

            Storage fromStorage = storageRepository.GetById(message.ProductTransfer.FromStorage.Id);

            if (fromStorage == null) throw new Exception(ProductTransferResourceNames.SPECIFIED_FROM_STORAGE_NOT_EXIST);

            Storage toStorage = storageRepository.GetById(message.ProductTransfer.ToStorage.Id);

            if (toStorage == null) throw new Exception(ProductTransferResourceNames.SPECIFIED_TO_STORAGE_NOT_EXIST);

            if (fromStorage.Id.Equals(toStorage.Id))
                throw new Exception(ProductTransferResourceNames.MOVEMENT_INSIDE_SINGLE_STORAGE_NOT_ALLOWED);

            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            foreach (ProductTransferItem transferItem in message
                         .ProductTransfer
                         .ProductTransferItems
                         .Where(i => i.Product != null && !i.Product.IsNew() && i.Qty > 0)) {
                ProductAvailability availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            transferItem.Product.Id,
                            fromStorage.Id
                        );

                if (availability == null)
                    throw new LocalizedException(
                        ProductTransferResourceNames.PRODUCT_NOT_AVAILABLE_ON_STORAGE,
                        new object[] { transferItem.Product?.VendorCode ?? "" });

                if (availability.Amount < transferItem.Qty)
                    throw new LocalizedException(
                        ProductTransferResourceNames.PRODUCT_LESS_AMOUNT_ON_STORAGE,
                        new object[] { transferItem.Product?.VendorCode ?? "" });
            }

            IProductTransferRepository productTransferRepository = _productRepositoriesFactory.NewProductTransferRepository(connection);
            IProductTransferItemRepository productTransferItemRepository = _productRepositoriesFactory.NewProductTransferItemRepository(connection);
            IConsignmentItemRepository consignmentItemRepository = _consignmentRepositoriesFactory.NewConsignmentItemRepository(connection);
            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            IProductWriteOffRuleRepository productWriteOffRuleRepository = _productRepositoriesFactory.NewProductWriteOffRuleRepository(connection);

            message.ProductTransfer.FromDate =
                message.ProductTransfer.FromDate.Year.Equals(1)
                    ? DateTime.UtcNow
                    : TimeZoneInfo.ConvertTimeToUtc(message.ProductTransfer.FromDate);

            message.ProductTransfer.ResponsibleId =
                message.ProductTransfer.Responsible != null && !message.ProductTransfer.Responsible.IsNew()
                    ? message.ProductTransfer.Responsible.Id
                    : userRepository.GetByNetIdWithoutIncludes(message.UserNetId).Id;
            User user = userRepository.GetByNetId(message.UserNetId);

            message.ProductTransfer.OrganizationId = fromStorage.OrganizationId ?? 0;
            message.ProductTransfer.FromStorageId = fromStorage.Id;
            message.ProductTransfer.ToStorageId = toStorage.Id;

            if (string.IsNullOrEmpty(message.ProductTransfer.Number)) {
                Organization organization = _organizationRepositoriesFactory.NewOrganizationRepository(connection).GetById(message.ProductTransfer.OrganizationId);

                ProductTransfer lastRecord = productTransferRepository.GetLastRecord(organization.Id);

                if (lastRecord != null && DateTime.Now.Year.Equals(lastRecord.Created.Year))
                    message.ProductTransfer.Number =
                        string.Format(
                            "{0}{1}",
                            organization.Code,
                            string.IsNullOrEmpty(organization.Code)
                                ? 1
                                : Convert.ToInt32(lastRecord.Number.Substring(organization.Code.Length, lastRecord.Number.Length - organization.Code.Length)) + 1
                        );
                else
                    message.ProductTransfer.Number =
                        $"{organization.Code}1";
            }

            long productTransferId = message.ProductTransfer.Id =
                productTransferRepository
                    .Add(
                        message.ProductTransfer
                    );

            Dictionary<long, long> productTransferItemProductAvailability = new();
            foreach (ProductTransferItem transferItem in message
                         .ProductTransfer
                         .ProductTransferItems
                         .Where(i => i.Product != null && !i.Product.IsNew() && i.Qty > 0)) {
                Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetId(transferItem.Product.NetUid);

                ProductAvailability availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            transferItem.Product.Id,
                            fromStorage.Id
                        );
                ProductAvailability productAvailability = product.ProductAvailabilities.FirstOrDefault(x => x.Id == availability.Id);
                availability.Amount -= transferItem.Qty;

                productAvailabilityRepository.Update(availability);

                availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            transferItem.Product.Id,
                            toStorage.Id
                        );

                if (availability == null) {
                    availability = new ProductAvailability {
                        Amount = transferItem.Qty,
                        ProductId = transferItem.Product.Id,
                        StorageId = toStorage.Id
                    };

                    availability.Id = productAvailabilityRepository.AddWithId(availability);
                } else {
                    availability.Amount += transferItem.Qty;

                    productAvailabilityRepository.Update(availability);
                }

                transferItem.ProductId = transferItem.Product.Id;
                transferItem.ProductTransferId = message.ProductTransfer.Id;

                transferItem.Id = productTransferItemRepository.Add(transferItem);

                productTransferItemProductAvailability.Add(transferItem.Id, availability.Id);
            }

            ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new StoreConsignmentFromProductTransferWithReSaleMessage(
                productTransferId,
                productTransferItemProductAvailability,
                message.ProductTransfer.IsManagement,
                false,
                message.RowNumber,
                message.CellNumber,
                message.StorageNumber,
                user.Id));

            Sender.Tell(
                productTransferRepository
                    .GetById(
                        message.ProductTransfer.Id
                    )
            );
        } catch (LocalizedException locExc) {
            Sender.Tell(locExc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessAddNewProductTransferFromActReconciliationItemsMessage(AddNewProductTransferFromActReconciliationItemsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);
            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);

            Storage fromStorage = storageRepository.GetByNetId(message.FromStorageNetId);

            if (fromStorage == null) throw new Exception(ProductTransferResourceNames.FROM_STORAGE_NEED_SPECIFIED);

            Storage toStorage = storageRepository.GetByNetId(message.ToStorageNetId);

            if (toStorage == null) throw new Exception(ProductTransferResourceNames.TO_STORAGE_NEED_SPECIFIED);

            Organization organization = _organizationRepositoriesFactory.NewOrganizationRepository(connection).GetByNetId(message.OrganizationNetId);

            if (organization == null) throw new Exception(ProductTransferResourceNames.ORGANIZATION_NEED_SPECIFIED);

            if (message.Items.Any(i => i.ToOperationQty > 0)) {
                IProductTransferRepository productTransferRepository = _productRepositoriesFactory.NewProductTransferRepository(connection);
                IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
                IActReconciliationItemRepository actReconciliationItemRepository = _supplyUkraineRepositoriesFactory.NewActReconciliationItemRepository(connection);

                ProductTransfer transfer = new() {
                    Comment = message.Comment,
                    FromDate = message.FromDate.Year.Equals(1)
                        ? DateTime.UtcNow
                        : TimeZoneInfo.ConvertTimeToUtc(message.FromDate),
                    FromStorageId = fromStorage.Id,
                    ToStorageId = toStorage.Id,
                    OrganizationId = organization.Id,
                    ResponsibleId = userRepository.GetByNetIdWithoutIncludes(message.UserNetId).Id
                };

                ProductTransfer lastRecord = productTransferRepository.GetLastRecord(organization.Id);

                if (lastRecord != null && DateTime.Now.Year.Equals(lastRecord.Created.Year))
                    transfer.Number =
                        string.Format(
                            "{0}{1}",
                            organization.Code,
                            Convert.ToInt32(lastRecord.Number.Substring(organization.Code.Length, lastRecord.Number.Length - organization.Code.Length)) + 1
                        );
                else
                    transfer.Number =
                        $"{organization.Code}{string.Format("{0:D11}", 1)}";

                transfer.Id = productTransferRepository.Add(transfer);

                foreach (ActReconciliationItem listItem in message.Items) {
                    ActReconciliationItem item =
                        actReconciliationItemRepository
                            .GetById(
                                listItem.Id
                            );

                    if (item.QtyDifference < listItem.ToOperationQty) listItem.ToOperationQty = item.QtyDifference;

                    if (listItem.ToOperationQty.Equals(0d)) continue;

                    ProductAvailability fromAvailability =
                        productAvailabilityRepository
                            .GetByProductAndStorageIds(
                                item.ProductId,
                                fromStorage.Id
                            );

                    if (fromAvailability == null) continue;

                    if (fromAvailability.Amount < listItem.ToOperationQty)
                        listItem.ToOperationQty = fromAvailability.Amount;

                    fromAvailability.Amount -= listItem.ToOperationQty;

                    if (fromAvailability.Amount > 0)
                        productAvailabilityRepository.RemoveById(fromAvailability.Id);
                    else
                        productAvailabilityRepository.Update(fromAvailability);

                    ProductTransferItem transferItem = new() {
                        ProductTransferId = transfer.Id,
                        ProductId = item.ProductId,
                        Qty = listItem.ToOperationQty,
                        ActReconciliationItemId = item.Id,
                        Reason = listItem.Reason
                    };

                    transferItem.Id =
                        _productRepositoriesFactory
                            .NewProductTransferItemRepository(connection)
                            .Add(transferItem);

                    ProductAvailability availability =
                        productAvailabilityRepository
                            .GetByProductAndStorageIds(
                                item.ProductId,
                                toStorage.Id
                            );

                    if (availability == null) {
                        availability = new ProductAvailability {
                            Amount = listItem.ToOperationQty,
                            ProductId = item.ProductId,
                            StorageId = toStorage.Id
                        };

                        availability.Id = productAvailabilityRepository.AddWithId(availability);
                    } else {
                        availability.Amount += listItem.ToOperationQty;

                        productAvailabilityRepository.Update(availability);
                    }

                    transferItem.ProductAvailability = availability;

                    item.QtyDifference -= listItem.ToOperationQty;

                    if (item.QtyDifference.Equals(0d)) {
                        item.HasDifference = false;
                        item.NegativeDifference = false;
                    }

                    actReconciliationItemRepository.Update(item);
                }

                transfer =
                    productTransferRepository
                        .GetById(
                            transfer.Id
                        );

                ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new AddNewConsignmentFromProductTransferMessage(transfer.Id));

                Sender.Tell(
                    new {
                        ProductTransfer = transfer,
                        ActReconciliationItems =
                            actReconciliationItemRepository
                                .GetByIds(
                                    message.Items.Select(i => i.Id)
                                )
                    }
                );
            } else {
                throw new Exception("No items was provided");
            }
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessAddNewProductTransferFromActReconciliationItemMessage(AddNewProductTransferFromActReconciliationItemMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);
            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);

            Storage fromStorage = storageRepository.GetByNetId(message.FromStorageNetId);

            if (fromStorage == null) throw new Exception(ProductTransferResourceNames.FROM_STORAGE_NEED_SPECIFIED);

            Storage toStorage = storageRepository.GetByNetId(message.ToStorageNetId);

            if (toStorage == null) throw new Exception(ProductTransferResourceNames.TO_STORAGE_NEED_SPECIFIED);

            if (!toStorage.ForDefective && (string.IsNullOrEmpty(message.StorageNumber) || string.IsNullOrEmpty(message.CellNumber)))
                throw new Exception(ProductTransferResourceNames.NEED_SPECIFY_CELL_AND_STORAGE_NUMBER);

            Organization organization = _organizationRepositoriesFactory.NewOrganizationRepository(connection).GetByNetId(message.OrganizationNetId);

            if (organization == null) throw new Exception(ProductTransferResourceNames.ORGANIZATION_NEED_SPECIFIED);

            IActReconciliationItemRepository actReconciliationItemRepository = _supplyUkraineRepositoriesFactory.NewActReconciliationItemRepository(connection);

            ActReconciliationItem item =
                actReconciliationItemRepository
                    .GetByNetId(
                        message.ItemNetId
                    );

            if (item == null) throw new Exception(ProductTransferResourceNames.ACT_RECONCILIATION_NOT_EXIST);
            if (item.QtyDifference < message.Qty) throw new Exception(ProductTransferResourceNames.SPECIFY_QTY_MORE);

            IProductTransferRepository productTransferRepository = _productRepositoriesFactory.NewProductTransferRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            ProductTransfer transfer = new() {
                Comment = message.Comment,
                FromDate = message.FromDate.Year.Equals(1)
                    ? DateTime.UtcNow
                    : TimeZoneInfo.ConvertTimeToUtc(message.FromDate),
                FromStorageId = fromStorage.Id,
                ToStorageId = toStorage.Id,
                OrganizationId = organization.Id,
                ResponsibleId = userRepository.GetByNetIdWithoutIncludes(message.UserNetId).Id
            };

            ProductTransfer lastRecord = productTransferRepository.GetLastRecord(organization.Id);

            if (lastRecord != null && DateTime.Now.Year.Equals(lastRecord.Created.Year))
                transfer.Number =
                    string.Format(
                        "{0}{1}",
                        organization.Code,
                        Convert.ToInt32(lastRecord.Number.Substring(organization.Code.Length, lastRecord.Number.Length - organization.Code.Length)) + 1
                    );
            else
                transfer.Number =
                    $"{organization.Code}{string.Format("{0:D11}", 1)}";

            ProductAvailability fromAvailability =
                productAvailabilityRepository
                    .GetByProductAndStorageIds(
                        item.ProductId,
                        fromStorage.Id
                    );

            if (fromAvailability != null) {
                transfer.Id = productTransferRepository.Add(transfer);

                ProductTransferItem transferItem = new() {
                    ProductTransferId = transfer.Id,
                    ProductId = item.ProductId,
                    Qty = message.Qty,
                    ActReconciliationItemId = item.Id,
                    Reason = message.Reason
                };

                transferItem.Id =
                    _productRepositoriesFactory
                        .NewProductTransferItemRepository(connection)
                        .Add(transferItem);

                if (message.Qty > fromAvailability.Amount)
                    message.Qty = fromAvailability.Amount;

                fromAvailability.Amount -= message.Qty;

                if (fromAvailability.Amount > 0)
                    productAvailabilityRepository.RemoveById(fromAvailability.Id);
                else
                    productAvailabilityRepository.Update(fromAvailability);

                ProductAvailability availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            item.ProductId,
                            toStorage.Id
                        );

                if (availability == null) {
                    availability = new ProductAvailability {
                        Amount = message.Qty,
                        ProductId = item.ProductId,
                        StorageId = toStorage.Id
                    };

                    availability.Id = productAvailabilityRepository.AddWithId(availability);
                } else {
                    availability.Amount += message.Qty;

                    productAvailabilityRepository.Update(availability);
                }

                transferItem.ProductAvailability = availability;

                item.QtyDifference -= message.Qty;

                if (item.QtyDifference.Equals(0d)) {
                    item.HasDifference = false;
                    item.NegativeDifference = false;
                }

                actReconciliationItemRepository.Update(item);
            }

            item =
                actReconciliationItemRepository
                    .GetByNetId(
                        message.ItemNetId
                    );

            transfer =
                productTransferRepository
                    .GetById(
                        transfer.Id
                    );

            ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new AddNewConsignmentFromProductTransferMessage(transfer.Id));

            Sender.Tell(
                new {
                    ProductTransfer = transfer,
                    ActReconciliationItem = item
                }
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessAddNewProductTransferFromPackingListMessage(AddNewProductTransferFromPackingListMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.PackingList == null) throw new Exception(ProductTransferResourceNames.PACKING_LIST_NULL_OR_EMPTY);

            IPackingListRepository packingListRepository = _supplyRepositoriesFactory.NewPackingListRepository(connection);

            PackingList packingListFromDb = packingListRepository.GetByIdForPlacement(message.PackingList.Id);

            if (packingListFromDb == null) throw new Exception(ProductTransferResourceNames.PACKING_LIST_NULL_OR_EMPTY);
            if (!packingListFromDb.PackingListPackageOrderItems.All(i => i.IsPlaced))
                throw new Exception(ProductTransferResourceNames.PACKING_LIST_ITEMS_NEED_PLACED);
            if (!packingListFromDb.PackingListBoxes.All(b => b.PackingListPackageOrderItems.All(i => i.IsPlaced)))
                throw new Exception(ProductTransferResourceNames.PACKING_LIST_ITEMS_NEED_PLACED);
            if (!packingListFromDb.PackingListPallets.All(p => p.PackingListPackageOrderItems.All(i => i.IsPlaced)))
                throw new Exception(ProductTransferResourceNames.PACKING_LIST_ITEMS_NEED_PLACED);

            Organization organization = _organizationRepositoriesFactory.NewOrganizationRepository(connection).GetByNetId(message.OrganizationNetId);

            if (organization == null) throw new Exception(ProductTransferResourceNames.ORGANIZATION_NEED_SPECIFIED);

            Storage toStorage = _storageRepositoryFactory.NewStorageRepository(connection).GetByNetId(message.ToStorageNetId);

            if (toStorage == null) throw new Exception(ProductTransferResourceNames.TO_STORAGE_NEED_SPECIFIED);

            if (!toStorage.ForDefective && (string.IsNullOrEmpty(message.StorageNumber) || string.IsNullOrEmpty(message.CellNumber)))
                throw new Exception(ProductTransferResourceNames.NEED_SPECIFY_CELL_AND_STORAGE_NUMBER);

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
                        ProductTransferResourceNames.SPECIFY_QTY_MORE_REMAINING,
                        new object[] { itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode, itemFromDb.RemainingQty, item.ToOperationQty });

                ProductAvailability availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                            storageId
                        );

                if (availability == null)
                    throw new LocalizedException(
                        ProductTransferResourceNames.PRODUCT_MOVED_FROM_STORAGE,
                        new object[] { itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode });

                if (availability.Amount < item.ToOperationQty)
                    throw new LocalizedException(
                        ProductTransferResourceNames.SPECIFY_QTY_MORE_REMAINING,
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
                        ProductTransferResourceNames.SPECIFY_QTY_MORE_REMAINING,
                        new object[] { itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode, itemFromDb.RemainingQty, item.ToOperationQty });

                ProductAvailability availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                            storageId
                        );

                if (availability == null)
                    throw new LocalizedException(
                        ProductTransferResourceNames.PRODUCT_MOVED_FROM_STORAGE,
                        new object[] { itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode });

                if (availability.Amount < item.ToOperationQty)
                    throw new LocalizedException(
                        ProductTransferResourceNames.SPECIFY_QTY_MORE_REMAINING,
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
                        ProductTransferResourceNames.SPECIFY_QTY_MORE_REMAINING,
                        new object[] { itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode, itemFromDb.RemainingQty, item.ToOperationQty });

                ProductAvailability availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                            storageId
                        );

                if (availability == null)
                    throw new LocalizedException(
                        ProductTransferResourceNames.PRODUCT_MOVED_FROM_STORAGE,
                        new object[] { itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode });

                if (availability.Amount < item.ToOperationQty)
                    throw new LocalizedException(
                        ProductTransferResourceNames.SPECIFY_QTY_MORE_REMAINING,
                        new object[] { itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode, availability.Amount, item.ToOperationQty });
            }

            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            IProductTransferRepository productTransferRepository = _productRepositoriesFactory.NewProductTransferRepository(connection);
            IProductTransferItemRepository productTransferItemRepository = _productRepositoriesFactory.NewProductTransferItemRepository(connection);

            ProductTransfer productTransfer = new() {
                FromDate =
                    message.FromDate.Year.Equals(1)
                        ? DateTime.UtcNow
                        : TimeZoneInfo.ConvertTimeToUtc(message.FromDate),
                ResponsibleId = userRepository.GetByNetIdWithoutIncludes(message.UserNetId).Id,
                FromStorageId = storageId,
                OrganizationId = organization.Id,
                ToStorageId = toStorage.Id
            };

            ProductTransfer lastRecord = productTransferRepository.GetLastRecord(organization.Id);

            if (lastRecord != null && DateTime.Now.Year.Equals(lastRecord.Created.Year))
                productTransfer.Number =
                    string.Format(
                        "{0}{1}",
                        organization.Code,
                        Convert.ToInt32(lastRecord.Number.Substring(organization.Code.Length, lastRecord.Number.Length - organization.Code.Length)) + 1
                    );
            else
                productTransfer.Number =
                    $"{organization.Code}{string.Format("{0:D11}", 1)}";

            productTransfer.Id =
                productTransferRepository
                    .Add(
                        productTransfer
                    );

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

                availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                            toStorage.Id
                        );

                if (availability == null) {
                    availability = new ProductAvailability {
                        Amount = item.ToOperationQty,
                        ProductId = itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                        StorageId = toStorage.Id
                    };

                    availability.Id = productAvailabilityRepository.AddWithId(availability);
                } else {
                    availability.Amount += item.ToOperationQty;

                    productAvailabilityRepository.Update(availability);
                }

                ProductTransferItem transferItem = new() {
                    ProductId = itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                    ProductTransferId = productTransfer.Id,
                    Qty = item.ToOperationQty,
                    Reason = item.Reason
                };

                transferItem.Id = productTransferItemRepository.Add(transferItem);

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

                availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                            toStorage.Id
                        );

                if (availability == null) {
                    availability = new ProductAvailability {
                        Amount = item.ToOperationQty,
                        ProductId = itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                        StorageId = toStorage.Id
                    };

                    availability.Id = productAvailabilityRepository.AddWithId(availability);
                } else {
                    availability.Amount += item.ToOperationQty;

                    productAvailabilityRepository.Update(availability);
                }

                ProductTransferItem transferItem = new() {
                    ProductId = itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                    ProductTransferId = productTransfer.Id,
                    Qty = item.ToOperationQty,
                    Reason = item.Reason
                };

                transferItem.Id = productTransferItemRepository.Add(transferItem);

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

                availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                            toStorage.Id
                        );

                if (availability == null) {
                    productAvailabilityRepository
                        .Add(new ProductAvailability {
                            Amount = item.ToOperationQty,
                            ProductId = itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                            StorageId = toStorage.Id
                        });
                } else {
                    availability.Amount += item.ToOperationQty;

                    productAvailabilityRepository.Update(availability);
                }

                ProductTransferItem productTransferItem = new() {
                    ProductId = itemFromDb.SupplyInvoiceOrderItem.Product.Id,
                    ProductTransferId = productTransfer.Id,
                    Qty = item.ToOperationQty,
                    Reason = item.Reason
                };

                productTransferItem.Id = productTransferItemRepository.Add(productTransferItem);

                itemFromDb.RemainingQty -= item.ToOperationQty;

                packingListPackageOrderItemRepository.UpdateRemainingQty(itemFromDb);
            }

            packingListFromDb = packingListRepository.GetByNetIdForPlacement(packingListFromDb.NetUid);

            productTransfer = productTransferRepository.GetById(productTransfer.Id);

            ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new AddNewConsignmentFromProductTransferMessage(productTransfer.Id));

            Sender.Tell(
                new {
                    ProductTransfer = productTransfer,
                    PackingList = packingListFromDb
                }
            );
        } catch (LocalizedException locExc) {
            Sender.Tell(locExc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}