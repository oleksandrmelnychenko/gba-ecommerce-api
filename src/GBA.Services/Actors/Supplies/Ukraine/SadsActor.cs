using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.Supplies;
using GBA.Domain.Messages.Consignments.Sads;
using GBA.Domain.Messages.Products.ProductSpecifications;
using GBA.Domain.Messages.Supplies.Ukraine.Sads;
using GBA.Domain.Repositories.Clients.OrganizationClients.Contracts;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Supplies.Ukraine;

public sealed class SadsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IOrganizationClientRepositoriesFactory _organizationClientRepositoriesFactory;
    private readonly IOrganizationRepositoriesFactory _organizationRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public SadsActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IOrganizationRepositoriesFactory organizationRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory,
        IOrganizationClientRepositoriesFactory organizationClientRepositoriesFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _organizationRepositoriesFactory = organizationRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;
        _organizationClientRepositoriesFactory = organizationClientRepositoriesFactory;

        Receive<AddOrUpdateSadMessage>(ProcessAddOrUpdateSadMessage);

        Receive<GetAllNotSentSadsMessage>(ProcessGetAllNotSentSadsMessage);

        Receive<GetAllSentSadsMessage>(ProcessGetAllSentSadsMessage);

        Receive<GetAllNotSentSadsFromSaleMessage>(ProcessGetAllNotSentSadsFromSaleMessage);

        Receive<GetAllSadsFilteredMessage>(ProcessGetAllSadsFilteredMessage);

        Receive<GetSadByNetIdMessage>(ProcessGetSadByNetIdMessage);

        Receive<GetSadWithProductSpecificationByNetIdMessage>(ProcessGetSadWithProductSpecificationByNetIdMessage);

        Receive<ExportSadDocumentsMessage>(ProcessExportSadDocumentsMessage);

        Receive<UploadSadDocumentsBySadNetIdMessage>(ProcessUploadSadDocumentsBySadNetIdMessage);

        Receive<RemoveSadDocumentByNetIdMessage>(ProcessRemoveSadDocumentByNetIdMessage);

        Receive<DeleteSadByNetIdMessage>(ProcessDeleteSadByNetIdMessage);

        Receive<AddOrUpdateSadFromSaleMessage>(ProcessAddOrUpdateSadFromSaleMessage);

        Receive<UploadProductSpecificationForSadMessage>(ProcessUploadProductSpecificationForSad);

        Receive<FinishAddOrUpdateSadMessage>(ProcessFinishAddOrUpdateSadMessage);
    }

    private void ProcessAddOrUpdateSadMessage(AddOrUpdateSadMessage message) {
        try {
            using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.Sad == null) throw new Exception("Sad entity can not be null or empty");

            ISadItemRepository sadItemRepository = _supplyUkraineRepositoriesFactory.NewSadItemRepository(connection);
            ISadPalletRepository sadPalletRepository = _supplyUkraineRepositoriesFactory.NewSadPalletRepository(connection);
            ISadRepository sadRepository = _supplyUkraineRepositoriesFactory.NewSadRepository(connection, exchangeRateConnection);
            ISadPalletItemRepository sadPalletItemRepository = _supplyUkraineRepositoriesFactory.NewSadPalletItemRepository(connection);
            ISupplyOrderUkraineCartItemRepository cartItemRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemRepository(connection);
            ISupplyOrderUkraineCartItemReservationRepository cartItemReservationRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemReservationRepository(connection);

            if (!message.Sad.IsNew()) {
                Sad sadFromDb = sadRepository.GetById(message.Sad.Id);

                if (sadFromDb == null) throw new Exception("Specified Sad does not exists in database");
                if (sadFromDb.IsSend) throw new Exception("Specified Sad is already sent");

                if (message.Sad.IsSend) {
                    if (!message.Sad.OrganizationId.HasValue && (message.Sad.Organization == null || message.Sad.Organization.IsNew()))
                        throw new Exception("You need to specify Organization");

                    if (message.Sad.MarginAmount <= 0m) throw new Exception("You need to specify MarginAmount");
                }
            }

            if (!message.Sad.SadItems.All(i => i.SupplyOrderUkraineCartItem != null && !i.SupplyOrderUkraineCartItem.IsNew()))
                throw new Exception("All SadItems should have specified SupplyOrderUkraineCartItem");

            if (message.Sad.SadPallets.Any()) {
                if (!message.Sad.SadPallets.All(p => p.SadPalletType != null && !p.SadPalletType.IsNew()))
                    throw new Exception("All SadPallets should have specified SadPalletType");

                List<SadItem> items = new();

                foreach (SadPallet pallet in message.Sad.SadPallets)
                foreach (SadPalletItem item in pallet.SadPalletItems) {
                    SadItem itemFromDb = sadItemRepository.GetById(item.SadItem.Id);
                    // SupplyOrderUkraineCartItem itemFromDb = cartItemRepository.GetById(item.SadItem.SupplyOrderUkraineCartItem.Id);

                    if (itemFromDb == null) continue;

                    if (!items.Any(i => i.Id.Equals(itemFromDb.Id))) {
                        if (!item.IsNew()) {
                            SadPalletItem palletItemFromDb = sadPalletItemRepository.GetById(item.Id);

                            if (palletItemFromDb.Qty < item.Qty && item.Qty > itemFromDb.UnpackedQty + palletItemFromDb.Qty)
                                throw new Exception(
                                    string.Format(
                                        "You specified more Qty for {0} SadPalletItem than available for packing",
                                        itemFromDb.SupplyOrderUkraineCartItem.Product.VendorCode
                                    )
                                );
                        } else {
                            if (message.Sad.SadItems.Any(i => i.Id.Equals(itemFromDb.Id))) {
                                if (item.Qty > itemFromDb.UnpackedQty)
                                    throw new Exception(
                                        string.Format(
                                            "You specified more Qty for {0} SadPalletItem than available for packing",
                                            itemFromDb.SupplyOrderUkraineCartItem.Product.VendorCode
                                        )
                                    );

                                itemFromDb.UnpackedQty -= item.Qty;

                                items.Add(itemFromDb);
                            } else {
                                throw new Exception(
                                    string.Format(
                                        "Product {0} can't be used as SadPalletItem because it does not exists in current TaxFreePackList",
                                        itemFromDb.SupplyOrderUkraineCartItem.Product.VendorCode
                                    )
                                );
                            }
                        }
                    } else {
                        SadItem itemFromList = items.First(i => i.Id.Equals(itemFromDb.Id));

                        if (item.Qty > itemFromList.UnpackedQty)
                            throw new Exception(
                                string.Format(
                                    "You specified more Qty for {0} SadPalletItem than available for packing",
                                    itemFromDb.SupplyOrderUkraineCartItem.Product.VendorCode
                                )
                            );
                        itemFromList.UnpackedQty -= item.Qty;
                    }
                }
            }

            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            User user = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

            if (message.Sad.Organization != null && !message.Sad.Organization.IsNew())
                message.Sad.OrganizationId = message.Sad.Organization.Id;
            else
                message.Sad.OrganizationId = null;

            if (message.Sad.Organization != null && !message.Sad.Organization.IsNew())
                message.Sad.OrganizationId = message.Sad.Organization.Id;
            else
                message.Sad.OrganizationId = null;

            if (message.Sad.Statham != null && !message.Sad.Statham.IsNew())
                message.Sad.StathamId = message.Sad.Statham.Id;
            else
                message.Sad.StathamId = null;

            if (message.Sad.StathamCar != null && !message.Sad.StathamCar.IsNew())
                message.Sad.StathamCarId = message.Sad.StathamCar.Id;
            else
                message.Sad.StathamCarId = null;

            if (message.Sad.StathamPassport != null && !message.Sad.StathamPassport.IsNew())
                message.Sad.StathamPassportId = message.Sad.StathamPassport.Id;
            else
                message.Sad.StathamPassportId = null;

            if (message.Sad.OrganizationClient != null && !message.Sad.OrganizationClient.IsNew())
                message.Sad.OrganizationClientId = message.Sad.OrganizationClient.Id;
            else
                message.Sad.OrganizationClientId = null;

            if (message.Sad.OrganizationClientAgreement != null && !message.Sad.OrganizationClientAgreement.IsNew())
                message.Sad.OrganizationClientAgreementId = message.Sad.OrganizationClientAgreement.Id;
            else
                message.Sad.OrganizationClientAgreementId = null;

            if (message.Sad.Client != null && !message.Sad.Client.IsNew())
                message.Sad.ClientId = message.Sad.Client.Id;
            else
                message.Sad.ClientId = null;

            if (message.Sad.ClientAgreement != null && !message.Sad.ClientAgreement.IsNew())
                message.Sad.ClientAgreementId = message.Sad.ClientAgreement.Id;
            else
                message.Sad.ClientAgreementId = null;

            message.Sad.FromDate =
                message.Sad.FromDate.Year.Equals(1)
                    ? DateTime.UtcNow
                    : TimeZoneInfo.ConvertTimeToUtc(message.Sad.FromDate);

            if (message.Sad.IsNew()) {
                Sad lastRecord = sadRepository.GetLastRecord();

                message.Sad.Number =
                    lastRecord != null && lastRecord.FromDate.Year.Equals(DateTime.Now.Year)
                        ? string.Format(
                            "EX{0:D10}",
                            lastRecord.Number.StartsWith("EX")
                                ? Convert.ToInt64(lastRecord.Number.Substring(2, 10)) + 1
                                : Convert.ToInt64(lastRecord.Number) + 1
                        )
                        : string.Format("EX{0:D10}", 1);

                message.Sad.ResponsibleId = user.Id;

                message.Sad.IsSend = false;
                message.Sad.IsFromSale = false;
                message.Sad.VatPercent = 23m;

                if (!message.Sad.OrganizationId.HasValue) {
                    Organization plOrganization =
                        _organizationRepositoriesFactory
                            .NewOrganizationRepository(connection)
                            .GetByOrganizationCultureIfExists("pl");

                    message.Sad.OrganizationId = plOrganization?.Id;
                }

                message.Sad.Id = sadRepository.Add(message.Sad);

                foreach (SadItem sadItem in message.Sad.SadItems) {
                    SupplyOrderUkraineCartItem itemFromDb = cartItemRepository.GetByIdWithReservations(sadItem.SupplyOrderUkraineCartItem.Id);

                    if (itemFromDb == null) continue;

                    cartItemRepository.Remove(itemFromDb.Id);

                    sadItem.SadId = message.Sad.Id;
                    sadItem.UnpackedQty = sadItem.Qty;
                    sadItem.SupplyOrderUkraineCartItemId = itemFromDb.Id;

                    sadItem.Id = sadItemRepository.Add(sadItem);

                    if (itemFromDb.ReservedQty > sadItem.Qty) {
                        double operationQty = itemFromDb.ReservedQty - sadItem.Qty;

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
                            UnpackedQty = itemFromDb.UnpackedQty,
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
                        if (itemFromDb.ReservedQty.Equals(sadItem.Qty)) continue;

                        IEnumerable<ProductAvailability> availabilities =
                            productAvailabilityRepository
                                .GetByProductAndCultureIds(
                                    itemFromDb.ProductId,
                                    "pl"
                                );

                        double operationQty = sadItem.Qty - itemFromDb.ReservedQty;

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

                        itemFromDb.ReservedQty += sadItem.Qty - itemFromDb.ReservedQty - operationQty;

                        cartItemRepository.Update(itemFromDb);

                        sadItem.Qty -= operationQty;

                        sadItemRepository.Update(sadItem);
                    }
                }

                ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new AddReservationOnConsignmentFromNewSadMessage(message.Sad.Id, Sender));

                return;
            }

            Sad beforeUpdate = sadRepository.GetByIdWithoutIncludes(message.Sad.Id);

            sadRepository.Update(message.Sad);

            //Need to be checked
            List<SadPallet> deletedPallets =
                sadPalletRepository
                    .GetAllBySadIdExceptProvided(
                        message.Sad.Id,
                        message.Sad.SadPallets.Where(p => !p.IsNew()).Select(p => p.Id)
                    );

            foreach (SadPallet pallet in deletedPallets) {
                foreach (SadPalletItem item in pallet.SadPalletItems) sadItemRepository.RestoreUnpackedQtyById(item.SadItemId, item.Qty);

                sadPalletRepository.Remove(pallet.Id);
            }

            foreach (SadPallet pallet in message.Sad.SadPallets) {
                pallet.SadId = message.Sad.Id;
                pallet.SadPalletTypeId = pallet.SadPalletType.Id;

                if (pallet.IsNew())
                    pallet.Id = sadPalletRepository.Add(pallet);
                else
                    sadPalletRepository.Update(pallet);

                sadPalletItemRepository
                    .RestoreUnpackedQtyByPalletIdExceptProvidedIds(
                        pallet.Id,
                        pallet.SadPalletItems.Where(i => !i.IsNew()).Select(i => i.Id)
                    );

                sadPalletItemRepository
                    .RemoveAllByPalletIdExceptProvided(
                        pallet.Id,
                        pallet.SadPalletItems.Where(i => !i.IsNew()).Select(i => i.Id)
                    );

                foreach (SadPalletItem item in pallet.SadPalletItems.Where(i => !i.IsNew())) {
                    SadPalletItem itemFromDb = sadPalletItemRepository.GetById(item.Id);

                    double qtyDifference = itemFromDb.Qty - item.Qty;

                    if (qtyDifference.Equals(0d)) continue;

                    sadItemRepository.RestoreUnpackedQtyById(item.SadItemId, qtyDifference);

                    sadPalletItemRepository.Update(item);
                }

                foreach (SadPalletItem item in pallet.SadPalletItems.Where(i => i.IsNew())) {
                    SadPalletItem itemFromDb = sadPalletItemRepository.GetByPalletAndSadItemIdIfExists(pallet.Id, item.SadItem.Id);

                    sadItemRepository.DecreaseUnpackedQtyById(item.SadItem.Id, item.Qty);

                    if (itemFromDb != null) {
                        itemFromDb.Qty += item.Qty;

                        sadPalletItemRepository.Update(itemFromDb);
                    } else {
                        item.SadItemId = item.SadItem.Id;
                        item.SadPalletId = pallet.Id;

                        sadPalletItemRepository.Add(item);
                    }
                }
            }

            IEnumerable<SadItem> deletedItems =
                sadItemRepository
                    .GetAllItemsForRemoveBySadIdExceptProvidedItemIds(
                        message.Sad.Id,
                        message.Sad.SadItems.Where(i => !i.IsNew()).Select(i => i.Id)
                    );

            sadItemRepository.RemoveAllByIds(deletedItems.Select(i => i.Id));

            List<long> createdItemIds = new();

            foreach (SadItem sadItem in message.Sad.SadItems.Where(i => i.IsNew())) {
                SupplyOrderUkraineCartItem itemFromDb = cartItemRepository.GetByIdWithReservations(sadItem.SupplyOrderUkraineCartItem.Id);

                if (itemFromDb == null) continue;

                cartItemRepository.Remove(itemFromDb.Id);

                sadItem.SadId = message.Sad.Id;
                sadItem.SupplyOrderUkraineCartItemId = itemFromDb.Id;

                sadItem.Id = sadItemRepository.Add(sadItem);

                createdItemIds.Add(sadItem.Id);

                if (itemFromDb.ReservedQty > sadItem.Qty) {
                    double operationQty = itemFromDb.ReservedQty - sadItem.Qty;

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
                        UnpackedQty = itemFromDb.UnpackedQty,
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
                    if (itemFromDb.ReservedQty.Equals(sadItem.Qty)) continue;

                    IEnumerable<ProductAvailability> availabilities =
                        productAvailabilityRepository
                            .GetByProductAndCultureIds(
                                itemFromDb.ProductId,
                                "pl"
                            );

                    double operationQty = sadItem.Qty - itemFromDb.ReservedQty;

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

                    itemFromDb.ReservedQty += sadItem.Qty - itemFromDb.ReservedQty - operationQty;

                    cartItemRepository.Update(itemFromDb);

                    sadItem.Qty -= operationQty;

                    sadItemRepository.Update(sadItem);
                }
            }

            List<SadItem> updatedItems =
                (from sadItem in message.Sad.SadItems.Where(i => !i.IsNew())
                    let itemFromDb = sadItemRepository.GetByIdWithoutIncludes(sadItem.Id)
                    where itemFromDb != null && !itemFromDb.Qty.Equals(sadItem.Qty)
                    select sadItem).ToList();

            ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(
                new ChangeReservationsOnConsignmentFromSadUpdateMessage(
                    message.Sad.Id,
                    createdItemIds,
                    updatedItems,
                    deletedItems,
                    message.UserNetId,
                    message.Sad.IsSend && message.Sad.IsSend != beforeUpdate?.IsSend,
                    Sender
                )
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetAllNotSentSadsMessage(GetAllNotSentSadsMessage message) {
        using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supplyUkraineRepositoriesFactory
                .NewSadRepository(connection, exchangeRateConnection)
                .GetAllNotSent(
                    message.Type
                )
        );
    }

    private void ProcessGetAllSentSadsMessage(GetAllSentSadsMessage message) {
        using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supplyUkraineRepositoriesFactory
                .NewSadRepository(connection, exchangeRateConnection)
                .GetAllSent()
        );
    }

    private void ProcessGetAllNotSentSadsFromSaleMessage(GetAllNotSentSadsFromSaleMessage message) {
        using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supplyUkraineRepositoriesFactory
                .NewSadRepository(connection, exchangeRateConnection)
                .GetAllNotSentFromSale(
                    message.Type
                )
        );
    }

    private void ProcessGetAllSadsFilteredMessage(GetAllSadsFilteredMessage message) {
        using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supplyUkraineRepositoriesFactory
                .NewSadRepository(connection, exchangeRateConnection)
                .GetAllFiltered(
                    message.From,
                    message.To,
                    message.Limit,
                    message.Offset
                )
        );
    }

    private void ProcessGetSadByNetIdMessage(GetSadByNetIdMessage message) {
        using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supplyUkraineRepositoriesFactory
                .NewSadRepository(connection, exchangeRateConnection)
                .GetByNetId(
                    message.NetId
                )
        );
    }

    private void ProcessGetSadWithProductSpecificationByNetIdMessage(GetSadWithProductSpecificationByNetIdMessage message) {
        using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supplyUkraineRepositoriesFactory
                .NewSadRepository(connection, exchangeRateConnection)
                .GetByNetIdWithProductSpecification(
                    message.NetId
                )
        );
    }

    private void ProcessExportSadDocumentsMessage(ExportSadDocumentsMessage message) {
        using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISadRepository sadRepository = _supplyUkraineRepositoriesFactory.NewSadRepository(connection, exchangeRateConnection);

        Sad sad =
            sadRepository
                .GetByNetIdWithoutIncludes(
                    message.NetId
                );

        if (sad != null) {
            User user =
                _userRepositoriesFactory
                    .NewUserRepository(connection)
                    .GetByNetIdWithoutIncludes(
                        message.UserNetId
                    );

            if (sad.IsFromSale) {
                Sad sadPl = sadRepository.GetForDocumentsExportByNetIdAndCultureWithProductSpecification(message.NetId, "pl");
                Sad sadUk = sadRepository.GetForDocumentsExportByNetIdAndCultureWithProductSpecification(message.NetId, "uk");

                (string xlsxFile, string pdfFile) =
                    _xlsFactoryManager
                        .NewTaxFreeAndSadXlsManager()
                        .ExportSadInvoiceToXlsx(message.Path, sadPl, sadUk, $"{user.LastName} {user.FirstName}", true);

                (string oldXlsxFile, string oldPdfFile) =
                    _xlsFactoryManager
                        .NewTaxFreeAndSadXlsManager()
                        .ExportOldSadInvoiceToXlsx(message.Path, sadPl, sadUk, $"{user.LastName} {user.FirstName}", true);

                List<GroupedProductSpecification> specificationsPl = new();
                List<GroupedProductSpecification> specificationsUk = new();

                foreach (Sale sale in sadPl.Sales)
                foreach (OrderItem item in sale.Order.OrderItems)
                    if (item.Product.ProductSpecifications.Any()) {
                        ProductSpecification specification = item.Product.ProductSpecifications.Last();

                        if (specificationsPl.Any(s => s.SpecificationCode.ToLower().Equals(specification.SpecificationCode.ToLower())))
                            specificationsPl
                                .First(s => s.SpecificationCode.ToLower().Equals(specification.SpecificationCode.ToLower()))
                                .OrderItems
                                .Add(item);
                        else
                            specificationsPl
                                .Add(new GroupedProductSpecification {
                                    SpecificationCode = specification.SpecificationCode,
                                    OrderItems = new List<OrderItem> {
                                        item
                                    }
                                });
                    } else {
                        if (specificationsPl.Any(s => s.SpecificationCode.Equals(string.Empty)))
                            specificationsPl
                                .First(s => s.SpecificationCode.Equals(string.Empty))
                                .OrderItems
                                .Add(item);
                        else
                            specificationsPl
                                .Add(new GroupedProductSpecification {
                                    SpecificationCode = string.Empty,
                                    OrderItems = new List<OrderItem> {
                                        item
                                    }
                                });
                    }

                foreach (Sale sale in sadUk.Sales)
                foreach (OrderItem item in sale.Order.OrderItems)
                    if (item.Product.ProductSpecifications.Any()) {
                        ProductSpecification specification = item.Product.ProductSpecifications.Last();

                        if (specificationsUk.Any(s => s.SpecificationCode.ToLower().Equals(specification.SpecificationCode.ToLower())))
                            specificationsUk
                                .First(s => s.SpecificationCode.ToLower().Equals(specification.SpecificationCode.ToLower()))
                                .OrderItems
                                .Add(item);
                        else
                            specificationsUk
                                .Add(new GroupedProductSpecification {
                                    SpecificationCode = specification.SpecificationCode,
                                    OrderItems = new List<OrderItem> {
                                        item
                                    }
                                });
                    } else {
                        if (specificationsUk.Any(s => s.SpecificationCode.Equals(string.Empty)))
                            specificationsUk
                                .First(s => s.SpecificationCode.Equals(string.Empty))
                                .OrderItems
                                .Add(item);
                        else
                            specificationsUk
                                .Add(new GroupedProductSpecification {
                                    SpecificationCode = string.Empty,
                                    OrderItems = new List<OrderItem> {
                                        item
                                    }
                                });
                    }

                //ToDo:
                (string specificationXlsxFile, string specificationPdfFile) =
                    _xlsFactoryManager
                        .NewProductsXlsManager()
                        .ExportSadProductSpecification(
                            message.Path,
                            sadPl,
                            sadUk,
                            specificationsPl,
                            specificationsUk,
                            true
                        );

                (string oldSpecificationXlsxFile, string oldSpecificationPdfFile) =
                    _xlsFactoryManager
                        .NewProductsXlsManager()
                        .ExportOldSadProductSpecification(
                            message.Path,
                            sadPl,
                            sadUk,
                            specificationsPl,
                            specificationsUk,
                            true
                        );

                Sender.Tell(
                    new ExportSadDocumentsResponse(
                        xlsxFile,
                        pdfFile,
                        specificationXlsxFile,
                        specificationPdfFile,
                        oldXlsxFile,
                        oldPdfFile,
                        oldSpecificationXlsxFile,
                        oldSpecificationPdfFile
                    )
                );
            } else {
                Sad sadPl = sadRepository.GetForDocumentsExportByNetIdAndCultureWithProductSpecification(message.NetId, "pl");
                Sad sadUk = sadRepository.GetForDocumentsExportByNetIdAndCultureWithProductSpecification(message.NetId, "uk");

                (string xlsxFile, string pdfFile) =
                    _xlsFactoryManager
                        .NewTaxFreeAndSadXlsManager()
                        .ExportSadInvoiceToXlsx(message.Path, sadPl, sadUk, $"{user.LastName} {user.FirstName}");

                (string oldXlsxFile, string oldPdfFile) =
                    _xlsFactoryManager
                        .NewTaxFreeAndSadXlsManager()
                        .ExportOldSadInvoiceToXlsx(message.Path, sadPl, sadUk, $"{user.LastName} {user.FirstName}");

                List<GroupedProductSpecification> specificationsPl = new();
                List<GroupedProductSpecification> specificationsUk = new();

                foreach (SadItem item in sadPl.SadItems)
                    if (item.SupplyOrderUkraineCartItem.Product.ProductSpecifications.Any()) {
                        ProductSpecification specification = item.SupplyOrderUkraineCartItem.Product.ProductSpecifications.Last();

                        if (specificationsPl.Any(s => s.SpecificationCode.ToLower().Equals(specification.SpecificationCode.ToLower())))
                            specificationsPl
                                .First(s => s.SpecificationCode.ToLower().Equals(specification.SpecificationCode.ToLower()))
                                .SadItems
                                .Add(item);
                        else
                            specificationsPl
                                .Add(new GroupedProductSpecification {
                                    SpecificationCode = specification.SpecificationCode,
                                    SadItems = new List<SadItem> {
                                        item
                                    }
                                });
                    } else {
                        if (specificationsPl.Any(s => s.SpecificationCode.Equals(string.Empty)))
                            specificationsPl
                                .First(s => s.SpecificationCode.Equals(string.Empty))
                                .SadItems
                                .Add(item);
                        else
                            specificationsPl
                                .Add(new GroupedProductSpecification {
                                    SpecificationCode = string.Empty,
                                    SadItems = new List<SadItem> {
                                        item
                                    }
                                });
                    }

                foreach (SadItem item in sadUk.SadItems)
                    if (item.SupplyOrderUkraineCartItem.Product.ProductSpecifications.Any()) {
                        ProductSpecification specification = item.SupplyOrderUkraineCartItem.Product.ProductSpecifications.Last();

                        if (specificationsUk.Any(s => s.SpecificationCode.ToLower().Equals(specification.SpecificationCode.ToLower())))
                            specificationsUk
                                .First(s => s.SpecificationCode.ToLower().Equals(specification.SpecificationCode.ToLower()))
                                .SadItems
                                .Add(item);
                        else
                            specificationsUk
                                .Add(new GroupedProductSpecification {
                                    SpecificationCode = specification.SpecificationCode,
                                    SadItems = new List<SadItem> {
                                        item
                                    }
                                });
                    } else {
                        if (specificationsUk.Any(s => s.SpecificationCode.Equals(string.Empty)))
                            specificationsUk
                                .First(s => s.SpecificationCode.Equals(string.Empty))
                                .SadItems
                                .Add(item);
                        else
                            specificationsUk
                                .Add(new GroupedProductSpecification {
                                    SpecificationCode = string.Empty,
                                    SadItems = new List<SadItem> {
                                        item
                                    }
                                });
                    }

                //ToDo:
                (string specificationXlsxFile, string specificationPdfFile) =
                    _xlsFactoryManager
                        .NewProductsXlsManager()
                        .ExportSadProductSpecification(
                            message.Path,
                            sadPl,
                            sadUk,
                            specificationsPl,
                            specificationsUk
                        );

                (string oldSpecificationXlsxFile, string oldSpecificationPdfFile) =
                    _xlsFactoryManager
                        .NewProductsXlsManager()
                        .ExportOldSadProductSpecification(
                            message.Path,
                            sadPl,
                            sadUk,
                            specificationsPl,
                            specificationsUk
                        );

                Sender.Tell(
                    new ExportSadDocumentsResponse(
                        xlsxFile,
                        pdfFile,
                        specificationXlsxFile,
                        specificationPdfFile,
                        oldXlsxFile,
                        oldPdfFile,
                        oldSpecificationXlsxFile,
                        oldSpecificationPdfFile
                    )
                );
            }
        } else {
            Sender.Tell(
                new ExportSadDocumentsResponse(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty
                )
            );
        }
    }

    private void ProcessUploadSadDocumentsBySadNetIdMessage(UploadSadDocumentsBySadNetIdMessage message) {
        try {
            using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISadRepository sadRepository = _supplyUkraineRepositoriesFactory.NewSadRepository(connection, exchangeRateConnection);

            Sad sad = sadRepository.GetByNetId(message.NetId);

            if (sad == null) throw new Exception("Sad with provided NetId does not exists");

            _supplyUkraineRepositoriesFactory
                .NewSadDocumentRepository(connection)
                .Add(
                    message
                        .Documents
                        .Select(document => {
                            document.SadId = sad.Id;

                            return document;
                        })
                );

            Sender.Tell(
                sadRepository
                    .GetByNetId(
                        message.NetId
                    )
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessRemoveSadDocumentByNetIdMessage(RemoveSadDocumentByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _supplyUkraineRepositoriesFactory
            .NewSadDocumentRepository(connection)
            .Remove(
                message.NetId
            );
    }

    private void ProcessDeleteSadByNetIdMessage(DeleteSadByNetIdMessage message) {
        using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISadRepository sadRepository = _supplyUkraineRepositoriesFactory.NewSadRepository(connection, exchangeRateConnection);

        Sad sad = sadRepository.GetByNetIdWithoutIncludes(message.SadNetId);

        if (sad != null) {
            if (!sad.IsSend) {
                if (sad.IsFromSale) {
                    ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

                    sad = sadRepository.GetByNetId(message.SadNetId);

                    foreach (Sale sale in sad.Sales) {
                        sale.SadId = null;

                        saleRepository.UpdateSadReference(sale);
                    }
                } else {
                    ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR)
                        .Tell(new RestoreReservationsOnConsignmentFromSadDeleteMessage(sad.Id, message.UserNetId));
                }

                sadRepository.Delete(sad.Id);

                Sender.Tell(
                    (true, string.Empty)
                );
            } else {
                Sender.Tell(
                    (false, "Specified Sad is already sent")
                );
            }
        } else {
            Sender.Tell(
                (false, "Sad with provided NetId does not exists in database")
            );
        }
    }

    private void ProcessAddOrUpdateSadFromSaleMessage(AddOrUpdateSadFromSaleMessage message) {
        try {
            using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.Sad == null) throw new Exception("Sad entity can not be null or empty");

            ISadRepository sadRepository = _supplyUkraineRepositoriesFactory.NewSadRepository(connection, exchangeRateConnection);
            ISadItemRepository sadItemRepository = _supplyUkraineRepositoriesFactory.NewSadItemRepository(connection);
            ISadPalletRepository sadPalletRepository = _supplyUkraineRepositoriesFactory.NewSadPalletRepository(connection);
            ISadPalletItemRepository sadPalletItemRepository = _supplyUkraineRepositoriesFactory.NewSadPalletItemRepository(connection);

            if (!message.Sad.IsNew()) {
                Sad sadFromDb = sadRepository.GetById(message.Sad.Id);

                if (sadFromDb == null)
                    throw new Exception("Specified Sad does not exists in database");
                if (sadFromDb.IsSend) throw new Exception("Specified Sad is already sent");

                if (message.Sad.IsSend) {
                    if (!message.Sad.OrganizationId.HasValue && (message.Sad.Organization == null || message.Sad.Organization.IsNew()))
                        throw new Exception("You need to specify Organization");

                    if (!message.Sad.Sales.Any()) throw new Exception("You need to specify at least one Sale");

                    long clientId = message.Sad.Sales.First().ClientAgreement.ClientId;

                    if (!message.Sad.Sales.All(s => s.ClientAgreement.ClientId.Equals(clientId))) throw new Exception("All Sales must be from same Client");

                    message.Sad.ClientId = clientId;
                }
            }

            if (message.Sad.SadPallets.Any()) {
                if (!message.Sad.SadPallets.All(p => p.SadPalletType != null && !p.SadPalletType.IsNew()))
                    throw new Exception("All SadPallets should have specified SadPalletType");

                List<SadItem> items = new();

                foreach (SadPallet pallet in message.Sad.SadPallets)
                foreach (SadPalletItem item in pallet.SadPalletItems) {
                    SadItem sadItemFromDb = sadItemRepository.GetById(item.SadItem.Id);

                    if (sadItemFromDb == null) continue;
                    if (!items.Any(i => i.Id.Equals(sadItemFromDb.Id))) {
                        if (!item.IsNew()) {
                            SadPalletItem palletItemFromDb = sadPalletItemRepository.GetById(item.Id);

                            if (palletItemFromDb.Qty < item.Qty && item.Qty > sadItemFromDb.UnpackedQty + palletItemFromDb.Qty)
                                throw new Exception(
                                    string.Format(
                                        "You specified more Qty for {0} SadPalletItem than available for packing",
                                        sadItemFromDb.OrderItem.Product.VendorCode
                                    )
                                );
                        } else {
                            if (message.Sad.SadItems.Any(i => i.Id.Equals(sadItemFromDb.Id))) {
                                if (item.Qty > sadItemFromDb.UnpackedQty)
                                    throw new Exception(
                                        string.Format(
                                            "You specified more Qty for {0} SadPalletItem than available for packing",
                                            sadItemFromDb.OrderItem.Product.VendorCode
                                        )
                                    );

                                sadItemFromDb.UnpackedQty -= item.Qty;

                                items.Add(sadItemFromDb);
                            } else {
                                throw new Exception(
                                    string.Format(
                                        "Product {0} can't be used as SadPalletItem because it does not exists in current TaxFreePackList",
                                        sadItemFromDb.OrderItem.Product.VendorCode
                                    )
                                );
                            }
                        }
                    } else {
                        SadItem itemFromList = items.First(i => i.Id.Equals(sadItemFromDb.Id));

                        if (item.Qty > itemFromList.UnpackedQty)
                            throw new Exception(
                                string.Format(
                                    "You specified more Qty for {0} SadPalletItem than available for packing",
                                    sadItemFromDb.OrderItem.Product.VendorCode
                                )
                            );
                        itemFromList.UnpackedQty -= item.Qty;
                    }
                }
            }

            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);

            User user = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

            if (message.Sad.Organization != null && !message.Sad.Organization.IsNew())
                message.Sad.OrganizationId = message.Sad.Organization.Id;
            else
                message.Sad.OrganizationId = null;

            if (message.Sad.Organization != null && !message.Sad.Organization.IsNew())
                message.Sad.OrganizationId = message.Sad.Organization.Id;
            else
                message.Sad.OrganizationId = null;

            if (message.Sad.Statham != null && !message.Sad.Statham.IsNew())
                message.Sad.StathamId = message.Sad.Statham.Id;
            else
                message.Sad.StathamId = null;

            if (message.Sad.StathamCar != null && !message.Sad.StathamCar.IsNew())
                message.Sad.StathamCarId = message.Sad.StathamCar.Id;
            else
                message.Sad.StathamCarId = null;

            if (message.Sad.StathamPassport != null && !message.Sad.StathamPassport.IsNew())
                message.Sad.StathamPassportId = message.Sad.StathamPassport.Id;
            else
                message.Sad.StathamPassportId = null;

            if (message.Sad.OrganizationClient != null && !message.Sad.OrganizationClient.IsNew())
                message.Sad.OrganizationClientId = message.Sad.OrganizationClient.Id;
            else
                message.Sad.OrganizationClientId = null;

            if (message.Sad.OrganizationClientAgreement != null && !message.Sad.OrganizationClientAgreement.IsNew())
                message.Sad.OrganizationClientAgreementId = message.Sad.OrganizationClientAgreement.Id;
            else
                message.Sad.OrganizationClientAgreementId = null;

            if (message.Sad.Sales.Any()) {
                Sale sale = message.Sad.Sales.First();

                message.Sad.ClientId = sale?.ClientAgreement?.ClientId;
                message.Sad.ClientAgreementId = sale?.ClientAgreement?.Id;

                if (message.Sad.IsNew()) message.Sad.VatPercent = sale?.SaleInvoiceDocument?.Vat ?? 23m;
            }

            message.Sad.FromDate =
                message.Sad.FromDate.Year.Equals(1)
                    ? DateTime.UtcNow
                    : TimeZoneInfo.ConvertTimeToUtc(message.Sad.FromDate);

            Sad beforeUpdate = sadRepository.GetByIdWithoutIncludes(message.Sad.Id);

            if (message.Sad.IsNew()) {
                Sad lastRecord = sadRepository.GetLastRecord();

                message.Sad.Number =
                    lastRecord != null && lastRecord.FromDate.Year.Equals(DateTime.Now.Year)
                        ? string.Format(
                            "EX{0:D10}",
                            lastRecord.Number.StartsWith("EX")
                                ? Convert.ToInt64(lastRecord.Number.Substring(2, 10)) + 1
                                : Convert.ToInt64(lastRecord.Number) + 1
                        )
                        : string.Format("EX{0:D10}", 1);

                message.Sad.ResponsibleId = user.Id;

                message.Sad.IsSend = false;
                message.Sad.IsFromSale = true;

                message.Sad.Id = sadRepository.Add(message.Sad);
            } else {
                sadRepository.Update(message.Sad);
            }

            IEnumerable<Sale> salesToDelete =
                saleRepository
                    .GetAllSalesBySadIdExceptProvided(
                        message.Sad.Id,
                        message.Sad.Sales.Select(s => s.Id)
                    );

            foreach (Sale sale in salesToDelete) {
                sale.SadId = null;

                saleRepository.UpdateSadReference(sale);
            }

            foreach (Sale sale in message.Sad.Sales.Where(s => !s.SadId.HasValue)) {
                List<OrderItem> items = orderItemRepository.GetAllWithConsignmentMovementBySaleId(sale.Id);

                foreach (OrderItem orderItem in items)
                    if (orderItem.ConsignmentItemMovements.Any()) {
                        double operationQty = orderItem.Qty;

                        foreach (ConsignmentItemMovement movement in orderItem.ConsignmentItemMovements) {
                            sadItemRepository
                                .Add(new SadItem {
                                    SadId = message.Sad.Id,
                                    OrderItemId = orderItem.Id,
                                    Qty = movement.Qty,
                                    UnpackedQty = movement.Qty,
                                    NetWeight = movement.ConsignmentItem.Weight,
                                    UnitPrice = orderItem.PricePerItem,
                                    ConsignmentItemId = movement.ConsignmentItemId
                                });

                            operationQty -= movement.RemainingQty;
                        }

                        if (operationQty > 0)
                            sadItemRepository
                                .Add(new SadItem {
                                    SadId = message.Sad.Id,
                                    OrderItemId = orderItem.Id,
                                    Qty = operationQty,
                                    UnpackedQty = operationQty,
                                    NetWeight = 0d,
                                    UnitPrice = orderItem.PricePerItem
                                });
                    } else {
                        sadItemRepository
                            .Add(new SadItem {
                                SadId = message.Sad.Id,
                                OrderItemId = orderItem.Id,
                                Qty = orderItem.Qty,
                                UnpackedQty = orderItem.Qty,
                                NetWeight = 0d,
                                UnitPrice = orderItem.PricePerItem
                            });
                    }

                sale.SadId = message.Sad.Id;

                saleRepository.UpdateSadReference(sale);
            }

            List<SadPallet> palletsToDelete =
                sadPalletRepository
                    .GetAllBySadIdExceptProvided(
                        message.Sad.Id,
                        message.Sad.SadPallets.Where(p => !p.IsNew()).Select(p => p.Id)
                    );

            foreach (SadPallet pallet in palletsToDelete) {
                foreach (SadPalletItem item in pallet.SadPalletItems) sadItemRepository.RestoreUnpackedQtyById(item.SadItemId, item.Qty);

                sadPalletRepository.Remove(pallet.Id);
            }

            foreach (SadPallet pallet in message.Sad.SadPallets) {
                pallet.SadId = message.Sad.Id;
                pallet.SadPalletTypeId = pallet.SadPalletType.Id;

                if (pallet.IsNew())
                    pallet.Id = sadPalletRepository.Add(pallet);
                else
                    sadPalletRepository.Update(pallet);

                sadPalletItemRepository
                    .RestoreUnpackedQtyByPalletIdExceptProvidedIds(
                        pallet.Id,
                        pallet.SadPalletItems.Where(i => !i.IsNew()).Select(i => i.Id)
                    );

                sadPalletItemRepository
                    .RemoveAllByPalletIdExceptProvided(
                        pallet.Id,
                        pallet.SadPalletItems.Where(i => !i.IsNew()).Select(i => i.Id)
                    );

                foreach (SadPalletItem item in pallet.SadPalletItems.Where(i => !i.IsNew())) {
                    SadPalletItem itemFromDb = sadPalletItemRepository.GetById(item.Id);

                    double qtyDifference = itemFromDb.Qty - item.Qty;

                    if (qtyDifference.Equals(0d)) continue;

                    sadItemRepository.RestoreUnpackedQtyById(item.SadItemId, qtyDifference);

                    sadPalletItemRepository.Update(item);
                }

                foreach (SadPalletItem item in pallet.SadPalletItems.Where(i => i.IsNew())) {
                    SadPalletItem itemFromDb = sadPalletItemRepository.GetByPalletAndSadItemIdIfExists(pallet.Id, item.SadItem.Id);

                    sadItemRepository.DecreaseUnpackedQtyById(item.SadItem.Id, item.Qty);

                    if (itemFromDb != null) {
                        itemFromDb.Qty += item.Qty;

                        sadPalletItemRepository.Update(itemFromDb);
                    } else {
                        item.SadItemId = item.SadItem.Id;
                        item.SadPalletId = pallet.Id;

                        sadPalletItemRepository.Add(item);
                    }
                }
            }

            message.Sad =
                sadRepository
                    .GetById(
                        message.Sad.Id
                    );

            if (message.Sad.IsSend) {
                if (message.Sad.OrganizationClient != null && !message.Sad.MarginAmount.Equals(message.Sad.OrganizationClient.MarginAmount)) {
                    message.Sad.OrganizationClient.MarginAmount = message.Sad.MarginAmount;

                    _organizationClientRepositoriesFactory
                        .NewOrganizationClientRepository(connection)
                        .Update(
                            message
                                .Sad
                                .OrganizationClient
                        );
                }

                if (beforeUpdate?.IsSend != message.Sad.IsSend)
                    ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new StoreConsignmentMovementFromSadFromSaleMessage(message.Sad.Id));
            }

            Sender.Tell(
                sadRepository
                    .GetById(
                        message.Sad.Id
                    )
            );

            ActorReferenceManager.Instance.Get(BaseActorNames.PRODUCTS_MANAGEMENT_ACTOR).Tell(new UpdateSadProductSpecificationAssignmentsMessage(message.Sad.NetUid));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUploadProductSpecificationForSad(UploadProductSpecificationForSadMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sad sad =
            _supplyUkraineRepositoriesFactory
                .NewSadRepository(connection, null)
                .GetByNetIdWithProductSpecification(message.SadNetId, CultureInfo.CurrentCulture.TwoLetterISOLanguageName);

        if (sad == null) {
            Sender.Tell(null);

            return;
        }

        List<ParsedProductSpecification> parsedSpecifications =
            _xlsFactoryManager
                .NewParseConfigurationXlsManager()
                .GetProductSpecificationsFromUploadByConfiguration(
                    message.PathToFile,
                    message.ParseConfiguration
                );

        if (parsedSpecifications.All(s => s.HasError) || !parsedSpecifications.Any()) {
            Sender.Tell(null);

            return;
        }

        IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
        IProductSpecificationRepository specificationRepository = _productRepositoriesFactory.NewProductSpecificationRepository(connection);
        IOrderProductSpecificationRepository orderProductSpecificationRepository = _supplyRepositoriesFactory.NewOrderProductSpecificationRepository(connection);

        User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

        UploadProductSpecificationResult result = new();

        List<ProductSpecification> specifications = new();

        foreach (ParsedProductSpecification parsedSpecification in parsedSpecifications) {
            Product product = getSingleProductRepository.GetProductByVendorCode(parsedSpecification.VendorCode);

            decimal totalCustoms = !(parsedSpecification.CustomsValue + parsedSpecification.Duty).Equals(0)
                ? parsedSpecification.CustomsValue + parsedSpecification.Duty
                : 1;

            decimal customsValue = !parsedSpecification.CustomsValue.Equals(0) ? parsedSpecification.CustomsValue : 1;

            if (product == null || parsedSpecification.HasError)
                result.MissingProducts.Add(parsedSpecification.VendorCode);
            else
                specifications.Add(new ProductSpecification {
                    CustomsValue = parsedSpecification.CustomsValue,
                    SpecificationCode = parsedSpecification.SpecificationCode,
                    Duty = parsedSpecification.Duty,
                    VATValue = parsedSpecification.VATValue,
                    Locale = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                    ProductId = product.Id,
                    Product = product,
                    AddedById = user.Id,
                    DutyPercent =
                        decimal.Round(parsedSpecification.Duty * 100 / customsValue, 1, MidpointRounding.AwayFromZero),
                    VATPercent =
                        decimal.Round(parsedSpecification.VATValue * 100 / totalCustoms, 1, MidpointRounding.AwayFromZero)
                });
        }

        foreach (ProductSpecification specification in specifications) {
            SadItem sadItem =
                sad.IsFromSale
                    ? sad.SadItems.FirstOrDefault(i => i.OrderItem.ProductId.Equals(specification.ProductId))
                    : sad.SadItems.FirstOrDefault(i => i.SupplyOrderUkraineCartItem.ProductId.Equals(specification.ProductId));

            if (sadItem == null) {
                result.MissingProducts.Add(specification.Product.VendorCode);

                continue;
            }

            if (sadItem.ProductSpecification == null) {
                ProductSpecification activeSpecification =
                    specificationRepository
                        .GetActiveByProductIdAndLocale(
                            specification.ProductId,
                            CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                        );

                specification.Id = specificationRepository.Add(specification);

                orderProductSpecificationRepository.Add(new OrderProductSpecification {
                    SadId = sad.Id,
                    ProductSpecificationId = specification.Id,
                    Qty = sadItem.Qty
                });

                result.SuccessfullyUpdatedProducts.Add(specification.Product.VendorCode);

                if (activeSpecification != null) continue;

                specificationRepository.SetInactiveByProductId(specification.ProductId, CultureInfo.CurrentCulture.TwoLetterISOLanguageName);

                specification.IsActive = true;

                specificationRepository.Update(specification);
            } else {
                if (sadItem.ProductSpecification.SpecificationCode == specification.SpecificationCode &&
                    sadItem.ProductSpecification.Name == specification.Name &&
                    sadItem.ProductSpecification.DutyPercent == specification.DutyPercent) {
                    result.UpdateNotRequiredProducts.Add(specification.Product.VendorCode);

                    continue;
                }

                ProductSpecification activeSpecification =
                    specificationRepository
                        .GetActiveByProductIdAndLocale(
                            specification.ProductId,
                            CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                        );

                specification.Id = specificationRepository.Add(specification);

                orderProductSpecificationRepository.Add(new OrderProductSpecification {
                    SadId = sad.Id,
                    ProductSpecificationId = specification.Id,
                    Qty = sadItem.Qty
                });

                result.SuccessfullyUpdatedProducts.Add(specification.Product.VendorCode);

                if (activeSpecification != null) continue;

                specificationRepository.SetInactiveByProductId(specification.ProductId, CultureInfo.CurrentCulture.TwoLetterISOLanguageName);

                specification.IsActive = true;

                specificationRepository.Update(specification);
            }
        }

        Sender.Tell(result);
    }

    private void ProcessFinishAddOrUpdateSadMessage(FinishAddOrUpdateSadMessage message) {
        try {
            using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISadRepository sadRepository = _supplyUkraineRepositoriesFactory.NewSadRepository(connection, exchangeRateConnection);

            Sad sad = sadRepository.GetById(message.SadId);

            if (sad != null && sad.IsSend && sad.OrganizationClient != null && !sad.MarginAmount.Equals(sad.OrganizationClient.MarginAmount)) {
                sad.OrganizationClient.MarginAmount = sad.MarginAmount;

                _organizationClientRepositoriesFactory
                    .NewOrganizationClientRepository(connection)
                    .Update(
                        sad.OrganizationClient
                    );
            }

            Sender.Tell(sad);

            ActorReferenceManager.Instance.Get(BaseActorNames.PRODUCTS_MANAGEMENT_ACTOR)
                .Tell(new UpdateSadProductSpecificationAssignmentsMessage(sad?.NetUid ?? Guid.Empty));
        } catch (Exception e) {
            Sender.Tell(e);
        }
    }
}