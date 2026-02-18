using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Messages.Products.ProductPlacementMovements;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Products;

public sealed class ProductPlacementMovementsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public ProductPlacementMovementsActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;

        Receive<AddNewProductPlacementMovementMessage>(ProcessAddNewProductPlacementMovementMessage);

        Receive<GetAllProductPlacementMovementsFilteredMessage>(ProcessGetAllProductPlacementMovementsFilteredMessage);
    }

    private void ProcessAddNewProductPlacementMovementMessage(AddNewProductPlacementMovementMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.ProductPlacementMovement == null) throw new Exception(ProductPlacementMovementResourceNames.EMPTY_ENTITY_PRODUCT_PLACEMENT);
            if (message.ProductPlacementMovement.Qty <= 0) throw new Exception(ProductPlacementMovementResourceNames.NEED_MORE_QTY);
            if (message.ProductPlacementMovement.FromProductPlacement == null || message.ProductPlacementMovement.FromProductPlacement.IsNew())
                throw new Exception(ProductPlacementMovementResourceNames.FROM_PRODUCT_PLACEMENT_EMPTY);

            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IProductWriteOffRuleRepository productWriteOffRuleRepository = _productRepositoriesFactory.NewProductWriteOffRuleRepository(connection);
            IProductPlacementMovementRepository productPlacementMovementRepository = _productRepositoriesFactory.NewProductPlacementMovementRepository(connection);

            message.ProductPlacementMovement.FromProductPlacement =
                productPlacementRepository
                    .GetByIdWithStorage(
                        message
                            .ProductPlacementMovement
                            .FromProductPlacement
                            .Id
                    );

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            message.ProductPlacementMovement.ResponsibleId = user.Id;

            Product product =
                getSingleProductRepository
                    .GetByIdAndRuleLocaleWithProductGroupAndWriteOffRules(
                        message.ProductPlacementMovement.FromProductPlacement.ProductId,
                        message.ProductPlacementMovement.FromProductPlacement.Storage.Organization.Code
                    );

            ProductWriteOffRule writeOffRule;

            if (product.ProductWriteOffRules.Any()) {
                writeOffRule = product.ProductWriteOffRules.First();
            } else if (product.ProductProductGroups.Any()) {
                writeOffRule = product.ProductProductGroups.First().ProductGroup.ProductWriteOffRules.First();
            } else {
                writeOffRule = productWriteOffRuleRepository.GetByRuleLocale(message.ProductPlacementMovement.FromProductPlacement.Storage.Organization.Culture);

                if (writeOffRule == null) {
                    productWriteOffRuleRepository.Add(new ProductWriteOffRule {
                        RuleLocale = message.ProductPlacementMovement.FromProductPlacement.Storage.Organization.Culture,
                        CreatedById = user.Id,
                        RuleType = ProductWriteOffRuleType.ByFromDate
                    });

                    writeOffRule = productWriteOffRuleRepository.GetByRuleLocale(message.ProductPlacementMovement.FromProductPlacement.Storage.Organization.Culture);
                }
            }

            IEnumerable<ProductPlacement> placements =
                productPlacementRepository
                    .GetAllFilteredAndOrderedByWriteOffRule(
                        message.ProductPlacementMovement.FromProductPlacement.StorageId,
                        message.ProductPlacementMovement.FromProductPlacement.ProductId,
                        message.ProductPlacementMovement.FromProductPlacement.RowNumber,
                        message.ProductPlacementMovement.FromProductPlacement.CellNumber,
                        message.ProductPlacementMovement.FromProductPlacement.StorageNumber,
                        writeOffRule.RuleType
                    );

            if (message.ProductPlacementMovement.FromProductPlacement == null)
                throw new Exception(ProductPlacementMovementResourceNames.PRODUCT_PLACEMENT_NOT_EXIST_OR_REMOVED);
            if (message.ProductPlacementMovement.FromProductPlacement.Qty < message.ProductPlacementMovement.Qty &&
                placements.Sum(p => p.Qty) < message.ProductPlacementMovement.Qty)
                throw new Exception(ProductPlacementMovementResourceNames.QTY_LESS_ALAILABILITY);

            if (message.ProductPlacementMovement.ToProductPlacement == null)
                throw new Exception(ProductPlacementMovementResourceNames.EMPTY_ENTITY_PRODUCT_PLACEMENT);

            if (!message.ProductPlacementMovement.ToProductPlacement.IsNew()) {
                message.ProductPlacementMovement.ToProductPlacement =
                    productPlacementRepository
                        .GetById(
                            message
                                .ProductPlacementMovement
                                .ToProductPlacement
                                .Id
                        );

                if (message.ProductPlacementMovement.ToProductPlacement == null)
                    throw new Exception(ProductPlacementMovementResourceNames.PRODUCT_PLACEMENT_NOT_EXIST_OR_REMOVED);
                if (!message.ProductPlacementMovement.FromProductPlacement.StorageId.Equals(message.ProductPlacementMovement.ToProductPlacement.StorageId))
                    throw new Exception(ProductPlacementMovementResourceNames.OPERATION_IN_SAME_STORAGE);
                if (!message.ProductPlacementMovement.FromProductPlacement.ProductId.Equals(message.ProductPlacementMovement.ToProductPlacement.ProductId))
                    throw new Exception(ProductPlacementMovementResourceNames.OPERATION_IN_SAME_PRODUCT);
                message.ProductPlacementMovement.ToProductPlacement.Qty += message.ProductPlacementMovement.Qty;
            } else if (string.IsNullOrEmpty(message.ProductPlacementMovement.ToProductPlacement.StorageNumber) ||
                       string.IsNullOrEmpty(message.ProductPlacementMovement.ToProductPlacement.CellNumber)) {
                throw new Exception(ProductPlacementMovementResourceNames.STORAGE_OR_CELL_NUMBER_NOT_SPECIFY);
            } else {
                message.ProductPlacementMovement.ToProductPlacement.ProductId = message.ProductPlacementMovement.FromProductPlacement.ProductId;
                message.ProductPlacementMovement.ToProductPlacement.StorageId = message.ProductPlacementMovement.FromProductPlacement.StorageId;
                message.ProductPlacementMovement.ToProductPlacement.Qty = message.ProductPlacementMovement.Qty;
            }

            double toMoveQty = message.ProductPlacementMovement.ToProductPlacement.Qty;

            foreach (ProductPlacement placement in placements) {
                ProductPlacementMovement lastRecord =
                    productPlacementMovementRepository
                        .GetLastRecord(
                            message.ProductPlacementMovement.FromProductPlacement.Storage.Locale
                        );

                if (lastRecord != null && DateTime.Now.Year.Equals(lastRecord.Created.Year))
                    message.ProductPlacementMovement.Number =
                        message.ProductPlacementMovement.FromProductPlacement.Storage.Organization.Code +
                        string.Format(
                            "{0:D11}",
                            Convert.ToInt32(
                                lastRecord.Number.Substring(
                                    message.ProductPlacementMovement.FromProductPlacement.Storage.Organization.Code.Length,
                                    lastRecord.Number.Length - message.ProductPlacementMovement.FromProductPlacement.Storage.Organization.Code.Length
                                )
                            ) + 1
                        );
                else
                    message.ProductPlacementMovement.Number =
                        $"{message.ProductPlacementMovement.FromProductPlacement.Storage.Organization.Code}{string.Format("{0:D11}", 1)}";

                double operationQty = toMoveQty;

                if (placement.Qty < operationQty)
                    operationQty = placement.Qty;

                ProductPlacement toPlacement =
                    productPlacementRepository
                        .GetIfExists(
                            message.ProductPlacementMovement.ToProductPlacement.RowNumber,
                            message.ProductPlacementMovement.ToProductPlacement.CellNumber,
                            message.ProductPlacementMovement.ToProductPlacement.StorageNumber,
                            message.ProductPlacementMovement.ToProductPlacement.ProductId,
                            message.ProductPlacementMovement.FromProductPlacement.StorageId,
                            null,
                            placement.ConsignmentItemId
                        ) ?? new ProductPlacement {
                        RowNumber = message.ProductPlacementMovement.ToProductPlacement.RowNumber,
                        CellNumber = message.ProductPlacementMovement.ToProductPlacement.CellNumber,
                        StorageNumber = message.ProductPlacementMovement.ToProductPlacement.StorageNumber,
                        StorageId = message.ProductPlacementMovement.FromProductPlacement.StorageId,
                        ProductId = message.ProductPlacementMovement.ToProductPlacement.ProductId,
                        Qty = operationQty,
                        ConsignmentItemId = placement.ConsignmentItemId
                    };

                if (toPlacement.IsNew()) {
                    toPlacement.Id = productPlacementRepository.AddWithId(toPlacement);
                } else {
                    toPlacement.Qty += operationQty;

                    productPlacementRepository.UpdateQty(toPlacement);
                }

                placement.Qty -= operationQty;

                message.ProductPlacementMovement.Qty = operationQty;
                message.ProductPlacementMovement.FromProductPlacementId = placement.Id;
                message.ProductPlacementMovement.ToProductPlacementId = toPlacement.Id;

                message.ProductPlacementMovement.Id = productPlacementMovementRepository.Add(message.ProductPlacementMovement);

                if (placement.Qty > 0)
                    productPlacementRepository.UpdateQty(placement);
                else
                    productPlacementRepository.Remove(placement);

                toMoveQty -= operationQty;

                if (toMoveQty.Equals(0d)) break;
            }

            Sender.Tell(
                productPlacementMovementRepository
                    .GetById(
                        message.ProductPlacementMovement.Id
                    )
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetAllProductPlacementMovementsFilteredMessage(GetAllProductPlacementMovementsFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductPlacementMovementRepository(connection)
                .GetAllFiltered(
                    message.StorageNetId,
                    message.Value,
                    message.From,
                    message.To,
                    message.Limit,
                    message.Offset
                )
        );
    }
}