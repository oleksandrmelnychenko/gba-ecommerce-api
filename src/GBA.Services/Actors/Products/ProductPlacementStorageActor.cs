using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Messages.Products.ProductPlacementMovements;
using GBA.Domain.Repositories.Consignments.Contracts;
using GBA.Domain.Repositories.DocumentMonths.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Products;

public sealed class ProductPlacementStorageActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConsignmentRepositoriesFactory _consignmentRepositoriesFactory;
    private readonly IDocumentMonthRepositoryFactory _documentMonthRepositoryFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public ProductPlacementStorageActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IXlsFactoryManager xlsFactoryManager,
        IStorageRepositoryFactory storageRepositoryFactory,
        IConsignmentRepositoriesFactory consignmentRepositoriesFactory,
        IDocumentMonthRepositoryFactory documentMonthRepositoryFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _xlsFactoryManager = xlsFactoryManager;
        _storageRepositoryFactory = storageRepositoryFactory;
        _consignmentRepositoriesFactory = consignmentRepositoriesFactory;
        _documentMonthRepositoryFactory = documentMonthRepositoryFactory;


        Receive<UploadPlacementMovementsileMessage>(ProcessUploadPlacementMovementsileMessage);

        Receive<GetAllProductPlacementStorageFilteredMessage>(ProcessGetAllProductPlacementStorageFilteredMessage);

        Receive<GetCreateDocumentProductPlacementStorageMessage>(ProcessGetCreateDocumentShipmentFilteredMessage);

        Receive<GetCreateDocumentProductPlacementStoragesMessage>(ProcessGetCreateDocumentProductPlacementStorageFilteredMessage);

        Receive<UpdateProductPlacementMessage>(ProcessUpdateProductPlacementMessage);

        Receive<AddNewProductPlacementMessage>(ProcessAddNewProductPlacementMessage);

        Receive<GetAllProductPlacementHistoryFilteredMessage>(ProcessGetAllProductPlacementHistoryFilteredMessage);
    }


    private void ProcessGetAllProductPlacementHistoryFilteredMessage(GetAllProductPlacementHistoryFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IGetSingleProductRepository productRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
        Product product = productRepository.GetByNetId(message.ProductNetId);
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductPlacementHistoryRepository(connection)
                .GetAllByProductId(
                    product.Id,
                    message.From,
                    message.To,
                    message.Limit,
                    message.Offset
                )
        );
    }

    private void ProcessAddNewProductPlacementMessage(AddNewProductPlacementMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            if (message.ProductPlacement.IsNew()) {
                Sender.Tell("Object cannot be null");
                return;
            }

            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            ProductAvailability productAvailability =
                productAvailabilityRepository.GetByProductAndStorageIds(message.ProductPlacement.ProductId, message.ProductPlacement.StorageId);

            IEnumerable<ProductPlacement> existingProductPlacements =
                productPlacementRepository.GetAllByProductAndStorageIds(message.ProductPlacement.ProductId, message.ProductPlacement.StorageId);

            if (Math.Abs(productAvailability.Amount - existingProductPlacements.Sum(e => e.Qty)) > 1.0d) {
                Sender.Tell("Total qty should be equal available qty");

                return;
            }

            productPlacementRepository.Add(message.ProductPlacement);

            Sender.Tell(productPlacementRepository.GetAllByProductAndStorageIds(message.ProductPlacement.ProductId, message.ProductPlacement.StorageId));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdateProductPlacementMessage(UpdateProductPlacementMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IConsignmentInfoRepository consignmentInfoRepository = _consignmentRepositoriesFactory.NewConsignmentInfoRepository(connection);
            IProductPlacementHistoryRepository productPlacementHistoryRepository = _productRepositoriesFactory.NewProductPlacementHistoryRepository(connection);
            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

            ProductPlacement productPlace = message.ProductPlacements.FirstOrDefault();
            List<ProductPlacement> productPlacementListDb = new();

            IEnumerable<ProductPlacement> productsPlacements = productPlacementRepository.GetAllByProductAndStorageIds(productPlace.ProductId, productPlace.StorageId);

            if (!message.ProductPlacements.Any()) {
                Sender.Tell("Nothing to update");

                return;
            }

            ProductAvailability productAvailability =
                productAvailabilityRepository.GetByProductAndStorageIds(
                    message.ProductPlacements.First().ProductId,
                    message.ProductPlacements.First().StorageId);

            foreach (ProductPlacement item in message.ProductPlacements) {
                productPlacementHistoryRepository.Add(new ProductPlacementHistory {
                    Placement = item.StorageNumber + "-" + item.RowNumber + "-" + item.CellNumber,
                    StorageId = item.StorageId,
                    ProductId = item.ProductId,
                    Qty = item.Qty,
                    StorageLocationType = StorageLocationType.Edit,
                    AdditionType = AdditionType.Add,
                    UserId = user.Id
                });
                long productPlacementId = productPlacementRepository.AddWithId(new ProductPlacement {
                    StorageNumber = item.StorageNumber,
                    CellNumber = item.CellNumber,
                    RowNumber = item.RowNumber,
                    StorageId = item.StorageId,
                    ProductId = item.ProductId,
                    Qty = item.Qty
                });

                productPlacementListDb.Add(productPlacementRepository.GetById(productPlacementId));
            }

            foreach (ProductPlacement productPlacement in productsPlacements) productPlacementRepository.Remove(productPlacement);
            //foreach (ProductPlacement productPlacement in message.ProductPlacements) {

            //    ProductPlacement productPlacementDb = productsPlacements.FirstOrDefault(x => x.Id == productPlacement.Id);
            //    if (productPlacementDb == null) {
            //        var qty =  productPlacement.Qty;
            //        productPlacementHistoryRepository.Add(new ProductPlacementHistory {
            //            Placement = productPlacement.StorageNumber + "-" + productPlacement.RowNumber + "-" + productPlacement.CellNumber,
            //            StorageId = productPlacement.StorageId,
            //            ProductId = productPlacement.ProductId,
            //            Qty = qty,
            //            StorageLocationType = StorageLocationType.Edit,
            //            AdditionType = AdditionType.Add,
            //            UserId = user.Id
            //        });

            //        productPlacementRepository.RemoveWithoutQty(productPlacement);
            //        if (!productPlacement.Qty.Equals(0)) {
            //            productPlacement.Id = productPlacementRepository.AddWithId(productPlacement);
            //            ProductPlacement getProductPlacement = productPlacementRepository.GetById(productPlacement.Id);
            //            if (getProductPlacement.ConsignmentItemId != null) {
            //                ConsignmentItem consignmentItem = consignmentInfoRepository.GetInfoIcomesFiltered((long)getProductPlacement.ConsignmentItemId);
            //                getProductPlacement.ConsignmentItem = consignmentItem;
            //            }
            //            productPlacementListDb.Add(getProductPlacement);
            //        }

            //        continue;
            //    }
            //    if (productPlacementDb.Qty > productPlacement.Qty) {
            //        var qty = productPlacementDb.Qty - productPlacement.Qty;

            //        productPlacementHistoryRepository.Add(new ProductPlacementHistory {
            //            Placement = productPlacement.StorageNumber + "-" + productPlacement.RowNumber + "-" + productPlacement.CellNumber,
            //            StorageId = productPlacement.StorageId,
            //            ProductId = productPlacement.ProductId,
            //            Qty = qty,
            //            StorageLocationType = StorageLocationType.Edit,
            //            AdditionType = AdditionType.Remove,
            //            UserId = user.Id
            //        });
            //    }

            //    if (productPlacementDb.Qty < productPlacement.Qty) {
            //        var qty = productPlacement.Qty - productPlacementDb.Qty;
            //        productPlacementHistoryRepository.Add(new ProductPlacementHistory {
            //            Placement = productPlacement.StorageNumber + "-" + productPlacement.RowNumber + "-" + productPlacement.CellNumber,
            //            StorageId = productPlacement.StorageId,
            //            ProductId = productPlacement.ProductId,
            //            Qty = qty,
            //            StorageLocationType = StorageLocationType.Edit,
            //            AdditionType = AdditionType.Add,
            //            UserId = user.Id
            //        });
            //    }
            //    productPlacementRepository.RemoveWithoutQty(productPlacement);
            //    if (!productPlacement.Qty.Equals(0)) {
            //        productPlacement.Id = productPlacementRepository.AddWithId(productPlacement);
            //        ProductPlacement getProductPlacement = productPlacementRepository.GetById(productPlacement.Id);
            //        //if (getProductPlacement.ConsignmentItemId != null) {
            //        //    ConsignmentItem consignmentItem = consignmentInfoRepository.GetInfoIcomesFiltered((long)getProductPlacement.ConsignmentItemId);
            //        //    getProductPlacement.ConsignmentItem = consignmentItem;
            //        //}
            //        productPlacementListDb.Add(getProductPlacement);
            //    }
            //}

            Sender.Tell(productPlacementListDb);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetAllProductPlacementStorageFilteredMessage(GetAllProductPlacementStorageFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductPlacementStorageRepository(connection)
                .GetAllFiltered(
                    message.StorageIds,
                    message.Value,
                    message.To,
                    message.Limit,
                    message.Offset
                )
        );
    }

    private void ProcessGetCreateDocumentShipmentFilteredMessage(GetCreateDocumentProductPlacementStorageMessage message) {
        string xlsxFile = string.Empty;
        string pdfFile = string.Empty;
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IEnumerable<ProductPlacementStorage> productsPlacementStorage = _productRepositoriesFactory
            .NewProductPlacementStorageRepository(connection)
            .GetAll();

        (xlsxFile, pdfFile) =
            _xlsFactoryManager
                .NewProductPlacementStorageManagerManager()
                .ExportProductPlacementStorageToXlsx(
                    message.SaleInvoicesFolderPath,
                    productsPlacementStorage,
                    _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk"));

        Sender.Tell(new Tuple<string, string>(xlsxFile, pdfFile));
    }

    private void ProcessGetCreateDocumentProductPlacementStorageFilteredMessage(GetCreateDocumentProductPlacementStoragesMessage message) {
        string xlsxFile = string.Empty;
        string pdfFile = string.Empty;
        using IDbConnection connection = _connectionFactory.NewSqlConnection();


        (xlsxFile, pdfFile) =
            _xlsFactoryManager
                .NewProductPlacementStorageManagerManager()
                .ExportProductPlacementStorageToXlsx(
                    message.SaleInvoicesFolderPath,
                    message.ProductPlacementStorages,
                    _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk"));

        Sender.Tell(new Tuple<string, string>(xlsxFile, pdfFile));
    }

    private void ProcessUploadPlacementMovementsileMessage(UploadPlacementMovementsileMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        List<Product> products = new();
        List<ProductPlacement> productPlacements = new();
        List<ProductPlacementStorage> productPlacementStorage = new();
        User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

        IGetSingleProductRepository productRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
        IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
        IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);
        IProductPlacementStorageRepository productPlacementStorageRepository = _productRepositoriesFactory.NewProductPlacementStorageRepository(connection);
        IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);
        IConsignmentItemRepository consignmentItemRepository = _consignmentRepositoriesFactory.NewConsignmentItemRepository(connection);
        IProductPlacementHistoryRepository productPlacementHistoryRepository = _productRepositoriesFactory.NewProductPlacementHistoryRepository(connection);

        try {
            List<ProductPlacementMovementVendorCode> productPlacementMovementVendorCodeList = null;
            if (message.FilePath != null && message.PlacementMovementsStorageParseConfiguration != null) {
                productPlacementMovementVendorCodeList = _xlsFactoryManager
                    .NewParseConfigurationXlsManager()
                    .GetProductPlacementMovementFromXlsx(message.FilePath, message.PlacementMovementsStorageParseConfiguration);
            } else {
                productPlacementMovementVendorCodeList = new List<ProductPlacementMovementVendorCode>();

                foreach (ProductPlacementStorage productPlacement in message.ProductPlacementStorages) {
                    ProductPlacementMovementVendorCode productPlacementMovementVendorCode = new();
                    string[] parts = productPlacement.Placement.Split('-');
                    productPlacementMovementVendorCode = new ProductPlacementMovementVendorCode {
                        Qty = (int)productPlacement.Qty,
                        VendorCode = productPlacement.VendorCode
                    };
                    if (parts.Length > 0)
                        productPlacementMovementVendorCode.StorageNumber = parts[0];

                    if (parts.Length > 1)
                        productPlacementMovementVendorCode.RowNumber = parts[1];

                    if (parts.Length > 2)
                        productPlacementMovementVendorCode.CellNumber = parts[2];
                    productPlacementMovementVendorCodeList.Add(productPlacementMovementVendorCode);
                }
            }

            List<ProductPlacementMovementVendorCode> productPlacementMovementVendorCodeMoreQty = new();
            string vendorCode = string.Empty;

            foreach (ProductPlacementMovementVendorCode ParseProductPlacement in productPlacementMovementVendorCodeList) {
                ParseProductPlacement.VendorCode = ParseProductPlacement.VendorCode?.Trim();
                ParseProductPlacement.StorageNumber = ParseProductPlacement.StorageNumber?.Replace(" ", "");
                ParseProductPlacement.CellNumber = ParseProductPlacement.CellNumber?.Replace(" ", "");
                ParseProductPlacement.RowNumber = ParseProductPlacement.RowNumber?.Replace(" ", "");

                Product product = productRepository.GetProductByVendorCode(ParseProductPlacement.VendorCode);
                if (product == null) {
                    productPlacementStorage.Add(
                        new ProductPlacementStorage {
                            StorageId = message.StorageId,
                            Placement = ParseProductPlacement.StorageNumber + "-" + ParseProductPlacement.RowNumber + "-" + ParseProductPlacement.CellNumber,
                            VendorCode = ParseProductPlacement.VendorCode,
                            Qty = ParseProductPlacement.Qty,
                            ErrorMessage = "³������� ������� �� ����� " + storageRepository.GetById(message.StorageId).Name
                        });
                    continue;
                }

                if (vendorCode == product.VendorCode) continue;

                ProductPlacement productPlacement = productPlacementRepository.GetQty(
                    ParseProductPlacement.Qty,
                    product.Id,
                    message.StorageId
                );
                //��������
                List<ProductPlacement> productPlacementList = productPlacementRepository
                    .GetAllByProductAndStorageIds(product.Id, message.StorageId)
                    .OrderByDescending(x => x.Qty).ToList();
                //������
                List<ProductPlacementMovementVendorCode> productPlacementMovementVendorCodeVendorCode =
                    productPlacementMovementVendorCodeList
                        .Where(x => x.VendorCode == product.VendorCode)
                        .OrderByDescending(x => x.Qty).ToList();

                ProductAvailability productAvailability = productAvailabilityRepository.GetByProductAndStorageIds(product.Id, message.StorageId);

                if (productAvailability == null) {
                    productPlacementStorage.Add(
                        new ProductPlacementStorage {
                            StorageId = message.StorageId,
                            Product = product,
                            Placement = ParseProductPlacement.StorageNumber + "-" + ParseProductPlacement.RowNumber + "-" + ParseProductPlacement.CellNumber,
                            VendorCode = ParseProductPlacement.VendorCode,
                            Qty = ParseProductPlacement.Qty,
                            ErrorMessage = "³������� �� �������� ������ " + storageRepository.GetById(message.StorageId).Name
                        });
                    continue;
                }

                if (productAvailability.Amount > productPlacementMovementVendorCodeVendorCode.Sum(x => x.Qty)) {
                    productPlacementStorage.Add(
                        new ProductPlacementStorage {
                            StorageId = message.StorageId,
                            Product = product,
                            Placement = ParseProductPlacement.StorageNumber + "-" + ParseProductPlacement.RowNumber + "-" + ParseProductPlacement.CellNumber,
                            VendorCode = ParseProductPlacement.VendorCode,
                            Qty = ParseProductPlacement.Qty,
                            ErrorMessage = "ʳ������ ����� ��� � ��� " + storageRepository.GetById(message.StorageId).Name
                        });
                    continue;
                }

                if (productAvailability.Amount < productPlacementMovementVendorCodeVendorCode.Sum(x => x.Qty)) {
                    productPlacementStorage.Add(
                        new ProductPlacementStorage {
                            StorageId = message.StorageId,
                            Product = product,
                            Placement = ParseProductPlacement.StorageNumber + "-" + ParseProductPlacement.RowNumber + "-" + ParseProductPlacement.CellNumber,
                            VendorCode = ParseProductPlacement.VendorCode,
                            Qty = ParseProductPlacement.Qty,
                            ErrorMessage = "ʳ������ ����� ��� � ��� " + storageRepository.GetById(message.StorageId).Name
                        });
                    continue;
                }

                if (productAvailability.StorageId != message.StorageId) {
                    productPlacementStorage.Add(
                        new ProductPlacementStorage {
                            StorageId = message.StorageId,
                            Product = product,
                            Placement = ParseProductPlacement.StorageNumber + "-" + ParseProductPlacement.RowNumber + "-" + ParseProductPlacement.CellNumber,
                            VendorCode = ParseProductPlacement.VendorCode,
                            Qty = ParseProductPlacement.Qty,
                            ErrorMessage = "³������� �� ����� " + storageRepository.GetById(message.StorageId).Name
                        });
                    continue;
                }

                vendorCode = product.VendorCode;

                productPlacementRepository.RemoveFromProductIdToStorageId(product.Id, message.StorageId);

                for (int i = 0; i < productPlacementMovementVendorCodeVendorCode.Count; i++) {
                    productPlacementHistoryRepository.Add(new ProductPlacementHistory {
                        Placement = productPlacementMovementVendorCodeVendorCode[i].StorageNumber + "-" + productPlacementMovementVendorCodeVendorCode[i].RowNumber + "-" +
                                    productPlacementMovementVendorCodeVendorCode[i].CellNumber,
                        StorageId = message.StorageId,
                        ProductId = product.Id,
                        Qty = productPlacementMovementVendorCodeVendorCode[i].Qty,
                        StorageLocationType = StorageLocationType.Placement,
                        AdditionType = AdditionType.Add,
                        UserId = user.Id
                    });

                    long productPlacementId = productPlacementRepository.AddWithId(new ProductPlacement {
                        StorageId = message.StorageId,
                        ProductId = product.Id,
                        StorageNumber = productPlacementMovementVendorCodeVendorCode[i].StorageNumber,
                        RowNumber = productPlacementMovementVendorCodeVendorCode[i].RowNumber,
                        CellNumber = productPlacementMovementVendorCodeVendorCode[i].CellNumber,
                        Qty = productPlacementMovementVendorCodeVendorCode[i].Qty
                    });

                    productPlacementStorageRepository.Add(
                        new ProductPlacementStorage {
                            StorageId = message.StorageId,
                            ProductId = product.Id,
                            Placement = productPlacementMovementVendorCodeVendorCode[i].StorageNumber + "-" + productPlacementMovementVendorCodeVendorCode[i].RowNumber + "-" +
                                        productPlacementMovementVendorCodeVendorCode[i].CellNumber,
                            VendorCode = productPlacementMovementVendorCodeVendorCode[i].VendorCode,
                            Qty = productPlacementMovementVendorCodeVendorCode[i].Qty,
                            ProductPlacementId = productPlacementId
                        });
                }
            }

            Sender.Tell(productPlacementStorage);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}