using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Akka.Actor;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Returns;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Messages.Consignments;
using GBA.Domain.Messages.Consignments.Sads;
using GBA.Domain.Messages.Consignments.TaxFreePackLists;
using GBA.Domain.Messages.Sales;
using GBA.Domain.Messages.Supplies.Ukraine.Sads;
using GBA.Domain.Messages.Supplies.Ukraine.TaxFreePackLists;
using GBA.Domain.Repositories.Consignments.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.DepreciatedOrders.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.ReSales.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Consignments;

public sealed class ConsignmentsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConsignmentRepositoriesFactory _consignmentRepositoriesFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IDepreciatedRepositoriesFactory _depreciatedRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IReSaleRepositoriesFactory _reSaleRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public ConsignmentsActor(
        IDbConnectionFactory connectionFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IReSaleRepositoriesFactory reSaleRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IConsignmentRepositoriesFactory consignmentRepositoriesFactory,
        IDepreciatedRepositoriesFactory depreciatedRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _reSaleRepositoriesFactory = reSaleRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _consignmentRepositoriesFactory = consignmentRepositoriesFactory;
        _depreciatedRepositoriesFactory = depreciatedRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;

        Receive<AddNewConsignmentMessage>(ProcessAddNewConsignmentMessage);

        Receive<AddNewConsignmentFromProductTransferMessage>(ProcessAddNewConsignmentFromProductTransferMessage);

        Receive<StoreConsignmentMovementFromDepreciatedOrderMessage>(ProcessStoreConsignmentMovementFromDepreciatedOrderMessage);

        Receive<StoreConsignmentMovementFromSadMessage>(ProcessStoreConsignmentMovementFromSadMessage);

        Receive<StoreConsignmentMovementFromSadFromSaleMessage>(ProcessStoreConsignmentMovementFromSadFromSaleMessage);

        Receive<StoreConsignmentMovementFromTaxFreePackListMessage>(ProcessStoreConsignmentMovementFromTaxFreePackListMessage);

        Receive<StoreConsignmentMovementFromTaxFreePackListFromSaleMessage>(ProcessStoreConsignmentMovementFromTaxFreePackListFromSaleMessage);

        Receive<StoreConsignmentMovementFromSaleMessage>(ProcessStoreConsignmentMovementFromSaleMessage);

        Receive<StoreConsignmentMovementFromNewOrderItemMessage>(ProcessStoreConsignmentMovementFromNewOrderItemMessage);

        Receive<StoreConsignmentMovementFromOrderItemBaseShiftStatusMessage>(ProcessStoreConsignmentMovementFromOrderItemBaseShiftStatusMessage);

        Receive<ValidateAndStoreConsignmentMovementFromSupplyReturnMessage>(ProcessValidateAndStoreConsignmentMovementFromSupplyReturnMessage);

        Receive<AddReservationOnConsignmentFromNewSadMessage>(ProcessAddReservationOnConsignmentFromNewSadMessage);

        Receive<ChangeReservationsOnConsignmentFromSadUpdateMessage>(ProcessChangeReservationsOnConsignmentFromSadUpdateMessage);

        Receive<RestoreReservationsOnConsignmentFromSadDeleteMessage>(ProcessRestoreReservationsOnConsignmentFromSadDeleteMessage);

        Receive<ChangeReservationsOnConsignmentFromTaxFreePackListMessage>(ProcessChangeReservationsOnConsignmentFromTaxFreePackListMessage);

        Receive<RestoreReservationsOnConsignmentFromTaxFreePackListDeleteMessage>(ProcessRestoreReservationsOnConsignmentFromTaxFreePackListDeleteMessage);

        Receive<RestoreReservationsOnConsignmentFromUnpackedTaxFreePackListCartItemsMessage>(
            ProcessRestoreReservationsOnConsignmentFromUnpackedTaxFreePackListCartItemsMessage);

        Receive<UpdateConsignmentItemGrossPriceMessage>(ProcessUpdateConsignmentItemGrossPrice);

        Receive<StoreConsignmentFromDepreciatedOrderWithReSaleMessage>(ProcessStoreConsignmentFromDepreciatedOrderWithReSale);

        Receive<StoreConsignmentFromProductTransferWithReSaleMessage>(ProcessStoreConsignmentFromProductTransferWithReSale);
    }

    private void ProcessAddNewConsignmentMessage(AddNewConsignmentMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ICurrencyRepository currencyRepository =
            _currencyRepositoriesFactory.NewCurrencyRepository(connection);
        IGovExchangeRateRepository govExchangeRateRepository =
            _exchangeRateRepositoriesFactory.NewGovExchangeRateRepository(connection);

        ProductIncome income =
            _productRepositoriesFactory
                .NewProductIncomeRepository(connection)
                .GetByIdForConsignmentCreate(
                    message.ProductIncomeId
                );

        if (income?.Organization == null) return;

        IConsignmentRepository consignmentRepository = _consignmentRepositoriesFactory.NewConsignmentRepository(connection);
        IConsignmentItemRepository consignmentItemRepository = _consignmentRepositoriesFactory.NewConsignmentItemRepository(connection);
        IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
        IProductSpecificationRepository specificationRepository = _productRepositoriesFactory.NewProductSpecificationRepository(connection);
        IConsignmentItemMovementRepository consignmentItemMovementRepository = _consignmentRepositoriesFactory.NewConsignmentItemMovementRepository(connection);
        IGovCrossExchangeRateRepository govCrossExchangeRateRepository = _exchangeRateRepositoriesFactory.NewGovCrossExchangeRateRepository(connection);
        ISupplyOrderUkraineItemRepository supplyOrderUkraineItemRepository =
            _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineItemRepository(connection);
        ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
        IProductLocationRepository productLocationRepository = _productRepositoriesFactory.NewProductLocationRepository(connection);

        Consignment consignment = new Consignment {
            IsVirtual = message.IsVirtual,
            FromDate = income.FromDate,
            StorageId = income.StorageId,
            OrganizationId = income.Organization.Id,
            ProductIncomeId = income.Id
        };

        consignment.Id = consignmentRepository.Add(consignment);

        decimal exchangeRateAmountToEur = 1m;
        decimal exchangeRateAmountToEurCorrect = 1m;

        if (income.ProductIncomeItems.Any()) {
            Currency fromCurrency;
            if (income.ProductIncomeItems.First().SupplyOrderUkraineItem != null) {
                SupplyOrderUkraine order = supplyOrderUkraineItemRepository.GetOrderByItemId(income.ProductIncomeItems.First().SupplyOrderUkraineItem.Id);
                fromCurrency = supplyOrderUkraineItemRepository.GetCurrencyFromOrderByItemId(income.ProductIncomeItems.First().SupplyOrderUkraineItem.Id);

                exchangeRateAmountToEur =
                    GetGovExchangeRateOnDateToEur(
                        fromCurrency,
                        order.InvDate,
                        govCrossExchangeRateRepository,
                        govExchangeRateRepository,
                        currencyRepository
                    );

                exchangeRateAmountToEurCorrect =
                    GetGovExchangeRateOnDateToEurCorrect(
                        fromCurrency,
                        order.InvDate,
                        govCrossExchangeRateRepository,
                        govExchangeRateRepository,
                        currencyRepository
                    );
            } else if (income.ProductIncomeItems.First().PackingListPackageOrderItem != null) {
                SupplyInvoice invoice = supplyOrderRepository.GetInvoiceByPackingListPackageOrderItemId(income.ProductIncomeItems.First().PackingListPackageOrderItem.Id);
                fromCurrency = supplyOrderRepository.GetCurrencyByInvoiceId(invoice.Id);

                exchangeRateAmountToEur =
                    GetGovExchangeRateOnDateToEur(
                        fromCurrency,
                        invoice.DateCustomDeclaration ?? invoice.Created,
                        govCrossExchangeRateRepository,
                        govExchangeRateRepository,
                        currencyRepository
                    );
                exchangeRateAmountToEurCorrect =
                    GetGovExchangeRateOnDateToEurCorrect(
                        fromCurrency,
                        invoice.DateCustomDeclaration ?? invoice.Created,
                        govCrossExchangeRateRepository,
                        govExchangeRateRepository,
                        currencyRepository
                    );
            }
        }

        foreach (ProductIncomeItem incomeItem in income.ProductIncomeItems) {
            if (incomeItem.SaleReturnItem != null) {
                if (!incomeItem
                        .SaleReturnItem
                        .OrderItem
                        .ConsignmentItemMovements
                        .Any(m => m.RemainingQty > 0)) continue;

                foreach (ConsignmentItemMovement movement in incomeItem
                             .SaleReturnItem
                             .OrderItem
                             .ConsignmentItemMovements
                             .Where(m => m.RemainingQty > 0)) {
                    double currentOperationQty = incomeItem.Qty;
                    //IEnumerable<ProductLocation> locations =
                    //productLocationRepository
                    //    .GetAllByOrderItemId(
                    //        movement.OrderItemId
                    //    );
                    if (movement.RemainingQty < currentOperationQty)
                        currentOperationQty = movement.RemainingQty;

                    movement.RemainingQty -= currentOperationQty;

                    consignmentItemMovementRepository.UpdateRemainingQty(movement);

                    incomeItem.Qty -= currentOperationQty;

                    ConsignmentItem item = new ConsignmentItem {
                        Qty = currentOperationQty,
                        RemainingQty = currentOperationQty,
                        ConsignmentId = consignment.Id,
                        ProductIncomeItemId = incomeItem.Id,
                        DutyPercent = incomeItem.OrderProductSpecification?.ProductSpecification?.DutyPercent ?? 0m,
                        ProductId = incomeItem.SaleReturnItem.OrderItem.ProductId,
                        Price = movement.ConsignmentItem.Price,
                        NetPrice = movement.ConsignmentItem.NetPrice,
                        AccountingPrice = movement.ConsignmentItem.AccountingPrice,
                        Weight = movement.ConsignmentItem.Weight,
                        RootConsignmentItemId = movement.ConsignmentItem.Id,
                        ProductSpecification = movement.ConsignmentItem?.ProductSpecification ?? new ProductSpecification(),
                        ExchangeRate = movement.ConsignmentItem.ExchangeRate
                    };

                    if (item.ProductSpecification.IsNew()) {
                        item.ProductSpecification.ProductId = item.ProductId;
                        item.ProductSpecification.Locale = income.Organization.Culture;
                        item.ProductSpecification.AddedById = _userRepositoriesFactory.NewUserRepository(connection).GetManagerOrGBAIdByClientNetId(Guid.Empty);
                    }

                    item.ProductSpecificationId = specificationRepository.Add(item.ProductSpecification);

                    ConsignmentItemMovement itemMovement = new ConsignmentItemMovement {
                        Qty = currentOperationQty,
                        IsIncomeMovement = true,
                        ProductIncomeItemId = incomeItem.Id,
                        MovementType = ConsignmentItemMovementType.Return,
                        ConsignmentItemId = consignmentItemRepository.Add(item)
                    };

                    consignmentItemMovementRepository.Add(itemMovement);

                    if (incomeItem.Qty.Equals(0d)) break;
                }
            } else {
                ConsignmentItem item = new ConsignmentItem {
                    Qty = incomeItem.Qty,
                    RemainingQty = incomeItem.Qty,
                    ConsignmentId = consignment.Id,
                    ProductIncomeItemId = incomeItem.Id,
                    DutyPercent = incomeItem.OrderProductSpecification?.ProductSpecification?.DutyPercent ?? 0m,
                    ProductSpecification = incomeItem.OrderProductSpecification?.ProductSpecification ?? new ProductSpecification(),
                    ExchangeRate = exchangeRateAmountToEur
                };

                ConsignmentItemMovement itemMovement = new ConsignmentItemMovement {
                    Qty = incomeItem.Qty,
                    IsIncomeMovement = true,
                    ProductIncomeItemId = incomeItem.Id
                };

                if (incomeItem.SupplyOrderUkraineItem != null) {
                    item.ProductId = incomeItem.SupplyOrderUkraineItem.ProductId;
                    item.Price = incomeItem.SupplyOrderUkraineItem.GrossUnitPrice;
                    item.Weight = incomeItem.SupplyOrderUkraineItem.GrossWeight;

                    if (exchangeRateAmountToEurCorrect.Equals(1))
                        item.NetPrice =
                            Math.Abs(exchangeRateAmountToEurCorrect > 0
                                ? incomeItem.SupplyOrderUkraineItem.UnitPriceLocal / exchangeRateAmountToEurCorrect
                                : Math.Abs(incomeItem.SupplyOrderUkraineItem.UnitPriceLocal * exchangeRateAmountToEurCorrect));
                    else
                        item.NetPrice = incomeItem.SupplyOrderUkraineItem.UnitPriceLocal;

                    item.AccountingPrice = incomeItem.SupplyOrderUkraineItem.AccountingGrossUnitPrice;
                    item.DutyPercent = 0;
                    if (incomeItem.SupplyOrderUkraineItem.ProductSpecification != null)
                        item.ProductSpecification = incomeItem.SupplyOrderUkraineItem.ProductSpecification;
                    itemMovement.MovementType = ConsignmentItemMovementType.UkraineOrder;
                }

                if (incomeItem.PackingListPackageOrderItem != null) {
                    item.ProductId = incomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem.ProductId;
                    item.Price = incomeItem.PackingListPackageOrderItem.GrossUnitPriceEur + incomeItem.PackingListPackageOrderItem.AccountingGrossUnitPriceEur +
                                 incomeItem.PackingListPackageOrderItem.AccountingGeneralGrossUnitPriceEur;
                    item.Weight = incomeItem.PackingListPackageOrderItem.GrossWeight;

                    if (exchangeRateAmountToEurCorrect.Equals(1))
                        item.NetPrice =
                            Math.Abs(exchangeRateAmountToEurCorrect > 0
                                ? incomeItem.PackingListPackageOrderItem.UnitPrice / exchangeRateAmountToEurCorrect
                                : Math.Abs(incomeItem.PackingListPackageOrderItem.UnitPrice * exchangeRateAmountToEurCorrect));
                    else
                        item.NetPrice = incomeItem.PackingListPackageOrderItem.UnitPrice;

                    item.AccountingPrice = incomeItem.PackingListPackageOrderItem.AccountingGrossUnitPriceEur;
                    itemMovement.MovementType = ConsignmentItemMovementType.Income;
                }

                if (incomeItem.ProductCapitalizationItem != null) {
                    item.ProductId = incomeItem.ProductCapitalizationItem.ProductId;
                    item.Price = incomeItem.ProductCapitalizationItem.UnitPrice;
                    item.Weight = incomeItem.ProductCapitalizationItem.Weight;
                    item.NetPrice = incomeItem.ProductCapitalizationItem.UnitPrice;
                    item.AccountingPrice = incomeItem.ProductCapitalizationItem.UnitPrice;

                    itemMovement.MovementType = ConsignmentItemMovementType.Capitalization;
                }

                if (incomeItem.ActReconciliationItem?.SupplyOrderUkraineItem != null) {
                    item.ProductId = incomeItem.ActReconciliationItem.SupplyOrderUkraineItem.ProductId;
                    item.Price = incomeItem.ActReconciliationItem.SupplyOrderUkraineItem.GrossUnitPrice;
                    item.Weight = incomeItem.ActReconciliationItem.SupplyOrderUkraineItem.GrossWeight;
                    item.NetPrice = incomeItem.ActReconciliationItem.SupplyOrderUkraineItem.UnitPrice;

                    itemMovement.MovementType = ConsignmentItemMovementType.UkraineOrder;
                }

                if (incomeItem.ActReconciliationItem?.SupplyInvoiceOrderItem != null) {
                    item.ProductId = incomeItem.ActReconciliationItem.SupplyInvoiceOrderItem.ProductId;
                    item.Price = incomeItem.ActReconciliationItem.SupplyInvoiceOrderItem.GrossUnitPrice;
                    item.AccountingPrice = incomeItem.ActReconciliationItem.SupplyInvoiceOrderItem.GrossUnitPrice;
                    item.Weight = incomeItem.ActReconciliationItem.SupplyInvoiceOrderItem.Weight;
                    item.NetPrice = incomeItem.ActReconciliationItem.SupplyInvoiceOrderItem.UnitPrice;

                    itemMovement.MovementType = ConsignmentItemMovementType.Income;
                }

                if (item.ProductId == 0) continue;

                if (item.ProductSpecification.IsNew()) {
                    item.ProductSpecification.ProductId = item.ProductId;
                    item.ProductSpecification.Locale = income.Organization.Culture;
                    item.ProductSpecification.AddedById = _userRepositoriesFactory.NewUserRepository(connection).GetManagerOrGBAIdByClientNetId(Guid.Empty);
                }

                specificationRepository.SetInactiveByProductId(item.ProductId, CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower());

                item.ProductSpecification.IsActive = true;

                item.ProductSpecificationId = specificationRepository.Add(item.ProductSpecification);

                itemMovement.ConsignmentItemId = consignmentItemRepository.Add(item);

                consignmentItemMovementRepository.Add(itemMovement);

                productPlacementRepository
                    .ReAssignProductPlacementFromProductIncomeItemToConsignmentItemByIds(
                        incomeItem.Id,
                        itemMovement.ConsignmentItemId
                    );
            }
        }
    }

    private void ProcessAddNewConsignmentFromProductTransferMessage(AddNewConsignmentFromProductTransferMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ProductTransfer transfer =
            _productRepositoriesFactory
                .NewProductTransferRepository(connection)
                .GetByIdForConsignmentCreation(
                    message.ProductTransferId
                );

        if (transfer == null) return;

        IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
        IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
        IConsignmentRepository consignmentRepository = _consignmentRepositoriesFactory.NewConsignmentRepository(connection);
        IProductLocationRepository productLocationRepository = _productRepositoriesFactory.NewProductLocationRepository(connection);
        IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
        IConsignmentItemRepository consignmentItemRepository = _consignmentRepositoriesFactory.NewConsignmentItemRepository(connection);
        IProductWriteOffRuleRepository productWriteOffRuleRepository = _productRepositoriesFactory.NewProductWriteOffRuleRepository(connection);
        IConsignmentItemMovementRepository consignmentItemMovementRepository = _consignmentRepositoriesFactory.NewConsignmentItemMovementRepository(connection);

        foreach (ProductTransferItem transferItem in transfer.ProductTransferItems) {
            Product product =
                getSingleProductRepository
                    .GetByIdAndRuleLocaleWithProductGroupAndWriteOffRules(
                        transferItem.ProductId,
                        transfer.Organization.Culture
                    );

            if (product == null) continue;

            ProductWriteOffRule writeOffRule =
                GetCurrentWriteOffRuleForProduct(
                    product,
                    transfer.Organization.Culture,
                    userRepository,
                    productWriteOffRuleRepository
                );

            IEnumerable<ConsignmentItem> consignmentItems =
                consignmentItemRepository
                    .GetAllAvailable(
                        transfer.OrganizationId,
                        transfer.FromStorageId,
                        transferItem.ProductId,
                        writeOffRule.RuleType
                    );

            if (!consignmentItems.Any()) continue;

            foreach (ConsignmentItem consignmentItem in consignmentItems) {
                Consignment consignment = new Consignment {
                    IsVirtual = true,
                    FromDate = transfer.FromDate,
                    StorageId = transfer.ToStorageId,
                    OrganizationId = transfer.OrganizationId,
                    ProductIncomeId = consignmentItem.Consignment.ProductIncomeId,
                    ProductTransferId = transfer.Id,
                    IsImportedFromOneC = consignmentItem.Consignment.IsImportedFromOneC
                };

                Consignment existingConsignment = consignmentRepository.GetIfExistsByConsignmentParams(consignment);

                consignment.Id = existingConsignment?.Id ?? consignmentRepository.Add(consignment);

                double currentOperationQty = transferItem.Qty;

                if (consignmentItem.RemainingQty < transferItem.Qty)
                    currentOperationQty = consignmentItem.RemainingQty;

                consignmentItem.RemainingQty -= currentOperationQty;

                consignmentItemRepository.UpdateRemainingQty(consignmentItem);

                ConsignmentItem createdItem = new ConsignmentItem {
                    ConsignmentId = consignment.Id,
                    Qty = currentOperationQty,
                    RemainingQty = currentOperationQty,
                    Weight = consignmentItem.Weight,
                    Price = consignmentItem.Price,
                    NetPrice = consignmentItem.NetPrice,
                    AccountingPrice = consignmentItem.AccountingPrice,
                    DutyPercent = consignmentItem.DutyPercent,
                    ProductId = consignmentItem.ProductId,
                    RootConsignmentItemId = consignmentItem.Id,
                    ProductIncomeItemId = consignmentItem.ProductIncomeItemId,
                    ProductSpecificationId = consignmentItem.ProductSpecificationId,
                    ExchangeRate = consignmentItem.ExchangeRate
                };

                createdItem.Id = consignmentItemRepository.Add(createdItem);

                consignmentItemMovementRepository.Add(new[] {
                    new ConsignmentItemMovement {
                        IsIncomeMovement = true,
                        Qty = currentOperationQty,
                        MovementType = ConsignmentItemMovementType.ProductTransfer,
                        ConsignmentItemId = createdItem.Id,
                        ProductTransferItemId = transferItem.Id
                    },
                    new ConsignmentItemMovement {
                        IsIncomeMovement = false,
                        Qty = currentOperationQty,
                        MovementType = ConsignmentItemMovementType.ProductTransfer,
                        ConsignmentItemId = createdItem.Id,
                        ProductTransferItemId = transferItem.Id
                    }
                });

                transferItem.Qty -= currentOperationQty;

                IEnumerable<ProductPlacement> placements =
                    productPlacementRepository
                        .GetAllByConsignmentItemId(
                            consignmentItem.Id
                        );

                double toMoveQty = currentOperationQty;

                foreach (ProductPlacement placement in placements) {
                    double operationQty = toMoveQty;

                    if (placement.Qty < operationQty)
                        operationQty = placement.Qty;

                    placement.Qty -= operationQty;

                    if (placement.Qty > 0)
                        productPlacementRepository.UpdateQty(placement);
                    else
                        productPlacementRepository.Remove(placement);

                    productLocationRepository.Add(new ProductLocation {
                        StorageId = transfer.FromStorageId,
                        Qty = operationQty,
                        ProductPlacementId = placement.Id,
                        ProductTransferItemId = transferItem.Id
                    });

                    toMoveQty -= operationQty;

                    if (!transfer.ToStorage.ForDefective) {
                        ProductPlacement toPlacement =
                            productPlacementRepository
                                .GetIfExists(
                                    "N",
                                    "N",
                                    "N",
                                    placement.ProductId,
                                    transfer.ToStorageId,
                                    null,
                                    createdItem.Id
                                ) ?? new ProductPlacement {
                                RowNumber = "N",
                                CellNumber = "N",
                                StorageNumber = "N",
                                StorageId = transfer.ToStorageId,
                                ProductId = placement.ProductId,
                                Qty = operationQty,
                                ConsignmentItemId = createdItem.Id
                            };

                        if (toPlacement.IsNew())
                            toPlacement.Id = productPlacementRepository.AddWithId(toPlacement);
                        else {
                            toPlacement.Qty += operationQty;

                            productPlacementRepository.UpdateQty(toPlacement);
                        }
                    }

                    if (toMoveQty.Equals(0d)) break;
                }

                if (transferItem.Qty.Equals(0d)) break;
            }
        }
    }

    private void ProcessStoreConsignmentFromProductTransferWithReSale(StoreConsignmentFromProductTransferWithReSaleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ProductTransfer transfer =
            _productRepositoriesFactory
                .NewProductTransferRepository(connection)
                .GetByIdForConsignmentCreation(
                    message.ProductTransferId
                );

        if (transfer == null) return;

        IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
        IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
        IConsignmentRepository consignmentRepository = _consignmentRepositoriesFactory.NewConsignmentRepository(connection);
        IProductLocationRepository productLocationRepository = _productRepositoriesFactory.NewProductLocationRepository(connection);
        IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
        IConsignmentItemRepository consignmentItemRepository = _consignmentRepositoriesFactory.NewConsignmentItemRepository(connection);
        IProductWriteOffRuleRepository productWriteOffRuleRepository = _productRepositoriesFactory.NewProductWriteOffRuleRepository(connection);
        IConsignmentItemMovementRepository consignmentItemMovementRepository = _consignmentRepositoriesFactory.NewConsignmentItemMovementRepository(connection);
        IReSaleAvailabilityRepository reSaleAvailabilityRepository = _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection);
        IProductPlacementHistoryRepository productPlacementHistoryRepository = _productRepositoriesFactory.NewProductPlacementHistoryRepository(connection);

        foreach (ProductTransferItem transferItem in transfer.ProductTransferItems) {
            Product product =
                getSingleProductRepository
                    .GetByIdAndRuleLocaleWithProductGroupAndWriteOffRules(
                        transferItem.ProductId,
                        transfer.Organization.Culture
                    );

            if (product == null) continue;

            ProductWriteOffRule writeOffRule =
                GetCurrentWriteOffRuleForProduct(
                    product,
                    transfer.Organization.Culture,
                    userRepository,
                    productWriteOffRuleRepository
                );

            //TODO OrganizationId Nullable
            if (!transfer.FromStorage.OrganizationId.HasValue) continue;

            IEnumerable<ConsignmentItem> consignmentItems =
                consignmentItemRepository
                    .GetAllAvailable(
                        transfer.FromStorage.OrganizationId.Value,
                        transfer.FromStorageId,
                        transferItem.ProductId,
                        writeOffRule.RuleType
                    );

            if (!consignmentItems.Any()) continue;

            double currentOperationQty = transferItem.Qty;

            foreach (ConsignmentItem consignmentItem in consignmentItems) {
                double remaining = consignmentItem.RemainingQty;

                Consignment consignment = new Consignment {
                    IsVirtual = true,
                    FromDate = transfer.FromDate,
                    StorageId = transfer.ToStorageId,
                    OrganizationId = transfer.ToStorage.OrganizationId ?? transfer.OrganizationId,
                    ProductIncomeId = consignmentItem.Consignment.ProductIncomeId,
                    ProductTransferId = transfer.Id,
                    IsImportedFromOneC = consignmentItem.Consignment.IsImportedFromOneC
                };

                Consignment existingConsignment = consignmentRepository.GetIfExistsByConsignmentParams(consignment);

                consignment.Id = existingConsignment?.Id ?? consignmentRepository.Add(consignment);

                double reSaleAvailabilityQty;

                if (consignmentItem.RemainingQty < currentOperationQty) {
                    currentOperationQty -= consignmentItem.RemainingQty;
                    reSaleAvailabilityQty = consignmentItem.RemainingQty;
                    consignmentItem.RemainingQty = 0;
                } else {
                    reSaleAvailabilityQty = currentOperationQty;
                    consignmentItem.RemainingQty -= currentOperationQty;
                    currentOperationQty = 0;
                }

                consignmentItemRepository.UpdateRemainingQty(consignmentItem);

                double consignmentQty = remaining - consignmentItem.RemainingQty;

                ConsignmentItem createdItem = new ConsignmentItem {
                    ConsignmentId = consignment.Id,
                    Qty = consignmentQty,
                    RemainingQty = consignmentQty,
                    Weight = consignmentItem.Weight,
                    Price = consignmentItem.Price,
                    NetPrice = consignmentItem.NetPrice,
                    AccountingPrice = consignmentItem.AccountingPrice,
                    DutyPercent = consignmentItem.DutyPercent,
                    ProductId = consignmentItem.ProductId,
                    RootConsignmentItemId = consignmentItem.Id,
                    ProductIncomeItemId = consignmentItem.ProductIncomeItemId,
                    ProductSpecificationId = consignmentItem.ProductSpecificationId,
                    ExchangeRate = consignmentItem.ExchangeRate
                };

                createdItem.Id = consignmentItemRepository.Add(createdItem);

                consignmentItemMovementRepository.Add(new[] {
                    new ConsignmentItemMovement {
                        IsIncomeMovement = true,
                        Qty = reSaleAvailabilityQty,
                        MovementType = ConsignmentItemMovementType.ProductTransfer,
                        ConsignmentItemId = createdItem.Id,
                        ProductTransferItemId = transferItem.Id
                    },
                    new ConsignmentItemMovement {
                        IsIncomeMovement = false,
                        Qty = reSaleAvailabilityQty,
                        MovementType = ConsignmentItemMovementType.ProductTransfer,
                        ConsignmentItemId = createdItem.Id,
                        ProductTransferItemId = transferItem.Id
                    }
                });

                // transferItem.Qty -= currentOperationQty;
                IEnumerable<ProductPlacement> placements =
                    productPlacementRepository.GetAllByProductAndStorageIds(
                        consignmentItem.ProductId,
                        transfer.FromStorageId);


                double toMoveQty = remaining - consignmentItem.RemainingQty;

                foreach (ProductPlacement placement in placements) {
                    double operationQty = toMoveQty;

                    if (placement.Qty < operationQty)
                        operationQty = placement.Qty;

                    placement.Qty -= operationQty;

                    if (placement.Qty > 0)
                        productPlacementRepository.UpdateQty(placement);
                    else
                        productPlacementRepository.Remove(placement);

                    productLocationRepository.Add(new ProductLocation {
                        StorageId = transfer.FromStorageId,
                        Qty = operationQty,
                        ProductPlacementId = placement.Id,
                        ProductTransferItemId = transferItem.Id
                    });

                    toMoveQty -= operationQty;

                    if (!transfer.ToStorage.ForDefective) {
                        ProductPlacement toPlacement = null;
                        if (!message.IsFile) {
                            toPlacement =
                                productPlacementRepository
                                    .GetIfExists(
                                        message.RowNumber ?? placement.RowNumber,
                                        message.CellNumber ?? placement.CellNumber,
                                        message.StorageNumber ?? placement.StorageNumber,
                                        placement.ProductId,
                                        transfer.ToStorageId,
                                        null,
                                        createdItem.Id
                                    ) ?? new ProductPlacement {
                                    RowNumber = message.RowNumber ?? placement.RowNumber,
                                    CellNumber = message.CellNumber ?? placement.CellNumber,
                                    StorageNumber = message.StorageNumber ?? placement.StorageNumber,
                                    StorageId = transfer.ToStorageId,
                                    ProductId = placement.ProductId,
                                    Qty = operationQty,
                                    ConsignmentItemId = createdItem.Id
                                };
                        } else {
                            toPlacement =
                                productPlacementRepository
                                    .GetIfExists(
                                        placement.RowNumber,
                                        placement.CellNumber,
                                        placement.StorageNumber,
                                        placement.ProductId,
                                        transfer.ToStorageId,
                                        null,
                                        createdItem.Id
                                    ) ?? new ProductPlacement {
                                    RowNumber = placement.RowNumber,
                                    CellNumber = placement.CellNumber,
                                    StorageNumber = placement.StorageNumber,
                                    StorageId = transfer.ToStorageId,
                                    ProductId = placement.ProductId,
                                    Qty = operationQty,
                                    ConsignmentItemId = createdItem.Id
                                };
                        }

                        productPlacementHistoryRepository.Add(new ProductPlacementHistory {
                            Placement = toPlacement.StorageNumber + "-" + toPlacement.RowNumber + "-" + toPlacement.CellNumber,
                            StorageId = toPlacement.StorageId,
                            ProductId = toPlacement.ProductId,
                            Qty = toPlacement.Qty,
                            StorageLocationType = StorageLocationType.Movement,
                            AdditionType = AdditionType.Add,
                            UserId = message.UserId
                        });

                        if (toPlacement.IsNew())
                            toPlacement.Id = productPlacementRepository.AddWithId(toPlacement);
                        else {
                            toPlacement.Qty += operationQty;

                            productPlacementRepository.UpdateQty(toPlacement);
                        }
                    }

                    if (toMoveQty.Equals(0d)) break;
                }

                if (message.WithReSale) {
                    long productAvailabilityId = message.ProductTransferItemProductAvailability.FirstOrDefault(x => x.Key.Equals(transferItem.Id)).Value;

                    decimal accountPrice = consignmentItemRepository.GetPriceForReSaleByConsignmentItemId(consignmentItem.Id);

                    reSaleAvailabilityRepository.Add(new ReSaleAvailability {
                        ProductTransferItemId = transferItem.Id,
                        ConsignmentItemId = consignmentItem.Id,
                        ProductAvailabilityId = productAvailabilityId,
                        ExchangeRate = accountPrice / (consignmentItem.AccountingPrice.Equals(0) ? consignmentItem.Price : consignmentItem.AccountingPrice),
                        PricePerItem = accountPrice,
                        Qty = reSaleAvailabilityQty,
                        RemainingQty = reSaleAvailabilityQty
                    });
                }

                if (currentOperationQty.Equals(0d)) break;
            }
        }
    }

    private void ProcessStoreConsignmentMovementFromDepreciatedOrderMessage(StoreConsignmentMovementFromDepreciatedOrderMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        DepreciatedOrder depreciatedOrder =
            _depreciatedRepositoriesFactory
                .NewDepreciatedOrderRepository(connection)
                .GetByIdForConsignment(
                    message.DepreciatedOrderId
                );

        if (depreciatedOrder == null) return;

        IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
        IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
        IProductLocationRepository productLocationRepository = _productRepositoriesFactory.NewProductLocationRepository(connection);
        IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
        IConsignmentItemRepository consignmentItemRepository = _consignmentRepositoriesFactory.NewConsignmentItemRepository(connection);
        IProductWriteOffRuleRepository productWriteOffRuleRepository = _productRepositoriesFactory.NewProductWriteOffRuleRepository(connection);
        IConsignmentItemMovementRepository consignmentItemMovementRepository = _consignmentRepositoriesFactory.NewConsignmentItemMovementRepository(connection);

        foreach (DepreciatedOrderItem depreciatedOrderItem in depreciatedOrder.DepreciatedOrderItems) {
            Product product =
                getSingleProductRepository
                    .GetByIdAndRuleLocaleWithProductGroupAndWriteOffRules(
                        depreciatedOrderItem.ProductId,
                        depreciatedOrder.Organization.Culture
                    );

            if (product == null) continue;

            ProductWriteOffRule writeOffRule =
                GetCurrentWriteOffRuleForProduct(
                    product,
                    depreciatedOrder.Organization.Culture,
                    userRepository,
                    productWriteOffRuleRepository
                );

            IEnumerable<ConsignmentItem> consignmentItems =
                consignmentItemRepository
                    .GetAllAvailable(
                        depreciatedOrder.OrganizationId,
                        depreciatedOrder.StorageId,
                        depreciatedOrderItem.ProductId,
                        writeOffRule.RuleType
                    );

            if (!consignmentItems.Any()) continue;

            foreach (ConsignmentItem consignmentItem in consignmentItems) {
                double currentOperationQty = depreciatedOrderItem.Qty;

                if (consignmentItem.RemainingQty < depreciatedOrderItem.Qty)
                    currentOperationQty = consignmentItem.RemainingQty;

                consignmentItem.RemainingQty -= currentOperationQty;

                consignmentItemRepository.UpdateRemainingQty(consignmentItem);

                consignmentItemMovementRepository.Add(
                    new ConsignmentItemMovement {
                        IsIncomeMovement = false,
                        Qty = currentOperationQty,
                        MovementType = ConsignmentItemMovementType.DepreciatedOrder,
                        ConsignmentItemId = consignmentItem.Id,
                        DepreciatedOrderItemId = depreciatedOrderItem.Id
                    });

                depreciatedOrderItem.Qty -= currentOperationQty;

                IEnumerable<ProductPlacement> placements =
                    productPlacementRepository
                        .GetAllByConsignmentItemId(
                            consignmentItem.Id
                        );

                double toMoveQty = currentOperationQty;

                foreach (ProductPlacement placement in placements) {
                    double operationQty = toMoveQty;

                    if (placement.Qty < operationQty)
                        operationQty = placement.Qty;

                    placement.Qty -= operationQty;

                    if (placement.Qty > 0)
                        productPlacementRepository.UpdateQty(placement);
                    else
                        productPlacementRepository.Remove(placement);

                    productLocationRepository.Add(new ProductLocation {
                        StorageId = depreciatedOrder.StorageId,
                        Qty = operationQty,
                        ProductPlacementId = placement.Id,
                        DepreciatedOrderItemId = depreciatedOrderItem.Id
                    });

                    toMoveQty -= operationQty;

                    if (toMoveQty.Equals(0d)) break;
                }

                if (depreciatedOrderItem.Qty.Equals(0d)) break;
            }
        }
    }

    private void ProcessStoreConsignmentFromDepreciatedOrderWithReSale(StoreConsignmentFromDepreciatedOrderWithReSaleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        DepreciatedOrder depreciatedOrder =
            _depreciatedRepositoriesFactory
                .NewDepreciatedOrderRepository(connection)
                .GetByIdForConsignment(
                    message.DepreciatedOrderId
                );

        if (depreciatedOrder == null) return;

        IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
        IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
        IProductLocationRepository productLocationRepository = _productRepositoriesFactory.NewProductLocationRepository(connection);
        IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
        IConsignmentItemRepository consignmentItemRepository = _consignmentRepositoriesFactory.NewConsignmentItemRepository(connection);
        IProductWriteOffRuleRepository productWriteOffRuleRepository = _productRepositoriesFactory.NewProductWriteOffRuleRepository(connection);
        IConsignmentItemMovementRepository consignmentItemMovementRepository = _consignmentRepositoriesFactory.NewConsignmentItemMovementRepository(connection);
        IReSaleAvailabilityRepository reSaleAvailabilityRepository = _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection);

        foreach (DepreciatedOrderItem depreciatedOrderItem in depreciatedOrder.DepreciatedOrderItems) {
            Product product =
                getSingleProductRepository
                    .GetByIdAndRuleLocaleWithProductGroupAndWriteOffRules(
                        depreciatedOrderItem.ProductId,
                        depreciatedOrder.Organization.Culture
                    );

            if (product == null) continue;

            ProductWriteOffRule writeOffRule =
                GetCurrentWriteOffRuleForProduct(
                    product,
                    depreciatedOrder.Organization.Culture,
                    userRepository,
                    productWriteOffRuleRepository
                );

            IEnumerable<ConsignmentItem> consignmentItems =
                consignmentItemRepository
                    .GetAllAvailable(
                        depreciatedOrder.OrganizationId,
                        depreciatedOrder.StorageId,
                        depreciatedOrderItem.ProductId,
                        writeOffRule.RuleType
                    );

            if (!consignmentItems.Any()) continue;

            IEnumerable<ProductPlacement> placements = productPlacementRepository.GetAllByProductAndStorageIds(
                depreciatedOrderItem.ProductId,
                depreciatedOrder.StorageId);

            double toMoveQty = depreciatedOrderItem.Qty;

            foreach (ProductPlacement placement in placements) {
                double operationQty = toMoveQty;

                if (placement.Qty < operationQty)
                    operationQty = placement.Qty;

                placement.Qty -= operationQty;

                if (placement.Qty > 0)
                    productPlacementRepository.UpdateQty(placement);
                else
                    productPlacementRepository.Remove(placement);

                productLocationRepository.Add(new ProductLocation {
                    StorageId = depreciatedOrder.StorageId,
                    Qty = operationQty,
                    ProductPlacementId = placement.Id,
                    DepreciatedOrderItemId = depreciatedOrderItem.Id
                });

                toMoveQty -= operationQty;
            }

            foreach (ConsignmentItem consignmentItem in consignmentItems) {
                double currentOperationQty = depreciatedOrderItem.Qty;
                double reSaleAvailabilityQty = consignmentItem.RemainingQty < depreciatedOrderItem.Qty ? consignmentItem.RemainingQty : currentOperationQty;

                if (consignmentItem.RemainingQty < depreciatedOrderItem.Qty) {
                    currentOperationQty -= consignmentItem.RemainingQty;
                    depreciatedOrderItem.Qty -= consignmentItem.RemainingQty;
                    consignmentItem.RemainingQty = 0;
                } else {
                    consignmentItem.RemainingQty -= currentOperationQty;
                    depreciatedOrderItem.Qty -= currentOperationQty;
                }

                consignmentItemRepository.UpdateRemainingQty(consignmentItem);

                consignmentItemMovementRepository.Add(
                    new ConsignmentItemMovement {
                        IsIncomeMovement = false,
                        Qty = reSaleAvailabilityQty,
                        MovementType = ConsignmentItemMovementType.DepreciatedOrder,
                        ConsignmentItemId = consignmentItem.Id,
                        DepreciatedOrderItemId = depreciatedOrderItem.Id
                    });

                if (message.WithReSale) {
                    long productAvailabilityId = message.DepreciatedOrderProductAvailabilityIds.FirstOrDefault(x => x.Key.Equals(depreciatedOrderItem.Id)).Value;

                    decimal accountPrice = consignmentItemRepository.GetPriceForReSaleByConsignmentItemId(consignmentItem.Id);

                    reSaleAvailabilityRepository.Add(new ReSaleAvailability {
                        DepreciatedOrderItemId = depreciatedOrderItem.Id,
                        ConsignmentItemId = consignmentItem.Id,
                        ProductAvailabilityId = productAvailabilityId,
                        ExchangeRate = accountPrice / (consignmentItem.AccountingPrice.Equals(0) ? consignmentItem.NetPrice : consignmentItem.AccountingPrice),
                        PricePerItem = accountPrice,
                        Qty = reSaleAvailabilityQty,
                        RemainingQty = reSaleAvailabilityQty
                    });
                }

                if (depreciatedOrderItem.Qty.Equals(0d)) break;
            }
        }
    }

    private void ProcessStoreConsignmentMovementFromSadMessage(StoreConsignmentMovementFromSadMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sad sad =
            _supplyUkraineRepositoriesFactory
                .NewSadRepository(connection, null)
                .GetByIdForConsignment(
                    message.SadId
                );

        if (sad?.Organization == null) return;

        IConsignmentItemMovementRepository consignmentItemMovementRepository = _consignmentRepositoriesFactory.NewConsignmentItemMovementRepository(connection);

        foreach (SadItem sadItem in sad.SadItems.Where(i => i.SupplyOrderUkraineCartItem != null)) {
            foreach (SupplyOrderUkraineCartItemReservation reservation in sadItem.SupplyOrderUkraineCartItem.SupplyOrderUkraineCartItemReservations) {
                if (reservation.ConsignmentItemId != null || sadItem.SupplyOrderUkraineCartItemId != null)
                    consignmentItemMovementRepository.Add(
                        new ConsignmentItemMovement {
                            IsIncomeMovement = false,
                            Qty = reservation.Qty,
                            MovementType = ConsignmentItemMovementType.Export,
                            ConsignmentItemId = reservation.ConsignmentItemId ?? sadItem.SupplyOrderUkraineCartItemId ?? 0,
                            SadItemId = sadItem.Id
                        });
            }
        }
    }

    private void ProcessStoreConsignmentMovementFromSadFromSaleMessage(StoreConsignmentMovementFromSadFromSaleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sad sad =
            _supplyUkraineRepositoriesFactory
                .NewSadRepository(connection, null)
                .GetByIdForConsignmentFromSale(
                    message.SadId
                );

        if (sad?.Organization == null) return;

        IConsignmentItemMovementRepository consignmentItemMovementRepository = _consignmentRepositoriesFactory.NewConsignmentItemMovementRepository(connection);

        foreach (SadItem sadItem in sad.SadItems.Where(i => i.ConsignmentItemId != null)) {
            consignmentItemMovementRepository.Add(
                new ConsignmentItemMovement {
                    IsIncomeMovement = false,
                    Qty = sadItem.Qty,
                    MovementType = ConsignmentItemMovementType.Export,
                    ConsignmentItemId = sadItem.ConsignmentItemId ?? 0,
                    SadItemId = sadItem.Id
                });
        }
    }

    private void ProcessStoreConsignmentMovementFromTaxFreePackListMessage(StoreConsignmentMovementFromTaxFreePackListMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        TaxFreePackList taxFreePackList =
            _supplyUkraineRepositoriesFactory
                .NewTaxFreePackListRepository(connection, null)
                .GetByIdForConsignmentMovement(
                    message.TaxFreePackListId
                );

        if (taxFreePackList == null) return;

        IConsignmentItemMovementRepository consignmentItemMovementRepository = _consignmentRepositoriesFactory.NewConsignmentItemMovementRepository(connection);

        foreach (TaxFree taxFree in taxFreePackList.TaxFrees)
        foreach (TaxFreeItem taxFreeItem in taxFree.TaxFreeItems.Where(i => i.SupplyOrderUkraineCartItem != null))
        foreach (SupplyOrderUkraineCartItemReservation reservation in taxFreeItem
                     .SupplyOrderUkraineCartItem
                     .SupplyOrderUkraineCartItemReservations
                     .Where(r => r.ConsignmentItemId != null))
            consignmentItemMovementRepository.Add(
                new ConsignmentItemMovement {
                    IsIncomeMovement = false,
                    Qty = taxFreeItem.Qty,
                    MovementType = ConsignmentItemMovementType.TaxFree,
                    ConsignmentItemId = reservation.ConsignmentItemId ?? 0,
                    TaxFreeItemId = taxFreeItem.Id
                });
    }

    private void ProcessStoreConsignmentMovementFromTaxFreePackListFromSaleMessage(StoreConsignmentMovementFromTaxFreePackListFromSaleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        TaxFreePackList taxFreePackList =
            _supplyUkraineRepositoriesFactory
                .NewTaxFreePackListRepository(connection, null)
                .GetByIdForConsignmentMovementFromSale(
                    message.TaxFreePackListId
                );

        if (taxFreePackList == null) return;

        IConsignmentItemMovementRepository consignmentItemMovementRepository = _consignmentRepositoriesFactory.NewConsignmentItemMovementRepository(connection);

        foreach (TaxFree taxFree in taxFreePackList.TaxFrees)
        foreach (TaxFreeItem taxFreeItem in taxFree.TaxFreeItems.Where(i => i.TaxFreePackListOrderItem?.ConsignmentItemId != null))
            consignmentItemMovementRepository.Add(
                new ConsignmentItemMovement {
                    IsIncomeMovement = false,
                    Qty = taxFreeItem.Qty,
                    MovementType = ConsignmentItemMovementType.TaxFree,
                    ConsignmentItemId = taxFreeItem.TaxFreePackListOrderItem.ConsignmentItemId ?? 0,
                    TaxFreeItemId = taxFreeItem.Id
                });
    }

    private void ProcessStoreConsignmentMovementFromSaleMessage(StoreConsignmentMovementFromSaleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sale sale =
            _saleRepositoriesFactory
                .NewSaleRepository(connection)
                .GetByIdForConsignment(
                    message.SaleId
                );

        if (sale?.ClientAgreement?.Agreement?.Organization == null) {
            ActorReferenceManager.Instance.Get(SalesActorNames.BASE_SALES_GET_ACTOR).Tell(
                new GetSaleStatisticWithResourceNameByNetIdMessage(
                    Guid.Empty,
                    message.ResponseMessage,
                    true,
                    false,
                    true
                ),
                (IActorRef)message.OriginalSender
            );

            return;
        }

        IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
        IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
        IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
        IReSaleAvailabilityRepository reSaleAvailabilityRepository = _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection);
        IProductLocationRepository productLocationRepository = _productRepositoriesFactory.NewProductLocationRepository(connection);
        IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
        IConsignmentItemRepository consignmentItemRepository = _consignmentRepositoriesFactory.NewConsignmentItemRepository(connection);
        IProductSpecificationRepository specificationRepository = _productRepositoriesFactory.NewProductSpecificationRepository(connection);
        IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
        IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
        IProductWriteOffRuleRepository productWriteOffRuleRepository = _productRepositoriesFactory.NewProductWriteOffRuleRepository(connection);
        IConsignmentItemMovementRepository consignmentItemMovementRepository = _consignmentRepositoriesFactory.NewConsignmentItemMovementRepository(connection);

        foreach (OrderItem orderItem in sale.Order.OrderItems) {
            Product product =
                getSingleProductRepository
                    .GetByIdAndRuleLocaleWithProductGroupAndWriteOffRules(
                        orderItem.ProductId,
                        sale.ClientAgreement.Agreement.Organization.Culture
                    );

            if (product == null) continue;

            ProductWriteOffRule writeOffRule =
                GetCurrentWriteOffRuleForProduct(
                    product,
                    sale.ClientAgreement.Agreement.Organization.Culture,
                    userRepository,
                    productWriteOffRuleRepository
                );

            IEnumerable<ConsignmentItem> consignmentItems =
                consignmentItemRepository
                    .GetAllAvailable(
                        sale.ClientAgreement.Agreement.Organization.Id,
                        orderItem.ProductId,
                        writeOffRule.RuleType,
                        sale.IsVatSale,
                        storageId: sale.ClientAgreement.Agreement.Organization.StorageId
                    );

            if (!consignmentItems.Any()) continue;

            if (orderItem.ProductReservations.Any()) {
                foreach (ProductReservation reservation in orderItem.ProductReservations) {
                    reservation.ProductAvailability.Amount += reservation.Qty;

                    productAvailabilityRepository.Update(reservation.ProductAvailability);

                    productReservationRepository.Delete(reservation.NetUid);
                }
            }

            double qtyOrderItem = orderItem.Qty;

            double qtyToReSaleAvailabilities = 0;

            foreach (ConsignmentItem consignmentItem in consignmentItems) {
                if (qtyOrderItem.Equals(0))
                    break;

                if (consignmentItem.RemainingQty < qtyOrderItem)
                    orderItemRepository.Remove(orderItem.NetUid);

                double remainingConsignmentItem = consignmentItem.RemainingQty;
                long orderItemId = orderItem.Id;

                // Absolutely dumb shit, removing it fixed consignment movement issue 1297
                // if (consignmentItem.Consignment.Storage.ForVatProducts && !sale.ParentNetId.HasValue) {
                //     orderItemRepository.Remove(orderItem.NetUid);
                //
                //     IEnumerable<ReSaleAvailability> reSaleAvailabilities =
                //         reSaleAvailabilityRepository.GetByProductAndStorageId(
                //             orderItem.ProductId,
                //             consignmentItem.Consignment.StorageId);

                //     (qtyOrderItem, qtyToReSaleAvailabilities, orderItemId) = StoreConsignmentItemFromReSaleAvailabilities(
                //         reSaleAvailabilities,
                //         consignmentItem,
                //         orderItem,
                //         qtyOrderItem,
                //         sale.ClientAgreement.NetUid,
                //         consignmentItemRepository,
                //         reSaleAvailabilityRepository,
                //         orderItemRepository,
                //         specificationRepository,
                //         consignmentItemMovementRepository,
                //         productReservationRepository);
                // }

                if (qtyOrderItem > 0 && consignmentItem.RemainingQty > 0) {
                    double qtyToMovement;

                    if (qtyOrderItem > consignmentItem.RemainingQty) {
                        qtyOrderItem -= consignmentItem.RemainingQty;
                        qtyToMovement = consignmentItem.RemainingQty;
                        consignmentItem.RemainingQty = 0;
                    } else {
                        consignmentItem.RemainingQty -= qtyOrderItem;
                        qtyToMovement = qtyOrderItem;
                        qtyOrderItem = 0;
                    }

                    consignmentItemRepository.UpdateRemainingQty(consignmentItem);

                    OrderItem newItem = new OrderItem {
                        ProductId = orderItem.ProductId,
                        Comment = orderItem.Comment,
                        Discount = orderItem.Discount,
                        DiscountAmount = orderItem.DiscountAmount,
                        PricePerItem = orderItem.PricePerItem,
                        Qty = qtyToMovement,
                        ChangedQty = orderItem.ChangedQty,
                        OrderedQty = orderItem.OrderedQty,
                        OrderId = orderItem.OrderId,
                        ReturnedQty = 0d,
                        TotalAmount = orderItem.TotalAmount,
                        TotalAmountLocal = orderItem.TotalAmountLocal,
                        TotalWeight = orderItem.TotalWeight,
                        UserId = orderItem.UserId,
                        DiscountUpdatedById = orderItem.DiscountUpdatedById,
                        ExchangeRateAmount = orderItem.ExchangeRateAmount,
                        OneTimeDiscountComment = orderItem.OneTimeDiscountComment,
                        OfferProcessingStatusChangedById = orderItem.OfferProcessingStatusChangedById,
                        UnpackedQty = orderItem.UnpackedQty,
                        FromOfferQty = orderItem.FromOfferQty,
                        IsFromOffer = orderItem.IsFromOffer,
                        OfferProcessingStatus = orderItem.OfferProcessingStatus,
                        OneTimeDiscount = orderItem.OneTimeDiscount,
                        IsValidForCurrentSale = orderItem.IsValidForCurrentSale,
                        PricePerItemWithoutVat = orderItem.PricePerItemWithoutVat,
                        IsFromReSale = consignmentItem.IsReSaleAvailability,
                        Vat = orderItem.Vat
                    };

                    OrderItem existingItem =
                        orderItemRepository
                            .GetOrderItemByOrderProductAndSpecification(
                                newItem.OrderId ?? 0,
                                newItem.ProductId,
                                consignmentItem.ProductSpecification,
                                newItem.IsFromReSale
                            );

                    if (existingItem != null) {
                        if (!existingItem.Id.Equals(orderItem.Id))
                            existingItem.Qty += qtyToMovement;

                        orderItemId = existingItem.Id;

                        orderItemRepository.UpdateQty(existingItem);
                    } else {
                        newItem.AssignedSpecificationId =
                            specificationRepository
                                .Add(new ProductSpecification {
                                    Name = consignmentItem.ProductSpecification.Name,
                                    Locale = consignmentItem.ProductSpecification.Locale,
                                    AddedById = consignmentItem.ProductSpecification.AddedById,
                                    ProductId = consignmentItem.ProductSpecification.ProductId,
                                    VATValue = consignmentItem.ProductSpecification.VATValue,
                                    CustomsValue = consignmentItem.ProductSpecification.CustomsValue,
                                    Duty = consignmentItem.ProductSpecification.Duty,
                                    VATPercent = consignmentItem.ProductSpecification.VATPercent,
                                    SpecificationCode = consignmentItem.ProductSpecification.SpecificationCode,
                                    DutyPercent = consignmentItem.ProductSpecification.DutyPercent
                                });

                        if (consignmentItem.IsReSaleAvailability) {
                            newItem.PricePerItemWithoutVat = newItem.PricePerItem =
                                orderItemRepository
                                    .GetReSalePricePerItem(
                                        orderItem.Product.NetUid,
                                        sale.ClientAgreement.NetUid,
                                        orderItem.Id
                                    );
                        }

                        orderItemRepository.Remove(orderItem.NetUid);

                        orderItemId = orderItemRepository.AddOneTimeDiscount(newItem);
                    }

                    consignmentItemMovementRepository.Add(
                        new ConsignmentItemMovement {
                            IsIncomeMovement = false,
                            Qty = qtyToMovement,
                            RemainingQty = qtyToMovement,
                            MovementType = ConsignmentItemMovementType.Sale,
                            ConsignmentItemId = consignmentItem.Id,
                            OrderItemId = orderItemId
                        });

                    if (consignmentItem.IsReSaleAvailability) {
                        newItem.PricePerItemWithoutVat = orderItem.PricePerItem =
                            orderItemRepository
                                .GetReSalePricePerItem(
                                    orderItem.Product.NetUid,
                                    sale.ClientAgreement.NetUid,
                                    orderItemId
                                );
                    }

                    orderItemRepository.AssignSpecification(newItem);
                }

                double qtyRemainingConsignmentItem = remainingConsignmentItem - consignmentItem.RemainingQty;

                StoreProductPlacementAndAvailabilitiesFromSale(
                    consignmentItem,
                    orderItemId,
                    orderItem.ProductId,
                    qtyRemainingConsignmentItem,
                    qtyToReSaleAvailabilities,
                    orderItem.IsFromShiftedItem,
                    productPlacementRepository,
                    productLocationRepository,
                    productAvailabilityRepository,
                    productReservationRepository,
                    consignmentItemRepository,
                    reSaleAvailabilityRepository);
            }
        }

        ActorReferenceManager.Instance.Get(SalesActorNames.BASE_SALES_GET_ACTOR).Tell(
            new GetSaleStatisticWithResourceNameByNetIdMessage(
                sale.NetUid,
                message.ResponseMessage,
                true,
                false,
                true
            ),
            (IActorRef)message.OriginalSender
        );
    }

    private void ProcessStoreConsignmentMovementFromNewOrderItemMessage(StoreConsignmentMovementFromNewOrderItemMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sale sale =
            _saleRepositoriesFactory
                .NewSaleRepository(connection)
                .GetByIdForConsignment(
                    message.SaleId,
                    message.OrderItemId
                );

        if (sale?.ClientAgreement?.Agreement?.Organization == null || !sale.Order.OrderItems.Any()) {
            ((IActorRef)message.ResponseActorRef)
                .Tell(
                    new GetOrderItemAndSaleStatisticAndIsNewSaleMessage(
                        null,
                        Guid.Empty,
                        false
                    ),
                    (IActorRef)message.OriginalSender
                );

            return;
        }

        IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
        IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
        IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
        IReSaleAvailabilityRepository reSaleAvailabilityRepository = _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection);
        IProductLocationRepository productLocationRepository = _productRepositoriesFactory.NewProductLocationRepository(connection);
        IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
        IConsignmentItemRepository consignmentItemRepository = _consignmentRepositoriesFactory.NewConsignmentItemRepository(connection);
        IProductSpecificationRepository specificationRepository = _productRepositoriesFactory.NewProductSpecificationRepository(connection);
        IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
        IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
        IProductWriteOffRuleRepository productWriteOffRuleRepository = _productRepositoriesFactory.NewProductWriteOffRuleRepository(connection);
        IConsignmentItemMovementRepository consignmentItemMovementRepository = _consignmentRepositoriesFactory.NewConsignmentItemMovementRepository(connection);

        OrderItem orderItem = sale.Order.OrderItems.First();

        Product product =
            getSingleProductRepository
                .GetByIdAndRuleLocaleWithProductGroupAndWriteOffRules(
                    orderItem.ProductId,
                    sale.ClientAgreement.Agreement.Organization.Culture
                );

        if (product != null) {
            ProductWriteOffRule writeOffRule =
                GetCurrentWriteOffRuleForProduct(
                    product,
                    sale.ClientAgreement.Agreement.Organization.Culture,
                    userRepository,
                    productWriteOffRuleRepository
                );

            IEnumerable<ConsignmentItem> consignmentItems =
                consignmentItemRepository
                    .GetAllAvailable(
                        sale.ClientAgreement.Agreement.Organization.Id,
                        orderItem.ProductId,
                        writeOffRule.RuleType,
                        sale.IsVatSale,
                        storageId: sale.ClientAgreement.Agreement.Organization.StorageId
                    );

            if (consignmentItems.Any()) {
                if (orderItem.ProductReservations.Any()) {
                    foreach (ProductReservation reservation in orderItem.ProductReservations) {
                        reservation.ProductAvailability.Amount += reservation.Qty;

                        productAvailabilityRepository.Update(reservation.ProductAvailability);

                        productReservationRepository.Delete(reservation.NetUid);
                    }
                }

                //Break up order item to multiple with separate specifications per consignment item
                orderItemRepository.Remove(orderItem);

                foreach (ConsignmentItem consignmentItem in consignmentItems) {
                    double currentOperationQty = orderItem.Qty;

                    if (consignmentItem.RemainingQty < orderItem.Qty)
                        currentOperationQty = consignmentItem.RemainingQty;

                    consignmentItem.RemainingQty -= currentOperationQty;

                    consignmentItemRepository.UpdateRemainingQty(consignmentItem);

                    consignmentItemMovementRepository.Add(
                        new ConsignmentItemMovement {
                            IsIncomeMovement = false,
                            Qty = currentOperationQty,
                            RemainingQty = currentOperationQty,
                            MovementType = ConsignmentItemMovementType.Sale,
                            ConsignmentItemId = consignmentItem.Id,
                            OrderItemId = orderItem.Id
                        });

                    orderItem.Qty -= currentOperationQty;

                    OrderItem newItem = new OrderItem {
                        ProductId = orderItem.ProductId,
                        Comment = orderItem.Comment,
                        Discount = orderItem.Discount,
                        DiscountAmount = orderItem.DiscountAmount,
                        PricePerItem = orderItem.PricePerItem,
                        Qty = currentOperationQty,
                        ChangedQty = orderItem.ChangedQty,
                        OrderedQty = orderItem.OrderedQty,
                        OrderId = orderItem.OrderId,
                        ReturnedQty = 0d,
                        TotalAmount = orderItem.TotalAmount,
                        TotalAmountLocal = orderItem.TotalAmountLocal,
                        TotalWeight = orderItem.TotalWeight,
                        UserId = orderItem.UserId,
                        DiscountUpdatedById = orderItem.DiscountUpdatedById,
                        ExchangeRateAmount = orderItem.ExchangeRateAmount,
                        OneTimeDiscountComment = orderItem.OneTimeDiscountComment,
                        OfferProcessingStatusChangedById = orderItem.OfferProcessingStatusChangedById,
                        UnpackedQty = orderItem.UnpackedQty,
                        FromOfferQty = orderItem.FromOfferQty,
                        IsFromOffer = orderItem.IsFromOffer,
                        OfferProcessingStatus = orderItem.OfferProcessingStatus,
                        OneTimeDiscount = orderItem.OneTimeDiscount,
                        IsValidForCurrentSale = orderItem.IsValidForCurrentSale,
                        PricePerItemWithoutVat = orderItem.PricePerItemWithoutVat,
                        IsFromReSale = consignmentItem.IsReSaleAvailability,
                        Vat = orderItem.Vat
                    };

                    OrderItem existingItem =
                        orderItemRepository
                            .GetOrderItemByOrderProductAndSpecification(
                                newItem.OrderId ?? 0,
                                newItem.ProductId,
                                consignmentItem.ProductSpecification,
                                newItem.IsFromReSale
                            );

                    if (existingItem != null) {
                        orderItem.Id = newItem.Id = existingItem.Id;

                        existingItem.Qty += currentOperationQty;

                        orderItemRepository.UpdateQty(existingItem);
                    } else {
                        newItem.AssignedSpecificationId =
                            specificationRepository
                                .Add(new ProductSpecification {
                                    Name = consignmentItem.ProductSpecification.Name,
                                    Locale = consignmentItem.ProductSpecification.Locale,
                                    AddedById = consignmentItem.ProductSpecification.AddedById,
                                    ProductId = consignmentItem.ProductSpecification.ProductId,
                                    VATValue = consignmentItem.ProductSpecification.VATValue,
                                    CustomsValue = consignmentItem.ProductSpecification.CustomsValue,
                                    Duty = consignmentItem.ProductSpecification.Duty,
                                    VATPercent =
                                        decimal.Round(
                                            consignmentItem.ProductSpecification.Duty +
                                            consignmentItem.ProductSpecification.CustomsValue > 0
                                                ? consignmentItem.ProductSpecification.VATValue * 100 /
                                                  (consignmentItem.ProductSpecification.Duty +
                                                   consignmentItem.ProductSpecification.CustomsValue)
                                                : 0, 2, MidpointRounding.AwayFromZero),
                                    SpecificationCode = consignmentItem.ProductSpecification.SpecificationCode,
                                    DutyPercent =
                                        decimal.Round(
                                            consignmentItem.ProductSpecification != null &&
                                            !consignmentItem.ProductSpecification.CustomsValue.Equals(0)
                                                ? consignmentItem.ProductSpecification.Duty * 100 /
                                                  consignmentItem.ProductSpecification.CustomsValue
                                                : 0, 2, MidpointRounding.AwayFromZero)
                                });

                        if (consignmentItem.IsReSaleAvailability) {
                            newItem.PricePerItemWithoutVat = newItem.PricePerItem =
                                orderItemRepository
                                    .GetReSalePricePerItem(
                                        orderItem.Product.NetUid,
                                        sale.ClientAgreement.NetUid,
                                        orderItem.Id
                                    );
                        }

                        orderItem.Id = newItem.Id = orderItemRepository.Add(newItem);
                    }

                    IEnumerable<ProductPlacement> placements =
                        productPlacementRepository
                            .GetAllByConsignmentItemId(
                                consignmentItem.Id
                            );

                    double toMoveQty = currentOperationQty;

                    foreach (ProductPlacement placement in placements) {
                        double operationQty = toMoveQty;

                        if (placement.Qty < operationQty)
                            operationQty = placement.Qty;

                        placement.Qty -= operationQty;

                        if (placement.Qty > 0)
                            productPlacementRepository.UpdateQty(placement);
                        else
                            productPlacementRepository.Remove(placement);

                        productLocationRepository.Add(new ProductLocation {
                            StorageId = consignmentItem.Consignment.StorageId,
                            Qty = operationQty,
                            ProductPlacementId = placement.Id,
                            OrderItemId = orderItem.Id
                        });

                        toMoveQty -= operationQty;

                        if (toMoveQty.Equals(0d)) break;
                    }

                    ProductAvailability availability =
                        productAvailabilityRepository
                            .GetByProductAndStorageIds(
                                orderItem.ProductId,
                                consignmentItem.Consignment.StorageId
                            );

                    if (availability != null) {
                        availability.Amount -= currentOperationQty;

                        productAvailabilityRepository.Update(availability);

                        ProductReservation reservation =
                            productReservationRepository
                                .GetByOrderItemProductAvailabilityAndConsignmentItemIds(
                                    newItem.Id,
                                    availability.Id,
                                    consignmentItem.Id
                                );

                        decimal accountPrice = consignmentItemRepository.GetPriceForReSaleByConsignmentItemId(consignmentItem.Id);

                        if (reservation != null) {
                            reservation.Qty += currentOperationQty;

                            productReservationRepository.Update(reservation);

                            if (reservation.IsReSaleReservation)
                                reSaleAvailabilityRepository.Add(new ReSaleAvailability {
                                    Qty = currentOperationQty,
                                    RemainingQty = currentOperationQty,
                                    ConsignmentItemId = consignmentItem.Id,
                                    ProductAvailabilityId = availability.Id,
                                    OrderItemId = orderItem.Id,
                                    ProductReservationId = reservation.Id,
                                    PricePerItem = accountPrice,
                                    ExchangeRate = accountPrice / consignmentItem.AccountingPrice
                                });
                        } else {
                            if (consignmentItem.IsReSaleAvailability)
                                reSaleAvailabilityRepository.Add(new ReSaleAvailability {
                                    Qty = currentOperationQty,
                                    RemainingQty = currentOperationQty,
                                    ConsignmentItemId = consignmentItem.Id,
                                    ProductAvailabilityId = availability.Id,
                                    OrderItemId = orderItem.Id,
                                    ProductReservationId = productReservationRepository.AddWithId(new ProductReservation {
                                        OrderItemId = orderItem.Id,
                                        ProductAvailabilityId = availability.Id,
                                        ConsignmentItemId = consignmentItem.Id,
                                        Qty = currentOperationQty,
                                        IsReSaleReservation = consignmentItem.IsReSaleAvailability
                                    }),
                                    PricePerItem = accountPrice,
                                    ExchangeRate = accountPrice / consignmentItem.AccountingPrice
                                });
                            else
                                productReservationRepository.Add(new ProductReservation {
                                    OrderItemId = orderItem.Id,
                                    ProductAvailabilityId = availability.Id,
                                    ConsignmentItemId = consignmentItem.Id,
                                    Qty = currentOperationQty,
                                    IsReSaleReservation = consignmentItem.IsReSaleAvailability
                                });
                        }
                    } else {
                        //Error
                    }

                    if (orderItem.Qty.Equals(0d)) break;
                }

                if (orderItem.Qty > 0) {
                    IEnumerable<ProductAvailability> availableAvailabilities =
                        productAvailabilityRepository
                            .GetByProductAndOrganizationIds(
                                orderItem.ProductId,
                                sale.ClientAgreement.Agreement.Organization.Id,
                                sale.IsVatSale,
                                true,
                                sale.ClientAgreement.Agreement.Organization?.StorageId
                            );

                    foreach (ProductAvailability availability in availableAvailabilities) {
                        OrderItem newRestItem = new OrderItem {
                            ProductId = orderItem.ProductId,
                            Comment = orderItem.Comment,
                            Discount = orderItem.Discount,
                            DiscountAmount = orderItem.DiscountAmount,
                            PricePerItem = orderItem.PricePerItem,
                            Qty = orderItem.Qty,
                            ChangedQty = orderItem.ChangedQty,
                            OrderedQty = orderItem.OrderedQty,
                            OrderId = orderItem.OrderId,
                            ReturnedQty = 0d,
                            TotalAmount = orderItem.TotalAmount,
                            TotalAmountLocal = orderItem.TotalAmountLocal,
                            TotalWeight = orderItem.TotalWeight,
                            UserId = orderItem.UserId,
                            DiscountUpdatedById = orderItem.DiscountUpdatedById,
                            ExchangeRateAmount = orderItem.ExchangeRateAmount,
                            OneTimeDiscountComment = orderItem.OneTimeDiscountComment,
                            OfferProcessingStatusChangedById = orderItem.OfferProcessingStatusChangedById,
                            UnpackedQty = orderItem.UnpackedQty,
                            FromOfferQty = orderItem.FromOfferQty,
                            IsFromOffer = orderItem.IsFromOffer,
                            OfferProcessingStatus = orderItem.OfferProcessingStatus,
                            OneTimeDiscount = orderItem.OneTimeDiscount,
                            IsValidForCurrentSale = orderItem.IsValidForCurrentSale,
                            PricePerItemWithoutVat = orderItem.PricePerItemWithoutVat,
                            AssignedSpecification = new ProductSpecification {
                                Name = string.Empty,
                                SpecificationCode = string.Empty,
                                Locale = sale.ClientAgreement.Agreement.Organization.Culture,
                                DutyPercent = 0m,
                                AddedById = sale.ChangedToInvoiceById ?? sale.UserId.Value,
                                ProductId = orderItem.ProductId
                            },
                            IsFromReSale = availability.IsReSaleAvailability,
                            Vat = orderItem.Vat
                        };

                        OrderItem existingOrderItem =
                            orderItemRepository
                                .GetOrderItemByOrderProductAndSpecification(
                                    newRestItem.OrderId ?? 0,
                                    newRestItem.ProductId,
                                    newRestItem.AssignedSpecification,
                                    newRestItem.IsFromReSale
                                );

                        if (existingOrderItem != null) {
                            newRestItem.Id = existingOrderItem.Id;
                        } else {
                            newRestItem.AssignedSpecificationId = specificationRepository.Add(newRestItem.AssignedSpecification);

                            if (availability.IsReSaleAvailability) {
                                newRestItem.PricePerItemWithoutVat = newRestItem.PricePerItem =
                                    orderItemRepository
                                        .GetReSalePricePerItem(
                                            orderItem.Product.NetUid,
                                            sale.ClientAgreement.NetUid,
                                            orderItem.Id
                                        );
                            }

                            newRestItem.Id = orderItemRepository.Add(newRestItem);
                        }

                        double currentOperationQty = orderItem.Qty;

                        if (availability.Amount < currentOperationQty)
                            currentOperationQty = availability.Amount;

                        ProductReservation reservation =
                            productReservationRepository
                                .GetByOrderItemAndProductAvailabilityIds(
                                    newRestItem.Id,
                                    availability.Id
                                );

                        decimal accountPrice = consignmentItemRepository.GetPriceForReSaleByConsignmentItemId(reservation.ConsignmentItemId.Value);

                        if (reservation != null) {
                            reservation.Qty += currentOperationQty;

                            if (reservation.IsReSaleReservation)
                                reSaleAvailabilityRepository.Add(new ReSaleAvailability {
                                    Qty = currentOperationQty,
                                    RemainingQty = currentOperationQty,
                                    ProductAvailabilityId = availability.Id,
                                    OrderItemId = orderItem.Id,
                                    ConsignmentItemId = reservation.ConsignmentItemId.Value,
                                    ProductReservationId = reservation.Id,
                                    PricePerItem = accountPrice,
                                    ExchangeRate = accountPrice / orderItem.PricePerItem
                                });
                        } else {
                            if (availability.IsReSaleAvailability)
                                reSaleAvailabilityRepository.Add(new ReSaleAvailability {
                                    Qty = currentOperationQty,
                                    RemainingQty = currentOperationQty,
                                    ProductAvailabilityId = availability.Id,
                                    OrderItemId = orderItem.Id,
                                    ProductReservationId = productReservationRepository.AddWithId(new ProductReservation {
                                        ProductAvailabilityId = availability.Id,
                                        OrderItemId = newRestItem.Id,
                                        Qty = currentOperationQty,
                                        IsReSaleReservation = availability.IsReSaleAvailability
                                    }),
                                    // TODO ConsignmentItemId = reservation.ConsignmentItemId,
                                    PricePerItem = accountPrice,
                                    ExchangeRate = accountPrice / orderItem.PricePerItem
                                });
                            else
                                productReservationRepository.Add(new ProductReservation {
                                    ProductAvailabilityId = availability.Id,
                                    OrderItemId = newRestItem.Id,
                                    Qty = currentOperationQty,
                                    IsReSaleReservation = availability.IsReSaleAvailability
                                });
                        }

                        availability.Amount -= currentOperationQty;

                        productAvailabilityRepository.Update(availability);

                        orderItem.Qty -= currentOperationQty;

                        if (orderItem.Qty.Equals(0d)) break;
                    }
                }
            }
        }

        orderItem =
            orderItemRepository
                .GetWithCalculatedProductPrices(
                    orderItem.Id,
                    sale.ClientAgreement.NetUid,
                    sale.ClientAgreement.Agreement.OrganizationId ?? 0,
                    sale.IsVatSale,
                    orderItem.IsFromReSale
                );

        if (!orderItem.PricePerItem.Equals(decimal.Zero)) {
            orderItem.Product.CurrentPrice = orderItem.PricePerItem;
            orderItem.Product.CurrentLocalPrice = orderItem.Product.CurrentPrice * orderItem.ExchangeRateAmount;
        }

        orderItem.TotalAmount = decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
        orderItem.TotalAmountLocal = decimal.Round(orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty), 2,
            MidpointRounding.AwayFromZero);

        orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

        orderItem.Product.AvailableQtyPl =
            orderItem
                .Product
                .ProductAvailabilities
                .Where(a => a.Storage.Locale.ToLower().Equals("pl") && !a.Storage.ForDefective && !a.Storage.ForVatProducts)
                .Sum(a => a.Amount);
        orderItem.Product.AvailableQtyUk =
            orderItem
                .Product
                .ProductAvailabilities
                .Where(a => a.Storage.Locale.ToLower().Equals("uk") && !a.Storage.ForDefective &&
                            (!a.Storage.ForVatProducts || a.Storage.AvailableForReSale))
                .Sum(a => a.Amount);
        orderItem.Product.AvailableQtyPlVAT =
            orderItem
                .Product
                .ProductAvailabilities
                .Where(a => a.Storage.Locale.ToLower().Equals("pl") && !a.Storage.ForDefective && a.Storage.ForVatProducts)
                .Sum(a => a.Amount);
        orderItem.Product.AvailableQtyUkVAT =
            orderItem
                .Product
                .ProductAvailabilities
                .Where(a => a.Storage.Locale.ToLower().Equals("uk") && !a.Storage.ForDefective && a.Storage.ForVatProducts)
                .Sum(a => a.Amount);

        Sale saleFromDb = _saleRepositoriesFactory
            .NewSaleRepository(connection).GetByNetId(sale.NetUid);

        if (saleFromDb != null) {
            foreach (var item in saleFromDb.Order.OrderItems) {
                if (item.Id.Equals(orderItem.Id) && item.Discount != 0) {
                    orderItem.Discount = item.Discount;
                }
            }
        }

        ((IActorRef)message.ResponseActorRef)
            .Tell(
                new GetOrderItemAndSaleStatisticAndIsNewSaleMessage(
                    orderItem,
                    sale.NetUid,
                    false
                ),
                (IActorRef)message.OriginalSender
            );
    }

    private void ProcessStoreConsignmentMovementFromOrderItemBaseShiftStatusMessage(StoreConsignmentMovementFromOrderItemBaseShiftStatusMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        OrderItemBaseShiftStatus shiftStatus =
            _saleRepositoriesFactory
                .NewOrderItemBaseShiftStatusRepository(connection)
                .GetByIdForConsignment(
                    message.ShiftStatusId
                );

        if (shiftStatus == null || !shiftStatus.OrderItem.ConsignmentItemMovements.Any()) return;

        IProductLocationRepository productLocationRepository = _productRepositoriesFactory.NewProductLocationRepository(connection);
        IProductLocationHistoryRepository productLocationHistoryRepository = _productRepositoriesFactory.NewProductLocationHistoryRepository(connection);
        IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
        IConsignmentItemRepository consignmentItemRepository = _consignmentRepositoriesFactory.NewConsignmentItemRepository(connection);
        IConsignmentItemMovementRepository consignmentItemMovementRepository = _consignmentRepositoriesFactory.NewConsignmentItemMovementRepository(connection);
        IHistoryInvoiceEditRepository historyInvoiceEditRepository = _saleRepositoriesFactory.NewHistoryInvoiceEditRepository(connection);
        HistoryInvoiceEdit historyInvoiceEdit = null;
        if (shiftStatus.HistoryInvoiceEditId != null) {
            historyInvoiceEdit = historyInvoiceEditRepository.GetById((long)shiftStatus.HistoryInvoiceEditId);
        }

        double currentOperationQty = shiftStatus.Qty;
        double operationQty = shiftStatus.Qty;
        TypeOfMovement typeOfMovement;
        foreach (ConsignmentItemMovement movement in shiftStatus.OrderItem.ConsignmentItemMovements) {
            if (currentOperationQty.Equals(0d)) break;
            ConsignmentItemMovement consignmentItemMovement = new ConsignmentItemMovement {
                IsIncomeMovement = true,
                //ProductIncomeItemId = movement.ConsignmentItemId,
                MovementType = shiftStatus.SaleId.HasValue ? ConsignmentItemMovementType.Shifting : ConsignmentItemMovementType.ShiftingStorage,
                OrderItemBaseShiftStatusId = shiftStatus.Id,
                ConsignmentItemId = movement.ConsignmentItemId
                //Qty = shiftStatus.Qty
            };
            if (currentOperationQty > movement.RemainingQty) {
                movement.ConsignmentItem.RemainingQty += movement.RemainingQty;
                consignmentItemMovement.Qty = movement.RemainingQty;
                currentOperationQty -= movement.RemainingQty;
                movement.RemainingQty = 0;
                consignmentItemMovementRepository.UpdateRemainingQty(movement);
                consignmentItemRepository.UpdateRemainingQty(movement.ConsignmentItem);
            } else {
                movement.ConsignmentItem.RemainingQty += currentOperationQty;
                consignmentItemMovement.Qty = currentOperationQty;
                movement.RemainingQty -= currentOperationQty;
                consignmentItemMovementRepository.UpdateRemainingQty(movement);
                consignmentItemRepository.UpdateRemainingQty(movement.ConsignmentItem);
                currentOperationQty = 0;
            }
            //1272
            //if (movement.RemainingQty < currentOperationQty)
            //    currentOperationQty = movement.RemainingQty;

            //movement.RemainingQty -= currentOperationQty;

            //consignmentItemMovementRepository.UpdateRemainingQty(movement);

            //movement.ConsignmentItem.RemainingQty += currentOperationQty;

            //consignmentItemRepository.UpdateRemainingQty(movement.ConsignmentItem);

            //shiftStatus.Qty -= currentOperationQty;

            IEnumerable<ProductLocation> locations =
                productLocationRepository
                    .GetAllByOrderItemId(
                        shiftStatus.OrderItemId
                    );
            foreach (ProductLocation location in locations) {
                ProductPlacement productPlacement = productPlacementRepository.GetByIdDeleted(location.ProductPlacementId);

                if (operationQty.Equals(0d)) break;

                if (operationQty > location.Qty) {
                    if (location.Qty > 0 && location.InvoiceDocumentQty == 0) {
                        location.InvoiceDocumentQty = location.Qty;
                        productLocationRepository.UpdateIvoiceDocumentQty(location);
                    }

                    if (shiftStatus.ShiftStatus == OrderItemShiftStatus.Store) {
                        typeOfMovement = TypeOfMovement.ActEditTheInvoice;
                    } else {
                        typeOfMovement = TypeOfMovement.Reserv;
                    }

                    productLocationHistoryRepository.Add(new ProductLocationHistory {
                        ProductPlacementId = productPlacement.Id,
                        Qty = location.Qty,
                        StorageId = location.StorageId,
                        TypeOfMovement = typeOfMovement,
                        OrderItemId = location.OrderItemId,
                        HistoryInvoiceEditId = historyInvoiceEdit?.Id
                    });
                    productPlacement.Qty += location.Qty;
                    operationQty -= location.Qty;
                    location.Qty = 0;
                    productLocationRepository.Remove(location);

                    if (location.ProductPlacement.Deleted)
                        productPlacementRepository.Restore(productPlacement);
                    else
                        productPlacementRepository.UpdateQty(productPlacement);
                } else {
                    if (location.Qty > 0 && location.InvoiceDocumentQty == 0) {
                        location.InvoiceDocumentQty = operationQty;
                        productLocationRepository.UpdateIvoiceDocumentQty(location);
                    }

                    if (shiftStatus.ShiftStatus == OrderItemShiftStatus.Store) {
                        typeOfMovement = TypeOfMovement.ActEditTheInvoice;
                    } else {
                        typeOfMovement = TypeOfMovement.Reserv;
                    }

                    productLocationHistoryRepository.Add(new ProductLocationHistory {
                        ProductPlacementId = productPlacement.Id,
                        Qty = operationQty,
                        StorageId = location.StorageId,
                        TypeOfMovement = typeOfMovement,
                        OrderItemId = location.OrderItemId,
                        HistoryInvoiceEditId = historyInvoiceEdit?.Id
                    });
                    productPlacement.Qty += operationQty;
                    location.Qty -= operationQty;
                    operationQty = 0;

                    if (location.Qty > 0)
                        productLocationRepository.Update(location);
                    else
                        productLocationRepository.Remove(location);

                    if (location.ProductPlacement.Deleted)
                        productPlacementRepository.Restore(productPlacement);
                    else
                        productPlacementRepository.UpdateQty(productPlacement);
                }

                //1272
                //if (location.Qty < operationQty)
                //    operationQty = location.Qty;

                //if (location.Qty > 0 && location.InvoiceDocumentQty == 0) {
                //    location.InvoiceDocumentQty = location.Qty;
                //    productLocationRepository.UpdateIvoiceDocumentQty(location);
                //}

                //location.Qty -= operationQty;

                //if (location.Qty > 0)
                //    productLocationRepository.Update(location);
                //else
                //    productLocationRepository.Remove(location);

                //location.ProductPlacement.Qty += operationQty;
                //TypeOfMovement typeOfMovement;
                //if (shiftStatus.ShiftStatus == OrderItemShiftStatus.Store) {
                //    typeOfMovement = TypeOfMovement.ActEditTheInvoice;
                //} else {
                //    typeOfMovement = TypeOfMovement.Reserv;
                //}
                //if (location.Qty != 0) {
                //    productLocationHistoryRepository.Add(new ProductLocationHistory {
                //        ProductPlacementId = location.ProductPlacementId,
                //        Qty = operationQty,
                //        StorageId = location.StorageId,
                //        TypeOfMovement = typeOfMovement,
                //        OrderItemId = location.OrderItemId,
                //    });
                //}

                //if (location.ProductPlacement.Deleted)
                //    productPlacementRepository.Restore(location.ProductPlacement);
                //else
                //    productPlacementRepository.UpdateQty(location.ProductPlacement);
            }

            consignmentItemMovementRepository.Add(consignmentItemMovement);
        }
    }

    private void ProcessValidateAndStoreConsignmentMovementFromSupplyReturnMessage(ValidateAndStoreConsignmentMovementFromSupplyReturnMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyReturnRepository supplyReturnRepository =
                _supplyRepositoriesFactory
                    .NewSupplyReturnRepository(connection);

            SupplyReturn supplyReturn =
                supplyReturnRepository
                    .GetByIdForConsignment(
                        message.SupplyReturnId
                    );

            if (supplyReturn?.Organization == null) {
                Sender.Tell(supplyReturnRepository.GetById(message.SupplyReturnId));

                return;
            }

            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IConsignmentItemRepository consignmentItemRepository = _consignmentRepositoriesFactory.NewConsignmentItemRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IConsignmentItemMovementRepository consignmentItemMovementRepository = _consignmentRepositoriesFactory.NewConsignmentItemMovementRepository(connection);
            IReSaleAvailabilityRepository reSaleAvailabilityRepository = _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection);

            bool hasError = false;

            foreach (SupplyReturnItem returnItem in supplyReturn.SupplyReturnItems) {
                IEnumerable<ConsignmentItem> availableConsignmentItems =
                    consignmentItemRepository
                        .GetAvailableItemsCreatedFromSpecificRootItemOnSpecificStorage(
                            returnItem.ConsignmentItemId,
                            supplyReturn.StorageId
                        );

                if (!availableConsignmentItems.Any()) {
                    hasError = true;

                    Sender.Tell(
                        new LocalizedException(
                            SupplyReturnResourceNames.PRODUCT_NOT_AVAILABLE_ON_STORAGE,
                            returnItem.Product.VendorCode
                        )
                    );

                    break;
                }

                if (availableConsignmentItems.Sum(i => i.RemainingQty) >= returnItem.Qty) continue;

                hasError = true;

                Sender.Tell(
                    new LocalizedException(
                        SupplyReturnResourceNames.PRODUCT_HAS_LESS_AVAILABILITY,
                        returnItem.Product.VendorCode
                    )
                );

                break;
            }

            if (hasError) {
                supplyReturnRepository.Remove(supplyReturn.Id);

                _supplyRepositoriesFactory.NewSupplyReturnItemRepository(connection).RemoveAllBySupplyReturnId(supplyReturn.Id);

                return;
            }

            foreach (SupplyReturnItem returnItem in supplyReturn.SupplyReturnItems) {
                IEnumerable<ConsignmentItem> availableConsignmentItems =
                    consignmentItemRepository
                        .GetAvailableItemsCreatedFromSpecificRootItemOnSpecificStorage(
                            returnItem.ConsignmentItemId,
                            supplyReturn.StorageId
                        );

                double operationQty = returnItem.Qty;

                foreach (ConsignmentItem consignmentItem in availableConsignmentItems) {
                    double currentOperationQty = operationQty;

                    if (consignmentItem.RemainingQty < currentOperationQty)
                        currentOperationQty = consignmentItem.RemainingQty;

                    consignmentItem.RemainingQty -= currentOperationQty;

                    consignmentItemRepository.UpdateRemainingQty(consignmentItem);

                    IEnumerable<ProductPlacement> placements =
                        productPlacementRepository
                            .GetAllByConsignmentItemId(
                                consignmentItem.Id
                            );

                    double toMoveQty = currentOperationQty;

                    foreach (ProductPlacement placement in placements) {
                        double currentMoveQty = toMoveQty;

                        if (placement.Qty < currentMoveQty)
                            currentMoveQty = placement.Qty;

                        placement.Qty -= currentMoveQty;

                        toMoveQty -= currentMoveQty;

                        if (placement.Qty > 0)
                            productPlacementRepository.UpdateQty(placement);
                        else
                            productPlacementRepository.Remove(placement);

                        if (toMoveQty.Equals(0d)) break;
                    }

                    ProductAvailability availability =
                        productAvailabilityRepository
                            .GetByProductAndStorageIds(
                                consignmentItem.ProductId,
                                supplyReturn.StorageId
                            );

                    if (availability != null) {
                        availability.Amount -= currentOperationQty;

                        productAvailabilityRepository.Update(availability);
                    } else {
                        //Error
                    }

                    if (message.WithReSale) {
                        decimal accountPrice = consignmentItemRepository.GetPriceForReSaleByConsignmentItemId(returnItem.ConsignmentItemId);

                        reSaleAvailabilityRepository.Add(new ReSaleAvailability {
                            SupplyReturnItemId = returnItem.Id,
                            ConsignmentItemId = returnItem.ConsignmentItemId,
                            ProductAvailabilityId = availability.Id,
                            ExchangeRate = accountPrice / (consignmentItem.AccountingPrice.Equals(0) ? consignmentItem.NetPrice : consignmentItem.AccountingPrice),
                            PricePerItem = accountPrice,
                            Qty = operationQty,
                            RemainingQty = operationQty
                        });
                    }
                }

                consignmentItemMovementRepository.Add(
                    new ConsignmentItemMovement {
                        IsIncomeMovement = false,
                        Qty = returnItem.Qty,
                        MovementType = ConsignmentItemMovementType.SupplyReturn,
                        ConsignmentItemId = returnItem.ConsignmentItemId,
                        SupplyReturnItemId = returnItem.Id
                    });
            }

            Sender.Tell(
                supplyReturnRepository
                    .GetById(
                        supplyReturn.Id
                    )
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessAddReservationOnConsignmentFromNewSadMessage(AddReservationOnConsignmentFromNewSadMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sad sad =
                _supplyUkraineRepositoriesFactory
                    .NewSadRepository(connection, null)
                    .GetByIdForConsignment(
                        message.CreatedSadId
                    );

            if (sad == null) {
                Sender.Tell(new FinishAddOrUpdateSadMessage(message.CreatedSadId), (IActorRef)message.OriginalSender);

                return;
            }

            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            ISadItemRepository sadItemRepository = _supplyUkraineRepositoriesFactory.NewSadItemRepository(connection);
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IConsignmentItemRepository consignmentItemRepository = _consignmentRepositoriesFactory.NewConsignmentItemRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IProductWriteOffRuleRepository productWriteOffRuleRepository = _productRepositoriesFactory.NewProductWriteOffRuleRepository(connection);
            ISupplyOrderUkraineCartItemRepository cartItemRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemRepository(connection);
            ISupplyOrderUkraineCartItemReservationRepository cartItemReservationRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemReservationRepository(connection);
            ISupplyOrderUkraineCartItemReservationProductPlacementRepository reservationProductPlacementRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemReservationProductPlacementRepository(connection);

            foreach (SadItem sadItem in sad.SadItems) {
                ReserveConsignmentsForNewSadItem(
                    getSingleProductRepository,
                    sadItem,
                    sad,
                    userRepository,
                    productWriteOffRuleRepository,
                    consignmentItemRepository,
                    productAvailabilityRepository,
                    cartItemReservationRepository,
                    sadItemRepository,
                    cartItemRepository,
                    productPlacementRepository,
                    reservationProductPlacementRepository
                );
            }

            Sender.Tell(new FinishAddOrUpdateSadMessage(sad.Id), (IActorRef)message.OriginalSender);
        } catch (Exception exc) {
            ((IActorRef)message.OriginalSender).Tell(exc);
        }
    }

    private void ProcessChangeReservationsOnConsignmentFromSadUpdateMessage(ChangeReservationsOnConsignmentFromSadUpdateMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sad sad =
                _supplyUkraineRepositoriesFactory
                    .NewSadRepository(connection, null)
                    .GetByIdForConsignment(
                        message.SadId
                    );

            if (sad == null) {
                Sender.Tell(new FinishAddOrUpdateSadMessage(message.SadId), (IActorRef)message.OriginalSender);

                return;
            }

            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            ISadItemRepository sadItemRepository = _supplyUkraineRepositoriesFactory.NewSadItemRepository(connection);
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IConsignmentItemRepository consignmentItemRepository = _consignmentRepositoriesFactory.NewConsignmentItemRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IProductWriteOffRuleRepository productWriteOffRuleRepository = _productRepositoriesFactory.NewProductWriteOffRuleRepository(connection);
            ISupplyOrderUkraineCartItemRepository cartItemRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemRepository(connection);
            ISupplyOrderUkraineCartItemReservationRepository cartItemReservationRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemReservationRepository(connection);
            ISupplyOrderUkraineCartItemReservationProductPlacementRepository reservationProductPlacementRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemReservationProductPlacementRepository(connection);

            User user = userRepository.GetByNetId(message.UserNetId);

            foreach (long createdItemId in message.CreatedItemIds) {
                SadItem sadItem = sad.SadItems.FirstOrDefault(i => i.Id.Equals(createdItemId));

                if (sadItem == null) continue;

                ReserveConsignmentsForNewSadItem(
                    getSingleProductRepository,
                    sadItem,
                    sad,
                    userRepository,
                    productWriteOffRuleRepository,
                    consignmentItemRepository,
                    productAvailabilityRepository,
                    cartItemReservationRepository,
                    sadItemRepository,
                    cartItemRepository,
                    productPlacementRepository,
                    reservationProductPlacementRepository
                );
            }

            foreach (SadItem sadItem in message.UpdatedItems) {
                SadItem itemFromDb = sad.SadItems.FirstOrDefault(i => i.Id.Equals(sadItem.Id));

                if (itemFromDb == null) continue;

                double changedQty = sadItem.Qty - itemFromDb.Qty;

                sadItemRepository.Update(sadItem);

                if (changedQty > 0) {
                    foreach (SupplyOrderUkraineCartItemReservation reservation in itemFromDb.SupplyOrderUkraineCartItem.SupplyOrderUkraineCartItemReservations) {
                        if (reservation.ProductAvailability != null) {
                            reservation.ProductAvailability.Amount += reservation.Qty;

                            productAvailabilityRepository.Update(reservation.ProductAvailability);
                        }

                        if (reservation.ConsignmentItem != null) {
                            reservation.ConsignmentItem.RemainingQty += reservation.Qty;

                            consignmentItemRepository.UpdateRemainingQty(reservation.ConsignmentItem);
                        }

                        IEnumerable<SupplyOrderUkraineCartItemReservationProductPlacement> placements =
                            reservationProductPlacementRepository
                                .GetAllByReservationId(
                                    reservation.Id
                                );

                        foreach (SupplyOrderUkraineCartItemReservationProductPlacement placement in placements) {
                            placement.ProductPlacement.Qty += placement.Qty;

                            if (placement.ProductPlacement.Deleted)
                                productPlacementRepository.Restore(placement.ProductPlacement);
                            else
                                productPlacementRepository.UpdateQty(placement.ProductPlacement);

                            placement.Qty = 0d;
                            placement.Deleted = true;

                            reservationProductPlacementRepository.Update(placement);
                        }

                        reservation.Deleted = true;

                        cartItemReservationRepository.Update(reservation);
                    }

                    sadItem.SupplyOrderUkraineCartItem.SupplyOrderUkraineCartItemReservations = new HashSet<SupplyOrderUkraineCartItemReservation>();

                    ReserveConsignmentsForNewSadItem(
                        getSingleProductRepository,
                        sadItem,
                        sad,
                        userRepository,
                        productWriteOffRuleRepository,
                        consignmentItemRepository,
                        productAvailabilityRepository,
                        cartItemReservationRepository,
                        sadItemRepository,
                        cartItemRepository,
                        productPlacementRepository,
                        reservationProductPlacementRepository
                    );
                } else {
                    double decreasedQty = changedQty = 0 - changedQty;

                    foreach (SupplyOrderUkraineCartItemReservation reservation in itemFromDb.SupplyOrderUkraineCartItem.SupplyOrderUkraineCartItemReservations) {
                        double operationQty = changedQty;

                        if (reservation.Qty < operationQty)
                            operationQty = reservation.Qty;

                        reservation.ProductAvailability.Amount += operationQty;

                        productAvailabilityRepository.Update(reservation.ProductAvailability);

                        if (reservation.ConsignmentItem != null) {
                            reservation.ConsignmentItem.RemainingQty += operationQty;

                            consignmentItemRepository.UpdateRemainingQty(reservation.ConsignmentItem);
                        }

                        reservation.Qty -= operationQty;

                        if (reservation.Qty <= 0d)
                            reservation.Deleted = true;

                        cartItemReservationRepository.Update(reservation);

                        changedQty -= operationQty;

                        IEnumerable<SupplyOrderUkraineCartItemReservationProductPlacement> placements =
                            reservationProductPlacementRepository
                                .GetAllByReservationId(
                                    reservation.Id
                                );

                        foreach (SupplyOrderUkraineCartItemReservationProductPlacement placement in placements) {
                            double currentOperationQty = operationQty;

                            if (placement.Qty < currentOperationQty)
                                currentOperationQty = placement.Qty;

                            placement.Qty -= currentOperationQty;

                            if (placement.Qty.Equals(0d))
                                placement.Deleted = true;

                            reservationProductPlacementRepository.Update(placement);

                            placement.ProductPlacement.Qty += currentOperationQty;

                            if (placement.ProductPlacement.Deleted)
                                productPlacementRepository.Restore(placement.ProductPlacement);
                            else
                                productPlacementRepository.UpdateQty(placement.ProductPlacement);

                            operationQty -= currentOperationQty;

                            if (operationQty.Equals(0d)) break;
                        }

                        if (changedQty.Equals(0d)) break;
                    }

                    sadItem.SupplyOrderUkraineCartItem.ReservedQty = sadItem.Qty;

                    cartItemRepository.Update(sadItem.SupplyOrderUkraineCartItem);

                    SupplyOrderUkraineCartItem cartItem = new SupplyOrderUkraineCartItem {
                        Comment = sadItem.SupplyOrderUkraineCartItem.Comment,
                        UploadedQty = decreasedQty,
                        ItemPriority = sadItem.SupplyOrderUkraineCartItem.ItemPriority,
                        ProductId = sadItem.SupplyOrderUkraineCartItem.ProductId,
                        CreatedById = sadItem.SupplyOrderUkraineCartItem.CreatedById,
                        UpdatedById = sadItem.SupplyOrderUkraineCartItem.UpdatedById,
                        ResponsibleId = sadItem.SupplyOrderUkraineCartItem.ResponsibleId,
                        ReservedQty = decreasedQty,
                        FromDate = sadItem.SupplyOrderUkraineCartItem.FromDate,
                        UnpackedQty = decreasedQty,
                        SupplierId = sadItem.SupplyOrderUkraineCartItem.SupplierId,
                        MaxQtyPerTF = sadItem.SupplyOrderUkraineCartItem.MaxQtyPerTF,
                        IsRecommended = sadItem.SupplyOrderUkraineCartItem.IsRecommended,
                        TaxFreePackListId = null,
                        PackingListPackageOrderItemId = null,
                        NetWeight = 0d,
                        UnitPrice = 0m
                    };

                    IEnumerable<ProductAvailability> availabilities =
                        productAvailabilityRepository
                            .GetByProductAndCultureIds(
                                cartItem.ProductId,
                                "pl"
                            );

                    List<SupplyOrderUkraineCartItemReservation> reservations = new List<SupplyOrderUkraineCartItemReservation>();

                    foreach (ProductAvailability availability in availabilities) {
                        if (availability.Amount < decreasedQty) {
                            decreasedQty -= availability.Amount;

                            reservations.Add(new SupplyOrderUkraineCartItemReservation {
                                Qty = availability.Amount,
                                ProductAvailabilityId = availability.Id
                            });

                            availability.Amount = 0;

                            productAvailabilityRepository.Update(availability);
                        } else {
                            reservations.Add(new SupplyOrderUkraineCartItemReservation {
                                Qty = decreasedQty,
                                ProductAvailabilityId = availability.Id
                            });

                            availability.Amount -= decreasedQty;

                            productAvailabilityRepository.Update(availability);

                            decreasedQty = 0d;
                        }

                        if (decreasedQty.Equals(0d)) break;
                    }

                    SupplyOrderUkraineCartItem fromDb = cartItemRepository.GetByProductIdIfExists(cartItem.ProductId);

                    if (fromDb != null) {
                        fromDb.ReservedQty += cartItem.ReservedQty;
                        fromDb.UpdatedById = user.Id;

                        cartItemRepository.Update(fromDb);

                        foreach (SupplyOrderUkraineCartItemReservation reservation in reservations) {
                            SupplyOrderUkraineCartItemReservation fromDbReservation =
                                cartItemReservationRepository
                                    .GetByIdsIfExists(
                                        fromDb.Id,
                                        reservation.ProductAvailabilityId
                                    );

                            if (fromDbReservation != null) {
                                fromDbReservation.Qty += reservation.Qty;

                                cartItemReservationRepository.Update(fromDbReservation);
                            } else {
                                reservation.SupplyOrderUkraineCartItemId = fromDb.Id;

                                cartItemReservationRepository.Add(reservation);
                            }
                        }
                    } else {
                        cartItem.CreatedById = user.Id;

                        cartItem.Id = cartItemRepository.Add(cartItem);

                        cartItemReservationRepository.Add(reservations.Select(reservation => {
                            reservation.SupplyOrderUkraineCartItemId = cartItem.Id;

                            return reservation;
                        }));
                    }
                }
            }

            foreach (SadItem sadItem in message.DeletedItems) {
                foreach (SupplyOrderUkraineCartItemReservation reservation in sadItem.SupplyOrderUkraineCartItem.SupplyOrderUkraineCartItemReservations) {
                    if (reservation.ProductAvailability != null) {
                        reservation.ProductAvailability.Amount += reservation.Qty;

                        productAvailabilityRepository.Update(reservation.ProductAvailability);
                    }

                    if (reservation.ConsignmentItem != null) {
                        reservation.ConsignmentItem.RemainingQty += reservation.Qty;

                        consignmentItemRepository.UpdateRemainingQty(reservation.ConsignmentItem);
                    }

                    IEnumerable<SupplyOrderUkraineCartItemReservationProductPlacement> placements =
                        reservationProductPlacementRepository
                            .GetAllByReservationId(
                                reservation.Id
                            );

                    foreach (SupplyOrderUkraineCartItemReservationProductPlacement placement in placements) {
                        placement.ProductPlacement.Qty += placement.Qty;

                        if (placement.ProductPlacement.Deleted)
                            productPlacementRepository.Restore(placement.ProductPlacement);
                        else
                            productPlacementRepository.UpdateQty(placement.ProductPlacement);

                        placement.Qty = 0d;
                        placement.Deleted = true;

                        reservationProductPlacementRepository.Update(placement);
                    }

                    reservation.Deleted = true;

                    cartItemReservationRepository.Update(reservation);
                }

                SupplyOrderUkraineCartItem cartItem = new SupplyOrderUkraineCartItem {
                    Comment = sadItem.SupplyOrderUkraineCartItem.Comment,
                    UploadedQty = sadItem.SupplyOrderUkraineCartItem.ReservedQty,
                    ItemPriority = sadItem.SupplyOrderUkraineCartItem.ItemPriority,
                    ProductId = sadItem.SupplyOrderUkraineCartItem.ProductId,
                    CreatedById = sadItem.SupplyOrderUkraineCartItem.CreatedById,
                    UpdatedById = sadItem.SupplyOrderUkraineCartItem.UpdatedById,
                    ResponsibleId = sadItem.SupplyOrderUkraineCartItem.ResponsibleId,
                    ReservedQty = sadItem.SupplyOrderUkraineCartItem.ReservedQty,
                    FromDate = sadItem.SupplyOrderUkraineCartItem.FromDate,
                    UnpackedQty = sadItem.SupplyOrderUkraineCartItem.UnpackedQty,
                    SupplierId = sadItem.SupplyOrderUkraineCartItem.SupplierId,
                    MaxQtyPerTF = sadItem.SupplyOrderUkraineCartItem.MaxQtyPerTF,
                    IsRecommended = sadItem.SupplyOrderUkraineCartItem.IsRecommended,
                    TaxFreePackListId = null,
                    PackingListPackageOrderItemId = null,
                    NetWeight = 0d,
                    UnitPrice = 0m
                };

                IEnumerable<ProductAvailability> availabilities =
                    productAvailabilityRepository
                        .GetByProductAndCultureIds(
                            cartItem.ProductId,
                            "pl"
                        );

                double operationQty = cartItem.UploadedQty;

                List<SupplyOrderUkraineCartItemReservation> reservations = new List<SupplyOrderUkraineCartItemReservation>();

                foreach (ProductAvailability availability in availabilities) {
                    if (availability.Amount < operationQty) {
                        operationQty -= availability.Amount;

                        reservations.Add(new SupplyOrderUkraineCartItemReservation {
                            Qty = availability.Amount,
                            ProductAvailabilityId = availability.Id
                        });

                        availability.Amount = 0;

                        productAvailabilityRepository.Update(availability);
                    } else {
                        reservations.Add(new SupplyOrderUkraineCartItemReservation {
                            Qty = operationQty,
                            ProductAvailabilityId = availability.Id
                        });

                        availability.Amount -= operationQty;

                        productAvailabilityRepository.Update(availability);

                        operationQty = 0d;
                    }

                    if (operationQty.Equals(0d)) break;
                }

                cartItem.ReservedQty -= operationQty;

                SupplyOrderUkraineCartItem fromDb = cartItemRepository.GetByProductIdIfExists(cartItem.ProductId);

                if (fromDb != null) {
                    fromDb.ReservedQty += cartItem.ReservedQty;
                    fromDb.UpdatedById = user.Id;

                    cartItemRepository.Update(fromDb);

                    foreach (SupplyOrderUkraineCartItemReservation reservation in reservations) {
                        SupplyOrderUkraineCartItemReservation existingReservation =
                            cartItemReservationRepository
                                .GetByIdsIfExists(
                                    fromDb.Id,
                                    reservation.ProductAvailabilityId
                                );

                        if (existingReservation != null) {
                            existingReservation.Qty += reservation.Qty;

                            cartItemReservationRepository.Update(existingReservation);
                        } else {
                            reservation.SupplyOrderUkraineCartItemId = fromDb.Id;

                            cartItemReservationRepository.Add(reservation);
                        }
                    }
                } else {
                    cartItem.CreatedById = user.Id;

                    cartItem.Id = cartItemRepository.Add(cartItem);

                    cartItemReservationRepository.Add(reservations.Select(reservation => {
                        reservation.SupplyOrderUkraineCartItemId = cartItem.Id;

                        return reservation;
                    }));
                }
            }

            if (message.StoreConsignmentMovement)
                Self.Tell(new StoreConsignmentMovementFromSadMessage(sad.Id));

            Sender.Tell(new FinishAddOrUpdateSadMessage(sad.Id), (IActorRef)message.OriginalSender);
        } catch (Exception exc) {
            ((IActorRef)message.OriginalSender).Tell(exc);
        }
    }

    private void ProcessRestoreReservationsOnConsignmentFromSadDeleteMessage(RestoreReservationsOnConsignmentFromSadDeleteMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sad sad =
            _supplyUkraineRepositoriesFactory
                .NewSadRepository(connection, null)
                .GetByIdForConsignment(
                    message.SadId
                );

        if (sad == null) return;

        IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
        IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
        IConsignmentItemRepository consignmentItemRepository = _consignmentRepositoriesFactory.NewConsignmentItemRepository(connection);
        IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
        ISupplyOrderUkraineCartItemRepository cartItemRepository =
            _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemRepository(connection);
        ISupplyOrderUkraineCartItemReservationRepository cartItemReservationRepository =
            _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemReservationRepository(connection);
        ISupplyOrderUkraineCartItemReservationProductPlacementRepository reservationProductPlacementRepository =
            _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemReservationProductPlacementRepository(connection);

        User user = userRepository.GetByNetId(message.UserNetId);

        foreach (SadItem sadItem in sad.SadItems) {
            foreach (SupplyOrderUkraineCartItemReservation reservation in sadItem.SupplyOrderUkraineCartItem.SupplyOrderUkraineCartItemReservations) {
                if (reservation.ProductAvailability != null) {
                    reservation.ProductAvailability.Amount += reservation.Qty;

                    productAvailabilityRepository.Update(reservation.ProductAvailability);
                }

                if (reservation.ConsignmentItem != null) {
                    reservation.ConsignmentItem.RemainingQty += reservation.Qty;

                    consignmentItemRepository.UpdateRemainingQty(reservation.ConsignmentItem);
                }

                IEnumerable<SupplyOrderUkraineCartItemReservationProductPlacement> placements =
                    reservationProductPlacementRepository
                        .GetAllByReservationId(
                            reservation.Id
                        );

                foreach (SupplyOrderUkraineCartItemReservationProductPlacement placement in placements) {
                    placement.ProductPlacement.Qty += placement.Qty;

                    if (placement.ProductPlacement.Deleted)
                        productPlacementRepository.Restore(placement.ProductPlacement);
                    else
                        productPlacementRepository.UpdateQty(placement.ProductPlacement);

                    placement.Qty = 0d;
                    placement.Deleted = true;

                    reservationProductPlacementRepository.Update(placement);
                }

                reservation.Deleted = true;

                cartItemReservationRepository.Update(reservation);
            }

            SupplyOrderUkraineCartItem cartItem = new SupplyOrderUkraineCartItem {
                Comment = sadItem.SupplyOrderUkraineCartItem.Comment,
                UploadedQty = sadItem.SupplyOrderUkraineCartItem.ReservedQty,
                ItemPriority = sadItem.SupplyOrderUkraineCartItem.ItemPriority,
                ProductId = sadItem.SupplyOrderUkraineCartItem.ProductId,
                CreatedById = sadItem.SupplyOrderUkraineCartItem.CreatedById,
                UpdatedById = sadItem.SupplyOrderUkraineCartItem.UpdatedById,
                ResponsibleId = sadItem.SupplyOrderUkraineCartItem.ResponsibleId,
                ReservedQty = sadItem.SupplyOrderUkraineCartItem.ReservedQty,
                FromDate = sadItem.SupplyOrderUkraineCartItem.FromDate,
                UnpackedQty = sadItem.SupplyOrderUkraineCartItem.UnpackedQty,
                SupplierId = sadItem.SupplyOrderUkraineCartItem.SupplierId,
                MaxQtyPerTF = sadItem.SupplyOrderUkraineCartItem.MaxQtyPerTF,
                IsRecommended = sadItem.SupplyOrderUkraineCartItem.IsRecommended,
                TaxFreePackListId = null,
                PackingListPackageOrderItemId = null,
                NetWeight = 0d,
                UnitPrice = 0m
            };

            IEnumerable<ProductAvailability> availabilities =
                productAvailabilityRepository
                    .GetByProductAndCultureIds(
                        cartItem.ProductId,
                        "pl"
                    );

            double operationQty = cartItem.UploadedQty;

            List<SupplyOrderUkraineCartItemReservation> reservations = new List<SupplyOrderUkraineCartItemReservation>();

            foreach (ProductAvailability availability in availabilities) {
                if (availability.Amount < operationQty) {
                    operationQty -= availability.Amount;

                    reservations.Add(new SupplyOrderUkraineCartItemReservation {
                        Qty = availability.Amount,
                        ProductAvailabilityId = availability.Id
                    });

                    availability.Amount = 0;

                    productAvailabilityRepository.Update(availability);
                } else {
                    reservations.Add(new SupplyOrderUkraineCartItemReservation {
                        Qty = operationQty,
                        ProductAvailabilityId = availability.Id
                    });

                    availability.Amount -= operationQty;

                    productAvailabilityRepository.Update(availability);

                    operationQty = 0d;
                }

                if (operationQty.Equals(0d)) break;
            }

            cartItem.ReservedQty -= operationQty;

            SupplyOrderUkraineCartItem fromDb = cartItemRepository.GetByProductIdIfExists(cartItem.ProductId);

            if (fromDb != null) {
                fromDb.ReservedQty += cartItem.ReservedQty;
                fromDb.UpdatedById = user.Id;

                cartItemRepository.Update(fromDb);

                foreach (SupplyOrderUkraineCartItemReservation reservation in reservations) {
                    SupplyOrderUkraineCartItemReservation existingReservation =
                        cartItemReservationRepository
                            .GetByIdsIfExists(
                                fromDb.Id,
                                reservation.ProductAvailabilityId
                            );

                    if (existingReservation != null) {
                        existingReservation.Qty += reservation.Qty;

                        cartItemReservationRepository.Update(existingReservation);
                    } else {
                        reservation.SupplyOrderUkraineCartItemId = fromDb.Id;

                        cartItemReservationRepository.Add(reservation);
                    }
                }
            } else {
                cartItem.CreatedById = user.Id;

                cartItem.Id = cartItemRepository.Add(cartItem);

                cartItemReservationRepository.Add(reservations.Select(reservation => {
                    reservation.SupplyOrderUkraineCartItemId = cartItem.Id;

                    return reservation;
                }));
            }
        }
    }

    private void ProcessChangeReservationsOnConsignmentFromTaxFreePackListMessage(ChangeReservationsOnConsignmentFromTaxFreePackListMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            TaxFreePackList packList =
                _supplyUkraineRepositoriesFactory
                    .NewTaxFreePackListRepository(connection, null)
                    .GetByIdForConsignment(
                        message.TaxFreePackListId
                    );

            if (packList == null) {
                Sender.Tell(
                    new FinishAddOrUpdateTaxFreePackListMessage(
                        message.TaxFreePackListId,
                        message.UserNetId,
                        new List<TaxFree>()
                    ),
                    (IActorRef)message.OriginalSender
                );

                return;
            }

            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IConsignmentItemRepository consignmentItemRepository = _consignmentRepositoriesFactory.NewConsignmentItemRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IProductWriteOffRuleRepository productWriteOffRuleRepository = _productRepositoriesFactory.NewProductWriteOffRuleRepository(connection);
            ISupplyOrderUkraineCartItemRepository cartItemRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemRepository(connection);
            ISupplyOrderUkraineCartItemReservationRepository cartItemReservationRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemReservationRepository(connection);
            ISupplyOrderUkraineCartItemReservationProductPlacementRepository reservationProductPlacementRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemReservationProductPlacementRepository(connection);

            User user = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

            foreach (long addedItemId in message.NewlyAddedItemIds) {
                SupplyOrderUkraineCartItem cartItem = packList.SupplyOrderUkraineCartItems.FirstOrDefault(i => i.Id.Equals(addedItemId));

                if (cartItem == null) continue;

                Product product =
                    getSingleProductRepository
                        .GetByIdAndRuleLocaleWithProductGroupAndWriteOffRules(
                            cartItem.ProductId,
                            packList.Organization.Culture
                        );

                if (product == null) continue;

                ProductWriteOffRule writeOffRule =
                    GetCurrentWriteOffRuleForProduct(
                        product,
                        packList.Organization.Culture,
                        userRepository,
                        productWriteOffRuleRepository
                    );

                IEnumerable<ConsignmentItem> consignmentItems =
                    consignmentItemRepository
                        .GetAllAvailable(
                            packList.Organization.Id,
                            cartItem.ProductId,
                            writeOffRule.RuleType,
                            packList.Organization.Culture
                        );

                if (!consignmentItems.Any()) continue;

                foreach (SupplyOrderUkraineCartItemReservation reservation in cartItem.SupplyOrderUkraineCartItemReservations) {
                    reservation.ProductAvailability.Amount += reservation.Qty;

                    productAvailabilityRepository.Update(reservation.ProductAvailability);

                    reservation.Deleted = true;

                    cartItemReservationRepository.Update(reservation);
                }

                ConsignmentItem firstItem = consignmentItems.First();

                if (firstItem.RemainingQty < cartItem.UploadedQty) {
                    cartItemRepository.Remove(cartItem.Id);

                    foreach (ConsignmentItem consignmentItem in consignmentItems) {
                        ProductAvailability availability =
                            productAvailabilityRepository
                                .GetByProductAndStorageIds(
                                    cartItem.ProductId,
                                    consignmentItem.Consignment.StorageId
                                );

                        if (availability != null) {
                            double currentOperationQty = cartItem.UploadedQty;

                            if (consignmentItem.RemainingQty < cartItem.UploadedQty)
                                currentOperationQty = consignmentItem.RemainingQty;

                            consignmentItem.RemainingQty -= currentOperationQty;

                            consignmentItemRepository.UpdateRemainingQty(consignmentItem);

                            cartItem.UploadedQty -= currentOperationQty;

                            availability.Amount -= currentOperationQty;

                            productAvailabilityRepository.Update(availability);

                            SupplyOrderUkraineCartItem newItem = new SupplyOrderUkraineCartItem {
                                Comment = cartItem.Comment,
                                UploadedQty = currentOperationQty,
                                ItemPriority = cartItem.ItemPriority,
                                ProductId = cartItem.ProductId,
                                CreatedById = cartItem.CreatedById,
                                UpdatedById = cartItem.UpdatedById,
                                ResponsibleId = cartItem.ResponsibleId,
                                ReservedQty = currentOperationQty,
                                FromDate = cartItem.FromDate,
                                TaxFreePackListId = packList.Id,
                                UnpackedQty = currentOperationQty,
                                NetWeight = consignmentItem.Weight,
                                UnitPrice = consignmentItem.Price,
                                SupplierId = cartItem.SupplierId,
                                PackingListPackageOrderItemId = null,
                                MaxQtyPerTF = cartItem.MaxQtyPerTF,
                                IsRecommended = cartItem.IsRecommended
                            };

                            newItem.Id = cartItemRepository.Add(newItem);

                            SupplyOrderUkraineCartItemReservation reservation = new SupplyOrderUkraineCartItemReservation {
                                Qty = currentOperationQty,
                                ConsignmentItemId = consignmentItem.Id,
                                ProductAvailabilityId = availability.Id,
                                SupplyOrderUkraineCartItemId = newItem.Id
                            };

                            reservation.Id = cartItemReservationRepository.Add(reservation);

                            IEnumerable<ProductPlacement> placements =
                                productPlacementRepository
                                    .GetAllByConsignmentItemId(
                                        consignmentItem.Id
                                    );

                            double toMoveQty = currentOperationQty;

                            foreach (ProductPlacement placement in placements) {
                                double currentMoveQty = toMoveQty;

                                if (placement.Qty < currentMoveQty)
                                    currentMoveQty = placement.Qty;

                                placement.Qty -= currentMoveQty;

                                toMoveQty -= currentMoveQty;

                                reservationProductPlacementRepository.Add(new SupplyOrderUkraineCartItemReservationProductPlacement {
                                    Qty = currentMoveQty,
                                    ProductPlacementId = placement.Id,
                                    SupplyOrderUkraineCartItemReservationId = reservation.Id
                                });

                                productPlacementRepository.UpdateQty(placement);

                                if (toMoveQty.Equals(0d)) break;
                            }
                        } else {
                            //Error
                        }

                        if (cartItem.UploadedQty.Equals(0d)) break;
                    }

                    if (cartItem.UploadedQty <= 0d) continue;

                    IEnumerable<ProductAvailability> availabilities =
                        productAvailabilityRepository
                            .GetByProductAndCultureIds(
                                cartItem.ProductId,
                                "pl"
                            );

                    if (!availabilities.Any()) continue;

                    if (availabilities.Sum(a => a.Amount) < cartItem.UploadedQty)
                        cartItem.UploadedQty = availabilities.Sum(a => a.Amount);

                    SupplyOrderUkraineCartItem restItem = new SupplyOrderUkraineCartItem {
                        Comment = cartItem.Comment,
                        UploadedQty = cartItem.UploadedQty,
                        ItemPriority = cartItem.ItemPriority,
                        ProductId = cartItem.ProductId,
                        CreatedById = cartItem.CreatedById,
                        UpdatedById = cartItem.UpdatedById,
                        ResponsibleId = cartItem.ResponsibleId,
                        ReservedQty = cartItem.UploadedQty,
                        FromDate = cartItem.FromDate,
                        TaxFreePackListId = null,
                        UnpackedQty = cartItem.UploadedQty,
                        NetWeight = cartItem.NetWeight,
                        UnitPrice = cartItem.UnitPrice,
                        SupplierId = cartItem.SupplierId,
                        PackingListPackageOrderItemId = null,
                        MaxQtyPerTF = cartItem.MaxQtyPerTF,
                        IsRecommended = cartItem.IsRecommended
                    };

                    restItem.Id = cartItemRepository.Add(restItem);

                    foreach (ProductAvailability availability in availabilities) {
                        SupplyOrderUkraineCartItemReservation reservation = new SupplyOrderUkraineCartItemReservation {
                            ProductAvailabilityId = availability.Id,
                            SupplyOrderUkraineCartItemId = restItem.Id
                        };

                        if (availability.Amount < cartItem.UploadedQty) {
                            cartItem.UploadedQty -= availability.Amount;

                            reservation.Qty = availability.Amount;

                            availability.Amount = 0;

                            productAvailabilityRepository.Update(availability);
                        } else {
                            reservation.Qty = cartItem.UploadedQty;

                            availability.Amount -= cartItem.UploadedQty;

                            productAvailabilityRepository.Update(availability);

                            cartItem.UploadedQty = 0d;
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

                        if (cartItem.UploadedQty.Equals(0d)) break;
                    }
                } else {
                    ConsignmentItem consignmentItem = consignmentItems.First();

                    ProductAvailability availability =
                        productAvailabilityRepository
                            .GetByProductAndStorageIds(
                                cartItem.ProductId,
                                consignmentItem.Consignment.StorageId
                            );

                    if (availability != null) {
                        double currentOperationQty = cartItem.UploadedQty;

                        consignmentItem.RemainingQty -= currentOperationQty;

                        consignmentItemRepository.UpdateRemainingQty(consignmentItem);

                        // cartItem.UploadedQty -= currentOperationQty;
                        cartItem.UnpackedQty = cartItem.ReservedQty;

                        availability.Amount -= currentOperationQty;

                        productAvailabilityRepository.Update(availability);

                        cartItem.UnitPrice = consignmentItem.Price;
                        cartItem.NetWeight = consignmentItem.Weight;

                        cartItemRepository.Update(cartItem);

                        SupplyOrderUkraineCartItemReservation reservation = new SupplyOrderUkraineCartItemReservation {
                            Qty = currentOperationQty,
                            ConsignmentItemId = consignmentItem.Id,
                            ProductAvailabilityId = availability.Id,
                            SupplyOrderUkraineCartItemId = cartItem.Id
                        };

                        reservation.Id = cartItemReservationRepository.Add(reservation);

                        IEnumerable<ProductPlacement> placements =
                            productPlacementRepository
                                .GetAllByConsignmentItemId(
                                    consignmentItem.Id
                                );

                        double toMoveQty = currentOperationQty;

                        foreach (ProductPlacement placement in placements) {
                            double currentMoveQty = toMoveQty;

                            if (placement.Qty < currentMoveQty)
                                currentMoveQty = placement.Qty;

                            placement.Qty -= currentMoveQty;

                            toMoveQty -= currentMoveQty;

                            reservationProductPlacementRepository.Add(new SupplyOrderUkraineCartItemReservationProductPlacement {
                                Qty = currentMoveQty,
                                ProductPlacementId = placement.Id,
                                SupplyOrderUkraineCartItemReservationId = reservation.Id
                            });

                            productPlacementRepository.UpdateQty(placement);

                            if (toMoveQty.Equals(0d)) break;
                        }
                    } else {
                        //Error
                    }
                }
            }

            foreach (SupplyOrderUkraineCartItem updatedItem in message.UpdatedItems) {
                SupplyOrderUkraineCartItem cartItem = packList.SupplyOrderUkraineCartItems.FirstOrDefault(i => i.Id.Equals(updatedItem.Id));

                if (cartItem == null) continue;

                double changedQty = updatedItem.UnpackedQty - cartItem.UnpackedQty;

                if (changedQty > 0) {
                    Product product =
                        getSingleProductRepository
                            .GetByIdAndRuleLocaleWithProductGroupAndWriteOffRules(
                                cartItem.ProductId,
                                packList.Organization.Culture
                            );

                    if (product == null) continue;

                    ProductWriteOffRule writeOffRule =
                        GetCurrentWriteOffRuleForProduct(
                            product,
                            packList.Organization.Culture,
                            userRepository,
                            productWriteOffRuleRepository
                        );

                    IEnumerable<ConsignmentItem> consignmentItems =
                        consignmentItemRepository
                            .GetAllAvailable(
                                packList.Organization.Id,
                                cartItem.ProductId,
                                writeOffRule.RuleType,
                                packList.Organization.Culture
                            );

                    if (consignmentItems.Any()) {
                        foreach (ConsignmentItem consignmentItem in consignmentItems) {
                            ProductAvailability availability =
                                productAvailabilityRepository
                                    .GetByProductAndStorageIds(
                                        cartItem.ProductId,
                                        consignmentItem.Consignment.StorageId
                                    );

                            if (availability != null) {
                                double operationQty = changedQty;

                                if (consignmentItem.RemainingQty < operationQty)
                                    operationQty = consignmentItem.RemainingQty;

                                SupplyOrderUkraineCartItem existingItem =
                                    cartItemRepository
                                        .GetAssignedItemByTaxFreePackListAndConsignmentItemIfExists(
                                            packList.Id,
                                            consignmentItem.Id
                                        );

                                if (existingItem != null) {
                                    existingItem.UploadedQty += operationQty;
                                    existingItem.ReservedQty += operationQty;
                                    existingItem.UnpackedQty += operationQty;

                                    cartItemRepository.Update(existingItem);

                                    SupplyOrderUkraineCartItemReservation existingReservation =
                                        cartItemReservationRepository
                                            .GetByIdsIfExists(
                                                existingItem.Id,
                                                availability.Id,
                                                consignmentItem.Id
                                            );

                                    if (existingReservation == null) {
                                        SupplyOrderUkraineCartItemReservation reservation = new SupplyOrderUkraineCartItemReservation {
                                            ConsignmentItemId = consignmentItem.Id,
                                            ProductAvailabilityId = availability.Id,
                                            SupplyOrderUkraineCartItemId = existingItem.Id,
                                            Qty = operationQty
                                        };

                                        reservation.Id = cartItemReservationRepository.Add(reservation);

                                        IEnumerable<ProductPlacement> placements =
                                            productPlacementRepository
                                                .GetAllByConsignmentItemId(
                                                    consignmentItem.Id
                                                );

                                        double toMoveQty = operationQty;

                                        foreach (ProductPlacement placement in placements) {
                                            double currentMoveQty = toMoveQty;

                                            if (placement.Qty < currentMoveQty)
                                                currentMoveQty = placement.Qty;

                                            placement.Qty -= currentMoveQty;

                                            toMoveQty -= currentMoveQty;

                                            reservationProductPlacementRepository.Add(new SupplyOrderUkraineCartItemReservationProductPlacement {
                                                Qty = currentMoveQty,
                                                ProductPlacementId = placement.Id,
                                                SupplyOrderUkraineCartItemReservationId = reservation.Id
                                            });

                                            productPlacementRepository.UpdateQty(placement);

                                            if (toMoveQty.Equals(0d)) break;
                                        }
                                    } else {
                                        existingReservation.Qty += operationQty;

                                        cartItemReservationRepository.Update(existingReservation);

                                        IEnumerable<ProductPlacement> placements =
                                            productPlacementRepository
                                                .GetAllByConsignmentItemId(
                                                    consignmentItem.Id
                                                );

                                        double toMoveQty = operationQty;

                                        foreach (ProductPlacement placement in placements) {
                                            double currentMoveQty = toMoveQty;

                                            if (placement.Qty < currentMoveQty)
                                                currentMoveQty = placement.Qty;

                                            placement.Qty -= currentMoveQty;

                                            toMoveQty -= currentMoveQty;

                                            reservationProductPlacementRepository.Add(new SupplyOrderUkraineCartItemReservationProductPlacement {
                                                Qty = currentMoveQty,
                                                ProductPlacementId = placement.Id,
                                                SupplyOrderUkraineCartItemReservationId = existingReservation.Id
                                            });

                                            productPlacementRepository.UpdateQty(placement);

                                            if (toMoveQty.Equals(0d)) break;
                                        }
                                    }
                                } else {
                                    SupplyOrderUkraineCartItem newItem = new SupplyOrderUkraineCartItem {
                                        Comment = cartItem.Comment,
                                        UploadedQty = operationQty,
                                        ItemPriority = cartItem.ItemPriority,
                                        ProductId = cartItem.ProductId,
                                        CreatedById = cartItem.CreatedById,
                                        UpdatedById = cartItem.UpdatedById,
                                        ResponsibleId = cartItem.ResponsibleId,
                                        ReservedQty = operationQty,
                                        FromDate = cartItem.FromDate,
                                        TaxFreePackListId = packList.Id,
                                        UnpackedQty = operationQty,
                                        NetWeight = consignmentItem.Weight,
                                        UnitPrice = consignmentItem.Price,
                                        SupplierId = cartItem.SupplierId,
                                        PackingListPackageOrderItemId = null,
                                        MaxQtyPerTF = cartItem.MaxQtyPerTF,
                                        IsRecommended = cartItem.IsRecommended
                                    };

                                    newItem.Id = cartItemRepository.Add(newItem);

                                    SupplyOrderUkraineCartItemReservation reservation = new SupplyOrderUkraineCartItemReservation {
                                        ConsignmentItemId = consignmentItem.Id,
                                        ProductAvailabilityId = availability.Id,
                                        SupplyOrderUkraineCartItemId = newItem.Id,
                                        Qty = operationQty
                                    };

                                    reservation.Id = cartItemReservationRepository.Add(reservation);

                                    IEnumerable<ProductPlacement> placements =
                                        productPlacementRepository
                                            .GetAllByConsignmentItemId(
                                                consignmentItem.Id
                                            );

                                    double toMoveQty = operationQty;

                                    foreach (ProductPlacement placement in placements) {
                                        double currentMoveQty = toMoveQty;

                                        if (placement.Qty < currentMoveQty)
                                            currentMoveQty = placement.Qty;

                                        placement.Qty -= currentMoveQty;

                                        toMoveQty -= currentMoveQty;

                                        reservationProductPlacementRepository.Add(new SupplyOrderUkraineCartItemReservationProductPlacement {
                                            Qty = currentMoveQty,
                                            ProductPlacementId = placement.Id,
                                            SupplyOrderUkraineCartItemReservationId = reservation.Id
                                        });

                                        productPlacementRepository.UpdateQty(placement);

                                        if (toMoveQty.Equals(0d)) break;
                                    }
                                }

                                consignmentItem.RemainingQty -= operationQty;

                                consignmentItemRepository.UpdateRemainingQty(consignmentItem);

                                availability.Amount -= operationQty;

                                productAvailabilityRepository.Update(availability);

                                changedQty -= operationQty;

                                if (changedQty.Equals(0d)) break;
                            } else {
                                //Error
                            }
                        }
                    }

                    if (changedQty.Equals(0d)) continue;

                    SupplyOrderUkraineCartItem newCartItem = new SupplyOrderUkraineCartItem {
                        Comment = cartItem.Comment,
                        UploadedQty = changedQty,
                        ItemPriority = cartItem.ItemPriority,
                        ProductId = cartItem.ProductId,
                        CreatedById = cartItem.CreatedById,
                        UpdatedById = cartItem.UpdatedById,
                        ResponsibleId = cartItem.ResponsibleId,
                        ReservedQty = changedQty,
                        FromDate = cartItem.FromDate,
                        UnpackedQty = changedQty,
                        SupplierId = cartItem.SupplierId,
                        MaxQtyPerTF = cartItem.MaxQtyPerTF,
                        IsRecommended = cartItem.IsRecommended,
                        TaxFreePackListId = packList.Id,
                        PackingListPackageOrderItemId = null,
                        NetWeight = 0d,
                        UnitPrice = 0m
                    };

                    IEnumerable<ProductAvailability> availabilities =
                        productAvailabilityRepository
                            .GetByProductAndCultureIds(
                                newCartItem.ProductId,
                                "pl"
                            );

                    if (!availabilities.Any()) continue;

                    List<SupplyOrderUkraineCartItemReservation> reservations = new List<SupplyOrderUkraineCartItemReservation>();

                    foreach (ProductAvailability availability in availabilities) {
                        if (availability.Amount < changedQty) {
                            changedQty -= availability.Amount;

                            reservations.Add(new SupplyOrderUkraineCartItemReservation {
                                Qty = availability.Amount,
                                ProductAvailabilityId = availability.Id
                            });

                            availability.Amount = 0;

                            productAvailabilityRepository.Update(availability);
                        } else {
                            reservations.Add(new SupplyOrderUkraineCartItemReservation {
                                Qty = changedQty,
                                ProductAvailabilityId = availability.Id
                            });

                            availability.Amount -= changedQty;

                            productAvailabilityRepository.Update(availability);

                            changedQty = 0d;
                        }

                        if (changedQty.Equals(0d)) break;
                    }

                    if (changedQty > 0) {
                        newCartItem.UploadedQty -= changedQty;
                        newCartItem.ReservedQty -= changedQty;
                        newCartItem.UnpackedQty -= changedQty;
                    }

                    SupplyOrderUkraineCartItem fromDb =
                        cartItemRepository
                            .GetByProductAndTaxFreePackListIdsIfExists(
                                newCartItem.ProductId,
                                packList.Id
                            );

                    if (fromDb != null) {
                        fromDb.ReservedQty += newCartItem.ReservedQty;
                        fromDb.UploadedQty += newCartItem.UploadedQty;
                        fromDb.UnpackedQty += newCartItem.UnpackedQty;
                        fromDb.UpdatedById = user.Id;

                        cartItemRepository.Update(fromDb);

                        foreach (SupplyOrderUkraineCartItemReservation reservation in reservations) {
                            SupplyOrderUkraineCartItemReservation fromDbReservation =
                                cartItemReservationRepository
                                    .GetByIdsIfExists(
                                        fromDb.Id,
                                        reservation.ProductAvailabilityId
                                    );

                            if (fromDbReservation != null) {
                                fromDbReservation.Qty += reservation.Qty;

                                cartItemReservationRepository.Update(fromDbReservation);
                            } else {
                                reservation.SupplyOrderUkraineCartItemId = fromDb.Id;

                                cartItemReservationRepository.Add(reservation);
                            }
                        }
                    } else {
                        newCartItem.CreatedById = user.Id;

                        newCartItem.Id = cartItemRepository.Add(newCartItem);

                        cartItemReservationRepository.Add(reservations.Select(reservation => {
                            reservation.SupplyOrderUkraineCartItemId = newCartItem.Id;

                            return reservation;
                        }));
                    }
                } else {
                    double decreasedQty = changedQty = 0 - changedQty;

                    foreach (SupplyOrderUkraineCartItemReservation reservation in cartItem.SupplyOrderUkraineCartItemReservations) {
                        double currentOperationQty = changedQty;

                        if (reservation.Qty < currentOperationQty)
                            currentOperationQty = reservation.Qty;

                        reservation.ProductAvailability.Amount += currentOperationQty;

                        productAvailabilityRepository.Update(reservation.ProductAvailability);

                        if (reservation.ConsignmentItem != null) {
                            reservation.ConsignmentItem.RemainingQty += currentOperationQty;

                            consignmentItemRepository.UpdateRemainingQty(reservation.ConsignmentItem);
                        }

                        reservation.Qty -= currentOperationQty;

                        if (reservation.Qty.Equals(0d))
                            reservation.Deleted = true;

                        cartItemReservationRepository.Update(reservation);

                        changedQty -= currentOperationQty;

                        IEnumerable<SupplyOrderUkraineCartItemReservationProductPlacement> placements =
                            reservationProductPlacementRepository
                                .GetAllByReservationId(
                                    reservation.Id
                                );

                        foreach (SupplyOrderUkraineCartItemReservationProductPlacement placement in placements) {
                            double restoreQty = currentOperationQty;

                            if (placement.Qty < restoreQty)
                                restoreQty = placement.Qty;

                            placement.Qty -= restoreQty;

                            if (placement.Qty.Equals(0d))
                                placement.Deleted = true;

                            reservationProductPlacementRepository.Update(placement);

                            placement.ProductPlacement.Qty += restoreQty;

                            if (placement.ProductPlacement.Deleted)
                                productPlacementRepository.Restore(placement.ProductPlacement);
                            else
                                productPlacementRepository.UpdateQty(placement.ProductPlacement);

                            currentOperationQty -= restoreQty;

                            if (currentOperationQty.Equals(0d)) break;
                        }

                        if (changedQty.Equals(0d)) break;
                    }

                    cartItem.ReservedQty -= decreasedQty;
                    cartItem.UploadedQty -= decreasedQty;
                    cartItem.UnpackedQty -= decreasedQty;

                    cartItemRepository.Update(cartItem);

                    SupplyOrderUkraineCartItem newCartItem = new SupplyOrderUkraineCartItem {
                        Comment = cartItem.Comment,
                        UploadedQty = decreasedQty,
                        ItemPriority = cartItem.ItemPriority,
                        ProductId = cartItem.ProductId,
                        CreatedById = cartItem.CreatedById,
                        UpdatedById = cartItem.UpdatedById,
                        ResponsibleId = cartItem.ResponsibleId,
                        ReservedQty = decreasedQty,
                        FromDate = cartItem.FromDate,
                        UnpackedQty = decreasedQty,
                        SupplierId = cartItem.SupplierId,
                        MaxQtyPerTF = cartItem.MaxQtyPerTF,
                        IsRecommended = cartItem.IsRecommended,
                        TaxFreePackListId = null,
                        PackingListPackageOrderItemId = null,
                        NetWeight = 0d,
                        UnitPrice = 0m
                    };

                    IEnumerable<ProductAvailability> availabilities =
                        productAvailabilityRepository
                            .GetByProductAndCultureIds(
                                newCartItem.ProductId,
                                "pl"
                            );

                    List<SupplyOrderUkraineCartItemReservation> reservations = new List<SupplyOrderUkraineCartItemReservation>();

                    foreach (ProductAvailability availability in availabilities) {
                        if (availability.Amount < decreasedQty) {
                            decreasedQty -= availability.Amount;

                            reservations.Add(new SupplyOrderUkraineCartItemReservation {
                                Qty = availability.Amount,
                                ProductAvailabilityId = availability.Id
                            });

                            availability.Amount = 0;

                            productAvailabilityRepository.Update(availability);
                        } else {
                            reservations.Add(new SupplyOrderUkraineCartItemReservation {
                                Qty = decreasedQty,
                                ProductAvailabilityId = availability.Id
                            });

                            availability.Amount -= decreasedQty;

                            productAvailabilityRepository.Update(availability);

                            decreasedQty = 0d;
                        }

                        if (decreasedQty.Equals(0d)) break;
                    }

                    SupplyOrderUkraineCartItem fromDb = cartItemRepository.GetByProductIdIfExists(newCartItem.ProductId);

                    if (fromDb != null) {
                        fromDb.ReservedQty += newCartItem.ReservedQty;
                        fromDb.UnpackedQty += newCartItem.UnpackedQty;
                        fromDb.UploadedQty += newCartItem.UploadedQty;
                        fromDb.UpdatedById = user.Id;

                        cartItemRepository.Update(fromDb);

                        foreach (SupplyOrderUkraineCartItemReservation reservation in reservations) {
                            SupplyOrderUkraineCartItemReservation fromDbReservation =
                                cartItemReservationRepository
                                    .GetByIdsIfExists(
                                        fromDb.Id,
                                        reservation.ProductAvailabilityId
                                    );

                            if (fromDbReservation != null) {
                                fromDbReservation.Qty += reservation.Qty;

                                cartItemReservationRepository.Update(fromDbReservation);
                            } else {
                                reservation.SupplyOrderUkraineCartItemId = fromDb.Id;

                                cartItemReservationRepository.Add(reservation);
                            }
                        }
                    } else {
                        newCartItem.CreatedById = user.Id;

                        newCartItem.Id = cartItemRepository.Add(newCartItem);

                        cartItemReservationRepository.Add(reservations.Select(reservation => {
                            reservation.SupplyOrderUkraineCartItemId = newCartItem.Id;

                            return reservation;
                        }));
                    }
                }
            }

            foreach (SupplyOrderUkraineCartItem deletedItem in message.DeletedItems) {
                foreach (SupplyOrderUkraineCartItemReservation reservation in deletedItem.SupplyOrderUkraineCartItemReservations) {
                    reservation.ProductAvailability.Amount += reservation.Qty;

                    productAvailabilityRepository.Update(reservation.ProductAvailability);

                    if (reservation.ConsignmentItem != null) {
                        reservation.ConsignmentItem.RemainingQty += reservation.Qty;

                        consignmentItemRepository.UpdateRemainingQty(reservation.ConsignmentItem);
                    }

                    IEnumerable<SupplyOrderUkraineCartItemReservationProductPlacement> placements =
                        reservationProductPlacementRepository
                            .GetAllByReservationId(
                                reservation.Id
                            );

                    foreach (SupplyOrderUkraineCartItemReservationProductPlacement placement in placements) {
                        placement.ProductPlacement.Qty += placement.Qty;

                        if (placement.ProductPlacement.Deleted)
                            productPlacementRepository.Restore(placement.ProductPlacement);
                        else
                            productPlacementRepository.UpdateQty(placement.ProductPlacement);

                        placement.Qty = 0d;
                        placement.Deleted = true;

                        reservationProductPlacementRepository.Update(placement);
                    }

                    reservation.Deleted = true;

                    cartItemReservationRepository.Update(reservation);
                }

                cartItemRepository.Remove(deletedItem.Id);

                IEnumerable<ProductAvailability> availabilities =
                    productAvailabilityRepository
                        .GetByProductAndCultureIds(
                            deletedItem.ProductId,
                            "pl"
                        );

                List<SupplyOrderUkraineCartItemReservation> reservations = new List<SupplyOrderUkraineCartItemReservation>();

                foreach (ProductAvailability availability in availabilities) {
                    if (availability.Amount < deletedItem.ReservedQty) {
                        deletedItem.ReservedQty -= availability.Amount;

                        reservations.Add(new SupplyOrderUkraineCartItemReservation {
                            Qty = availability.Amount,
                            ProductAvailabilityId = availability.Id
                        });

                        availability.Amount = 0;

                        productAvailabilityRepository.Update(availability);
                    } else {
                        reservations.Add(new SupplyOrderUkraineCartItemReservation {
                            Qty = deletedItem.ReservedQty,
                            ProductAvailabilityId = availability.Id
                        });

                        availability.Amount -= deletedItem.ReservedQty;

                        productAvailabilityRepository.Update(availability);

                        deletedItem.ReservedQty = 0d;
                    }

                    if (deletedItem.ReservedQty.Equals(0d)) break;
                }

                SupplyOrderUkraineCartItem existingItem =
                    cartItemRepository
                        .GetByProductIdIfExists(
                            deletedItem.ProductId
                        );

                if (existingItem != null) {
                    existingItem.ReservedQty += deletedItem.ReservedQty;
                    existingItem.UploadedQty += deletedItem.ReservedQty;
                    existingItem.UnpackedQty += deletedItem.ReservedQty;

                    cartItemRepository.Update(existingItem);

                    foreach (SupplyOrderUkraineCartItemReservation reservation in reservations) {
                        SupplyOrderUkraineCartItemReservation fromDbReservation =
                            cartItemReservationRepository
                                .GetByIdsIfExists(
                                    existingItem.Id,
                                    reservation.ProductAvailabilityId
                                );

                        if (fromDbReservation != null) {
                            fromDbReservation.Qty += reservation.Qty;

                            cartItemReservationRepository.Update(fromDbReservation);
                        } else {
                            reservation.SupplyOrderUkraineCartItemId = existingItem.Id;

                            cartItemReservationRepository.Add(reservation);
                        }
                    }
                } else {
                    SupplyOrderUkraineCartItem newCartItem = new SupplyOrderUkraineCartItem {
                        Comment = deletedItem.Comment,
                        UploadedQty = deletedItem.ReservedQty,
                        ItemPriority = deletedItem.ItemPriority,
                        ProductId = deletedItem.ProductId,
                        CreatedById = deletedItem.CreatedById,
                        UpdatedById = deletedItem.UpdatedById,
                        ResponsibleId = deletedItem.ResponsibleId,
                        ReservedQty = deletedItem.ReservedQty,
                        FromDate = deletedItem.FromDate,
                        UnpackedQty = deletedItem.ReservedQty,
                        SupplierId = deletedItem.SupplierId,
                        MaxQtyPerTF = deletedItem.MaxQtyPerTF,
                        IsRecommended = deletedItem.IsRecommended,
                        TaxFreePackListId = null,
                        PackingListPackageOrderItemId = null,
                        NetWeight = 0d,
                        UnitPrice = 0m
                    };

                    newCartItem.CreatedById = user.Id;

                    newCartItem.Id = cartItemRepository.Add(newCartItem);

                    cartItemReservationRepository.Add(reservations.Select(reservation => {
                        reservation.SupplyOrderUkraineCartItemId = newCartItem.Id;

                        return reservation;
                    }));
                }
            }

            Sender.Tell(
                new FinishAddOrUpdateTaxFreePackListMessage(
                    packList.Id,
                    message.UserNetId,
                    message.TaxFrees
                ),
                (IActorRef)message.OriginalSender
            );
        } catch (Exception exc) {
            ((IActorRef)message.OriginalSender).Tell(exc);
        }
    }

    private void ProcessRestoreReservationsOnConsignmentFromTaxFreePackListDeleteMessage(RestoreReservationsOnConsignmentFromTaxFreePackListDeleteMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        TaxFreePackList packList =
            _supplyUkraineRepositoriesFactory
                .NewTaxFreePackListRepository(connection, null)
                .GetByIdForConsignment(
                    message.TaxFreePackListId
                );

        if (packList == null) {
            return;
        }

        IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
        IConsignmentItemRepository consignmentItemRepository = _consignmentRepositoriesFactory.NewConsignmentItemRepository(connection);
        IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
        ISupplyOrderUkraineCartItemRepository cartItemRepository =
            _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemRepository(connection);
        ISupplyOrderUkraineCartItemReservationRepository cartItemReservationRepository =
            _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemReservationRepository(connection);
        ISupplyOrderUkraineCartItemReservationProductPlacementRepository reservationProductPlacementRepository =
            _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemReservationProductPlacementRepository(connection);

        User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

        foreach (SupplyOrderUkraineCartItem deletedItem in packList.SupplyOrderUkraineCartItems) {
            foreach (SupplyOrderUkraineCartItemReservation reservation in deletedItem.SupplyOrderUkraineCartItemReservations) {
                reservation.ProductAvailability.Amount += reservation.Qty;

                productAvailabilityRepository.Update(reservation.ProductAvailability);

                if (reservation.ConsignmentItem != null) {
                    reservation.ConsignmentItem.RemainingQty += reservation.Qty;

                    consignmentItemRepository.UpdateRemainingQty(reservation.ConsignmentItem);
                }

                IEnumerable<SupplyOrderUkraineCartItemReservationProductPlacement> placements =
                    reservationProductPlacementRepository
                        .GetAllByReservationId(
                            reservation.Id
                        );

                foreach (SupplyOrderUkraineCartItemReservationProductPlacement placement in placements) {
                    placement.ProductPlacement.Qty += placement.Qty;

                    if (placement.ProductPlacement.Deleted)
                        productPlacementRepository.Restore(placement.ProductPlacement);
                    else
                        productPlacementRepository.UpdateQty(placement.ProductPlacement);

                    placement.Qty = 0d;
                    placement.Deleted = true;

                    reservationProductPlacementRepository.Update(placement);
                }

                reservation.Deleted = true;

                cartItemReservationRepository.Update(reservation);
            }

            cartItemRepository.Remove(deletedItem.Id);

            IEnumerable<ProductAvailability> availabilities =
                productAvailabilityRepository
                    .GetByProductAndCultureIds(
                        deletedItem.ProductId,
                        "pl"
                    );

            List<SupplyOrderUkraineCartItemReservation> reservations = new List<SupplyOrderUkraineCartItemReservation>();

            foreach (ProductAvailability availability in availabilities) {
                if (availability.Amount < deletedItem.ReservedQty) {
                    deletedItem.ReservedQty -= availability.Amount;

                    reservations.Add(new SupplyOrderUkraineCartItemReservation {
                        Qty = availability.Amount,
                        ProductAvailabilityId = availability.Id
                    });

                    availability.Amount = 0;

                    productAvailabilityRepository.Update(availability);
                } else {
                    reservations.Add(new SupplyOrderUkraineCartItemReservation {
                        Qty = deletedItem.ReservedQty,
                        ProductAvailabilityId = availability.Id
                    });

                    availability.Amount -= deletedItem.ReservedQty;

                    productAvailabilityRepository.Update(availability);

                    deletedItem.ReservedQty = 0d;
                }

                if (deletedItem.ReservedQty.Equals(0d)) break;
            }

            SupplyOrderUkraineCartItem existingItem =
                cartItemRepository
                    .GetByProductIdIfExists(
                        deletedItem.ProductId
                    );

            if (existingItem != null) {
                existingItem.ReservedQty += deletedItem.ReservedQty;
                existingItem.UploadedQty += deletedItem.ReservedQty;
                existingItem.UnpackedQty += deletedItem.ReservedQty;

                cartItemRepository.Update(existingItem);

                foreach (SupplyOrderUkraineCartItemReservation reservation in reservations) {
                    SupplyOrderUkraineCartItemReservation fromDbReservation =
                        cartItemReservationRepository
                            .GetByIdsIfExists(
                                existingItem.Id,
                                reservation.ProductAvailabilityId
                            );

                    if (fromDbReservation != null) {
                        fromDbReservation.Qty += reservation.Qty;

                        cartItemReservationRepository.Update(fromDbReservation);
                    } else {
                        reservation.SupplyOrderUkraineCartItemId = existingItem.Id;

                        cartItemReservationRepository.Add(reservation);
                    }
                }
            } else {
                SupplyOrderUkraineCartItem newCartItem = new SupplyOrderUkraineCartItem {
                    Comment = deletedItem.Comment,
                    UploadedQty = deletedItem.ReservedQty,
                    ItemPriority = deletedItem.ItemPriority,
                    ProductId = deletedItem.ProductId,
                    CreatedById = deletedItem.CreatedById,
                    UpdatedById = deletedItem.UpdatedById,
                    ResponsibleId = deletedItem.ResponsibleId,
                    ReservedQty = deletedItem.ReservedQty,
                    FromDate = deletedItem.FromDate,
                    UnpackedQty = deletedItem.ReservedQty,
                    SupplierId = deletedItem.SupplierId,
                    MaxQtyPerTF = deletedItem.MaxQtyPerTF,
                    IsRecommended = deletedItem.IsRecommended,
                    TaxFreePackListId = null,
                    PackingListPackageOrderItemId = null,
                    NetWeight = 0d,
                    UnitPrice = 0m
                };

                newCartItem.CreatedById = user.Id;

                newCartItem.Id = cartItemRepository.Add(newCartItem);

                cartItemReservationRepository.Add(reservations.Select(reservation => {
                    reservation.SupplyOrderUkraineCartItemId = newCartItem.Id;

                    return reservation;
                }));
            }
        }
    }

    private void ProcessRestoreReservationsOnConsignmentFromUnpackedTaxFreePackListCartItemsMessage(
        RestoreReservationsOnConsignmentFromUnpackedTaxFreePackListCartItemsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        TaxFreePackList packList =
            _supplyUkraineRepositoriesFactory
                .NewTaxFreePackListRepository(connection, null)
                .GetByIdForConsignment(
                    message.TaxFreePackListId
                );

        if (packList == null) {
            return;
        }

        IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
        IConsignmentItemRepository consignmentItemRepository = _consignmentRepositoriesFactory.NewConsignmentItemRepository(connection);
        IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
        ISupplyOrderUkraineCartItemRepository cartItemRepository =
            _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemRepository(connection);
        ISupplyOrderUkraineCartItemReservationRepository cartItemReservationRepository =
            _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemReservationRepository(connection);
        ISupplyOrderUkraineCartItemReservationProductPlacementRepository reservationProductPlacementRepository =
            _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemReservationProductPlacementRepository(connection);

        User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

        foreach (SupplyOrderUkraineCartItem cartItem in packList.SupplyOrderUkraineCartItems.Where(i => i.UnpackedQty > 0d)) {
            double operationQty = cartItem.UnpackedQty;

            foreach (SupplyOrderUkraineCartItemReservation reservation in cartItem.SupplyOrderUkraineCartItemReservations) {
                double currentOperationQty = operationQty;

                if (reservation.Qty < currentOperationQty)
                    currentOperationQty = reservation.Qty;

                reservation.ProductAvailability.Amount += currentOperationQty;

                productAvailabilityRepository.Update(reservation.ProductAvailability);

                if (reservation.ConsignmentItem != null) {
                    reservation.ConsignmentItem.RemainingQty += currentOperationQty;

                    consignmentItemRepository.UpdateRemainingQty(reservation.ConsignmentItem);
                }

                reservation.Qty -= currentOperationQty;

                if (reservation.Qty.Equals(0d))
                    reservation.Deleted = true;

                cartItemReservationRepository.Update(reservation);

                IEnumerable<SupplyOrderUkraineCartItemReservationProductPlacement> placements =
                    reservationProductPlacementRepository
                        .GetAllByReservationId(
                            reservation.Id
                        );

                foreach (SupplyOrderUkraineCartItemReservationProductPlacement placement in placements) {
                    double restoreQty = currentOperationQty;

                    if (placement.Qty < restoreQty)
                        restoreQty = placement.Qty;

                    placement.Qty -= restoreQty;

                    if (placement.Qty.Equals(0d))
                        placement.Deleted = true;

                    reservationProductPlacementRepository.Update(placement);

                    placement.ProductPlacement.Qty += restoreQty;

                    if (placement.ProductPlacement.Deleted)
                        productPlacementRepository.Restore(placement.ProductPlacement);
                    else
                        productPlacementRepository.UpdateQty(placement.ProductPlacement);

                    currentOperationQty -= restoreQty;

                    if (currentOperationQty.Equals(0d)) break;
                }

                operationQty -= currentOperationQty;
            }

            operationQty = cartItem.UnpackedQty;

            cartItem.ReservedQty -= operationQty;
            cartItem.UnpackedQty -= operationQty;
            cartItem.UploadedQty -= operationQty;

            if (cartItem.ReservedQty.Equals(0d)) {
                cartItemRepository.Remove(cartItem.Id);
            } else {
                cartItemRepository.Update(cartItem);
            }

            IEnumerable<ProductAvailability> availabilities =
                productAvailabilityRepository
                    .GetByProductAndCultureIds(
                        cartItem.ProductId,
                        "pl"
                    );

            if (!availabilities.Any()) continue;

            List<SupplyOrderUkraineCartItemReservation> reservations = new List<SupplyOrderUkraineCartItemReservation>();

            double toReserveQty = operationQty;

            foreach (ProductAvailability availability in availabilities) {
                if (availability.Amount < toReserveQty) {
                    toReserveQty -= availability.Amount;

                    reservations.Add(new SupplyOrderUkraineCartItemReservation {
                        Qty = availability.Amount,
                        ProductAvailabilityId = availability.Id
                    });

                    availability.Amount = 0;

                    productAvailabilityRepository.Update(availability);
                } else {
                    reservations.Add(new SupplyOrderUkraineCartItemReservation {
                        Qty = toReserveQty,
                        ProductAvailabilityId = availability.Id
                    });

                    availability.Amount -= toReserveQty;

                    productAvailabilityRepository.Update(availability);

                    toReserveQty = 0d;
                }

                if (toReserveQty.Equals(0d)) break;
            }

            SupplyOrderUkraineCartItem existingItem =
                cartItemRepository
                    .GetByProductIdIfExists(
                        cartItem.ProductId
                    );

            if (existingItem != null) {
                existingItem.ReservedQty += operationQty;
                existingItem.UploadedQty += operationQty;
                existingItem.UnpackedQty += operationQty;

                cartItemRepository.Update(existingItem);

                foreach (SupplyOrderUkraineCartItemReservation reservation in reservations) {
                    SupplyOrderUkraineCartItemReservation fromDbReservation =
                        cartItemReservationRepository
                            .GetByIdsIfExists(
                                existingItem.Id,
                                reservation.ProductAvailabilityId
                            );

                    if (fromDbReservation != null) {
                        fromDbReservation.Qty += reservation.Qty;

                        cartItemReservationRepository.Update(fromDbReservation);
                    } else {
                        reservation.SupplyOrderUkraineCartItemId = existingItem.Id;

                        cartItemReservationRepository.Add(reservation);
                    }
                }
            } else {
                SupplyOrderUkraineCartItem newCartItem = new SupplyOrderUkraineCartItem {
                    Comment = cartItem.Comment,
                    UploadedQty = operationQty,
                    ItemPriority = cartItem.ItemPriority,
                    ProductId = cartItem.ProductId,
                    CreatedById = cartItem.CreatedById,
                    UpdatedById = cartItem.UpdatedById,
                    ResponsibleId = cartItem.ResponsibleId,
                    ReservedQty = operationQty,
                    FromDate = cartItem.FromDate,
                    UnpackedQty = operationQty,
                    SupplierId = cartItem.SupplierId,
                    MaxQtyPerTF = cartItem.MaxQtyPerTF,
                    IsRecommended = cartItem.IsRecommended,
                    TaxFreePackListId = null,
                    PackingListPackageOrderItemId = null,
                    NetWeight = 0d,
                    UnitPrice = 0m
                };

                newCartItem.CreatedById = user.Id;

                newCartItem.Id = cartItemRepository.Add(newCartItem);

                cartItemReservationRepository.Add(reservations.Select(reservation => {
                    reservation.SupplyOrderUkraineCartItemId = newCartItem.Id;

                    return reservation;
                }));
            }
        }
    }

    private static void ReserveConsignmentsForNewSadItem(
        IGetSingleProductRepository getSingleProductRepository,
        SadItem sadItem,
        Sad sad,
        IUserRepository userRepository,
        IProductWriteOffRuleRepository productWriteOffRuleRepository,
        IConsignmentItemRepository consignmentItemRepository,
        IProductAvailabilityRepository productAvailabilityRepository,
        ISupplyOrderUkraineCartItemReservationRepository cartItemReservationRepository,
        ISadItemRepository sadItemRepository,
        ISupplyOrderUkraineCartItemRepository cartItemRepository,
        IProductPlacementRepository productPlacementRepository,
        ISupplyOrderUkraineCartItemReservationProductPlacementRepository reservationProductPlacementRepository) {
        Product product =
            getSingleProductRepository
                .GetByIdAndRuleLocaleWithProductGroupAndWriteOffRules(
                    sadItem.SupplyOrderUkraineCartItem.ProductId,
                    sad.Organization.Culture
                );

        if (product == null) return;

        ProductWriteOffRule writeOffRule =
            GetCurrentWriteOffRuleForProduct(
                product,
                sad.Organization.Culture,
                userRepository,
                productWriteOffRuleRepository
            );

        IEnumerable<ConsignmentItem> consignmentItems =
            consignmentItemRepository
                .GetAllAvailable(
                    sad.Organization.Id,
                    sadItem.SupplyOrderUkraineCartItem.ProductId,
                    writeOffRule.RuleType,
                    sad.Organization.Culture
                );

        if (!consignmentItems.Any()) return;

        foreach (SupplyOrderUkraineCartItemReservation reservation in sadItem.SupplyOrderUkraineCartItem.SupplyOrderUkraineCartItemReservations) {
            reservation.ProductAvailability.Amount += reservation.Qty;

            productAvailabilityRepository.Update(reservation.ProductAvailability);

            reservation.Deleted = true;

            cartItemReservationRepository.Update(reservation);
        }

        ConsignmentItem firstItem = consignmentItems.First();

        if (firstItem.RemainingQty < sadItem.Qty) {
            sadItem.Deleted = true;

            sadItemRepository.Update(sadItem);

            foreach (ConsignmentItem consignmentItem in consignmentItems) {
                ProductAvailability availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            sadItem.SupplyOrderUkraineCartItem.ProductId,
                            consignmentItem.Consignment.StorageId
                        );

                if (availability != null) {
                    double currentOperationQty = sadItem.Qty;

                    if (consignmentItem.RemainingQty < sadItem.Qty)
                        currentOperationQty = consignmentItem.RemainingQty;

                    consignmentItem.RemainingQty -= currentOperationQty;

                    consignmentItemRepository.UpdateRemainingQty(consignmentItem);

                    sadItem.Qty -= currentOperationQty;

                    availability.Amount -= currentOperationQty;

                    productAvailabilityRepository.Update(availability);

                    SupplyOrderUkraineCartItem newItem = new SupplyOrderUkraineCartItem {
                        Comment = sadItem.SupplyOrderUkraineCartItem.Comment,
                        UploadedQty = currentOperationQty,
                        ItemPriority = sadItem.SupplyOrderUkraineCartItem.ItemPriority,
                        ProductId = sadItem.SupplyOrderUkraineCartItem.ProductId,
                        CreatedById = sadItem.SupplyOrderUkraineCartItem.CreatedById,
                        UpdatedById = sadItem.SupplyOrderUkraineCartItem.UpdatedById,
                        ResponsibleId = sadItem.SupplyOrderUkraineCartItem.ResponsibleId,
                        ReservedQty = currentOperationQty,
                        FromDate = sadItem.SupplyOrderUkraineCartItem.FromDate,
                        TaxFreePackListId = null,
                        UnpackedQty = sadItem.SupplyOrderUkraineCartItem.UnpackedQty,
                        NetWeight = consignmentItem.Weight,
                        UnitPrice = consignmentItem.Price,
                        SupplierId = sadItem.SupplyOrderUkraineCartItem.SupplierId,
                        PackingListPackageOrderItemId = null,
                        MaxQtyPerTF = sadItem.SupplyOrderUkraineCartItem.MaxQtyPerTF,
                        IsRecommended = sadItem.SupplyOrderUkraineCartItem.IsRecommended,
                        Deleted = true
                    };

                    newItem.Id = cartItemRepository.Add(newItem);

                    SupplyOrderUkraineCartItemReservation cartItemReservation = new SupplyOrderUkraineCartItemReservation {
                        Qty = currentOperationQty,
                        ConsignmentItemId = consignmentItem.Id,
                        ProductAvailabilityId = availability.Id,
                        SupplyOrderUkraineCartItemId = newItem.Id
                    };

                    cartItemReservation.Id = cartItemReservationRepository.Add(cartItemReservation);

                    IEnumerable<ProductPlacement> placements =
                        productPlacementRepository
                            .GetAllByConsignmentItemId(
                                consignmentItem.Id
                            );

                    double toMoveQty = currentOperationQty;

                    foreach (ProductPlacement placement in placements) {
                        double currentMoveQty = toMoveQty;

                        if (placement.Qty < currentMoveQty)
                            currentMoveQty = placement.Qty;

                        placement.Qty -= currentMoveQty;

                        toMoveQty -= currentMoveQty;

                        reservationProductPlacementRepository.Add(new SupplyOrderUkraineCartItemReservationProductPlacement {
                            Qty = currentMoveQty,
                            ProductPlacementId = placement.Id,
                            SupplyOrderUkraineCartItemReservationId = cartItemReservation.Id
                        });

                        productPlacementRepository.UpdateQty(placement);

                        if (toMoveQty.Equals(0d)) break;
                    }

                    sadItemRepository.Add(new SadItem {
                        Qty = currentOperationQty,
                        UnpackedQty = currentOperationQty,
                        SupplyOrderUkraineCartItemId = newItem.Id,
                        SadId = sad.Id
                    });
                } else {
                    //Error
                }

                if (sadItem.Qty.Equals(0d)) break;
            }

            if (sadItem.Qty <= 0d) return;

            IEnumerable<ProductAvailability> availabilities =
                productAvailabilityRepository
                    .GetByProductAndCultureIds(
                        sadItem.SupplyOrderUkraineCartItem.ProductId,
                        "pl"
                    );

            if (!availabilities.Any()) return;

            if (availabilities.Sum(a => a.Amount) < sadItem.Qty)
                sadItem.Qty = availabilities.Sum(a => a.Amount);

            SupplyOrderUkraineCartItem restItem = new SupplyOrderUkraineCartItem {
                Comment = sadItem.SupplyOrderUkraineCartItem.Comment,
                UploadedQty = sadItem.Qty,
                ItemPriority = sadItem.SupplyOrderUkraineCartItem.ItemPriority,
                ProductId = sadItem.SupplyOrderUkraineCartItem.ProductId,
                CreatedById = sadItem.SupplyOrderUkraineCartItem.CreatedById,
                UpdatedById = sadItem.SupplyOrderUkraineCartItem.UpdatedById,
                ResponsibleId = sadItem.SupplyOrderUkraineCartItem.ResponsibleId,
                ReservedQty = sadItem.Qty,
                FromDate = sadItem.SupplyOrderUkraineCartItem.FromDate,
                TaxFreePackListId = null,
                UnpackedQty = sadItem.SupplyOrderUkraineCartItem.UnpackedQty,
                NetWeight = sadItem.SupplyOrderUkraineCartItem.NetWeight,
                UnitPrice = sadItem.SupplyOrderUkraineCartItem.UnitPrice,
                SupplierId = sadItem.SupplyOrderUkraineCartItem.SupplierId,
                PackingListPackageOrderItemId = null,
                MaxQtyPerTF = sadItem.SupplyOrderUkraineCartItem.MaxQtyPerTF,
                IsRecommended = sadItem.SupplyOrderUkraineCartItem.IsRecommended,
                Deleted = true
            };

            restItem.Id = cartItemRepository.Add(restItem);

            foreach (ProductAvailability availability in availabilities) {
                SupplyOrderUkraineCartItemReservation reservation = new SupplyOrderUkraineCartItemReservation {
                    ProductAvailabilityId = availability.Id,
                    SupplyOrderUkraineCartItemId = restItem.Id
                };

                if (availability.Amount < sadItem.Qty) {
                    sadItem.Qty -= availability.Amount;

                    reservation.Qty = availability.Amount;

                    availability.Amount = 0;

                    productAvailabilityRepository.Update(availability);
                } else {
                    reservation.Qty = sadItem.Qty;

                    availability.Amount -= sadItem.Qty;

                    productAvailabilityRepository.Update(availability);

                    sadItem.Qty = 0d;
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
                    reservation.Id = cartItemReservationRepository.Add(reservation);
                }

                IEnumerable<ProductPlacement> placements =
                    productPlacementRepository
                        .GetAllByProductAndStorageId(
                            availability.ProductId,
                            availability.StorageId
                        );

                double toMoveQty = reservation.Qty;

                foreach (ProductPlacement placement in placements) {
                    double currentMoveQty = toMoveQty;

                    if (placement.Qty < currentMoveQty)
                        currentMoveQty = placement.Qty;

                    placement.Qty -= currentMoveQty;

                    toMoveQty -= currentMoveQty;

                    reservationProductPlacementRepository.Add(new SupplyOrderUkraineCartItemReservationProductPlacement {
                        Qty = currentMoveQty,
                        ProductPlacementId = placement.Id,
                        SupplyOrderUkraineCartItemReservationId = existingReservation?.Id ?? reservation.Id
                    });

                    productPlacementRepository.UpdateQty(placement);

                    if (toMoveQty.Equals(0d)) break;
                }

                if (sadItem.Qty.Equals(0d)) break;
            }
        } else {
            ConsignmentItem consignmentItem = consignmentItems.First();

            ProductAvailability availability =
                productAvailabilityRepository
                    .GetByProductAndStorageIds(
                        sadItem.SupplyOrderUkraineCartItem.ProductId,
                        consignmentItem.Consignment.StorageId
                    );

            if (availability != null) {
                double currentOperationQty = sadItem.Qty;

                consignmentItem.RemainingQty -= currentOperationQty;

                consignmentItemRepository.UpdateRemainingQty(consignmentItem);

                sadItem.Qty -= currentOperationQty;

                availability.Amount -= currentOperationQty;

                productAvailabilityRepository.Update(availability);

                sadItem.SupplyOrderUkraineCartItem.UnitPrice = consignmentItem.Price;
                sadItem.SupplyOrderUkraineCartItem.NetWeight = consignmentItem.Weight;

                cartItemRepository.Update(sadItem.SupplyOrderUkraineCartItem);

                SupplyOrderUkraineCartItemReservation cartItemReservation = new SupplyOrderUkraineCartItemReservation {
                    Qty = currentOperationQty,
                    ConsignmentItemId = consignmentItem.Id,
                    ProductAvailabilityId = availability.Id,
                    SupplyOrderUkraineCartItemId = sadItem.SupplyOrderUkraineCartItem.Id
                };

                cartItemReservation.Id = cartItemReservationRepository.Add(cartItemReservation);

                IEnumerable<ProductPlacement> placements =
                    productPlacementRepository
                        .GetAllByConsignmentItemId(
                            consignmentItem.Id
                        );

                double toMoveQty = currentOperationQty;

                foreach (ProductPlacement placement in placements) {
                    double currentMoveQty = toMoveQty;

                    if (placement.Qty < currentMoveQty)
                        currentMoveQty = placement.Qty;

                    placement.Qty -= currentMoveQty;

                    toMoveQty -= currentMoveQty;

                    reservationProductPlacementRepository.Add(new SupplyOrderUkraineCartItemReservationProductPlacement {
                        Qty = currentMoveQty,
                        ProductPlacementId = placement.Id,
                        SupplyOrderUkraineCartItemReservationId = cartItemReservation.Id
                    });

                    productPlacementRepository.UpdateQty(placement);

                    if (toMoveQty.Equals(0d)) break;
                }
            } else {
                //Error
            }
        }
    }

    private static ProductWriteOffRule GetCurrentWriteOffRuleForProduct(
        Product product,
        string ruleLocale,
        IUserRepository userRepository,
        IProductWriteOffRuleRepository productWriteOffRuleRepository) {
        ProductWriteOffRule writeOffRule;

        if (product.ProductWriteOffRules.Any()) {
            writeOffRule = product.ProductWriteOffRules.First();
        } else if (product.ProductProductGroups.Any() && product.ProductProductGroups.First().ProductGroup.ProductWriteOffRules.Any()) {
            writeOffRule = product.ProductProductGroups.First().ProductGroup.ProductWriteOffRules.First();
        } else {
            writeOffRule = productWriteOffRuleRepository.GetByRuleLocale(ruleLocale);

            if (writeOffRule != null) return writeOffRule;

            productWriteOffRuleRepository.Add(new ProductWriteOffRule {
                RuleLocale = ruleLocale,
                CreatedById = userRepository.GetManagerOrGBAIdByClientNetId(Guid.Empty),
                RuleType = ProductWriteOffRuleType.ByFromDate
            });

            writeOffRule = productWriteOffRuleRepository.GetByRuleLocale(ruleLocale);
        }

        return writeOffRule;
    }

    private void ProcessUpdateConsignmentItemGrossPrice(UpdateConsignmentItemGrossPriceMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IConsignmentItemRepository consignmentItemRepository =
                _consignmentRepositoriesFactory.NewConsignmentItemRepository(connection);

            List<SupplyInvoice> supplyInvoices =
                _supplyRepositoriesFactory
                    .NewSupplyInvoiceRepository(connection)
                    .GetWithConsignmentsByIds(message.SupplyInvoiceIds);

            foreach (SupplyInvoice invoice in supplyInvoices) {
                foreach (PackingList pacList in invoice.PackingLists) {
                    foreach (PackingListPackageOrderItem listItem in pacList.PackingListPackageOrderItems) {
                        foreach (ProductIncomeItem incomeItem in listItem.ProductIncomeItems) {
                            foreach (ConsignmentItem consignmentItem in incomeItem.ConsignmentItems) {
                                decimal price = listItem.GrossUnitPriceEur +
                                                listItem.AccountingGrossUnitPriceEur +
                                                listItem.AccountingGeneralGrossUnitPriceEur;

                                if (!consignmentItem.Price.Equals(price) ||
                                    !consignmentItem.AccountingPrice.Equals(listItem.AccountingGrossUnitPriceEur) ||
                                    !listItem.UnitPriceEur.Equals(consignmentItem.NetPrice)) {
                                    consignmentItem.Price = price;
                                    consignmentItem.AccountingPrice = listItem.AccountingGrossUnitPriceEur;
                                    consignmentItem.NetPrice = listItem.UnitPriceEur;
                                    consignmentItem.ExchangeRate = listItem.ExchangeRateAmount;

                                    consignmentItemRepository
                                        .UpdateGrossPriceAfterIncomes(consignmentItem);
                                }
                            }
                        }
                    }
                }
            }
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private static decimal GetGovExchangeRateOnDateToEurCorrect(
        Currency from,
        DateTime onDate,
        IGovCrossExchangeRateRepository govCrossExchangeRateRepository,
        IGovExchangeRateRepository govExchangeRateRepository,
        ICurrencyRepository currencyRepository) {
        Currency eur = currencyRepository.GetEURCurrencyIfExists();
        Currency usd = currencyRepository.GetUSDCurrencyIfExists();
        Currency uah = currencyRepository.GetUAHCurrencyIfExists();

        if (from.Id.Equals(eur.Id))
            return 1m;

        if (from.Code.Equals(usd.Code)) {
            GovCrossExchangeRate govCrossExchangeRate =
                govCrossExchangeRateRepository
                    .GetByCurrenciesIds(eur.Id, from.Id, onDate);

            return govCrossExchangeRate?.Amount ?? 1m;
        }

        GovExchangeRate govExchangeRate =
            govExchangeRateRepository
                .GetByCurrencyIdAndCode(
                    eur.Id, from.Code, onDate)
            ??
            govExchangeRateRepository
                .GetByCurrencyIdAndCode(
                    from.Id, eur.Code, onDate);

        return govExchangeRate?.Amount ?? 1m;
    }

    private static decimal GetGovExchangeRateOnDateToEur(
        Currency from,
        DateTime onDate,
        IGovCrossExchangeRateRepository govCrossExchangeRateRepository,
        IGovExchangeRateRepository govExchangeRateRepository,
        ICurrencyRepository currencyRepository) {
        Currency eur = currencyRepository.GetEURCurrencyIfExists();
        Currency usd = currencyRepository.GetUSDCurrencyIfExists();
        Currency uah = currencyRepository.GetUAHCurrencyIfExists();

        if (from.Id.Equals(uah.Id))
            return 1m;

        // if (from.Code.Equals(usd.Code)) {
        //     GovCrossExchangeRate govCrossExchangeRate =
        //         govCrossExchangeRateRepository
        //             .GetByCurrenciesIds(eur.Id, from.Id, onDate);
        //
        //     return govCrossExchangeRate?.Amount ?? 1m;
        // }

        GovExchangeRate govExchangeRate =
            govExchangeRateRepository
                .GetByCurrencyIdAndCode(
                    uah.Id, from.Code, onDate)
            ??
            govExchangeRateRepository
                .GetByCurrencyIdAndCode(
                    from.Id, uah.Code, onDate);

        return govExchangeRate?.Amount ?? 1m;
    }

    private Tuple<double, double, long> StoreConsignmentItemFromReSaleAvailabilities(
        IEnumerable<ReSaleAvailability> reSaleAvailabilities,
        ConsignmentItem consignmentItem,
        OrderItem orderItem,
        double orderItemQty,
        Guid clientAgreementNetId,
        IConsignmentItemRepository consignmentItemRepository,
        IReSaleAvailabilityRepository reSaleAvailabilityRepository,
        IOrderItemRepository orderItemRepository,
        IProductSpecificationRepository specificationRepository,
        IConsignmentItemMovementRepository consignmentItemMovementRepository,
        IProductReservationRepository productReservationRepository) {
        double consignmentItemsQty = consignmentItem.RemainingQty;
        long newOrderItemId = 0;
        decimal accountPrice = consignmentItemRepository.GetPriceForReSaleByConsignmentItemId(consignmentItem.Id);

        double qtyToReSaleAvailabilities = 0;

        foreach (ReSaleAvailability reSaleAvailability in reSaleAvailabilities.Where(x => x.RemainingQty > 0)) {
            if (consignmentItemsQty.Equals(0) || orderItemQty.Equals(0))
                break;

            double qtyNewReSaleAvailability;
            // what the actual fuck
            if (reSaleAvailability.RemainingQty > consignmentItemsQty) {
                if (orderItemQty < consignmentItemsQty) {
                    if (reSaleAvailability.RemainingQty > orderItemQty) {
                        reSaleAvailability.RemainingQty -= orderItemQty;
                        qtyNewReSaleAvailability = orderItemQty;
                        consignmentItemsQty -= orderItemQty;
                        orderItemQty = 0;
                    } else {
                        consignmentItemsQty -= reSaleAvailability.RemainingQty;
                        orderItemQty -= reSaleAvailability.RemainingQty;
                        qtyNewReSaleAvailability = reSaleAvailability.RemainingQty;
                        reSaleAvailability.RemainingQty = 0;
                    }
                } else {
                    reSaleAvailability.RemainingQty -= consignmentItemsQty;
                    qtyNewReSaleAvailability = consignmentItemsQty;
                    orderItemQty -= consignmentItemsQty;
                    consignmentItemsQty = 0;
                }
            } else {
                if (orderItemQty < consignmentItemsQty) {
                    if (reSaleAvailability.RemainingQty > orderItemQty) {
                        reSaleAvailability.RemainingQty -= orderItemQty;
                        qtyNewReSaleAvailability = orderItemQty;
                        consignmentItemsQty -= orderItemQty;
                        orderItemQty = 0;
                    } else {
                        consignmentItemsQty -= reSaleAvailability.RemainingQty;
                        orderItemQty -= reSaleAvailability.RemainingQty;
                        qtyNewReSaleAvailability = reSaleAvailability.RemainingQty;
                        reSaleAvailability.RemainingQty = 0;
                    }
                } else {
                    if (orderItemQty < reSaleAvailability.RemainingQty) {
                        reSaleAvailability.RemainingQty -= orderItemQty;
                        qtyNewReSaleAvailability = orderItemQty;
                        consignmentItemsQty -= orderItemQty;
                        orderItemQty -= consignmentItem.RemainingQty - consignmentItemsQty;
                    } else {
                        consignmentItemsQty -= reSaleAvailability.RemainingQty;
                        orderItemQty -= reSaleAvailability.RemainingQty;
                        qtyNewReSaleAvailability = reSaleAvailability.RemainingQty;
                        reSaleAvailability.RemainingQty = 0;
                    }
                }
            }

            qtyToReSaleAvailabilities += qtyNewReSaleAvailability;

            reSaleAvailabilityRepository.Add(new ReSaleAvailability {
                ConsignmentItemId = consignmentItem.Id,
                ProductAvailabilityId = reSaleAvailability.ProductAvailabilityId,
                ExchangeRate = accountPrice / consignmentItem.AccountingPrice,
                PricePerItem = accountPrice,
                Qty = qtyNewReSaleAvailability,
                RemainingQty = qtyNewReSaleAvailability,
                OrderItemId = orderItem.Id
            });

            OrderItem newItemFromReSale = new OrderItem {
                ProductId = orderItem.ProductId,
                Comment = orderItem.Comment,
                Discount = orderItem.Discount,
                DiscountAmount = orderItem.DiscountAmount,
                PricePerItem = orderItem.PricePerItem,
                Qty = qtyNewReSaleAvailability,
                ChangedQty = orderItem.ChangedQty,
                OrderedQty = orderItem.OrderedQty,
                OrderId = orderItem.OrderId,
                ReturnedQty = 0d,
                TotalAmount = orderItem.TotalAmount,
                TotalAmountLocal = orderItem.TotalAmountLocal,
                TotalWeight = orderItem.TotalWeight,
                UserId = orderItem.UserId,
                DiscountUpdatedById = orderItem.DiscountUpdatedById,
                ExchangeRateAmount = orderItem.ExchangeRateAmount,
                OneTimeDiscountComment = orderItem.OneTimeDiscountComment,
                OfferProcessingStatusChangedById = orderItem.OfferProcessingStatusChangedById,
                UnpackedQty = orderItem.UnpackedQty,
                FromOfferQty = orderItem.FromOfferQty,
                IsFromOffer = orderItem.IsFromOffer,
                OfferProcessingStatus = orderItem.OfferProcessingStatus,
                OneTimeDiscount = orderItem.OneTimeDiscount,
                IsValidForCurrentSale = orderItem.IsValidForCurrentSale,
                PricePerItemWithoutVat = orderItem.PricePerItemWithoutVat,
                IsFromReSale = consignmentItem.IsReSaleAvailability,
                Vat = orderItem.Vat
            };

            OrderItem existingItemFromReSale =
                orderItemRepository
                    .GetOrderItemByOrderProductAndSpecification(
                        newItemFromReSale.OrderId ?? 0,
                        newItemFromReSale.ProductId,
                        reSaleAvailability.ConsignmentItem.ProductSpecification,
                        newItemFromReSale.IsFromReSale
                    );

            long orderItemId;
            if (existingItemFromReSale != null) {
                orderItemId = newItemFromReSale.Id = existingItemFromReSale.Id;

                newItemFromReSale.AssignedSpecificationId = existingItemFromReSale.AssignedSpecificationId;

                existingItemFromReSale.Qty += qtyNewReSaleAvailability;

                orderItemRepository.UpdateQty(existingItemFromReSale);
            } else {
                newItemFromReSale.AssignedSpecificationId =
                    specificationRepository
                        .Add(new ProductSpecification {
                            Name = reSaleAvailability.ConsignmentItem.ProductSpecification.Name,
                            Locale = reSaleAvailability.ConsignmentItem.ProductSpecification.Locale,
                            AddedById = reSaleAvailability.ConsignmentItem.ProductSpecification.AddedById,
                            ProductId = reSaleAvailability.ConsignmentItem.ProductSpecification.ProductId,
                            VATValue = reSaleAvailability.ConsignmentItem.ProductSpecification.VATValue,
                            CustomsValue = reSaleAvailability.ConsignmentItem.ProductSpecification.CustomsValue,
                            Duty = reSaleAvailability.ConsignmentItem.ProductSpecification.Duty,
                            VATPercent = reSaleAvailability.ConsignmentItem.ProductSpecification.VATPercent,
                            SpecificationCode = reSaleAvailability.ConsignmentItem.ProductSpecification.SpecificationCode,
                            DutyPercent = reSaleAvailability.ConsignmentItem.ProductSpecification.DutyPercent
                        });

                if (consignmentItem.IsReSaleAvailability) {
                    newItemFromReSale.PricePerItemWithoutVat = newItemFromReSale.PricePerItem =
                        orderItemRepository
                            .GetReSalePricePerItem(
                                orderItem.Product.NetUid,
                                clientAgreementNetId,
                                orderItem.Id
                            );
                }

                orderItemId = newItemFromReSale.Id = orderItemRepository.Add(newItemFromReSale);
            }

            if (consignmentItem.IsReSaleAvailability) {
                decimal accountPriceFromReSale =
                    consignmentItemRepository.GetPriceForReSaleByConsignmentItemId(reSaleAvailability.ConsignmentItemId);

                reSaleAvailabilityRepository.Add(new ReSaleAvailability {
                    ConsignmentItemId = reSaleAvailability.ConsignmentItemId,
                    ProductAvailabilityId = reSaleAvailability.ProductAvailabilityId,
                    ExchangeRate = accountPriceFromReSale / reSaleAvailability.ConsignmentItem.AccountingPrice,
                    PricePerItem = accountPriceFromReSale,
                    Qty = qtyNewReSaleAvailability,
                    RemainingQty = qtyNewReSaleAvailability,
                    OrderItemId = orderItem.Id,
                    ProductReservationId = productReservationRepository.AddWithId(new ProductReservation {
                        OrderItemId = orderItemId,
                        ProductAvailabilityId = reSaleAvailability.ProductAvailabilityId,
                        ConsignmentItemId = consignmentItem.Id,
                        Qty = qtyNewReSaleAvailability,
                        IsReSaleReservation = consignmentItem.IsReSaleAvailability
                    })
                });
            }

            consignmentItemMovementRepository.Add(
                new ConsignmentItemMovement {
                    IsIncomeMovement = false,
                    Qty = qtyNewReSaleAvailability,
                    RemainingQty = qtyNewReSaleAvailability,
                    MovementType = ConsignmentItemMovementType.Sale,
                    ConsignmentItemId = reSaleAvailability.ConsignmentItem.Id,
                    OrderItemId = orderItemId
                });

            if (reSaleAvailability.ConsignmentItem.IsReSaleAvailability) {
                orderItem.PricePerItemWithoutVat = orderItem.PricePerItem =
                    orderItemRepository
                        .GetReSalePricePerItem(
                            orderItem.Product.NetUid,
                            clientAgreementNetId,
                            orderItemId
                        );
            }

            newOrderItemId = newItemFromReSale.Id;
            orderItemRepository.AssignSpecification(newItemFromReSale);

            reSaleAvailabilityRepository.UpdateRemainingQty(reSaleAvailability);

            if (reSaleAvailability.RemainingQty.Equals(0))
                reSaleAvailabilityRepository.Delete(reSaleAvailability.Id);
        }

        consignmentItem.RemainingQty = consignmentItemsQty;

        consignmentItemRepository.UpdateRemainingQty(consignmentItem);
        if (newOrderItemId.Equals(0)) {
            newOrderItemId = orderItem.Id;
        }

        return new Tuple<double, double, long>(orderItemQty, qtyToReSaleAvailabilities, newOrderItemId);
    }

    private void StoreProductPlacementAndAvailabilitiesFromSale(
        ConsignmentItem consignmentItem,
        long orderItemId,
        long productId,
        double qtyRemainingConsignmentItem,
        double qtyToReSaleAvailabilities,
        bool fromShiftedSale,
        IProductPlacementRepository productPlacementRepository,
        IProductLocationRepository productLocationRepository,
        IProductAvailabilityRepository productAvailabilityRepository,
        IProductReservationRepository productReservationRepository,
        IConsignmentItemRepository consignmentItemRepository,
        IReSaleAvailabilityRepository reSaleAvailabilityRepository) {
        //1259
        //IEnumerable<ProductPlacement> placementsAfterSale =
        //    productPlacementRepository
        //        .GetAllByConsignmentItemId(
        //            consignmentItem.Id
        //        );
        //if (!placementsAfterSale.Any()) {
        //IEnumerable<ProductPlacement> placementsAfterSale =
        //   productPlacementRepository.GetAllByProductAndStorageIds(consignmentItem.ProductId,
        //   consignmentItem.Consignment.StorageId);
        //}
        //}

        IEnumerable<ProductPlacement> placementsAfterSale =
            productPlacementRepository.GetAllByProductAndStorageIds(
                consignmentItem.ProductId,
                consignmentItem.Consignment.StorageId);


        double qtyToMovementPlacement = qtyRemainingConsignmentItem;

        foreach (ProductPlacement placement in placementsAfterSale) {
            double operationQty = qtyToMovementPlacement;

            if (placement.Qty < operationQty)
                operationQty = placement.Qty;

            placement.Qty -= operationQty;

            if (placement.Qty > 0)
                productPlacementRepository.UpdateQty(placement);
            else
                productPlacementRepository.Remove(placement);

            productLocationRepository.Add(new ProductLocation {
                StorageId = consignmentItem.Consignment.StorageId,
                Qty = operationQty,
                ProductPlacementId = placement.Id,
                OrderItemId = orderItemId
            });

            qtyToMovementPlacement -= operationQty;

            if (qtyToMovementPlacement.Equals(0d)) break;
        }

        qtyToMovementPlacement = qtyRemainingConsignmentItem;

        ProductAvailability availabilityAfterSale =
            productAvailabilityRepository
                .GetByProductAndStorageIds(
                    productId,
                    consignmentItem.Consignment.StorageId
                );

        if (availabilityAfterSale != null) {
            availabilityAfterSale.Amount -= qtyToMovementPlacement;

            productAvailabilityRepository.Update(availabilityAfterSale);

            ProductReservation reservation =
                productReservationRepository
                    .GetByOrderItemProductAvailabilityAndConsignmentItemIds(
                        orderItemId,
                        availabilityAfterSale.Id,
                        consignmentItem.Id
                    );

            decimal accountPrice = consignmentItemRepository.GetPriceForReSaleByConsignmentItemId(consignmentItem.Id);

            double qtyToReSale = qtyToMovementPlacement - qtyToReSaleAvailabilities;

            if (reservation != null) {
                reservation.Qty += qtyToReSale;

                productReservationRepository.Update(reservation);

                if (reservation.IsReSaleReservation && !fromShiftedSale && !qtyToReSale.Equals(0))
                    reSaleAvailabilityRepository.Add(new ReSaleAvailability {
                        Qty = qtyToReSale,
                        RemainingQty = qtyToReSale,
                        ConsignmentItemId = consignmentItem.Id,
                        ProductAvailabilityId = availabilityAfterSale.Id,
                        OrderItemId = orderItemId,
                        ProductReservationId = reservation.Id,
                        PricePerItem = accountPrice,
                        ExchangeRate = accountPrice / consignmentItem.AccountingPrice
                    });
            } else {
                if (consignmentItem.IsReSaleAvailability && !fromShiftedSale && !qtyToReSale.Equals(0))
                    reSaleAvailabilityRepository.Add(new ReSaleAvailability {
                        Qty = qtyToReSale,
                        RemainingQty = qtyToReSale,
                        ConsignmentItemId = consignmentItem.Id,
                        ProductAvailabilityId = availabilityAfterSale.Id,
                        OrderItemId = orderItemId,
                        ProductReservationId = productReservationRepository.AddWithId(new ProductReservation {
                            OrderItemId = orderItemId,
                            ProductAvailabilityId = availabilityAfterSale.Id,
                            ConsignmentItemId = consignmentItem.Id,
                            Qty = qtyToReSale,
                            IsReSaleReservation = consignmentItem.IsReSaleAvailability
                        }),
                        PricePerItem = accountPrice,
                        ExchangeRate = accountPrice / consignmentItem.AccountingPrice
                    });
                else if (!qtyToReSale.Equals(0))
                    productReservationRepository.Add(new ProductReservation {
                        OrderItemId = orderItemId,
                        ProductAvailabilityId = availabilityAfterSale.Id,
                        ConsignmentItemId = consignmentItem.Id,
                        Qty = qtyToReSale,
                        IsReSaleReservation = consignmentItem.IsReSaleAvailability
                    });
            }
        }
    }
}