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
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies.ActProvidingServices;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.Supplies;
using GBA.Domain.Messages.Supplies.Ukraine.Orders;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Domain.Repositories.Supplies.ActProvidingServices.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.Documents.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Supplies.Ukraine;

public sealed class SupplyOrderUkrainesActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;

    private readonly string _defaultComment = "��� ������� � 1�.";
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public SupplyOrderUkrainesActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IStorageRepositoryFactory storageRepositoryFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _storageRepositoryFactory = storageRepositoryFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;

        Receive<AddOrUpdateSupplyOrderUkraineMessage>(ProcessAddOrUpdateSupplyOrderUkraineMessage);

        Receive<AddOrUpdateSupplyOrderUkraineAfterPreviewValidationMessage>(ProcessAddOrUpdateSupplyOrderUkraineAfterPreviewValidationMessage);

        Receive<PreviewValidateAddOrUpdateSupplyOrderUkraineMessage>(ProcessPreviewValidateAddOrUpdateSupplyOrderUkraineMessage);

        Receive<AddNewSupplyOrderUkraineFromTaxFreePackListMessage>(ProcessAddNewSupplyOrderUkraineFromTaxFreePackListMessage);

        Receive<AddNewSupplyOrderUkraineFromSadMessage>(ProcessAddNewSupplyOrderUkraineFromSadMessage);

        Receive<UpdateSupplyOrderUkraineMessage>(ProcessUpdateSupplyOrderUkraineMessage);

        Receive<GetAllSupplyOrdersUkraineFilteredMessage>(ProcessGetAllSupplyOrdersUkraineFilteredMessage);

        Receive<GetSupplyOrderUkraineByNetIdMessage>(ProcessGetSupplyOrderUkraineByNetIdMessage);

        Receive<SetSupplyOrderUkraineIsPlacedByNetIdMessage>(ProcessSetSupplyOrderUkraineIsPlacedByNetIdMessage);

        Receive<AddOrUpdateSupplyOrderUkraineFromSupplierMessage>(ProcessAddOrUpdateSupplyOrderUkraineFromSupplierMessage);

        Receive<DeleteSupplyOrderUkraineFromSupplierMessage>(ProcessDeleteSupplyOrderUkraineFromSupplierMessage);

        Receive<GetAllSupplyOrderUkrainePaymentDeliveryProtocolKeysMessage>(ProcessGetAllSupplyOrderUkrainePaymentDeliveryProtocolKeysMessage);

        Receive<AddVatPercentToSupplyOrderUkraineMessage>(ProcessAddVatPercentToSupplyOrderUkraine);

        Receive<UpdateSupplyOrderUkraineItemMessage>(ProcessUpdateSupplyOrderUkraineItem);

        Receive<ManageSupplyOrderUkraineDocumentMessage>(ProcessManageSupplyOrderUkraineDocument);

        Receive<UpdateSupplyOrderUkraineItemPriceMessage>(ProcessUpdateSupplyOrderUkraineItemPrice);

        Receive<AddDeliveryExpensesMessage>(ProcessAddDeliveryExpensesMessage);

        Receive<UpdateDeliveryExpensesMessage>(ProcessUpdateDeliveryExpensesMessage);
    }


    private void ProcessUpdateDeliveryExpensesMessage(UpdateDeliveryExpensesMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyOrganizationRepository supplyOrganizationRepository = _supplyRepositoriesFactory.NewSupplyOrganizationRepository(connection);
            ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
                _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);
            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            IDeliveryExpenseRepository deliveryExpenseRepository = _supplyUkraineRepositoriesFactory.NewDeliveryExpenseRepository(connection);
            IActProvidingServiceRepository actProvidingServiceRepository =
                _supplyRepositoriesFactory.NewActProvidingServiceRepository(connection);

            if (message.DeliveryExpense.SupplyOrganization != null && !message.DeliveryExpense.SupplyOrganization.IsNew()) {
                message.DeliveryExpense.SupplyOrganizationId = message.DeliveryExpense.SupplyOrganization.Id;
            } else if (message.DeliveryExpense.SupplyOrganization == null && message.DeliveryExpense.SupplyOrganizationId > 0) {
                message.DeliveryExpense.SupplyOrganization = supplyOrganizationRepository.GetById(message.DeliveryExpense.SupplyOrganizationId);
            } else {
                Sender.Tell(new Exception("SupplyOrganizationMissing"));
                return;
            }

            if (message.DeliveryExpense.SupplyOrganizationAgreement != null && !message.DeliveryExpense.SupplyOrganizationAgreement.IsNew()) {
                message.DeliveryExpense.SupplyOrganizationAgreementId = message.DeliveryExpense.SupplyOrganizationAgreement.Id;
            } else if (message.DeliveryExpense.SupplyOrganizationAgreement == null && message.DeliveryExpense.SupplyOrganizationAgreementId > 0) {
                message.DeliveryExpense.SupplyOrganizationAgreement = supplyOrganizationAgreementRepository.GetById(message.DeliveryExpense.SupplyOrganizationAgreementId);
            } else {
                Sender.Tell(new Exception("SupplyOrganizationAgreementMissing"));
                return;
            }

            DeliveryExpense deliveryExpenseFromDb = deliveryExpenseRepository.GetById(message.DeliveryExpense.Id);

            if (deliveryExpenseFromDb.AccountingGrossAmount == decimal.Zero && message.DeliveryExpense.AccountingGrossAmount != decimal.Zero)
                message.DeliveryExpense.AccountingActProvidingServiceId =
                    CreateActProvidingService(message.DeliveryExpense, actProvidingServiceRepository, true);

            if (deliveryExpenseFromDb.GrossAmount == decimal.Zero && message.DeliveryExpense.GrossAmount != decimal.Zero)
                message.DeliveryExpense.AccountingActProvidingServiceId =
                    CreateActProvidingService(message.DeliveryExpense, actProvidingServiceRepository, false);

            User user = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

            message.DeliveryExpense.User = user;
            message.DeliveryExpense.UserId = user.Id;

            if (message.DeliveryExpense.ConsumableProduct != null && !message.DeliveryExpense.ConsumableProduct.IsNew())
                message.DeliveryExpense.ConsumableProductId = message.DeliveryExpense.ConsumableProduct.Id;

            if (message.DeliveryExpense.SupplyOrderUkraine != null && !message.DeliveryExpense.SupplyOrderUkraine.IsNew())
                message.DeliveryExpense.SupplyOrderUkraineId = message.DeliveryExpense.SupplyOrderUkraine.Id;

            deliveryExpenseRepository.Update(message.DeliveryExpense);

            Sender.Tell(new { Success = true });
        } catch (Exception exc) {
            Sender.Tell(exc.Message);
        }
    }

    private void ProcessAddDeliveryExpensesMessage(AddDeliveryExpensesMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IActProvidingServiceDocumentRepository actProvidingServiceDocumentRepository =
                _supplyRepositoriesFactory.NewActProvidingServiceDocumentRepository(connection);
            ISupplyOrganizationRepository supplyOrganizationRepository = _supplyRepositoriesFactory.NewSupplyOrganizationRepository(connection);
            ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
                _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);
            IActProvidingServiceRepository actProvidingServiceRepository =
                _supplyRepositoriesFactory.NewActProvidingServiceRepository(connection);
            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            IDeliveryExpenseRepository deliveryExpenseRepository = _supplyUkraineRepositoriesFactory.NewDeliveryExpenseRepository(connection);

            if (message.DeliveryExpense.SupplyOrganization != null && !message.DeliveryExpense.SupplyOrganization.IsNew()) {
                message.DeliveryExpense.SupplyOrganizationId = message.DeliveryExpense.SupplyOrganization.Id;
            } else if (message.DeliveryExpense.SupplyOrganization == null && message.DeliveryExpense.SupplyOrganizationId > 0) {
                message.DeliveryExpense.SupplyOrganization = supplyOrganizationRepository.GetById(message.DeliveryExpense.SupplyOrganizationId);
            } else {
                Sender.Tell(new Exception("SupplyOrganizationMissing"));
                return;
            }

            if (message.DeliveryExpense.SupplyOrganizationAgreement != null && !message.DeliveryExpense.SupplyOrganizationAgreement.IsNew()) {
                message.DeliveryExpense.SupplyOrganizationAgreementId = message.DeliveryExpense.SupplyOrganizationAgreement.Id;
            } else if (message.DeliveryExpense.SupplyOrganizationAgreement == null && message.DeliveryExpense.SupplyOrganizationAgreementId > 0) {
                message.DeliveryExpense.SupplyOrganizationAgreement = supplyOrganizationAgreementRepository.GetById(message.DeliveryExpense.SupplyOrganizationAgreementId);
            } else {
                Sender.Tell(new Exception("SupplyOrganizationAgreementMissing"));
                return;
            }

            User user = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

            message.DeliveryExpense.User = user;
            message.DeliveryExpense.UserId = user.Id;

            if (message.DeliveryExpense.ConsumableProduct != null && !message.DeliveryExpense.ConsumableProduct.IsNew())
                message.DeliveryExpense.ConsumableProductId = message.DeliveryExpense.ConsumableProduct.Id;

            if (message.DeliveryExpense.SupplyOrderUkraine != null && !message.DeliveryExpense.SupplyOrderUkraine.IsNew())
                message.DeliveryExpense.SupplyOrderUkraineId = message.DeliveryExpense.SupplyOrderUkraine.Id;

            if (message.DeliveryExpense.ActProvidingService == null) {
                if (message.DeliveryExpense.AccountingGrossAmount != decimal.Zero)
                    message.DeliveryExpense.AccountingActProvidingServiceId =
                        CreateActProvidingService(message.DeliveryExpense, actProvidingServiceRepository, true);

                if (message.DeliveryExpense.GrossAmount != decimal.Zero)
                    message.DeliveryExpense.ActProvidingServiceId =
                        CreateActProvidingService(message.DeliveryExpense, actProvidingServiceRepository, false);

                if (!message.DeliveryExpense.SupplyOrganizationAgreement.IsNew()) {
                    message.DeliveryExpense.SupplyOrganizationAgreement =
                        supplyOrganizationAgreementRepository.GetById(message.DeliveryExpense.SupplyOrganizationAgreement.Id);

                    message.DeliveryExpense.SupplyOrganizationAgreement.CurrentAmount =
                        Math.Round(
                            message.DeliveryExpense.SupplyOrganizationAgreement.CurrentAmount - message.DeliveryExpense.GrossAmount, 2);

                    supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.DeliveryExpense.SupplyOrganizationAgreement);
                }
            }

            if (message.DeliveryExpense.ActProvidingServiceDocument != null) {
                if (message.DeliveryExpense.ActProvidingServiceDocument.IsNew()) {
                    ActProvidingServiceDocument lastRecord =
                        actProvidingServiceDocumentRepository.GetLastRecord();

                    if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                        !string.IsNullOrEmpty(lastRecord.Number))
                        message.DeliveryExpense.ActProvidingServiceDocument.Number = string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                    else
                        message.DeliveryExpense.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                    message.DeliveryExpense.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                        .New(message.DeliveryExpense.ActProvidingServiceDocument);
                } else if (message.DeliveryExpense.ActProvidingServiceDocument.Deleted.Equals(true)) {
                    message.DeliveryExpense.ActProvidingServiceDocumentId = null;
                    actProvidingServiceDocumentRepository.RemoveById(message.DeliveryExpense.ActProvidingServiceDocument.Id);
                }
            }

            long addedDeliveryExpenseId = deliveryExpenseRepository.Add(message.DeliveryExpense);

            Sender.Tell(addedDeliveryExpenseId);
        } catch (Exception exc) {
            Sender.Tell(exc.Message);
        }
    }

    private long CreateActProvidingService(DeliveryExpense deliveryExpense, IActProvidingServiceRepository actProvidingServiceRepository, bool isAccounting) {
        deliveryExpense.ActProvidingService = new ActProvidingService {
            UserId = deliveryExpense.User.Id,
            IsAccounting = isAccounting,
            Price = isAccounting ? deliveryExpense.AccountingGrossAmount : deliveryExpense.GrossAmount,
            FromDate = DateTime.UtcNow
        };

        ActProvidingService lastAct = actProvidingServiceRepository.GetLastRecord(_defaultComment);

        if (lastAct != null && lastAct.Created.Year.Equals(DateTime.Now.Year) && !string.IsNullOrEmpty(lastAct.Number))
            deliveryExpense.ActProvidingService.Number = string.Format("P{0:D10}", int.Parse(lastAct.Number.Substring(1)) + 1);
        else
            deliveryExpense.ActProvidingService.Number = string.Format("P{0:D10}", 1);

        return actProvidingServiceRepository.New(deliveryExpense.ActProvidingService);
    }

    private void ProcessAddOrUpdateSupplyOrderUkraineMessage(AddOrUpdateSupplyOrderUkraineMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            ISupplyOrderUkraineRepository supplyOrderUkraineRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineRepository(connection);
            ISupplyOrderUkraineItemRepository supplyOrderUkraineItemRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineItemRepository(connection);

            List<ParsedProductForUkraine> parsedProducts =
                _xlsFactoryManager
                    .NewParseConfigurationXlsManager()
                    .GetProductsFromUkraineSupplyDocumentsByConfiguration(
                        message.PathToFile,
                        message.ParseConfiguration
                    );

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            if (message.SupplyOrderUkraine.IsNew()) {
                foreach (ParsedProductForUkraine parsedProduct in parsedProducts) {
                    Product product = getSingleProductRepository.GetProductByVendorCode(parsedProduct.VendorCode);

                    if (product == null) throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.NoProductByVendorCode, 0, 0, parsedProduct.VendorCode);
                }

                foreach (ParsedProductForUkraine parsedProduct in parsedProducts) {
                    Product product = getSingleProductRepository.GetByVendorCodeAndRuleLocaleWithProductGroupAndWriteOffRules(parsedProduct.VendorCode, "pl");

                    ProductAvailability productAvailability =
                        productAvailabilityRepository
                            .GetByProductIdForCulture(
                                product.Id,
                                "pl"
                            );

                    if (productAvailability == null) continue;

                    if (productAvailability.Amount <= 0) continue;

                    if (productAvailability.Amount >= parsedProduct.Qty) {
                        message
                            .SupplyOrderUkraine
                            .SupplyOrderUkraineItems
                            .Add(
                                new SupplyOrderUkraineItem {
                                    ProductId = product.Id,
                                    Qty = parsedProduct.Qty,
                                    NetWeight = parsedProduct.TotalNetWeight,
                                    UnitPrice = parsedProduct.TotalNetPrice
                                }
                            );

                        productAvailability.Amount = Math.Round(productAvailability.Amount - parsedProduct.Qty, 2, MidpointRounding.AwayFromZero);
                    } else {
                        message
                            .SupplyOrderUkraine
                            .SupplyOrderUkraineItems
                            .Add(
                                new SupplyOrderUkraineItem {
                                    ProductId = product.Id,
                                    Qty = productAvailability.Amount,
                                    NetWeight = parsedProduct.TotalNetWeight,
                                    UnitPrice = parsedProduct.TotalNetPrice
                                }
                            );

                        productAvailability.Amount = 0;
                    }

                    productAvailabilityRepository.Update(productAvailability);
                }

                message.SupplyOrderUkraine.ResponsibleId = user.Id;
                message.SupplyOrderUkraine.OrganizationId = message.SupplyOrderUkraine.Organization.Id;
                message.SupplyOrderUkraine.IsDirectFromSupplier = false;
                message.SupplyOrderUkraine.FromDate =
                    message.SupplyOrderUkraine.FromDate.Year.Equals(1)
                        ? DateTime.UtcNow.Date
                        : message.SupplyOrderUkraine.FromDate;

                SupplyOrderUkraine lastRecord = supplyOrderUkraineRepository.GetLastRecord();

                if (lastRecord != null && DateTime.Now.Year.Equals(lastRecord.Created.Year))
                    message.SupplyOrderUkraine.Number = $"{string.Format("{0:D10}", Convert.ToInt32(lastRecord.Number) + 1)}";
                else
                    message.SupplyOrderUkraine.Number = $"{string.Format("{0:D10}", 1)}";

                message.SupplyOrderUkraine.Id = supplyOrderUkraineRepository.Add(message.SupplyOrderUkraine);

                supplyOrderUkraineItemRepository
                    .Add(
                        message
                            .SupplyOrderUkraine
                            .SupplyOrderUkraineItems
                            .Select(item => {
                                item.SupplyOrderUkraineId = message.SupplyOrderUkraine.Id;

                                return item;
                            })
                    );
            } else {
                message.SupplyOrderUkraine = supplyOrderUkraineRepository.GetById(message.SupplyOrderUkraine.Id);

                if (message.SupplyOrderUkraine == null) throw new Exception("Incorrect SupplyOrderUkraine entity. Order with such ID does not exists.");

                foreach (ParsedProductForUkraine parsedProduct in parsedProducts) {
                    Product product = getSingleProductRepository.GetProductByVendorCode(parsedProduct.VendorCode);

                    if (product == null) throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.NoProductByVendorCode, 0, 0, parsedProduct.VendorCode);
                }

                foreach (ParsedProductForUkraine parsedProduct in parsedProducts) {
                    Product product = getSingleProductRepository.GetProductByVendorCode(parsedProduct.VendorCode);

                    ProductAvailability productAvailability =
                        productAvailabilityRepository
                            .GetByProductIdForCulture(
                                product.Id,
                                "pl"
                            );

                    if (productAvailability != null) {
                        if (message.SupplyOrderUkraine.SupplyOrderUkraineItems.Any(i => i.ProductId.Equals(product.Id))) {
                            SupplyOrderUkraineItem itemFromList = message.SupplyOrderUkraine.SupplyOrderUkraineItems.First(i => i.ProductId.Equals(product.Id));

                            if (!itemFromList.Qty.Equals(parsedProduct.Qty)) {
                                double differenceQty = Math.Round(itemFromList.Qty - parsedProduct.Qty, 2, MidpointRounding.AwayFromZero);

                                if (differenceQty > 0) {
                                    productAvailability.Amount = Math.Round(productAvailability.Amount + differenceQty, 2, MidpointRounding.AwayFromZero);

                                    itemFromList.Qty = parsedProduct.Qty;
                                } else {
                                    if (productAvailability.Amount > 0) {
                                        differenceQty = 0 - differenceQty;

                                        if (productAvailability.Amount >= differenceQty) {
                                            itemFromList.Qty = Math.Round(itemFromList.Qty + differenceQty, 2, MidpointRounding.AwayFromZero);

                                            productAvailability.Amount = Math.Round(productAvailability.Amount - differenceQty, 2, MidpointRounding.AwayFromZero);
                                        } else {
                                            itemFromList.Qty = Math.Round(itemFromList.Qty + productAvailability.Amount, 2, MidpointRounding.AwayFromZero);

                                            productAvailability.Amount = 0;
                                        }
                                    }
                                }
                            }

                            itemFromList.IsUpdated = true;
                            itemFromList.NetWeight = parsedProduct.TotalNetWeight;
                            itemFromList.UnitPrice = parsedProduct.TotalNetPrice;
                        } else {
                            if (productAvailability.Amount > 0) {
                                if (productAvailability.Amount >= parsedProduct.Qty) {
                                    message
                                        .SupplyOrderUkraine
                                        .SupplyOrderUkraineItems
                                        .Add(
                                            new SupplyOrderUkraineItem {
                                                ProductId = product.Id,
                                                Qty = parsedProduct.Qty,
                                                NetWeight = parsedProduct.TotalNetWeight,
                                                UnitPrice = parsedProduct.TotalNetPrice
                                            }
                                        );

                                    productAvailability.Amount = Math.Round(productAvailability.Amount - parsedProduct.Qty, 2, MidpointRounding.AwayFromZero);
                                } else {
                                    message
                                        .SupplyOrderUkraine
                                        .SupplyOrderUkraineItems
                                        .Add(
                                            new SupplyOrderUkraineItem {
                                                ProductId = product.Id,
                                                Qty = productAvailability.Amount,
                                                NetWeight = parsedProduct.TotalNetWeight,
                                                UnitPrice = parsedProduct.TotalNetPrice,
                                                SupplyOrderUkraineId = message.SupplyOrderUkraine.Id
                                            }
                                        );

                                    productAvailability.Amount = 0;
                                }
                            }
                        }

                        productAvailabilityRepository.Update(productAvailability);
                    }
                }

                foreach (SupplyOrderUkraineItem item in message.SupplyOrderUkraine.SupplyOrderUkraineItems.Where(i => !i.IsNew() && !i.IsUpdated)) {
                    ProductAvailability productAvailability =
                        productAvailabilityRepository
                            .GetByProductIdForCulture(
                                item.ProductId,
                                "pl"
                            );

                    if (productAvailability != null) {
                        productAvailability.Amount = Math.Round(productAvailability.Amount + item.Qty, 2, MidpointRounding.AwayFromZero);

                        productAvailabilityRepository.Update(productAvailability);
                    }
                }

                supplyOrderUkraineItemRepository
                    .RemoveAllByOrderUkraineIdExceptProvided(
                        message.SupplyOrderUkraine.Id,
                        message
                            .SupplyOrderUkraine
                            .SupplyOrderUkraineItems
                            .Where(i => !i.IsNew() && i.IsUpdated)
                            .Select(i => i.Id)
                    );

                supplyOrderUkraineItemRepository
                    .Add(
                        message
                            .SupplyOrderUkraine
                            .SupplyOrderUkraineItems
                            .Where(i => !i.IsNew() && i.IsUpdated)
                    );


                supplyOrderUkraineItemRepository
                    .Add(
                        message
                            .SupplyOrderUkraine
                            .SupplyOrderUkraineItems
                            .Where(i => i.IsNew())
                    );
            }

            Sender.Tell(
                supplyOrderUkraineRepository
                    .GetById(
                        message.SupplyOrderUkraine.Id
                    )
            );
        } catch (SupplyDocumentParseException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessAddOrUpdateSupplyOrderUkraineAfterPreviewValidationMessage(AddOrUpdateSupplyOrderUkraineAfterPreviewValidationMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);
            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            IProductWriteOffRuleRepository productWriteOffRuleRepository = _productRepositoriesFactory.NewProductWriteOffRuleRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            ISupplyOrderUkraineRepository supplyOrderUkraineRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineRepository(connection);
            ISupplyOrderUkraineItemRepository supplyOrderUkraineItemRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineItemRepository(connection);
            IPackingListPackageOrderItemRepository packingListPackageOrderItemRepository =
                _supplyRepositoriesFactory.NewPackingListPackageOrderItemRepository(connection);

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            if (!message.UkraineOrderValidation.UkraineOrderValidationItems.Any(i => !i.HasError || i.LessAvailable))
                throw new Exception("File has no items that are available");

            if (message.UkraineOrderValidation.SupplyOrderUkraine.IsNew()) {
                message.UkraineOrderValidation.SupplyOrderUkraine.ResponsibleId = user.Id;
                message.UkraineOrderValidation.SupplyOrderUkraine.OrganizationId = message.UkraineOrderValidation.SupplyOrderUkraine.Organization.Id;
                message.UkraineOrderValidation.SupplyOrderUkraine.ClientAgreementId = message.UkraineOrderValidation.SupplyOrderUkraine.ClientAgreement.Id;
                message.UkraineOrderValidation.SupplyOrderUkraine.SupplierId = message.UkraineOrderValidation.SupplyOrderUkraine.Supplier.Id;
                message.UkraineOrderValidation.SupplyOrderUkraine.IsDirectFromSupplier = false;
                message.UkraineOrderValidation.SupplyOrderUkraine.FromDate =
                    message.UkraineOrderValidation.SupplyOrderUkraine.FromDate.Year.Equals(1)
                        ? DateTime.UtcNow.Date
                        : message.UkraineOrderValidation.SupplyOrderUkraine.FromDate.Date;

                SupplyOrderUkraine lastRecord = supplyOrderUkraineRepository.GetLastRecord();

                if (lastRecord != null && DateTime.Now.Year.Equals(lastRecord.Created.Year))
                    message.UkraineOrderValidation.SupplyOrderUkraine.Number = $"{string.Format("{0:D12}", Convert.ToInt32(lastRecord.Number) + 1)}";
                else
                    message.UkraineOrderValidation.SupplyOrderUkraine.Number = $"{string.Format("{0:D12}", 1)}";

                message.UkraineOrderValidation.SupplyOrderUkraine.Id = supplyOrderUkraineRepository.Add(message.UkraineOrderValidation.SupplyOrderUkraine);

                foreach (UkraineOrderValidationItem validationItem in message
                             .UkraineOrderValidation
                             .UkraineOrderValidationItems
                             .Where(i => !i.ZeroAvailable && !i.VendorCodeNotFinded)) {
                    Product product = getSingleProductRepository.GetByVendorCodeAndRuleLocaleWithProductGroupAndWriteOffRules(validationItem.ParsedProduct.VendorCode, "pl");

                    ProductAvailability productAvailability =
                        productAvailabilityRepository
                            .GetByProductIdForCulture(
                                product.Id,
                                "pl"
                            );

                    if (productAvailability == null || productAvailability.Amount <= 0) continue;

                    ProductWriteOffRule writeOffRule;

                    if (product.ProductWriteOffRules.Any()) {
                        writeOffRule = product.ProductWriteOffRules.First();
                    } else if (product.ProductProductGroups.Any()) {
                        writeOffRule = product.ProductProductGroups.First().ProductGroup.ProductWriteOffRules.First();
                    } else {
                        writeOffRule = productWriteOffRuleRepository.GetByRuleLocale("pl");

                        if (writeOffRule == null) {
                            productWriteOffRuleRepository.Add(new ProductWriteOffRule {
                                RuleLocale = "pl",
                                CreatedById = userRepository.GetManagerOrGBAIdByClientNetId(Guid.Empty),
                                RuleType = ProductWriteOffRuleType.ByFromDate
                            });

                            writeOffRule = productWriteOffRuleRepository.GetByRuleLocale("pl");
                        }
                    }

                    long storageId = storageRepository.GetByLocale("pl").Id;

                    IEnumerable<PackingListPackageOrderItem> packageOrderItems =
                        packingListPackageOrderItemRepository
                            .GetAllArrivedItemsByProductIdWithSupplierOrderedByWriteOffRuleType(
                                product.Id,
                                storageId,
                                writeOffRule.RuleType,
                                validationItem.ParsedProduct.SupplierName
                            );

                    double decreasedQty = 0d;
                    double toDecreaseQty;

                    SupplyOrderUkraineItem newItem = new() {
                        ProductId = product.Id,
                        NotOrdered = false,
                        NetWeight = validationItem.ParsedProduct.TotalNetWeight,
                        UnitPrice = validationItem.ParsedProduct.TotalNetPrice,
                        SupplyOrderUkraineId = message.UkraineOrderValidation.SupplyOrderUkraine.Id
                    };

                    if (productAvailability.Amount >= validationItem.ParsedProduct.Qty) {
                        newItem.Qty = validationItem.ParsedProduct.Qty;

                        toDecreaseQty = validationItem.ParsedProduct.Qty;

                        productAvailability.Amount = Math.Round(productAvailability.Amount - validationItem.ParsedProduct.Qty, 2,
                            MidpointRounding.AwayFromZero);
                    } else {
                        newItem.Qty = productAvailability.Amount;

                        toDecreaseQty = productAvailability.Amount;

                        productAvailability.Amount = 0;
                    }

                    if (packageOrderItems.Any()) {
                        foreach (PackingListPackageOrderItem packageOrderItem in packageOrderItems) {
                            if (toDecreaseQty.Equals(0d)) break;

                            SupplyOrderUkraineItem itemFromDb =
                                supplyOrderUkraineItemRepository
                                    .GetByRefIdsIfExists(
                                        newItem.ProductId,
                                        newItem.SupplyOrderUkraineId,
                                        packageOrderItem.Supplier.Id
                                    );

                            if (packageOrderItem.RemainingQty >= toDecreaseQty) {
                                if (itemFromDb != null) {
                                    itemFromDb.Qty += toDecreaseQty;

                                    supplyOrderUkraineItemRepository.Update(itemFromDb);
                                } else {
                                    itemFromDb =
                                        supplyOrderUkraineItemRepository
                                            .GetById(
                                                supplyOrderUkraineItemRepository
                                                    .Add(
                                                        new SupplyOrderUkraineItem {
                                                            ProductId = product.Id,
                                                            Qty = toDecreaseQty,
                                                            NotOrdered = false,
                                                            SupplyOrderUkraineId = message.UkraineOrderValidation.SupplyOrderUkraine.Id,
                                                            SupplierId = packageOrderItem.Supplier.Id
                                                        }
                                                    )
                                            );
                                }

                                packageOrderItem.RemainingQty -= toDecreaseQty;

                                decreasedQty += toDecreaseQty;

                                toDecreaseQty = 0d;
                            } else {
                                if (itemFromDb != null) {
                                    itemFromDb.Qty += packageOrderItem.RemainingQty;

                                    supplyOrderUkraineItemRepository.Update(itemFromDb);
                                } else {
                                    itemFromDb =
                                        supplyOrderUkraineItemRepository
                                            .GetById(
                                                supplyOrderUkraineItemRepository
                                                    .Add(
                                                        new SupplyOrderUkraineItem {
                                                            ProductId = product.Id,
                                                            Qty = packageOrderItem.RemainingQty,
                                                            NotOrdered = false,
                                                            SupplyOrderUkraineId = message.UkraineOrderValidation.SupplyOrderUkraine.Id,
                                                            SupplierId = packageOrderItem.Supplier.Id
                                                        }
                                                    )
                                            );
                                }

                                toDecreaseQty -= packageOrderItem.RemainingQty;

                                decreasedQty += packageOrderItem.RemainingQty;

                                packageOrderItem.RemainingQty = 0d;
                            }

                            packingListPackageOrderItemRepository.UpdateRemainingQty(packageOrderItem);

                            itemFromDb.NetWeight = packageOrderItem.NetWeight;
                            itemFromDb.UnitPrice = packageOrderItem.UnitPrice;

                            supplyOrderUkraineItemRepository.Update(itemFromDb);
                        }

                        if (decreasedQty < newItem.Qty) {
                            newItem.Qty -= decreasedQty;

                            supplyOrderUkraineItemRepository.Add(newItem);
                        }
                    } else {
                        newItem.Id = supplyOrderUkraineItemRepository.Add(newItem);
                    }

                    productAvailabilityRepository.Update(productAvailability);
                }
            } else {
                message.UkraineOrderValidation.SupplyOrderUkraine = supplyOrderUkraineRepository.GetById(message.UkraineOrderValidation.SupplyOrderUkraine.Id);

                if (message.UkraineOrderValidation.SupplyOrderUkraine.SupplyOrderUkraineItems.Any(i => i.PlacedQty > 0))
                    throw new Exception("You can not update the Order after it starts placing");

                if (message.UkraineOrderValidation.SupplyOrderUkraine == null)
                    throw new Exception("Incorrect SupplyOrderUkraine entity. Order with such ID does not exists.");

                foreach (SupplyOrderUkraineItem item in message
                             .UkraineOrderValidation
                             .SupplyOrderUkraine
                             .SupplyOrderUkraineItems
                             .Where(i => !i.IsNew()
                                         && !message
                                             .UkraineOrderValidation
                                             .UkraineOrderValidationItems
                                             .Any(item => !item.VendorCodeNotFinded && item.ParsedProduct.ProductId.Equals(i.ProductId)))) {
                    ProductAvailability productAvailability =
                        productAvailabilityRepository
                            .GetByProductIdForCulture(
                                item.ProductId,
                                "pl"
                            );

                    if (productAvailability == null) continue;

                    productAvailability.Amount = Math.Round(productAvailability.Amount + item.Qty, 2, MidpointRounding.AwayFromZero);

                    productAvailabilityRepository.Update(productAvailability);
                }

                supplyOrderUkraineItemRepository
                    .RemoveAllByOrderUkraineIdExceptProvided(
                        message.UkraineOrderValidation.SupplyOrderUkraine.Id,
                        message
                            .UkraineOrderValidation
                            .SupplyOrderUkraine
                            .SupplyOrderUkraineItems
                            .Where(i => !i.IsNew() &&
                                        (message
                                             .UkraineOrderValidation
                                             .UkraineOrderValidationItems
                                             .Any(item => !item.VendorCodeNotFinded && item.ParsedProduct.ProductId.Equals(i.ProductId))
                                         || i.NotOrdered)
                            )
                            .Select(i => i.Id)
                    );

                foreach (UkraineOrderValidationItem validationItem in message
                             .UkraineOrderValidation
                             .UkraineOrderValidationItems
                             .Where(i => !i.ZeroAvailable && !i.VendorCodeNotFinded)) {
                    Product product = getSingleProductRepository.GetByVendorCodeAndRuleLocaleWithProductGroupAndWriteOffRules(validationItem.ParsedProduct.VendorCode, "pl");

                    ProductAvailability productAvailability =
                        productAvailabilityRepository
                            .GetByProductIdForCulture(
                                product.Id,
                                "pl"
                            );

                    if (productAvailability == null) continue;

                    if (message.UkraineOrderValidation.SupplyOrderUkraine.SupplyOrderUkraineItems.Any(i => i.ProductId.Equals(product.Id))) {
                        SupplyOrderUkraineItem itemFromList =
                            message.UkraineOrderValidation.SupplyOrderUkraine.SupplyOrderUkraineItems.First(i => i.ProductId.Equals(product.Id));

                        if (!itemFromList.Qty.Equals(validationItem.ParsedProduct.Qty)) {
                            double differenceQty = Math.Round(itemFromList.Qty - validationItem.ParsedProduct.Qty, 2, MidpointRounding.AwayFromZero);

                            if (differenceQty > 0) {
                                productAvailability.Amount = Math.Round(productAvailability.Amount + differenceQty, 2, MidpointRounding.AwayFromZero);

                                itemFromList.Qty = validationItem.ParsedProduct.Qty;
                            } else {
                                if (productAvailability.Amount > 0) {
                                    differenceQty = 0 - differenceQty;

                                    if (productAvailability.Amount >= differenceQty) {
                                        itemFromList.Qty = Math.Round(itemFromList.Qty + differenceQty, 2, MidpointRounding.AwayFromZero);

                                        productAvailability.Amount = Math.Round(productAvailability.Amount - differenceQty, 2, MidpointRounding.AwayFromZero);
                                    } else {
                                        itemFromList.Qty = Math.Round(itemFromList.Qty + productAvailability.Amount, 2, MidpointRounding.AwayFromZero);

                                        productAvailability.Amount = 0;
                                    }
                                }
                            }
                        }

                        itemFromList.IsUpdated = true;
                        itemFromList.NetWeight = validationItem.ParsedProduct.TotalNetWeight;
                        itemFromList.UnitPrice = validationItem.ParsedProduct.TotalNetPrice;
                    } else {
                        if (productAvailability.Amount > 0) {
                            SupplyOrderUkraineItem newItem = new() {
                                ProductId = product.Id,
                                NotOrdered = false,
                                NetWeight = validationItem.ParsedProduct.TotalNetWeight,
                                UnitPrice = validationItem.ParsedProduct.TotalNetPrice,
                                SupplyOrderUkraineId = message.UkraineOrderValidation.SupplyOrderUkraine.Id
                            };

                            if (productAvailability.Amount >= validationItem.ParsedProduct.Qty) {
                                newItem.Qty = validationItem.ParsedProduct.Qty;

                                productAvailability.Amount = Math.Round(productAvailability.Amount - validationItem.ParsedProduct.Qty, 2,
                                    MidpointRounding.AwayFromZero);
                            } else {
                                newItem.Qty = productAvailability.Amount;

                                productAvailability.Amount = 0;
                            }

                            newItem.Id = supplyOrderUkraineItemRepository.Add(newItem);
                        }
                    }

                    productAvailabilityRepository.Update(productAvailability);
                }

                supplyOrderUkraineItemRepository
                    .Update(
                        message
                            .UkraineOrderValidation
                            .SupplyOrderUkraine
                            .SupplyOrderUkraineItems
                            .Where(i => !i.IsNew() && i.IsUpdated)
                    );
            }

            Sender.Tell(
                supplyOrderUkraineRepository
                    .GetById(
                        message.UkraineOrderValidation.SupplyOrderUkraine.Id
                    )
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessPreviewValidateAddOrUpdateSupplyOrderUkraineMessage(PreviewValidateAddOrUpdateSupplyOrderUkraineMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            List<ParsedProductForUkraine> parsedProducts =
                _xlsFactoryManager
                    .NewParseConfigurationXlsManager()
                    .GetProductsFromUkraineSupplyDocumentsByConfiguration(
                        message.PathToFile,
                        message.ParseConfiguration
                    );

            UkraineOrderValidation ukraineOrderValidation = new() {
                SupplyOrderUkraine = message.SupplyOrderUkraine
            };

            foreach (ParsedProductForUkraine parsedProduct in parsedProducts) {
                Product product = getSingleProductRepository.GetProductByVendorCode(parsedProduct.VendorCode);

                if (product == null) {
                    ukraineOrderValidation
                        .UkraineOrderValidationItems
                        .Add(
                            new UkraineOrderValidationItem {
                                ProductVendorCode = parsedProduct.VendorCode,
                                HasError = true,
                                VendorCodeNotFinded = true,
                                ParsedProduct = parsedProduct
                            }
                        );
                } else {
                    parsedProduct.ProductId = product.Id;

                    ProductAvailability productAvailability =
                        productAvailabilityRepository
                            .GetByProductIdForCulture(
                                product.Id,
                                "pl"
                            );

                    if (productAvailability != null && productAvailability.Amount > 0) {
                        if (productAvailability.Amount >= parsedProduct.Qty)
                            ukraineOrderValidation
                                .UkraineOrderValidationItems
                                .Add(
                                    new UkraineOrderValidationItem {
                                        ProductVendorCode = parsedProduct.VendorCode,
                                        ParsedProduct = parsedProduct
                                    }
                                );
                        else
                            ukraineOrderValidation
                                .UkraineOrderValidationItems
                                .Add(
                                    new UkraineOrderValidationItem {
                                        ProductVendorCode = parsedProduct.VendorCode,
                                        HasError = true,
                                        LessAvailable = true,
                                        AvailableQty = productAvailability.Amount,
                                        ParsedProduct = parsedProduct
                                    }
                                );
                    } else {
                        ukraineOrderValidation
                            .UkraineOrderValidationItems
                            .Add(
                                new UkraineOrderValidationItem {
                                    ProductVendorCode = parsedProduct.VendorCode,
                                    HasError = true,
                                    ZeroAvailable = true,
                                    ParsedProduct = parsedProduct
                                }
                            );
                    }
                }
            }

            ukraineOrderValidation.HasErrors = ukraineOrderValidation.UkraineOrderValidationItems.Any(i => i.HasError);

            ukraineOrderValidation.UkraineOrderValidationItems =
                ukraineOrderValidation
                    .UkraineOrderValidationItems
                    .OrderByDescending(i => i.HasError)
                    .ThenByDescending(i => i.VendorCodeNotFinded)
                    .ThenByDescending(i => i.ZeroAvailable)
                    .ThenByDescending(i => i.LessAvailable)
                    .ToList();

            Sender.Tell(ukraineOrderValidation);
        } catch (SupplyDocumentParseException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessAddNewSupplyOrderUkraineFromTaxFreePackListMessage(AddNewSupplyOrderUkraineFromTaxFreePackListMessage message) {
        try {
            using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.SupplyOrderUkraine == null)
                throw new Exception("SupplyOrderUkraine entity can not be null or empty");
            if (!message.SupplyOrderUkraine.IsNew())
                throw new Exception("Existing SupplyOrderUkraine entity is not valid payload for current request");
            if (message.SupplyOrderUkraine.ClientAgreement == null || message.SupplyOrderUkraine.ClientAgreement.IsNew())
                throw new Exception("You need to specify ClientAgreement");
            if (message.SupplyOrderUkraine.Organization == null || message.SupplyOrderUkraine.Organization.IsNew())
                throw new Exception("You need to specify Organization");
            if (message.SupplyOrderUkraine.Supplier == null || message.SupplyOrderUkraine.Supplier.IsNew())
                throw new Exception("You need to specify Supplier");

            ITaxFreePackListRepository taxFreePackListRepository =
                _supplyUkraineRepositoriesFactory.NewTaxFreePackListRepository(connection, exchangeRateConnection);

            TaxFreePackList taxFreePackList = taxFreePackListRepository.GetByNetIdForConsignment(message.PackListNetId);

            if (taxFreePackList == null)
                throw new Exception("TaxFreePackList with provided NetId does not exists in database");
            if (!taxFreePackList.IsSent)
                throw new Exception("Selected TaxFreePackList still was not sent");
            if (taxFreePackList.SupplyOrderUkraineId.HasValue)
                throw new Exception("From selected TaxFreePackList already created new SupplyOrderUkraine");

            IActReconciliationRepository actReconciliationRepository = _supplyUkraineRepositoriesFactory.NewActReconciliationRepository(connection);
            ISupplyOrderUkraineRepository supplyOrderUkraineRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineRepository(connection);
            ISupplyOrderUkraineItemRepository supplyOrderUkraineItemRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineItemRepository(connection);

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            message.SupplyOrderUkraine.ResponsibleId = user.Id;
            message.SupplyOrderUkraine.OrganizationId = message.SupplyOrderUkraine.Organization.Id;
            message.SupplyOrderUkraine.ClientAgreementId = message.SupplyOrderUkraine.ClientAgreement.Id;
            message.SupplyOrderUkraine.SupplierId = message.SupplyOrderUkraine.Supplier.Id;
            message.SupplyOrderUkraine.IsDirectFromSupplier = false;
            message.SupplyOrderUkraine.InvNumber = taxFreePackList.Number;
            message.SupplyOrderUkraine.FromDate =
                message.SupplyOrderUkraine.FromDate.Year.Equals(1)
                    ? DateTime.UtcNow
                    : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrderUkraine.FromDate);

            SupplyOrderUkraine lastRecord = supplyOrderUkraineRepository.GetLastRecord();

            if (lastRecord != null && DateTime.Now.Year.Equals(lastRecord.Created.Year))
                message.SupplyOrderUkraine.Number = $"{Convert.ToInt32(lastRecord.Number) + 1:D10}";
            else
                message.SupplyOrderUkraine.Number = $"{string.Format("{0:D10}", 1)}";

            message.SupplyOrderUkraine.Id = supplyOrderUkraineRepository.Add(message.SupplyOrderUkraine);

            List<SupplyOrderUkraineItem> supplyOrderUkraineItems = new();

            foreach (SupplyOrderUkraineCartItem item in taxFreePackList.SupplyOrderUkraineCartItems)
                if (item.SupplyOrderUkraineCartItemReservations.Any())
                    supplyOrderUkraineItems
                        .AddRange(
                            item.SupplyOrderUkraineCartItemReservations.Select(reservation => new SupplyOrderUkraineItem {
                                ProductId = item.ProductId,
                                Qty = reservation.Qty,
                                NotOrdered = false,
                                UnitPrice = item.UnitPrice,
                                GrossUnitPrice = item.UnitPrice,
                                AccountingGrossUnitPrice = item.UnitPrice,
                                UnitPriceLocal = item.UnitPrice,
                                GrossUnitPriceLocal = item.UnitPrice,
                                NetWeight = item.NetWeight,
                                SupplyOrderUkraineId = message.SupplyOrderUkraine.Id,
                                SupplierId = item.SupplierId,
                                ConsignmentItemId = reservation.ConsignmentItemId
                            })
                        );
                else
                    supplyOrderUkraineItems.Add(new SupplyOrderUkraineItem {
                        ProductId = item.ProductId,
                        Qty = item.UploadedQty,
                        NotOrdered = false,
                        UnitPrice = item.UnitPrice,
                        GrossUnitPrice = item.UnitPrice,
                        AccountingGrossUnitPrice = item.UnitPrice,
                        UnitPriceLocal = item.UnitPrice,
                        GrossUnitPriceLocal = item.UnitPrice,
                        NetWeight = item.NetWeight,
                        SupplyOrderUkraineId = message.SupplyOrderUkraine.Id,
                        SupplierId = item.SupplierId
                    });

            supplyOrderUkraineItemRepository.Add(supplyOrderUkraineItems);

            taxFreePackList.SupplyOrderUkraineId = message.SupplyOrderUkraine.Id;

            taxFreePackListRepository.Update(taxFreePackList);

            taxFreePackList.SupplyOrderUkraine =
                supplyOrderUkraineRepository
                    .GetById(
                        message.SupplyOrderUkraine.Id
                    );

            ActReconciliation actReconciliation = new() {
                FromDate = taxFreePackList.SupplyOrderUkraine.FromDate,
                ResponsibleId = user.Id,
                SupplyOrderUkraineId = taxFreePackList.SupplyOrderUkraine.Id,
                Number = taxFreePackList.SupplyOrderUkraine.Number
            };

            actReconciliation.Id = actReconciliationRepository.Add(actReconciliation);

            List<ActReconciliationItem> items =
                taxFreePackList.SupplyOrderUkraine.SupplyOrderUkraineItems.Select(item => new ActReconciliationItem {
                    ProductId = item.ProductId,
                    ActReconciliationId = actReconciliation.Id,
                    HasDifference = !item.IsFullyPlaced,
                    NegativeDifference = !item.NotOrdered && item.Qty > item.PlacedQty,
                    QtyDifference = item.Qty - item.PlacedQty,
                    ActualQty = item.NotOrdered ? item.Qty : item.PlacedQty,
                    OrderedQty = item.NotOrdered ? 0d : item.Qty,
                    UnitPrice = item.UnitPrice,
                    NetWeight = item.NetWeight,
                    SupplyOrderUkraineItemId = item.Id
                }).ToList();

            _supplyUkraineRepositoriesFactory
                .NewActReconciliationItemRepository(connection)
                .Add(
                    items
                );

            taxFreePackList.SupplyOrderUkraine
                .ActReconciliations
                .Add(
                    actReconciliationRepository.GetById(
                        actReconciliation.Id
                    )
                );

            Sender.Tell(taxFreePackList.SupplyOrderUkraine);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessAddNewSupplyOrderUkraineFromSadMessage(AddNewSupplyOrderUkraineFromSadMessage message) {
        try {
            using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.SupplyOrderUkraine == null)
                throw new Exception("SupplyOrderUkraine entity can not be null or empty");
            if (!message.SupplyOrderUkraine.IsNew())
                throw new Exception("Existing SupplyOrderUkraine entity is not valid payload for current request");
            if (message.SupplyOrderUkraine.ClientAgreement == null || message.SupplyOrderUkraine.ClientAgreement.IsNew())
                throw new Exception("You need to specify ClientAgreement");
            if (message.SupplyOrderUkraine.Organization == null || message.SupplyOrderUkraine.Organization.IsNew())
                throw new Exception("You need to specify Organization");
            if (message.SupplyOrderUkraine.Supplier == null || message.SupplyOrderUkraine.Supplier.IsNew())
                throw new Exception("You need to specify Supplier");

            ISadRepository sadRepository = _supplyUkraineRepositoriesFactory.NewSadRepository(connection, exchangeRateConnection);

            Sad sad = sadRepository.GetByNetIdForConsignment(message.SadNetId);

            if (sad == null)
                throw new Exception("Sad with provided NetId does not exists in database");
            if (!sad.IsSend)
                throw new Exception("Selected Sad still was not sent");
            if (sad.SupplyOrderUkraineId.HasValue)
                throw new Exception("From selected Sad already created new SupplyOrderUkraine");

            IActReconciliationRepository actReconciliationRepository = _supplyUkraineRepositoriesFactory.NewActReconciliationRepository(connection);
            ISupplyOrderUkraineRepository supplyOrderUkraineRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineRepository(connection);
            ISupplyOrderUkraineItemRepository supplyOrderUkraineItemRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineItemRepository(connection);

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            message.SupplyOrderUkraine.ResponsibleId = user.Id;
            message.SupplyOrderUkraine.OrganizationId = message.SupplyOrderUkraine.Organization.Id;
            message.SupplyOrderUkraine.ClientAgreementId = message.SupplyOrderUkraine.ClientAgreement.Id;
            message.SupplyOrderUkraine.SupplierId = message.SupplyOrderUkraine.Supplier.Id;
            message.SupplyOrderUkraine.IsDirectFromSupplier = false;
            message.SupplyOrderUkraine.InvNumber = sad.Number;
            message.SupplyOrderUkraine.FromDate =
                message.SupplyOrderUkraine.FromDate.Year.Equals(1)
                    ? DateTime.UtcNow
                    : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrderUkraine.FromDate);

            SupplyOrderUkraine lastRecord = supplyOrderUkraineRepository.GetLastRecord();

            if (lastRecord != null && DateTime.Now.Year.Equals(lastRecord.Created.Year))
                message.SupplyOrderUkraine.Number = $"{Convert.ToInt32(lastRecord.Number) + 1:D10}";
            else
                message.SupplyOrderUkraine.Number = string.Format("{0:D10}", 1);

            message.SupplyOrderUkraine.Id = supplyOrderUkraineRepository.Add(message.SupplyOrderUkraine);

            List<SupplyOrderUkraineItem> supplyOrderUkraineItems = new();

            foreach (SadItem sadItem in sad.SadItems.Where(i => i.SupplyOrderUkraineCartItemId.HasValue))
                if (sadItem.SupplyOrderUkraineCartItem.SupplyOrderUkraineCartItemReservations.Any())
                    supplyOrderUkraineItems
                        .AddRange(
                            sadItem.SupplyOrderUkraineCartItem.SupplyOrderUkraineCartItemReservations.Select(reservation => new SupplyOrderUkraineItem {
                                ProductId = sadItem.SupplyOrderUkraineCartItem.ProductId,
                                Qty = reservation.Qty,
                                NotOrdered = false,
                                UnitPrice = sadItem.SupplyOrderUkraineCartItem.UnitPrice,
                                GrossUnitPrice = sadItem.SupplyOrderUkraineCartItem.UnitPrice,
                                AccountingGrossUnitPrice = sadItem.SupplyOrderUkraineCartItem.UnitPrice,
                                UnitPriceLocal = sadItem.SupplyOrderUkraineCartItem.UnitPrice,
                                GrossUnitPriceLocal = sadItem.SupplyOrderUkraineCartItem.UnitPrice,
                                NetWeight = sadItem.SupplyOrderUkraineCartItem.NetWeight,
                                SupplyOrderUkraineId = message.SupplyOrderUkraine.Id,
                                SupplierId = sadItem.SupplyOrderUkraineCartItem.SupplierId,
                                ConsignmentItemId = reservation.ConsignmentItemId
                            })
                        );
                else
                    supplyOrderUkraineItems.Add(new SupplyOrderUkraineItem {
                        ProductId = sadItem.SupplyOrderUkraineCartItem.ProductId,
                        Qty = sadItem.Qty,
                        NotOrdered = false,
                        UnitPrice = sadItem.SupplyOrderUkraineCartItem.UnitPrice,
                        GrossUnitPrice = sadItem.SupplyOrderUkraineCartItem.UnitPrice,
                        AccountingGrossUnitPrice = sadItem.SupplyOrderUkraineCartItem.UnitPrice,
                        UnitPriceLocal = sadItem.SupplyOrderUkraineCartItem.UnitPrice,
                        GrossUnitPriceLocal = sadItem.SupplyOrderUkraineCartItem.UnitPrice,
                        NetWeight = sadItem.SupplyOrderUkraineCartItem.NetWeight,
                        SupplyOrderUkraineId = message.SupplyOrderUkraine.Id,
                        SupplierId = sadItem.SupplyOrderUkraineCartItem.SupplierId
                    });

            supplyOrderUkraineItemRepository.Add(supplyOrderUkraineItems);

            sad.SupplyOrderUkraineId = message.SupplyOrderUkraine.Id;

            sadRepository.Update(sad);

            sad.SupplyOrderUkraine =
                supplyOrderUkraineRepository
                    .GetById(
                        message.SupplyOrderUkraine.Id
                    );

            ActReconciliation actReconciliation = new() {
                FromDate = sad.SupplyOrderUkraine.FromDate,
                ResponsibleId = user.Id,
                SupplyOrderUkraineId = sad.SupplyOrderUkraine.Id,
                Number = sad.SupplyOrderUkraine.Number
            };

            actReconciliation.Id = actReconciliationRepository.Add(actReconciliation);

            List<ActReconciliationItem> items =
                sad.SupplyOrderUkraine.SupplyOrderUkraineItems.Select(item => new ActReconciliationItem {
                    ProductId = item.ProductId,
                    ActReconciliationId = actReconciliation.Id,
                    HasDifference = !item.IsFullyPlaced,
                    NegativeDifference = !item.NotOrdered && item.Qty > item.PlacedQty,
                    QtyDifference = item.Qty - item.PlacedQty,
                    ActualQty = item.NotOrdered ? item.Qty : item.PlacedQty,
                    OrderedQty = item.NotOrdered ? 0d : item.Qty,
                    UnitPrice = item.UnitPrice,
                    NetWeight = item.NetWeight,
                    SupplyOrderUkraineItemId = item.Id
                }).ToList();

            _supplyUkraineRepositoriesFactory
                .NewActReconciliationItemRepository(connection)
                .Add(
                    items
                );

            sad.SupplyOrderUkraine
                .ActReconciliations
                .Add(
                    actReconciliationRepository.GetById(
                        actReconciliation.Id
                    )
                );

            Sender.Tell(sad.SupplyOrderUkraine);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdateSupplyOrderUkraineMessage(UpdateSupplyOrderUkraineMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.SupplyOrderUkraine == null) throw new Exception("SupplyOrderUkraine entity can not be null or empty");
            if (message.SupplyOrderUkraine.IsNew()) throw new Exception("New SupplyOrderUkraine is not valid payload for current request");
            if (message.SupplyOrderUkraine.SupplyOrderUkrainePaymentDeliveryProtocols.Any(p => p.SupplyOrderUkrainePaymentDeliveryProtocolKey == null))
                throw new Exception("All SupplyOrderUkrainePaymentDeliveryProtocols should have specified SupplyOrderUkrainePaymentDeliveryProtocolKey");

            ISupplyOrderUkraineRepository supplyOrderUkraineRepository =
                _supplyUkraineRepositoriesFactory
                    .NewSupplyOrderUkraineRepository(connection);

            SupplyOrderUkraine fromDb = supplyOrderUkraineRepository.GetById(message.SupplyOrderUkraine.Id);

            if (fromDb == null) throw new Exception("Provided SupplyOrderUkraine does not exists in database");

            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);

            User updatedBy = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

            if (message.SupplyOrderUkraine.Organization != null && !fromDb.OrganizationId.Equals(message.SupplyOrderUkraine.Organization.Id)) {
                fromDb.OrganizationId = message.SupplyOrderUkraine.Organization.Id;

                supplyOrderUkraineRepository.UpdateOrganization(fromDb);
            }

            _supplyUkraineRepositoriesFactory
                .NewSupplyOrderUkraineItemRepository(connection)
                .UpdateWeightAndPrice(
                    message
                        .SupplyOrderUkraine
                        .SupplyOrderUkraineItems
                        .Where(i => i.PlacedQty.Equals(0d) && !i.IsNew())
                );

            ISupplyPaymentTaskRepository supplyPaymentTaskRepository = _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);
            IActReconciliationRepository actReconciliationRepository = _supplyUkraineRepositoriesFactory.NewActReconciliationRepository(connection);
            IActReconciliationItemRepository actReconciliationItemRepository = _supplyUkraineRepositoriesFactory.NewActReconciliationItemRepository(connection);
            IDynamicProductPlacementRepository dynamicProductPlacementRepository =
                _supplyUkraineRepositoriesFactory.NewDynamicProductPlacementRepository(connection);
            IDynamicProductPlacementRowRepository dynamicProductPlacementRowRepository =
                _supplyUkraineRepositoriesFactory.NewDynamicProductPlacementRowRepository(connection);
            IDynamicProductPlacementColumnRepository dynamicProductPlacementColumnRepository =
                _supplyUkraineRepositoriesFactory.NewDynamicProductPlacementColumnRepository(connection);

            if (message.SupplyOrderUkraine.DynamicProductPlacementColumns.Any()) {
                foreach (DynamicProductPlacementColumn column in fromDb
                             .DynamicProductPlacementColumns
                             .Where(c => !message.SupplyOrderUkraine.DynamicProductPlacementColumns.Any(col => col.Id.Equals(c.Id))))
                    if (!column.DynamicProductPlacementRows.Any(r => r.DynamicProductPlacements.Any(p => p.IsApplied)))
                        dynamicProductPlacementColumnRepository.RemoveById(column.Id);

                foreach (DynamicProductPlacementColumn column in message.SupplyOrderUkraine.DynamicProductPlacementColumns) {
                    column.FromDate =
                        column.FromDate.Year.Equals(1)
                            ? DateTime.UtcNow.Date
                            : column.FromDate.AddHours(5).Date;

                    column.SupplyOrderUkraineId = fromDb.Id;

                    if (column.IsNew())
                        column.Id = dynamicProductPlacementColumnRepository.Add(column);
                    else
                        dynamicProductPlacementColumnRepository.Update(column);

                    foreach (DynamicProductPlacementRow row in column.DynamicProductPlacementRows.Where(r => r.SupplyOrderUkraineItem != null))
                        if (message.SupplyOrderUkraine.SupplyOrderUkraineItems.Any(i => i.Id.Equals(row.SupplyOrderUkraineItem.Id))) {
                            if (row.IsNew()) {
                                row.DynamicProductPlacementColumnId = column.Id;
                                row.SupplyOrderUkraineItemId = row.SupplyOrderUkraineItem.Id;

                                row.Id = dynamicProductPlacementRowRepository.Add(row);

                                dynamicProductPlacementRepository.Add(new DynamicProductPlacement {
                                    DynamicProductPlacementRowId = row.Id,
                                    StorageNumber = "N",
                                    CellNumber = "N",
                                    RowNumber = "N",
                                    Qty = row.Qty
                                });
                            } else {
                                dynamicProductPlacementRowRepository.Update(row);

                                dynamicProductPlacementRepository
                                    .RemoveAllByRowId(
                                        row.Id
                                    );

                                IEnumerable<DynamicProductPlacement> appliedPlacements =
                                    dynamicProductPlacementRepository
                                        .GetAllAppliedByRowId(
                                            row.Id
                                        );

                                row.Qty = row.Qty - appliedPlacements.Sum(p => p.Qty);

                                if (row.Qty > 0)
                                    dynamicProductPlacementRepository.Add(new DynamicProductPlacement {
                                        DynamicProductPlacementRowId = row.Id,
                                        StorageNumber = "N",
                                        CellNumber = "N",
                                        RowNumber = "N",
                                        Qty = row.Qty
                                    });
                            }
                        }
                }
            } else {
                foreach (DynamicProductPlacementColumn column in fromDb
                             .DynamicProductPlacementColumns)
                    if (!column.DynamicProductPlacementRows.Any(r => r.DynamicProductPlacements.Any(p => p.IsApplied)))
                        dynamicProductPlacementColumnRepository.RemoveById(column.Id);
            }

            ISupplyOrderUkraineItemRepository supplyOrderUkraineItemRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineItemRepository(connection);

            supplyOrderUkraineItemRepository
                .RemoveAllByOrderUkraineIdExceptProvided(
                    message.SupplyOrderUkraine.Id,
                    message
                        .SupplyOrderUkraine
                        .SupplyOrderUkraineItems
                        .Where(i => !i.IsNew())
                        .Select(i => i.Id)
                );

            ActReconciliation actReconciliation = actReconciliationRepository.GetBySupplyOrderUkraineId(message.SupplyOrderUkraine.Id);

            if (actReconciliation != null)
                actReconciliationItemRepository.RemoveAllByActReconciliationIdExceptProvidedSupplyOrderUkraineItemIds(
                    actReconciliation.Id,
                    message
                        .SupplyOrderUkraine
                        .SupplyOrderUkraineItems
                        .Where(i => !i.IsNew())
                        .Select(i => i.Id)
                );

            if (message
                .SupplyOrderUkraine
                .SupplyOrderUkraineItems
                .Any(i => i.IsNew() && i.Product != null && !i.Product.IsNew() && !i.Deleted && i.Qty > 0)) {
                IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);

                foreach (SupplyOrderUkraineItem item in message
                             .SupplyOrderUkraine
                             .SupplyOrderUkraineItems
                             .Where(i => i.IsNew() && i.Product != null && !i.Product.IsNew() && !i.Deleted && i.Qty > 0)) {
                    Product product = getSingleProductRepository.GetByIdAndRuleLocaleWithProductGroupAndWriteOffRules(item.Product.Id, "pl");

                    SupplyOrderUkraineItem itemFromDb =
                        supplyOrderUkraineItemRepository
                            .GetNotOrderedByRefIdsIfExists(
                                product.Id,
                                message.SupplyOrderUkraine.Id,
                                fromDb.SupplierId
                            );

                    if (itemFromDb != null) {
                        itemFromDb.Qty += item.Qty;
                        itemFromDb.UnitPrice = item.UnitPrice;
                        itemFromDb.GrossUnitPrice = item.UnitPrice;
                        itemFromDb.NetWeight = item.NetWeight;

                        supplyOrderUkraineItemRepository.Update(itemFromDb);

                        ActReconciliationItem reconciliationItem = actReconciliationItemRepository.GetBySupplyOrderUkraineItemId(itemFromDb.Id);

                        if (actReconciliation != null) {
                            if (reconciliationItem != null) {
                                reconciliationItem.HasDifference = !itemFromDb.IsFullyPlaced;
                                reconciliationItem.NegativeDifference = !itemFromDb.NotOrdered && itemFromDb.Qty > itemFromDb.PlacedQty;
                                reconciliationItem.QtyDifference = itemFromDb.Qty - itemFromDb.PlacedQty;
                                reconciliationItem.ActualQty = itemFromDb.NotOrdered ? itemFromDb.Qty : itemFromDb.PlacedQty;
                                reconciliationItem.OrderedQty = itemFromDb.NotOrdered ? 0d : itemFromDb.Qty;
                                reconciliationItem.UnitPrice = item.UnitPrice;
                                reconciliationItem.NetWeight = item.NetWeight;

                                actReconciliationItemRepository.Update(reconciliationItem);
                            } else {
                                actReconciliationItemRepository.Add(new ActReconciliationItem {
                                    ProductId = itemFromDb.ProductId,
                                    ActReconciliationId = actReconciliation.Id,
                                    HasDifference = !itemFromDb.IsFullyPlaced,
                                    NegativeDifference = !itemFromDb.NotOrdered && itemFromDb.Qty > itemFromDb.PlacedQty,
                                    QtyDifference = itemFromDb.Qty - itemFromDb.PlacedQty,
                                    ActualQty = itemFromDb.NotOrdered ? itemFromDb.Qty : itemFromDb.PlacedQty,
                                    OrderedQty = itemFromDb.NotOrdered ? 0d : itemFromDb.Qty,
                                    UnitPrice = item.UnitPrice,
                                    NetWeight = item.NetWeight,
                                    SupplyOrderUkraineItemId = itemFromDb.Id
                                });
                            }
                        }
                    } else {
                        itemFromDb =
                            supplyOrderUkraineItemRepository
                                .GetById(
                                    supplyOrderUkraineItemRepository
                                        .Add(
                                            new SupplyOrderUkraineItem {
                                                ProductId = product.Id,
                                                Qty = item.Qty,
                                                UnitPrice = item.UnitPrice,
                                                GrossUnitPrice = item.UnitPrice,
                                                NetWeight = item.NetWeight,
                                                NotOrdered = true,
                                                SupplyOrderUkraineId = message.SupplyOrderUkraine.Id,
                                                SupplierId = fromDb.SupplierId
                                            }
                                        )
                                );

                        if (actReconciliation != null)
                            actReconciliationItemRepository.Add(new ActReconciliationItem {
                                ProductId = itemFromDb.ProductId,
                                ActReconciliationId = actReconciliation.Id,
                                HasDifference = !itemFromDb.IsFullyPlaced,
                                NegativeDifference = !itemFromDb.NotOrdered && itemFromDb.Qty > itemFromDb.PlacedQty,
                                QtyDifference = itemFromDb.Qty - itemFromDb.PlacedQty,
                                ActualQty = itemFromDb.NotOrdered ? itemFromDb.Qty : itemFromDb.PlacedQty,
                                OrderedQty = itemFromDb.NotOrdered ? 0d : itemFromDb.Qty,
                                UnitPrice = item.UnitPrice,
                                NetWeight = item.NetWeight,
                                SupplyOrderUkraineItemId = itemFromDb.Id
                            });
                    }
                }
            }

            if (message.SupplyOrderUkraine.MergedServices.Any()) {
                IMergedServiceRepository mergedServiceRepository = _supplyRepositoriesFactory.NewMergedServiceRepository(connection);
                IInvoiceDocumentRepository invoiceDocumentRepository = _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection);
                IServiceDetailItemRepository serviceDetailItemRepository = _supplyRepositoriesFactory.NewServiceDetailItemRepository(connection);
                ISupplyServiceNumberRepository supplyServiceNumberRepository = _supplyRepositoriesFactory.NewSupplyServiceNumberRepository(connection);
                IServiceDetailItemKeyRepository serviceDetailItemKeyRepository = _supplyRepositoriesFactory.NewServiceDetailItemKeyRepository(connection);
                ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
                    _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);

                foreach (MergedService service in message.SupplyOrderUkraine.MergedServices.Where(s => !s.IsNew() && s.Deleted)) {
                    mergedServiceRepository.Remove(service.Id);

                    if (service.SupplyOrganizationAgreement != null && !service.SupplyOrganizationAgreement.IsNew()) {
                        service.SupplyOrganizationAgreement = supplyOrganizationAgreementRepository.GetById(service.SupplyOrganizationAgreement.Id);

                        service.SupplyOrganizationAgreement.CurrentAmount =
                            Math.Round(service.SupplyOrganizationAgreement.CurrentAmount + service.GrossPrice, 2);

                        service.SupplyOrganizationAgreement.AccountingCurrentAmount =
                            Math.Round(service.SupplyOrganizationAgreement.AccountingCurrentAmount + service.AccountingGrossPrice, 2);

                        supplyOrganizationAgreementRepository.UpdateCurrentAmount(service.SupplyOrganizationAgreement);
                    }

                    if (service.SupplyPaymentTaskId.HasValue)
                        supplyPaymentTaskRepository.RemoveById(service.SupplyPaymentTaskId ?? 0, updatedBy.Id);

                    if (service.AccountingPaymentTaskId.HasValue)
                        supplyPaymentTaskRepository.RemoveById(service.AccountingPaymentTaskId ?? 0, updatedBy.Id);
                }

                foreach (MergedService service in message.SupplyOrderUkraine.MergedServices.Where(s => !s.IsNew() && !s.Deleted))
                    if (service.InvoiceDocuments.Any())
                        invoiceDocumentRepository
                            .RemoveAllByMergedServiceIdExceptProvided(
                                service.Id,
                                service.InvoiceDocuments.Where(d => !d.IsNew() && !d.Deleted).Select(d => d.Id)
                            );
                    else
                        invoiceDocumentRepository.RemoveAllByMergedServiceId(service.Id);

                mergedServiceRepository.Add(
                    message
                        .SupplyOrderUkraine
                        .MergedServices
                        .Where(s => s.IsNew() && !s.InvoiceDocuments.Any(d => d.IsNew()) && !s.ServiceDetailItems.Any() && !s.Deleted)
                        .Select(service => {
                            service.NetPrice = Math.Round(service.GrossPrice * 100 / Convert.ToDecimal(100 + service.VatPercent), 2);
                            service.AccountingNetPrice = Math.Round(service.AccountingGrossPrice * 100 / Convert.ToDecimal(100 + service.AccountingVatPercent), 2);
                            service.Vat = Math.Round(service.GrossPrice - service.NetPrice, 2);
                            service.AccountingVat = Math.Round(service.AccountingGrossPrice - service.AccountingNetPrice, 2);

                            service.SupplyOrganizationId = service.SupplyOrganization.Id;
                            service.SupplyOrderUkraineId = message.SupplyOrderUkraine.Id;
                            service.UserId = updatedBy.Id;
                            service.SupplyOrganizationAgreementId = service.SupplyOrganizationAgreement.Id;

                            service.FromDate =
                                !service.FromDate.HasValue
                                    ? DateTime.UtcNow
                                    : TimeZoneInfo.ConvertTimeToUtc(service.FromDate.Value);

                            if (service.SupplyPaymentTask != null) {
                                service.SupplyPaymentTask.UserId = service.SupplyPaymentTask.User.Id;
                                service.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                                service.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.MergedService;

                                service.SupplyPaymentTask.PayToDate =
                                    !service.SupplyPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(service.SupplyPaymentTask.PayToDate.Value);

                                service.SupplyPaymentTask.NetPrice = service.NetPrice;
                                service.SupplyPaymentTask.GrossPrice = service.GrossPrice;

                                service.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(service.SupplyPaymentTask);

                                SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                                if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                                    service.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                                else
                                    service.ServiceNumber = string.Format("P{0:D10}", 1);

                                supplyServiceNumberRepository.Add(service.ServiceNumber);
                            }

                            if (service.AccountingPaymentTask != null) {
                                service.AccountingPaymentTask.UserId = service.AccountingPaymentTask.User.Id;
                                service.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                                service.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.MergedService;
                                service.AccountingPaymentTask.IsAccounting = true;

                                service.AccountingPaymentTask.PayToDate =
                                    !service.AccountingPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(service.AccountingPaymentTask.PayToDate.Value);

                                service.AccountingPaymentTask.NetPrice = service.AccountingNetPrice;
                                service.AccountingPaymentTask.GrossPrice = service.AccountingGrossPrice;

                                service.AccountingPaymentTaskId = supplyPaymentTaskRepository.Add(service.AccountingPaymentTask);

                                SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                                if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                                    service.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                                else
                                    service.ServiceNumber = string.Format("P{0:D10}", 1);

                                supplyServiceNumberRepository.Add(service.ServiceNumber);
                            }

                            if (service.SupplyOrganizationAgreement != null && !service.SupplyOrganizationAgreement.IsNew()) {
                                service.SupplyOrganizationAgreement = supplyOrganizationAgreementRepository.GetById(service.SupplyOrganizationAgreement.Id);

                                service.SupplyOrganizationAgreement.CurrentAmount =
                                    Math.Round(service.SupplyOrganizationAgreement.CurrentAmount - service.GrossPrice, 2);

                                service.SupplyOrganizationAgreement.AccountingCurrentAmount =
                                    Math.Round(service.SupplyOrganizationAgreement.AccountingCurrentAmount - service.AccountingGrossPrice, 2);

                                supplyOrganizationAgreementRepository.UpdateCurrentAmount(service.SupplyOrganizationAgreement);
                            }

                            return service;
                        })
                );

                foreach (MergedService service in message.SupplyOrderUkraine.MergedServices.Where(s => !s.IsNew() && !s.Deleted)) {
                    service.NetPrice = Math.Round(service.GrossPrice * 100 / Convert.ToDecimal(100 + service.VatPercent), 2);
                    service.AccountingNetPrice = Math.Round(service.AccountingGrossPrice * 100 / Convert.ToDecimal(100 + service.AccountingVatPercent), 2);
                    service.Vat = Math.Round(service.GrossPrice - service.NetPrice, 2);
                    service.AccountingVat = Math.Round(service.AccountingGrossPrice - service.AccountingNetPrice, 2);

                    service.FromDate = service.FromDate ?? DateTime.UtcNow;

                    if (service.SupplyPaymentTask != null) {
                        if (service.SupplyPaymentTask.IsNew()) {
                            service.SupplyPaymentTask.UserId = service.SupplyPaymentTask.User.Id;
                            service.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                            service.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.MergedService;

                            service.SupplyPaymentTask.PayToDate = service.SupplyPaymentTask.PayToDate ?? DateTime.UtcNow;

                            service.SupplyPaymentTask.NetPrice = service.NetPrice;
                            service.SupplyPaymentTask.GrossPrice = service.GrossPrice;

                            service.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(service.SupplyPaymentTask);
                        } else {
                            if (service.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                                && !service.SupplyPaymentTask.IsAvailableForPayment) {
                                if (service.SupplyPaymentTask.Deleted) {
                                    supplyPaymentTaskRepository.RemoveById(service.SupplyPaymentTask.Id, updatedBy.Id);

                                    service.SupplyPaymentTaskId = null;
                                } else {
                                    service.SupplyPaymentTask.PayToDate = service.SupplyPaymentTask.PayToDate ?? DateTime.UtcNow;

                                    service.SupplyPaymentTask.NetPrice = service.NetPrice;
                                    service.SupplyPaymentTask.GrossPrice = service.NetPrice;

                                    supplyPaymentTaskRepository.Update(service.SupplyPaymentTask);
                                }
                            }
                        }

                        SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                        if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                            service.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                        else
                            service.ServiceNumber = string.Format("P{0:D10}", 1);

                        supplyServiceNumberRepository.Add(service.ServiceNumber);
                    }

                    if (service.AccountingPaymentTask != null) {
                        if (service.AccountingPaymentTask.IsNew()) {
                            service.AccountingPaymentTask.UserId = service.AccountingPaymentTask.User.Id;
                            service.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                            service.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.MergedService;
                            service.AccountingPaymentTask.IsAccounting = true;

                            service.AccountingPaymentTask.PayToDate = service.SupplyPaymentTask.PayToDate ?? DateTime.UtcNow;

                            service.AccountingPaymentTask.NetPrice = service.AccountingNetPrice;
                            service.AccountingPaymentTask.GrossPrice = service.AccountingGrossPrice;

                            service.AccountingPaymentTaskId = supplyPaymentTaskRepository.Add(service.AccountingPaymentTask);
                        } else {
                            if (service.AccountingPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                                && !service.AccountingPaymentTask.IsAvailableForPayment) {
                                if (service.AccountingPaymentTask.Deleted) {
                                    supplyPaymentTaskRepository.RemoveById(service.AccountingPaymentTask.Id, updatedBy.Id);

                                    service.SupplyPaymentTaskId = null;
                                } else {
                                    service.AccountingPaymentTask.PayToDate = service.AccountingPaymentTask.PayToDate ?? DateTime.UtcNow;

                                    service.AccountingPaymentTask.NetPrice = service.AccountingNetPrice;
                                    service.AccountingPaymentTask.GrossPrice = service.AccountingNetPrice;

                                    service.AccountingPaymentTask.IsAccounting = true;

                                    supplyPaymentTaskRepository.Update(service.AccountingPaymentTask);
                                }
                            }
                        }

                        SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                        if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                            service.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                        else
                            service.ServiceNumber = string.Format("P{0:D10}", 1);

                        supplyServiceNumberRepository.Add(service.ServiceNumber);
                    }

                    if (service.InvoiceDocuments.Any(d => d.IsNew()))
                        invoiceDocumentRepository
                            .Add(
                                service.InvoiceDocuments
                                    .Where(d => d.IsNew())
                                    .Select(d => {
                                        d.MergedServiceId = service.Id;

                                        return d;
                                    })
                            );
                    if (service.ServiceDetailItems.Any()) {
                        serviceDetailItemRepository.RemoveAllByMergedServiceIdExceptProvided(
                            service.Id,
                            service.ServiceDetailItems.Where(i => !i.IsNew()).Select(i => i.Id)
                        );

                        InsertOrUpdateServiceDetailItems(
                            serviceDetailItemRepository,
                            serviceDetailItemKeyRepository,
                            service.ServiceDetailItems
                                .Select(i => {
                                    i.MergedServiceId = service.Id;

                                    return i;
                                })
                        );
                    } else {
                        serviceDetailItemRepository.RemoveAllByMergedServiceId(service.Id);
                    }

                    mergedServiceRepository.Update(service);
                }

                foreach (MergedService service in message.SupplyOrderUkraine.MergedServices.Where(s => s.IsNew() && s.InvoiceDocuments.Any(d => d.IsNew()) && !s.Deleted)) {
                    service.NetPrice = Math.Round(service.GrossPrice * 100 / Convert.ToDecimal(100 + service.VatPercent), 2);
                    service.Vat = Math.Round(service.GrossPrice - service.NetPrice, 2);
                    service.AccountingNetPrice = Math.Round(service.AccountingGrossPrice * 100 / Convert.ToDecimal(100 + service.AccountingVatPercent), 2);
                    service.AccountingVat = Math.Round(service.AccountingGrossPrice - service.AccountingNetPrice, 2);

                    if (service.SupplyPaymentTask != null) {
                        service.SupplyPaymentTask.UserId = service.SupplyPaymentTask.User.Id;
                        service.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                        service.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.MergedService;

                        service.SupplyPaymentTask.PayToDate = service.SupplyPaymentTask.PayToDate ?? DateTime.UtcNow;

                        service.SupplyPaymentTask.NetPrice = service.NetPrice;
                        service.SupplyPaymentTask.GrossPrice = service.GrossPrice;

                        service.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(service.SupplyPaymentTask);

                        SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                        if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                            service.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                        else
                            service.ServiceNumber = string.Format("P{0:D10}", 1);

                        supplyServiceNumberRepository.Add(service.ServiceNumber);
                    }

                    if (service.AccountingPaymentTask != null) {
                        service.AccountingPaymentTask.UserId = service.SupplyPaymentTask.User.Id;
                        service.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                        service.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.MergedService;
                        service.AccountingPaymentTask.IsAccounting = true;

                        service.AccountingPaymentTask.PayToDate = service.AccountingPaymentTask.PayToDate ?? DateTime.UtcNow;

                        service.AccountingPaymentTask.NetPrice = service.AccountingNetPrice;
                        service.AccountingPaymentTask.GrossPrice = service.AccountingGrossPrice;

                        service.AccountingPaymentTaskId = supplyPaymentTaskRepository.Add(service.AccountingPaymentTask);

                        SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                        if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                            service.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                        else
                            service.ServiceNumber = string.Format("P{0:D10}", 1);

                        supplyServiceNumberRepository.Add(service.ServiceNumber);
                    }

                    service.SupplyOrganizationId = service.SupplyOrganization.Id;
                    service.SupplyOrderUkraineId = message.SupplyOrderUkraine.Id;
                    service.UserId = updatedBy.Id;
                    service.SupplyOrganizationAgreementId = service.SupplyOrganizationAgreement.Id;

                    service.FromDate = service.FromDate ?? DateTime.UtcNow;

                    service.Id = mergedServiceRepository.Add(service);

                    if (service.SupplyOrganizationAgreement != null && !service.SupplyOrganizationAgreement.IsNew()) {
                        service.SupplyOrganizationAgreement = supplyOrganizationAgreementRepository.GetById(service.SupplyOrganizationAgreement.Id);

                        service.SupplyOrganizationAgreement.CurrentAmount =
                            Math.Round(service.SupplyOrganizationAgreement.CurrentAmount - service.GrossPrice, 2);

                        service.SupplyOrganizationAgreement.AccountingCurrentAmount =
                            Math.Round(service.SupplyOrganizationAgreement.AccountingCurrentAmount - service.AccountingGrossPrice, 2);

                        supplyOrganizationAgreementRepository.UpdateCurrentAmount(service.SupplyOrganizationAgreement);
                    }

                    invoiceDocumentRepository
                        .Add(
                            service.InvoiceDocuments
                                .Where(d => d.IsNew())
                                .Select(d => {
                                    d.MergedServiceId = service.Id;

                                    return d;
                                })
                        );

                    if (service.ServiceDetailItems.Any())
                        InsertOrUpdateServiceDetailItems(
                            serviceDetailItemRepository,
                            serviceDetailItemKeyRepository,
                            service.ServiceDetailItems
                                .Select(i => {
                                    i.MergedServiceId = service.Id;

                                    return i;
                                })
                        );
                }
            }

            if (message.SupplyOrderUkraine.SupplyOrderUkrainePaymentDeliveryProtocols.Any()) {
                ISupplyOrderUkrainePaymentDeliveryProtocolRepository supplyOrderUkrainePaymentDeliveryProtocolRepository =
                    _supplyUkraineRepositoriesFactory.NewSupplyOrderUkrainePaymentDeliveryProtocolRepository(connection);

                ISupplyOrderUkrainePaymentDeliveryProtocolKeyRepository supplyOrderUkrainePaymentDeliveryProtocolKeyRepository =
                    _supplyUkraineRepositoriesFactory.NewSupplyOrderUkrainePaymentDeliveryProtocolKeyRepository(connection);

                foreach (SupplyOrderUkrainePaymentDeliveryProtocol protocol in message.SupplyOrderUkraine.SupplyOrderUkrainePaymentDeliveryProtocols.Where(p =>
                             !p.IsNew() && p.Deleted)) {
                    supplyOrderUkrainePaymentDeliveryProtocolRepository.Remove(protocol.Id);

                    if (protocol.SupplyPaymentTaskId.HasValue) supplyPaymentTaskRepository.RemoveById(protocol.SupplyPaymentTaskId ?? 0, updatedBy.Id);
                }

                supplyOrderUkrainePaymentDeliveryProtocolRepository
                    .Add(
                        message
                            .SupplyOrderUkraine
                            .SupplyOrderUkrainePaymentDeliveryProtocols
                            .Where(p => p.IsNew() && !p.Deleted)
                            .Select(protocol => {
                                if (protocol.SupplyPaymentTask != null) {
                                    protocol.SupplyPaymentTask.UserId = protocol.SupplyPaymentTask.User.Id;
                                    protocol.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                                    protocol.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.UkrainePaymentDeliveryProtocol;

                                    protocol.SupplyPaymentTask.IsAccounting = protocol.IsAccounting;

                                    protocol.SupplyPaymentTask.PayToDate =
                                        !protocol.SupplyPaymentTask.PayToDate.HasValue
                                            ? DateTime.UtcNow
                                            : TimeZoneInfo.ConvertTimeToUtc(protocol.SupplyPaymentTask.PayToDate.Value);

                                    protocol.SupplyPaymentTask.NetPrice = protocol.Value;
                                    protocol.SupplyPaymentTask.GrossPrice = protocol.Value;

                                    protocol.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(protocol.SupplyPaymentTask);
                                }

                                protocol.UserId = updatedBy.Id;
                                protocol.Created = DateTime.UtcNow;
                                protocol.SupplyOrderUkraineId = message.SupplyOrderUkraine.Id;
                                if (protocol.SupplyOrderUkrainePaymentDeliveryProtocolKey.IsNew())
                                    protocol.SupplyOrderUkrainePaymentDeliveryProtocolKeyId =
                                        supplyOrderUkrainePaymentDeliveryProtocolKeyRepository.Add(protocol.SupplyOrderUkrainePaymentDeliveryProtocolKey);
                                else
                                    protocol.SupplyOrderUkrainePaymentDeliveryProtocolKeyId = protocol.SupplyOrderUkrainePaymentDeliveryProtocolKey.Id;

                                return protocol;
                            })
                    );

                supplyOrderUkrainePaymentDeliveryProtocolRepository
                    .Update(
                        message
                            .SupplyOrderUkraine
                            .SupplyOrderUkrainePaymentDeliveryProtocols
                            .Where(p => !p.IsNew() && !p.Deleted)
                            .Select(protocol => {
                                if (protocol.SupplyPaymentTask != null && protocol.SupplyPaymentTask.IsNew()) {
                                    protocol.SupplyPaymentTask.UserId = protocol.SupplyPaymentTask.User.Id;
                                    protocol.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                                    protocol.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.UkrainePaymentDeliveryProtocol;

                                    protocol.SupplyPaymentTask.IsAccounting = protocol.IsAccounting;

                                    protocol.SupplyPaymentTask.PayToDate =
                                        !protocol.SupplyPaymentTask.PayToDate.HasValue
                                            ? DateTime.UtcNow
                                            : TimeZoneInfo.ConvertTimeToUtc(protocol.SupplyPaymentTask.PayToDate.Value);

                                    protocol.SupplyPaymentTask.NetPrice = protocol.Value;
                                    protocol.SupplyPaymentTask.GrossPrice = protocol.Value;

                                    protocol.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(protocol.SupplyPaymentTask);
                                } else if (protocol.SupplyPaymentTask != null && !protocol.SupplyPaymentTask.IsNew() && protocol.SupplyPaymentTask.Deleted) {
                                    supplyPaymentTaskRepository.RemoveById(protocol.SupplyPaymentTask.Id, updatedBy.Id);
                                }

                                protocol.UserId = updatedBy.Id;
                                protocol.Created = DateTime.UtcNow;
                                protocol.SupplyOrderUkraineId = message.SupplyOrderUkraine.Id;
                                protocol.SupplyOrderUkrainePaymentDeliveryProtocolKeyId = protocol.SupplyOrderUkrainePaymentDeliveryProtocolKey.Id;

                                return protocol;
                            })
                    );
            }

            if (message.SupplyOrderUkraine.ShipmentAmountLocal > 0m && !message.SupplyOrderUkraine.ShipmentAmountLocal.Equals(fromDb.ShipmentAmountLocal)) {
                IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
                ICrossExchangeRateRepository crossExchangeRateRepository = _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection);

                fromDb = supplyOrderUkraineRepository.GetById(message.SupplyOrderUkraine.Id);

                fromDb.ShipmentAmountLocal = message.SupplyOrderUkraine.ShipmentAmountLocal;

                Currency eur = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

                decimal exchangeRateAmount =
                    GetExchangeRateUk(
                        fromDb.ClientAgreement.Agreement.Currency,
                        eur,
                        exchangeRateRepository,
                        crossExchangeRateRepository
                    );

                fromDb.ShipmentAmount =
                    exchangeRateAmount < 0
                        ? decimal.Round(
                            fromDb.ShipmentAmountLocal / (0 - exchangeRateAmount),
                            2,
                            MidpointRounding.AwayFromZero
                        )
                        : decimal.Round(
                            fromDb.ShipmentAmountLocal / exchangeRateAmount,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                supplyOrderUkraineRepository.UpdateShipmentAmount(fromDb);
            }

            ProcessUpdateSupplyOrderUkraineItemPrice(new UpdateSupplyOrderUkraineItemPriceMessage(
                fromDb.Id
            ));

            Sender.Tell(
                supplyOrderUkraineRepository
                    .GetByNetId(
                        fromDb.NetUid
                    )
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetAllSupplyOrdersUkraineFilteredMessage(GetAllSupplyOrdersUkraineFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supplyUkraineRepositoriesFactory
                .NewSupplyOrderUkraineRepository(connection)
                .GetAllFiltered(
                    message.From,
                    message.To,
                    message.SupplierName,
                    message.CurrencyId,
                    message.Limit,
                    message.Offset,
                    message.NonPlaced
                )
        );
    }

    private void ProcessGetSupplyOrderUkraineByNetIdMessage(GetSupplyOrderUkraineByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyUkraineRepositoriesFactory
            .NewSupplyOrderUkraineRepository(connection)
            .GetByNetId(message.NetId));
    }

    private void ProcessSetSupplyOrderUkraineIsPlacedByNetIdMessage(SetSupplyOrderUkraineIsPlacedByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyOrderUkraineRepository supplyOrderUkraineRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineRepository(connection);

        SupplyOrderUkraine supplyOrderUkraine = supplyOrderUkraineRepository.GetByNetId(message.NetId);

        if (supplyOrderUkraine != null) {
            supplyOrderUkraine.IsPlaced = true;

            _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineRepository(connection).UpdateIsPlaced(supplyOrderUkraine);
        }

        Sender.Tell(supplyOrderUkraine);
    }

    private void ProcessAddOrUpdateSupplyOrderUkraineFromSupplierMessage(AddOrUpdateSupplyOrderUkraineFromSupplierMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            IProductSpecificationRepository productSpecificationRepository =
                _productRepositoriesFactory.NewProductSpecificationRepository(connection);

            List<ParsedProductForUkraine> parsedProducts =
                _xlsFactoryManager
                    .NewParseConfigurationXlsManager()
                    .GetProductsForUkraineOrderFromSupplierByConfiguration(
                        message.PathToFile,
                        message.ParseConfiguration
                    );

            AddSupplyOrderUkraineFromFileResponse response = new();

            foreach (ParsedProductForUkraine parsedProduct in
                     from parsedProduct
                         in parsedProducts
                     let product = getSingleProductRepository.GetProductByVendorCode(parsedProduct.VendorCode)
                     where product == null
                     select parsedProduct)
                response.MissingVendorCodes.Add(parsedProduct.VendorCode);

            if (response.HasError) {
                response.MissingVendorCodesFileUrl =
                    _xlsFactoryManager
                        .NewProductsXlsManager()
                        .ExportMissingVendorCodes(message.TempFolderPath, response.MissingVendorCodes);

                Sender.Tell(response);
                return;
            }

            IActReconciliationRepository actReconciliationRepository = _supplyUkraineRepositoriesFactory.NewActReconciliationRepository(connection);
            ISupplyOrderUkraineRepository supplyOrderUkraineRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineRepository(connection);
            ISupplyOrderUkraineItemRepository supplyOrderUkraineItemRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineItemRepository(connection);

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            if (message.SupplyOrderUkraine.IsNew()) {
                message.SupplyOrderUkraine.ResponsibleId = user.Id;
                message.SupplyOrderUkraine.SupplierId = message.SupplyOrderUkraine.Supplier.Id;
                message.SupplyOrderUkraine.ClientAgreementId = message.SupplyOrderUkraine.ClientAgreement.Id;
                message.SupplyOrderUkraine.OrganizationId = message.SupplyOrderUkraine.Organization.Id;
                message.SupplyOrderUkraine.IsDirectFromSupplier = true;
                message.SupplyOrderUkraine.FromDate =
                    message.SupplyOrderUkraine.FromDate.Year.Equals(1)
                        ? DateTime.UtcNow
                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrderUkraine.FromDate);

                message.SupplyOrderUkraine.InvDate =
                    message.SupplyOrderUkraine.InvDate.Year.Equals(1)
                        ? DateTime.UtcNow
                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrderUkraine.InvDate);

                SupplyOrderUkraine lastRecord = supplyOrderUkraineRepository.GetLastRecord();

                if (lastRecord != null && DateTime.Now.Year.Equals(lastRecord.Created.Year))
                    message.SupplyOrderUkraine.Number = $"{string.Format("{0:D10}", Convert.ToInt32(lastRecord.Number) + 1)}";
                else
                    message.SupplyOrderUkraine.Number = $"{string.Format("{0:D10}", 1)}";

                message.SupplyOrderUkraine.Id = supplyOrderUkraineRepository.Add(message.SupplyOrderUkraine);
            }

            message.SupplyOrderUkraine = supplyOrderUkraineRepository.GetById(message.SupplyOrderUkraine.Id);

            decimal exchangeRateAmount;

            if (message.SupplyOrderUkraine.ClientAgreement.Agreement.Currency.Code.ToLower().Equals("eur")) {
                exchangeRateAmount = 1m;
            } else {
                GovExchangeRate govExchangeRate =
                    _exchangeRateRepositoriesFactory
                        .NewGovExchangeRateRepository(connection)
                        .GetByCurrencyIdAndCode(
                            message.SupplyOrderUkraine.ClientAgreement.Agreement.Currency.Id,
                            "EUR",
                            message.SupplyOrderUkraine.InvDate
                        );

                if (govExchangeRate != null) {
                    exchangeRateAmount = govExchangeRate.Amount;
                } else {
                    Currency eur = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

                    if (eur != null) {
                        GovCrossExchangeRate govCrossExchangeRate =
                            _exchangeRateRepositoriesFactory
                                .NewGovCrossExchangeRateRepository(connection)
                                .GetByCurrenciesIds(
                                    eur.Id,
                                    message.SupplyOrderUkraine.ClientAgreement.Agreement.Currency.Id,
                                    message.SupplyOrderUkraine.InvDate
                                );

                        exchangeRateAmount = govCrossExchangeRate?.Amount ?? 1m;
                    } else {
                        exchangeRateAmount = 1m;
                    }
                }
            }

            foreach (ParsedProductForUkraine parsedProduct in parsedProducts) {
                Product product = getSingleProductRepository.GetByVendorCodeAndRuleLocaleWithProductGroupAndWriteOffRules(parsedProduct.VendorCode, "pl");

                if (message.SupplyOrderUkraine.SupplyOrderUkraineItems.Any(i => i.ProductId.Equals(product.Id))) {
                    SupplyOrderUkraineItem itemFromList =
                        message
                            .SupplyOrderUkraine
                            .SupplyOrderUkraineItems
                            .First(i => i.ProductId.Equals(product.Id));

                    itemFromList.Qty = parsedProduct.Qty;
                    itemFromList.UnitPrice = parsedProduct.UnitPrice / exchangeRateAmount;
                    itemFromList.UnitPriceLocal = parsedProduct.UnitPrice;
                    itemFromList.GrossUnitPrice = parsedProduct.UnitPrice / exchangeRateAmount;
                    itemFromList.AccountingGrossUnitPrice = parsedProduct.UnitPrice / exchangeRateAmount;
                    itemFromList.GrossUnitPriceLocal = parsedProduct.UnitPrice;
                    itemFromList.ExchangeRateAmount = exchangeRateAmount;
                    itemFromList.NetWeight = parsedProduct.TotalNetWeight;
                    itemFromList.GrossWeight = parsedProduct.TotalGrossWeight;
                    itemFromList.IsUpdated = true;
                    itemFromList.ProductIsImported = parsedProduct.ProductIsImported;

                    if (!string.IsNullOrEmpty(parsedProduct.SpecificationCode)) {
                        if (itemFromList.ProductSpecificationId == null) {
                            itemFromList.ProductSpecificationId = productSpecificationRepository.Add(new ProductSpecification {
                                SpecificationCode = parsedProduct.SpecificationCode,
                                AddedById = user.Id,
                                Updated = DateTime.Now,
                                ProductId = itemFromList.ProductId,
                                Locale = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower()
                            });
                        } else {
                            ProductSpecification productSpecification =
                                productSpecificationRepository.GetById(itemFromList.ProductSpecificationId.Value);

                            if (productSpecification != null && !parsedProduct.SpecificationCode.Equals(productSpecification.SpecificationCode))
                                itemFromList.ProductSpecificationId = productSpecificationRepository.Add(new ProductSpecification {
                                    SpecificationCode = parsedProduct.SpecificationCode,
                                    AddedById = user.Id,
                                    Updated = DateTime.Now,
                                    ProductId = itemFromList.ProductId,
                                    Locale = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower()
                                });
                        }
                    } else {
                        itemFromList.ProductSpecificationId = null;
                    }
                } else {
                    long? productSpecificationId = null;

                    if (!string.IsNullOrEmpty(parsedProduct.SpecificationCode)) {
                        productSpecificationId = productSpecificationRepository.Add(new ProductSpecification {
                            SpecificationCode = parsedProduct.SpecificationCode,
                            AddedById = user.Id,
                            Updated = DateTime.Now,
                            ProductId = product.Id,
                            Locale = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower()
                        });
                    } else {
                        ProductSpecification activeProductSpecification =
                            productSpecificationRepository.GetActiveByProductIdAndLocale(product.Id, CultureInfo.CurrentCulture.TwoLetterISOLanguageName);

                        if (activeProductSpecification != null)
                            productSpecificationId = activeProductSpecification.Id;
                    }

                    message
                        .SupplyOrderUkraine
                        .SupplyOrderUkraineItems
                        .Add(
                            new SupplyOrderUkraineItem {
                                ProductId = product.Id,
                                Qty = parsedProduct.Qty,
                                SupplyOrderUkraineId = message.SupplyOrderUkraine.Id,
                                NetWeight = parsedProduct.TotalNetWeight,
                                GrossWeight = parsedProduct.TotalGrossWeight,
                                UnitPrice = parsedProduct.UnitPrice / exchangeRateAmount,
                                UnitPriceLocal = parsedProduct.UnitPrice,
                                GrossUnitPrice = parsedProduct.UnitPrice / exchangeRateAmount,
                                AccountingGrossUnitPrice = parsedProduct.UnitPrice / exchangeRateAmount,
                                GrossUnitPriceLocal = parsedProduct.UnitPrice,
                                ExchangeRateAmount = exchangeRateAmount,
                                SupplierId = message.SupplyOrderUkraine.SupplierId,
                                ProductSpecificationId = productSpecificationId,
                                ProductIsImported = parsedProduct.ProductIsImported
                            }
                        );
                }
            }

            supplyOrderUkraineItemRepository
                .RemoveAllByOrderUkraineIdExceptProvided(
                    message.SupplyOrderUkraine.Id,
                    message
                        .SupplyOrderUkraine
                        .SupplyOrderUkraineItems
                        .Where(i => !i.IsNew() && i.IsUpdated)
                        .Select(i => i.Id)
                );

            supplyOrderUkraineItemRepository
                .Update(
                    message
                        .SupplyOrderUkraine
                        .SupplyOrderUkraineItems
                        .Where(i => !i.IsNew() && i.IsUpdated)
                );

            foreach (SupplyOrderUkraineItem item in message.SupplyOrderUkraine.SupplyOrderUkraineItems)
                item.Id =
                    supplyOrderUkraineItemRepository
                        .Add(
                            item
                        );

            response.SupplyOrderUkraine =
                supplyOrderUkraineRepository
                    .GetById(
                        message.SupplyOrderUkraine.Id
                    );

            ActReconciliation actReconciliation = new() {
                FromDate = response.SupplyOrderUkraine.FromDate,
                ResponsibleId = user.Id,
                SupplyOrderUkraineId = response.SupplyOrderUkraine.Id,
                Number = response.SupplyOrderUkraine.Number
            };

            actReconciliation.Id = actReconciliationRepository.Add(actReconciliation);

            List<ActReconciliationItem> items = response.SupplyOrderUkraine.SupplyOrderUkraineItems.Select(item => new ActReconciliationItem {
                    ProductId = item.ProductId,
                    ActReconciliationId = actReconciliation.Id,
                    HasDifference = !item.IsFullyPlaced,
                    NegativeDifference = !item.NotOrdered && item.Qty > item.PlacedQty,
                    QtyDifference = item.Qty - item.PlacedQty,
                    ActualQty = item.NotOrdered ? item.Qty : item.PlacedQty,
                    OrderedQty = item.NotOrdered ? 0d : item.Qty,
                    UnitPrice = item.UnitPrice,
                    NetWeight = item.NetWeight,
                    SupplyOrderUkraineItemId = item.Id
                })
                .ToList();

            _supplyUkraineRepositoriesFactory
                .NewActReconciliationItemRepository(connection)
                .Add(
                    items
                );

            response.SupplyOrderUkraine
                .ActReconciliations
                .Add(
                    actReconciliationRepository.GetById(
                        actReconciliation.Id
                    )
                );

            Self.Tell(new UpdateSupplyOrderUkraineItemPriceMessage(
                message.SupplyOrderUkraine.Id
            ));

            Sender.Tell(response);
        } catch (SupplyDocumentParseException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessDeleteSupplyOrderUkraineFromSupplierMessage(DeleteSupplyOrderUkraineFromSupplierMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyOrderUkraineRepository supplyOrderUkraineRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineRepository(connection);

            SupplyOrderUkraine supplyOrderUkraine =
                supplyOrderUkraineRepository
                    .GetByNetId(
                        message.NetId
                    );

            if (supplyOrderUkraine != null) {
                if (supplyOrderUkraine.IsDirectFromSupplier) {
                    if (!supplyOrderUkraine.IsPlaced && !supplyOrderUkraine.DynamicProductPlacementColumns.Any()) {
                        supplyOrderUkraineRepository.Remove(supplyOrderUkraine.Id);

                        Sender.Tell(null);
                    } else {
                        throw new Exception(SupplyOrderUkraineResourceNames.PLACED_ORDER_CAN_NOT_BE_DELETED);
                    }
                } else {
                    throw new Exception(SupplyOrderUkraineResourceNames.ORDER_FROM_TAX_FREE_OR_SAD_CAN_NOT_BE_DELETED);
                }
            } else {
                throw new Exception(SupplyOrderUkraineResourceNames.ORDER_DOES_NOT_EXISTS);
            }
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetAllSupplyOrderUkrainePaymentDeliveryProtocolKeysMessage(GetAllSupplyOrderUkrainePaymentDeliveryProtocolKeysMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyOrderUkrainePaymentDeliveryProtocolKeyRepository supplyOrderUkrainePaymentDeliveryProtocolKeyRepository =
            _supplyUkraineRepositoriesFactory.NewSupplyOrderUkrainePaymentDeliveryProtocolKeyRepository(connection);

        IEnumerable<SupplyOrderUkrainePaymentDeliveryProtocolKey> protocolKeys =
            supplyOrderUkrainePaymentDeliveryProtocolKeyRepository.GetAll();

        if (!protocolKeys.Any()) {
            supplyOrderUkrainePaymentDeliveryProtocolKeyRepository
                .Add(new SupplyOrderUkrainePaymentDeliveryProtocolKey {
                    Key = "������"
                });

            supplyOrderUkrainePaymentDeliveryProtocolKeyRepository
                .Add(new SupplyOrderUkrainePaymentDeliveryProtocolKey {
                    Key = "�����������"
                });

            protocolKeys = supplyOrderUkrainePaymentDeliveryProtocolKeyRepository.GetAll();
        }

        Sender.Tell(protocolKeys);
    }

    private void ProcessAddVatPercentToSupplyOrderUkraine(AddVatPercentToSupplyOrderUkraineMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyOrderUkraineRepository supplyOrderUkraineRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineRepository(connection);

            supplyOrderUkraineRepository.UpdateVatPercent(message.SupplyOrderUkraine.Id, message.SupplyOrderUkraine.VatPercent);

            foreach (SupplyOrderUkraineItem item in message.SupplyOrderUkraine.SupplyOrderUkraineItems) {
                if (message.SupplyOrderUkraine.ClientAgreement != null)
                    message.SupplyOrderUkraine.ClientAgreement.CurrentAmount += Convert.ToDecimal(item.Qty) * item.UnitPriceLocal + item.VatAmountLocal;

                item.VatPercent = message.SupplyOrderUkraine.VatPercent;
                item.VatAmountLocal = Convert.ToDecimal(item.Qty) * item.UnitPriceLocal * message.SupplyOrderUkraine.VatPercent / 100;
                item.VatAmount = Convert.ToDecimal(item.Qty) * item.UnitPrice * message.SupplyOrderUkraine.VatPercent / 100;

                if (message.SupplyOrderUkraine.ClientAgreement != null)
                    message.SupplyOrderUkraine.ClientAgreement.CurrentAmount -= Convert.ToDecimal(item.Qty) * item.UnitPriceLocal + item.VatAmountLocal;
            }

            if (message.SupplyOrderUkraine.ClientAgreement != null)
                _clientRepositoriesFactory
                    .NewClientAgreementRepository(connection)
                    .UpdateAmountByNetId(message.SupplyOrderUkraine.ClientAgreement.NetUid, message.SupplyOrderUkraine.ClientAgreement.CurrentAmount);

            _supplyUkraineRepositoriesFactory
                .NewSupplyOrderUkraineItemRepository(connection)
                .Update(message.SupplyOrderUkraine.SupplyOrderUkraineItems);

            ProcessUpdateSupplyOrderUkraineItemPrice(new UpdateSupplyOrderUkraineItemPriceMessage(
                message.SupplyOrderUkraine.Id
            ));

            Sender.Tell(
                supplyOrderUkraineRepository
                    .GetByNetId(message.SupplyOrderUkraine.NetUid)
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdateSupplyOrderUkraineItem(UpdateSupplyOrderUkraineItemMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyOrderUkraineItemRepository supplyOrderUkraineItemRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineItemRepository(connection);
            IClientAgreementRepository clientAgreementRepository =
                _clientRepositoriesFactory.NewClientAgreementRepository(connection);

            if (!message.SupplyOrderUkraineItems.Any()) {
                Sender.Tell(
                    _supplyUkraineRepositoriesFactory
                        .NewSupplyOrderUkraineRepository(connection)
                        .GetByNetId(message.NetId));
                return;
            }

            ClientAgreement clientAgreement =
                clientAgreementRepository.GetClientAgreementBySupplyOrderUkraineId(message.SupplyOrderUkraineItems.First().SupplyOrderUkraineId);

            foreach (SupplyOrderUkraineItem item in message.SupplyOrderUkraineItems) {
                if (clientAgreement != null)
                    clientAgreement.CurrentAmount += Convert.ToDecimal(item.Qty) * item.UnitPriceLocal + item.VatAmountLocal;

                item.VatAmountLocal = Convert.ToDecimal(item.Qty) * item.UnitPriceLocal *
                    item.VatPercent / 100;
                item.VatAmount = Convert.ToDecimal(item.Qty) * item.UnitPrice *
                    item.VatPercent / 100;

                if (clientAgreement != null)
                    clientAgreement.CurrentAmount -= Convert.ToDecimal(item.Qty) * item.UnitPriceLocal + item.VatAmountLocal;
            }

            if (clientAgreement != null)
                clientAgreementRepository
                    .UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

            supplyOrderUkraineItemRepository.Update(message.SupplyOrderUkraineItems);

            Sender.Tell(
                _supplyUkraineRepositoriesFactory
                    .NewSupplyOrderUkraineRepository(connection)
                    .GetByNetId(message.NetId));

            Self.Tell(new UpdateSupplyOrderUkraineItemPriceMessage(
                message.SupplyOrderUkraineItems.First().SupplyOrderUkraineId
            ));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessManageSupplyOrderUkraineDocument(ManageSupplyOrderUkraineDocumentMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyOrderUkraineDocumentRepository supplyOrderUkraineDocumentRepository =
                _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineDocumentRepository(connection);

            supplyOrderUkraineDocumentRepository.New(
                message.SupplyOrderUkraine.SupplyOrderUkraineDocuments.Where(x => x.IsNew()));

            supplyOrderUkraineDocumentRepository.Remove(
                message.SupplyOrderUkraine.SupplyOrderUkraineDocuments.Where(x => x.Deleted.Equals(true)));

            Sender.Tell(_supplyUkraineRepositoriesFactory
                .NewSupplyOrderUkraineRepository(connection)
                .GetByNetId(message.SupplyOrderUkraine.NetUid));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdateSupplyOrderUkraineItemPrice(UpdateSupplyOrderUkraineItemPriceMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyOrderUkraineRepository supplyOrderUkraineRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineRepository(connection);

        SupplyOrderUkraine fromDb = supplyOrderUkraineRepository.GetById(message.SupplyOrderUkraineId);

        decimal totalNetPrice =
            fromDb
                .SupplyOrderUkraineItems
                .Sum(i => i.UnitPrice * Convert.ToDecimal(i.Qty));

        decimal totalNetPriceLocal =
            fromDb
                .SupplyOrderUkraineItems
                .Sum(i => i.UnitPriceLocal * Convert.ToDecimal(i.Qty));

        decimal deliveryAmountPercent =
            decimal.Round(
                fromDb.ShipmentAmount * 100m / totalNetPrice,
                14,
                MidpointRounding.AwayFromZero
            );

        decimal deliveryAmountLocalPercent =
            decimal.Round(
                fromDb.ShipmentAmountLocal * 100m / totalNetPriceLocal,
                14,
                MidpointRounding.AwayFromZero
            );

        foreach (SupplyOrderUkraineItem item in fromDb.SupplyOrderUkraineItems) {
            item.GrossUnitPrice = item.UnitPrice + item.VatAmount / Convert.ToDecimal(item.Qty);
            item.GrossUnitPriceLocal = item.UnitPriceLocal + item.VatAmountLocal / Convert.ToDecimal(item.Qty);
            item.UnitDeliveryAmount =
                decimal.Round(item.UnitPrice * deliveryAmountPercent / 100m, 14, MidpointRounding.AwayFromZero);
            item.UnitDeliveryAmountLocal =
                decimal.Round(item.UnitPriceLocal * deliveryAmountLocalPercent / 100m, 14, MidpointRounding.AwayFromZero);
            item.AccountingGrossUnitPrice = item.GrossUnitPrice + item.UnitDeliveryAmount;
            item.AccountingGrossUnitPriceLocal = item.GrossUnitPriceLocal + item.UnitDeliveryAmountLocal;
        }

        _supplyUkraineRepositoriesFactory
            .NewSupplyOrderUkraineItemRepository(connection)
            .UpdateGrossPrice(fromDb.SupplyOrderUkraineItems);
    }

    private static void InsertOrUpdateServiceDetailItems(
        IServiceDetailItemRepository serviceDetailItemRepository,
        IServiceDetailItemKeyRepository serviceDetailItemKeyRepository,
        IEnumerable<ServiceDetailItem> serviceDetailItems) {
        foreach (ServiceDetailItem item in serviceDetailItems) {
            if (item.ServiceDetailItemKey != null)
                item.ServiceDetailItemKeyId = item.ServiceDetailItemKey.IsNew() ? serviceDetailItemKeyRepository.Add(item.ServiceDetailItemKey) : item.ServiceDetailItemKey.Id;

            if (item.UnitPrice > 0 && item.Qty > 0) {
                item.NetPrice = decimal.Round(item.UnitPrice * Convert.ToDecimal(item.Qty), 4, MidpointRounding.AwayFromZero);

                item.Vat = decimal.Round(item.NetPrice * Convert.ToDecimal(item.VatPercent) / 100m, 4, MidpointRounding.AwayFromZero);

                item.GrossPrice = decimal.Round(item.NetPrice + item.Vat, 4, MidpointRounding.AwayFromZero);
            } else if (item.GrossPrice > 0 && item.Qty > 0) {
                item.Vat =
                    item.VatPercent > 0
                        ? decimal.Round(item.GrossPrice * 100m / (Convert.ToDecimal(item.VatPercent) + 100m), 3, MidpointRounding.AwayFromZero)
                        : 0m;

                item.NetPrice = decimal.Round(item.GrossPrice - item.Vat, 4, MidpointRounding.AwayFromZero);

                item.UnitPrice = decimal.Round(item.NetPrice / Convert.ToDecimal(item.Qty), 4, MidpointRounding.AwayFromZero);
            }
        }

        if (serviceDetailItems.Any(i => i.IsNew())) serviceDetailItemRepository.Add(serviceDetailItems.Where(i => i.IsNew()));
        if (serviceDetailItems.Any(i => !i.IsNew())) serviceDetailItemRepository.Update(serviceDetailItems.Where(i => !i.IsNew()));
    }

    private static decimal GetExchangeRateUk(
        Currency from,
        Currency to,
        IExchangeRateRepository exchangeRateRepository,
        ICrossExchangeRateRepository crossExchangeRateRepository) {
        if (from.Id.Equals(to.Id))
            return 1m;

        ExchangeRate exchangeRate =
            exchangeRateRepository.GetByCurrencyIdAndCode(to.Id, from.Code);

        if (exchangeRate != null) return exchangeRate.Amount;

        exchangeRate =
            exchangeRateRepository.GetByCurrencyIdAndCode(from.Id, to.Code);

        if (exchangeRate != null) return decimal.Zero - exchangeRate.Amount;

        CrossExchangeRate crossExchangeRate =
            crossExchangeRateRepository.GetByCurrenciesIds(to.Id, from.Id);

        if (crossExchangeRate != null) return decimal.Zero - crossExchangeRate.Amount;

        crossExchangeRate =
            crossExchangeRateRepository.GetByCurrenciesIds(from.Id, to.Id);

        return crossExchangeRate?.Amount ?? 1m;
    }
}