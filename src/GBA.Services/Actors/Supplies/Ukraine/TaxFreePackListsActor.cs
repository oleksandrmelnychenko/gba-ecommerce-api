using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.Helpers;
using GBA.Common.Helpers.PrintingDocuments;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Messages.Consignments.TaxFreePackLists;
using GBA.Domain.Messages.Supplies.Ukraine.TaxFreePackLists;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using Google.OrTools.LinearSolver;
using Constraint = Google.OrTools.LinearSolver.Constraint;

namespace GBA.Services.Actors.Supplies.Ukraine;

public sealed class TaxFreePackListsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IOrganizationRepositoriesFactory _organizationRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public TaxFreePackListsActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IOrganizationRepositoriesFactory organizationRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory,
        IXlsFactoryManager xlsFactoryManager) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _organizationRepositoriesFactory = organizationRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;
        _xlsFactoryManager = xlsFactoryManager;

        Receive<AddOrUpdateTaxFreePackListMessage>(ProcessAddOrUpdateTaxFreePackListMessage);

        Receive<GetTaxFreePackListByNetIdMessage>(ProcessGetTaxFreePackListByNetIdMessage);

        Receive<GetAllNotSentTaxFreePackListsMessage>(ProcessGetAllNotSentTaxFreePackListsMessage);

        Receive<GetAllNotSentTaxFreePackListsFromSaleMessage>(ProcessGetAllNotSentTaxFreePackListsFromSaleMessage);

        Receive<GetAllSentTaxFreePackListsMessage>(ProcessGetAllSentTaxFreePackListsMessage);

        Receive<GetAllTaxFreePackListsFilteredMessage>(ProcessGetAllTaxFreePackListsFilteredMessage);

        Receive<AddOrUpdateTaxFreePackListFromSalesMessage>(ProcessAddOrUpdateTaxFreePackListFromSalesMessage);

        Receive<DeleteTaxFreePackListByNetIdMessage>(ProcessDeleteTaxFreePackListByNetIdMessage);

        Receive<BreakPackListToTaxFreesMessage>(ProcessBreakPackListToTaxFreesMessage);

        Receive<FinishAddOrUpdateTaxFreePackListMessage>(ProcessFinishAddOrUpdateTaxFreePackListMessage);

        Receive<TaxFreePackingListPrintDocumentsMessage>(ProcessTaxFreePackingListPrintDocuments);
    }

    private void ProcessAddOrUpdateTaxFreePackListMessage(AddOrUpdateTaxFreePackListMessage message) {
        try {
            using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.TaxFreePackList == null)
                throw new Exception(TaxFreePackListResourceNames.EMPTY_PACK_LIST);

            ITaxFreeItemRepository taxFreeItemRepository = _supplyUkraineRepositoriesFactory.NewTaxFreeItemRepository(connection);
            ITaxFreeRepository taxFreeRepository = _supplyUkraineRepositoriesFactory.NewTaxFreeRepository(connection, exchangeRateConnection);
            ITaxFreePackListRepository taxFreePackListRepository =
                _supplyUkraineRepositoriesFactory.NewTaxFreePackListRepository(connection, exchangeRateConnection);
            ISupplyOrderUkraineCartItemRepository cartItemRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemRepository(connection);

            TaxFreePackList fromDb = null;

            if (!message.TaxFreePackList.IsNew()) {
                fromDb = taxFreePackListRepository.GetById(message.TaxFreePackList.Id);

                if (fromDb == null) throw new Exception(TaxFreePackListResourceNames.NON_EXISTING_PACK_LIST);

                if (fromDb.IsSent) {
                    foreach (TaxFree taxFree in message.TaxFreePackList.TaxFrees) {
                        TaxFree taxFreeFromDb = taxFreeRepository.GetById(taxFree.Id);

                        if (taxFreeFromDb != null)
                            if (!taxFreeFromDb.AmountPayedStatham.Equals(taxFree.AmountPayedStatham))
                                taxFree.DateOfStathamPayment = DateTime.Now.Date;

                        taxFree.DateOfPrint =
                            taxFree.DateOfPrint.HasValue
                                ? TimeZoneInfo.ConvertTimeToUtc(taxFree.DateOfPrint.Value)
                                : taxFree.DateOfPrint;

                        taxFree.DateOfIssue =
                            taxFree.DateOfIssue.HasValue
                                ? TimeZoneInfo.ConvertTimeToUtc(taxFree.DateOfIssue.Value)
                                : taxFree.DateOfIssue;

                        taxFree.DateOfStathamPayment =
                            taxFree.DateOfStathamPayment.HasValue
                                ? TimeZoneInfo.ConvertTimeToUtc(taxFree.DateOfStathamPayment.Value)
                                : taxFree.DateOfStathamPayment;

                        taxFree.DateOfTabulation =
                            taxFree.DateOfTabulation.HasValue
                                ? TimeZoneInfo.ConvertTimeToUtc(taxFree.DateOfTabulation.Value)
                                : taxFree.DateOfTabulation;

                        taxFree.FormedDate =
                            taxFree.FormedDate.HasValue
                                ? TimeZoneInfo.ConvertTimeToUtc(taxFree.FormedDate.Value)
                                : taxFree.FormedDate;

                        taxFree.SelectedDate =
                            taxFree.SelectedDate.HasValue
                                ? TimeZoneInfo.ConvertTimeToUtc(taxFree.SelectedDate.Value)
                                : taxFree.SelectedDate;

                        taxFree.ReturnedDate =
                            taxFree.ReturnedDate.HasValue
                                ? TimeZoneInfo.ConvertTimeToUtc(taxFree.ReturnedDate.Value)
                                : taxFree.ReturnedDate;

                        taxFree.ClosedDate =
                            taxFree.ClosedDate.HasValue
                                ? TimeZoneInfo.ConvertTimeToUtc(taxFree.ClosedDate.Value)
                                : taxFree.ClosedDate;

                        taxFree.CanceledDate =
                            taxFree.CanceledDate.HasValue
                                ? TimeZoneInfo.ConvertTimeToUtc(taxFree.CanceledDate.Value)
                                : taxFree.CanceledDate;

                        if (!taxFree.TaxFreeStatus.Equals(taxFreeFromDb.TaxFreeStatus))
                            switch (taxFree.TaxFreeStatus) {
                                case TaxFreeStatus.Formed:
                                    taxFree.FormedDate = taxFree.FormedDate ?? DateTime.UtcNow;
                                    break;
                                case TaxFreeStatus.Printed:
                                    taxFree.DateOfPrint = taxFree.DateOfPrint ?? DateTime.UtcNow;
                                    break;
                                case TaxFreeStatus.Tabulated:
                                    taxFree.DateOfTabulation = taxFree.DateOfTabulation ?? DateTime.UtcNow;
                                    break;
                                case TaxFreeStatus.Returned:
                                    taxFree.ReturnedDate = taxFree.ReturnedDate ?? DateTime.UtcNow;
                                    break;
                                case TaxFreeStatus.Closed:
                                    taxFree.ClosedDate = taxFree.ClosedDate ?? DateTime.UtcNow;
                                    break;
                                case TaxFreeStatus.NotFormed:
                                default:
                                    break;
                            }

                        taxFreeRepository.Update(taxFree);
                    }

                    Sender.Tell(
                        taxFreePackListRepository
                            .GetById(
                                message.TaxFreePackList.Id
                            )
                    );

                    return;
                }

                if (message.TaxFreePackList.IsSent && !message.TaxFreePackList.TaxFrees.Any())
                    throw new Exception(TaxFreePackListResourceNames.CAN_NOT_SENT_PACK_LIST_WITHOUT_TAX_FREES);

                if (message.TaxFreePackList.IsSent && (message.TaxFreePackList.Organization == null || message.TaxFreePackList.Organization.IsNew()))
                    throw new Exception(TaxFreePackListResourceNames.SPECIFY_ORGANIZATION);

                if (message.TaxFreePackList.IsSent && (message.TaxFreePackList.Client == null || message.TaxFreePackList.Client.IsNew()))
                    throw new Exception(TaxFreePackListResourceNames.SPECIFY_CLIENT);

                if (message.TaxFreePackList.IsSent && (message.TaxFreePackList.ClientAgreement == null || message.TaxFreePackList.ClientAgreement.IsNew()))
                    throw new Exception(TaxFreePackListResourceNames.SPECIFY_CLIENT_AGREEMENT);

                if (message.TaxFreePackList.TaxFrees.Any()) {
                    List<SupplyOrderUkraineCartItem> items = new();

                    foreach (TaxFree taxFree in message.TaxFreePackList.TaxFrees) {
                        if (!taxFree.TaxFreeItems.All(i => i.SupplyOrderUkraineCartItem != null && !i.SupplyOrderUkraineCartItem.IsNew()))
                            throw new Exception(TaxFreePackListResourceNames.TAX_FREE_ITEMS_HAVE_UNSPECIFIED_ITEMS);
                        if (!taxFree.TaxFreeItems.All(i => i.Qty > 0))
                            throw new Exception(TaxFreePackListResourceNames.SPECIFY_QTY_FOR_ALL_TAX_FREE_ITEMS);

                        foreach (TaxFreeItem item in taxFree.TaxFreeItems) {
                            SupplyOrderUkraineCartItem itemFromDb = cartItemRepository.GetById(item.SupplyOrderUkraineCartItem.Id);

                            if (itemFromDb == null)
                                continue;

                            if (!items.Any(i => i.Id.Equals(itemFromDb.Id))) {
                                if (!item.IsNew()) {
                                    TaxFreeItem taxFreeItemFromDb = taxFreeItemRepository.GetById(item.Id);

                                    if (taxFreeItemFromDb.Qty < item.Qty && item.Qty > itemFromDb.UnpackedQty + taxFreeItemFromDb.Qty)
                                        throw new LocalizedException(
                                            TaxFreePackListResourceNames.SPECIFIED_MORE_QTY_THAN_AVAILABLE,
                                            itemFromDb.Product.VendorCode
                                        );
                                } else {
                                    if (itemFromDb.TaxFreePackListId.HasValue) {
                                        if (item.Qty > itemFromDb.UnpackedQty)
                                            throw new LocalizedException(
                                                TaxFreePackListResourceNames.SPECIFIED_MORE_QTY_THAN_AVAILABLE,
                                                itemFromDb.Product.VendorCode
                                            );

                                        itemFromDb.UnpackedQty -= item.Qty;

                                        items.Add(itemFromDb);
                                    } else {
                                        if (message.TaxFreePackList.SupplyOrderUkraineCartItems.Any(i => i.Id.Equals(itemFromDb.Id))) {
                                            SupplyOrderUkraineCartItem itemFromList =
                                                message.TaxFreePackList.SupplyOrderUkraineCartItems.First(i => i.Id.Equals(itemFromDb.Id));

                                            itemFromDb.UnpackedQty =
                                                itemFromList.UploadedQty > itemFromDb.AvailableQty + itemFromDb.ReservedQty
                                                    ? itemFromDb.AvailableQty + itemFromDb.ReservedQty
                                                    : itemFromList.UploadedQty;

                                            if (item.Qty > itemFromDb.UnpackedQty)
                                                throw new LocalizedException(
                                                    TaxFreePackListResourceNames.SPECIFIED_MORE_QTY_THAN_AVAILABLE,
                                                    itemFromDb.Product.VendorCode
                                                );

                                            itemFromDb.UnpackedQty -= item.Qty;

                                            items.Add(itemFromDb);
                                        } else {
                                            throw new LocalizedException(
                                                TaxFreePackListResourceNames.PRODUCT_DOES_NOT_EXISTS_IN_CURRENT_PACK_LIST,
                                                itemFromDb.Product.VendorCode
                                            );
                                        }
                                    }
                                }
                            } else {
                                SupplyOrderUkraineCartItem itemFromList = items.First(i => i.Id.Equals(itemFromDb.Id));

                                if (item.Qty > itemFromList.UnpackedQty)
                                    throw new LocalizedException(
                                        TaxFreePackListResourceNames.SPECIFIED_MORE_QTY_THAN_AVAILABLE,
                                        itemFromList.Product.VendorCode
                                    );
                                itemFromList.UnpackedQty -= item.Qty;
                            }
                        }
                    }
                }
            }

            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            ISupplyOrderUkraineCartItemReservationRepository cartItemReservationRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemReservationRepository(connection);

            User user = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

            if (message.TaxFreePackList.Organization != null && !message.TaxFreePackList.Organization.IsNew())
                message.TaxFreePackList.OrganizationId = message.TaxFreePackList.Organization.Id;
            else
                message.TaxFreePackList.OrganizationId = null;
            if (message.TaxFreePackList.Client != null && !message.TaxFreePackList.Client.IsNew())
                message.TaxFreePackList.ClientId = message.TaxFreePackList.Client.Id;
            else
                message.TaxFreePackList.ClientId = null;

            message.TaxFreePackList.FromDate =
                message.TaxFreePackList.FromDate.Year.Equals(1)
                    ? DateTime.UtcNow
                    : TimeZoneInfo.ConvertTimeToUtc(message.TaxFreePackList.FromDate);

            if (message.TaxFreePackList.IsNew()) {
                TaxFreePackList lastRecord = taxFreePackListRepository.GetLastRecord();

                message.TaxFreePackList.Number =
                    lastRecord != null && lastRecord.FromDate.Year.Equals(DateTime.Now.Year)
                        ? string.Format(
                            "TF{0:D10}",
                            lastRecord.Number.StartsWith("TF")
                                ? Convert.ToInt64(lastRecord.Number.Substring(2, 10)) + 1
                                : Convert.ToInt64(lastRecord.Number) + 1
                        )
                        : string.Format("TF{0:D10}", 1);

                message.TaxFreePackList.ResponsibleId = user.Id;

                message.TaxFreePackList.IsSent = false;
                message.TaxFreePackList.IsFromSale = false;

                if (lastRecord != null) {
                    message.TaxFreePackList.WeightLimit = lastRecord.WeightLimit;
                    message.TaxFreePackList.MaxPriceLimit = lastRecord.MaxPriceLimit;
                    message.TaxFreePackList.MinPriceLimit = lastRecord.MinPriceLimit;
                    message.TaxFreePackList.MaxQtyInTaxFree = lastRecord.MaxQtyInTaxFree;
                    message.TaxFreePackList.MaxPositionsInTaxFree = lastRecord.MaxPositionsInTaxFree;

                    foreach (SupplyOrderUkraineCartItem supplyOrderUkraineCartItem in message.TaxFreePackList.SupplyOrderUkraineCartItems)
                        supplyOrderUkraineCartItem.MaxQtyPerTF = lastRecord.MaxQtyInTaxFree;
                }

                if (message.TaxFreePackList.OrganizationId == null) {
                    Organization plOrganization =
                        _organizationRepositoriesFactory
                            .NewOrganizationRepository(connection)
                            .GetByOrganizationCultureIfExists("pl");

                    message.TaxFreePackList.OrganizationId = plOrganization?.Id;
                }

                message.TaxFreePackList.Id = taxFreePackListRepository.Add(message.TaxFreePackList);
            } else {
                if (fromDb == null) fromDb = taxFreePackListRepository.GetById(message.TaxFreePackList.Id);

                fromDb.OrganizationId = message.TaxFreePackList.Organization?.Id;
                fromDb.ClientId = message.TaxFreePackList.Client?.Id;
                fromDb.ClientAgreementId = message.TaxFreePackList.ClientAgreement?.Id;
                fromDb.IsSent = message.TaxFreePackList.IsSent;
                fromDb.FromDate = message.TaxFreePackList.FromDate;

                if (!message.TaxFreePackList.TaxFrees.Any())
                    fromDb.MarginAmount = message.TaxFreePackList.MarginAmount;

                taxFreePackListRepository.Update(fromDb);

                List<TaxFree> toDelete =
                    taxFreeRepository
                        .GetAllByPackListIdExceptProvided(
                            message.TaxFreePackList.Id,
                            message.TaxFreePackList.TaxFrees.Where(t => !t.IsNew()).Select(t => t.Id)
                        );

                foreach (TaxFree taxFree in toDelete) {
                    if (taxFree.TaxFreeItems.Any()) {
                        foreach (TaxFreeItem item in taxFree.TaxFreeItems)
                            if (item.SupplyOrderUkraineCartItem.TaxFreePackListId.HasValue) {
                                if (item.SupplyOrderUkraineCartItemId.HasValue)
                                    cartItemRepository.UpdateUnpackedQty(item.SupplyOrderUkraineCartItemId.Value, item.Qty);
                            } else {
                                if (message
                                    .TaxFreePackList
                                    .SupplyOrderUkraineCartItems
                                    .Any(i => !i.IsNew()
                                              && i.ProductId.Equals(item.SupplyOrderUkraineCartItem.ProductId)
                                              && i.PackingListPackageOrderItemId.Equals(item.SupplyOrderUkraineCartItem.PackingListPackageOrderItemId))) {
                                    SupplyOrderUkraineCartItem cartItem = message
                                        .TaxFreePackList
                                        .SupplyOrderUkraineCartItems
                                        .First(i => !i.IsNew()
                                                    && i.ProductId.Equals(item.SupplyOrderUkraineCartItem.ProductId)
                                                    && i.PackingListPackageOrderItemId.Equals(item.SupplyOrderUkraineCartItem.PackingListPackageOrderItemId));

                                    cartItemRepository.UpdateUnpackedQty(cartItem.Id, item.Qty);
                                } else {
                                    SupplyOrderUkraineCartItem newItem = new() {
                                        CreatedById = item.SupplyOrderUkraineCartItem.CreatedById,
                                        UpdatedById = item.SupplyOrderUkraineCartItem.UpdatedById,
                                        ResponsibleId = item.SupplyOrderUkraineCartItem.ResponsibleId,
                                        ProductId = item.SupplyOrderUkraineCartItem.ProductId,
                                        FromDate = item.SupplyOrderUkraineCartItem.FromDate,
                                        ReservedQty = item.Qty,
                                        UploadedQty = item.Qty,
                                        UnpackedQty = item.Qty,
                                        NetWeight = item.SupplyOrderUkraineCartItem.NetWeight,
                                        UnitPrice = item.SupplyOrderUkraineCartItem.UnitPrice,
                                        SupplierId = item.SupplyOrderUkraineCartItem.SupplierId,
                                        TaxFreePackListId = message.TaxFreePackList.Id,
                                        PackingListPackageOrderItemId = item.SupplyOrderUkraineCartItem.PackingListPackageOrderItemId,
                                        Comment = item.SupplyOrderUkraineCartItem.Comment,
                                        ItemPriority = item.SupplyOrderUkraineCartItem.ItemPriority
                                    };

                                    newItem.Id = cartItemRepository.Add(newItem);

                                    message.TaxFreePackList.SupplyOrderUkraineCartItems.Add(newItem);
                                }
                            }

                        taxFreeItemRepository.RemoveAllByIds(taxFree.TaxFreeItems.Select(i => i.Id));
                    }

                    taxFreeRepository.Remove(taxFree);
                }
            }

            List<long> newlyAddedItemIds = new();
            List<SupplyOrderUkraineCartItem> updatedItems = new();

            foreach (SupplyOrderUkraineCartItem item in message.TaxFreePackList.SupplyOrderUkraineCartItems.Where(i => i.UploadedQty > 0)) {
                SupplyOrderUkraineCartItem itemFromDb = cartItemRepository.GetByIdWithReservations(item.Id);

                if (itemFromDb == null) continue;

                if (itemFromDb.MaxQtyPerTF != item.MaxQtyPerTF) {
                    itemFromDb.MaxQtyPerTF = item.MaxQtyPerTF;

                    cartItemRepository.UpdateMaxQtyPerTf(itemFromDb);
                }

                if (itemFromDb.TaxFreePackListId.HasValue) {
                    if (itemFromDb.UnpackedQty.Equals(item.UnpackedQty)) continue;

                    updatedItems.Add(item);
                } else {
                    itemFromDb.ResponsibleId = user.Id;
                    itemFromDb.UpdatedById = user.Id;
                    itemFromDb.TaxFreePackListId = message.TaxFreePackList.Id;

                    cartItemRepository.Update(itemFromDb);

                    newlyAddedItemIds.Add(itemFromDb.Id);

                    if (itemFromDb.ReservedQty > item.UploadedQty) {
                        double operationQty = itemFromDb.ReservedQty - item.UploadedQty;

                        SupplyOrderUkraineCartItem newItem = new() {
                            Comment = itemFromDb.Comment,
                            UploadedQty = operationQty,
                            ItemPriority = itemFromDb.ItemPriority,
                            ProductId = itemFromDb.ProductId,
                            CreatedById = itemFromDb.CreatedById,
                            UpdatedById = itemFromDb.UpdatedById,
                            ResponsibleId = itemFromDb.ResponsibleId,
                            ReservedQty = operationQty,
                            FromDate = itemFromDb.FromDate,
                            TaxFreePackListId = null,
                            UnpackedQty = operationQty,
                            NetWeight = itemFromDb.NetWeight,
                            UnitPrice = itemFromDb.UnitPrice,
                            SupplierId = itemFromDb.SupplierId,
                            PackingListPackageOrderItemId = null,
                            MaxQtyPerTF = itemFromDb.MaxQtyPerTF,
                            IsRecommended = itemFromDb.IsRecommended
                        };

                        newItem.Id = cartItemRepository.Add(newItem);

                        foreach (SupplyOrderUkraineCartItemReservation reservation in itemFromDb.SupplyOrderUkraineCartItemReservations) {
                            if (reservation.Qty <= operationQty) {
                                reservation.SupplyOrderUkraineCartItemId = newItem.Id;

                                cartItemReservationRepository.Update(reservation);

                                operationQty -= reservation.Qty;
                            } else {
                                reservation.Qty -= operationQty;

                                cartItemReservationRepository.Update(reservation);

                                cartItemReservationRepository.Add(new SupplyOrderUkraineCartItemReservation {
                                    Qty = operationQty,
                                    ProductAvailabilityId = reservation.ProductAvailabilityId,
                                    SupplyOrderUkraineCartItemId = newItem.Id
                                });
                            }

                            if (operationQty.Equals(0d)) break;
                        }
                    } else {
                        if (itemFromDb.ReservedQty.Equals(item.UploadedQty)) {
                            itemFromDb.UploadedQty = itemFromDb.ReservedQty;

                            cartItemRepository.Update(itemFromDb);

                            continue;
                        }

                        IEnumerable<ProductAvailability> availabilities =
                            productAvailabilityRepository
                                .GetByProductAndCultureIds(
                                    itemFromDb.ProductId,
                                    "pl"
                                );

                        double operationQty = item.UploadedQty - itemFromDb.ReservedQty;

                        foreach (ProductAvailability availability in availabilities) {
                            SupplyOrderUkraineCartItemReservation reservation = new() {
                                ProductAvailabilityId = availability.Id,
                                SupplyOrderUkraineCartItemId = itemFromDb.Id
                            };

                            if (availability.Amount < operationQty) {
                                operationQty -= availability.Amount;

                                reservation.Qty = availability.Amount;

                                availability.Amount = 0;

                                productAvailabilityRepository.Update(availability);
                            } else {
                                reservation.Qty = operationQty;

                                availability.Amount -= operationQty;

                                productAvailabilityRepository.Update(availability);

                                operationQty = 0d;
                            }

                            SupplyOrderUkraineCartItemReservation existingReservation =
                                cartItemReservationRepository
                                    .GetByIdsIfExists(
                                        reservation.SupplyOrderUkraineCartItemId,
                                        reservation.ProductAvailabilityId
                                    );

                            if (existingReservation != null) {
                                existingReservation.Qty += reservation.Qty;

                                cartItemReservationRepository.Update(existingReservation);
                            } else {
                                cartItemReservationRepository.Add(reservation);
                            }

                            if (operationQty.Equals(0d)) break;
                        }

                        itemFromDb.ReservedQty += item.UploadedQty - itemFromDb.ReservedQty - operationQty;
                        itemFromDb.UploadedQty = itemFromDb.ReservedQty;

                        cartItemRepository.Update(itemFromDb);
                    }
                }
            }

            IEnumerable<SupplyOrderUkraineCartItem> deletedItems =
                cartItemRepository
                    .GetAllByPackListIdExceptProvided(
                        message.TaxFreePackList.Id,
                        message.TaxFreePackList.SupplyOrderUkraineCartItems.Select(i => i.Id)
                    );

            taxFreeItemRepository
                .RemoveAllByPackListAndCartItemIds(
                    message.TaxFreePackList.Id,
                    deletedItems.Select(i => i.Id)
                );

            ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new ChangeReservationsOnConsignmentFromTaxFreePackListMessage(
                message.TaxFreePackList.Id,
                newlyAddedItemIds,
                updatedItems,
                deletedItems,
                message.TaxFreePackList.TaxFrees,
                message.UserNetId,
                Sender
            ));
        } catch (LocalizedException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetTaxFreePackListByNetIdMessage(GetTaxFreePackListByNetIdMessage message) {
        using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        TaxFreePackList taxFreePackList =
            _supplyUkraineRepositoriesFactory
                .NewTaxFreePackListRepository(connection, exchangeRateConnection)
                .GetByNetId(
                    message.NetId
                );

        if (taxFreePackList.IsFromSale)
            CalculatePricingForSalesWithDynamicPrices(
                taxFreePackList.Sales,
                _exchangeRateRepositoriesFactory
                    .NewExchangeRateRepository(connection)
            );

        Sender.Tell(
            taxFreePackList
        );
    }

    private void ProcessGetAllNotSentTaxFreePackListsMessage(GetAllNotSentTaxFreePackListsMessage message) {
        using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supplyUkraineRepositoriesFactory
                .NewTaxFreePackListRepository(connection, exchangeRateConnection)
                .GetAllNotSent()
        );
    }

    private void ProcessGetAllNotSentTaxFreePackListsFromSaleMessage(GetAllNotSentTaxFreePackListsFromSaleMessage message) {
        using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IEnumerable<TaxFreePackList> packLists =
            _supplyUkraineRepositoriesFactory
                .NewTaxFreePackListRepository(connection, exchangeRateConnection)
                .GetAllNotSentFromSales();

        foreach (TaxFreePackList packList in packLists)
            if (packList.IsFromSale)
                CalculatePricingForSalesWithDynamicPrices(
                    packList.Sales,
                    _exchangeRateRepositoriesFactory
                        .NewExchangeRateRepository(connection)
                );

        Sender.Tell(
            packLists
        );
    }

    private void ProcessGetAllSentTaxFreePackListsMessage(GetAllSentTaxFreePackListsMessage message) {
        using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supplyUkraineRepositoriesFactory
                .NewTaxFreePackListRepository(connection, exchangeRateConnection)
                .GetAllSent()
        );
    }

    private void ProcessGetAllTaxFreePackListsFilteredMessage(GetAllTaxFreePackListsFilteredMessage message) {
        using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IEnumerable<TaxFreePackList> packLists =
            _supplyUkraineRepositoriesFactory
                .NewTaxFreePackListRepository(connection, exchangeRateConnection)
                .GetAllFiltered(
                    message.From,
                    message.To,
                    message.Limit,
                    message.Offset
                );

        foreach (TaxFreePackList packList in packLists)
            if (packList.IsFromSale)
                CalculatePricingForSalesWithDynamicPrices(
                    packList.Sales,
                    _exchangeRateRepositoriesFactory
                        .NewExchangeRateRepository(connection)
                );

        Sender.Tell(
            packLists
        );
    }

    private void ProcessAddOrUpdateTaxFreePackListFromSalesMessage(AddOrUpdateTaxFreePackListFromSalesMessage message) {
        try {
            using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.TaxFreePackList == null)
                throw new Exception(TaxFreePackListResourceNames.EMPTY_PACK_LIST);

            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            ITaxFreeItemRepository taxFreeItemRepository = _supplyUkraineRepositoriesFactory.NewTaxFreeItemRepository(connection);
            ITaxFreeRepository taxFreeRepository = _supplyUkraineRepositoriesFactory.NewTaxFreeRepository(connection, exchangeRateConnection);
            ITaxFreePackListRepository taxFreePackListRepository =
                _supplyUkraineRepositoriesFactory.NewTaxFreePackListRepository(connection, exchangeRateConnection);
            ITaxFreePackListOrderItemRepository taxFreePackListOrderItemRepository =
                _supplyUkraineRepositoriesFactory.NewTaxFreePackListOrderItemRepository(connection);

            TaxFreePackList fromDb = null;

            if (!message.TaxFreePackList.IsNew()) {
                fromDb = taxFreePackListRepository.GetById(message.TaxFreePackList.Id);

                if (fromDb != null) {
                    if (fromDb.IsSent) {
                        foreach (TaxFree taxFree in message.TaxFreePackList.TaxFrees) {
                            TaxFree taxFreeFromDb = taxFreeRepository.GetById(taxFree.Id);

                            if (taxFreeFromDb != null)
                                if (!taxFreeFromDb.AmountPayedStatham.Equals(taxFree.AmountPayedStatham))
                                    taxFree.DateOfStathamPayment = DateTime.Now.Date;

                            taxFree.DateOfPrint =
                                taxFree.DateOfPrint.HasValue
                                    ? TimeZoneInfo.ConvertTimeToUtc(taxFree.DateOfPrint.Value)
                                    : taxFree.DateOfPrint;

                            taxFree.DateOfIssue =
                                taxFree.DateOfIssue.HasValue
                                    ? TimeZoneInfo.ConvertTimeToUtc(taxFree.DateOfIssue.Value)
                                    : taxFree.DateOfIssue;

                            taxFree.DateOfStathamPayment =
                                taxFree.DateOfStathamPayment.HasValue
                                    ? TimeZoneInfo.ConvertTimeToUtc(taxFree.DateOfStathamPayment.Value)
                                    : taxFree.DateOfStathamPayment;

                            taxFree.DateOfTabulation =
                                taxFree.DateOfTabulation.HasValue
                                    ? TimeZoneInfo.ConvertTimeToUtc(taxFree.DateOfTabulation.Value)
                                    : taxFree.DateOfTabulation;

                            taxFree.FormedDate =
                                taxFree.FormedDate.HasValue
                                    ? TimeZoneInfo.ConvertTimeToUtc(taxFree.FormedDate.Value)
                                    : taxFree.FormedDate;

                            taxFree.SelectedDate =
                                taxFree.SelectedDate.HasValue
                                    ? TimeZoneInfo.ConvertTimeToUtc(taxFree.SelectedDate.Value)
                                    : taxFree.SelectedDate;

                            taxFree.ReturnedDate =
                                taxFree.ReturnedDate.HasValue
                                    ? TimeZoneInfo.ConvertTimeToUtc(taxFree.ReturnedDate.Value)
                                    : taxFree.ReturnedDate;

                            taxFree.ClosedDate =
                                taxFree.ClosedDate.HasValue
                                    ? TimeZoneInfo.ConvertTimeToUtc(taxFree.ClosedDate.Value)
                                    : taxFree.ClosedDate;

                            taxFree.CanceledDate =
                                taxFree.CanceledDate.HasValue
                                    ? TimeZoneInfo.ConvertTimeToUtc(taxFree.CanceledDate.Value)
                                    : taxFree.CanceledDate;

                            if (taxFreeFromDb != null && !taxFree.TaxFreeStatus.Equals(taxFreeFromDb.TaxFreeStatus))
                                switch (taxFree.TaxFreeStatus) {
                                    case TaxFreeStatus.Formed:
                                        taxFree.FormedDate = taxFree.FormedDate ?? DateTime.UtcNow;
                                        break;
                                    case TaxFreeStatus.Printed:
                                        taxFree.DateOfPrint = taxFree.DateOfPrint ?? DateTime.UtcNow;
                                        break;
                                    case TaxFreeStatus.Tabulated:
                                        taxFree.DateOfTabulation = taxFree.DateOfTabulation ?? DateTime.UtcNow;
                                        break;
                                    case TaxFreeStatus.Returned:
                                        taxFree.ReturnedDate = taxFree.ReturnedDate ?? DateTime.UtcNow;
                                        break;
                                    case TaxFreeStatus.Closed:
                                        taxFree.ClosedDate = taxFree.ClosedDate ?? DateTime.UtcNow;
                                        break;
                                    case TaxFreeStatus.NotFormed:
                                    default:
                                        break;
                                }

                            taxFreeRepository.Update(taxFree);
                        }

                        Sender.Tell(
                            taxFreePackListRepository
                                .GetById(
                                    message.TaxFreePackList.Id
                                )
                        );

                        return;
                    }

                    if (message.TaxFreePackList.IsSent && !message.TaxFreePackList.TaxFrees.Any())
                        throw new Exception(TaxFreePackListResourceNames.CAN_NOT_SENT_PACK_LIST_WITHOUT_TAX_FREES);

                    if (message.TaxFreePackList.IsSent && (message.TaxFreePackList.Organization == null || message.TaxFreePackList.Organization.IsNew()))
                        throw new Exception(TaxFreePackListResourceNames.SPECIFY_ORGANIZATION);

                    if (message.TaxFreePackList.IsSent && (message.TaxFreePackList.Client == null || message.TaxFreePackList.Client.IsNew()))
                        throw new Exception(TaxFreePackListResourceNames.SPECIFY_CLIENT);

                    if (message.TaxFreePackList.IsSent && (message.TaxFreePackList.ClientAgreement == null || message.TaxFreePackList.ClientAgreement.IsNew()))
                        throw new Exception(TaxFreePackListResourceNames.SPECIFY_CLIENT_AGREEMENT);

                    if (message.TaxFreePackList.TaxFrees.Any())
                        foreach (TaxFree taxFree in message.TaxFreePackList.TaxFrees) {
                            if (!taxFree.TaxFreeItems.All(i => i.TaxFreePackListOrderItem != null && !i.TaxFreePackListOrderItem.IsNew()))
                                throw new Exception(TaxFreePackListResourceNames.TAX_FREE_ITEMS_HAVE_UNSPECIFIED_ITEMS);
                            if (!taxFree.TaxFreeItems.All(i => i.Qty > 0))
                                throw new Exception(TaxFreePackListResourceNames.SPECIFY_QTY_FOR_ALL_TAX_FREE_ITEMS);

                            List<TaxFreePackListOrderItem> items = new();

                            foreach (TaxFreeItem item in taxFree.TaxFreeItems) {
                                TaxFreePackListOrderItem itemFromDb = taxFreePackListOrderItemRepository.GetById(item.TaxFreePackListOrderItem.Id);

                                if (itemFromDb == null)
                                    continue;

                                if (!items.Any(i => i.Id.Equals(itemFromDb.Id))) {
                                    if (!item.IsNew()) {
                                        TaxFreeItem taxFreeItemFromDb = taxFreeItemRepository.GetById(item.Id);

                                        if (taxFreeItemFromDb.Qty < item.Qty && item.Qty > itemFromDb.UnpackedQty + taxFreeItemFromDb.Qty)
                                            throw new LocalizedException(
                                                TaxFreePackListResourceNames.SPECIFIED_MORE_QTY_THAN_AVAILABLE,
                                                itemFromDb.OrderItem.Product.VendorCode
                                            );
                                    } else {
                                        if (item.Qty > itemFromDb.UnpackedQty)
                                            throw new LocalizedException(
                                                TaxFreePackListResourceNames.SPECIFIED_MORE_QTY_THAN_AVAILABLE,
                                                itemFromDb.OrderItem.Product.VendorCode
                                            );

                                        itemFromDb.UnpackedQty -= item.Qty;

                                        items.Add(itemFromDb);
                                    }
                                } else {
                                    TaxFreePackListOrderItem itemFromList = items.First(i => i.Id.Equals(itemFromDb.Id));

                                    if (item.Qty > itemFromList.UnpackedQty)
                                        throw new LocalizedException(
                                            TaxFreePackListResourceNames.SPECIFIED_MORE_QTY_THAN_AVAILABLE,
                                            itemFromDb.OrderItem.Product.VendorCode
                                        );

                                    itemFromList.UnpackedQty -= item.Qty;
                                }
                            }
                        }
                } else {
                    throw new Exception(TaxFreePackListResourceNames.NON_EXISTING_PACK_LIST);
                }

                if (!message.TaxFreePackList.Sales.Any())
                    throw new Exception(TaxFreePackListResourceNames.ADD_AT_LEAST_ONE_SALE);

                long clientId = message.TaxFreePackList.Sales.First().ClientAgreement.ClientId;

                if (!message.TaxFreePackList.Sales.All(s => s.ClientAgreement.ClientId.Equals(clientId)))
                    throw new Exception(TaxFreePackListResourceNames.ALL_SALES_MUST_BE_FROM_SINGLE_CLIENT);

                message.TaxFreePackList.ClientId = clientId;

                if (message.TaxFreePackList.IsSent)
                    if (fromDb.TaxFreePackListOrderItems.Any(i => i.UnpackedQty > 0))
                        throw new Exception(TaxFreePackListResourceNames.ALL_ITEMS_NEED_TO_BE_PLACED_INTO_TAX_FREES);
            }

            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            User user = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

            if (message.TaxFreePackList.Organization != null && !message.TaxFreePackList.Organization.IsNew())
                message.TaxFreePackList.OrganizationId = message.TaxFreePackList.Organization.Id;
            else
                message.TaxFreePackList.OrganizationId = null;

            message.TaxFreePackList.FromDate =
                message.TaxFreePackList.FromDate.Year.Equals(1)
                    ? DateTime.UtcNow
                    : TimeZoneInfo.ConvertTimeToUtc(message.TaxFreePackList.FromDate);

            bool isPackListWasSent = false;

            if (message.TaxFreePackList.IsNew()) {
                TaxFreePackList lastRecord = taxFreePackListRepository.GetLastRecord();

                message.TaxFreePackList.Number =
                    lastRecord != null && lastRecord.FromDate.Year.Equals(DateTime.Now.Year)
                        ? string.Format(
                            "TF{0:D10}",
                            lastRecord.Number.StartsWith("TF")
                                ? Convert.ToInt64(lastRecord.Number.Substring(2, 10)) + 1
                                : Convert.ToInt64(lastRecord.Number) + 1
                        )
                        : string.Format("TF{0:D10}", 1);

                message.TaxFreePackList.ResponsibleId = user.Id;

                message.TaxFreePackList.IsSent = false;
                message.TaxFreePackList.IsFromSale = true;

                if (lastRecord != null) {
                    message.TaxFreePackList.WeightLimit = lastRecord.WeightLimit;
                    message.TaxFreePackList.MaxPriceLimit = lastRecord.MaxPriceLimit;
                    message.TaxFreePackList.MinPriceLimit = lastRecord.MinPriceLimit;
                    message.TaxFreePackList.MaxQtyInTaxFree = lastRecord.MaxQtyInTaxFree;
                    message.TaxFreePackList.MaxPositionsInTaxFree = lastRecord.MaxPositionsInTaxFree;
                }

                if (message.TaxFreePackList.Sales.Any()) {
                    Sale sale = message.TaxFreePackList.Sales.First();

                    message.TaxFreePackList.ClientAgreementId = sale.ClientAgreement?.Id;
                    message.TaxFreePackList.ClientId = sale.ClientAgreement?.ClientId;
                }

                message.TaxFreePackList.Id = taxFreePackListRepository.Add(message.TaxFreePackList);
            } else {
                if (fromDb != null) {
                    isPackListWasSent = !fromDb.IsSent && message.TaxFreePackList.IsSent;

                    fromDb.OrganizationId = message.TaxFreePackList.Organization?.Id;
                    fromDb.ClientId = message.TaxFreePackList.Client?.Id;
                    fromDb.ClientAgreementId = message.TaxFreePackList.ClientAgreement?.Id;
                    fromDb.IsSent = message.TaxFreePackList.IsSent;

                    if (message.TaxFreePackList.Sales.Any()) {
                        Sale sale = message.TaxFreePackList.Sales.First();

                        message.TaxFreePackList.ClientAgreementId = sale.ClientAgreement?.Id;
                        message.TaxFreePackList.ClientId = sale.ClientAgreement?.ClientId;
                    }

                    if (!message.TaxFreePackList.TaxFrees.Any())
                        fromDb.MarginAmount = message.TaxFreePackList.MarginAmount;

                    taxFreePackListRepository.Update(fromDb);

                    List<TaxFree> toDelete =
                        taxFreeRepository
                            .GetAllByPackListIdExceptProvided(
                                message.TaxFreePackList.Id,
                                message.TaxFreePackList.TaxFrees.Where(t => !t.IsNew()).Select(t => t.Id)
                            );

                    foreach (TaxFree taxFree in toDelete) {
                        taxFreePackListOrderItemRepository.RestoreUnpackedQtyByTaxFreeItemsIds(taxFree.TaxFreeItems.Select(i => i.Id));

                        taxFreeItemRepository.RemoveAllByIds(taxFree.TaxFreeItems.Select(i => i.Id));

                        taxFreeRepository.Remove(taxFree);
                    }
                }
            }

            IEnumerable<Sale> salesToRemove =
                saleRepository
                    .GetAllSalesByTaxFreePackListIdExceptProvided(
                        message.TaxFreePackList.Id,
                        message.TaxFreePackList.Sales.Select(s => s.Id)
                    );

            foreach (Sale sale in salesToRemove) {
                sale.TaxFreePackListId = null;

                saleRepository.UpdateTaxFreePackListReference(sale);

                taxFreeItemRepository.RemoveAllByOrderItemIds(sale.Order.OrderItems.Select(i => i.Id));
            }

            foreach (Sale sale in message.TaxFreePackList.Sales.Where(s => !s.TaxFreePackListId.HasValue)) {
                List<OrderItem> items = orderItemRepository.GetAllWithConsignmentMovementBySaleId(sale.Id);

                foreach (OrderItem orderItem in items)
                    if (orderItem.ConsignmentItemMovements.Any()) {
                        double operationQty = orderItem.Qty;

                        foreach (ConsignmentItemMovement movement in orderItem.ConsignmentItemMovements) {
                            taxFreePackListOrderItemRepository
                                .Add(new TaxFreePackListOrderItem {
                                    NetWeight = movement.ConsignmentItem.Weight,
                                    Qty = movement.Qty,
                                    UnpackedQty = movement.Qty,
                                    TaxFreePackListId = message.TaxFreePackList.Id,
                                    OrderItemId = orderItem.Id,
                                    ConsignmentItemId = movement.ConsignmentItemId
                                });

                            operationQty -= movement.RemainingQty;
                        }

                        if (operationQty > 0)
                            taxFreePackListOrderItemRepository
                                .Add(new TaxFreePackListOrderItem {
                                    NetWeight = 0d,
                                    Qty = operationQty,
                                    UnpackedQty = operationQty,
                                    TaxFreePackListId = message.TaxFreePackList.Id,
                                    OrderItemId = orderItem.Id
                                });
                    } else {
                        taxFreePackListOrderItemRepository
                            .Add(new TaxFreePackListOrderItem {
                                NetWeight = 0d,
                                Qty = orderItem.Qty,
                                UnpackedQty = orderItem.Qty,
                                TaxFreePackListId = message.TaxFreePackList.Id,
                                OrderItemId = orderItem.Id
                            });
                    }

                sale.TaxFreePackListId = message.TaxFreePackList.Id;

                saleRepository.UpdateTaxFreePackListReference(sale);
            }

            foreach (TaxFree taxFree in message.TaxFreePackList.TaxFrees) {
                taxFree.ResponsibleId =
                    taxFree.Responsible != null && !taxFree.Responsible.IsNew()
                        ? taxFree.Responsible.Id
                        : user.Id;

                taxFree.DateOfPrint =
                    taxFree.DateOfPrint.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(taxFree.DateOfPrint.Value)
                        : taxFree.DateOfPrint;

                taxFree.DateOfIssue =
                    taxFree.DateOfIssue.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(taxFree.DateOfIssue.Value)
                        : taxFree.DateOfIssue;

                taxFree.DateOfStathamPayment =
                    taxFree.DateOfStathamPayment.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(taxFree.DateOfStathamPayment.Value)
                        : taxFree.DateOfStathamPayment;

                taxFree.DateOfTabulation =
                    taxFree.DateOfTabulation.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(taxFree.DateOfTabulation.Value)
                        : taxFree.DateOfTabulation;

                taxFree.FormedDate =
                    taxFree.FormedDate.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(taxFree.FormedDate.Value)
                        : taxFree.FormedDate;

                taxFree.SelectedDate =
                    taxFree.SelectedDate.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(taxFree.SelectedDate.Value)
                        : taxFree.SelectedDate;

                taxFree.ReturnedDate =
                    taxFree.ReturnedDate.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(taxFree.ReturnedDate.Value)
                        : taxFree.ReturnedDate;

                taxFree.ClosedDate =
                    taxFree.ClosedDate.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(taxFree.ClosedDate.Value)
                        : taxFree.ClosedDate;

                taxFree.CanceledDate =
                    taxFree.CanceledDate.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(taxFree.CanceledDate.Value)
                        : taxFree.CanceledDate;


                if (taxFree.Statham != null && !taxFree.Statham.IsNew())
                    taxFree.StathamId = taxFree.Statham.Id;
                else
                    taxFree.StathamId = null;
                if (taxFree.StathamCar != null && !taxFree.StathamCar.IsNew())
                    taxFree.StathamCarId = taxFree.StathamCar.Id;
                else
                    taxFree.StathamCarId = null;
                if (taxFree.StathamPassport != null && !taxFree.StathamPassport.IsNew())
                    taxFree.StathamPassportId = taxFree.StathamPassport.Id;
                else
                    taxFree.StathamPassportId = null;

                taxFree.TaxFreePackListId = message.TaxFreePackList.Id;

                if (taxFree.IsNew()) {
                    TaxFree lastRecord = taxFreeRepository.GetLastRecord();

                    taxFree.Number =
                        lastRecord != null && lastRecord.Created.Year.Equals(DateTime.UtcNow.Year)
                            ? string.Format(
                                "TF{0:D10}",
                                lastRecord.Number.StartsWith("TF")
                                    ? Convert.ToInt64(lastRecord.Number.Substring(2, 10)) + 1
                                    : Convert.ToInt64(lastRecord.Number) + 1
                            )
                            : string.Format("TF{0:D10}", 1);

                    if (taxFree.VatPercent <= 0)
                        taxFree.VatPercent = 23;

                    taxFree.Id = taxFreeRepository.Add(taxFree);
                } else {
                    if (taxFree.VatPercent <= 0)
                        taxFree.VatPercent = 23;

                    taxFreeRepository.Update(taxFree);
                }

                taxFreePackListOrderItemRepository
                    .RestoreUnpackedQtyByTaxFreeIdExceptProvidedIds(
                        taxFree.Id,
                        taxFree.TaxFreeItems.Where(i => !i.IsNew()).Select(i => i.Id)
                    );

                List<TaxFreeItem> itemsToRemove =
                    taxFreeItemRepository
                        .GetAllByTaxFreeIdExceptProvided(
                            taxFree.Id,
                            taxFree.TaxFreeItems.Where(i => !i.IsNew()).Select(i => i.Id)
                        );

                if (itemsToRemove.Any())
                    taxFreeItemRepository
                        .RemoveAllByTaxFreeIdExceptProvided(
                            taxFree.Id,
                            taxFree.TaxFreeItems.Where(i => !i.IsNew()).Select(i => i.Id)
                        );

                foreach (TaxFreeItem item in taxFree.TaxFreeItems.Where(i => !i.IsNew())) {
                    TaxFreeItem taxFreeItemFromDb = taxFreeItemRepository.GetById(item.Id);

                    double qtyDifference = item.Qty - taxFreeItemFromDb.Qty;

                    taxFreeItemRepository.Update(item);

                    if (!qtyDifference.Equals(0d))
                        taxFreePackListOrderItemRepository.DecreaseUnpackedQtyById(item.TaxFreePackListOrderItem.Id, qtyDifference);
                }

                foreach (TaxFreeItem item in taxFree.TaxFreeItems.Where(i => i.IsNew())) {
                    taxFreePackListOrderItemRepository.DecreaseUnpackedQtyById(item.TaxFreePackListOrderItem.Id, item.Qty);

                    TaxFreeItem itemFromDb =
                        taxFreeItemRepository
                            .GetByTaxFreeAndPackListOrderItemIdsIfExists(
                                taxFree.Id,
                                item.TaxFreePackListOrderItem.Id
                            );

                    if (itemFromDb != null) {
                        itemFromDb.Qty += item.Qty;

                        taxFreeItemRepository.Update(itemFromDb);
                    } else {
                        item.TaxFreeId = taxFree.Id;
                        item.TaxFreePackListOrderItemId = item.TaxFreePackListOrderItem.Id;

                        item.Id = taxFreeItemRepository.Add(item);
                    }
                }
            }

            if (isPackListWasSent)
                ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR)
                    .Tell(new StoreConsignmentMovementFromTaxFreePackListFromSaleMessage(message.TaxFreePackList.Id));

            Sender.Tell(
                taxFreePackListRepository
                    .GetById(
                        message.TaxFreePackList.Id
                    )
            );
        } catch (LocalizedException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessDeleteTaxFreePackListByNetIdMessage(DeleteTaxFreePackListByNetIdMessage message) {
        using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ITaxFreePackListRepository taxFreePackListRepository = _supplyUkraineRepositoriesFactory.NewTaxFreePackListRepository(connection, exchangeRateConnection);

        TaxFreePackList fromDb = taxFreePackListRepository.GetByNetId(message.NetId);

        if (fromDb != null) {
            if (!fromDb.IsSent) {
                if (!fromDb.TaxFrees.Any()) {
                    if (fromDb.IsFromSale) {
                        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

                        foreach (Sale sale in fromDb.Sales) {
                            sale.TaxFreePackListId = null;

                            saleRepository.UpdateTaxFreePackListReference(sale);
                        }
                    } else {
                        ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR)
                            .Tell(new RestoreReservationsOnConsignmentFromTaxFreePackListDeleteMessage(fromDb.Id, message.UserNetId));
                    }

                    taxFreePackListRepository.Remove(message.NetId);

                    Sender.Tell(
                        (true, string.Empty)
                    );
                } else {
                    Sender.Tell(
                        (false, TaxFreePackListResourceNames.PACK_LIST_WITH_TAX_FREES_CAN_NOT_BE_DELETED)
                    );
                }
            } else {
                Sender.Tell(
                    (false, TaxFreePackListResourceNames.PACK_LIST_IS_SENT)
                );
            }
        } else {
            Sender.Tell(
                (false, TaxFreePackListResourceNames.NON_EXISTING_PACK_LIST)
            );
        }
    }

    private void ProcessBreakPackListToTaxFreesMessage(BreakPackListToTaxFreesMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
            TaxFreePackList messageTaxFreePackList = message.TaxFreePackList;

            // Validation
            if (messageTaxFreePackList == null || messageTaxFreePackList.IsNew())
                throw new Exception(TaxFreePackListResourceNames.NON_EXISTING_PACK_LIST);

            ICollection<SupplyOrderUkraineCartItem> supplyOrderUkraineCartItems = messageTaxFreePackList.SupplyOrderUkraineCartItems;

            ICollection<TaxFreePackListOrderItem> taxFreePackListOrderItems = messageTaxFreePackList.TaxFreePackListOrderItems;

            decimal minPriceLimitPln = messageTaxFreePackList.MinPriceLimit;

            decimal maxPriceLimitEuro = messageTaxFreePackList.MaxPriceLimit;

            decimal marginAmount = messageTaxFreePackList.MarginAmount;

            double weightLimit = messageTaxFreePackList.WeightLimit;

            int maxQtyInTaxFree = messageTaxFreePackList.MaxQtyInTaxFree;

            int maxPositionsInTaxFree = messageTaxFreePackList.MaxPositionsInTaxFree;

            bool isFromSale = messageTaxFreePackList.IsFromSale;

            if (!supplyOrderUkraineCartItems.Any() && !taxFreePackListOrderItems.Any())
                throw new Exception(TaxFreePackListResourceNames.NO_ITEMS_IN_PACK_LIST);

            if (minPriceLimitPln <= 0m)
                throw new Exception(TaxFreePackListResourceNames.SET_MIN_PRICE_LIMIT);

            if (maxPriceLimitEuro <= 0m)
                throw new Exception(TaxFreePackListResourceNames.SET_MAX_PRICE_LIMIT);

            if (maxQtyInTaxFree <= 0)
                throw new Exception(TaxFreePackListResourceNames.SET_MAX_QTY_IN_TAX_FREE);

            if (maxPositionsInTaxFree <= 0)
                throw new Exception(TaxFreePackListResourceNames.SET_MAX_POSITIONS_IN_TAX_FREE);

            if (weightLimit <= 0d)
                throw new Exception(TaxFreePackListResourceNames.SET_WEIGHT_LIMIT);

            if (!isFromSale && marginAmount <= 0m)
                throw new Exception(TaxFreePackListResourceNames.SET_MARGIN_AMOUNT);

            // Get TaxFreePackList's FromDate property
            ITaxFreePackListRepository taxFreePackListRepository =
                _supplyUkraineRepositoriesFactory.NewTaxFreePackListRepository(connection, exchangeRateConnection);

            TaxFreePackList taxFreePackList = taxFreePackListRepository
                .GetById(
                    messageTaxFreePackList.Id
                );

            DateTime taxFreePackListFromDate = taxFreePackList.FromDate;

            taxFreePackListFromDate =
                taxFreePackListFromDate.Year.Equals(1)
                    ? DateTime.UtcNow
                    : TimeZoneInfo.ConvertTimeToUtc(taxFreePackListFromDate);

            // Convert max price limit from EURO to PLN
            ExchangeRate euroToPlnExchangeRate =
                _exchangeRateRepositoriesFactory
                    .NewExchangeRateRepository(exchangeRateConnection)
                    .GetByCurrencyIdAndCode(4, "EUR", taxFreePackListFromDate.AddDays(-1));

            decimal maxPriceLimitPln = maxPriceLimitEuro * euroToPlnExchangeRate?.Amount ?? maxPriceLimitEuro;

            if (minPriceLimitPln > maxPriceLimitPln)
                throw new Exception(TaxFreePackListResourceNames.MIN_PRICE_LIMIT_GREATER_THAN_MAX_PRICE_LIMIT);

            // Update TaxFreePackList
            taxFreePackList.WeightLimit = weightLimit;
            taxFreePackList.MaxPriceLimit = maxPriceLimitEuro;
            taxFreePackList.MinPriceLimit = minPriceLimitPln;
            taxFreePackList.MaxQtyInTaxFree = maxQtyInTaxFree;
            taxFreePackList.MaxPositionsInTaxFree = maxPositionsInTaxFree;
            taxFreePackList.FromDate = taxFreePackListFromDate;

            taxFreePackListRepository.Update(taxFreePackList);

            // Get and validate unpacked order items
            IEnumerable<dynamic> unpackedItems;

            if (isFromSale)
                unpackedItems = taxFreePackListOrderItems.Where(i => i.UnpackedQty > 0);
            else
                unpackedItems = supplyOrderUkraineCartItems.Where(i => i.UnpackedQty > 0);

            if (!unpackedItems.Any())
                throw new Exception(TaxFreePackListResourceNames.NO_ITEMS_TO_BREAK);

            foreach (dynamic item in unpackedItems) {
                if (item.MaxQtyPerTF <= 0)
                    item.MaxQtyPerTF = maxQtyInTaxFree;

                if (item.NetWeight <= 0)
                    item.NetWeight = 0.12d;

                if (item.UnitPriceLocal <= 0)
                    item.UnitPriceLocal = 1m;
            }

            List<TaxFree> taxFrees = GetTaxFreesFromPackList(
                unpackedItems.ToList(),
                isFromSale,
                messageTaxFreePackList,
                maxPriceLimitPln
            );

            if (!taxFrees.Any())
                throw new Exception(TaxFreePackListResourceNames.NO_TAX_FREES_WAS_GENERATED);

            // Update order items
            if (isFromSale) {
                ITaxFreePackListOrderItemRepository taxFreePackListOrderItemRepository =
                    _supplyUkraineRepositoriesFactory.NewTaxFreePackListOrderItemRepository(connection);

                taxFreePackListOrderItemRepository.Update(taxFreePackListOrderItems);
            } else {
                ISupplyOrderUkraineCartItemRepository supplyOrderUkraineCartItemRepository =
                    _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemRepository(connection);

                supplyOrderUkraineCartItemRepository.Update(supplyOrderUkraineCartItems);
            }

            ITaxFreeRepository taxFreeRepository =
                _supplyUkraineRepositoriesFactory.NewTaxFreeRepository(connection, exchangeRateConnection);

            TaxFree lastRecord = taxFreeRepository.GetLastRecord();

            long number =
                lastRecord != null && lastRecord.Created.Year.Equals(DateTime.UtcNow.Year)
                    ? lastRecord.Number.StartsWith("TF")
                        ? Convert.ToInt64(lastRecord.Number.Substring(2, 10)) + 1
                        : Convert.ToInt64(lastRecord.Number) + 1
                    : 1;

            // Fill tax free data
            ITaxFreeItemRepository taxFreeItemRepository =
                _supplyUkraineRepositoriesFactory.NewTaxFreeItemRepository(connection);

            long messageTaxFreePackListId = messageTaxFreePackList.Id;

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);
            long userId = user.Id;

            foreach (TaxFree taxFree in taxFrees) {
                taxFree.ResponsibleId = userId;

                taxFree.StathamId = null;
                taxFree.StathamCarId = null;
                taxFree.StathamPassportId = null;

                taxFree.TaxFreePackListId = messageTaxFreePackListId;

                taxFree.MarginAmount = marginAmount;

                taxFree.Number = string.Format("TF{0:D10}", number);

                taxFree.Id = taxFreeRepository.Add(taxFree);

                taxFreeItemRepository
                    .Add(
                        taxFree
                            .TaxFreeItems
                            .Select(item => {
                                item.TaxFreeId = taxFree.Id;

                                return item;
                            })
                    );

                number++;
            }

            Sender.Tell(
                _supplyUkraineRepositoriesFactory
                    .NewTaxFreePackListRepository(connection, exchangeRateConnection)
                    .GetById(
                        messageTaxFreePackList.Id
                    )
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessFinishAddOrUpdateTaxFreePackListMessage(FinishAddOrUpdateTaxFreePackListMessage message) {
        try {
            using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ITaxFreeItemRepository taxFreeItemRepository = _supplyUkraineRepositoriesFactory.NewTaxFreeItemRepository(connection);
            ITaxFreeRepository taxFreeRepository = _supplyUkraineRepositoriesFactory.NewTaxFreeRepository(connection, exchangeRateConnection);
            ITaxFreePackListRepository taxFreePackListRepository =
                _supplyUkraineRepositoriesFactory.NewTaxFreePackListRepository(connection, exchangeRateConnection);
            ISupplyOrderUkraineCartItemRepository cartItemRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemRepository(connection);

            TaxFreePackList taxFreePackList =
                taxFreePackListRepository
                    .GetById(
                        message.TaxFreePackListId
                    );

            if (taxFreePackList == null) {
                Sender.Tell(null);

                return;
            }

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            foreach (TaxFree taxFree in message.TaxFrees) {
                switch (taxFree.TaxFreeStatus) {
                    case TaxFreeStatus.Formed:
                        taxFree.DateOfIssue = taxFree.DateOfIssue ?? DateTime.UtcNow;
                        break;
                    case TaxFreeStatus.Printed:
                        taxFree.DateOfIssue = taxFree.DateOfIssue ?? DateTime.UtcNow;
                        taxFree.DateOfPrint = taxFree.DateOfPrint ?? DateTime.UtcNow;
                        break;
                    case TaxFreeStatus.NotFormed:
                    case TaxFreeStatus.Tabulated:
                    case TaxFreeStatus.Returned:
                    case TaxFreeStatus.Closed:
                    default:
                        break;
                }

                taxFree.ResponsibleId =
                    taxFree.Responsible != null && !taxFree.Responsible.IsNew()
                        ? taxFree.Responsible.Id
                        : user.Id;

                taxFree.DateOfPrint =
                    taxFree.DateOfPrint.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(taxFree.DateOfPrint.Value)
                        : taxFree.DateOfPrint;

                taxFree.DateOfIssue =
                    taxFree.DateOfIssue.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(taxFree.DateOfIssue.Value)
                        : taxFree.DateOfIssue;

                taxFree.DateOfStathamPayment =
                    taxFree.DateOfStathamPayment.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(taxFree.DateOfStathamPayment.Value)
                        : taxFree.DateOfStathamPayment;

                taxFree.DateOfTabulation =
                    taxFree.DateOfTabulation.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(taxFree.DateOfTabulation.Value)
                        : taxFree.DateOfTabulation;

                taxFree.FormedDate =
                    taxFree.FormedDate.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(taxFree.FormedDate.Value)
                        : taxFree.FormedDate;

                taxFree.SelectedDate =
                    taxFree.SelectedDate.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(taxFree.SelectedDate.Value)
                        : taxFree.SelectedDate;

                taxFree.ReturnedDate =
                    taxFree.ReturnedDate.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(taxFree.ReturnedDate.Value)
                        : taxFree.ReturnedDate;

                taxFree.ClosedDate =
                    taxFree.ClosedDate.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(taxFree.ClosedDate.Value)
                        : taxFree.ClosedDate;

                taxFree.CanceledDate =
                    taxFree.CanceledDate.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(taxFree.CanceledDate.Value)
                        : taxFree.CanceledDate;

                if (taxFree.Statham != null && !taxFree.Statham.IsNew())
                    taxFree.StathamId = taxFree.Statham.Id;
                else
                    taxFree.StathamId = null;
                if (taxFree.StathamCar != null && !taxFree.StathamCar.IsNew())
                    taxFree.StathamCarId = taxFree.StathamCar.Id;
                else
                    taxFree.StathamCarId = null;
                if (taxFree.StathamPassport != null && !taxFree.StathamPassport.IsNew())
                    taxFree.StathamPassportId = taxFree.StathamPassport.Id;
                else
                    taxFree.StathamPassportId = null;

                taxFree.TaxFreePackListId = taxFreePackList.Id;

                if (taxFree.IsNew()) {
                    TaxFree lastRecord = taxFreeRepository.GetLastRecord();

                    taxFree.Number =
                        lastRecord != null && lastRecord.Created.Year.Equals(DateTime.UtcNow.Year)
                            ? string.Format(
                                "TF{0:D10}",
                                lastRecord.Number.StartsWith("TF")
                                    ? Convert.ToInt64(lastRecord.Number.Substring(2, 10)) + 1
                                    : Convert.ToInt64(lastRecord.Number) + 1
                            )
                            : string.Format("TF{0:D10}", 1);

                    if (taxFree.VatPercent <= 0)
                        taxFree.VatPercent = 23;

                    taxFree.Id = taxFreeRepository.Add(taxFree);
                } else {
                    if (taxFree.VatPercent <= 0)
                        taxFree.VatPercent = 23;

                    taxFreeRepository.Update(taxFree);
                }

                cartItemRepository
                    .RestoreUnpackedQtyByTaxFreeIdExceptProvidedIds(
                        taxFree.Id,
                        taxFree.TaxFreeItems.Where(i => !i.IsNew()).Select(i => i.Id)
                    );

                List<TaxFreeItem> itemsToRemove =
                    taxFreeItemRepository
                        .GetAllByTaxFreeIdExceptProvided(
                            taxFree.Id,
                            taxFree.TaxFreeItems.Where(i => !i.IsNew()).Select(i => i.Id)
                        );

                if (itemsToRemove.Any())
                    taxFreeItemRepository
                        .RemoveAllByTaxFreeIdExceptProvided(
                            taxFree.Id,
                            taxFree.TaxFreeItems.Where(i => !i.IsNew()).Select(i => i.Id)
                        );

                foreach (TaxFreeItem item in taxFree.TaxFreeItems.Where(i => !i.IsNew())) {
                    TaxFreeItem taxFreeItemFromDb = taxFreeItemRepository.GetById(item.Id);

                    double qtyDifference = item.Qty - taxFreeItemFromDb.Qty;

                    taxFreeItemRepository.Update(item);

                    if (!qtyDifference.Equals(0d))
                        cartItemRepository.DecreaseUnpackedQtyById(item.SupplyOrderUkraineCartItem.Id, qtyDifference);
                }

                foreach (TaxFreeItem item in taxFree.TaxFreeItems.Where(i => i.IsNew())) {
                    cartItemRepository.DecreaseUnpackedQtyById(item.SupplyOrderUkraineCartItem.Id, item.Qty);

                    TaxFreeItem itemFromDb =
                        taxFreeItemRepository
                            .GetByTaxFreeAndCartItemIdsIfExists(
                                taxFree.Id,
                                item.SupplyOrderUkraineCartItem.Id
                            );

                    if (itemFromDb != null) {
                        itemFromDb.Qty += item.Qty;

                        taxFreeItemRepository.Update(itemFromDb);
                    } else {
                        item.TaxFreeId = taxFree.Id;
                        item.SupplyOrderUkraineCartItemId = item.SupplyOrderUkraineCartItem.Id;

                        item.Id = taxFreeItemRepository.Add(item);
                    }
                }
            }

            if (taxFreePackList.IsSent) {
                ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new StoreConsignmentMovementFromTaxFreePackListMessage(taxFreePackList.Id));

                if (taxFreePackList.SupplyOrderUkraineCartItems.Any(i => i.UnpackedQty > 0))
                    ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR)
                        .Tell(new RestoreReservationsOnConsignmentFromUnpackedTaxFreePackListCartItemsMessage(taxFreePackList.Id, message.UserNetId));
            }

            Sender.Tell(
                taxFreePackListRepository
                    .GetById(
                        taxFreePackList.Id
                    )
            );
        } catch (LocalizedException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessTaxFreePackingListPrintDocuments(TaxFreePackingListPrintDocumentsMessage message) {
        try {
            using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IEnumerable<TaxFreePackList> forPrint =
                _supplyUkraineRepositoriesFactory
                    .NewTaxFreePackListRepository(connection, exchangeRateConnection)
                    .GetAllFilteredForPrintDocument(
                        message.From,
                        message.To
                    );

            if (forPrint == null) {
                Sender.Tell((string.Empty, string.Empty));
                return;
            }

            foreach (TaxFreePackList packList in forPrint)
                if (packList.IsFromSale)
                    CalculatePricingForSalesWithDynamicPrices(
                        packList.Sales,
                        _exchangeRateRepositoriesFactory
                            .NewExchangeRateRepository(connection)
                    );

            PrintDocumentsHelper printDocumentsHelper = new(forPrint, message.ColumnDataForPrint);

            List<Dictionary<string, string>> rows = printDocumentsHelper.GetRowsForPrintDocument();

            (string pathXls, string pathPdf) =
                _xlsFactoryManager
                    .NewPrintDocumentsManager()
                    .GetPrintDocument(
                        message.PathToFolder,
                        message.ColumnDataForPrint,
                        rows);

            Sender.Tell((pathXls, pathPdf));
        } catch {
            Sender.Tell((string.Empty, string.Empty));
        }
    }

    private static void CalculatePricingForSalesWithDynamicPrices(
        IEnumerable<Sale> sales,
        IExchangeRateRepository exchangeRateRepository) {
        foreach (Sale sale in sales) {
            if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) {
                ExchangeRate euroExchangeRate = exchangeRateRepository.GetEuroExchangeRateByCurrentCulture();

                foreach (OrderItem orderItem in sale.Order.OrderItems) {
                    orderItem.Product.CurrentPrice =
                        decimal.Round(orderItem.Product.CurrentPrice - orderItem.Product.CurrentPrice * orderItem.OneTimeDiscount / 100, 4, MidpointRounding.AwayFromZero);
                    orderItem.Product.CurrentLocalPrice =
                        decimal.Round(orderItem.Product.CurrentPrice * euroExchangeRate.Amount, 4, MidpointRounding.AwayFromZero);

                    orderItem.TotalAmount = decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal = orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty);

                    orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
                    orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                    orderItem.TotalAmount = decimal.Round(orderItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);
                }
            } else {
                foreach (OrderItem orderItem in sale.Order.OrderItems) {
                    orderItem.Product.CurrentLocalPrice =
                        decimal.Round(orderItem.PricePerItem * orderItem.ExchangeRateAmount, 4, MidpointRounding.AwayFromZero);

                    orderItem.TotalAmount =
                        decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal =
                        decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty) * orderItem.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero);

                    orderItem.Product.CurrentPrice = decimal.Round(orderItem.PricePerItem, 2, MidpointRounding.AwayFromZero);
                    orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                    //orderItem.TotalAmount = Decimal.Round(orderItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                    //orderItem.TotalAmountLocal = Decimal.Round(orderItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);
                }
            }

            sale.Order.TotalAmount = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmount), 2, MidpointRounding.AwayFromZero);
            sale.Order.TotalAmountLocal = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmountLocal), 2, MidpointRounding.AwayFromZero);
            sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);

            sale.TotalAmount = sale.Order.TotalAmount;
            sale.TotalAmountLocal = sale.Order.TotalAmountLocal;
            sale.TotalCount = sale.Order.TotalCount;
        }
    }

    private static List<TaxFree> GetTaxFreesFromPackList(
        List<dynamic> unpackedItems,
        bool isFromSale,
        TaxFreePackList messageTaxFreePackList,
        decimal maxPriceLimitPln) {
        int orderItemsQuantity = unpackedItems.Count;

        List<TaxFree> taxFrees = new();

        List<TaxFree> invalidTaxFrees = new();

        bool validTaxFreeAdded = true;

        long? taxFreeItemId;

        while (validTaxFreeAdded) {
            validTaxFreeAdded = false;

            while (true) {
                Solver solver = Solver.CreateSolver("CBC");

                // Initialize constraints
                Constraint ctCost =
                    solver.MakeConstraint(
                        (double)messageTaxFreePackList.MinPriceLimit,
                        (double)maxPriceLimitPln,
                        "ctCost"
                    );

                Constraint ctWeight =
                    solver.MakeConstraint(
                        0,
                        messageTaxFreePackList.WeightLimit,
                        "ctWeight"
                    );

                Variable[] variables = new Variable[orderItemsQuantity];

                // Initialize objective function
                Objective objective = solver.Objective();
                objective.SetMaximization();

                for (int i = 0; i < orderItemsQuantity; i++) {
                    double variableUpperBound = Math.Min(unpackedItems[i].UnpackedQty, unpackedItems[i].MaxQtyPerTF);

                    // Variable
                    variables[i] = solver.MakeIntVar(0.0, variableUpperBound, unpackedItems[i].Id.ToString());

                    // Summands of constraints                            
                    ctWeight.SetCoefficient(variables[i], unpackedItems[i].NetWeight);

                    double unitPriceLocal = (double)unpackedItems[i].UnitPriceLocal;
                    ctCost.SetCoefficient(variables[i], unitPriceLocal);

                    // Summand of objective function
                    objective.SetCoefficient(variables[i], unitPriceLocal);
                }

                Solver.ResultStatus resultStatus = solver.Solve();

                if (resultStatus != Solver.ResultStatus.FEASIBLE && resultStatus != Solver.ResultStatus.OPTIMAL)
                    break;

                TaxFree taxFree = new() {
                    AmountInPLN = (decimal)solver.Objective().Value(),
                    TotalNetWeight = 0
                };

                for (int i = 0; i < orderItemsQuantity; i++) {
                    double itemPackedQuantity = variables[i].SolutionValue();

                    if (itemPackedQuantity.Equals(0))
                        continue;

                    unpackedItems[i].UnpackedQty -= itemPackedQuantity;

                    taxFree.TotalNetWeight += itemPackedQuantity * unpackedItems[i].NetWeight;

                    // Add tax free item
                    TaxFreeItem newTaxFreeItem = new() {
                        Qty = itemPackedQuantity
                    };

                    if (isFromSale) {
                        newTaxFreeItem.TaxFreePackListOrderItemId = unpackedItems[i].Id;
                        newTaxFreeItem.TaxFreePackListOrderItemId = unpackedItems[i];
                    } else {
                        newTaxFreeItem.SupplyOrderUkraineCartItemId = unpackedItems[i].Id;
                        newTaxFreeItem.SupplyOrderUkraineCartItem = unpackedItems[i];
                    }

                    taxFree.TaxFreeItems.Add(newTaxFreeItem);
                }

                if (taxFree.TaxFreeItems.Count <= messageTaxFreePackList.MaxPositionsInTaxFree) {
                    taxFrees.Add(taxFree);
                    validTaxFreeAdded = true;
                } else {
                    invalidTaxFrees.Add(taxFree);
                }
            }

            foreach (TaxFreeItem taxFreeItem in invalidTaxFrees.SelectMany(taxFree => taxFree.TaxFreeItems)) {
                taxFreeItemId = isFromSale ? taxFreeItem.TaxFreePackListOrderItemId : taxFreeItem.SupplyOrderUkraineCartItemId;

                unpackedItems.First(ui => ui.Id.Equals(taxFreeItemId)).UnpackedQty += taxFreeItem.Qty;
            }

            invalidTaxFrees.Clear();
        }

        return taxFrees;
    }
}