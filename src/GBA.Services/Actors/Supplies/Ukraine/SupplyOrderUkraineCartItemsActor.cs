using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.Helpers;
using GBA.Common.Helpers.SupplyOrders;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Messages.Supplies.Ukraine.SupplyOrderUkraineCartItems;
using GBA.Domain.Repositories.Consignments.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Supplies.Ukraine;

public sealed class SupplyOrderUkraineCartItemsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConsignmentRepositoriesFactory _consignmentRepositoriesFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IOrganizationRepositoriesFactory _organizationRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public SupplyOrderUkraineCartItemsActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IConsignmentRepositoriesFactory consignmentRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        IOrganizationRepositoriesFactory organizationRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _consignmentRepositoriesFactory = consignmentRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _organizationRepositoriesFactory = organizationRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;

        Receive<UploadSupplyOrderUkraineCartItemsFromFileMessage>(ProcessUploadSupplyOrderUkraineCartItemsFromFileMessage);

        Receive<UpdateSupplyOrderUkraineCartItemMessage>(ProcessUpdateSupplyOrderUkraineCartItemMessage);

        Receive<GetAllExistingSupplyOrderUkraineCartItemsMessage>(ProcessGetAllExistingSupplyOrderUkraineCartItemsMessage);

        Receive<UploadSelectCartItemsFileForPreviewValidationMessage>(ProcessUploadSelectCartItemsFileForPreviewValidationMessage);

        Receive<CalculateTotalsForSupplyOrderUkraineCartItemsMessage>(ProcessCalculateTotalsForSupplyOrderUkraineCartItemsMessage);

        Receive<CalculateTotalsForSalesMessage>(ProcessCalculateTotalsForSalesMessage);
    }

    private void ProcessUploadSupplyOrderUkraineCartItemsFromFileMessage(UploadSupplyOrderUkraineCartItemsFromFileMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            List<ParsedProduct> parsedProducts =
                _xlsFactoryManager
                    .NewParseConfigurationXlsManager()
                    .GetProductsFromCartItemsDocumentByConfiguration(
                        message.PathToFile,
                        message.Configuration
                    );

            IGetSingleProductRepository productRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);

            List<SupplyOrderUkraineCartItem> ukraineCartItems = new();

            foreach (ParsedProduct parsedProduct in parsedProducts) {
                Product product = productRepository.GetProductByVendorCode(parsedProduct.VendorCode);

                if (product == null) throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.NoProductByVendorCode, 0, 0, parsedProduct.VendorCode);

                ukraineCartItems.Add(new SupplyOrderUkraineCartItem {
                    UploadedQty = parsedProduct.Qty,
                    FromDate = TimeZoneInfo.ConvertTimeToUtc(parsedProduct.FromDate),
                    ItemPriority =
                        parsedProduct.Priority < 0 || parsedProduct.Priority > 3
                            ? SupplyOrderUkraineCartItemPriority.High
                            : (SupplyOrderUkraineCartItemPriority)parsedProduct.Priority,
                    Product = product,
                    ProductId = product.Id
                });
            }

            if (!ukraineCartItems.Any()) throw new Exception(SupplyOrderUkraineCartItemsResourceNames.UPLOAD_AT_LEAST_ONE_PRODUCT);

            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            ISupplyOrderUkraineCartItemRepository supplyOrderUkraineCartItemRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemRepository(connection);
            ISupplyOrderUkraineCartItemReservationRepository cartItemReservationRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemReservationRepository(connection);

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            foreach (SupplyOrderUkraineCartItem item in ukraineCartItems) {
                IEnumerable<ProductAvailability> availabilities =
                    productAvailabilityRepository
                        .GetByProductAndCultureIds(
                            item.ProductId,
                            "pl"
                        );

                double operationQty = item.UploadedQty;

                List<SupplyOrderUkraineCartItemReservation> reservations = new();

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

                item.ReservedQty = item.UploadedQty - operationQty;

                SupplyOrderUkraineCartItem fromDb = supplyOrderUkraineCartItemRepository.GetByProductIdIfExists(item.ProductId);

                if (fromDb != null) {
                    fromDb.ReservedQty += item.ReservedQty;
                    fromDb.UpdatedById = user.Id;

                    supplyOrderUkraineCartItemRepository.Update(fromDb);

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
                    item.CreatedById = user.Id;

                    item.Id = supplyOrderUkraineCartItemRepository.Add(item);

                    cartItemReservationRepository.Add(reservations.Select(reservation => {
                        reservation.SupplyOrderUkraineCartItemId = item.Id;

                        return reservation;
                    }));
                }
            }

            Sender.Tell(supplyOrderUkraineCartItemRepository.GetAll());
        } catch (SupplyDocumentParseException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdateSupplyOrderUkraineCartItemMessage(UpdateSupplyOrderUkraineCartItemMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.Item == null) throw new Exception("SupplyOrderUkraineCartItem can not be null or empty");
            if (message.Item.ReservedQty < 0) throw new Exception("ReservedQty must be more or equal to zero");

            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            ISupplyOrderUkraineCartItemRepository supplyOrderUkraineCartItemRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemRepository(connection);
            ISupplyOrderUkraineCartItemReservationRepository cartItemReservationRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemReservationRepository(connection);

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            SupplyOrderUkraineCartItem fromDb = supplyOrderUkraineCartItemRepository.GetById(message.Item.Id);

            if (fromDb == null) throw new Exception("Such item does not exists in db");

            if (fromDb.ReservedQty.Equals(message.Item.ReservedQty)) {
                Sender.Tell(
                    supplyOrderUkraineCartItemRepository.GetById(message.Item.Id)
                );

                return;
            }

            if (message.Item.ReservedQty > fromDb.ReservedQty) {
                IEnumerable<ProductAvailability> availabilities =
                    productAvailabilityRepository
                        .GetByProductAndCultureIds(
                            fromDb.ProductId,
                            "pl"
                        );

                double operationQty = message.Item.ReservedQty - fromDb.ReservedQty;

                foreach (ProductAvailability availability in availabilities) {
                    SupplyOrderUkraineCartItemReservation reservation = new() {
                        ProductAvailabilityId = availability.Id,
                        SupplyOrderUkraineCartItemId = fromDb.Id
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
            } else {
                IEnumerable<SupplyOrderUkraineCartItemReservation> reservations =
                    cartItemReservationRepository
                        .GetAllByCartItemId(fromDb.Id);

                double operationQty = fromDb.ReservedQty - message.Item.ReservedQty;

                foreach (SupplyOrderUkraineCartItemReservation reservation in reservations) {
                    if (reservation.Qty < operationQty) {
                        operationQty -= reservation.Qty;

                        reservation.ProductAvailability.Amount += reservation.Qty;

                        productAvailabilityRepository.Update(reservation.ProductAvailability);

                        reservation.Qty = 0d;
                        reservation.Deleted = true;

                        cartItemReservationRepository.Update(reservation);
                    } else {
                        reservation.Qty -= operationQty;

                        reservation.ProductAvailability.Amount += operationQty;

                        productAvailabilityRepository.Update(reservation.ProductAvailability);

                        cartItemReservationRepository.Update(reservation);

                        operationQty = 0d;
                    }

                    if (operationQty.Equals(0d)) break;
                }
            }

            message.Item.UpdatedById = user.Id;

            supplyOrderUkraineCartItemRepository.Update(message.Item);

            Sender.Tell(
                supplyOrderUkraineCartItemRepository.GetById(message.Item.Id)
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetAllExistingSupplyOrderUkraineCartItemsMessage(GetAllExistingSupplyOrderUkraineCartItemsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supplyUkraineRepositoriesFactory
                .NewSupplyOrderUkraineCartItemRepository(connection)
                .GetAll()
        );
    }

    private void ProcessUploadSelectCartItemsFileForPreviewValidationMessage(UploadSelectCartItemsFileForPreviewValidationMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            List<ParsedProduct> parsedProducts =
                _xlsFactoryManager
                    .NewParseConfigurationXlsManager()
                    .GetProductsFromCartItemsDocumentByConfiguration(
                        message.PathToFile,
                        message.Configuration
                    );

            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);

            foreach (ParsedProduct parsedProduct in from parsedProduct in parsedProducts
                     let product = getSingleProductRepository.GetProductByVendorCode(parsedProduct.VendorCode)
                     where product == null
                     select parsedProduct)
                throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.NoProductByVendorCode, 0, 0, parsedProduct.VendorCode);

            ISupplyOrderUkraineCartItemRepository supplyOrderUkraineCartItemRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineCartItemRepository(connection);

            List<PreviewCartItem> previewCartItems = new();

            foreach (ParsedProduct parsedProduct in parsedProducts) {
                Product product = getSingleProductRepository.GetProductByVendorCode(parsedProduct.VendorCode);

                SupplyOrderUkraineCartItem item = supplyOrderUkraineCartItemRepository.GetByProductIdIfExists(product.Id);

                if (item != null) {
                    item = supplyOrderUkraineCartItemRepository.GetById(item.Id);

                    double totalQty = item.ReservedQty + item.AvailableQty;

                    if (totalQty >= parsedProduct.Qty) {
                        previewCartItems.Add(new PreviewCartItem {
                            Product = product,
                            SupplyOrderUkraineCartItem = item,
                            AvailableQty = totalQty,
                            Qty = parsedProduct.Qty
                        });
                    } else {
                        if (totalQty > 0)
                            previewCartItems.Add(new PreviewCartItem {
                                HasError = true,
                                LessAvailable = true,
                                SupplyOrderUkraineCartItem = item,
                                Product = product,
                                AvailableQty = totalQty,
                                Qty = totalQty
                            });
                        else
                            previewCartItems.Add(new PreviewCartItem {
                                HasError = true,
                                ZeroAvailable = true,
                                SupplyOrderUkraineCartItem = item,
                                Product = product,
                                AvailableQty = totalQty,
                                Qty = parsedProduct.Qty
                            });
                    }
                } else {
                    previewCartItems.Add(new PreviewCartItem {
                        HasError = true,
                        NoCartItem = true,
                        Product = product,
                        Qty = parsedProduct.Qty
                    });
                }
            }

            Sender.Tell(previewCartItems);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessCalculateTotalsForSupplyOrderUkraineCartItemsMessage(CalculateTotalsForSupplyOrderUkraineCartItemsMessage message) {
        if (message.Items == null || !message.Items.Any()) {
            Sender.Tell(new { TotalQty = 0d, TotalWeight = 0d, TotalEuroAmount = 0m, TotalPlnAmount = 0m });
            return;
        }

        double totalQty = Math.Round(message.Items.Sum(i => i.UploadedQty), 2);
        double totalWeight = 0d;
        decimal totalEuroAmount = 0m;
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
        IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
        IOrganizationRepository organizationRepository = _organizationRepositoriesFactory.NewOrganizationRepository(connection);
        IConsignmentItemRepository consignmentItemRepository = _consignmentRepositoriesFactory.NewConsignmentItemRepository(connection);
        IProductWriteOffRuleRepository productWriteOffRuleRepository = _productRepositoriesFactory.NewProductWriteOffRuleRepository(connection);

        Organization organization = organizationRepository.GetByOrganizationCultureIfExists("pl");

        if (organization == null) {
            Sender.Tell(new {
                TotalQty = totalQty,
                TotalWeight = totalWeight,
                TotalEuroAmount = totalEuroAmount,
                TotalPlnAmount = 0m
            });

            return;
        }

        foreach (SupplyOrderUkraineCartItem cartItem in message.Items) {
            Product product =
                getSingleProductRepository
                    .GetByIdAndRuleLocaleWithProductGroupAndWriteOffRules(
                        cartItem.ProductId,
                        organization.Culture
                    );

            if (product == null) continue;

            ProductWriteOffRule writeOffRule;

            if (product.ProductWriteOffRules.Any()) {
                writeOffRule = product.ProductWriteOffRules.First();
            } else if (product.ProductProductGroups.Any()) {
                writeOffRule = product.ProductProductGroups.First().ProductGroup.ProductWriteOffRules.First();
            } else {
                writeOffRule = productWriteOffRuleRepository.GetByRuleLocale(organization.Culture);

                if (writeOffRule == null) {
                    productWriteOffRuleRepository.Add(new ProductWriteOffRule {
                        RuleLocale = "pl",
                        CreatedById = userRepository.GetManagerOrGBAIdByClientNetId(Guid.Empty),
                        RuleType = ProductWriteOffRuleType.ByFromDate
                    });

                    writeOffRule = productWriteOffRuleRepository.GetByRuleLocale(organization.Culture);
                }
            }

            IEnumerable<ConsignmentItem> consignmentItems =
                consignmentItemRepository
                    .GetAllAvailable(
                        organization.Id,
                        cartItem.ProductId,
                        writeOffRule.RuleType,
                        organization.Culture
                    );

            if (!consignmentItems.Any()) continue;

            ConsignmentItem firstItem = consignmentItems.First();

            if (cartItem.UploadedQty > firstItem.RemainingQty) {
                foreach (ConsignmentItem consignmentItem in consignmentItems) {
                    double currentOperationQty = cartItem.UploadedQty;

                    if (consignmentItem.RemainingQty < cartItem.UploadedQty)
                        currentOperationQty = consignmentItem.RemainingQty;

                    totalEuroAmount =
                        decimal.Round(
                            totalEuroAmount + consignmentItem.Price * Convert.ToDecimal(currentOperationQty),
                            4,
                            MidpointRounding.AwayFromZero
                        );

                    totalWeight =
                        Math.Round(totalWeight + consignmentItem.Weight * currentOperationQty, 3, MidpointRounding.AwayFromZero);

                    cartItem.UploadedQty -= currentOperationQty;
                }
            } else {
                totalEuroAmount =
                    decimal.Round(totalEuroAmount + firstItem.Price * Convert.ToDecimal(cartItem.UploadedQty), 4, MidpointRounding.AwayFromZero);

                totalWeight =
                    Math.Round(totalWeight + firstItem.Weight * cartItem.UploadedQty, 3, MidpointRounding.AwayFromZero);
            }
        }

        Sender.Tell(new {
            TotalQty = totalQty,
            TotalWeight = totalWeight,
            TotalEuroAmount = totalEuroAmount,
            TotalPlnAmount =
                decimal.Round(
                    totalEuroAmount * GetCurrentPlnExchangeRate(
                        _currencyRepositoriesFactory.NewCurrencyRepository(connection),
                        _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection)
                    ),
                    4,
                    MidpointRounding.AwayFromZero
                )
        });
    }

    private void ProcessCalculateTotalsForSalesMessage(CalculateTotalsForSalesMessage message) {
        if (message.Sales == null || !message.Sales.Any()) {
            Sender.Tell(new { TotalQty = 0d, TotalWeight = 0d, TotalEuroAmount = 0m, TotalPlnAmount = 0m });
            return;
        }

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
        IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);

        List<Sale> sales =
            saleRepository
                .GetAllByIds(
                    message.Sales.Select(s => s.Id).ToList()
                );

        CalculatePricingForSalesWithDynamicPrices(sales, exchangeRateRepository);

        double totalQty = Math.Round(sales.Sum(s => s.Order.OrderItems.Sum(i => i.Qty)), 2);
        decimal totalEuroAmount = sales.Sum(s => decimal.Round(s.TotalAmount, 3, MidpointRounding.AwayFromZero));
        decimal totalPlnAmount = sales.Sum(s => decimal.Round(s.TotalAmountLocal, 3, MidpointRounding.AwayFromZero));

        Sender.Tell(new {
            TotalQty = totalQty,
            TotalWeight = saleRepository.GetCalculatedTotalWeightFromConsignmentsBySaleIds(sales.Select(s => s.Id)),
            TotalEuroAmount = totalEuroAmount,
            TotalPlnAmount = totalPlnAmount
        });
    }

    private static decimal GetCurrentPlnExchangeRate(
        ICurrencyRepository currencyRepository,
        IExchangeRateRepository exchangeRateRepository) {
        Currency pln = currencyRepository.GetPLNCurrencyIfExists();

        if (pln == null) return 1m;

        ExchangeRate exchangeRate =
            exchangeRateRepository
                .GetByCurrencyIdAndCode(
                    pln.Id,
                    "EUR",
                    DateTime.UtcNow.AddDays(-1)
                );

        return exchangeRate?.Amount ?? 1m;
    }

    private static void CalculatePricingForSalesWithDynamicPrices(
        List<Sale> sales,
        IExchangeRateRepository exchangeRateRepository) {
        sales.ForEach(sale => {
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
                }
            }

            sale.Order.TotalAmount = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmount), 2, MidpointRounding.AwayFromZero);
            sale.Order.TotalAmountLocal = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmountLocal), 2, MidpointRounding.AwayFromZero);
            sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);

            sale.TotalAmount = sale.Order.TotalAmount;
            sale.TotalAmountLocal = sale.Order.TotalAmountLocal;
            sale.TotalCount = sale.Order.TotalCount;
        });
    }
}