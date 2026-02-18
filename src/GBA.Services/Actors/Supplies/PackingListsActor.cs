using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Akka.Actor;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.Helpers;
using GBA.Common.Helpers.SupplyOrders;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Messages.Auditing;
using GBA.Domain.Messages.Products.ProductSpecifications;
using GBA.Domain.Messages.Supplies.Invoices;
using GBA.Domain.Messages.Supplies.PackingLists;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Supplies;

public sealed class PackingListsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IOrganizationRepositoriesFactory _organizationRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public PackingListsActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory,
        IOrganizationRepositoriesFactory organizationRepositoriesFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;
        _organizationRepositoriesFactory = organizationRepositoriesFactory;

        Receive<AddOrUpdatePackingListsMessage>(ProcessAddOrUpdatePackingListsMessage);

        Receive<AddOrUpdatePackingListFromFileMessage>(ProcessAddOrUpdatePackingListFromFileMessage);

        Receive<UpdatePackingListInvoiceDocumentsMessage>(ProcessUpdatePackingListInvoiceDocumentsMessage);

        Receive<UpdatePackingListProductPlacementInfoMessage>(ProcessUpdatePackingListProductPlacementInfoMessage);

        Receive<UpdatePackingListFromFileMessage>(ProcessUpdatePackingListFromFileMessage);

        Receive<SetReadyToPlaceToPackingListPackageOrderItemByNetIdMessage>(ProcessSetReadyToPlaceToPackingListPackageOrderItemByNetIdMessage);

        Receive<SetReadyToPlaceToAllPackingListPackageOrderItemsByNetIdMessage>(ProcessSetReadyToPlaceToAllPackingListPackageOrderItemsByNetIdMessage);

        Receive<DeletePackingListByNetIdMessage>(ProcessDeletePackingListByNetIdMessage);

        Receive<UpdateVatsForPackingListMessage>(ProcessUpdateVatsForPackingListMessage);

        Receive<UpdatePlacementPackingListMessage>(ProcessUpdatePlacementPackingListMessage);
    }

    private void ProcessAddOrUpdatePackingListsMessage(AddOrUpdatePackingListsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.SupplyInvoice.Equals(null)) {
            Sender.Tell(new Tuple<SupplyInvoice, string>(null, "Supply invoice can not be null"));
        } else if (message.SupplyInvoice.IsNew()) {
            Sender.Tell(new Tuple<SupplyInvoice, string>(null, "New supply invoice is not valid input for current request"));
        } else if (message.SupplyInvoice.PackingLists.Any(p => p.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.Equals(null)) ||
                                                               p.PackingListPallets.Any(i =>
                                                                   i.PackingListPackageOrderItems.Any(o => o.SupplyInvoiceOrderItem.Equals(null))) ||
                                                               p.PackingListBoxes.Any(i =>
                                                                   i.PackingListPackageOrderItems.Any(o => o.SupplyInvoiceOrderItem.Equals(null))))) {
            Sender.Tell(new Tuple<SupplyInvoice, string>(null, "Each PackingListPackageOrderItem should have SupplyInvoiceOrderItem inside of it's entity"));
        } else if (message.SupplyInvoice.PackingLists.Any()) {
            IPackingListRepository packingListRepository = _supplyRepositoriesFactory.NewPackingListRepository(connection);
            IInvoiceDocumentRepository invoiceDocumentRepository = _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection);
            IPackingListPackageRepository packingListPackageRepository = _supplyRepositoriesFactory.NewPackingListPackageRepository(connection);
            IPackingListPackageOrderItemRepository packingListPackageOrderItemRepository =
                _supplyRepositoriesFactory.NewPackingListPackageOrderItemRepository(connection);

            List<PackingList> packingLists = new();

            SupplyInvoice invoice =
                _supplyRepositoriesFactory
                    .NewSupplyInvoiceRepository(connection)
                    .GetSupplyInvoiceByPackingListNetId(message.SupplyInvoice.NetUid);

            foreach (PackingList item in message.SupplyInvoice.PackingLists) {
                PackingList toReturn = _supplyRepositoriesFactory
                    .NewPackingListRepository(connection)
                    .GetById(item.Id);
                packingLists.Add(toReturn);
            }

            foreach (PackingListPackageOrderItem packingListPackageOrderItemBack in packingLists
                         .SelectMany(pl => pl.PackingListPackageOrderItems))
            foreach (PackingListPackageOrderItem packingListPackageOrderItem in message.SupplyInvoice.PackingLists
                         .SelectMany(pl => pl.PackingListPackageOrderItems))
                if (packingListPackageOrderItemBack.Id == packingListPackageOrderItem.Id) {
                    packingListPackageOrderItem.VatAmount = packingListPackageOrderItemBack.VatAmount;
                    packingListPackageOrderItem.VatPercent = packingListPackageOrderItemBack.VatPercent;
                }


            if (message.SupplyInvoice.PackingLists.Any()) {
                packingListRepository.RemoveAllByInvoiceIdExceptProvided(
                    message.SupplyInvoice.Id,
                    message.SupplyInvoice.PackingLists.Where(p => !p.IsNew() && !p.Deleted).Select(p => p.Id)
                );

                foreach (PackingList packingList in message.SupplyInvoice.PackingLists.Where(p => !p.IsNew() && !p.Deleted)) {
                    if (packingList.InvoiceDocuments.Any()) {
                        invoiceDocumentRepository.RemoveAllByPackingListIdExceptProvided(
                            packingList.Id,
                            packingList.InvoiceDocuments
                                .Where(d => !d.IsNew() && !d.Deleted)
                                .Select(d => d.Id)
                        );

                        if (packingList.InvoiceDocuments.Any(d => d.IsNew())) {
                            packingList.IsDocumentsAdded = true;

                            invoiceDocumentRepository.Add(
                                packingList.InvoiceDocuments
                                    .Where(d => d.IsNew())
                                    .Select(d => {
                                        d.PackingListId = packingList.Id;

                                        return d;
                                    })
                            );
                        }
                    } else {
                        invoiceDocumentRepository.RemoveAllByPackingListId(packingList.Id);
                    }

                    packingListPackageOrderItemRepository.RemoveAllByPackingListIdExceptProvided(
                        packingList.Id,
                        packingList.PackingListPackageOrderItems.Where(i => !i.IsNew() && !i.Deleted).Select(i => i.Id)
                    );

                    packingListPackageRepository.RemoveAllByPackingListIdExceptProvided(
                        packingList.Id,
                        packingList.PackingListBoxes.Where(p => !p.IsNew() && !p.Deleted).Select(p => p.Id),
                        packingList.PackingListPallets.Where(p => !p.IsNew() && !p.Deleted).Select(p => p.Id)
                    );

                    if (packingList.PackingListPackageOrderItems.Any(i => i.IsNew()))
                        packingListPackageOrderItemRepository.Add(
                            packingList.PackingListPackageOrderItems.Where(i => i.IsNew()).Select(item => {
                                item.SupplyInvoiceOrderItemId = item.SupplyInvoiceOrderItem.Id;
                                item.PackingListId = packingList.Id;

                                if (item.SupplyInvoiceOrderItem?.SupplyOrderItem != null) item.UnitPrice = item.SupplyInvoiceOrderItem.SupplyOrderItem.UnitPrice;

                                return item;
                            })
                        );

                    if (packingList.PackingListPackageOrderItems.Any(i => !i.IsNew()))
                        packingListPackageOrderItemRepository.Update(packingList.PackingListPackageOrderItems.Where(i => !i.IsNew()));

                    if (packingList.PackingListBoxes.Any(p => !p.IsNew() && !p.PackingListPackageOrderItems.Any()))
                        packingListPackageRepository.Update(packingList.PackingListBoxes.Where(p => !p.IsNew() && !p.PackingListPackageOrderItems.Any()));

                    if (packingList.PackingListPallets.Any(p => !p.IsNew() && !p.PackingListPackageOrderItems.Any()))
                        packingListPackageRepository.Update(packingList.PackingListPallets.Where(p => !p.IsNew() && !p.PackingListPackageOrderItems.Any()));

                    foreach (PackingListPackage package in packingList.PackingListBoxes.Where(p => p.IsNew())) {
                        if (!package.Width.Equals(0) && !package.Height.Equals(0) && !package.Lenght.Equals(0))
                            package.CBM = Math.Round(package.Lenght * package.Height * package.Width * 0.000001, 6);

                        package.Type = PackingListPackageType.Box;
                        package.PackingListId = packingList.Id;

                        package.Id = packingListPackageRepository.Add(package);

                        packingListPackageOrderItemRepository.Add(
                            package.PackingListPackageOrderItems.Select(item => {
                                item.SupplyInvoiceOrderItemId = item.SupplyInvoiceOrderItem.Id;
                                item.PackingListPackageId = package.Id;

                                if (item.SupplyInvoiceOrderItem?.SupplyOrderItem != null) item.UnitPrice = item.SupplyInvoiceOrderItem.SupplyOrderItem.UnitPrice;

                                return item;
                            })
                        );
                    }

                    foreach (PackingListPackage package in packingList.PackingListBoxes.Where(p => !p.IsNew())) {
                        if (!package.Width.Equals(0) && !package.Height.Equals(0) && !package.Lenght.Equals(0))
                            package.CBM = Math.Round(package.Lenght * package.Height * package.Width * 0.000001, 6);

                        packingListPackageRepository.Update(package);

                        packingListPackageOrderItemRepository.Add(
                            package.PackingListPackageOrderItems.Where(i => i.IsNew()).Select(item => {
                                item.SupplyInvoiceOrderItemId = item.SupplyInvoiceOrderItem.Id;
                                item.PackingListPackageId = package.Id;

                                if (item.SupplyInvoiceOrderItem?.SupplyOrderItem != null) item.UnitPrice = item.SupplyInvoiceOrderItem.SupplyOrderItem.UnitPrice;

                                return item;
                            })
                        );

                        packingListPackageOrderItemRepository.Update(package.PackingListPackageOrderItems.Where(i => !i.IsNew()));
                    }

                    foreach (PackingListPackage package in packingList.PackingListPallets.Where(p => p.IsNew())) {
                        if (!package.Width.Equals(0) && !package.Height.Equals(0) && !package.Lenght.Equals(0))
                            package.CBM = Math.Round(package.Lenght * package.Height * package.Width * 0.000001, 6);

                        package.Type = PackingListPackageType.Pallet;
                        package.PackingListId = packingList.Id;

                        package.Id = packingListPackageRepository.Add(package);

                        packingListPackageOrderItemRepository.Add(
                            package.PackingListPackageOrderItems.Select(item => {
                                item.SupplyInvoiceOrderItemId = item.SupplyInvoiceOrderItem.Id;
                                item.PackingListPackageId = package.Id;

                                if (item.SupplyInvoiceOrderItem?.SupplyOrderItem != null) item.UnitPrice = item.SupplyInvoiceOrderItem.SupplyOrderItem.UnitPrice;

                                return item;
                            })
                        );
                    }

                    foreach (PackingListPackage package in packingList.PackingListPallets.Where(p => !p.IsNew())) {
                        if (!package.Width.Equals(0) && !package.Height.Equals(0) && !package.Lenght.Equals(0))
                            package.CBM = Math.Round(package.Lenght * package.Height * package.Width * 0.000001, 6);

                        packingListPackageRepository.Update(package);

                        packingListPackageOrderItemRepository.Add(
                            package.PackingListPackageOrderItems.Where(i => i.IsNew()).Select(item => {
                                item.SupplyInvoiceOrderItemId = item.SupplyInvoiceOrderItem.Id;
                                item.PackingListPackageId = package.Id;

                                if (item.SupplyInvoiceOrderItem?.SupplyOrderItem != null) item.UnitPrice = item.SupplyInvoiceOrderItem.SupplyOrderItem.UnitPrice;

                                return item;
                            })
                        );

                        packingListPackageOrderItemRepository.Update(package.PackingListPackageOrderItems.Where(i => !i.IsNew()));
                    }

                    if (packingList.DynamicProductPlacementColumns.Any()) {
                        IDynamicProductPlacementRepository dynamicProductPlacementRepository =
                            _supplyUkraineRepositoriesFactory.NewDynamicProductPlacementRepository(connection);
                        IDynamicProductPlacementRowRepository dynamicProductPlacementRowRepository =
                            _supplyUkraineRepositoriesFactory.NewDynamicProductPlacementRowRepository(connection);
                        IDynamicProductPlacementColumnRepository dynamicProductPlacementColumnRepository =
                            _supplyUkraineRepositoriesFactory.NewDynamicProductPlacementColumnRepository(connection);
                        IProductPlacementRepository productPlacementRepository =
                            _productRepositoriesFactory.NewProductPlacementRepository(connection);

                        PackingList fromDb = packingListRepository.GetById(packingList.Id);

                        foreach (DynamicProductPlacementColumn column in fromDb
                                     .DynamicProductPlacementColumns
                                     .Where(c => !packingList.DynamicProductPlacementColumns.Any(col => col.Id.Equals(c.Id))))
                            if (!column.DynamicProductPlacementRows.Any(r => r.DynamicProductPlacements.Any(p => p.IsApplied)))
                                dynamicProductPlacementColumnRepository.RemoveById(column.Id);

                        foreach (DynamicProductPlacementColumn column in packingList.DynamicProductPlacementColumns) {
                            column.FromDate =
                                column.FromDate.Year.Equals(1)
                                    ? DateTime.UtcNow.Date
                                    : column.FromDate.AddHours(5).Date;

                            column.PackingListId = fromDb.Id;

                            if (column.IsNew())
                                column.Id = dynamicProductPlacementColumnRepository.Add(column);
                            else
                                dynamicProductPlacementColumnRepository.Update(column);

                            List<DynamicProductPlacementRow> existingRows =
                                dynamicProductPlacementRowRepository
                                    .GetAllByColumnIdExceptProvidedIds(
                                        column.Id,
                                        column.DynamicProductPlacementRows.Where(r => !r.IsNew()).Select(r => r.Id)
                                    );

                            foreach (DynamicProductPlacementRow row in existingRows)
                                if (!row.DynamicProductPlacements.Any(p => p.IsApplied))
                                    dynamicProductPlacementRowRepository.RemoveById(row.Id);

                            foreach (DynamicProductPlacementRow row in column.DynamicProductPlacementRows.Where(r => r.PackingListPackageOrderItem != null))
                                if (fromDb.PackingListPackageOrderItems.Any(i => i.Id.Equals(row.PackingListPackageOrderItem.Id))) {
                                    if (row.IsNew()) {
                                        row.DynamicProductPlacementColumnId = column.Id;
                                        row.PackingListPackageOrderItemId = row.PackingListPackageOrderItem.Id;

                                        row.Id = dynamicProductPlacementRowRepository.Add(row);

                                        ProductPlacement existingProductPlacement =
                                            productPlacementRepository.GetLastByProductId(row.PackingListPackageOrderItem.SupplyInvoiceOrderItem.ProductId);

                                        if (existingProductPlacement == null)
                                            dynamicProductPlacementRepository.Add(new DynamicProductPlacement {
                                                DynamicProductPlacementRowId = row.Id,
                                                StorageNumber = "N",
                                                CellNumber = "N",
                                                RowNumber = "N",
                                                Qty = row.Qty
                                            });
                                        else
                                            dynamicProductPlacementRepository.Add(new DynamicProductPlacement {
                                                DynamicProductPlacementRowId = row.Id,
                                                StorageNumber = existingProductPlacement.StorageNumber,
                                                CellNumber = existingProductPlacement.CellNumber,
                                                RowNumber = existingProductPlacement.RowNumber,
                                                Qty = row.Qty
                                            });
                                    } else {
                                        dynamicProductPlacementRowRepository.Update(row);
                                    }
                                }
                        }
                    } else {
                        IDynamicProductPlacementColumnRepository dynamicProductPlacementColumnRepository =
                            _supplyUkraineRepositoriesFactory.NewDynamicProductPlacementColumnRepository(connection);

                        PackingList fromDb = packingListRepository.GetById(packingList.Id);

                        foreach (DynamicProductPlacementColumn column in fromDb
                                     .DynamicProductPlacementColumns)
                            if (!column.DynamicProductPlacementRows.Any(r => r.DynamicProductPlacements.Any(p => p.IsApplied)))
                                dynamicProductPlacementColumnRepository.RemoveById(column.Id);
                    }

                    packingListRepository.Update(packingList);
                }

                foreach (PackingList packingList in message.SupplyInvoice.PackingLists.Where(p => p.IsNew() && !p.Deleted)) {
                    packingList.SupplyInvoiceId = message.SupplyInvoice.Id;

                    if (packingList.InvoiceDocuments.Any(d => d.IsNew())) {
                        packingList.IsDocumentsAdded = true;

                        packingList.Id = packingListRepository.Add(packingList);

                        invoiceDocumentRepository.Add(
                            packingList.InvoiceDocuments
                                .Where(d => d.IsNew())
                                .Select(d => {
                                    d.PackingListId = packingList.Id;

                                    return d;
                                })
                        );
                    } else {
                        packingList.Id = packingListRepository.Add(packingList);
                    }

                    if (packingList.PackingListPackageOrderItems.Any())
                        packingListPackageOrderItemRepository.Add(
                            packingList.PackingListPackageOrderItems.Select(item => {
                                item.SupplyInvoiceOrderItemId = item.SupplyInvoiceOrderItem.Id;
                                item.PackingListId = packingList.Id;

                                if (item.SupplyInvoiceOrderItem?.SupplyOrderItem != null) item.UnitPrice = item.SupplyInvoiceOrderItem.SupplyOrderItem.UnitPrice;

                                return item;
                            })
                        );

                    if (packingList.PackingListPallets.Any()) {
                        packingListPackageRepository.Add(packingList.PackingListPallets.Where(p => !p.PackingListPackageOrderItems.Any()));

                        foreach (PackingListPackage package in packingList.PackingListPallets.Where(p => p.PackingListPackageOrderItems.Any())) {
                            if (!package.Width.Equals(0) && !package.Height.Equals(0) && !package.Lenght.Equals(0))
                                package.CBM = Math.Round(package.Lenght * package.Height * package.Width * 0.000001, 6);

                            package.Type = PackingListPackageType.Pallet;
                            package.PackingListId = packingList.Id;

                            package.Id = packingListPackageRepository.Add(package);

                            packingListPackageOrderItemRepository.Add(
                                package.PackingListPackageOrderItems.Select(item => {
                                    item.SupplyInvoiceOrderItemId = item.SupplyInvoiceOrderItem.Id;
                                    item.PackingListPackageId = package.Id;

                                    return item;
                                })
                            );
                        }
                    }

                    if (packingList.PackingListBoxes.Any()) {
                        packingListPackageRepository.Add(packingList.PackingListBoxes.Where(p => !p.PackingListPackageOrderItems.Any()));

                        foreach (PackingListPackage package in packingList.PackingListBoxes.Where(p => p.PackingListPackageOrderItems.Any())) {
                            if (!package.Width.Equals(0) && !package.Height.Equals(0) && !package.Lenght.Equals(0))
                                package.CBM = Math.Round(package.Lenght * package.Height * package.Width * 0.000001, 6);

                            package.Type = PackingListPackageType.Box;
                            package.PackingListId = packingList.Id;

                            package.Id = packingListPackageRepository.Add(package);

                            packingListPackageOrderItemRepository.Add(
                                package.PackingListPackageOrderItems.Select(item => {
                                    item.SupplyInvoiceOrderItemId = item.SupplyInvoiceOrderItem.Id;
                                    item.PackingListPackageId = package.Id;

                                    if (item.SupplyInvoiceOrderItem?.SupplyOrderItem != null) item.UnitPrice = item.SupplyInvoiceOrderItem.SupplyOrderItem.UnitPrice;

                                    return item;
                                })
                            );
                        }
                    }
                }
            } else {
                packingListRepository.RemoveAllByInvoiceId(message.SupplyInvoice.Id);
            }

            Sender.Tell(
                new Tuple<SupplyInvoice, string>(
                    _supplyRepositoriesFactory.NewSupplyInvoiceRepository(connection).GetByNetIdWithAllIncludes(message.SupplyInvoice.NetUid),
                    string.Empty
                )
            );

            packingListRepository.SetIsDocumentsAddedFalse();
        } else {
            Sender.Tell(
                new Tuple<SupplyInvoice, string>(
                    _supplyRepositoriesFactory.NewSupplyInvoiceRepository(connection).GetByNetIdWithAllIncludes(message.SupplyInvoice.NetUid),
                    string.Empty
                )
            );
        }
    }

    private void ProcessAddOrUpdatePackingListFromFileMessage(AddOrUpdatePackingListFromFileMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (string.IsNullOrEmpty(message.PackingList.InvNo)) throw new Exception(PackingListResourceNames.EMPTY_NUMBER);

            ISupplyInvoiceRepository supplyInvoiceRepository = _supplyRepositoriesFactory.NewSupplyInvoiceRepository(connection);

            SupplyInvoice supplyInvoice = supplyInvoiceRepository.GetByNetIdWithoutIncludes(message.SupplyInvoiceNetId);

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            if (supplyInvoice == null) throw new Exception(PackingListResourceNames.INVOICE_NOT_EXISTS);

            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
            IPackingListRepository packingListRepository = _supplyRepositoriesFactory.NewPackingListRepository(connection);
            ISupplyOrderItemRepository supplyOrderItemRepository = _supplyRepositoriesFactory.NewSupplyOrderItemRepository(connection);
            IActReconciliationRepository actReconciliationRepository = _supplyUkraineRepositoriesFactory.NewActReconciliationRepository(connection);
            IActReconciliationItemRepository actReconciliationItemRepository = _supplyUkraineRepositoriesFactory.NewActReconciliationItemRepository(connection);
            ISupplyInvoiceOrderItemRepository supplyInvoiceOrderItemRepository = _supplyRepositoriesFactory.NewSupplyInvoiceOrderItemRepository(connection);
            IPackingListPackageOrderItemRepository packingListPackageOrderItemRepository =
                _supplyRepositoriesFactory.NewPackingListPackageOrderItemRepository(connection);

            supplyInvoice = supplyInvoiceRepository.GetByNetIdForDocumentUpload(message.SupplyInvoiceNetId);

            List<ParsedProduct> parsedProducts =
                _xlsFactoryManager
                    .NewParseConfigurationXlsManager()
                    .GetProductsFromSupplyDocumentsByConfiguration(
                        message.PathToFile,
                        message.ParseConfiguration
                    );

            foreach (ParsedProduct parsedProduct in parsedProducts) {
                Product product = getSingleProductRepository.GetProductByVendorCode(parsedProduct.VendorCode);

                if (product == null)
                    throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.NoProductByVendorCode, 0, 0, parsedProduct.VendorCode);

                if (supplyInvoice.SupplyOrder.SupplyOrderItems.Any(i => i.ProductId.Equals(product.Id))) {
                    SupplyOrderItem fromList =
                        supplyOrderItemRepository.GetByOrderAndProductIdAndQtyWithInvoiceItemsIfExists(supplyInvoice.SupplyOrderId, product.Id, parsedProduct.Qty,
                            parsedProduct.UnitPrice)
                        ?? supplyOrderItemRepository.GetByOrderAndProductIdWithInvoiceItemsIfExists(supplyInvoice.SupplyOrderId, product.Id);

                    fromList.IsUpdated = true;
                    fromList.NetWeight = parsedProduct.NetWeight;
                    fromList.GrossWeight = parsedProduct.GrossWeight;

                    supplyOrderItemRepository.Update(fromList);
                }
                //supplyInvoice
                //    .SupplyOrder
                //    .SupplyOrderItems
                //    .Add(new SupplyOrderItem {
                //        Qty = parsedProduct.Qty,
                //        NetWeight = parsedProduct.NetWeight,
                //        GrossWeight = parsedProduct.GrossWeight,
                //        ProductId = product.Id,
                //        SupplyOrderId = supplyInvoice.SupplyOrder.Id
                //    });
            }

            supplyInvoice.SupplyOrder.NetPrice = decimal.Round(supplyInvoice.SupplyOrder.SupplyOrderItems.Sum(i => i.TotalAmount), 2, MidpointRounding.AwayFromZero);
            supplyInvoice.SupplyOrder.Qty = Math.Round(supplyInvoice.SupplyOrder.SupplyOrderItems.Sum(i => i.Qty), 2, MidpointRounding.AwayFromZero);

            supplyOrderRepository.Update(supplyInvoice.SupplyOrder);

            supplyOrderItemRepository
                .Add(
                    supplyInvoice
                        .SupplyOrder
                        .SupplyOrderItems
                        .Where(i => i.IsNew())
                );

            supplyInvoice = supplyInvoiceRepository.GetByNetIdForDocumentUpload(message.SupplyInvoiceNetId);

            IEnumerable<SupplyInvoiceOrderItem> supplyInvoiceOrderItemsToAdd =
                from parsedProduct in parsedProducts
                let product = getSingleProductRepository.GetProductByVendorCode(parsedProduct.VendorCode)
                let fromList = supplyInvoice.SupplyOrder.SupplyOrderItems.FirstOrDefault(i => i.ProductId.Equals(product.Id))
                where !supplyInvoice.SupplyInvoiceOrderItems.Any(i => i.ProductId.Equals(product.Id))
                select new SupplyInvoiceOrderItem
                    { Qty = parsedProduct.Qty, SupplyOrderItem = fromList, SupplyOrderItemId = fromList?.Id, SupplyInvoiceId = supplyInvoice.Id, ProductId = product.Id };

            ActReconciliation actReconciliation = actReconciliationRepository.GetBySupplyInvoiceId(supplyInvoice.Id);

            if (actReconciliation == null) {
                actReconciliation = new ActReconciliation {
                    FromDate = supplyInvoice.DateFrom ?? supplyInvoice.Created,
                    ResponsibleId = user.Id,
                    SupplyInvoiceId = supplyInvoice.Id,
                    Comment = supplyInvoice.Comment,
                    Number = supplyInvoice.ServiceNumber
                };

                actReconciliation.Id = actReconciliationRepository.Add(actReconciliation);

                foreach (SupplyInvoiceOrderItem item in supplyInvoice.SupplyInvoiceOrderItems)
                    actReconciliationItemRepository
                        .Add(new ActReconciliationItem {
                            ActReconciliationId = actReconciliation.Id,
                            SupplyInvoiceOrderItemId = item.Id,
                            ProductId = item.ProductId,
                            HasDifference = true,
                            NegativeDifference = true,
                            QtyDifference = item.Qty,
                            ActualQty = 0d,
                            OrderedQty = item.Qty,
                            UnitPrice = item.SupplyOrderItem.UnitPrice,
                            NetWeight = item.SupplyOrderItem.NetWeight
                        });
            }

            if (supplyInvoiceOrderItemsToAdd.Any()) {
                foreach (SupplyInvoiceOrderItem item in supplyInvoiceOrderItemsToAdd) {
                    SupplyOrderItem fromList =
                        supplyOrderItemRepository.GetByOrderAndProductIdWithInvoiceItemsIfExists(supplyInvoice.SupplyOrderId, item.ProductId)
                        ?? supplyOrderItemRepository.GetByOrderAndProductIdWithInvoiceItemsIfExists(supplyInvoice.SupplyOrderId, item.Id);

                    if (fromList.Qty < item.Qty + fromList.SupplyInvoiceOrderItems.Sum(i => i.Qty)) {
                        fromList.Qty = item.Qty + fromList.SupplyInvoiceOrderItems.Sum(i => i.Qty);

                        supplyOrderItemRepository.UpdateQty(fromList);
                    }

                    actReconciliationItemRepository
                        .Add(new ActReconciliationItem {
                            ActReconciliationId = actReconciliation.Id,
                            SupplyInvoiceOrderItemId =
                                supplyInvoiceOrderItemRepository
                                    .Add(item),
                            ProductId = item.ProductId,
                            HasDifference = true,
                            NegativeDifference = true,
                            QtyDifference = item.Qty,
                            ActualQty = 0d,
                            OrderedQty = item.Qty,
                            UnitPrice = item.UnitPrice,
                            NetWeight = item.SupplyOrderItem?.NetWeight ?? 0
                        });
                }

                supplyInvoice = supplyInvoiceRepository.GetByNetIdForDocumentUpload(message.SupplyInvoiceNetId);
            }

            if (supplyInvoice.PackingLists.Any(p => p.InvNo.Equals(message.PackingList.InvNo))) {
                PackingList packingListFromList = supplyInvoice.PackingLists.First(p => p.InvNo.Equals(message.PackingList.InvNo));

                packingListFromList.FromDate =
                    message.PackingList.FromDate.Year.Equals(1)
                        ? DateTime.UtcNow.Date
                        : message.PackingList.FromDate;

                message.PackingList.Id = packingListFromList.Id;

                foreach (ParsedProduct parsedProduct in parsedProducts) {
                    Product product = getSingleProductRepository.GetProductByVendorCode(parsedProduct.VendorCode);

                    //TODO Can be empty, need to check
                    SupplyOrderItem supplyOrderItemFromList = supplyInvoice.SupplyOrder.SupplyOrderItems.FirstOrDefault(i =>
                        i.ProductId.Equals(product.Id) && parsedProduct.Qty == i.Qty && parsedProduct.UnitPrice == i.UnitPrice);

                    SupplyInvoiceOrderItem invoiceOrderItemFromList =
                        supplyOrderItemFromList != null
                            ? supplyInvoiceOrderItemRepository.GetByInvoiceAndSupplyOrderItemIds(supplyInvoice.Id, supplyOrderItemFromList.Id)
                            : supplyInvoiceOrderItemRepository.GetByInvoiceAndProductIds(supplyInvoice.Id, product.Id, parsedProduct.Qty, parsedProduct.UnitPrice);

                    if (invoiceOrderItemFromList == null) continue;

                    if (packingListFromList.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItemId.Equals(invoiceOrderItemFromList.Id))) {
                        PackingListPackageOrderItem packingListPackageOrderItemFromList =
                            packingListFromList.PackingListPackageOrderItems.First(i => i.SupplyInvoiceOrderItemId.Equals(invoiceOrderItemFromList.Id));

                        packingListPackageOrderItemFromList.IsUpdated = true;
                        packingListPackageOrderItemFromList.Qty = parsedProduct.Qty;
                        packingListPackageOrderItemFromList.UnitPrice = invoiceOrderItemFromList.UnitPrice;
                        packingListPackageOrderItemFromList.GrossWeight = parsedProduct.GrossWeight;
                        packingListPackageOrderItemFromList.NetWeight = parsedProduct.NetWeight;

                        packingListPackageOrderItemRepository
                            .Update(
                                packingListPackageOrderItemFromList
                            );

                        invoiceOrderItemFromList =
                            supplyInvoiceOrderItemRepository.GetByInvoiceAndSupplyOrderItemIds(supplyInvoice.Id, supplyOrderItemFromList.Id);

                        if (!(invoiceOrderItemFromList.Qty < invoiceOrderItemFromList.PackingListPackageOrderItems.Sum(i => i.Qty))) continue;

                        invoiceOrderItemFromList.Qty = invoiceOrderItemFromList.PackingListPackageOrderItems.Sum(i => i.Qty);

                        supplyInvoiceOrderItemRepository.Update(invoiceOrderItemFromList);

                        ActReconciliationItem reconciliationItem = actReconciliationItemRepository.GetBySupplyInvoiceOrderItemId(invoiceOrderItemFromList.Id);

                        if (reconciliationItem != null) {
                            reconciliationItem.QtyDifference = invoiceOrderItemFromList.Qty;
                            reconciliationItem.OrderedQty = invoiceOrderItemFromList.Qty;

                            actReconciliationItemRepository.FullUpdate(reconciliationItem);
                        } else {
                            actReconciliationItemRepository
                                .Add(new ActReconciliationItem {
                                    ActReconciliationId = actReconciliation.Id,
                                    SupplyInvoiceOrderItemId = invoiceOrderItemFromList.Id,
                                    ProductId = invoiceOrderItemFromList.ProductId,
                                    HasDifference = true,
                                    NegativeDifference = true,
                                    QtyDifference = invoiceOrderItemFromList.Qty,
                                    ActualQty = 0d,
                                    OrderedQty = invoiceOrderItemFromList.Qty,
                                    UnitPrice = invoiceOrderItemFromList.UnitPrice,
                                    NetWeight = invoiceOrderItemFromList.SupplyOrderItem?.NetWeight ?? 0
                                });
                        }

                        supplyOrderItemFromList =
                            supplyOrderItemRepository.GetByOrderAndProductIdWithInvoiceItemsIfExists(supplyInvoice.SupplyOrderId, product.Id);

                        if (supplyOrderItemFromList.Qty >= supplyOrderItemFromList.SupplyInvoiceOrderItems.Sum(i => i.Qty)) continue;

                        supplyOrderItemFromList.Qty = supplyOrderItemFromList.SupplyInvoiceOrderItems.Sum(i => i.Qty);

                        supplyOrderItemRepository.UpdateQty(supplyOrderItemFromList);
                    } else {
                        if (invoiceOrderItemFromList.Qty < parsedProduct.Qty + invoiceOrderItemFromList.PackingListPackageOrderItems.Sum(i => i.Qty)) {
                            invoiceOrderItemFromList.Qty = parsedProduct.Qty + invoiceOrderItemFromList.PackingListPackageOrderItems.Sum(i => i.Qty);

                            supplyInvoiceOrderItemRepository.Update(invoiceOrderItemFromList);

                            ActReconciliationItem reconciliationItem = actReconciliationItemRepository.GetBySupplyInvoiceOrderItemId(invoiceOrderItemFromList.Id);

                            if (reconciliationItem != null) {
                                reconciliationItem.QtyDifference = invoiceOrderItemFromList.Qty;
                                reconciliationItem.OrderedQty = invoiceOrderItemFromList.Qty;

                                actReconciliationItemRepository.FullUpdate(reconciliationItem);
                            } else {
                                actReconciliationItemRepository
                                    .Add(new ActReconciliationItem {
                                        ActReconciliationId = actReconciliation.Id,
                                        SupplyInvoiceOrderItemId = invoiceOrderItemFromList.Id,
                                        ProductId = invoiceOrderItemFromList.ProductId,
                                        HasDifference = true,
                                        NegativeDifference = true,
                                        QtyDifference = invoiceOrderItemFromList.Qty,
                                        ActualQty = 0d,
                                        OrderedQty = invoiceOrderItemFromList.Qty,
                                        UnitPrice = invoiceOrderItemFromList.UnitPrice,
                                        NetWeight = invoiceOrderItemFromList.SupplyOrderItem?.NetWeight ?? 0
                                    });
                            }

                            supplyOrderItemFromList =
                                supplyOrderItemRepository.GetByOrderAndProductIdWithInvoiceItemsIfExistsAndQty(supplyInvoice.SupplyOrderId, product.Id,
                                    supplyOrderItemFromList.Qty);


                            if (supplyOrderItemFromList.Qty < supplyOrderItemFromList.SupplyInvoiceOrderItems.Sum(i => i.Qty)) {
                                supplyOrderItemFromList.Qty = supplyOrderItemFromList.SupplyInvoiceOrderItems.Sum(i => i.Qty);

                                supplyOrderItemRepository.UpdateQty(supplyOrderItemFromList);
                            }
                        }

                        packingListFromList
                            .PackingListPackageOrderItems
                            .Add(
                                new PackingListPackageOrderItem {
                                    SupplyInvoiceOrderItemId = invoiceOrderItemFromList.Id,
                                    PackingListId = packingListFromList.Id,
                                    Qty = parsedProduct.Qty,
                                    UnitPrice = invoiceOrderItemFromList.UnitPrice,
                                    NetWeight = parsedProduct.NetWeight,
                                    GrossWeight = parsedProduct.GrossWeight,
                                    ProductIsImported = invoiceOrderItemFromList.ProductIsImported
                                }
                            );
                    }
                }

                packingListRepository.Update(packingListFromList);

                packingListPackageOrderItemRepository
                    .RemoveAllByPackingListIdExceptProvided(
                        packingListFromList.Id,
                        packingListFromList
                            .PackingListPackageOrderItems
                            .Where(i => !i.IsNew() && i.IsUpdated)
                            .Select(i => i.Id)
                    );

                packingListPackageOrderItemRepository
                    .Add(
                        packingListFromList
                            .PackingListPackageOrderItems
                            .Where(i => i.IsNew())
                    );
            } else {
                message.PackingList.FromDate =
                    message.PackingList.FromDate.Year.Equals(1)
                        ? DateTime.UtcNow.Date
                        : message.PackingList.FromDate;

                message.PackingList.SupplyInvoiceId = supplyInvoice.Id;

                message.PackingList.Id = packingListRepository.Add(message.PackingList);

                foreach (ParsedProduct parsedProduct in parsedProducts) {
                    Product product = getSingleProductRepository.GetProductByVendorCode(parsedProduct.VendorCode);

                    //TODO Can be empty, need to check
                    SupplyOrderItem supplyOrderItemFromList = supplyInvoice.SupplyOrder.SupplyOrderItems.FirstOrDefault(i =>
                        i.ProductId.Equals(product.Id) && parsedProduct.Qty == i.Qty && parsedProduct.UnitPrice == i.UnitPrice);

                    SupplyInvoiceOrderItem invoiceOrderItemFromList =
                        supplyOrderItemFromList != null
                            ? supplyInvoiceOrderItemRepository.GetByInvoiceAndSupplyOrderItemIds(supplyInvoice.Id, supplyOrderItemFromList.Id)
                            : supplyInvoiceOrderItemRepository.GetByInvoiceAndProductIds(supplyInvoice.Id, product.Id, parsedProduct.Qty, parsedProduct.UnitPrice);

                    if (invoiceOrderItemFromList == null) continue;

                    if (invoiceOrderItemFromList.Qty < parsedProduct.Qty + invoiceOrderItemFromList.PackingListPackageOrderItems.Sum(i => i.Qty)) {
                        invoiceOrderItemFromList.Qty = parsedProduct.Qty + invoiceOrderItemFromList.PackingListPackageOrderItems.Sum(i => i.Qty);

                        supplyInvoiceOrderItemRepository.Update(invoiceOrderItemFromList);

                        ActReconciliationItem reconciliationItem = actReconciliationItemRepository.GetBySupplyInvoiceOrderItemId(invoiceOrderItemFromList.Id);

                        if (reconciliationItem != null) {
                            reconciliationItem.QtyDifference = invoiceOrderItemFromList.Qty;
                            reconciliationItem.OrderedQty = invoiceOrderItemFromList.Qty;

                            actReconciliationItemRepository.FullUpdate(reconciliationItem);
                        } else {
                            actReconciliationItemRepository
                                .Add(new ActReconciliationItem {
                                    ActReconciliationId = actReconciliation.Id,
                                    SupplyInvoiceOrderItemId = invoiceOrderItemFromList.Id,
                                    ProductId = invoiceOrderItemFromList.ProductId,
                                    HasDifference = true,
                                    NegativeDifference = true,
                                    QtyDifference = invoiceOrderItemFromList.Qty,
                                    ActualQty = 0d,
                                    OrderedQty = invoiceOrderItemFromList.Qty,
                                    UnitPrice = invoiceOrderItemFromList.UnitPrice,
                                    NetWeight = invoiceOrderItemFromList.SupplyOrderItem?.NetWeight ?? 0
                                });
                        }

                        if (supplyOrderItemFromList != null) {
                            supplyOrderItemFromList =
                                supplyOrderItemRepository.GetByOrderAndProductIdWithInvoiceItemsIfExistsAndQty(supplyInvoice.SupplyOrderId, product.Id,
                                    supplyOrderItemFromList.Qty);

                            if (supplyOrderItemFromList.Qty < supplyOrderItemFromList.SupplyInvoiceOrderItems.Sum(i => i.Qty)) {
                                supplyOrderItemFromList.Qty = supplyOrderItemFromList.SupplyInvoiceOrderItems.Sum(i => i.Qty);

                                supplyOrderItemRepository.UpdateQty(supplyOrderItemFromList);
                            }
                        }
                    }

                    message
                        .PackingList
                        .PackingListPackageOrderItems
                        .Add(
                            new PackingListPackageOrderItem {
                                SupplyInvoiceOrderItemId = invoiceOrderItemFromList.Id,
                                PackingListId = message.PackingList.Id,
                                Qty = parsedProduct.Qty,
                                UnitPrice = invoiceOrderItemFromList.UnitPrice,
                                NetWeight = parsedProduct.NetWeight,
                                GrossWeight = parsedProduct.GrossWeight,
                                ProductIsImported = invoiceOrderItemFromList.ProductIsImported
                            }
                        );
                }

                packingListPackageOrderItemRepository
                    .Add(
                        message
                            .PackingList
                            .PackingListPackageOrderItems
                    );
            }

            supplyInvoice = supplyInvoiceRepository.GetByNetIdWithAllIncludes(message.SupplyInvoiceNetId);

            ActorReferenceManager.Instance.Get(SupplyActorNames.SUPPLY_INVOICE_ACTOR).Tell(new UpdateSupplyInvoiceItemGrossPriceMessage(
                supplyInvoiceRepository.GetBySupplyOrderId(supplyInvoice.SupplyOrder.Id).Select(x => x.Id),
                message.UserNetId
            ));

            supplyInvoice = supplyInvoiceRepository.GetByNetIdWithAllIncludes(message.SupplyInvoiceNetId);

            Sender.Tell(supplyInvoice.PackingLists.First(p => p.InvNo.Equals(message.PackingList.InvNo)));

            ActorReferenceManager.Instance.Get(BaseActorNames.PRODUCTS_MANAGEMENT_ACTOR)
                .Tell(new UpdateInvoiceProductSpecificationAssignmentsMessage(supplyInvoice.NetUid));
        } catch (SupplyDocumentParseException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdatePackingListInvoiceDocumentsMessage(UpdatePackingListInvoiceDocumentsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IInvoiceDocumentRepository invoiceDocumentRepository = _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection);

        if (message.PackingList.InvoiceDocuments.Any(d => d.IsNew()))
            invoiceDocumentRepository.Add(
                message.PackingList.InvoiceDocuments
                    .Where(d => d.IsNew())
                    .Select(d => {
                        d.PackingListId = message.PackingList.Id;

                        return d;
                    })
            );

        if (message.PackingList.InvoiceDocuments.Any(d => !d.IsNew())) invoiceDocumentRepository.Update(message.PackingList.InvoiceDocuments.Where(d => !d.IsNew()));

        Sender.Tell(_supplyRepositoriesFactory.NewPackingListRepository(connection).GetByNetId(message.PackingList.NetUid));
    }

    private void ProcessUpdatePackingListProductPlacementInfoMessage(UpdatePackingListProductPlacementInfoMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IPackingListRepository packingListRepository = _supplyRepositoriesFactory.NewPackingListRepository(connection);

            PackingList packingListFromDb =
                packingListRepository
                    .GetByIdForPlacement(
                        message.PackingList.Id
                    );

            if (packingListFromDb == null) throw new Exception("PackingList with specified ID does not exists");
            if (!packingListFromDb.PackingListPackageOrderItems.All(i => i.IsPlaced))
                throw new Exception("All PackingList items should be placed before saving placement information");
            if (!packingListFromDb.PackingListBoxes.All(b => b.PackingListPackageOrderItems.All(i => i.IsPlaced)))
                throw new Exception("All PackingList items in all boxes should be placed before saving placement information");
            if (!packingListFromDb.PackingListPallets.All(p => p.PackingListPackageOrderItems.All(i => i.IsPlaced)))
                throw new Exception("All PackingList items in all pallets should be placed before saving placement information");

            IPackingListPackageOrderItemRepository packingListPackageOrderItemRepository =
                _supplyRepositoriesFactory.NewPackingListPackageOrderItemRepository(connection);

            foreach (PackingListPackageOrderItem item in message
                         .PackingList
                         .PackingListPackageOrderItems
                         .Where(i => i.ProductPlacements.Any(p => p.IsNew()))) {
                PackingListPackageOrderItem itemFromDb = packingListPackageOrderItemRepository.GetByIdForPlacement(item.Id);

                if (itemFromDb != null) {
                    double placedQty = itemFromDb.ProductPlacements.Sum(p => p.Qty);

                    if (itemFromDb.Qty - placedQty < item.ProductPlacements.Where(p => p.IsNew()).Sum(p => p.Qty))
                        throw new Exception(
                            string.Format(
                                "For product {0} has available for placement {1} and you specified {2}",
                                itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode,
                                itemFromDb.Qty - placedQty,
                                item.ProductPlacements.Where(p => p.IsNew()).Sum(p => p.Qty)
                            )
                        );
                    if (!item.ProductPlacements.Where(p => p.IsNew()).All(p => !string.IsNullOrEmpty(p.StorageNumber) && !string.IsNullOrEmpty(p.CellNumber)))
                        throw new Exception(
                            string.Format(
                                "For product {0} not all ProductPlacements has specified cells and rows numbers",
                                itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode
                            )
                        );
                }
            }

            foreach (PackingListPackage box in message.PackingList.PackingListBoxes)
            foreach (PackingListPackageOrderItem item in box
                         .PackingListPackageOrderItems
                         .Where(i => i.ProductPlacements.Any(p => p.IsNew()))) {
                PackingListPackageOrderItem itemFromDb = packingListPackageOrderItemRepository.GetByIdForPlacement(item.Id);

                if (itemFromDb != null) {
                    double placedQty = itemFromDb.ProductPlacements.Sum(p => p.Qty);

                    if (itemFromDb.Qty - placedQty < item.ProductPlacements.Where(p => p.IsNew()).Sum(p => p.Qty))
                        throw new Exception(
                            string.Format(
                                "For product {0} in box has available for placement {1} and you specified {2}",
                                itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode,
                                itemFromDb.Qty - placedQty,
                                item.ProductPlacements.Where(p => p.IsNew()).Sum(p => p.Qty)
                            )
                        );
                    if (!item.ProductPlacements.Where(p => p.IsNew()).All(p => !string.IsNullOrEmpty(p.StorageNumber) && !string.IsNullOrEmpty(p.CellNumber)))
                        throw new Exception(
                            string.Format(
                                "For product {0} in box not all ProductPlacements has specified cells and rows numbers",
                                itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode
                            )
                        );
                }
            }

            foreach (PackingListPackage pallet in message.PackingList.PackingListPallets)
            foreach (PackingListPackageOrderItem item in pallet
                         .PackingListPackageOrderItems
                         .Where(i => i.ProductPlacements.Any(p => p.IsNew()))) {
                PackingListPackageOrderItem itemFromDb = packingListPackageOrderItemRepository.GetByIdForPlacement(item.Id);

                if (itemFromDb != null) {
                    double placedQty = itemFromDb.ProductPlacements.Sum(p => p.Qty);

                    if (itemFromDb.Qty - placedQty < item.ProductPlacements.Where(p => p.IsNew()).Sum(p => p.Qty))
                        throw new Exception(
                            string.Format(
                                "For product {0} in pallet has available for placement {1} and you specified {2}",
                                itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode,
                                itemFromDb.Qty - placedQty,
                                item.ProductPlacements.Where(p => p.IsNew()).Sum(p => p.Qty)
                            )
                        );
                    if (!item.ProductPlacements.Where(p => p.IsNew()).All(p => !string.IsNullOrEmpty(p.StorageNumber) && !string.IsNullOrEmpty(p.CellNumber)))
                        throw new Exception(
                            string.Format(
                                "For product {0} in pallet not all ProductPlacements has specified cells and rows numbers",
                                itemFromDb.SupplyInvoiceOrderItem.Product.VendorCode
                            )
                        );
                }
            }

            long storageId = 0;

            if (packingListFromDb.PackingListPackageOrderItems.Any())
                storageId =
                    packingListFromDb.PackingListPackageOrderItems.First(i => i.ProductIncomeItem != null).ProductIncomeItem.ProductIncome.StorageId;
            else if (packingListFromDb.PackingListBoxes.Any())
                storageId =
                    packingListFromDb
                        .PackingListBoxes
                        .First()
                        .PackingListPackageOrderItems.First(i => i.ProductIncomeItem != null).ProductIncomeItem.ProductIncome.StorageId;
            else if (packingListFromDb.PackingListPallets.Any())
                storageId =
                    packingListFromDb
                        .PackingListPallets
                        .First()
                        .PackingListPackageOrderItems.First(i => i.ProductIncomeItem != null).ProductIncomeItem.ProductIncome.StorageId;

            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);

            foreach (PackingListPackageOrderItem item in message
                         .PackingList
                         .PackingListPackageOrderItems
                         .Where(i => i.ProductPlacements.Any(p => p.IsNew()))) {
                PackingListPackageOrderItem itemFromDb = packingListPackageOrderItemRepository.GetByIdForPlacement(item.Id);

                if (itemFromDb != null)
                    productPlacementRepository
                        .Add(
                            item
                                .ProductPlacements
                                .Where(p => p.IsNew())
                                .Select(placement => {
                                    placement.ProductId = itemFromDb.SupplyInvoiceOrderItem.ProductId;
                                    placement.StorageId = storageId;

                                    ProductPlacement placementFromDb =
                                        productPlacementRepository
                                            .GetIfExists(
                                                placement.RowNumber,
                                                placement.CellNumber,
                                                placement.StorageNumber,
                                                placement.ProductId,
                                                placement.StorageId
                                            );

                                    if (placementFromDb != null) {
                                        if (placementFromDb.Deleted) {
                                            placementFromDb.Qty = placement.Qty;

                                            productPlacementRepository.Restore(placementFromDb);
                                        } else {
                                            placementFromDb.Qty += placement.Qty;

                                            productPlacementRepository.UpdateQty(placementFromDb);
                                        }
                                    } else {
                                        productPlacementRepository
                                            .Add(new ProductPlacement {
                                                ProductId = placement.ProductId,
                                                StorageId = placement.StorageId,
                                                Qty = placement.Qty,
                                                StorageNumber = placement.StorageNumber,
                                                CellNumber = placement.CellNumber,
                                                RowNumber = placement.RowNumber
                                            });
                                    }

                                    placement.PackingListPackageOrderItemId = itemFromDb.Id;

                                    return placement;
                                })
                        );
            }

            foreach (PackingListPackage box in message.PackingList.PackingListBoxes)
            foreach (PackingListPackageOrderItem item in box
                         .PackingListPackageOrderItems
                         .Where(i => i.ProductPlacements.Any(p => p.IsNew()))) {
                PackingListPackageOrderItem itemFromDb = packingListPackageOrderItemRepository.GetByIdForPlacement(item.Id);

                if (itemFromDb != null)
                    productPlacementRepository
                        .Add(
                            item
                                .ProductPlacements
                                .Where(p => p.IsNew())
                                .Select(placement => {
                                    placement.ProductId = itemFromDb.SupplyInvoiceOrderItem.ProductId;
                                    placement.StorageId = storageId;

                                    ProductPlacement placementFromDb =
                                        productPlacementRepository
                                            .GetIfExists(
                                                placement.RowNumber,
                                                placement.CellNumber,
                                                placement.StorageNumber,
                                                placement.ProductId,
                                                placement.StorageId
                                            );

                                    if (placementFromDb != null) {
                                        if (placementFromDb.Deleted) {
                                            placementFromDb.Qty = placement.Qty;

                                            productPlacementRepository.Restore(placementFromDb);
                                        } else {
                                            placementFromDb.Qty += placement.Qty;

                                            productPlacementRepository.UpdateQty(placementFromDb);
                                        }
                                    } else {
                                        productPlacementRepository
                                            .Add(new ProductPlacement {
                                                ProductId = placement.ProductId,
                                                StorageId = placement.StorageId,
                                                Qty = placement.Qty,
                                                StorageNumber = placement.StorageNumber,
                                                CellNumber = placement.CellNumber,
                                                RowNumber = placement.RowNumber
                                            });
                                    }

                                    placement.PackingListPackageOrderItemId = itemFromDb.Id;

                                    return placement;
                                })
                        );
            }

            foreach (PackingListPackage pallet in message.PackingList.PackingListPallets)
            foreach (PackingListPackageOrderItem item in pallet
                         .PackingListPackageOrderItems
                         .Where(i => i.ProductPlacements.Any(p => p.IsNew()))) {
                PackingListPackageOrderItem itemFromDb = packingListPackageOrderItemRepository.GetByIdForPlacement(item.Id);

                if (itemFromDb != null)
                    productPlacementRepository
                        .Add(
                            item
                                .ProductPlacements
                                .Where(p => p.IsNew())
                                .Select(placement => {
                                    placement.ProductId = itemFromDb.SupplyInvoiceOrderItem.ProductId;
                                    placement.StorageId = storageId;

                                    ProductPlacement placementFromDb =
                                        productPlacementRepository
                                            .GetIfExists(
                                                placement.RowNumber,
                                                placement.CellNumber,
                                                placement.StorageNumber,
                                                placement.ProductId,
                                                placement.StorageId
                                            );

                                    if (placementFromDb != null) {
                                        if (placementFromDb.Deleted) {
                                            placementFromDb.Qty = placement.Qty;

                                            productPlacementRepository.Restore(placementFromDb);
                                        } else {
                                            placementFromDb.Qty += placement.Qty;

                                            productPlacementRepository.UpdateQty(placementFromDb);
                                        }
                                    } else {
                                        productPlacementRepository
                                            .Add(new ProductPlacement {
                                                ProductId = placement.ProductId,
                                                StorageId = placement.StorageId,
                                                Qty = placement.Qty,
                                                StorageNumber = placement.StorageNumber,
                                                CellNumber = placement.CellNumber,
                                                RowNumber = placement.RowNumber
                                            });
                                    }

                                    placement.PackingListPackageOrderItemId = itemFromDb.Id;

                                    return placement;
                                })
                        );
            }

            Sender.Tell(
                packingListRepository
                    .GetByNetIdForPlacement(
                        packingListFromDb.NetUid
                    )
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdatePackingListFromFileMessage(UpdatePackingListFromFileMessage message) {
        bool isErrorInParsing = false;

        List<PackingListItemWithVendorCode> parsedItems = null;

        try {
            parsedItems =
                _xlsFactoryManager
                    .NewParseConfigurationXlsManager()
                    .GetPackingListItemsWithVendorCodesFromXlsx(message.PathToFile);
        } catch (Exception) {
            isErrorInParsing = true;
        }

        if (isErrorInParsing) {
            Sender.Tell(new Tuple<SupplyOrder, string>(null, "File parsing error."));
        } else {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);

            SupplyOrder orderFromDb = supplyOrderRepository.GetByNetIdForPlacement(message.SupplyOrderNetId);

            if (orderFromDb != null) {
                SupplyInvoice invoice = orderFromDb.SupplyInvoices.FirstOrDefault(i => i.PackingLists.Any(p => p.NetUid.Equals(message.PackingListNetId)));

                if (invoice != null) {
                    PackingList packingList = invoice.PackingLists.First(p => p.NetUid.Equals(message.PackingListNetId));

                    foreach (PackingListPackageOrderItem item in packingList.PackingListPackageOrderItems) {
                        PackingListItemWithVendorCode parsedItem =
                            parsedItems.FirstOrDefault(p => p.VendorCode.ToLower().Equals(item.SupplyInvoiceOrderItem.Product.VendorCode.ToLower()));

                        if (parsedItem != null) {
                            item.IsErrorInPlaced = !item.Qty.Equals(parsedItem.Qty);
                            item.UploadedQty = parsedItem.Qty;
                            item.NetWeight = parsedItem.NetWeight;
                            item.GrossWeight = parsedItem.GrossWeight;
                            item.UnitPrice = parsedItem.UnitPrice;
                            item.IsReadyToPlaced = true;
                        } else {
                            item.IsErrorInPlaced = true;
                            item.GrossWeight = 0;
                            item.NetWeight = 0;
                            item.UnitPrice = decimal.Zero;
                            item.UploadedQty = 0;
                            item.IsReadyToPlaced = true;
                        }
                    }

                    _supplyRepositoriesFactory
                        .NewPackingListPackageOrderItemRepository(connection)
                        .Update(
                            packingList.PackingListPackageOrderItems
                        );
                }
            }

            Sender.Tell(new Tuple<SupplyOrder, string>(supplyOrderRepository.GetByNetIdForPlacement(message.SupplyOrderNetId), string.Empty));
        }

        NoltFolderManager.DeleteFile(message.PathToFile);
    }

    private void ProcessSetReadyToPlaceToPackingListPackageOrderItemByNetIdMessage(SetReadyToPlaceToPackingListPackageOrderItemByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _supplyRepositoriesFactory
            .NewPackingListPackageOrderItemRepository(connection)
            .SetIsReadyToPlacedByNetId(
                message.ItemNetId,
                message.ToSetValue
            );
    }

    private void ProcessSetReadyToPlaceToAllPackingListPackageOrderItemsByNetIdMessage(SetReadyToPlaceToAllPackingListPackageOrderItemsByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _supplyRepositoriesFactory
            .NewPackingListPackageOrderItemRepository(connection)
            .SetIsReadyToPlacedByPackingListNetId(
                message.NetId
            );

        Sender.Tell(
            _supplyRepositoriesFactory
                .NewPackingListRepository(connection)
                .GetByNetIdForPlacement(
                    message.NetId
                )
        );
    }

    private void ProcessDeletePackingListByNetIdMessage(DeletePackingListByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPackingListRepository packingListRepository = _supplyRepositoriesFactory.NewPackingListRepository(connection);

        PackingList packingList = packingListRepository.GetByNetId(message.NetId);

        if (packingList != null) {
            if (!packingList.IsPlaced) {
                ISupplyOrderItemRepository supplyOrderItemRepository = _supplyRepositoriesFactory.NewSupplyOrderItemRepository(connection);
                IActReconciliationItemRepository actReconciliationItemRepository = _supplyUkraineRepositoriesFactory.NewActReconciliationItemRepository(connection);
                ISupplyInvoiceOrderItemRepository supplyInvoiceOrderItemRepository =
                    _supplyRepositoriesFactory.NewSupplyInvoiceOrderItemRepository(connection);
                IPackingListPackageOrderItemRepository packingListPackageOrderItemRepository =
                    _supplyRepositoriesFactory.NewPackingListPackageOrderItemRepository(connection);

                foreach (PackingListPackageOrderItem item in packingList.PackingListPackageOrderItems) {
                    if (item.SupplyInvoiceOrderItem != null) {
                        if (item.SupplyInvoiceOrderItem.SupplyOrderItem != null)
                            if (item.SupplyInvoiceOrderItem.SupplyOrderItem.Qty != item.Qty) {
                                item.SupplyInvoiceOrderItem.SupplyOrderItem.Qty -= item.Qty;
                                supplyOrderItemRepository.UpdateQty(item.SupplyInvoiceOrderItem.SupplyOrderItem);
                            }

                        if (item.SupplyInvoiceOrderItem.Qty != item.Qty) {
                            item.SupplyInvoiceOrderItem.Qty -= item.Qty;
                            supplyInvoiceOrderItemRepository.Update(item.SupplyInvoiceOrderItem);
                        }


                        ActReconciliationItem reconciliationItem = actReconciliationItemRepository.GetBySupplyInvoiceOrderItemId(item.SupplyInvoiceOrderItem.Id);

                        if (reconciliationItem != null) {
                            reconciliationItem.QtyDifference = item.SupplyInvoiceOrderItem.Qty;
                            reconciliationItem.OrderedQty = item.SupplyInvoiceOrderItem.Qty;

                            actReconciliationItemRepository.FullUpdate(reconciliationItem);
                        }
                    }

                    packingListPackageOrderItemRepository.RemoveById(item.Id);
                }

                packingListRepository
                    .Remove(
                        message.NetId
                    );

                Sender.Tell(
                    (true, string.Empty)
                );
            } else {
                Sender.Tell(
                    (false,
                        CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower() == "pl"
                            ? "Wybranej pack list nie mozna usunac, poniewaz ma ona juz przychodow od towara"
                            : "������� ��� ���� ��������� ��������, ������� �� ��� �� ����� �� ������")
                );
            }
        } else {
            Sender.Tell(
                (false,
                    CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower() == "pl" ? "Pack list z wybranym NetId nie ma" : "��� ����� �� ������� NetId �� ����")
            );
        }
    }

    private void ProcessUpdateVatsForPackingListMessage(UpdateVatsForPackingListMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.PackingList != null && !message.PackingList.IsNew()) {
            IPackingListRepository packingListRepository = _supplyRepositoriesFactory.NewPackingListRepository(connection);

            packingListRepository.UpdateVats(message.PackingList);

            PackingList packingList =
                packingListRepository
                    .GetByNetIdWithInvoice(
                        message.PackingList.NetUid
                    );

            if (packingList != null)
                ActorReferenceManager.Instance.Get(SupplyActorNames.SUPPLY_INVOICE_ACTOR).Tell(new UpdateSupplyInvoiceItemGrossPriceMessage(
                    new List<long> { packingList.SupplyInvoiceId },
                    message.UserNetId
                ));

            Sender.Tell(
                packingListRepository
                    .GetByNetIdForPlacement(
                        message.PackingList.NetUid
                    )
            );
        } else {
            Sender.Tell(
                message.PackingList
            );
        }
    }

    private void ProcessUpdatePlacementPackingListMessage(UpdatePlacementPackingListMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyOrderItemRepository supplyOrderItemRepository = _supplyRepositoriesFactory.NewSupplyOrderItemRepository(connection);

            foreach (PackingListPackageOrderItem item in message.PackingList.PackingListPackageOrderItems)
                if (item.SupplyInvoiceOrderItem.SupplyOrderItem != null) {
                    SupplyOrderItem supplyOrderItemFromDb = supplyOrderItemRepository.GetByNetId(item.SupplyInvoiceOrderItem.SupplyOrderItem.NetUid);

                    ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                        new RetrieveAndStoreAuditDataMessage(
                            message.UserNetId,
                            supplyOrderItemFromDb.Product.NetUid,
                            "Product",
                            item.SupplyInvoiceOrderItem.SupplyOrderItem,
                            supplyOrderItemFromDb
                        )
                    );

                    item.SupplyInvoiceOrderItem.SupplyOrderItem.TotalAmount =
                        Convert.ToDecimal(item.SupplyInvoiceOrderItem.SupplyOrderItem.Qty) * item.SupplyInvoiceOrderItem.SupplyOrderItem.UnitPrice;

                    item.SupplyInvoiceOrderItem.SupplyOrderItem.NetWeight = item.NetWeight;
                    item.SupplyInvoiceOrderItem.SupplyOrderItem.GrossWeight = item.GrossWeight;

                    supplyOrderItemRepository.Update(item.SupplyInvoiceOrderItem.SupplyOrderItem);
                }

            Sender.Tell(_supplyRepositoriesFactory
                .NewSupplyInvoiceRepository(connection)
                .GetByNetIdWithAllIncludes(
                    message.InvoiceNetId
                ));
        } catch (Exception ex) {
            Sender.Tell(ex);
        }
    }
}