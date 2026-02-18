using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.Helpers.SupplyOrders;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Messages.Consignments;
using GBA.Domain.Messages.Products.ProductCapitalizations;
using GBA.Domain.Repositories.Consignments.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Products;

public sealed class ProductCapitalizationActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConsignmentRepositoriesFactory _consignmentRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public ProductCapitalizationActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IConsignmentRepositoriesFactory consignmentRepositoriesFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _consignmentRepositoriesFactory = consignmentRepositoriesFactory;

        Receive<AddNewProductCapitalizationFromFileMessage>(ProcessAddNewProductCapitalizationFromFileMessage);

        Receive<AddNewProductCapitalizationMessage>(ProcessAddNewProductCapitalizationMessage);

        Receive<GetProductCapitalizationByNetIdMessage>(ProcessGetProductCapitalizationByNetIdMessage);

        Receive<GetAllProductCapitalizationsFilteredMessage>(ProcessGetAllProductCapitalizationsFilteredMessage);

        Receive<GetProductCapitalizationItemsFromFileMessage>(ProcessGetProductCapitalizationItemsFromFileMessage);

        Receive<ExportProductCapitalizationDocumentMessage>(ProcessExportProductCapitalizationDocumentMessage);
    }

    private void ProcessAddNewProductCapitalizationFromFileMessage(AddNewProductCapitalizationFromFileMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.ProductCapitalization == null)
                throw new Exception(ProductCapitalizationResourceNames.PRODUCT_CAPITALIZATION_EMPTY);
            if (message.ProductCapitalization.Organization == null || message.ProductCapitalization.Organization.IsNew())
                throw new Exception(ProductCapitalizationResourceNames.ORGANIZATION_EMPTY);
            if (message.ProductCapitalization.Storage == null || message.ProductCapitalization.Storage.IsNew())
                throw new Exception(ProductCapitalizationResourceNames.STORAGE_EMPTY);

            List<ParsedProduct> parsedProducts =
                _xlsFactoryManager
                    .NewParseConfigurationXlsManager()
                    .GetProductsFromUploadForCapitalizationByConfiguration(
                        message.Path,
                        message.Configuration
                    );

            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);

            foreach (ParsedProduct parsedProduct in parsedProducts) {
                Product product = getSingleProductRepository.GetProductByVendorCode(parsedProduct.VendorCode);

                if (product == null)
                    throw new LocalizedException(
                        ProductCapitalizationResourceNames.PRODUCT_NOT_EXIST,
                        new object[] { parsedProduct.VendorCode });

                message
                    .ProductCapitalization
                    .ProductCapitalizationItems
                    .Add(new ProductCapitalizationItem {
                        ProductId = product.Id,
                        Qty = parsedProduct.Qty,
                        RemainingQty = parsedProduct.Qty,
                        UnitPrice = parsedProduct.UnitPrice,
                        Weight = parsedProduct.GrossWeight
                    });
            }

            IProductIncomeRepository productIncomeRepository = _productRepositoriesFactory.NewProductIncomeRepository(connection);
            IProductIncomeItemRepository productIncomeItemRepository = _productRepositoriesFactory.NewProductIncomeItemRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IProductCapitalizationRepository productCapitalizationRepository = _productRepositoriesFactory.NewProductCapitalizationRepository(connection);
            IProductCapitalizationItemRepository productCapitalizationItemRepository = _productRepositoriesFactory.NewProductCapitalizationItemRepository(connection);
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            message.ProductCapitalization.FromDate =
                message.ProductCapitalization.FromDate.Year.Equals(1)
                    ? DateTime.UtcNow
                    : TimeZoneInfo.ConvertTimeToUtc(message.ProductCapitalization.FromDate);

            string prefix = message.ProductCapitalization.Organization.Code ?? string.Empty;

            ProductCapitalization lastRecord = productCapitalizationRepository.GetLastRecord(prefix);

            if (lastRecord != null && lastRecord.FromDate.Year.Equals(DateTime.UtcNow.Year))
                message.ProductCapitalization.Number =
                    string.Format(
                        "{0}{1}",
                        prefix,
                        string.Format(
                            "{0:D11}",
                            Convert.ToInt64(
                                lastRecord.Number.Substring(prefix.Length, lastRecord.Number.Length - prefix.Length)
                            ) + 1
                        )
                    );
            else
                message.ProductCapitalization.Number =
                    string.Format(
                        "{0}{1}",
                        prefix,
                        string.Format(
                            "{0:D11}",
                            1
                        )
                    );

            message.ProductCapitalization.OrganizationId = message.ProductCapitalization.Organization.Id;
            message.ProductCapitalization.StorageId = message.ProductCapitalization.Storage.Id;

            message.ProductCapitalization.ResponsibleId =
                _userRepositoriesFactory
                    .NewUserRepository(connection)
                    .GetByNetIdWithoutIncludes(
                        message.UserNetId
                    ).Id;

            message.ProductCapitalization.Id = productCapitalizationRepository.Add(message.ProductCapitalization);

            ProductIncome productIncome = new() {
                StorageId = message.ProductCapitalization.StorageId,
                UserId = message.ProductCapitalization.ResponsibleId,
                FromDate = message.ProductCapitalization.FromDate,
                Number = message.ProductCapitalization.Number,
                ProductIncomeType = ProductIncomeType.Capitalization
            };

            productIncome.Id = productIncomeRepository.Add(productIncome);

            foreach (ProductCapitalizationItem item in message.ProductCapitalization.ProductCapitalizationItems) {
                item.RemainingQty = item.Qty;
                item.ProductCapitalizationId = message.ProductCapitalization.Id;

                item.Id = productCapitalizationItemRepository.Add(item);

                ProductAvailability availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            item.ProductId,
                            message.ProductCapitalization.StorageId
                        );

                if (availability == null) {
                    availability = new ProductAvailability {
                        StorageId = message.ProductCapitalization.StorageId,
                        ProductId = item.ProductId,
                        Amount = item.Qty
                    };

                    availability.Id = productAvailabilityRepository.AddWithId(availability);
                } else {
                    availability.Amount += item.Qty;

                    productAvailabilityRepository.Update(availability);
                }

                ProductIncomeItem incomeItem = new() {
                    ProductIncomeId = productIncome.Id,
                    ProductCapitalizationItemId = item.Id,
                    Qty = item.Qty,
                    RemainingQty = item.RemainingQty
                };
                productPlacementRepository.Add(new ProductPlacement {
                    Qty = item.Qty,
                    ProductId = item.ProductId,
                    StorageId = message.ProductCapitalization.StorageId,
                    //ConsignmentItemId = consignmentItem.Id,
                    RowNumber = "N",
                    CellNumber = "N",
                    StorageNumber = "N"
                });
                incomeItem.Id = productIncomeItemRepository.Add(incomeItem);
            }

            ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new AddNewConsignmentMessage(productIncome.Id, false));

            Sender.Tell(
                productCapitalizationRepository
                    .GetById(
                        message.ProductCapitalization.Id
                    )
            );
        } catch (LocalizedException exc) {
            Sender.Tell(exc);
        } catch (SupplyDocumentParseException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private async void ProcessAddNewProductCapitalizationMessage(AddNewProductCapitalizationMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.ProductCapitalization == null) throw new Exception(ProductCapitalizationResourceNames.PRODUCT_CAPITALIZATION_EMPTY);
            if (message.ProductCapitalization.Organization == null || message.ProductCapitalization.Organization.IsNew())
                throw new Exception(ProductCapitalizationResourceNames.ORGANIZATION_EMPTY);
            if (message.ProductCapitalization.Storage == null || message.ProductCapitalization.Storage.IsNew())
                throw new Exception(ProductCapitalizationResourceNames.STORAGE_EMPTY);
            if (!message.ProductCapitalization.ProductCapitalizationItems.Any())
                throw new Exception(ProductCapitalizationResourceNames.NEED_ITEM_PRODUCT_CAPITALIZATION);
            if (!message.ProductCapitalization.ProductCapitalizationItems.All(i => i.Product != null && !i.Product.IsNew() && i.Qty > 0))
                throw new Exception(ProductCapitalizationResourceNames.ITEM_NOT_HAVE_QTY_OR_PRODUCT);

            IProductIncomeRepository productIncomeRepository = _productRepositoriesFactory.NewProductIncomeRepository(connection);
            IProductIncomeItemRepository productIncomeItemRepository = _productRepositoriesFactory.NewProductIncomeItemRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IProductCapitalizationRepository productCapitalizationRepository = _productRepositoriesFactory.NewProductCapitalizationRepository(connection);
            IProductCapitalizationItemRepository productCapitalizationItemRepository = _productRepositoriesFactory.NewProductCapitalizationItemRepository(connection);
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);

            message.ProductCapitalization.FromDate =
                message.ProductCapitalization.FromDate.Year.Equals(1)
                    ? DateTime.UtcNow
                    : TimeZoneInfo.ConvertTimeToUtc(message.ProductCapitalization.FromDate);

            string prefix = message.ProductCapitalization.Organization.Code ?? string.Empty;

            ProductCapitalization lastRecord = productCapitalizationRepository.GetLastRecord(prefix);

            if (lastRecord != null && lastRecord.FromDate.Year.Equals(DateTime.UtcNow.Year))
                message.ProductCapitalization.Number =
                    string.Format(
                        "{0}{1}",
                        prefix,
                        string.Format(
                            "{0:D10}",
                            Convert.ToInt64(
                                lastRecord.Number.Substring(prefix.Length, lastRecord.Number.Length - prefix.Length)
                            ) + 1
                        )
                    );
            else
                message.ProductCapitalization.Number =
                    string.Format(
                        "{0}{1}",
                        prefix,
                        string.Format(
                            "{0:D10}",
                            1
                        )
                    );

            message.ProductCapitalization.OrganizationId = message.ProductCapitalization.Organization.Id;
            message.ProductCapitalization.StorageId = message.ProductCapitalization.Storage.Id;

            message.ProductCapitalization.ResponsibleId =
                _userRepositoriesFactory
                    .NewUserRepository(connection)
                    .GetByNetIdWithoutIncludes(
                        message.UserNetId
                    ).Id;

            message.ProductCapitalization.Id = productCapitalizationRepository.Add(message.ProductCapitalization);

            ProductIncome productIncome = new() {
                StorageId = message.ProductCapitalization.StorageId,
                UserId = message.ProductCapitalization.ResponsibleId,
                FromDate = message.ProductCapitalization.FromDate,
                ProductIncomeType = ProductIncomeType.Capitalization,
                Number = message.ProductCapitalization.Number
            };

            productIncome.Id = productIncomeRepository.Add(productIncome);
            List<ProductPlacement> placementList = new();
            foreach (ProductCapitalizationItem item in message.ProductCapitalization.ProductCapitalizationItems) {
                item.RemainingQty = item.Qty;
                item.ProductId = item.Product.Id;
                item.ProductCapitalizationId = message.ProductCapitalization.Id;

                item.Id = productCapitalizationItemRepository.Add(item);

                ProductAvailability availability =
                    productAvailabilityRepository
                        .GetByProductAndStorageIds(
                            item.ProductId,
                            message.ProductCapitalization.StorageId
                        );

                if (availability == null) {
                    availability = new ProductAvailability {
                        StorageId = message.ProductCapitalization.StorageId,
                        ProductId = item.ProductId,
                        Amount = item.Qty
                    };

                    availability.Id = productAvailabilityRepository.AddWithId(availability);
                } else {
                    availability.Amount += item.Qty;

                    productAvailabilityRepository.Update(availability);
                }

                ProductIncomeItem incomeItem = new() {
                    ProductIncomeId = productIncome.Id,
                    ProductCapitalizationItemId = item.Id,
                    Qty = item.Qty,
                    RemainingQty = item.RemainingQty
                };
                incomeItem.Id = productIncomeItemRepository.Add(incomeItem);

                ProductPlacement productPlacement = new() {
                    Qty = item.Qty,
                    ProductId = item.ProductId,
                    StorageId = message.ProductCapitalization.StorageId,
                    RowNumber = "N",
                    CellNumber = "N",
                    StorageNumber = "N",
                    ProductIncomeItemId = incomeItem.Id
                };
                productPlacementRepository.Add(productPlacement);
            }

            ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new AddNewConsignmentMessage(productIncome.Id, false));
            Sender.Tell(
                productCapitalizationRepository
                    .GetById(
                        message.ProductCapitalization.Id
                    )
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetProductCapitalizationByNetIdMessage(GetProductCapitalizationByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductCapitalizationRepository(connection)
                .GetByNetId(
                    message.NetId
                )
        );
    }

    private void ProcessGetAllProductCapitalizationsFilteredMessage(GetAllProductCapitalizationsFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductCapitalizationRepository(connection)
                .GetAllFiltered(
                    message.From,
                    message.To,
                    message.Limit,
                    message.Offset
                )
        );
    }

    private void ProcessGetProductCapitalizationItemsFromFileMessage(GetProductCapitalizationItemsFromFileMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            List<ProductCapitalizationItem> items = new();
            List<string> missingVendorCodes = new();

            List<ParsedProduct> parsedProducts =
                _xlsFactoryManager
                    .NewParseConfigurationXlsManager()
                    .GetProductsFromUploadForCapitalizationByConfiguration(
                        message.Path,
                        message.Configuration
                    );

            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);

            foreach (ParsedProduct parsedProduct in parsedProducts) {
                Product product = getSingleProductRepository.GetProductByVendorCodeWithMeasureUnit(parsedProduct.VendorCode);

                if (product == null)
                    missingVendorCodes.Add(parsedProduct.VendorCode);
                else
                    items
                        .Add(new ProductCapitalizationItem {
                            ProductId = product.Id,
                            Product = product,
                            Qty = parsedProduct.Qty,
                            RemainingQty = parsedProduct.Qty,
                            UnitPrice = parsedProduct.UnitPrice,
                            Weight = parsedProduct.GrossWeight
                        });
            }

            Sender.Tell(new Tuple<List<ProductCapitalizationItem>, List<string>>(items, missingVendorCodes));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessExportProductCapitalizationDocumentMessage(ExportProductCapitalizationDocumentMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ProductCapitalization productCapitalization =
                _productRepositoriesFactory
                    .NewProductCapitalizationRepository(connection)
                    .GetByNetId(message.NetId);

            (string excelFilePath, string pdfFilePath) =
                _xlsFactoryManager
                    .NewProductsXlsManager()
                    .ExportProductCapitalizationToXlsx(
                        message.PathToFolder,
                        productCapitalization
                    );

            Sender.Tell(new Tuple<string, string>(excelFilePath, pdfFilePath));
        } catch (Exception) {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }
}