using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Messages.Products.ProductSpecifications;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Products;

public sealed class ProductSpecificationsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public ProductSpecificationsActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;

        Receive<UpdateInvoiceProductSpecificationAssignmentsMessage>(ProcessUpdateInvoiceProductSpecificationAssignmentsMessage);

        Receive<UpdateSadProductSpecificationAssignmentsMessage>(ProcessUpdateSadProductSpecificationAssignmentsMessage);

        Receive<AddOrUpdateProductSpecificationMessage>(ProcessAddOrUpdateProductSpecificationMessage);

        Receive<GetAllProductSpecificationsFilteredMessage>(ProcessGetAllProductSpecificationsFilteredMessage);

        Receive<ChangeProductSpecificationBySelectedModeMessage>(ProcessChangeProductSpecificationBySelectedModeMessage);
    }

    private void ProcessUpdateInvoiceProductSpecificationAssignmentsMessage(UpdateInvoiceProductSpecificationAssignmentsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IProductSpecificationRepository productSpecificationRepository = _productRepositoriesFactory.NewProductSpecificationRepository(connection);
        IOrderProductSpecificationRepository orderProductSpecificationRepository = _supplyRepositoriesFactory.NewOrderProductSpecificationRepository(connection);

        SupplyInvoice invoice =
            _supplyRepositoriesFactory
                .NewSupplyInvoiceRepository(connection)
                .GetByNetIdWithProducts(
                    message.InvoiceNetId
                );

        if (invoice == null) return;

        foreach (SupplyInvoiceOrderItem orderItem in invoice.SupplyInvoiceOrderItems) {
            ProductSpecification specification =
                productSpecificationRepository
                    .GetByProductAndSupplyInvoiceIdsIfExists(
                        orderItem.ProductId,
                        invoice.Id
                    );

            if (specification != null) continue;

            ProductSpecification activeSpecification =
                productSpecificationRepository
                    .GetActiveByProductIdAndLocale(
                        orderItem.ProductId,
                        invoice.SupplyOrder.Organization.Culture
                    );

            if (activeSpecification == null) continue;

            productSpecificationRepository.SetInactiveByProductId(orderItem.ProductId, invoice.SupplyOrder.Organization.Culture);

            specification = new ProductSpecification {
                IsActive = true,
                Name = activeSpecification.Name,
                SpecificationCode = activeSpecification.SpecificationCode,
                Locale = activeSpecification.Locale,
                AddedById = activeSpecification.AddedById,
                ProductId = orderItem.ProductId
            };

            specification.Id = productSpecificationRepository.Add(specification);

            orderProductSpecificationRepository
                .Add(new OrderProductSpecification {
                    Qty = orderItem.Qty,
                    SupplyInvoiceId = invoice.Id,
                    ProductSpecificationId = specification.Id
                });
        }
    }

    private void ProcessUpdateSadProductSpecificationAssignmentsMessage(UpdateSadProductSpecificationAssignmentsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IProductSpecificationRepository productSpecificationRepository = _productRepositoriesFactory.NewProductSpecificationRepository(connection);
        IOrderProductSpecificationRepository orderProductSpecificationRepository = _supplyRepositoriesFactory.NewOrderProductSpecificationRepository(connection);

        Sad sad =
            _supplyUkraineRepositoriesFactory
                .NewSadRepository(connection, null)
                .GetByNetIdWithItems(
                    message.SadNetId
                );

        if (sad == null) return;

        foreach (SadItem sadItem in sad.SadItems) {
            ProductSpecification specification =
                productSpecificationRepository
                    .GetByProductAndSadIdsIfExists(
                        sadItem.OrderItem?.ProductId ?? sadItem.SupplyOrderUkraineCartItem.ProductId,
                        sad.Id,
                        "uk"
                    );

            if (specification != null) continue;

            ProductSpecification activeSpecification =
                productSpecificationRepository
                    .GetActiveByProductIdAndLocale(
                        sadItem.OrderItem?.ProductId ?? sadItem.SupplyOrderUkraineCartItem.ProductId,
                        "uk"
                    );

            if (activeSpecification == null) continue;

            productSpecificationRepository
                .SetInactiveByProductId(
                    sadItem.OrderItem?.ProductId ?? sadItem.SupplyOrderUkraineCartItem.ProductId,
                    "uk"
                );

            specification = new ProductSpecification {
                IsActive = true,
                Name = activeSpecification.Name,
                SpecificationCode = activeSpecification.SpecificationCode,
                Locale = activeSpecification.Locale,
                AddedById = activeSpecification.AddedById,
                ProductId = sadItem.OrderItem?.ProductId ?? sadItem.SupplyOrderUkraineCartItem.ProductId,
                DutyPercent = activeSpecification.DutyPercent
            };

            specification.Id = productSpecificationRepository.Add(specification);

            orderProductSpecificationRepository
                .Add(new OrderProductSpecification {
                    Qty = sadItem.Qty,
                    SadId = sad.Id,
                    ProductSpecificationId = specification.Id
                });

            specification =
                productSpecificationRepository
                    .GetByProductAndSadIdsIfExists(
                        sadItem.OrderItem?.ProductId ?? sadItem.SupplyOrderUkraineCartItem.ProductId,
                        sad.Id,
                        "pl"
                    );

            if (specification != null) continue;

            activeSpecification =
                productSpecificationRepository
                    .GetActiveByProductIdAndLocale(
                        sadItem.OrderItem?.ProductId ?? sadItem.SupplyOrderUkraineCartItem.ProductId,
                        "pl"
                    );

            if (activeSpecification == null) continue;

            productSpecificationRepository
                .SetInactiveByProductId(
                    sadItem.OrderItem?.ProductId ?? sadItem.SupplyOrderUkraineCartItem.ProductId,
                    "pl"
                );

            specification = new ProductSpecification {
                IsActive = true,
                Name = activeSpecification.Name,
                SpecificationCode = activeSpecification.SpecificationCode,
                Locale = activeSpecification.Locale,
                AddedById = activeSpecification.AddedById,
                ProductId = sadItem.OrderItem?.ProductId ?? sadItem.SupplyOrderUkraineCartItem.ProductId,
                DutyPercent = activeSpecification.DutyPercent
            };

            specification.Id = productSpecificationRepository.Add(specification);

            orderProductSpecificationRepository
                .Add(new OrderProductSpecification {
                    Qty = sadItem.Qty,
                    SadId = sad.Id,
                    ProductSpecificationId = specification.Id
                });
        }
    }

    private void ProcessAddOrUpdateProductSpecificationMessage(AddOrUpdateProductSpecificationMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            if (message.Specification == null)
                throw new Exception(ProductSpecificationResourceNames.PRODUCT_SPECIFICATION_EMPTY);

            if (message.Specification.ProductId == 0)
                throw new Exception(ProductSpecificationResourceNames.PRODUCT_ID_EMPTY);

            ISupplyInvoiceRepository supplyInvoiceRepository = _supplyRepositoriesFactory.NewSupplyInvoiceRepository(connection);
            IProductSpecificationRepository specificationRepository = _productRepositoriesFactory.NewProductSpecificationRepository(connection);

            SupplyInvoice invoice =
                supplyInvoiceRepository
                    .GetByNetIdAndProductIdWithSupplyOrderIncludes(
                        message.SupplyInvoiceNetId,
                        message.Specification.ProductId
                    );

            Sad sad =
                _supplyUkraineRepositoriesFactory
                    .NewSadRepository(connection, null)
                    .GetByNetIdAndProductId(
                        message.SadNetId,
                        message.Specification.ProductId
                    );

            if (invoice == null && sad == null)
                throw new Exception(ProductSpecificationResourceNames.PRODUCT_EMPTY);

            User currentUser = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

            if (invoice != null) {
                specificationRepository.SetInactiveByProductId(message.Specification.ProductId, invoice.SupplyOrder.Organization.Culture);

                ProductSpecification specification = new() {
                    Locale = invoice.SupplyOrder.Organization.Culture,
                    Name = message.Specification.Name,
                    SpecificationCode = message.Specification.SpecificationCode,
                    ProductId = message.Specification.ProductId,
                    CustomsValue = message.Specification.CustomsValue,
                    Duty = message.Specification.Duty,
                    VATValue = message.Specification.VATValue,
                    DutyPercent = message.Specification.CustomsValue > 0
                        ? decimal.Round(message.Specification.Duty * 100 / message.Specification.CustomsValue,
                            2,
                            MidpointRounding.AwayFromZero)
                        : 0,
                    VATPercent = message.Specification.CustomsValue + message.Specification.Duty > 0
                        ? message.Specification.VATValue * 100 / (message.Specification.CustomsValue + message.Specification.Duty)
                        : 0,
                    AddedById = currentUser.Id,
                    IsActive = true
                };

                specification.Id = specificationRepository.Add(specification);

                _supplyRepositoriesFactory
                    .NewOrderProductSpecificationRepository(connection)
                    .Add(new OrderProductSpecification {
                        Qty = invoice.SupplyInvoiceOrderItems.Sum(i => i.Qty),
                        ProductSpecificationId = specification.Id,
                        SupplyInvoiceId = invoice.Id
                    });

                Sender.Tell(
                    specificationRepository
                        .GetByProductAndSupplyOrderIdsIfExists(
                            message.Specification.ProductId,
                            invoice.SupplyOrderId
                        )
                );
            } else {
                specificationRepository.SetInactiveByProductId(message.Specification.ProductId, CultureInfo.CurrentCulture.TwoLetterISOLanguageName);

                ProductSpecification specification = new() {
                    Locale = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                    Name = message.Specification.Name,
                    SpecificationCode = message.Specification.SpecificationCode,
                    ProductId = message.Specification.ProductId,
                    CustomsValue = message.Specification.CustomsValue,
                    Duty = message.Specification.Duty,
                    VATValue = message.Specification.VATValue,
                    DutyPercent = message.Specification.CustomsValue + message.Specification.Duty > 0
                        ? decimal.Round(message.Specification.VATValue * 100 / (message.Specification.CustomsValue + message.Specification.Duty),
                            2,
                            MidpointRounding.AwayFromZero)
                        : 0,
                    AddedById = currentUser.Id,
                    IsActive = true
                };

                specification.Id = specificationRepository.Add(specification);

                _supplyRepositoriesFactory
                    .NewOrderProductSpecificationRepository(connection)
                    .Add(new OrderProductSpecification {
                        Qty = sad.SadItems.Sum(i => i.Qty),
                        ProductSpecificationId = specification.Id,
                        SadId = sad.Id
                    });

                Sender.Tell(
                    specificationRepository
                        .GetByProductAndSadIdsIfExists(
                            message.Specification.ProductId,
                            sad.Id,
                            CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                        )
                );
            }
        } catch (Exception e) {
            Sender.Tell(e);
        }
    }

    private void ProcessGetAllProductSpecificationsFilteredMessage(GetAllProductSpecificationsFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductSpecificationRepository(connection)
                .GetAllProductSpecificationsFiltered(
                    message.VendorCode,
                    message.SpecificationCode,
                    message.Locale,
                    message.Limit <= 0 ? 20 : message.Limit,
                    message.Offset < 0 ? 0 : message.Offset
                )
        );
    }

    private void ProcessChangeProductSpecificationBySelectedModeMessage(ChangeProductSpecificationBySelectedModeMessage message) {
        if (message?.Specification == null) {
            Sender.Tell(new Exception(ProductSpecificationResourceNames.PRODUCT_SPECIFICATION_EMPTY));
            return;
        }

        if (message.Specification.ProductId == 0) {
            Sender.Tell(new Exception(ProductSpecificationResourceNames.PRODUCT_ID_EMPTY));
            return;
        }

        switch (message.SpecificationChangeMode) {
            case ProductSpecificationChangeMode.SingleProduct:
                ChangeSpecificationForSingleProduct(message);
                break;
            case ProductSpecificationChangeMode.AllProductsByName:
                ChangeSpecificationForAllProductsProductByName(message);
                break;
            case ProductSpecificationChangeMode.AllProductsByCode:
                ChangeSpecificationForAllProductsProductByCode(message);
                break;
            default:
                Sender.Tell(new Exception(ProductSpecificationResourceNames.UNSUPPORTED_SPECIFICATION_CHANGE_MODE));
                return;
        }

        Sender.Tell(null);
    }

    private void ChangeSpecificationForSingleProduct(ChangeProductSpecificationBySelectedModeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IProductSpecificationRepository specificationRepository = _productRepositoriesFactory.NewProductSpecificationRepository(connection);

        User addedBy = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

        ProductSpecification activeSpecification =
            specificationRepository
                .GetActiveByProductIdAndLocale(
                    message.Specification.ProductId,
                    message.Specification.Locale
                );

        if (activeSpecification == null) {
            activeSpecification = new ProductSpecification {
                AddedById = addedBy.Id,
                ProductId = message.Specification.ProductId,
                SpecificationCode = message.Specification.SpecificationCode,
                Name = message.Specification.Name,
                Locale = message.Specification.Locale,
                CustomsValue = message.Specification.CustomsValue,
                Duty = message.Specification.Duty,
                VATValue = message.Specification.VATValue,
                DutyPercent = message.Specification.CustomsValue + message.Specification.Duty > 0
                    ? decimal.Round(message.Specification.VATValue * 100 / (message.Specification.CustomsValue + message.Specification.Duty),
                        2,
                        MidpointRounding.AwayFromZero)
                    : 0,
                IsActive = true
            };

            specificationRepository.Add(activeSpecification);
        } else {
            if (activeSpecification.SpecificationCode == message.Specification.SpecificationCode
                && activeSpecification.CustomsValue == message.Specification.CustomsValue
                && activeSpecification.Duty == message.Specification.Duty
                && activeSpecification.VATValue == message.Specification.VATValue) return;

            specificationRepository
                .SetInactiveByProductId(
                    message.Specification.ProductId,
                    message.Specification.Locale
                );

            specificationRepository.Add(new ProductSpecification {
                AddedById = addedBy.Id,
                ProductId = message.Specification.ProductId,
                Name = message.Specification.Name,
                Locale = message.Specification.Locale,
                SpecificationCode = message.Specification.SpecificationCode,
                CustomsValue = message.Specification.CustomsValue,
                Duty = message.Specification.Duty,
                VATValue = message.Specification.VATValue,
                DutyPercent = message.Specification.CustomsValue + message.Specification.Duty > 0
                    ? decimal.Round(message.Specification.VATValue * 100 / (message.Specification.CustomsValue + message.Specification.Duty),
                        2,
                        MidpointRounding.AwayFromZero)
                    : 0,
                IsActive = true
            });
        }
    }

    private void ChangeSpecificationForAllProductsProductByName(ChangeProductSpecificationBySelectedModeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IProductSpecificationRepository specificationRepository = _productRepositoriesFactory.NewProductSpecificationRepository(connection);

        User addedBy = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

        ProductSpecification activeSpecification =
            specificationRepository
                .GetActiveByProductIdAndLocale(
                    message.Specification.ProductId,
                    message.Specification.Locale
                );

        if (activeSpecification == null) {
            IEnumerable<Product> products =
                _productRepositoriesFactory
                    .NewGetMultipleProductsRepository(connection)
                    .GetAllWithoutActiveSpecificationByLocale(
                        message.Specification.Locale
                    );

            foreach (Product product in products)
                specificationRepository.Add(new ProductSpecification {
                    AddedById = addedBy.Id,
                    ProductId = product.Id,
                    Name = message.Specification.Name,
                    Locale = message.Specification.Locale,
                    SpecificationCode = message.Specification.SpecificationCode,
                    CustomsValue = message.Specification.CustomsValue,
                    Duty = message.Specification.Duty,
                    VATValue = message.Specification.VATValue,
                    DutyPercent = message.Specification.CustomsValue + message.Specification.Duty > 0
                        ? decimal.Round(message.Specification.VATValue * 100 / (message.Specification.CustomsValue + message.Specification.Duty),
                            2,
                            MidpointRounding.AwayFromZero)
                        : 0,
                    IsActive = true
                });
        } else {
            IEnumerable<Product> products =
                _productRepositoriesFactory
                    .NewGetMultipleProductsRepository(connection)
                    .GetAllFilteredByActiveSpecificationNameByLocale(
                        activeSpecification.Name,
                        message.Specification.Locale
                    );

            foreach (Product product in products) {
                specificationRepository
                    .SetInactiveByProductId(
                        product.Id,
                        message.Specification.Locale
                    );

                specificationRepository.Add(new ProductSpecification {
                    AddedById = addedBy.Id,
                    ProductId = product.Id,
                    Name = message.Specification.Name,
                    Locale = message.Specification.Locale,
                    SpecificationCode = message.Specification.SpecificationCode,
                    CustomsValue = message.Specification.CustomsValue,
                    Duty = message.Specification.Duty,
                    VATValue = message.Specification.VATValue,
                    VATPercent = message.Specification.CustomsValue + message.Specification.Duty > 0
                        ? decimal.Round(message.Specification.VATValue * 100 / (message.Specification.CustomsValue + message.Specification.Duty),
                            2,
                            MidpointRounding.AwayFromZero)
                        : 0,
                    DutyPercent = message.Specification.CustomsValue > 0
                        ? decimal.Round(message.Specification.Duty * 100 / message.Specification.CustomsValue,
                            2,
                            MidpointRounding.AwayFromZero)
                        : 0,
                    IsActive = true
                });
            }
        }
    }

    private void ChangeSpecificationForAllProductsProductByCode(ChangeProductSpecificationBySelectedModeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IProductSpecificationRepository specificationRepository = _productRepositoriesFactory.NewProductSpecificationRepository(connection);

        User addedBy = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

        ProductSpecification activeSpecification =
            specificationRepository
                .GetActiveByProductIdAndLocale(
                    message.Specification.ProductId,
                    message.Specification.Locale
                );

        if (activeSpecification == null) {
            IEnumerable<Product> products =
                _productRepositoriesFactory
                    .NewGetMultipleProductsRepository(connection)
                    .GetAllWithoutActiveSpecificationByLocale(
                        message.Specification.Locale
                    );

            foreach (Product product in products)
                specificationRepository.Add(new ProductSpecification {
                    AddedById = addedBy.Id,
                    ProductId = product.Id,
                    Name = message.Specification.Name,
                    Locale = message.Specification.Locale,
                    SpecificationCode = message.Specification.SpecificationCode,
                    CustomsValue = message.Specification.CustomsValue,
                    Duty = message.Specification.Duty,
                    VATValue = message.Specification.VATValue,
                    DutyPercent = message.Specification.CustomsValue + message.Specification.Duty > 0
                        ? decimal.Round(message.Specification.VATValue * 100 / (message.Specification.CustomsValue + message.Specification.Duty),
                            2,
                            MidpointRounding.AwayFromZero)
                        : 0,
                    IsActive = true
                });
        } else {
            IEnumerable<Product> products =
                _productRepositoriesFactory
                    .NewGetMultipleProductsRepository(connection)
                    .GetAllFilteredByActiveSpecificationCodeByLocale(
                        activeSpecification.SpecificationCode,
                        message.Specification.Locale
                    );

            foreach (Product product in products) {
                specificationRepository
                    .SetInactiveByProductId(
                        product.Id,
                        message.Specification.Locale
                    );

                specificationRepository.Add(new ProductSpecification {
                    AddedById = addedBy.Id,
                    ProductId = product.Id,
                    Name = message.Specification.Name,
                    Locale = message.Specification.Locale,
                    SpecificationCode = message.Specification.SpecificationCode,
                    CustomsValue = message.Specification.CustomsValue,
                    Duty = message.Specification.Duty,
                    VATValue = message.Specification.VATValue,
                    DutyPercent = message.Specification.CustomsValue + message.Specification.Duty > 0
                        ? decimal.Round(message.Specification.VATValue * 100 / (message.Specification.CustomsValue + message.Specification.Duty),
                            2,
                            MidpointRounding.AwayFromZero)
                        : 0,
                    IsActive = true
                });
            }
        }
    }
}