using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.Helpers;
using GBA.Common.Helpers.SupplyOrders;
using GBA.Common.ResourceNames;
using GBA.Domain.AuditEntities;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.Supplies.PackingLists;
using GBA.Domain.Messages.Auditing;
using GBA.Domain.Messages.Supplies;
using GBA.Domain.Messages.Supplies.Documents;
using GBA.Domain.Messages.Supplies.Invoices;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.Documents.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Domain.SignalRMessages;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Supplies;

public sealed class SupplyOrdersActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public SupplyOrdersActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;

        Receive<AddSupplyOrderMessage>(ProcessAddSupplyOrderMessage);

        Receive<AddNewSupplyOrderFromFileMessage>(ProcessAddNewSupplyOrderFromFileMessage);

        Receive<AddAdditionalPaymentsToSupplyOrderMessage>(ProcessAddAdditionalPaymentsToSupplyOrderMessage);

        Receive<UpdateSupplyOrderMessage>(ProcessUpdateSupplyOrderMessage);

        Receive<AddPackingListDocumentsMessage>(ProcessAddPackingListDocumentsMessage);

        Receive<UpdateBillOfLadingDocumentMessage>(ProcessUpdateBillOfLadingDocumentMessage);

        Receive<AddCreditNoteDocumentMessage>(ProcessAddCreditNoteDocumentMessage);

        Receive<AddPolandPaymentDeliveryProtocolDocumentsMessage>(ProcessAddPolandPaymentDeliveryProtocolDocumentsMessage);

        Receive<DeleteSupplyOrderByNetIdMessage>(ProcessDeleteSupplyOrderByNetIdMessage);
    }

    private void ProcessAddSupplyOrderMessage(AddSupplyOrderMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
        ISupplyOrderNumberRepository supplyOrderNumberRepository = _supplyRepositoriesFactory.NewSupplyOrderNumberRepository(connection);
        IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);

        User updatedBy = userRepository.GetByNetIdWithoutIncludes(message.UpdatedByNetId);

        SupplyOrderNumber lastSupplyOrderNumber = _supplyRepositoriesFactory
            .NewSupplyOrderNumberRepository(connection)
            .GetLastRecord();

        SupplyOrderNumber supplyOrderNumber;

        if (lastSupplyOrderNumber != null && DateTime.Now.Year.Equals(lastSupplyOrderNumber.Created.Year))
            supplyOrderNumber = new SupplyOrderNumber {
                Number = $"{string.Format("{0:D11}", Convert.ToInt32(lastSupplyOrderNumber.Number) + 1)}"
            };
        else
            supplyOrderNumber = new SupplyOrderNumber {
                Number = $"{string.Format("{0:D11}", 1)}"
            };

        message.SupplyOrder.SupplyOrderNumberId = supplyOrderNumberRepository.Add(supplyOrderNumber);

        if (message.SupplyOrder.Organization != null) message.SupplyOrder.OrganizationId = message.SupplyOrder.Organization.Id;

        if (message.SupplyOrder.Client != null) message.SupplyOrder.ClientId = message.SupplyOrder.Client.Id;

        long supplyOrderId;

        if (message.SupplyOrder.SupplyOrderItems.Any()) {
            foreach (SupplyOrderItem orderItem in message.SupplyOrder.SupplyOrderItems) {
                if (orderItem.Product != null) orderItem.ProductId = orderItem.Product.Id;

                orderItem.TotalAmount = orderItem.UnitPrice * Convert.ToDecimal(orderItem.Qty);
                message.SupplyOrder.NetPrice += orderItem.TotalAmount;
                message.SupplyOrder.Qty += orderItem.Qty;
            }

            supplyOrderId = supplyOrderRepository.Add(message.SupplyOrder);

            _supplyRepositoriesFactory.NewSupplyOrderItemRepository(connection).Add(
                message.SupplyOrder.SupplyOrderItems.Select(i => {
                    i.SupplyOrderId = supplyOrderId;

                    return i;
                }));
        } else {
            supplyOrderId = supplyOrderRepository.Add(message.SupplyOrder);
        }

        if (message.SupplyOrder.ResponsibilityDeliveryProtocols.Any()) {
            foreach (ResponsibilityDeliveryProtocol protocol in message.SupplyOrder.ResponsibilityDeliveryProtocols) {
                protocol.SupplyOrderId = supplyOrderId;
                if (protocol.User != null) protocol.UserId = updatedBy.Id;
            }

            _supplyRepositoriesFactory.NewResponsibilityDeliveryProtocolRepository(connection).Add(message.SupplyOrder.ResponsibilityDeliveryProtocols);
        }

        List<SupplyDeliveryDocument> supplyDeliveryDocumentsFromDb =
            _supplyRepositoriesFactory.NewSupplyDeliveryDocumentRepository(connection).GetAllByType(message.SupplyOrder.TransportationType);

        if (supplyDeliveryDocumentsFromDb != null && supplyDeliveryDocumentsFromDb.Any()) {
            List<SupplyOrderDeliveryDocument> supplyOrderDeliveryDocumentsToAdd = new();

            foreach (SupplyDeliveryDocument document in supplyDeliveryDocumentsFromDb)
                supplyOrderDeliveryDocumentsToAdd.Add(new SupplyOrderDeliveryDocument {
                    SupplyOrderId = supplyOrderId,
                    SupplyDeliveryDocumentId = document.Id,
                    UserId = updatedBy.Id
                });

            _supplyRepositoriesFactory.NewSupplyOrderDeliveryDocumentRepository(connection).Add(supplyOrderDeliveryDocumentsToAdd);
        }

        InformationMessage informationMessage = new() {
            CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
            Title = $"���� ���������� � {supplyOrderNumber.Number}",
            Message = $"���������� ���� ����������. ������������ {message.SupplyOrder.Client?.Brand ?? string.Empty}. ���� ���������� {message.SupplyOrder.GrossPrice}",
            Amount = $"�� ���� {message.SupplyOrder.NetPrice}"
        };
        SupplyOrder supplyOrderToReturn = supplyOrderRepository.GetById(supplyOrderId);

        Sender.Tell(new Tuple<SupplyOrder, InformationMessage>(supplyOrderToReturn, informationMessage));

        List<AuditEntityProperty> newProperties = new() {
            new AuditEntityProperty {
                Type = AuditEntityPropertyType.New,
                Name = "Client",
                Value = supplyOrderToReturn.Client.FullName
            },
            new AuditEntityProperty {
                Type = AuditEntityPropertyType.New,
                Name = "SupplyOrderNumber",
                Value = supplyOrderToReturn.SupplyOrderNumber.Number
            },
            new AuditEntityProperty {
                Type = AuditEntityPropertyType.New,
                Name = "Organization",
                Value = supplyOrderToReturn.Organization.Name
            }
        };

        ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(new RetrieveAndStoreAuditDataMessage(
            message.UpdatedByNetId,
            supplyOrderToReturn.NetUid,
            "SupplyOrder",
            supplyOrderToReturn,
            null,
            newProperties
        ));
    }

    private void ProcessAddNewSupplyOrderFromFileMessage(AddNewSupplyOrderFromFileMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
            ISupplyOrderNumberRepository supplyOrderNumberRepository = _supplyRepositoriesFactory.NewSupplyOrderNumberRepository(connection);

            AddSupplyOrderFromFileResponse response = new();

            if (message.SupplyOrder.Organization != null && !message.SupplyOrder.Organization.IsNew())
                message.SupplyOrder.OrganizationId = message.SupplyOrder.Organization.Id;
            else
                throw new Exception(SupplyOrderResourceNames.UNSPECIFIED_ORGANIZATION);

            if (message.SupplyOrder.Client != null && !message.SupplyOrder.Client.IsNew())
                message.SupplyOrder.ClientId = message.SupplyOrder.Client.Id;
            else
                throw new Exception(SupplyOrderResourceNames.UNSPECIFIED_SUPPLIER);

            if (message.SupplyOrder.ClientAgreement != null && !message.SupplyOrder.ClientAgreement.IsNew())
                message.SupplyOrder.ClientAgreementId = message.SupplyOrder.ClientAgreement.Id;
            else
                throw new Exception(SupplyOrderResourceNames.UNSPECIFIED_SUPPLIER_AGREEMENT);

            User updatedBy = userRepository.GetByNetId(message.UserNetId);
            if (updatedBy != null) message.SupplyOrder.ResponsibleId = updatedBy.Id;
            SupplyOrderNumber lastSupplyOrderNumber =
                _supplyRepositoriesFactory
                    .NewSupplyOrderNumberRepository(connection)
                    .GetLastRecord();

            SupplyOrderNumber supplyOrderNumber;

            if (lastSupplyOrderNumber != null && DateTime.Now.Year.Equals(lastSupplyOrderNumber.Created.Year))
                supplyOrderNumber = new SupplyOrderNumber {
                    Number = $"{string.Format("{0:D11}", Convert.ToInt32(lastSupplyOrderNumber.Number) + 1)}"
                };
            else
                supplyOrderNumber = new SupplyOrderNumber {
                    Number = $"{string.Format("{0:D11}", 1)}"
                };

            message.SupplyOrder.SupplyOrderNumberId = supplyOrderNumberRepository.Add(supplyOrderNumber);

            List<ParsedProduct> parsedProducts =
                _xlsFactoryManager
                    .NewParseConfigurationXlsManager()
                    .GetProductsFromSupplyDocumentsByConfiguration(message.PathToFile, message.ParseConfiguration);

            message.SupplyOrder.NetPrice = decimal.Zero;
            message.SupplyOrder.Qty = 0d;
            message.SupplyOrder.DateFrom =
                !message.SupplyOrder.DateFrom.HasValue
                    ? DateTime.UtcNow
                    : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.DateFrom.Value);

            foreach (ParsedProduct parsedProduct in parsedProducts) {
                Product product = getSingleProductRepository.GetProductByVendorCode(parsedProduct.VendorCode);

                if (product == null) {
                    response.MissingVendorCodes.Add(parsedProduct.VendorCode);
                } else {
                    message
                        .SupplyOrder
                        .SupplyOrderItems
                        .Add(new SupplyOrderItem {
                            Qty = parsedProduct.Qty,
                            NetWeight = parsedProduct.NetWeight,
                            GrossWeight = parsedProduct.GrossWeight,
                            UnitPrice = parsedProduct.UnitPrice,
                            TotalAmount = decimal.Round(parsedProduct.UnitPrice * Convert.ToDecimal(parsedProduct.Qty), 2, MidpointRounding.AwayFromZero),
                            ProductId = product.Id
                        });

                    message.SupplyOrder.NetPrice =
                        decimal.Round(
                            message.SupplyOrder.NetPrice + decimal.Round(parsedProduct.UnitPrice * Convert.ToDecimal(parsedProduct.Qty), 2,
                                MidpointRounding.AwayFromZero), 2, MidpointRounding.AwayFromZero);
                    message.SupplyOrder.Qty =
                        Math.Round(message.SupplyOrder.Qty + parsedProduct.Qty, 2, MidpointRounding.AwayFromZero);
                }
            }

            if (response.HasError) {
                response.MissingVendorCodesFileUrl =
                    _xlsFactoryManager
                        .NewProductsXlsManager()
                        .ExportMissingVendorCodes(
                            message.TempDataFolder,
                            response.MissingVendorCodes
                        );

                Sender.Tell(response);

                return;
            }

            message.SupplyOrder.Id = supplyOrderRepository.Add(message.SupplyOrder);

            _supplyRepositoriesFactory
                .NewSupplyOrderItemRepository(connection)
                .Add(
                    message.SupplyOrder.SupplyOrderItems.Select(i => {
                        i.SupplyOrderId = message.SupplyOrder.Id;

                        return i;
                    })
                );

            if (!message.SupplyOrder.ResponsibilityDeliveryProtocols.Any()) {
                message
                    .SupplyOrder
                    .ResponsibilityDeliveryProtocols
                    .Add(new ResponsibilityDeliveryProtocol {
                        SupplyOrderId = message.SupplyOrder.Id,
                        UserId = updatedBy.Id,
                        SupplyOrderStatus = SupplyOrderStatus.Proform,
                        Created = DateTime.UtcNow
                    });

                message
                    .SupplyOrder
                    .ResponsibilityDeliveryProtocols
                    .Add(new ResponsibilityDeliveryProtocol {
                        SupplyOrderId = message.SupplyOrder.Id,
                        UserId = updatedBy.Id,
                        SupplyOrderStatus = SupplyOrderStatus.Invoice,
                        Created = DateTime.UtcNow
                    });
            }

            _supplyRepositoriesFactory
                .NewResponsibilityDeliveryProtocolRepository(connection)
                .Add(
                    message.SupplyOrder.ResponsibilityDeliveryProtocols
                );

            List<SupplyDeliveryDocument> supplyDeliveryDocumentsFromDb =
                _supplyRepositoriesFactory
                    .NewSupplyDeliveryDocumentRepository(connection)
                    .GetAllByType(message.SupplyOrder.TransportationType);

            if (supplyDeliveryDocumentsFromDb != null && supplyDeliveryDocumentsFromDb.Any()) {
                List<SupplyOrderDeliveryDocument> supplyOrderDeliveryDocumentsToAdd =
                    supplyDeliveryDocumentsFromDb
                        .Select(document => new SupplyOrderDeliveryDocument {
                            SupplyOrderId = message.SupplyOrder.Id,
                            SupplyDeliveryDocumentId = document.Id,
                            UserId = updatedBy.Id
                        }).ToList();

                _supplyRepositoriesFactory
                    .NewSupplyOrderDeliveryDocumentRepository(connection)
                    .Add(supplyOrderDeliveryDocumentsToAdd);
            }

            InformationMessage informationMessage = new() {
                CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                Title = $"���� ���������� � {supplyOrderNumber.Number}",
                Message = $"���������� ���� ����������. ������������ {message.SupplyOrder.Client.Brand}. ���� ���������� {message.SupplyOrder.GrossPrice}",
                Amount = $"�� ���� {message.SupplyOrder.NetPrice}"
            };

            SupplyOrder supplyOrderToReturn = supplyOrderRepository.GetById(message.SupplyOrder.Id);

            response.SupplyOrder = supplyOrderToReturn;
            response.InformationMessage = informationMessage;

            Sender.Tell(response);

            List<AuditEntityProperty> newProperties = new() {
                new AuditEntityProperty {
                    Type = AuditEntityPropertyType.New,
                    Name = "Client",
                    Value = supplyOrderToReturn.Client.FullName
                },
                new AuditEntityProperty {
                    Type = AuditEntityPropertyType.New,
                    Name = "SupplyOrderNumber",
                    Value = supplyOrderToReturn.SupplyOrderNumber.Number
                },
                new AuditEntityProperty {
                    Type = AuditEntityPropertyType.New,
                    Name = "Organization",
                    Value = supplyOrderToReturn.Organization.Name
                }
            };

            ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(new RetrieveAndStoreAuditDataMessage(
                message.UserNetId,
                supplyOrderToReturn.NetUid,
                "SupplyOrder",
                supplyOrderToReturn,
                null,
                newProperties
            ));
        } catch (SupplyDocumentParseException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessAddAdditionalPaymentsToSupplyOrderMessage(AddAdditionalPaymentsToSupplyOrderMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(connection);
            ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);

            Currency additionalPaymentCurrency = currencyRepository.GetByNetId(message.CurrencyNetId);

            if (additionalPaymentCurrency == null) throw new Exception(SupplyOrderResourceNames.CURRENCY_DOES_NOT_EXISTS);

            SupplyOrder supplyOrder = supplyOrderRepository.GetByNetId(message.SupplyOrderNetId);

            if (supplyOrder == null) {
                ISupplyOrderUkraineRepository supplyOrderUkraineRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineRepository(connection);

                SupplyOrderUkraine supplyOrderUkraine = supplyOrderUkraineRepository.GetByNetId(message.SupplyOrderNetId);

                if (supplyOrderUkraine == null) throw new Exception(SupplyOrderResourceNames.ORDER_DOES_NOT_EXISTS);

                supplyOrderUkraine.AdditionalAmount = message.AdditionalAmount;
                supplyOrderUkraine.AdditionalPercent = message.AdditionalPercent;
                supplyOrderUkraine.AdditionalPaymentFromDate = message.FromDate;

                supplyOrderUkraine.AdditionalPaymentCurrencyId = additionalPaymentCurrency.Id;

                supplyOrderUkraineRepository.UpdateAdditionalPaymentFields(supplyOrderUkraine);

                if (supplyOrderUkraine.SupplyOrderUkraineItems.Any()) {
                    supplyOrderUkraine = supplyOrderUkraineRepository.GetById(supplyOrderUkraine.Id);

                    if (supplyOrderUkraine.ShipmentAmount > 0m || supplyOrderUkraine.AdditionalAmount > 0m || supplyOrderUkraine.AdditionalPercent > 0) {
                        IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
                        ICrossExchangeRateRepository crossExchangeRateRepository = _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection);

                        decimal totalNetPrice =
                            decimal.Round(
                                supplyOrderUkraine
                                    .SupplyOrderUkraineItems
                                    .Where(i => !i.NotOrdered)
                                    .Sum(i => decimal.Round(i.UnitPrice * Convert.ToDecimal(i.Qty), 2, MidpointRounding.AwayFromZero)),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        Currency eur = currencyRepository.GetEURCurrencyIfExists();

                        decimal exchangeRateAmount =
                            GetExchangeRateUk(
                                supplyOrderUkraine.ClientAgreement.Agreement.Currency,
                                eur,
                                exchangeRateRepository,
                                crossExchangeRateRepository
                            );

                        decimal shipmentAmount =
                            exchangeRateAmount < 0
                                ? decimal.Round(
                                    supplyOrderUkraine.ShipmentAmount / (0 - exchangeRateAmount),
                                    2,
                                    MidpointRounding.AwayFromZero
                                )
                                : decimal.Round(
                                    supplyOrderUkraine.ShipmentAmount / exchangeRateAmount,
                                    2,
                                    MidpointRounding.AwayFromZero
                                );

                        if (supplyOrderUkraine.AdditionalPaymentCurrency != null) {
                            exchangeRateAmount =
                                GetExchangeRateUk(
                                    supplyOrderUkraine.AdditionalPaymentCurrency,
                                    eur,
                                    exchangeRateRepository,
                                    crossExchangeRateRepository
                                );

                            shipmentAmount =
                                exchangeRateAmount < 0
                                    ? decimal.Round(
                                        shipmentAmount + supplyOrderUkraine.AdditionalAmount / (0 - exchangeRateAmount),
                                        2,
                                        MidpointRounding.AwayFromZero
                                    )
                                    : decimal.Round(
                                        shipmentAmount + supplyOrderUkraine.AdditionalAmount / exchangeRateAmount,
                                        2,
                                        MidpointRounding.AwayFromZero
                                    );
                        }

                        foreach (MergedService service in supplyOrderUkraine.MergedServices) {
                            exchangeRateAmount =
                                GetExchangeRateUk(
                                    service.SupplyOrganizationAgreement.Currency,
                                    eur,
                                    exchangeRateRepository,
                                    crossExchangeRateRepository
                                );

                            shipmentAmount =
                                exchangeRateAmount < 0
                                    ? decimal.Round(
                                        shipmentAmount + decimal.Round(service.GrossPrice / (0 - exchangeRateAmount), 2, MidpointRounding.AwayFromZero),
                                        2,
                                        MidpointRounding.AwayFromZero
                                    )
                                    : decimal.Round(
                                        shipmentAmount + decimal.Round(service.GrossPrice * exchangeRateAmount, 2, MidpointRounding.AwayFromZero),
                                        2,
                                        MidpointRounding.AwayFromZero
                                    );
                        }

                        decimal grossPercent =
                            decimal.Round(
                                shipmentAmount * 100m / totalNetPrice,
                                14,
                                MidpointRounding.AwayFromZero
                            );

                        grossPercent =
                            decimal.Round(
                                grossPercent + Convert.ToDecimal(supplyOrderUkraine.AdditionalPercent),
                                14,
                                MidpointRounding.AwayFromZero
                            );

                        _supplyUkraineRepositoriesFactory
                            .NewSupplyOrderUkraineItemRepository(connection)
                            .UpdateWeightAndPrice(
                                supplyOrderUkraine
                                    .SupplyOrderUkraineItems
                                    .Where(i => !i.NotOrdered)
                                    .Select(item => {
                                        item.GrossUnitPrice =
                                            decimal.Round(
                                                item.UnitPrice +
                                                decimal.Round(item.UnitPrice * grossPercent / 100m, 10, MidpointRounding.AwayFromZero),
                                                14,
                                                MidpointRounding.AwayFromZero
                                            );

                                        item.GrossUnitPriceLocal =
                                            decimal.Round(
                                                item.UnitPriceLocal +
                                                decimal.Round(item.UnitPriceLocal * grossPercent / 100m, 10, MidpointRounding.AwayFromZero),
                                                14,
                                                MidpointRounding.AwayFromZero
                                            );

                                        return item;
                                    })
                            );
                    }
                }

                Sender.Tell(supplyOrderUkraineRepository.GetById(supplyOrderUkraine.Id));
            } else {
                supplyOrder.AdditionalAmount = message.AdditionalAmount;
                supplyOrder.AdditionalPercent = message.AdditionalPercent;
                supplyOrder.AdditionalPaymentFromDate = message.FromDate;

                supplyOrder.AdditionalPaymentCurrencyId = additionalPaymentCurrency.Id;

                supplyOrderRepository.UpdateAdditionalPaymentFields(supplyOrder);

                Sender.Tell(supplyOrderRepository.GetByNetId(message.SupplyOrderNetId));

                ActorReferenceManager.Instance.Get(SupplyActorNames.SUPPLY_INVOICE_ACTOR).Tell(new UpdateSupplyInvoiceItemGrossPriceMessage(
                    _supplyRepositoriesFactory
                        .NewSupplyInvoiceRepository(connection)
                        .GetBySupplyOrderId(supplyOrder.Id).Select(x => x.Id),
                    message.UserNetId
                ));
            }
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdateSupplyOrderMessage(UpdateSupplyOrderMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            IPackingListRepository packingListRepository = _supplyRepositoriesFactory.NewPackingListRepository(connection);
            ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
            ISupplyInvoiceRepository supplyInvoiceRepository = _supplyRepositoriesFactory.NewSupplyInvoiceRepository(connection);
            ISupplyProFormRepository supplyProFormRepository = _supplyRepositoriesFactory.NewSupplyProFormRepository(connection);
            ICustomServiceRepository customServiceRepository = _supplyRepositoriesFactory.NewCustomServiceRepository(connection);
            IMergedServiceRepository mergedServiceRepository = _supplyRepositoriesFactory.NewMergedServiceRepository(connection);
            IInvoiceDocumentRepository invoiceDocumentRepository = _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection);
            IProFormDocumentRepository proFormDocumentRepository = _supplyRepositoriesFactory.NewProFormDocumentRepository(connection);
            IContainerServiceRepository containerServiceRepository = _supplyRepositoriesFactory.NewContainerServiceRepository(connection);
            ISupplyPaymentTaskRepository supplyPaymentTaskRepository = _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);
            IServiceDetailItemRepository serviceDetailItemRepository = _supplyRepositoriesFactory.NewServiceDetailItemRepository(connection);
            ISupplyServiceNumberRepository supplyServiceNumberRepository = _supplyRepositoriesFactory.NewSupplyServiceNumberRepository(connection);
            IBillOfLadingDocumentRepository billOfLadingDocumentRepository = _supplyRepositoriesFactory.NewBillOfLadingDocumentRepository(connection);
            IServiceDetailItemKeyRepository serviceDetailItemKeyRepository = _supplyRepositoriesFactory.NewServiceDetailItemKeyRepository(connection);
            ISupplyOrderDeliveryDocumentRepository supplyOrderDeliveryDocumentRepository =
                _supplyRepositoriesFactory.NewSupplyOrderDeliveryDocumentRepository(connection);
            ISupplyOrderContainerServiceRepository supplyOrderContainerServiceRepository =
                _supplyRepositoriesFactory.NewSupplyOrderContainerServiceRepository(connection);
            ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
                _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);
            ISupplyInformationDeliveryProtocolRepository supplyInformationDeliveryProtocolRepository =
                _supplyRepositoriesFactory.NewSupplyInformationDeliveryProtocolRepository(connection);
            ISupplyOrderPaymentDeliveryProtocolRepository supplyOrderPaymentDeliveryProtocolRepository =
                _supplyRepositoriesFactory.NewSupplyOrderPaymentDeliveryProtocolRepository(connection);
            ISupplyInformationDeliveryProtocolKeyRepository supplyInformationDeliveryProtocolKeyRepository =
                _supplyRepositoriesFactory.NewSupplyInformationDeliveryProtocolKeyRepository(connection);
            ISupplyOrderPaymentDeliveryProtocolKeyRepository supplyOrderPaymentDeliveryProtocolKeyRepository =
                _supplyRepositoriesFactory.NewSupplyOrderPaymentDeliveryProtocolKeyRepository(connection);
            ISupplyOrderPolandPaymentDeliveryProtocolRepository supplyOrderPolandPaymentDeliveryProtocolRepository =
                _supplyRepositoriesFactory.NewSupplyOrderPolandPaymentDeliveryProtocolRepository(connection);
            IVehicleServiceRepository vehicleServiceRepository =
                _supplyRepositoriesFactory.NewVehicleServiceRepository(connection);
            ISupplyOrderVehicleServiceRepository supplyOrderVehicleServiceRepository =
                _supplyRepositoriesFactory.NewSupplyOrderVehicleServiceRepository(connection);
            ISupplyInformationTaskRepository supplyInformationTaskRepository =
                _supplyRepositoriesFactory.NewSupplyInformationTaskRepository(connection);
            IPackingListPackageOrderItemSupplyServiceRepository packingListPackageOrderItemSupplyServiceRepository =
                _supplyRepositoriesFactory.NewPackingListPackageOrderItemSupplyServiceRepository(connection);
            IActProvidingServiceDocumentRepository actProvidingServiceDocumentRepository =
                _supplyRepositoriesFactory.NewActProvidingServiceDocumentRepository(connection);
            ISupplyServiceAccountDocumentRepository supplyServiceAccountDocumentRepository =
                _supplyRepositoriesFactory.NewSupplyServiceAccountDocumentRepository(connection);

            List<PaymentTaskMessage> messagesToSend = new();

            InformationMessage informationMessage = null;

            User updatedBy = userRepository.GetByNetId(message.UpdatedByNetId);

            if (message.SupplyOrder.IsOrderArrived)
                if (!message.SupplyOrder.PlaneArrived.HasValue && !message.SupplyOrder.VechicalArrived.HasValue && !message.SupplyOrder.ShipArrived.HasValue)
                    message.SupplyOrder.PlaneArrived =
                        message.SupplyOrder.VechicalArrived =
                            message.SupplyOrder.ShipArrived = DateTime.UtcNow;

            if (message.SupplyOrder.SupplyProForm != null) {
                if (message.SupplyOrder.SupplyProForm.IsNew()) {
                    SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                    if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                        message.SupplyOrder.SupplyProForm.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                    else
                        message.SupplyOrder.SupplyProForm.ServiceNumber = string.Format("P{0:D10}", 1);

                    if (message.SupplyOrder.SupplyProForm.DateFrom.HasValue)
                        message.SupplyOrder.SupplyProForm.DateFrom = TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.SupplyProForm.DateFrom.Value);

                    supplyServiceNumberRepository.Add(message.SupplyOrder.SupplyProForm.ServiceNumber);

                    message.SupplyOrder.SupplyProForm.Id = supplyProFormRepository.Add(message.SupplyOrder.SupplyProForm);

                    List<SupplyInformationDeliveryProtocolKey> defaultInformationKeys =
                        supplyInformationDeliveryProtocolKeyRepository.GetAllDefaultByDestination(KeyAssignedTo.SupplyProForm);

                    if (defaultInformationKeys.Any()) {
                        List<SupplyInformationDeliveryProtocol> informationProtocolsToAdd = new();

                        foreach (SupplyInformationDeliveryProtocolKey key in defaultInformationKeys)
                            informationProtocolsToAdd.Add(new SupplyInformationDeliveryProtocol {
                                SupplyInformationDeliveryProtocolKeyId = key.Id,
                                SupplyProFormId = message.SupplyOrder.SupplyProForm.Id,
                                UserId = updatedBy.Id,
                                Created = message.SupplyOrder.SupplyProForm.DateFrom ?? DateTime.UtcNow,
                                IsDefault = true
                            });

                        supplyInformationDeliveryProtocolRepository.Add(informationProtocolsToAdd);
                    }

                    informationMessage = new InformationMessage {
                        CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                        Title = $"���� �������� � {message.SupplyOrder.SupplyProForm.Number}",
                        Message = $"������ �������� �� ���������� {message.SupplyOrder.SupplyOrderNumber.Number}.",
                        Amount = $"�� ���� {message.SupplyOrder.SupplyProForm.NetPrice}"
                    };

                    message.SupplyOrder.SupplyProFormId = message.SupplyOrder.SupplyProForm.Id;
                } else {
                    if (message.SupplyOrder.SupplyProForm.DateFrom.HasValue)
                        message.SupplyOrder.SupplyProForm.DateFrom = TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.SupplyProForm.DateFrom.Value);

                    supplyProFormRepository.Update(message.SupplyOrder.SupplyProForm);
                }

                if (message.SupplyOrder.SupplyProForm.ProFormDocuments.Any()) {
                    proFormDocumentRepository.RemoveAllByProFormIdExceptProvided(
                        message.SupplyOrder.SupplyProForm.Id,
                        message.SupplyOrder.SupplyProForm.ProFormDocuments.Where(d => !d.IsNew()).Select(d => d.Id)
                    );

                    proFormDocumentRepository.Add(message.SupplyOrder.SupplyProForm.ProFormDocuments
                        .Where(d => d.IsNew())
                        .Select(d => {
                            d.SupplyProFormId = message.SupplyOrder.SupplyProForm.Id;

                            return d;
                        }));
                    proFormDocumentRepository.Update(message.SupplyOrder.SupplyProForm.ProFormDocuments.Where(d => !d.IsNew()));
                } else {
                    proFormDocumentRepository.RemoveAllByProFormId(message.SupplyOrder.SupplyProForm.Id);
                }

                if (message.SupplyOrder.SupplyProForm.PaymentDeliveryProtocols.Any(p => p.IsNew()))
                    supplyOrderPaymentDeliveryProtocolRepository.Add(
                        message.SupplyOrder.SupplyProForm.PaymentDeliveryProtocols
                            .Where(p => p.IsNew())
                            .Select(p => {
                                if (p.SupplyPaymentTask != null) {
                                    p.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                                    p.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.PaymentDeliveryProtocol;
                                    p.SupplyPaymentTask.UserId = p.SupplyPaymentTask.User.Id;

                                    p.SupplyPaymentTask.IsAccounting = p.IsAccounting;

                                    p.SupplyPaymentTask.PayToDate =
                                        !p.SupplyPaymentTask.PayToDate.HasValue
                                            ? DateTime.UtcNow
                                            : TimeZoneInfo.ConvertTimeToUtc(p.SupplyPaymentTask.PayToDate.Value);

                                    p.SupplyPaymentTask.NetPrice = p.Value;
                                    p.SupplyPaymentTask.GrossPrice = p.Value;

                                    p.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(p.SupplyPaymentTask);

                                    messagesToSend.Add(new PaymentTaskMessage {
                                        Amount = p.Value,
                                        Discount = p.Discount,
                                        CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                        PayToDate = p.SupplyPaymentTask.PayToDate,
                                        OrganisationName = $"�������� {message.SupplyOrder.SupplyProForm.Number}",
                                        PaymentForm = p.SupplyOrderPaymentDeliveryProtocolKey.Key
                                    });
                                }

                                if (p.SupplyOrderPaymentDeliveryProtocolKey != null)
                                    p.SupplyOrderPaymentDeliveryProtocolKeyId = p.SupplyOrderPaymentDeliveryProtocolKey.IsNew()
                                        ? supplyOrderPaymentDeliveryProtocolKeyRepository.Add(p.SupplyOrderPaymentDeliveryProtocolKey)
                                        : p.SupplyOrderPaymentDeliveryProtocolKey.Id;

                                p.UserId = updatedBy.Id;
                                p.SupplyProFormId = message.SupplyOrder.SupplyProForm.Id;

                                return p;
                            })
                    );

                if (message.SupplyOrder.SupplyProForm.PaymentDeliveryProtocols.Any(p => !p.IsNew() && p.SupplyPaymentTask != null)) {
                    foreach (SupplyOrderPaymentDeliveryProtocol protocol in message
                                 .SupplyOrder
                                 .SupplyProForm
                                 .PaymentDeliveryProtocols
                                 .Where(p => !p.IsNew() && p.SupplyPaymentTask != null && p.SupplyPaymentTask.IsNew())) {
                        protocol.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                        protocol.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.PaymentDeliveryProtocol;
                        protocol.SupplyPaymentTask.UserId = protocol.SupplyPaymentTask.User.Id;

                        protocol.SupplyPaymentTask.IsAccounting = protocol.IsAccounting;

                        protocol.SupplyPaymentTask.PayToDate =
                            !protocol.SupplyPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(protocol.SupplyPaymentTask.PayToDate.Value);

                        protocol.SupplyPaymentTask.NetPrice = protocol.Value;
                        protocol.SupplyPaymentTask.GrossPrice = protocol.Value;

                        protocol.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(protocol.SupplyPaymentTask);

                        supplyOrderPaymentDeliveryProtocolRepository.Update(protocol);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = protocol.Value,
                            Discount = protocol.Discount,
                            CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                            PayToDate = protocol.SupplyPaymentTask.PayToDate,
                            OrganisationName = $"�������� {message.SupplyOrder.SupplyProForm.Number}",
                            PaymentForm = protocol.SupplyOrderPaymentDeliveryProtocolKey.Key
                        });
                    }

                    foreach (SupplyOrderPaymentDeliveryProtocol protocol in message
                                 .SupplyOrder
                                 .SupplyProForm
                                 .PaymentDeliveryProtocols
                                 .Where(p => !p.IsNew() && p.SupplyPaymentTask != null && !p.SupplyPaymentTask.IsNew()))
                        if (protocol.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone) && !protocol.SupplyPaymentTask.IsAvailableForPayment) {
                            if (protocol.SupplyPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(protocol.SupplyPaymentTask.Id, updatedBy.Id);

                                supplyOrderPaymentDeliveryProtocolRepository.Remove(protocol.NetUid);
                            } else {
                                protocol.SupplyPaymentTask.NetPrice = protocol.Value;
                                protocol.SupplyPaymentTask.GrossPrice = protocol.Value;
                                protocol.SupplyPaymentTask.UpdatedById = updatedBy.Id;

                                protocol.SupplyPaymentTask.IsAccounting = protocol.IsAccounting;

                                protocol.SupplyPaymentTask.PayToDate =
                                    !protocol.SupplyPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(protocol.SupplyPaymentTask.PayToDate.Value);

                                supplyPaymentTaskRepository.Update(protocol.SupplyPaymentTask);
                            }
                        }
                }

                if (message.SupplyOrder.SupplyProForm.InformationDeliveryProtocols.Any()) {
                    supplyInformationDeliveryProtocolRepository
                        .Add(
                            message.SupplyOrder.SupplyProForm.InformationDeliveryProtocols
                                .Where(p => p.IsNew())
                                .Select(p => {
                                    if (p.SupplyInformationDeliveryProtocolKey != null) {
                                        if (p.SupplyInformationDeliveryProtocolKey.IsNew()) {
                                            p.SupplyInformationDeliveryProtocolKey.KeyAssignedTo = KeyAssignedTo.SupplyProForm;
                                            p.SupplyInformationDeliveryProtocolKey.TransportationType = message.SupplyOrder.TransportationType;

                                            //ToDo: Insert Translation
                                            p.SupplyInformationDeliveryProtocolKeyId =
                                                supplyInformationDeliveryProtocolKeyRepository.Add(p.SupplyInformationDeliveryProtocolKey);
                                        } else {
                                            p.SupplyInformationDeliveryProtocolKeyId = p.SupplyInformationDeliveryProtocolKey.Id;
                                        }
                                    }

                                    p.SupplyProFormId = message.SupplyOrder.SupplyProForm.Id;
                                    p.UserId = updatedBy.Id;

                                    p.Created = message.SupplyOrder.SupplyProForm.DateFrom ?? DateTime.UtcNow;

                                    return p;
                                })
                        );
                    supplyInformationDeliveryProtocolRepository
                        .Update(
                            message.SupplyOrder.SupplyProForm.InformationDeliveryProtocols
                                .Where(p => !p.IsNew())
                                .Select(p => {
                                    p.Created = message.SupplyOrder.SupplyProForm.DateFrom ?? DateTime.UtcNow;

                                    return p;
                                })
                        );
                }
            }

            if (message.SupplyOrder.SupplyInvoices.Any()) {
                foreach (SupplyInvoice invoice in message.SupplyOrder.SupplyInvoices.Where(i => i.IsNew() && !string.IsNullOrEmpty(i.Number))) {
                    invoice.SupplyOrderId = message.SupplyOrder.Id;

                    SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                    if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                        invoice.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                    else
                        invoice.ServiceNumber = string.Format("P{0:D10}", 1);

                    supplyServiceNumberRepository.Add(invoice.ServiceNumber);

                    if (invoice.DateFrom.HasValue) invoice.DateFrom = TimeZoneInfo.ConvertTimeToUtc(invoice.DateFrom.Value);

                    if (invoice.PaymentTo.HasValue) invoice.PaymentTo = TimeZoneInfo.ConvertTimeToUtc(invoice.PaymentTo.Value);

                    invoice.Id = supplyInvoiceRepository.Add(invoice);

                    if (invoice.InvoiceDocuments.Any())
                        invoiceDocumentRepository.Add(invoice.InvoiceDocuments.Select(d => {
                            d.SupplyInvoiceId = invoice.Id;

                            return d;
                        }));

                    if (invoice.PaymentDeliveryProtocols.Any())
                        supplyOrderPaymentDeliveryProtocolRepository.Add(
                            invoice.PaymentDeliveryProtocols
                                .Select(p => {
                                    if (p.SupplyPaymentTask != null) {
                                        p.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                                        p.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.PaymentDeliveryProtocol;
                                        p.SupplyPaymentTask.UserId = p.SupplyPaymentTask.User.Id;

                                        p.SupplyPaymentTask.PayToDate = p.SupplyPaymentTask.PayToDate ?? DateTime.UtcNow;

                                        p.SupplyPaymentTask.IsAccounting = p.IsAccounting;

                                        p.SupplyPaymentTask.NetPrice = p.Value;
                                        p.SupplyPaymentTask.GrossPrice = p.Value;

                                        p.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(p.SupplyPaymentTask);

                                        messagesToSend.Add(new PaymentTaskMessage {
                                            Amount = p.Value,
                                            Discount = p.Discount,
                                            CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                            PayToDate = p.SupplyPaymentTask.PayToDate,
                                            OrganisationName = $"������ {invoice.Number}",
                                            PaymentForm = p.SupplyOrderPaymentDeliveryProtocolKey.Key
                                        });
                                    }

                                    if (p.SupplyOrderPaymentDeliveryProtocolKey != null)
                                        p.SupplyOrderPaymentDeliveryProtocolKeyId = p.SupplyOrderPaymentDeliveryProtocolKey.IsNew()
                                            ? supplyOrderPaymentDeliveryProtocolKeyRepository.Add(p.SupplyOrderPaymentDeliveryProtocolKey)
                                            : p.SupplyOrderPaymentDeliveryProtocolKey.Id;

                                    p.UserId = updatedBy.Id;
                                    p.SupplyInvoiceId = invoice.Id;

                                    return p;
                                })
                        );

                    List<SupplyInformationDeliveryProtocolKey> defaultInformationKeys =
                        supplyInformationDeliveryProtocolKeyRepository.GetAllDefaultByTransportationTypeAndDestination(message.SupplyOrder.TransportationType,
                            KeyAssignedTo.SupplyInvoice);

                    if (defaultInformationKeys.Any()) {
                        List<SupplyInformationDeliveryProtocol> informationProtocolsToAdd = new();

                        foreach (SupplyInformationDeliveryProtocolKey key in defaultInformationKeys)
                            informationProtocolsToAdd.Add(new SupplyInformationDeliveryProtocol {
                                SupplyInformationDeliveryProtocolKeyId = key.Id,
                                SupplyInvoiceId = invoice.Id,
                                UserId = updatedBy.Id,
                                Created = invoice.DateFrom ?? DateTime.UtcNow,
                                IsDefault = true
                            });

                        supplyInformationDeliveryProtocolRepository.Add(informationProtocolsToAdd);
                    }

                    informationMessage = new InformationMessage {
                        CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                        Title = $"����� Invoice � {invoice.Number}",
                        Message = $"������ Invoice �� ���������� {message.SupplyOrder.SupplyOrderNumber.Number}.",
                        Amount = $"�� ���� {invoice.NetPrice}"
                    };
                }

                foreach (SupplyInvoice invoice in message.SupplyOrder.SupplyInvoices.Where(i => !i.IsNew() && !string.IsNullOrEmpty(i.Number))) {
                    if (invoice.DateFrom.HasValue) invoice.DateFrom = TimeZoneInfo.ConvertTimeToUtc(invoice.DateFrom.Value);

                    if (invoice.PaymentTo.HasValue) invoice.PaymentTo = TimeZoneInfo.ConvertTimeToUtc(invoice.PaymentTo.Value);

                    if (invoice.InvoiceDocuments.Any()) {
                        invoiceDocumentRepository.RemoveAllBySupplyInvoiceIdExceptProvided(invoice.Id,
                            invoice.InvoiceDocuments.Where(d => !d.IsNew() && !d.Deleted).Select(d => d.Id));

                        invoiceDocumentRepository.Add(invoice.InvoiceDocuments.Where(d => d.IsNew()).Select(d => {
                            d.SupplyInvoiceId = invoice.Id;

                            return d;
                        }));

                        invoiceDocumentRepository.Update(invoice.InvoiceDocuments.Where(d => !d.IsNew()));
                    } else {
                        invoiceDocumentRepository.RemoveAllBySupplyInvoiceId(invoice.Id);
                    }

                    if (message.SupplyOrder.IsOrderShipped)
                        invoice.IsShipped = true;

                    if (invoice.PaymentDeliveryProtocols.Any(p => p.IsNew()))
                        supplyOrderPaymentDeliveryProtocolRepository.Add(
                            invoice.PaymentDeliveryProtocols
                                .Where(p => p.IsNew())
                                .Select(p => {
                                    if (p.SupplyPaymentTask != null) {
                                        p.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                                        p.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.PaymentDeliveryProtocol;
                                        p.SupplyPaymentTask.UserId = p.SupplyPaymentTask.User.Id;

                                        p.SupplyPaymentTask.IsAccounting = p.IsAccounting;

                                        p.SupplyPaymentTask.PayToDate =
                                            !p.SupplyPaymentTask.PayToDate.HasValue
                                                ? DateTime.UtcNow
                                                : TimeZoneInfo.ConvertTimeToUtc(p.SupplyPaymentTask.PayToDate.Value);

                                        p.SupplyPaymentTask.NetPrice = p.Value;
                                        p.SupplyPaymentTask.GrossPrice = p.Value;

                                        p.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(p.SupplyPaymentTask);
                                    }

                                    if (p.SupplyOrderPaymentDeliveryProtocolKey != null) {
                                        if (p.SupplyOrderPaymentDeliveryProtocolKey.IsNew())
                                            p.SupplyOrderPaymentDeliveryProtocolKeyId =
                                                supplyOrderPaymentDeliveryProtocolKeyRepository.Add(p.SupplyOrderPaymentDeliveryProtocolKey);
                                        else
                                            p.SupplyOrderPaymentDeliveryProtocolKeyId = p.SupplyOrderPaymentDeliveryProtocolKey.Id;
                                    }

                                    p.UserId = updatedBy.Id;
                                    p.SupplyInvoiceId = invoice.Id;

                                    return p;
                                })
                        );

                    if (invoice.PaymentDeliveryProtocols.Any(p => !p.IsNew() && p.SupplyPaymentTask != null)) {
                        foreach (SupplyOrderPaymentDeliveryProtocol p in invoice
                                     .PaymentDeliveryProtocols
                                     .Where(p => !p.IsNew() && p.SupplyPaymentTask != null && p.SupplyPaymentTask.IsNew())) {
                            p.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                            p.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.PaymentDeliveryProtocol;
                            p.SupplyPaymentTask.UserId = p.SupplyPaymentTask.User.Id;

                            p.SupplyPaymentTask.IsAccounting = p.IsAccounting;

                            p.SupplyPaymentTask.PayToDate =
                                !p.SupplyPaymentTask.PayToDate.HasValue
                                    ? DateTime.UtcNow
                                    : TimeZoneInfo.ConvertTimeToUtc(p.SupplyPaymentTask.PayToDate.Value);

                            p.SupplyPaymentTask.NetPrice = p.Value;
                            p.SupplyPaymentTask.GrossPrice = p.Value;

                            p.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(p.SupplyPaymentTask);

                            supplyOrderPaymentDeliveryProtocolRepository.Update(p);
                        }

                        foreach (SupplyOrderPaymentDeliveryProtocol protocol in invoice
                                     .PaymentDeliveryProtocols
                                     .Where(p => !p.IsNew() && p.SupplyPaymentTask != null && !p.SupplyPaymentTask.IsNew()))
                            if (protocol.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone) && !protocol.SupplyPaymentTask.IsAvailableForPayment) {
                                if (protocol.SupplyPaymentTask.Deleted) {
                                    supplyPaymentTaskRepository.RemoveById(protocol.SupplyPaymentTask.Id, updatedBy.Id);

                                    supplyOrderPaymentDeliveryProtocolRepository.Remove(protocol.NetUid);
                                } else {
                                    protocol.SupplyPaymentTask.NetPrice = protocol.Value;
                                    protocol.SupplyPaymentTask.GrossPrice = protocol.Value;
                                    protocol.SupplyPaymentTask.UpdatedById = updatedBy.Id;

                                    protocol.SupplyPaymentTask.IsAccounting = protocol.IsAccounting;

                                    supplyPaymentTaskRepository.Update(protocol.SupplyPaymentTask);
                                }
                            }
                    }

                    supplyInformationDeliveryProtocolRepository
                        .Update(
                            invoice
                                .InformationDeliveryProtocols
                                .Select(p => {
                                    p.Created = invoice.DateFrom ?? DateTime.UtcNow;

                                    return p;
                                })
                        );
                }

                supplyInvoiceRepository.Update(message.SupplyOrder.SupplyInvoices.Where(i => !i.IsNew() && !string.IsNullOrEmpty(i.Number)));
            }

            if (message.SupplyOrder.ResponsibilityDeliveryProtocols.Any()) {
                if (message.SupplyOrder.ResponsibilityDeliveryProtocols.Any(p => p.IsNew()))
                    _supplyRepositoriesFactory.NewResponsibilityDeliveryProtocolRepository(connection).Add(
                        message.SupplyOrder.ResponsibilityDeliveryProtocols
                            .Where(p => p.IsNew())
                            .Select(p => {
                                p.UserId = updatedBy.Id;
                                p.SupplyOrderId = message.SupplyOrder.Id;

                                switch (p.SupplyOrderStatus) {
                                    case SupplyOrderStatus.New:
                                        p.Created = DateTime.UtcNow;
                                        break;
                                    case SupplyOrderStatus.Order:
                                        p.Created = message.SupplyOrder.DateFrom ?? DateTime.UtcNow;
                                        break;
                                    case SupplyOrderStatus.Proform:
                                        p.Created = message.SupplyOrder.SupplyProForm?.DateFrom ?? DateTime.UtcNow;
                                        break;
                                    case SupplyOrderStatus.Invoice:
                                        p.Created =
                                            message.SupplyOrder.SupplyInvoices.Any()
                                                ? message.SupplyOrder.SupplyInvoices.First().DateFrom ?? DateTime.UtcNow
                                                : DateTime.UtcNow;
                                        break;
                                    default:
                                        p.Created = DateTime.UtcNow;
                                        break;
                                }

                                return p;
                            })
                    );

                if (message.SupplyOrder.ResponsibilityDeliveryProtocols.Any(p => !p.IsNew()))
                    _supplyRepositoriesFactory
                        .NewResponsibilityDeliveryProtocolRepository(connection)
                        .Update(
                            message
                                .SupplyOrder
                                .ResponsibilityDeliveryProtocols
                                .Where(p => !p.IsNew())
                                .Select(p => {
                                    switch (p.SupplyOrderStatus) {
                                        case SupplyOrderStatus.New:
                                            p.Created = DateTime.UtcNow;
                                            break;
                                        case SupplyOrderStatus.Order:
                                            p.Created = message.SupplyOrder.DateFrom ?? DateTime.UtcNow;
                                            break;
                                        case SupplyOrderStatus.Proform:
                                            p.Created = message.SupplyOrder.SupplyProForm?.DateFrom ?? DateTime.UtcNow;
                                            break;
                                        case SupplyOrderStatus.Invoice:
                                            p.Created =
                                                message.SupplyOrder.SupplyInvoices.Any()
                                                    ? message.SupplyOrder.SupplyInvoices.First().DateFrom ?? DateTime.UtcNow
                                                    : DateTime.UtcNow;
                                            break;
                                        default:
                                            p.Created = DateTime.UtcNow;
                                            break;
                                    }

                                    return p;
                                })
                        );
            }

            if (message.SupplyOrder.SupplyOrderDeliveryDocuments.Any()) {
                supplyOrderDeliveryDocumentRepository.RemoveAllBySupplyOrderIdExceptProvided(
                    message.SupplyOrder.Id,
                    message.SupplyOrder.SupplyOrderDeliveryDocuments.Where(d => !d.IsNew()).Select(d => d.Id)
                );

                if (message.SupplyOrder.SupplyOrderDeliveryDocuments.Any(d => d.IsProcessed && !d.IsNotified && !d.IsReceived)) {
                    SupplyOrderDeliveryDocument document = message.SupplyOrder.SupplyOrderDeliveryDocuments.First(d => d.IsProcessed && !d.IsNotified && !d.IsReceived);

                    informationMessage = new InformationMessage {
                        CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                        Title = $"�� �������� �������� � ��������� � {message.SupplyOrder.SupplyProForm.Number}",
                        Message = $"�� �������� �������� \"{document.SupplyDeliveryDocument.Name}\". �������: {document.Comment}"
                    };

                    document.IsNotified = true;
                }

                supplyOrderDeliveryDocumentRepository.Update(message.SupplyOrder.SupplyOrderDeliveryDocuments);
            }

            if (message.SupplyOrder.SupplyOrderContainerServices.Any()) {
                foreach (SupplyOrderContainerService junction in message.SupplyOrder.SupplyOrderContainerServices
                             .Where(s => s.ContainerService != null && !s.ContainerService.IsNew() && !string.IsNullOrEmpty(s.ContainerService.ContainerNumber))) {
                    if (junction.ContainerService.InvoiceDocuments.Any())
                        invoiceDocumentRepository.RemoveAllByContainerServiceIdExceptProvided(
                            junction.ContainerService.Id,
                            junction.ContainerService.InvoiceDocuments
                                .Where(d => !d.IsNew() && !d.Deleted)
                                .Select(d => d.Id)
                        );
                    else
                        invoiceDocumentRepository.RemoveAllByContainerServiceId(junction.ContainerService.Id);

                    if (junction.ContainerService.PackingLists.Any()) {
                        IEnumerable<long> ids = junction.ContainerService.PackingLists.Where(p => !p.IsNew()).Select(p => p.Id);

                        packingListRepository.UnassignAllByContainerServiceIdExceptProvided(junction.ContainerService.Id, ids);

                        packingListRepository.AssignProvidedToContainerService(junction.ContainerService.Id, ids);
                    } else {
                        packingListRepository.UnassignAllByContainerServiceId(junction.ContainerService.Id);
                    }
                }

                if (message.SupplyOrder.SupplyOrderContainerServices.Any(s => !s.IsNew() && !s.Deleted))
                    supplyOrderContainerServiceRepository.RemoveAllBySupplyOrderIdExceptProvided(
                        message.SupplyOrder.Id,
                        message.SupplyOrder.SupplyOrderContainerServices
                            .Where(s => !s.IsNew() && !s.Deleted)
                            .Select(s => s.Id)
                    );
                else
                    supplyOrderContainerServiceRepository.RemoveAllBySupplyOrderId(message.SupplyOrder.Id);

                if (message.SupplyOrder.SupplyOrderContainerServices.Any(s =>
                        s.IsNew() && s.ContainerService != null && s.ContainerService.ContainerOrganization == null &&
                        !s.ContainerService.ContainerOrganizationId.HasValue))
                    throw new Exception(SupplyOrderResourceNames.EMPTY_CONTAINER_SERVICE_ORGANIZATION);

                if (message.SupplyOrder.SupplyOrderContainerServices.Any(s =>
                        s.IsNew() && s.ContainerService != null && s.ContainerService.SupplyOrganizationAgreement == null &&
                        s.ContainerService.SupplyOrganizationAgreementId.Equals(0)))
                    throw new Exception(SupplyOrderResourceNames.EMPTY_CONTAINER_SERVICE_ORGANIZATION_AGREEMENT);

                supplyOrderContainerServiceRepository.Add(
                    message.SupplyOrder.SupplyOrderContainerServices
                        .Where(s => s.IsNew() && s.ContainerService != null && !s.Deleted && !string.IsNullOrEmpty(s.ContainerService.ContainerNumber))
                        .Select(junction => {
                            if (junction.ContainerService.IsNew()) {
                                if (junction.ContainerService.SupplyPaymentTask != null) {
                                    junction.ContainerService.SupplyPaymentTask.UserId = junction.ContainerService.SupplyPaymentTask.User.Id;
                                    junction.ContainerService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                                    junction.ContainerService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.ContainerService;

                                    junction.ContainerService.SupplyPaymentTask.PayToDate =
                                        !junction.ContainerService.SupplyPaymentTask.PayToDate.HasValue
                                            ? DateTime.UtcNow
                                            : TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.SupplyPaymentTask.PayToDate.Value);

                                    junction.ContainerService.GrossPrice = junction.ContainerService.NetPrice;
                                    junction.ContainerService.SupplyPaymentTask.NetPrice = junction.ContainerService.NetPrice;
                                    junction.ContainerService.SupplyPaymentTask.GrossPrice = junction.ContainerService.NetPrice;

                                    junction.ContainerService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(junction.ContainerService.SupplyPaymentTask);

                                    messagesToSend.Add(new PaymentTaskMessage {
                                        Amount = junction.ContainerService.GrossPrice,
                                        Discount = Convert.ToDouble(junction.ContainerService.Vat),
                                        CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                        PayToDate = junction.ContainerService.SupplyPaymentTask.PayToDate,
                                        OrganisationName = junction.ContainerService.ContainerOrganization.Name,
                                        PaymentForm = "Container service"
                                    });
                                }

                                if (junction.ContainerService.AccountingPaymentTask != null) {
                                    junction.ContainerService.AccountingPaymentTask.UserId = junction.ContainerService.AccountingPaymentTask.User.Id;
                                    junction.ContainerService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                                    junction.ContainerService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.ContainerService;
                                    junction.ContainerService.AccountingPaymentTask.IsAccounting = true;

                                    junction.ContainerService.AccountingPaymentTask.PayToDate =
                                        !junction.ContainerService.AccountingPaymentTask.PayToDate.HasValue
                                            ? DateTime.UtcNow
                                            : TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.AccountingPaymentTask.PayToDate.Value);

                                    junction.ContainerService.AccountingGrossPrice = junction.ContainerService.AccountingNetPrice;
                                    junction.ContainerService.AccountingPaymentTask.NetPrice = junction.ContainerService.AccountingNetPrice;
                                    junction.ContainerService.AccountingPaymentTask.GrossPrice = junction.ContainerService.AccountingNetPrice;

                                    junction.ContainerService.AccountingPaymentTaskId =
                                        supplyPaymentTaskRepository.Add(junction.ContainerService.AccountingPaymentTask);

                                    messagesToSend.Add(new PaymentTaskMessage {
                                        Amount = junction.ContainerService.AccountingGrossPrice,
                                        Discount = Convert.ToDouble(junction.ContainerService.AccountingVat),
                                        CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                        PayToDate = junction.ContainerService.AccountingPaymentTask.PayToDate,
                                        OrganisationName = junction.ContainerService.ContainerOrganization.Name,
                                        PaymentForm = "Container service"
                                    });
                                }

                                if (junction.ContainerService.SupplyInformationTask != null) {
                                    junction.ContainerService.SupplyInformationTask.UserId = updatedBy.Id;
                                    junction.ContainerService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                                    junction.ContainerService.AccountingSupplyCostsWithinCountry =
                                        junction.ContainerService.SupplyInformationTask.GrossPrice;

                                    junction.ContainerService.SupplyInformationTaskId =
                                        supplyInformationTaskRepository.Add(junction.ContainerService.SupplyInformationTask);
                                }

                                if (junction.ContainerService.ActProvidingServiceDocument != null) {
                                    if (junction.ContainerService.ActProvidingServiceDocument.IsNew()) {
                                        ActProvidingServiceDocument lastRecord =
                                            actProvidingServiceDocumentRepository.GetLastRecord();

                                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                            !string.IsNullOrEmpty(lastRecord.Number))
                                            junction.ContainerService.ActProvidingServiceDocument.Number =
                                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                                        else
                                            junction.ContainerService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                                        junction.ContainerService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                                            .New(junction.ContainerService.ActProvidingServiceDocument);
                                    } else if (junction.ContainerService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                                        junction.ContainerService.ActProvidingServiceDocumentId = null;
                                        actProvidingServiceDocumentRepository.RemoveById(junction.ContainerService.ActProvidingServiceDocument.Id);
                                    }
                                }

                                if (junction.ContainerService.SupplyServiceAccountDocument != null) {
                                    if (junction.ContainerService.SupplyServiceAccountDocument.IsNew()) {
                                        SupplyServiceAccountDocument lastRecord =
                                            supplyServiceAccountDocumentRepository.GetLastRecord();

                                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                            !string.IsNullOrEmpty(lastRecord.Number))
                                            junction.ContainerService.SupplyServiceAccountDocument.Number =
                                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                                        else
                                            junction.ContainerService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                                        junction.ContainerService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                                            .New(junction.ContainerService.SupplyServiceAccountDocument);
                                    } else if (junction.ContainerService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                                        junction.ContainerService.SupplyServiceAccountDocumentId = null;
                                        supplyServiceAccountDocumentRepository.RemoveById(junction.ContainerService.SupplyServiceAccountDocument.Id);
                                    }
                                }

                                if (junction.ContainerService.SupplyOrganizationAgreement != null &&
                                    !junction.ContainerService.SupplyOrganizationAgreement.IsNew()) {
                                    junction.ContainerService.SupplyOrganizationAgreement =
                                        supplyOrganizationAgreementRepository.GetById(junction.ContainerService.SupplyOrganizationAgreement.Id);

                                    junction.ContainerService.SupplyOrganizationAgreement.CurrentAmount =
                                        Math.Round(
                                            junction.ContainerService.SupplyOrganizationAgreement.CurrentAmount - junction.ContainerService.NetPrice, 2);

                                    junction.ContainerService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                                        Math.Round(
                                            junction.ContainerService.SupplyOrganizationAgreement.AccountingCurrentAmount -
                                            junction.ContainerService.AccountingNetPrice, 2);

                                    supplyOrganizationAgreementRepository.UpdateCurrentAmount(junction.ContainerService.SupplyOrganizationAgreement);
                                }

                                SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                                if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                                    junction.ContainerService.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                                else
                                    junction.ContainerService.ServiceNumber = string.Format("P{0:D10}", 1);

                                supplyServiceNumberRepository.Add(junction.ContainerService.ServiceNumber);

                                if (junction.ContainerService.BillOfLadingDocument != null && junction.ContainerService.BillOfLadingDocument.IsNew()) {
                                    junction.ContainerService.BillOfLadingDocument.Date =
                                        TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.BillOfLadingDocument.Date);

                                    junction.ContainerService.BillOfLadingDocumentId =
                                        billOfLadingDocumentRepository.Add(junction.ContainerService.BillOfLadingDocument);
                                }

                                junction.ContainerService.ContainerOrganizationId = junction.ContainerService.ContainerOrganization?.Id;
                                junction.ContainerService.UserId = updatedBy.Id;
                                if (junction.ContainerService.SupplyOrganizationAgreement != null)
                                    junction.ContainerService.SupplyOrganizationAgreementId = junction.ContainerService.SupplyOrganizationAgreement.Id;

                                junction.ContainerService.LoadDate = TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.LoadDate);

                                if (junction.ContainerService.FromDate.HasValue)
                                    junction.ContainerService.FromDate = TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.FromDate.Value);

                                junction.ContainerService.Id = containerServiceRepository.Add(junction.ContainerService);
                            } else {
                                if (junction.ContainerService.SupplyPaymentTask != null) {
                                    if (junction.ContainerService.SupplyPaymentTask.IsNew()) {
                                        junction.ContainerService.SupplyPaymentTask.UserId = junction.ContainerService.SupplyPaymentTask.User.Id;
                                        junction.ContainerService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                                        junction.ContainerService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.ContainerService;

                                        junction.ContainerService.SupplyPaymentTask.PayToDate =
                                            !junction.ContainerService.SupplyPaymentTask.PayToDate.HasValue
                                                ? DateTime.UtcNow
                                                : TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.SupplyPaymentTask.PayToDate.Value);

                                        junction.ContainerService.GrossPrice = junction.ContainerService.NetPrice;
                                        junction.ContainerService.SupplyPaymentTask.NetPrice = junction.ContainerService.NetPrice;
                                        junction.ContainerService.SupplyPaymentTask.GrossPrice = junction.ContainerService.NetPrice;

                                        junction.ContainerService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(junction.ContainerService.SupplyPaymentTask);

                                        messagesToSend.Add(new PaymentTaskMessage {
                                            Amount = junction.ContainerService.GrossPrice,
                                            Discount = Convert.ToDouble(junction.ContainerService.Vat),
                                            CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                            PayToDate = junction.ContainerService.SupplyPaymentTask.PayToDate,
                                            OrganisationName = junction.ContainerService.ContainerOrganization.Name,
                                            PaymentForm = "Container service"
                                        });
                                    } else {
                                        if (junction.ContainerService.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                                            && !junction.ContainerService.SupplyPaymentTask.IsAvailableForPayment) {
                                            if (junction.ContainerService.SupplyPaymentTask.Deleted) {
                                                supplyPaymentTaskRepository.RemoveById(junction.ContainerService.SupplyPaymentTask.Id, updatedBy.Id);

                                                junction.ContainerService.SupplyPaymentTaskId = null;
                                            } else {
                                                junction.ContainerService.SupplyPaymentTask.PayToDate =
                                                    !junction.ContainerService.SupplyPaymentTask.PayToDate.HasValue
                                                        ? DateTime.UtcNow
                                                        : TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.SupplyPaymentTask.PayToDate.Value);

                                                junction.ContainerService.GrossPrice = junction.ContainerService.NetPrice;
                                                junction.ContainerService.SupplyPaymentTask.NetPrice = junction.ContainerService.NetPrice;
                                                junction.ContainerService.SupplyPaymentTask.GrossPrice = junction.ContainerService.NetPrice;

                                                supplyPaymentTaskRepository.Update(junction.ContainerService.SupplyPaymentTask);
                                            }
                                        }
                                    }
                                }

                                if (junction.ContainerService.AccountingPaymentTask != null) {
                                    if (junction.ContainerService.AccountingPaymentTask.IsNew()) {
                                        junction.ContainerService.AccountingPaymentTask.UserId = junction.ContainerService.AccountingPaymentTask.User.Id;
                                        junction.ContainerService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                                        junction.ContainerService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.ContainerService;
                                        junction.ContainerService.AccountingPaymentTask.IsAccounting = true;

                                        junction.ContainerService.AccountingPaymentTask.PayToDate =
                                            !junction.ContainerService.AccountingPaymentTask.PayToDate.HasValue
                                                ? DateTime.UtcNow
                                                : TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.AccountingPaymentTask.PayToDate.Value);

                                        junction.ContainerService.AccountingGrossPrice = junction.ContainerService.AccountingNetPrice;
                                        junction.ContainerService.AccountingPaymentTask.NetPrice = junction.ContainerService.AccountingNetPrice;
                                        junction.ContainerService.AccountingPaymentTask.GrossPrice = junction.ContainerService.AccountingNetPrice;

                                        junction.ContainerService.AccountingPaymentTaskId =
                                            supplyPaymentTaskRepository.Add(junction.ContainerService.AccountingPaymentTask);

                                        messagesToSend.Add(new PaymentTaskMessage {
                                            Amount = junction.ContainerService.AccountingGrossPrice,
                                            Discount = Convert.ToDouble(junction.ContainerService.AccountingVat),
                                            CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                            PayToDate = junction.ContainerService.AccountingPaymentTask.PayToDate,
                                            OrganisationName = junction.ContainerService.ContainerOrganization.Name,
                                            PaymentForm = "Container service"
                                        });
                                    } else {
                                        if (junction.ContainerService.AccountingPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                                            && !junction.ContainerService.AccountingPaymentTask.IsAvailableForPayment) {
                                            if (junction.ContainerService.AccountingPaymentTask.Deleted) {
                                                supplyPaymentTaskRepository.RemoveById(junction.ContainerService.AccountingPaymentTask.Id, updatedBy.Id);

                                                junction.ContainerService.AccountingPaymentTaskId = null;
                                            } else {
                                                junction.ContainerService.AccountingPaymentTask.PayToDate =
                                                    !junction.ContainerService.AccountingPaymentTask.PayToDate.HasValue
                                                        ? DateTime.UtcNow
                                                        : TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.AccountingPaymentTask.PayToDate.Value);

                                                junction.ContainerService.AccountingGrossPrice = junction.ContainerService.AccountingNetPrice;
                                                junction.ContainerService.AccountingPaymentTask.NetPrice = junction.ContainerService.AccountingNetPrice;
                                                junction.ContainerService.AccountingPaymentTask.GrossPrice = junction.ContainerService.AccountingNetPrice;

                                                supplyPaymentTaskRepository.Update(junction.ContainerService.AccountingPaymentTask);
                                            }
                                        }
                                    }
                                }

                                if (junction.ContainerService.SupplyInformationTask != null) {
                                    if (junction.ContainerService.SupplyInformationTask.IsNew()) {
                                        junction.ContainerService.SupplyInformationTask.UserId = updatedBy.Id;
                                        junction.ContainerService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                                        junction.ContainerService.AccountingSupplyCostsWithinCountry =
                                            junction.ContainerService.SupplyInformationTask.GrossPrice;

                                        junction.ContainerService.SupplyInformationTaskId =
                                            supplyInformationTaskRepository.Add(junction.ContainerService.SupplyInformationTask);
                                    } else {
                                        if (junction.ContainerService.SupplyInformationTask.Deleted) {
                                            junction.ContainerService.SupplyInformationTask.DeletedById = updatedBy.Id;

                                            supplyInformationTaskRepository.Remove(junction.ContainerService.SupplyInformationTask);

                                            junction.ContainerService.SupplyInformationTaskId = null;
                                        } else {
                                            junction.ContainerService.SupplyInformationTask.UpdatedById = updatedBy.Id;
                                            junction.ContainerService.SupplyInformationTask.UserId = updatedBy.Id;

                                            junction.ContainerService.AccountingSupplyCostsWithinCountry =
                                                junction.ContainerService.SupplyInformationTask.GrossPrice;

                                            supplyInformationTaskRepository.Update(junction.ContainerService.SupplyInformationTask);
                                        }
                                    }
                                }

                                if (junction.ContainerService.ActProvidingServiceDocument != null) {
                                    if (junction.ContainerService.ActProvidingServiceDocument.IsNew()) {
                                        ActProvidingServiceDocument lastRecord =
                                            actProvidingServiceDocumentRepository.GetLastRecord();

                                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                            !string.IsNullOrEmpty(lastRecord.Number))
                                            junction.ContainerService.ActProvidingServiceDocument.Number =
                                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                                        else
                                            junction.ContainerService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                                        junction.ContainerService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                                            .New(junction.ContainerService.ActProvidingServiceDocument);
                                    } else if (junction.ContainerService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                                        junction.ContainerService.ActProvidingServiceDocumentId = null;
                                        actProvidingServiceDocumentRepository.RemoveById(junction.ContainerService.ActProvidingServiceDocument.Id);
                                    }
                                }

                                if (junction.ContainerService.SupplyServiceAccountDocument != null) {
                                    if (junction.ContainerService.SupplyServiceAccountDocument.IsNew()) {
                                        SupplyServiceAccountDocument lastRecord =
                                            supplyServiceAccountDocumentRepository.GetLastRecord();

                                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                            !string.IsNullOrEmpty(lastRecord.Number))
                                            junction.ContainerService.SupplyServiceAccountDocument.Number =
                                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                                        else
                                            junction.ContainerService.SupplyServiceAccountDocument.Number = string.Format("{0:D10}", 1);

                                        junction.ContainerService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                                            .New(junction.ContainerService.SupplyServiceAccountDocument);
                                    } else if (junction.ContainerService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                                        junction.ContainerService.SupplyServiceAccountDocumentId = null;
                                        supplyServiceAccountDocumentRepository.RemoveById(junction.ContainerService.SupplyServiceAccountDocument.Id);
                                    }
                                }

                                junction.ContainerService.LoadDate = TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.LoadDate);

                                if (junction.ContainerService.FromDate.HasValue)
                                    junction.ContainerService.FromDate = TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.FromDate.Value);

                                containerServiceRepository.Update(junction.ContainerService);
                            }

                            invoiceDocumentRepository.Add(junction.ContainerService.InvoiceDocuments
                                .Where(d => d.IsNew())
                                .Select(d => {
                                    d.ContainerServiceId = junction.ContainerService.Id;

                                    return d;
                                })
                            );

                            junction.ContainerServiceId = junction.ContainerService.Id;
                            junction.SupplyOrderId = message.SupplyOrder.Id;

                            return junction;
                        })
                );

                supplyOrderContainerServiceRepository.Update(
                    message.SupplyOrder.SupplyOrderContainerServices
                        .Where(s => !s.IsNew() && s.ContainerService != null && !s.Deleted && !string.IsNullOrEmpty(s.ContainerService.ContainerNumber))
                        .Select(junction => {
                            ContainerService containerService = containerServiceRepository.GetByIdWithoutIncludes(junction.ContainerService.Id);

                            UpdateSupplyOrganizationAndAgreement(
                                supplyOrganizationAgreementRepository,
                                containerService.SupplyOrganizationAgreementId,
                                containerService.NetPrice,
                                containerService.AccountingNetPrice,
                                junction.ContainerService.SupplyOrganizationAgreement.Id,
                                junction.ContainerService.NetPrice,
                                junction.ContainerService.AccountingNetPrice);

                            junction.ContainerService.ContainerOrganizationId =
                                junction.ContainerService.ContainerOrganization.Id;
                            junction.ContainerService.SupplyOrganizationAgreementId =
                                junction.ContainerService.SupplyOrganizationAgreement.Id;

                            if (junction.ContainerService.IsNew()) {
                                if (junction.ContainerService.SupplyPaymentTask != null) {
                                    junction.ContainerService.SupplyPaymentTask.UserId = junction.ContainerService.SupplyPaymentTask.User.Id;
                                    junction.ContainerService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                                    junction.ContainerService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.ContainerService;

                                    junction.ContainerService.SupplyPaymentTask.PayToDate =
                                        !junction.ContainerService.SupplyPaymentTask.PayToDate.HasValue
                                            ? DateTime.UtcNow
                                            : TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.SupplyPaymentTask.PayToDate.Value);

                                    junction.ContainerService.GrossPrice = junction.ContainerService.NetPrice;
                                    junction.ContainerService.SupplyPaymentTask.NetPrice = junction.ContainerService.NetPrice;
                                    junction.ContainerService.SupplyPaymentTask.GrossPrice = junction.ContainerService.NetPrice;

                                    junction.ContainerService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(junction.ContainerService.SupplyPaymentTask);

                                    messagesToSend.Add(new PaymentTaskMessage {
                                        Amount = junction.ContainerService.GrossPrice,
                                        Discount = Convert.ToDouble(junction.ContainerService.Vat),
                                        CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                        PayToDate = junction.ContainerService.SupplyPaymentTask.PayToDate,
                                        OrganisationName = junction.ContainerService.ContainerOrganization.Name,
                                        PaymentForm = "Container service"
                                    });
                                }

                                if (junction.ContainerService.AccountingPaymentTask != null) {
                                    junction.ContainerService.AccountingPaymentTask.UserId = junction.ContainerService.AccountingPaymentTask.User.Id;
                                    junction.ContainerService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                                    junction.ContainerService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.ContainerService;
                                    junction.ContainerService.AccountingPaymentTask.IsAccounting = true;

                                    junction.ContainerService.AccountingPaymentTask.PayToDate =
                                        !junction.ContainerService.AccountingPaymentTask.PayToDate.HasValue
                                            ? DateTime.UtcNow
                                            : TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.AccountingPaymentTask.PayToDate.Value);

                                    junction.ContainerService.AccountingGrossPrice = junction.ContainerService.AccountingNetPrice;
                                    junction.ContainerService.AccountingPaymentTask.NetPrice = junction.ContainerService.AccountingNetPrice;
                                    junction.ContainerService.AccountingPaymentTask.GrossPrice = junction.ContainerService.AccountingNetPrice;

                                    junction.ContainerService.AccountingPaymentTaskId =
                                        supplyPaymentTaskRepository.Add(junction.ContainerService.AccountingPaymentTask);

                                    messagesToSend.Add(new PaymentTaskMessage {
                                        Amount = junction.ContainerService.AccountingGrossPrice,
                                        Discount = Convert.ToDouble(junction.ContainerService.AccountingVat),
                                        CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                        PayToDate = junction.ContainerService.AccountingPaymentTask.PayToDate,
                                        OrganisationName = junction.ContainerService.ContainerOrganization.Name,
                                        PaymentForm = "Container service"
                                    });
                                }

                                if (junction.ContainerService.SupplyInformationTask != null) {
                                    junction.ContainerService.SupplyInformationTask.UserId = updatedBy.Id;
                                    junction.ContainerService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                                    junction.ContainerService.AccountingSupplyCostsWithinCountry =
                                        junction.ContainerService.SupplyInformationTask.GrossPrice;

                                    junction.ContainerService.SupplyInformationTaskId =
                                        supplyInformationTaskRepository.Add(junction.ContainerService.SupplyInformationTask);
                                }

                                if (junction.ContainerService.ActProvidingServiceDocument != null) {
                                    if (junction.ContainerService.ActProvidingServiceDocument.IsNew()) {
                                        ActProvidingServiceDocument lastRecord =
                                            actProvidingServiceDocumentRepository.GetLastRecord();

                                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                            !string.IsNullOrEmpty(lastRecord.Number))
                                            junction.ContainerService.ActProvidingServiceDocument.Number =
                                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                                        else
                                            junction.ContainerService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                                        junction.ContainerService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                                            .New(junction.ContainerService.ActProvidingServiceDocument);
                                    } else if (junction.ContainerService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                                        junction.ContainerService.ActProvidingServiceDocumentId = null;
                                        actProvidingServiceDocumentRepository.RemoveById(junction.ContainerService.ActProvidingServiceDocument.Id);
                                    }
                                }

                                if (junction.ContainerService.SupplyServiceAccountDocument != null) {
                                    if (junction.ContainerService.SupplyServiceAccountDocument.IsNew()) {
                                        SupplyServiceAccountDocument lastRecord =
                                            supplyServiceAccountDocumentRepository.GetLastRecord();

                                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                            !string.IsNullOrEmpty(lastRecord.Number))
                                            junction.ContainerService.SupplyServiceAccountDocument.Number =
                                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                                        else
                                            junction.ContainerService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                                        junction.ContainerService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                                            .New(junction.ContainerService.SupplyServiceAccountDocument);
                                    } else if (junction.ContainerService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                                        junction.ContainerService.SupplyServiceAccountDocumentId = null;
                                        supplyServiceAccountDocumentRepository.RemoveById(junction.ContainerService.SupplyServiceAccountDocument.Id);
                                    }
                                }

                                SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                                if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                                    junction.ContainerService.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                                else
                                    junction.ContainerService.ServiceNumber = string.Format("P{0:D10}", 1);

                                supplyServiceNumberRepository.Add(junction.ContainerService.ServiceNumber);

                                if (junction.ContainerService.BillOfLadingDocument != null && junction.ContainerService.BillOfLadingDocument.IsNew()) {
                                    junction.ContainerService.BillOfLadingDocument.Date =
                                        TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.BillOfLadingDocument.Date);

                                    junction.ContainerService.BillOfLadingDocumentId =
                                        billOfLadingDocumentRepository.Add(junction.ContainerService.BillOfLadingDocument);
                                }

                                junction.ContainerService.ContainerOrganizationId = junction.ContainerService.ContainerOrganization?.Id;
                                junction.ContainerService.UserId = updatedBy.Id;

                                junction.ContainerService.LoadDate = TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.LoadDate);

                                if (junction.ContainerService.FromDate.HasValue)
                                    junction.ContainerService.FromDate = TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.FromDate.Value);

                                junction.ContainerService.Id = containerServiceRepository.Add(junction.ContainerService);
                            } else {
                                if (junction.ContainerService.SupplyPaymentTask != null) {
                                    if (junction.ContainerService.SupplyPaymentTask.IsNew()) {
                                        junction.ContainerService.SupplyPaymentTask.UserId = junction.ContainerService.SupplyPaymentTask.User.Id;
                                        junction.ContainerService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                                        junction.ContainerService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.ContainerService;

                                        junction.ContainerService.SupplyPaymentTask.PayToDate =
                                            !junction.ContainerService.SupplyPaymentTask.PayToDate.HasValue
                                                ? DateTime.UtcNow
                                                : TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.SupplyPaymentTask.PayToDate.Value);

                                        junction.ContainerService.GrossPrice = junction.ContainerService.NetPrice;
                                        junction.ContainerService.SupplyPaymentTask.NetPrice = junction.ContainerService.NetPrice;
                                        junction.ContainerService.SupplyPaymentTask.GrossPrice = junction.ContainerService.NetPrice;

                                        junction.ContainerService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(junction.ContainerService.SupplyPaymentTask);

                                        messagesToSend.Add(new PaymentTaskMessage {
                                            Amount = junction.ContainerService.GrossPrice,
                                            Discount = Convert.ToDouble(junction.ContainerService.Vat),
                                            CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                            PayToDate = junction.ContainerService.SupplyPaymentTask.PayToDate,
                                            OrganisationName = junction.ContainerService.ContainerOrganization.Name,
                                            PaymentForm = "Container service"
                                        });
                                    } else {
                                        if (junction.ContainerService.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                                            && !junction.ContainerService.SupplyPaymentTask.IsAvailableForPayment) {
                                            if (junction.ContainerService.SupplyPaymentTask.Deleted) {
                                                supplyPaymentTaskRepository.RemoveById(junction.ContainerService.SupplyPaymentTask.Id, updatedBy.Id);

                                                junction.ContainerService.SupplyPaymentTaskId = null;
                                            } else {
                                                junction.ContainerService.SupplyPaymentTask.PayToDate =
                                                    !junction.ContainerService.SupplyPaymentTask.PayToDate.HasValue
                                                        ? DateTime.UtcNow
                                                        : TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.SupplyPaymentTask.PayToDate.Value);

                                                junction.ContainerService.GrossPrice = junction.ContainerService.NetPrice;
                                                junction.ContainerService.SupplyPaymentTask.NetPrice = junction.ContainerService.NetPrice;
                                                junction.ContainerService.SupplyPaymentTask.GrossPrice = junction.ContainerService.NetPrice;

                                                supplyPaymentTaskRepository.Update(junction.ContainerService.SupplyPaymentTask);
                                            }
                                        }
                                    }
                                }

                                if (junction.ContainerService.AccountingPaymentTask != null) {
                                    if (junction.ContainerService.AccountingPaymentTask.IsNew()) {
                                        junction.ContainerService.AccountingPaymentTask.UserId = junction.ContainerService.AccountingPaymentTask.User.Id;
                                        junction.ContainerService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                                        junction.ContainerService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.ContainerService;
                                        junction.ContainerService.AccountingPaymentTask.IsAccounting = true;

                                        junction.ContainerService.AccountingPaymentTask.PayToDate =
                                            !junction.ContainerService.AccountingPaymentTask.PayToDate.HasValue
                                                ? DateTime.UtcNow
                                                : TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.AccountingPaymentTask.PayToDate.Value);

                                        junction.ContainerService.AccountingGrossPrice = junction.ContainerService.AccountingNetPrice;
                                        junction.ContainerService.AccountingPaymentTask.NetPrice = junction.ContainerService.AccountingNetPrice;
                                        junction.ContainerService.AccountingPaymentTask.GrossPrice = junction.ContainerService.AccountingNetPrice;

                                        junction.ContainerService.AccountingPaymentTaskId =
                                            supplyPaymentTaskRepository.Add(junction.ContainerService.AccountingPaymentTask);

                                        messagesToSend.Add(new PaymentTaskMessage {
                                            Amount = junction.ContainerService.AccountingGrossPrice,
                                            Discount = Convert.ToDouble(junction.ContainerService.AccountingVat),
                                            CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                            PayToDate = junction.ContainerService.AccountingPaymentTask.PayToDate,
                                            OrganisationName = junction.ContainerService.ContainerOrganization.Name,
                                            PaymentForm = "Container service"
                                        });
                                    } else {
                                        if (junction.ContainerService.AccountingPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                                            && !junction.ContainerService.AccountingPaymentTask.IsAvailableForPayment) {
                                            if (junction.ContainerService.AccountingPaymentTask.Deleted) {
                                                supplyPaymentTaskRepository.RemoveById(junction.ContainerService.AccountingPaymentTask.Id, updatedBy.Id);

                                                junction.ContainerService.AccountingPaymentTaskId = null;
                                            } else {
                                                junction.ContainerService.AccountingPaymentTask.PayToDate =
                                                    !junction.ContainerService.AccountingPaymentTask.PayToDate.HasValue
                                                        ? DateTime.UtcNow
                                                        : TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.AccountingPaymentTask.PayToDate.Value);

                                                junction.ContainerService.AccountingGrossPrice = junction.ContainerService.AccountingNetPrice;
                                                junction.ContainerService.AccountingPaymentTask.NetPrice = junction.ContainerService.AccountingNetPrice;
                                                junction.ContainerService.AccountingPaymentTask.GrossPrice = junction.ContainerService.AccountingNetPrice;

                                                supplyPaymentTaskRepository.Update(junction.ContainerService.AccountingPaymentTask);
                                            }
                                        }
                                    }
                                }

                                if (junction.ContainerService.SupplyInformationTask != null) {
                                    if (junction.ContainerService.SupplyInformationTask.IsNew()) {
                                        junction.ContainerService.SupplyInformationTask.UserId = updatedBy.Id;
                                        junction.ContainerService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                                        junction.ContainerService.AccountingSupplyCostsWithinCountry =
                                            junction.ContainerService.SupplyInformationTask.GrossPrice;

                                        junction.ContainerService.SupplyInformationTaskId =
                                            supplyInformationTaskRepository.Add(junction.ContainerService.SupplyInformationTask);
                                    } else {
                                        if (junction.ContainerService.SupplyInformationTask.Deleted) {
                                            junction.ContainerService.SupplyInformationTask.DeletedById = updatedBy.Id;

                                            supplyInformationTaskRepository.Remove(junction.ContainerService.SupplyInformationTask);

                                            junction.ContainerService.SupplyInformationTaskId = null;
                                        } else {
                                            junction.ContainerService.SupplyInformationTask.UpdatedById = updatedBy.Id;
                                            junction.ContainerService.SupplyInformationTask.UserId = updatedBy.Id;

                                            junction.ContainerService.AccountingSupplyCostsWithinCountry =
                                                junction.ContainerService.SupplyInformationTask.GrossPrice;

                                            supplyInformationTaskRepository.Update(junction.ContainerService.SupplyInformationTask);
                                        }
                                    }
                                }

                                if (junction.ContainerService.ActProvidingServiceDocument != null) {
                                    if (junction.ContainerService.ActProvidingServiceDocument.IsNew()) {
                                        ActProvidingServiceDocument lastRecord =
                                            actProvidingServiceDocumentRepository.GetLastRecord();

                                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                            !string.IsNullOrEmpty(lastRecord.Number))
                                            junction.ContainerService.ActProvidingServiceDocument.Number =
                                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                                        else
                                            junction.ContainerService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                                        junction.ContainerService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                                            .New(junction.ContainerService.ActProvidingServiceDocument);
                                    } else if (junction.ContainerService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                                        junction.ContainerService.ActProvidingServiceDocumentId = null;
                                        actProvidingServiceDocumentRepository.RemoveById(junction.ContainerService.ActProvidingServiceDocument.Id);
                                    }
                                }

                                if (junction.ContainerService.SupplyServiceAccountDocument != null) {
                                    if (junction.ContainerService.SupplyServiceAccountDocument.IsNew()) {
                                        SupplyServiceAccountDocument lastRecord =
                                            supplyServiceAccountDocumentRepository.GetLastRecord();

                                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                            !string.IsNullOrEmpty(lastRecord.Number))
                                            junction.ContainerService.SupplyServiceAccountDocument.Number =
                                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                                        else
                                            junction.ContainerService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                                        junction.ContainerService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                                            .New(junction.ContainerService.SupplyServiceAccountDocument);
                                    } else if (junction.ContainerService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                                        junction.ContainerService.SupplyServiceAccountDocumentId = null;
                                        supplyServiceAccountDocumentRepository.RemoveById(junction.ContainerService.SupplyServiceAccountDocument.Id);
                                    }
                                }

                                junction.ContainerService.LoadDate = TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.LoadDate);

                                if (junction.ContainerService.FromDate.HasValue)
                                    junction.ContainerService.FromDate = TimeZoneInfo.ConvertTimeToUtc(junction.ContainerService.FromDate.Value);

                                containerServiceRepository.Update(junction.ContainerService);
                            }

                            invoiceDocumentRepository.Add(junction.ContainerService.InvoiceDocuments
                                .Where(d => d.IsNew())
                                .Select(d => {
                                    d.ContainerServiceId = junction.ContainerService.Id;

                                    return d;
                                })
                            );

                            junction.ContainerServiceId = junction.ContainerService.Id;
                            junction.SupplyOrderId = message.SupplyOrder.Id;

                            return junction;
                        })
                );
            } else {
                supplyOrderContainerServiceRepository.RemoveAllBySupplyOrderId(message.SupplyOrder.Id);
            }

            if (message.SupplyOrder.SupplyOrderVehicleServices.Any()) {
                foreach (SupplyOrderVehicleService junction in message.SupplyOrder.SupplyOrderVehicleServices
                             .Where(s => s.VehicleService != null && !s.VehicleService.IsNew() && !string.IsNullOrEmpty(s.VehicleService.VehicleNumber))) {
                    if (junction.VehicleService.InvoiceDocuments.Any())
                        invoiceDocumentRepository.RemoveAllByVehicleServiceIdExceptProvided(
                            junction.VehicleService.Id,
                            junction.VehicleService.InvoiceDocuments
                                .Where(d => !d.IsNew() && !d.Deleted)
                                .Select(d => d.Id)
                        );
                    else
                        invoiceDocumentRepository.RemoveAllByVehicleServiceId(junction.VehicleService.Id);

                    if (junction.VehicleService.PackingLists.Any()) {
                        IEnumerable<long> ids = junction.VehicleService.PackingLists.Where(p => !p.IsNew()).Select(p => p.Id);

                        packingListRepository.UnassignAllByVehicleServiceIdExceptProvided(junction.VehicleService.Id, ids);

                        packingListRepository.AssignProvidedToVehicleService(junction.VehicleService.Id, ids);
                    } else {
                        packingListRepository.UnassignAllByVehicleServiceId(junction.VehicleService.Id);
                    }
                }

                if (message.SupplyOrder.SupplyOrderVehicleServices.Any(s => !s.IsNew() && !s.Deleted))
                    supplyOrderVehicleServiceRepository.RemoveAllBySupplyOrderIdExceptProvided(
                        message.SupplyOrder.Id,
                        message.SupplyOrder.SupplyOrderVehicleServices
                            .Where(s => !s.IsNew() && !s.Deleted)
                            .Select(s => s.Id)
                    );
                else
                    supplyOrderVehicleServiceRepository.RemoveAllBySupplyOrderId(message.SupplyOrder.Id);

                if (message.SupplyOrder.SupplyOrderVehicleServices.Any(s =>
                        s.IsNew() && s.VehicleService != null && s.VehicleService.VehicleOrganization == null &&
                        !s.VehicleService.VehicleOrganizationId.HasValue))
                    throw new Exception(SupplyOrderResourceNames.EMPTY_CONTAINER_SERVICE_ORGANIZATION);

                if (message.SupplyOrder.SupplyOrderVehicleServices.Any(s =>
                        s.IsNew() && s.VehicleService != null && s.VehicleService.SupplyOrganizationAgreement == null &&
                        s.VehicleService.SupplyOrganizationAgreementId.Equals(0)))
                    throw new Exception(SupplyOrderResourceNames.EMPTY_CONTAINER_SERVICE_ORGANIZATION_AGREEMENT);

                supplyOrderVehicleServiceRepository.Add(
                    message.SupplyOrder.SupplyOrderVehicleServices
                        .Where(s => s.IsNew() && s.VehicleService != null && !s.Deleted && !string.IsNullOrEmpty(s.VehicleService.VehicleNumber))
                        .Select(junction => {
                            if (junction.VehicleService.IsNew()) {
                                if (junction.VehicleService.SupplyPaymentTask != null) {
                                    junction.VehicleService.SupplyPaymentTask.UserId = junction.VehicleService.SupplyPaymentTask.User.Id;
                                    junction.VehicleService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                                    junction.VehicleService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.VehicleService;

                                    junction.VehicleService.SupplyPaymentTask.PayToDate =
                                        !junction.VehicleService.SupplyPaymentTask.PayToDate.HasValue
                                            ? DateTime.UtcNow
                                            : TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.SupplyPaymentTask.PayToDate.Value);

                                    junction.VehicleService.GrossPrice = junction.VehicleService.NetPrice;
                                    junction.VehicleService.SupplyPaymentTask.NetPrice = junction.VehicleService.NetPrice;
                                    junction.VehicleService.SupplyPaymentTask.GrossPrice = junction.VehicleService.NetPrice;

                                    junction.VehicleService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(junction.VehicleService.SupplyPaymentTask);

                                    messagesToSend.Add(new PaymentTaskMessage {
                                        Amount = junction.VehicleService.GrossPrice,
                                        Discount = Convert.ToDouble(junction.VehicleService.Vat),
                                        CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                        PayToDate = junction.VehicleService.SupplyPaymentTask.PayToDate,
                                        OrganisationName = junction.VehicleService.VehicleOrganization.Name,
                                        PaymentForm = "Vehicle service"
                                    });
                                }

                                if (junction.VehicleService.AccountingPaymentTask != null) {
                                    junction.VehicleService.AccountingPaymentTask.UserId = junction.VehicleService.AccountingPaymentTask.User.Id;
                                    junction.VehicleService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                                    junction.VehicleService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.VehicleService;
                                    junction.VehicleService.AccountingPaymentTask.IsAccounting = true;

                                    junction.VehicleService.AccountingPaymentTask.PayToDate =
                                        !junction.VehicleService.AccountingPaymentTask.PayToDate.HasValue
                                            ? DateTime.UtcNow
                                            : TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.AccountingPaymentTask.PayToDate.Value);

                                    junction.VehicleService.AccountingGrossPrice = junction.VehicleService.AccountingNetPrice;
                                    junction.VehicleService.AccountingPaymentTask.NetPrice = junction.VehicleService.AccountingNetPrice;
                                    junction.VehicleService.AccountingPaymentTask.GrossPrice = junction.VehicleService.AccountingNetPrice;

                                    junction.VehicleService.AccountingPaymentTaskId = supplyPaymentTaskRepository.Add(junction.VehicleService.AccountingPaymentTask);

                                    messagesToSend.Add(new PaymentTaskMessage {
                                        Amount = junction.VehicleService.AccountingGrossPrice,
                                        Discount = Convert.ToDouble(junction.VehicleService.AccountingVat),
                                        CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                        PayToDate = junction.VehicleService.AccountingPaymentTask.PayToDate,
                                        OrganisationName = junction.VehicleService.VehicleOrganization.Name,
                                        PaymentForm = "Vehicle service"
                                    });
                                }

                                if (junction.VehicleService.SupplyInformationTask != null) {
                                    junction.VehicleService.SupplyInformationTask.UserId = updatedBy.Id;
                                    junction.VehicleService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                                    junction.VehicleService.AccountingSupplyCostsWithinCountry =
                                        junction.VehicleService.SupplyInformationTask.GrossPrice;

                                    junction.VehicleService.SupplyInformationTaskId =
                                        supplyInformationTaskRepository.Add(junction.VehicleService.SupplyInformationTask);
                                }

                                if (junction.VehicleService.ActProvidingServiceDocument != null) {
                                    if (junction.VehicleService.ActProvidingServiceDocument.IsNew()) {
                                        ActProvidingServiceDocument lastRecord =
                                            actProvidingServiceDocumentRepository.GetLastRecord();

                                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                            !string.IsNullOrEmpty(lastRecord.Number))
                                            junction.VehicleService.ActProvidingServiceDocument.Number =
                                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                                        else
                                            junction.VehicleService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                                        junction.VehicleService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                                            .New(junction.VehicleService.ActProvidingServiceDocument);
                                    } else if (junction.VehicleService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                                        junction.VehicleService.ActProvidingServiceDocumentId = null;
                                        actProvidingServiceDocumentRepository.RemoveById(junction.VehicleService.ActProvidingServiceDocument.Id);
                                    }
                                }

                                if (junction.VehicleService.SupplyServiceAccountDocument != null) {
                                    if (junction.VehicleService.SupplyServiceAccountDocument.IsNew()) {
                                        SupplyServiceAccountDocument lastRecord =
                                            supplyServiceAccountDocumentRepository.GetLastRecord();

                                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                            !string.IsNullOrEmpty(lastRecord.Number))
                                            junction.VehicleService.SupplyServiceAccountDocument.Number =
                                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                                        else
                                            junction.VehicleService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                                        junction.VehicleService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                                            .New(junction.VehicleService.SupplyServiceAccountDocument);
                                    } else if (junction.VehicleService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                                        junction.VehicleService.SupplyServiceAccountDocumentId = null;
                                        supplyServiceAccountDocumentRepository.RemoveById(junction.VehicleService.SupplyServiceAccountDocument.Id);
                                    }
                                }

                                if (junction.VehicleService.SupplyOrganizationAgreement != null && !junction.VehicleService.SupplyOrganizationAgreement.IsNew()) {
                                    junction.VehicleService.SupplyOrganizationAgreement =
                                        supplyOrganizationAgreementRepository.GetById(junction.VehicleService.SupplyOrganizationAgreement.Id);

                                    junction.VehicleService.SupplyOrganizationAgreement.CurrentAmount =
                                        Math.Round(
                                            junction.VehicleService.SupplyOrganizationAgreement.CurrentAmount - junction.VehicleService.NetPrice, 2);

                                    junction.VehicleService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                                        Math.Round(
                                            junction.VehicleService.SupplyOrganizationAgreement.AccountingCurrentAmount -
                                            junction.VehicleService.AccountingNetPrice, 2);

                                    supplyOrganizationAgreementRepository.UpdateCurrentAmount(junction.VehicleService.SupplyOrganizationAgreement);
                                }

                                SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                                if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                                    junction.VehicleService.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                                else
                                    junction.VehicleService.ServiceNumber = string.Format("P{0:D10}", 1);

                                supplyServiceNumberRepository.Add(junction.VehicleService.ServiceNumber);

                                if (junction.VehicleService.BillOfLadingDocument != null && junction.VehicleService.BillOfLadingDocument.IsNew()) {
                                    junction.VehicleService.BillOfLadingDocument.Date =
                                        TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.BillOfLadingDocument.Date);

                                    junction.VehicleService.BillOfLadingDocumentId =
                                        billOfLadingDocumentRepository.Add(junction.VehicleService.BillOfLadingDocument);
                                }

                                junction.VehicleService.VehicleOrganizationId = junction.VehicleService.VehicleOrganization?.Id;
                                junction.VehicleService.UserId = updatedBy.Id;
                                if (junction.VehicleService.SupplyOrganizationAgreement != null)
                                    junction.VehicleService.SupplyOrganizationAgreementId = junction.VehicleService.SupplyOrganizationAgreement.Id;

                                junction.VehicleService.LoadDate = TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.LoadDate);

                                if (junction.VehicleService.FromDate.HasValue)
                                    junction.VehicleService.FromDate = TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.FromDate.Value);

                                junction.VehicleServiceId = junction.VehicleService.Id = vehicleServiceRepository.Add(junction.VehicleService);
                            } else {
                                if (junction.VehicleService.SupplyPaymentTask != null) {
                                    if (junction.VehicleService.SupplyPaymentTask.IsNew()) {
                                        junction.VehicleService.SupplyPaymentTask.UserId = junction.VehicleService.SupplyPaymentTask.User.Id;
                                        junction.VehicleService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                                        junction.VehicleService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.VehicleService;

                                        junction.VehicleService.SupplyPaymentTask.PayToDate =
                                            !junction.VehicleService.SupplyPaymentTask.PayToDate.HasValue
                                                ? DateTime.UtcNow
                                                : TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.SupplyPaymentTask.PayToDate.Value);

                                        junction.VehicleService.GrossPrice = junction.VehicleService.NetPrice;
                                        junction.VehicleService.SupplyPaymentTask.NetPrice = junction.VehicleService.NetPrice;
                                        junction.VehicleService.SupplyPaymentTask.GrossPrice = junction.VehicleService.NetPrice;

                                        junction.VehicleService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(junction.VehicleService.SupplyPaymentTask);

                                        messagesToSend.Add(new PaymentTaskMessage {
                                            Amount = junction.VehicleService.GrossPrice,
                                            Discount = Convert.ToDouble(junction.VehicleService.Vat),
                                            CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                            PayToDate = junction.VehicleService.SupplyPaymentTask.PayToDate,
                                            OrganisationName = junction.VehicleService.VehicleOrganization.Name,
                                            PaymentForm = "Vehicle service"
                                        });
                                    } else {
                                        if (junction.VehicleService.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                                            && !junction.VehicleService.SupplyPaymentTask.IsAvailableForPayment) {
                                            if (junction.VehicleService.SupplyPaymentTask.Deleted) {
                                                supplyPaymentTaskRepository.RemoveById(junction.VehicleService.SupplyPaymentTask.Id, updatedBy.Id);

                                                junction.VehicleService.SupplyPaymentTaskId = null;
                                            } else {
                                                junction.VehicleService.SupplyPaymentTask.PayToDate =
                                                    !junction.VehicleService.SupplyPaymentTask.PayToDate.HasValue
                                                        ? DateTime.UtcNow
                                                        : TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.SupplyPaymentTask.PayToDate.Value);

                                                junction.VehicleService.GrossPrice = junction.VehicleService.NetPrice;
                                                junction.VehicleService.SupplyPaymentTask.NetPrice = junction.VehicleService.NetPrice;
                                                junction.VehicleService.SupplyPaymentTask.GrossPrice = junction.VehicleService.NetPrice;

                                                supplyPaymentTaskRepository.Update(junction.VehicleService.SupplyPaymentTask);
                                            }
                                        }
                                    }
                                }

                                if (junction.VehicleService.AccountingPaymentTask != null) {
                                    if (junction.VehicleService.AccountingPaymentTask.IsNew()) {
                                        junction.VehicleService.AccountingPaymentTask.UserId = junction.VehicleService.AccountingPaymentTask.User.Id;
                                        junction.VehicleService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                                        junction.VehicleService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.VehicleService;
                                        junction.VehicleService.AccountingPaymentTask.IsAccounting = true;

                                        junction.VehicleService.AccountingPaymentTask.PayToDate =
                                            !junction.VehicleService.AccountingPaymentTask.PayToDate.HasValue
                                                ? DateTime.UtcNow
                                                : TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.AccountingPaymentTask.PayToDate.Value);

                                        junction.VehicleService.AccountingGrossPrice = junction.VehicleService.AccountingNetPrice;
                                        junction.VehicleService.AccountingPaymentTask.NetPrice = junction.VehicleService.AccountingNetPrice;
                                        junction.VehicleService.AccountingPaymentTask.GrossPrice = junction.VehicleService.AccountingNetPrice;

                                        junction.VehicleService.AccountingPaymentTaskId =
                                            supplyPaymentTaskRepository.Add(junction.VehicleService.AccountingPaymentTask);

                                        messagesToSend.Add(new PaymentTaskMessage {
                                            Amount = junction.VehicleService.AccountingGrossPrice,
                                            Discount = Convert.ToDouble(junction.VehicleService.AccountingVat),
                                            CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                            PayToDate = junction.VehicleService.AccountingPaymentTask.PayToDate,
                                            OrganisationName = junction.VehicleService.VehicleOrganization.Name,
                                            PaymentForm = "Vehicle service"
                                        });
                                    } else {
                                        if (junction.VehicleService.AccountingPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                                            && !junction.VehicleService.AccountingPaymentTask.IsAvailableForPayment) {
                                            if (junction.VehicleService.AccountingPaymentTask.Deleted) {
                                                supplyPaymentTaskRepository.RemoveById(junction.VehicleService.AccountingPaymentTask.Id, updatedBy.Id);

                                                junction.VehicleService.AccountingPaymentTaskId = null;
                                            } else {
                                                junction.VehicleService.AccountingPaymentTask.PayToDate =
                                                    !junction.VehicleService.AccountingPaymentTask.PayToDate.HasValue
                                                        ? DateTime.UtcNow
                                                        : TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.AccountingPaymentTask.PayToDate.Value);

                                                junction.VehicleService.AccountingGrossPrice = junction.VehicleService.AccountingNetPrice;
                                                junction.VehicleService.AccountingPaymentTask.NetPrice = junction.VehicleService.AccountingNetPrice;
                                                junction.VehicleService.AccountingPaymentTask.GrossPrice = junction.VehicleService.AccountingNetPrice;

                                                supplyPaymentTaskRepository.Update(junction.VehicleService.AccountingPaymentTask);
                                            }
                                        }
                                    }
                                }

                                if (junction.VehicleService.ActProvidingServiceDocument != null) {
                                    if (junction.VehicleService.ActProvidingServiceDocument.IsNew()) {
                                        ActProvidingServiceDocument lastRecord =
                                            actProvidingServiceDocumentRepository.GetLastRecord();

                                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                            !string.IsNullOrEmpty(lastRecord.Number))
                                            junction.VehicleService.ActProvidingServiceDocument.Number =
                                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                                        else
                                            junction.VehicleService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                                        junction.VehicleService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                                            .New(junction.VehicleService.ActProvidingServiceDocument);
                                    } else if (junction.VehicleService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                                        junction.VehicleService.ActProvidingServiceDocumentId = null;
                                        actProvidingServiceDocumentRepository.RemoveById(junction.VehicleService.ActProvidingServiceDocument.Id);
                                    }
                                }

                                if (junction.VehicleService.SupplyServiceAccountDocument != null) {
                                    if (junction.VehicleService.SupplyServiceAccountDocument.IsNew()) {
                                        SupplyServiceAccountDocument lastRecord =
                                            supplyServiceAccountDocumentRepository.GetLastRecord();

                                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                            !string.IsNullOrEmpty(lastRecord.Number))
                                            junction.VehicleService.SupplyServiceAccountDocument.Number =
                                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                                        else
                                            junction.VehicleService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                                        junction.VehicleService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                                            .New(junction.VehicleService.SupplyServiceAccountDocument);
                                    } else if (junction.VehicleService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                                        junction.VehicleService.SupplyServiceAccountDocumentId = null;
                                        supplyServiceAccountDocumentRepository.RemoveById(junction.VehicleService.SupplyServiceAccountDocument.Id);
                                    }
                                }

                                junction.VehicleService.LoadDate = TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.LoadDate);

                                if (junction.VehicleService.FromDate.HasValue)
                                    junction.VehicleService.FromDate = TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.FromDate.Value);

                                vehicleServiceRepository.Update(junction.VehicleService);
                            }

                            invoiceDocumentRepository.Add(junction.VehicleService.InvoiceDocuments
                                .Where(d => d.IsNew())
                                .Select(d => {
                                    d.VehicleServiceId = junction.VehicleService.Id;

                                    return d;
                                })
                            );

                            junction.VehicleServiceId = junction.VehicleService.Id;
                            junction.SupplyOrderId = message.SupplyOrder.Id;

                            return junction;
                        })
                );

                supplyOrderVehicleServiceRepository.Update(
                    message.SupplyOrder.SupplyOrderVehicleServices
                        .Where(s => !s.IsNew() && s.VehicleService != null && !s.Deleted && !string.IsNullOrEmpty(s.VehicleService.VehicleNumber))
                        .Select(junction => {
                            VehicleService vehicleService = vehicleServiceRepository.GetByIdWithoutIncludes(junction.VehicleService.Id);

                            UpdateSupplyOrganizationAndAgreement(
                                supplyOrganizationAgreementRepository,
                                vehicleService.SupplyOrganizationAgreementId,
                                vehicleService.NetPrice,
                                vehicleService.AccountingNetPrice,
                                junction.VehicleService.SupplyOrganizationAgreement.Id,
                                junction.VehicleService.NetPrice,
                                junction.VehicleService.AccountingNetPrice);

                            junction.VehicleService.VehicleOrganizationId =
                                junction.VehicleService.VehicleOrganization.Id;
                            junction.VehicleService.SupplyOrganizationAgreementId =
                                junction.VehicleService.SupplyOrganizationAgreement.Id;

                            if (junction.VehicleService.IsNew()) {
                                if (junction.VehicleService.SupplyPaymentTask != null) {
                                    junction.VehicleService.SupplyPaymentTask.UserId = junction.VehicleService.SupplyPaymentTask.User.Id;
                                    junction.VehicleService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                                    junction.VehicleService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.VehicleService;

                                    junction.VehicleService.SupplyPaymentTask.PayToDate =
                                        !junction.VehicleService.SupplyPaymentTask.PayToDate.HasValue
                                            ? DateTime.UtcNow
                                            : TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.SupplyPaymentTask.PayToDate.Value);

                                    junction.VehicleService.GrossPrice = junction.VehicleService.NetPrice;
                                    junction.VehicleService.SupplyPaymentTask.NetPrice = junction.VehicleService.NetPrice;
                                    junction.VehicleService.SupplyPaymentTask.GrossPrice = junction.VehicleService.NetPrice;

                                    junction.VehicleService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(junction.VehicleService.SupplyPaymentTask);

                                    messagesToSend.Add(new PaymentTaskMessage {
                                        Amount = junction.VehicleService.GrossPrice,
                                        Discount = Convert.ToDouble(junction.VehicleService.Vat),
                                        CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                        PayToDate = junction.VehicleService.SupplyPaymentTask.PayToDate,
                                        OrganisationName = junction.VehicleService.VehicleOrganization.Name,
                                        PaymentForm = "Vehicle service"
                                    });
                                }

                                if (junction.VehicleService.AccountingPaymentTask != null) {
                                    junction.VehicleService.AccountingPaymentTask.UserId = junction.VehicleService.AccountingPaymentTask.User.Id;
                                    junction.VehicleService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                                    junction.VehicleService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.VehicleService;
                                    junction.VehicleService.AccountingPaymentTask.IsAccounting = true;

                                    junction.VehicleService.AccountingPaymentTask.PayToDate =
                                        !junction.VehicleService.AccountingPaymentTask.PayToDate.HasValue
                                            ? DateTime.UtcNow
                                            : TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.AccountingPaymentTask.PayToDate.Value);

                                    junction.VehicleService.AccountingGrossPrice = junction.VehicleService.AccountingNetPrice;
                                    junction.VehicleService.AccountingPaymentTask.NetPrice = junction.VehicleService.AccountingNetPrice;
                                    junction.VehicleService.AccountingPaymentTask.GrossPrice = junction.VehicleService.AccountingNetPrice;

                                    junction.VehicleService.AccountingPaymentTaskId = supplyPaymentTaskRepository.Add(junction.VehicleService.AccountingPaymentTask);

                                    messagesToSend.Add(new PaymentTaskMessage {
                                        Amount = junction.VehicleService.AccountingGrossPrice,
                                        Discount = Convert.ToDouble(junction.VehicleService.AccountingVat),
                                        CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                        PayToDate = junction.VehicleService.AccountingPaymentTask.PayToDate,
                                        OrganisationName = junction.VehicleService.VehicleOrganization.Name,
                                        PaymentForm = "Vehicle service"
                                    });
                                }

                                if (junction.VehicleService.SupplyInformationTask != null) {
                                    junction.VehicleService.SupplyInformationTask.UserId = updatedBy.Id;
                                    junction.VehicleService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                                    junction.VehicleService.AccountingSupplyCostsWithinCountry =
                                        junction.VehicleService.SupplyInformationTask.GrossPrice;

                                    junction.VehicleService.SupplyInformationTaskId =
                                        supplyInformationTaskRepository.Add(junction.VehicleService.SupplyInformationTask);
                                }

                                if (junction.VehicleService.ActProvidingServiceDocument != null) {
                                    if (junction.VehicleService.ActProvidingServiceDocument.IsNew()) {
                                        ActProvidingServiceDocument lastRecord =
                                            actProvidingServiceDocumentRepository.GetLastRecord();

                                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                            !string.IsNullOrEmpty(lastRecord.Number))
                                            junction.VehicleService.ActProvidingServiceDocument.Number =
                                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                                        else
                                            junction.VehicleService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                                        junction.VehicleService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                                            .New(junction.VehicleService.ActProvidingServiceDocument);
                                    } else if (junction.VehicleService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                                        junction.VehicleService.ActProvidingServiceDocumentId = null;
                                        actProvidingServiceDocumentRepository.RemoveById(junction.VehicleService.ActProvidingServiceDocument.Id);
                                    }
                                }

                                if (junction.VehicleService.SupplyServiceAccountDocument != null) {
                                    if (junction.VehicleService.SupplyServiceAccountDocument.IsNew()) {
                                        SupplyServiceAccountDocument lastRecord =
                                            supplyServiceAccountDocumentRepository.GetLastRecord();

                                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                            !string.IsNullOrEmpty(lastRecord.Number))
                                            junction.VehicleService.SupplyServiceAccountDocument.Number =
                                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                                        else
                                            junction.VehicleService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                                        junction.VehicleService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                                            .New(junction.VehicleService.SupplyServiceAccountDocument);
                                    } else if (junction.VehicleService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                                        junction.VehicleService.SupplyServiceAccountDocumentId = null;
                                        supplyServiceAccountDocumentRepository.RemoveById(junction.VehicleService.SupplyServiceAccountDocument.Id);
                                    }
                                }

                                SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                                if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                                    junction.VehicleService.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                                else
                                    junction.VehicleService.ServiceNumber = string.Format("P{0:D10}", 1);

                                supplyServiceNumberRepository.Add(junction.VehicleService.ServiceNumber);

                                if (junction.VehicleService.BillOfLadingDocument != null && junction.VehicleService.BillOfLadingDocument.IsNew()) {
                                    junction.VehicleService.BillOfLadingDocument.Date =
                                        TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.BillOfLadingDocument.Date);

                                    junction.VehicleService.BillOfLadingDocumentId =
                                        billOfLadingDocumentRepository.Add(junction.VehicleService.BillOfLadingDocument);
                                }

                                junction.VehicleService.VehicleOrganizationId = junction.VehicleService.VehicleOrganization?.Id;
                                junction.VehicleService.UserId = updatedBy.Id;

                                junction.VehicleService.LoadDate = TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.LoadDate);

                                if (junction.VehicleService.FromDate.HasValue)
                                    junction.VehicleService.FromDate = TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.FromDate.Value);

                                junction.VehicleService.Id = vehicleServiceRepository.Add(junction.VehicleService);
                            } else {
                                if (junction.VehicleService.SupplyPaymentTask != null) {
                                    if (junction.VehicleService.SupplyPaymentTask.IsNew()) {
                                        junction.VehicleService.SupplyPaymentTask.UserId = junction.VehicleService.SupplyPaymentTask.User.Id;
                                        junction.VehicleService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                                        junction.VehicleService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.VehicleService;

                                        junction.VehicleService.SupplyPaymentTask.PayToDate =
                                            !junction.VehicleService.SupplyPaymentTask.PayToDate.HasValue
                                                ? DateTime.UtcNow
                                                : TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.SupplyPaymentTask.PayToDate.Value);

                                        junction.VehicleService.GrossPrice = junction.VehicleService.NetPrice;
                                        junction.VehicleService.SupplyPaymentTask.NetPrice = junction.VehicleService.NetPrice;
                                        junction.VehicleService.SupplyPaymentTask.GrossPrice = junction.VehicleService.NetPrice;

                                        junction.VehicleService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(junction.VehicleService.SupplyPaymentTask);

                                        messagesToSend.Add(new PaymentTaskMessage {
                                            Amount = junction.VehicleService.GrossPrice,
                                            Discount = Convert.ToDouble(junction.VehicleService.Vat),
                                            CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                            PayToDate = junction.VehicleService.SupplyPaymentTask.PayToDate,
                                            OrganisationName = junction.VehicleService.VehicleOrganization.Name,
                                            PaymentForm = "Vehicle service"
                                        });
                                    } else {
                                        if (junction.VehicleService.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                                            && !junction.VehicleService.SupplyPaymentTask.IsAvailableForPayment) {
                                            if (junction.VehicleService.SupplyPaymentTask.Deleted) {
                                                supplyPaymentTaskRepository.RemoveById(junction.VehicleService.SupplyPaymentTask.Id, updatedBy.Id);

                                                junction.VehicleService.SupplyPaymentTaskId = null;
                                            } else {
                                                junction.VehicleService.SupplyPaymentTask.PayToDate =
                                                    !junction.VehicleService.SupplyPaymentTask.PayToDate.HasValue
                                                        ? DateTime.UtcNow
                                                        : TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.SupplyPaymentTask.PayToDate.Value);

                                                junction.VehicleService.GrossPrice = junction.VehicleService.NetPrice;
                                                junction.VehicleService.SupplyPaymentTask.NetPrice = junction.VehicleService.NetPrice;
                                                junction.VehicleService.SupplyPaymentTask.GrossPrice = junction.VehicleService.NetPrice;

                                                supplyPaymentTaskRepository.Update(junction.VehicleService.SupplyPaymentTask);
                                            }
                                        }
                                    }
                                }

                                if (junction.VehicleService.AccountingPaymentTask != null) {
                                    if (junction.VehicleService.AccountingPaymentTask.IsNew()) {
                                        junction.VehicleService.AccountingPaymentTask.UserId = junction.VehicleService.AccountingPaymentTask.User.Id;
                                        junction.VehicleService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                                        junction.VehicleService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.VehicleService;
                                        junction.VehicleService.AccountingPaymentTask.IsAccounting = true;

                                        junction.VehicleService.AccountingPaymentTask.PayToDate =
                                            !junction.VehicleService.AccountingPaymentTask.PayToDate.HasValue
                                                ? DateTime.UtcNow
                                                : TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.AccountingPaymentTask.PayToDate.Value);

                                        junction.VehicleService.AccountingGrossPrice = junction.VehicleService.AccountingNetPrice;
                                        junction.VehicleService.AccountingPaymentTask.NetPrice = junction.VehicleService.AccountingNetPrice;
                                        junction.VehicleService.AccountingPaymentTask.GrossPrice = junction.VehicleService.AccountingNetPrice;

                                        junction.VehicleService.AccountingPaymentTaskId =
                                            supplyPaymentTaskRepository.Add(junction.VehicleService.AccountingPaymentTask);

                                        messagesToSend.Add(new PaymentTaskMessage {
                                            Amount = junction.VehicleService.AccountingGrossPrice,
                                            Discount = Convert.ToDouble(junction.VehicleService.AccountingVat),
                                            CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                            PayToDate = junction.VehicleService.AccountingPaymentTask.PayToDate,
                                            OrganisationName = junction.VehicleService.VehicleOrganization.Name,
                                            PaymentForm = "Vehicle service"
                                        });
                                    } else {
                                        if (junction.VehicleService.AccountingPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                                            && !junction.VehicleService.AccountingPaymentTask.IsAvailableForPayment) {
                                            if (junction.VehicleService.AccountingPaymentTask.Deleted) {
                                                supplyPaymentTaskRepository.RemoveById(junction.VehicleService.AccountingPaymentTask.Id, updatedBy.Id);

                                                junction.VehicleService.AccountingPaymentTaskId = null;
                                            } else {
                                                junction.VehicleService.AccountingPaymentTask.PayToDate =
                                                    !junction.VehicleService.AccountingPaymentTask.PayToDate.HasValue
                                                        ? DateTime.UtcNow
                                                        : TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.AccountingPaymentTask.PayToDate.Value);

                                                junction.VehicleService.AccountingGrossPrice = junction.VehicleService.AccountingNetPrice;
                                                junction.VehicleService.AccountingPaymentTask.NetPrice = junction.VehicleService.AccountingNetPrice;
                                                junction.VehicleService.AccountingPaymentTask.GrossPrice = junction.VehicleService.AccountingNetPrice;

                                                supplyPaymentTaskRepository.Update(junction.VehicleService.AccountingPaymentTask);
                                            }
                                        }
                                    }
                                }

                                if (junction.VehicleService.SupplyInformationTask != null) {
                                    if (junction.VehicleService.SupplyInformationTask.IsNew()) {
                                        junction.VehicleService.SupplyInformationTask.UserId = updatedBy.Id;
                                        junction.VehicleService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                                        junction.VehicleService.AccountingSupplyCostsWithinCountry =
                                            junction.VehicleService.SupplyInformationTask.GrossPrice;

                                        junction.VehicleService.SupplyInformationTaskId =
                                            supplyInformationTaskRepository.Add(junction.VehicleService.SupplyInformationTask);
                                    } else {
                                        if (junction.VehicleService.SupplyInformationTask.Deleted) {
                                            junction.VehicleService.SupplyInformationTask.DeletedById = updatedBy.Id;

                                            supplyInformationTaskRepository.Remove(junction.VehicleService.SupplyInformationTask);

                                            junction.VehicleService.SupplyInformationTaskId = null;
                                        } else {
                                            junction.VehicleService.SupplyInformationTask.UpdatedById = updatedBy.Id;
                                            junction.VehicleService.SupplyInformationTask.UserId = updatedBy.Id;

                                            junction.VehicleService.AccountingSupplyCostsWithinCountry =
                                                junction.VehicleService.SupplyInformationTask.GrossPrice;

                                            supplyInformationTaskRepository.Update(junction.VehicleService.SupplyInformationTask);
                                        }
                                    }
                                }

                                if (junction.VehicleService.ActProvidingServiceDocument != null) {
                                    if (junction.VehicleService.ActProvidingServiceDocument.IsNew()) {
                                        ActProvidingServiceDocument lastRecord =
                                            actProvidingServiceDocumentRepository.GetLastRecord();

                                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                            !string.IsNullOrEmpty(lastRecord.Number))
                                            junction.VehicleService.ActProvidingServiceDocument.Number =
                                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                                        else
                                            junction.VehicleService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                                        junction.VehicleService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                                            .New(junction.VehicleService.ActProvidingServiceDocument);
                                    } else if (junction.VehicleService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                                        junction.VehicleService.ActProvidingServiceDocumentId = null;
                                        actProvidingServiceDocumentRepository.RemoveById(junction.VehicleService.ActProvidingServiceDocument.Id);
                                    }
                                }

                                if (junction.VehicleService.SupplyServiceAccountDocument != null) {
                                    if (junction.VehicleService.SupplyServiceAccountDocument.IsNew()) {
                                        SupplyServiceAccountDocument lastRecord =
                                            supplyServiceAccountDocumentRepository.GetLastRecord();

                                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                            !string.IsNullOrEmpty(lastRecord.Number))
                                            junction.VehicleService.SupplyServiceAccountDocument.Number =
                                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                                        else
                                            junction.VehicleService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                                        junction.VehicleService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                                            .New(junction.VehicleService.SupplyServiceAccountDocument);
                                    } else if (junction.VehicleService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                                        junction.VehicleService.SupplyServiceAccountDocumentId = null;
                                        supplyServiceAccountDocumentRepository.RemoveById(junction.VehicleService.SupplyServiceAccountDocument.Id);
                                    }
                                }

                                junction.VehicleService.LoadDate = TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.LoadDate);

                                if (junction.VehicleService.FromDate.HasValue)
                                    junction.VehicleService.FromDate = TimeZoneInfo.ConvertTimeToUtc(junction.VehicleService.FromDate.Value);

                                vehicleServiceRepository.Update(junction.VehicleService);
                            }

                            invoiceDocumentRepository.Add(junction.VehicleService.InvoiceDocuments
                                .Where(d => d.IsNew())
                                .Select(d => {
                                    d.VehicleServiceId = junction.VehicleService.Id;

                                    return d;
                                })
                            );

                            junction.VehicleServiceId = junction.VehicleService.Id;
                            junction.SupplyOrderId = message.SupplyOrder.Id;

                            return junction;
                        })
                );
            } else {
                supplyOrderVehicleServiceRepository.RemoveAllBySupplyOrderId(message.SupplyOrder.Id);
            }

            if (message.SupplyOrder.MergedServices.Any()) {
                foreach (MergedService service in message.SupplyOrder.MergedServices.Where(s => !s.IsNew() && s.Deleted)) {
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
                        supplyPaymentTaskRepository.RemoveById(service.SupplyPaymentTaskId.Value, updatedBy.Id);

                    if (service.AccountingPaymentTaskId.HasValue)
                        supplyPaymentTaskRepository.RemoveById(service.AccountingPaymentTaskId.Value, updatedBy.Id);

                    packingListPackageOrderItemSupplyServiceRepository.RemoveByServiceId(service.Id, TypeService.MergedService);
                }

                foreach (MergedService service in message.SupplyOrder.MergedServices.Where(s => !s.IsNew() && !s.Deleted))
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
                        .SupplyOrder
                        .MergedServices
                        .Where(s => s.IsNew() && !s.InvoiceDocuments.Any(d => d.IsNew()) && !s.ServiceDetailItems.Any() && !s.Deleted)
                        .Select(service => {
                            service.NetPrice = Math.Round(service.GrossPrice * 100 / Convert.ToDecimal(100 + service.VatPercent), 2);
                            service.AccountingNetPrice = Math.Round(service.AccountingGrossPrice * 100 / Convert.ToDecimal(100 + service.AccountingVatPercent), 2);
                            service.Vat = Math.Round(service.GrossPrice - service.NetPrice, 2);
                            service.AccountingVat = Math.Round(service.AccountingGrossPrice - service.AccountingNetPrice, 2);
                            service.SupplyOrganizationId = service.SupplyOrganization.Id;
                            service.SupplyOrderId = message.SupplyOrder.Id;
                            service.UserId = updatedBy.Id;
                            service.SupplyOrganizationAgreementId = service.SupplyOrganizationAgreement.Id;

                            if (service.FromDate.HasValue) service.FromDate = TimeZoneInfo.ConvertTimeToUtc(service.FromDate.Value);

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

                                messagesToSend.Add(new PaymentTaskMessage {
                                    Amount = service.GrossPrice,
                                    Discount = Convert.ToDouble(service.Vat),
                                    CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                    PayToDate = service.SupplyPaymentTask.PayToDate,
                                    OrganisationName = service.SupplyOrganization.Name,
                                    PaymentForm = "Merged service"
                                });
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

                                messagesToSend.Add(new PaymentTaskMessage {
                                    Amount = service.AccountingGrossPrice,
                                    Discount = Convert.ToDouble(service.AccountingVat),
                                    CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                    PayToDate = service.AccountingPaymentTask.PayToDate,
                                    OrganisationName = service.SupplyOrganization.Name,
                                    PaymentForm = "Merged service"
                                });
                            }

                            if (service.SupplyInformationTask != null) {
                                service.SupplyInformationTask.UserId = updatedBy.Id;
                                service.SupplyInformationTask.UpdatedById = updatedBy.Id;

                                service.AccountingSupplyCostsWithinCountry =
                                    service.SupplyInformationTask.GrossPrice;

                                service.SupplyInformationTaskId =
                                    supplyInformationTaskRepository.Add(service.SupplyInformationTask);
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

                foreach (MergedService service in message.SupplyOrder.MergedServices.Where(s => !s.IsNew() && !s.Deleted)) {
                    service.NetPrice = Math.Round(service.GrossPrice * 100 / Convert.ToDecimal(100 + service.VatPercent), 2);
                    service.Vat = Math.Round(service.GrossPrice - service.NetPrice, 2);
                    service.AccountingNetPrice = Math.Round(service.AccountingGrossPrice * 100 / Convert.ToDecimal(100 + service.AccountingVatPercent), 2);
                    service.AccountingVat = Math.Round(service.AccountingGrossPrice - service.AccountingNetPrice, 2);

                    if (service.FromDate.HasValue) service.FromDate = TimeZoneInfo.ConvertTimeToUtc(service.FromDate.Value);

                    if (service.SupplyPaymentTask != null) {
                        if (service.SupplyPaymentTask.IsNew()) {
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
                        } else {
                            if (service.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                                && !service.SupplyPaymentTask.IsAvailableForPayment) {
                                if (service.SupplyPaymentTask.Deleted) {
                                    supplyPaymentTaskRepository.RemoveById(service.SupplyPaymentTask.Id, updatedBy.Id);

                                    service.SupplyPaymentTaskId = null;
                                } else {
                                    service.SupplyPaymentTask.PayToDate =
                                        !service.SupplyPaymentTask.PayToDate.HasValue
                                            ? DateTime.UtcNow
                                            : TimeZoneInfo.ConvertTimeToUtc(service.SupplyPaymentTask.PayToDate.Value);

                                    service.SupplyPaymentTask.NetPrice = service.NetPrice;
                                    service.SupplyPaymentTask.GrossPrice = service.GrossPrice;

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

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = service.GrossPrice,
                            Discount = Convert.ToDouble(service.Vat),
                            CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                            PayToDate = service.SupplyPaymentTask.PayToDate,
                            OrganisationName = service.SupplyOrganization.Name,
                            PaymentForm = "Merged service"
                        });
                    }

                    if (service.AccountingPaymentTask != null) {
                        if (service.AccountingPaymentTask.IsNew()) {
                            service.AccountingPaymentTask.UserId = service.SupplyPaymentTask.User.Id;
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
                        } else {
                            if (service.AccountingPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                                && !service.AccountingPaymentTask.IsAvailableForPayment) {
                                if (service.AccountingPaymentTask.Deleted) {
                                    supplyPaymentTaskRepository.RemoveById(service.AccountingPaymentTask.Id, updatedBy.Id);

                                    service.AccountingPaymentTaskId = null;
                                } else {
                                    service.AccountingPaymentTask.PayToDate =
                                        !service.AccountingPaymentTask.PayToDate.HasValue
                                            ? DateTime.UtcNow
                                            : TimeZoneInfo.ConvertTimeToUtc(service.AccountingPaymentTask.PayToDate.Value);

                                    service.AccountingPaymentTask.NetPrice = service.AccountingNetPrice;
                                    service.AccountingPaymentTask.GrossPrice = service.AccountingGrossPrice;

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

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = service.GrossPrice,
                            Discount = Convert.ToDouble(service.Vat),
                            CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                            PayToDate = service.AccountingPaymentTask.PayToDate,
                            OrganisationName = service.SupplyOrganization.Name,
                            PaymentForm = "Merged service"
                        });
                    }

                    if (service.SupplyInformationTask != null) {
                        if (service.SupplyInformationTask.IsNew()) {
                            service.SupplyInformationTask.UserId = updatedBy.Id;
                            service.SupplyInformationTask.UpdatedById = updatedBy.Id;

                            service.AccountingSupplyCostsWithinCountry =
                                service.SupplyInformationTask.GrossPrice;

                            service.SupplyInformationTaskId =
                                supplyInformationTaskRepository.Add(service.SupplyInformationTask);
                        } else {
                            if (service.SupplyInformationTask.Deleted) {
                                service.SupplyInformationTask.DeletedById = updatedBy.Id;

                                supplyInformationTaskRepository.Remove(service.SupplyInformationTask);

                                service.SupplyInformationTaskId = null;
                            } else {
                                service.SupplyInformationTask.UpdatedById = updatedBy.Id;
                                service.SupplyInformationTask.UserId = updatedBy.Id;

                                service.AccountingSupplyCostsWithinCountry =
                                    service.SupplyInformationTask.GrossPrice;

                                supplyInformationTaskRepository.Update(service.SupplyInformationTask);
                            }
                        }
                    }

                    if (service.ActProvidingServiceDocument != null) {
                        if (service.ActProvidingServiceDocument.IsNew()) {
                            ActProvidingServiceDocument lastRecord =
                                actProvidingServiceDocumentRepository.GetLastRecord();

                            if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                !string.IsNullOrEmpty(lastRecord.Number))
                                service.ActProvidingServiceDocument.Number =
                                    string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                            else
                                service.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                            service.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                                .New(service.ActProvidingServiceDocument);
                        } else if (service.ActProvidingServiceDocument.Deleted.Equals(true)) {
                            service.ActProvidingServiceDocumentId = null;
                            actProvidingServiceDocumentRepository.RemoveById(service.ActProvidingServiceDocument.Id);
                        }
                    }

                    if (service.SupplyServiceAccountDocument != null) {
                        if (service.SupplyServiceAccountDocument.IsNew()) {
                            SupplyServiceAccountDocument lastRecord =
                                supplyServiceAccountDocumentRepository.GetLastRecord();

                            if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                !string.IsNullOrEmpty(lastRecord.Number))
                                service.SupplyServiceAccountDocument.Number =
                                    string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                            else
                                service.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                            service.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                                .New(service.SupplyServiceAccountDocument);
                        } else if (service.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                            service.SupplyServiceAccountDocumentId = null;
                            supplyServiceAccountDocumentRepository.RemoveById(service.SupplyServiceAccountDocument.Id);
                        }
                    }

                    if (service.InvoiceDocuments.Any(d => d.IsNew()))
                        invoiceDocumentRepository.Add(service.InvoiceDocuments
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

                foreach (MergedService service in message.SupplyOrder.MergedServices.Where(s => s.IsNew() &&
                                                                                                s.InvoiceDocuments.Any(d => d.IsNew())
                                                                                                && !s.Deleted)) {
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

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = service.GrossPrice,
                            Discount = Convert.ToDouble(service.Vat),
                            CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                            PayToDate = service.SupplyPaymentTask.PayToDate,
                            OrganisationName = service.SupplyOrganization.Name,
                            PaymentForm = "Merged service"
                        });
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

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = service.AccountingGrossPrice,
                            Discount = Convert.ToDouble(service.AccountingVat),
                            CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                            PayToDate = service.AccountingPaymentTask.PayToDate,
                            OrganisationName = service.SupplyOrganization.Name,
                            PaymentForm = "Merged service"
                        });
                    }

                    if (service.SupplyInformationTask != null) {
                        service.SupplyInformationTask.UserId = updatedBy.Id;
                        service.SupplyInformationTask.UpdatedById = updatedBy.Id;

                        service.AccountingSupplyCostsWithinCountry =
                            service.SupplyInformationTask.GrossPrice;

                        service.SupplyInformationTaskId =
                            supplyInformationTaskRepository.Add(service.SupplyInformationTask);
                    }

                    if (service.ActProvidingServiceDocument != null) {
                        if (service.ActProvidingServiceDocument.IsNew()) {
                            ActProvidingServiceDocument lastRecord =
                                actProvidingServiceDocumentRepository.GetLastRecord();

                            if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                !string.IsNullOrEmpty(lastRecord.Number))
                                service.ActProvidingServiceDocument.Number =
                                    string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                            else
                                service.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                            service.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                                .New(service.ActProvidingServiceDocument);
                        } else if (service.ActProvidingServiceDocument.Deleted.Equals(true)) {
                            service.ActProvidingServiceDocumentId = null;
                            actProvidingServiceDocumentRepository.RemoveById(service.ActProvidingServiceDocument.Id);
                        }
                    }

                    if (service.SupplyServiceAccountDocument != null) {
                        if (service.SupplyServiceAccountDocument.IsNew()) {
                            SupplyServiceAccountDocument lastRecord =
                                supplyServiceAccountDocumentRepository.GetLastRecord();

                            if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                !string.IsNullOrEmpty(lastRecord.Number))
                                service.SupplyServiceAccountDocument.Number =
                                    string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                            else
                                service.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                            service.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                                .New(service.SupplyServiceAccountDocument);
                        } else if (service.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                            service.SupplyServiceAccountDocumentId = null;
                            supplyServiceAccountDocumentRepository.RemoveById(service.SupplyServiceAccountDocument.Id);
                        }
                    }

                    service.NetPrice = Math.Round(service.GrossPrice * 100 / Convert.ToDecimal(100 + service.VatPercent), 2);
                    service.Vat = Math.Round(service.GrossPrice - service.NetPrice, 2);
                    service.SupplyOrganizationId = service.SupplyOrganization.Id;
                    service.SupplyOrderId = message.SupplyOrder.Id;
                    service.UserId = updatedBy.Id;
                    service.SupplyOrganizationAgreementId = service.SupplyOrganizationAgreement.Id;

                    if (service.FromDate.HasValue) service.FromDate = TimeZoneInfo.ConvertTimeToUtc(service.FromDate.Value);

                    service.Id = mergedServiceRepository.Add(service);

                    if (service.SupplyOrganizationAgreement != null && !service.SupplyOrganizationAgreement.IsNew()) {
                        service.SupplyOrganizationAgreement = supplyOrganizationAgreementRepository.GetById(service.SupplyOrganizationAgreement.Id);

                        service.SupplyOrganizationAgreement.CurrentAmount =
                            Math.Round(service.SupplyOrganizationAgreement.CurrentAmount - service.GrossPrice, 2);

                        service.SupplyOrganizationAgreement.AccountingCurrentAmount =
                            Math.Round(service.SupplyOrganizationAgreement.AccountingCurrentAmount - service.AccountingGrossPrice, 2);

                        supplyOrganizationAgreementRepository.UpdateCurrentAmount(service.SupplyOrganizationAgreement);
                    }

                    invoiceDocumentRepository.Add(service.InvoiceDocuments
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

            if (message.SupplyOrder.CustomServices.Any()) {
                foreach (CustomService service in message.SupplyOrder.CustomServices.Where(s => !s.IsNew()))
                    if (service.InvoiceDocuments.Any())
                        invoiceDocumentRepository.RemoveAllByCustomServiceIdExceptProvided(service.Id,
                            service.InvoiceDocuments.Where(d => !d.IsNew() && !d.Deleted).Select(d => d.Id));
                    else
                        invoiceDocumentRepository.RemoveAllByCustomServiceId(service.Id);

                customServiceRepository.Add(
                    message
                        .SupplyOrder
                        .CustomServices
                        .Where(s => s.IsNew() && !s.InvoiceDocuments.Any(d => d.IsNew()) && !s.ServiceDetailItems.Any())
                        .Select(service => {
                            service.NetPrice = Math.Round(service.GrossPrice * 100 / Convert.ToDecimal(100 + service.VatPercent), 2);
                            service.AccountingNetPrice = Math.Round(service.AccountingGrossPrice * 100 / Convert.ToDecimal(100 + service.AccountingVatPercent), 2);
                            service.Vat = Math.Round(service.GrossPrice - service.NetPrice, 2);
                            service.AccountingVat = Math.Round(service.AccountingGrossPrice - service.AccountingNetPrice, 2);
                            service.CustomOrganizationId = service.CustomOrganization?.Id;
                            service.ExciseDutyOrganizationId = service.ExciseDutyOrganization?.Id;
                            service.SupplyOrderId = message.SupplyOrder.Id;
                            service.UserId = updatedBy.Id;
                            service.SupplyOrganizationAgreementId = service.SupplyOrganizationAgreement.Id;

                            if (service.FromDate.HasValue) service.FromDate = TimeZoneInfo.ConvertTimeToUtc(service.FromDate.Value);

                            if (service.SupplyPaymentTask != null) {
                                service.SupplyPaymentTask.UserId = service.SupplyPaymentTask.User.Id;
                                service.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                                service.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomService;

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

                                messagesToSend.Add(new PaymentTaskMessage {
                                    Amount = service.GrossPrice,
                                    Discount = Convert.ToDouble(service.Vat),
                                    CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                    PayToDate = service.SupplyPaymentTask.PayToDate,
                                    OrganisationName = service.CustomOrganization != null
                                        ? service.CustomOrganization.Name
                                        : service.ExciseDutyOrganization != null
                                            ? service.ExciseDutyOrganization.Name
                                            : string.Empty,
                                    PaymentForm = "Custom service"
                                });
                            }

                            if (service.AccountingPaymentTask != null) {
                                service.AccountingPaymentTask.UserId = service.AccountingPaymentTask.User.Id;
                                service.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                                service.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomService;
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

                                messagesToSend.Add(new PaymentTaskMessage {
                                    Amount = service.AccountingGrossPrice,
                                    Discount = Convert.ToDouble(service.AccountingVat),
                                    CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                                    PayToDate = service.AccountingPaymentTask.PayToDate,
                                    OrganisationName = service.CustomOrganization != null
                                        ? service.CustomOrganization.Name
                                        : service.ExciseDutyOrganization != null
                                            ? service.ExciseDutyOrganization.Name
                                            : string.Empty,
                                    PaymentForm = "Custom service"
                                });
                            }

                            if (service.SupplyInformationTask != null) {
                                service.SupplyInformationTask.UserId = updatedBy.Id;
                                service.SupplyInformationTask.UpdatedById = updatedBy.Id;

                                service.AccountingSupplyCostsWithinCountry =
                                    service.SupplyInformationTask.GrossPrice;

                                service.SupplyInformationTaskId =
                                    supplyInformationTaskRepository.Add(service.SupplyInformationTask);
                            }

                            if (service.SupplyOrganizationAgreement != null && !service.SupplyOrganizationAgreement.IsNew()) {
                                service.SupplyOrganizationAgreement = supplyOrganizationAgreementRepository.GetById(service.SupplyOrganizationAgreement.Id);

                                service.SupplyOrganizationAgreement.CurrentAmount =
                                    Math.Round(service.SupplyOrganizationAgreement.CurrentAmount - service.GrossPrice, 2);

                                service.SupplyOrganizationAgreement.AccountingCurrentAmount =
                                    Math.Round(service.SupplyOrganizationAgreement.AccountingCurrentAmount - service.AccountingGrossPrice, 2);

                                supplyOrganizationAgreementRepository.UpdateCurrentAmount(service.SupplyOrganizationAgreement);
                            }

                            if (service.SupplyOrganizationAgreement == null || service.SupplyOrganizationAgreement.IsNew()) return service;

                            return service;
                        })
                );

                foreach (CustomService service in message.SupplyOrder.CustomServices.Where(s => !s.IsNew())) {
                    CustomService existCustomService = customServiceRepository.GetByIdWithoutIncludes(service.Id);

                    UpdateSupplyOrganizationAndAgreement(
                        supplyOrganizationAgreementRepository,
                        service.SupplyOrganizationAgreementId,
                        existCustomService.GrossPrice,
                        existCustomService.AccountingGrossPrice,
                        service.SupplyOrganizationAgreement.Id,
                        service.GrossPrice,
                        service.AccountingGrossPrice);

                    if (service.SupplyCustomType.Equals(SupplyCustomType.Custom))
                        service.CustomOrganizationId =
                            service.CustomOrganization.Id;
                    else
                        service.ExciseDutyOrganizationId =
                            service.ExciseDutyOrganization.Id;

                    service.SupplyOrganizationAgreementId =
                        service.SupplyOrganizationAgreement.Id;

                    service.NetPrice = Math.Round(service.GrossPrice * 100 / Convert.ToDecimal(100 + service.VatPercent), 2);
                    service.Vat = Math.Round(service.GrossPrice - service.NetPrice, 2);
                    service.AccountingNetPrice = Math.Round(service.AccountingGrossPrice * 100 / Convert.ToDecimal(100 + service.AccountingVatPercent), 2);
                    service.AccountingVat = Math.Round(service.AccountingGrossPrice - service.AccountingNetPrice, 2);

                    if (service.FromDate.HasValue) service.FromDate = TimeZoneInfo.ConvertTimeToUtc(service.FromDate.Value);

                    if (service.SupplyPaymentTask != null) {
                        if (service.SupplyPaymentTask.IsNew()) {
                            service.SupplyPaymentTask.UserId = service.SupplyPaymentTask.User.Id;
                            service.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                            service.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomService;

                            service.SupplyPaymentTask.PayToDate =
                                !service.SupplyPaymentTask.PayToDate.HasValue
                                    ? DateTime.UtcNow
                                    : TimeZoneInfo.ConvertTimeToUtc(service.SupplyPaymentTask.PayToDate.Value);

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
                                    service.SupplyPaymentTask.PayToDate =
                                        !service.SupplyPaymentTask.PayToDate.HasValue
                                            ? DateTime.UtcNow
                                            : TimeZoneInfo.ConvertTimeToUtc(service.SupplyPaymentTask.PayToDate.Value);

                                    service.SupplyPaymentTask.NetPrice = service.NetPrice;
                                    service.SupplyPaymentTask.GrossPrice = service.GrossPrice;

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

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = service.GrossPrice,
                            Discount = Convert.ToDouble(service.Vat),
                            CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                            PayToDate = service.SupplyPaymentTask.PayToDate,
                            OrganisationName = service.CustomOrganization != null ? service.CustomOrganization.Name : service.ExciseDutyOrganization.Name,
                            PaymentForm = "Custom service"
                        });
                    }

                    if (service.AccountingPaymentTask != null) {
                        if (service.AccountingPaymentTask.IsNew()) {
                            service.AccountingPaymentTask.UserId = service.AccountingPaymentTask.User.Id;
                            service.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                            service.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomService;
                            service.AccountingPaymentTask.IsAccounting = true;

                            service.AccountingPaymentTask.PayToDate =
                                !service.AccountingPaymentTask.PayToDate.HasValue
                                    ? DateTime.UtcNow
                                    : TimeZoneInfo.ConvertTimeToUtc(service.AccountingPaymentTask.PayToDate.Value);

                            service.AccountingPaymentTask.NetPrice = service.AccountingNetPrice;
                            service.AccountingPaymentTask.GrossPrice = service.AccountingGrossPrice;

                            service.AccountingPaymentTaskId = supplyPaymentTaskRepository.Add(service.AccountingPaymentTask);
                        } else {
                            if (service.AccountingPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                                && !service.AccountingPaymentTask.IsAvailableForPayment) {
                                if (service.AccountingPaymentTask.Deleted) {
                                    supplyPaymentTaskRepository.RemoveById(service.AccountingPaymentTask.Id, updatedBy.Id);

                                    service.AccountingPaymentTaskId = null;
                                } else {
                                    service.AccountingPaymentTask.PayToDate =
                                        !service.AccountingPaymentTask.PayToDate.HasValue
                                            ? DateTime.UtcNow
                                            : TimeZoneInfo.ConvertTimeToUtc(service.AccountingPaymentTask.PayToDate.Value);

                                    service.AccountingPaymentTask.NetPrice = service.AccountingNetPrice;
                                    service.AccountingPaymentTask.GrossPrice = service.AccountingGrossPrice;

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

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = service.GrossPrice,
                            Discount = Convert.ToDouble(service.Vat),
                            CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                            PayToDate = service.SupplyPaymentTask.PayToDate,
                            OrganisationName = service.CustomOrganization != null ? service.CustomOrganization.Name : service.ExciseDutyOrganization.Name,
                            PaymentForm = "Custom service"
                        });
                    }

                    if (service.SupplyInformationTask != null) {
                        if (service.SupplyInformationTask.IsNew()) {
                            service.SupplyInformationTask.UserId = updatedBy.Id;
                            service.SupplyInformationTask.UpdatedById = updatedBy.Id;

                            service.AccountingSupplyCostsWithinCountry =
                                service.SupplyInformationTask.GrossPrice;

                            service.SupplyInformationTaskId =
                                supplyInformationTaskRepository.Add(service.SupplyInformationTask);
                        } else {
                            if (service.SupplyInformationTask.Deleted) {
                                service.SupplyInformationTask.DeletedById = updatedBy.Id;

                                supplyInformationTaskRepository.Remove(service.SupplyInformationTask);

                                service.SupplyInformationTaskId = null;
                            } else {
                                service.SupplyInformationTask.UpdatedById = updatedBy.Id;
                                service.SupplyInformationTask.UserId = updatedBy.Id;

                                service.AccountingSupplyCostsWithinCountry =
                                    service.SupplyInformationTask.GrossPrice;

                                supplyInformationTaskRepository.Update(service.SupplyInformationTask);
                            }
                        }
                    }

                    if (service.ActProvidingServiceDocument != null) {
                        if (service.ActProvidingServiceDocument.IsNew()) {
                            ActProvidingServiceDocument lastRecord =
                                actProvidingServiceDocumentRepository.GetLastRecord();

                            if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                !string.IsNullOrEmpty(lastRecord.Number))
                                service.ActProvidingServiceDocument.Number =
                                    string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                            else
                                service.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                            service.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                                .New(service.ActProvidingServiceDocument);
                        } else if (service.ActProvidingServiceDocument.Deleted.Equals(true)) {
                            service.ActProvidingServiceDocumentId = null;
                            actProvidingServiceDocumentRepository.RemoveById(service.ActProvidingServiceDocument.Id);
                        }
                    }

                    if (service.SupplyServiceAccountDocument != null) {
                        if (service.SupplyServiceAccountDocument.IsNew()) {
                            SupplyServiceAccountDocument lastRecord =
                                supplyServiceAccountDocumentRepository.GetLastRecord();

                            if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                !string.IsNullOrEmpty(lastRecord.Number))
                                service.SupplyServiceAccountDocument.Number =
                                    string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                            else
                                service.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                            service.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                                .New(service.SupplyServiceAccountDocument);
                        } else if (service.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                            service.SupplyServiceAccountDocumentId = null;
                            supplyServiceAccountDocumentRepository.RemoveById(service.SupplyServiceAccountDocument.Id);
                        }
                    }

                    if (service.InvoiceDocuments.Any(d => d.IsNew()))
                        invoiceDocumentRepository.Add(service.InvoiceDocuments
                            .Where(d => d.IsNew())
                            .Select(d => {
                                d.CustomServiceId = service.Id;

                                return d;
                            })
                        );

                    if (service.ServiceDetailItems.Any()) {
                        serviceDetailItemRepository.RemoveAllByCustomServiceIdExceptProvided(
                            service.Id,
                            service.ServiceDetailItems.Where(i => !i.IsNew()).Select(i => i.Id)
                        );

                        InsertOrUpdateServiceDetailItems(
                            serviceDetailItemRepository,
                            serviceDetailItemKeyRepository,
                            service.ServiceDetailItems
                                .Select(i => {
                                    i.CustomServiceId = service.Id;

                                    return i;
                                })
                        );
                    } else {
                        serviceDetailItemRepository.RemoveAllByCustomServiceId(service.Id);
                    }

                    customServiceRepository.Update(service);
                }

                foreach (CustomService service in message.SupplyOrder.CustomServices.Where(s => s.IsNew() && s.InvoiceDocuments.Any(d => d.IsNew()))) {
                    service.NetPrice = Math.Round(service.GrossPrice * 100 / Convert.ToDecimal(100 + service.VatPercent), 2);
                    service.AccountingNetPrice = Math.Round(service.AccountingGrossPrice * 100 / Convert.ToDecimal(100 + service.AccountingVatPercent), 2);
                    service.Vat = Math.Round(service.GrossPrice - service.NetPrice, 2);
                    service.AccountingVat = Math.Round(service.AccountingGrossPrice - service.AccountingNetPrice, 2);
                    service.CustomOrganizationId = service.CustomOrganization?.Id;
                    service.ExciseDutyOrganizationId = service.ExciseDutyOrganization?.Id;
                    service.SupplyOrderId = message.SupplyOrder.Id;
                    service.UserId = updatedBy.Id;
                    service.SupplyOrganizationAgreementId = service.SupplyOrganizationAgreement.Id;

                    if (service.SupplyPaymentTask != null) {
                        service.SupplyPaymentTask.UserId = service.SupplyPaymentTask.User.Id;
                        service.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                        service.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomService;

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

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = service.GrossPrice,
                            Discount = Convert.ToDouble(service.Vat),
                            CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                            PayToDate = service.SupplyPaymentTask.PayToDate,
                            OrganisationName = service.CustomOrganization != null ? service.CustomOrganization.Name : service.ExciseDutyOrganization.Name,
                            PaymentForm = "Custom service"
                        });
                    }

                    if (service.AccountingPaymentTask != null) {
                        service.AccountingPaymentTask.UserId = service.AccountingPaymentTask.User.Id;
                        service.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                        service.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomService;
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

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = service.AccountingGrossPrice,
                            Discount = Convert.ToDouble(service.AccountingVat),
                            CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}",
                            PayToDate = service.AccountingPaymentTask.PayToDate,
                            OrganisationName = service.CustomOrganization != null ? service.CustomOrganization.Name : service.ExciseDutyOrganization.Name,
                            PaymentForm = "Custom service"
                        });
                    }

                    if (service.SupplyInformationTask != null) {
                        service.SupplyInformationTask.UserId = updatedBy.Id;
                        service.SupplyInformationTask.UpdatedById = updatedBy.Id;

                        service.AccountingSupplyCostsWithinCountry =
                            service.SupplyInformationTask.GrossPrice;

                        service.SupplyInformationTaskId =
                            supplyInformationTaskRepository.Add(service.SupplyInformationTask);
                    }

                    if (service.ActProvidingServiceDocument != null) {
                        if (service.ActProvidingServiceDocument.IsNew()) {
                            ActProvidingServiceDocument lastRecord =
                                actProvidingServiceDocumentRepository.GetLastRecord();

                            if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                !string.IsNullOrEmpty(lastRecord.Number))
                                service.ActProvidingServiceDocument.Number =
                                    string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                            else
                                service.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                            service.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                                .New(service.ActProvidingServiceDocument);
                        } else if (service.ActProvidingServiceDocument.Deleted.Equals(true)) {
                            service.ActProvidingServiceDocumentId = null;
                            actProvidingServiceDocumentRepository.RemoveById(service.ActProvidingServiceDocument.Id);
                        }
                    }

                    if (service.SupplyServiceAccountDocument != null) {
                        if (service.SupplyServiceAccountDocument.IsNew()) {
                            SupplyServiceAccountDocument lastRecord =
                                supplyServiceAccountDocumentRepository.GetLastRecord();

                            if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                !string.IsNullOrEmpty(lastRecord.Number))
                                service.SupplyServiceAccountDocument.Number =
                                    string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                            else
                                service.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                            service.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                                .New(service.SupplyServiceAccountDocument);
                        } else if (service.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                            service.SupplyServiceAccountDocumentId = null;
                            supplyServiceAccountDocumentRepository.RemoveById(service.SupplyServiceAccountDocument.Id);
                        }
                    }

                    if (service.SupplyOrganizationAgreement != null && !service.SupplyOrganizationAgreement.IsNew()) {
                        service.SupplyOrganizationAgreement = supplyOrganizationAgreementRepository.GetById(service.SupplyOrganizationAgreement.Id);

                        service.SupplyOrganizationAgreement.CurrentAmount =
                            Math.Round(service.SupplyOrganizationAgreement.CurrentAmount - service.GrossPrice, 2);

                        service.SupplyOrganizationAgreement.AccountingCurrentAmount =
                            Math.Round(service.SupplyOrganizationAgreement.AccountingCurrentAmount - service.AccountingGrossPrice, 2);

                        supplyOrganizationAgreementRepository.UpdateCurrentAmount(service.SupplyOrganizationAgreement);
                    }

                    if (service.FromDate.HasValue) service.FromDate = TimeZoneInfo.ConvertTimeToUtc(service.FromDate.Value);

                    service.Id = customServiceRepository.Add(service);

                    invoiceDocumentRepository.Add(service.InvoiceDocuments
                        .Where(d => d.IsNew())
                        .Select(d => {
                            d.CustomServiceId = service.Id;

                            return d;
                        })
                    );

                    if (service.ServiceDetailItems.Any())
                        InsertOrUpdateServiceDetailItems(
                            serviceDetailItemRepository,
                            serviceDetailItemKeyRepository,
                            service.ServiceDetailItems
                                .Select(i => {
                                    i.CustomServiceId = service.Id;

                                    return i;
                                })
                        );
                }
            }

            if (message.SupplyOrder.SupplyOrderPolandPaymentDeliveryProtocols.Any(p => p.SupplyPaymentTask != null && !p.SupplyPaymentTask.IsNew()))
                foreach (SupplyOrderPolandPaymentDeliveryProtocol protocol in message
                             .SupplyOrder
                             .SupplyOrderPolandPaymentDeliveryProtocols
                             .Where(p => p.SupplyPaymentTask != null && !p.SupplyPaymentTask.IsNew()))
                    if (protocol.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone) && !protocol.SupplyPaymentTask.IsAvailableForPayment) {
                        if (protocol.SupplyPaymentTask.Deleted) {
                            supplyPaymentTaskRepository.RemoveById(protocol.SupplyPaymentTask.Id, updatedBy.Id);

                            supplyOrderPolandPaymentDeliveryProtocolRepository.Remove(protocol.Id);
                        } else {
                            protocol.SupplyPaymentTask.UpdatedById = updatedBy.Id;

                            if (protocol.SupplyPaymentTask.PayToDate.HasValue)
                                protocol.SupplyPaymentTask.PayToDate = TimeZoneInfo.ConvertTimeToUtc(protocol.SupplyPaymentTask.PayToDate.Value);

                            protocol.SupplyPaymentTask.IsAccounting = protocol.IsAccounting;

                            supplyPaymentTaskRepository.Update(protocol.SupplyPaymentTask);
                        }
                    }

            if (informationMessage == null) informationMessage = new InformationMessage();

            switch (message.SupplyOrder.TransportationType) {
                case SupplyTransportationType.Vehicle:
                    CreateOrUpdateVehicleServices(
                        message,
                        _supplyRepositoriesFactory,
                        connection,
                        supplyPaymentTaskRepository,
                        invoiceDocumentRepository,
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        supplyServiceNumberRepository,
                        supplyOrganizationAgreementRepository,
                        messagesToSend,
                        informationMessage,
                        updatedBy,
                        updatedBy,
                        supplyInformationTaskRepository,
                        actProvidingServiceDocumentRepository,
                        supplyServiceAccountDocumentRepository);

                    break;
                case SupplyTransportationType.Ship:
                    CreateOrUpdateShipServices(
                        message,
                        _supplyRepositoriesFactory,
                        connection,
                        supplyPaymentTaskRepository,
                        invoiceDocumentRepository,
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        supplyServiceNumberRepository,
                        supplyOrganizationAgreementRepository,
                        messagesToSend,
                        informationMessage,
                        updatedBy,
                        updatedBy,
                        supplyInformationTaskRepository,
                        actProvidingServiceDocumentRepository,
                        supplyServiceAccountDocumentRepository);

                    break;
                case SupplyTransportationType.Plane:
                    CreateOrUpdatePlaneServices(
                        message,
                        _supplyRepositoriesFactory,
                        connection,
                        supplyPaymentTaskRepository,
                        invoiceDocumentRepository,
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        supplyServiceNumberRepository,
                        supplyOrganizationAgreementRepository,
                        messagesToSend,
                        informationMessage,
                        updatedBy,
                        updatedBy,
                        supplyInformationTaskRepository,
                        actProvidingServiceDocumentRepository,
                        supplyServiceAccountDocumentRepository);

                    break;
            }

            if (message.SupplyOrder.OrderShippedDate.HasValue) message.SupplyOrder.OrderShippedDate = TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.OrderShippedDate.Value);

            if (message.SupplyOrder.DateFrom.HasValue) message.SupplyOrder.DateFrom = TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.DateFrom.Value);

            if (message.SupplyOrder.CompleteDate.HasValue) message.SupplyOrder.CompleteDate = TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CompleteDate.Value);

            if (message.SupplyOrder.OrderArrivedDate.HasValue) {
                message.SupplyOrder.OrderArrivedDate = TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.OrderArrivedDate.Value);

                message.SupplyOrder.VechicalArrived = message.SupplyOrder.OrderArrivedDate;
                message.SupplyOrder.ShipArrived = message.SupplyOrder.OrderArrivedDate;
                message.SupplyOrder.PlaneArrived = message.SupplyOrder.OrderArrivedDate;
            }

            if (message.SupplyOrder.ShipArrived.HasValue) message.SupplyOrder.ShipArrived = TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.ShipArrived.Value);

            if (message.SupplyOrder.VechicalArrived.HasValue) message.SupplyOrder.VechicalArrived = TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.VechicalArrived.Value);

            if (message.SupplyOrder.PlaneArrived.HasValue) message.SupplyOrder.PlaneArrived = TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PlaneArrived.Value);

            message.SupplyOrder.IsGrossPricesCalculated = true;

            supplyOrderRepository.Update(message.SupplyOrder);

            Sender.Tell(
                new Tuple<SupplyOrder, List<PaymentTaskMessage>, InformationMessage>(
                    supplyOrderRepository
                        .GetByNetId(
                            message.SupplyOrder.NetUid
                        ),
                    messagesToSend,
                    informationMessage
                )
            );

            ActorReferenceManager.Instance.Get(SupplyActorNames.SUPPLY_INVOICE_ACTOR).Tell(new UpdateSupplyInvoiceItemGrossPriceMessage(
                _supplyRepositoriesFactory
                    .NewSupplyInvoiceRepository(connection)
                    .GetBySupplyOrderId(message.SupplyOrder.Id).Select(x => x.Id),
                updatedBy.NetUid
            ));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessAddPackingListDocumentsMessage(AddPackingListDocumentsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.PackingListDocuments.Any()) {
            _supplyRepositoriesFactory.NewPackingListDocumentRepository(connection).Add(message.PackingListDocuments);

            Sender.Tell(_supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetByNetId(message.SupplyOrderNetId));
        } else {
            Sender.Tell(null);
        }
    }

    private void ProcessUpdateBillOfLadingDocumentMessage(UpdateBillOfLadingDocumentMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _supplyRepositoriesFactory.NewBillOfLadingDocumentRepository(connection).Update(message.BillOfLadingDocument);

        Sender.Tell(_supplyRepositoriesFactory
            .NewSupplyOrderRepository(connection)
            .GetByNetId(message.SupplyOrderNetId)
        );
    }

    private void ProcessAddCreditNoteDocumentMessage(AddCreditNoteDocumentMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
        SupplyOrder supplyOrderFromDb = supplyOrderRepository.GetByNetIdIfExist(message.NetId);

        if (supplyOrderFromDb != null) {
            message.CreditNoteDocument.SupplyOrderId = supplyOrderFromDb.Id;
            _supplyRepositoriesFactory.NewCreditNoteDocumentRepository(connection).Add(message.CreditNoteDocument);

            Sender.Tell(supplyOrderRepository.GetByNetId(message.NetId));
        } else {
            Sender.Tell(null);
        }
    }

    private void ProcessAddPolandPaymentDeliveryProtocolDocumentsMessage(AddPolandPaymentDeliveryProtocolDocumentsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
        ISupplyServiceNumberRepository supplyServiceNumberRepository = _supplyRepositoriesFactory.NewSupplyServiceNumberRepository(connection);

        SupplyOrder supplyOrderFromDb = supplyOrderRepository.GetByNetIdIfExist(message.NetId);

        if (supplyOrderFromDb != null) {
            if (message.PolandPaymentDeliveryProtocol.SupplyPaymentTask != null) {
                message.PolandPaymentDeliveryProtocol.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                message.PolandPaymentDeliveryProtocol.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.PaymentDeliveryProtocol;
                message.PolandPaymentDeliveryProtocol.SupplyPaymentTask.UserId = message.PolandPaymentDeliveryProtocol.SupplyPaymentTask.User.Id;

                message.PolandPaymentDeliveryProtocol.SupplyPaymentTask.PayToDate =
                    !message.PolandPaymentDeliveryProtocol.SupplyPaymentTask.PayToDate.HasValue
                        ? DateTime.UtcNow
                        : TimeZoneInfo.ConvertTimeToUtc(message.PolandPaymentDeliveryProtocol.SupplyPaymentTask.PayToDate.Value);

                message.PolandPaymentDeliveryProtocol.SupplyPaymentTask.NetPrice = message.PolandPaymentDeliveryProtocol.NetPrice;
                message.PolandPaymentDeliveryProtocol.SupplyPaymentTask.GrossPrice = message.PolandPaymentDeliveryProtocol.GrossPrice;

                message.PolandPaymentDeliveryProtocol.SupplyPaymentTask.IsAccounting = message.PolandPaymentDeliveryProtocol.IsAccounting;

                message.PolandPaymentDeliveryProtocol.SupplyPaymentTaskId = _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection)
                    .Add(message.PolandPaymentDeliveryProtocol.SupplyPaymentTask);
            }

            message.PolandPaymentDeliveryProtocol.NetPrice =
                Math.Round(
                    message.PolandPaymentDeliveryProtocol.GrossPrice * 100 / Convert.ToDecimal(100 + message.PolandPaymentDeliveryProtocol.VatPercent),
                    2
                );

            message.PolandPaymentDeliveryProtocol.Vat =
                Math.Round(
                    message.PolandPaymentDeliveryProtocol.GrossPrice - message.PolandPaymentDeliveryProtocol.NetPrice,
                    2
                );

            if (message.PolandPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey != null) {
                if (message.PolandPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey.IsNew())
                    message.PolandPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKeyId = _supplyRepositoriesFactory
                        .NewSupplyOrderPaymentDeliveryProtocolKeyRepository(connection)
                        .Add(message.PolandPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey);
                else
                    message.PolandPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKeyId =
                        message.PolandPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey.Id;
            }

            message.PolandPaymentDeliveryProtocol.FromDate = TimeZoneInfo.ConvertTimeToUtc(message.PolandPaymentDeliveryProtocol.FromDate);

            message.PolandPaymentDeliveryProtocol.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetHeadPurchaseAnalytic().Id;
            message.PolandPaymentDeliveryProtocol.SupplyOrderId = supplyOrderFromDb.Id;

            SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

            if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                message.PolandPaymentDeliveryProtocol.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
            else
                message.PolandPaymentDeliveryProtocol.ServiceNumber = string.Format("P{0:D10}", 1);

            supplyServiceNumberRepository.Add(message.PolandPaymentDeliveryProtocol.ServiceNumber);

            long polandPaymentDeliveryProtocolId =
                _supplyRepositoriesFactory.NewSupplyOrderPolandPaymentDeliveryProtocolRepository(connection).Add(message.PolandPaymentDeliveryProtocol);

            _supplyRepositoriesFactory
                .NewInvoiceDocumentRepository(connection)
                .Add(message.PolandPaymentDeliveryProtocol.InvoiceDocuments.Select(d => {
                    d.SupplyOrderPolandPaymentDeliveryProtocolId = polandPaymentDeliveryProtocolId;
                    return d;
                }));

            Sender.Tell(supplyOrderRepository.GetByNetId(message.NetId));
        } else {
            Sender.Tell(null);
        }
    }

    private void ProcessDeleteSupplyOrderByNetIdMessage(DeleteSupplyOrderByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);

        SupplyOrder supplyOrder = supplyOrderRepository.GetByNetId(message.OrderNetId);

        if (supplyOrder != null) {
            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            supplyOrder = supplyOrderRepository.GetByNetId(message.OrderNetId);

            if (!supplyOrder.IsPartiallyPlaced) {
                if (!supplyOrder.SupplyInvoices.Any()) {
                    ISupplyPaymentTaskRepository supplyPaymentTaskRepository = _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);

                    foreach (SupplyOrderPolandPaymentDeliveryProtocol protocol in supplyOrder
                                 .SupplyOrderPolandPaymentDeliveryProtocols
                                 .Where(p => p.SupplyPaymentTask != null))
                        supplyPaymentTaskRepository.RemoveById(protocol.SupplyPaymentTask.Id, user.Id);

                    if (supplyOrder.SupplyProForm != null && supplyOrder.SupplyProForm.PaymentDeliveryProtocols.Any(p => p.SupplyPaymentTask != null))
                        foreach (SupplyOrderPaymentDeliveryProtocol protocol in supplyOrder.SupplyProForm.PaymentDeliveryProtocols.Where(p =>
                                     p.SupplyPaymentTask != null))
                            supplyPaymentTaskRepository.RemoveById(protocol.SupplyPaymentTask.Id, user.Id);

                    foreach (SupplyInvoice invoice in supplyOrder.SupplyInvoices) {
                        if (invoice.PaymentDeliveryProtocols.All(p => p.SupplyPaymentTask == null)) continue;

                        foreach (SupplyOrderPaymentDeliveryProtocol protocol in invoice.PaymentDeliveryProtocols.Where(p => p.SupplyPaymentTask != null))
                            supplyPaymentTaskRepository.RemoveById(protocol.SupplyPaymentTask.Id, user.Id);
                    }

                    foreach (SupplyOrderContainerService service in supplyOrder.SupplyOrderContainerServices)
                        if (service.ContainerService != null) {
                            if (service.ContainerService.SupplyPaymentTask != null) supplyPaymentTaskRepository.RemoveById(service.ContainerService.SupplyPaymentTask.Id, user.Id);

                            if (service.ContainerService.AccountingPaymentTask != null)
                                supplyPaymentTaskRepository.RemoveById(service.ContainerService.AccountingPaymentTask.Id, user.Id);
                        }

                    if (supplyOrder.PortWorkService != null) {
                        if (supplyOrder.PortWorkService.SupplyPaymentTask != null)
                            supplyPaymentTaskRepository.RemoveById(supplyOrder.PortWorkService.SupplyPaymentTask.Id, user.Id);

                        if (supplyOrder.PortWorkService.AccountingPaymentTask != null)
                            supplyPaymentTaskRepository.RemoveById(supplyOrder.PortWorkService.AccountingPaymentTask.Id, user.Id);
                    }

                    if (supplyOrder.TransportationService != null) {
                        if (supplyOrder.TransportationService.SupplyPaymentTask != null)
                            supplyPaymentTaskRepository.RemoveById(supplyOrder.TransportationService.SupplyPaymentTask.Id, user.Id);

                        if (supplyOrder.TransportationService.AccountingPaymentTask != null)
                            supplyPaymentTaskRepository.RemoveById(supplyOrder.TransportationService.AccountingPaymentTask.Id, user.Id);
                    }

                    if (supplyOrder.CustomAgencyService != null) {
                        if (supplyOrder.CustomAgencyService.SupplyPaymentTask != null)
                            supplyPaymentTaskRepository.RemoveById(supplyOrder.CustomAgencyService.SupplyPaymentTask.Id, user.Id);

                        if (supplyOrder.CustomAgencyService.AccountingPaymentTask != null)
                            supplyPaymentTaskRepository.RemoveById(supplyOrder.CustomAgencyService.AccountingPaymentTask.Id, user.Id);
                    }

                    if (supplyOrder.PortCustomAgencyService != null) {
                        if (supplyOrder.PortCustomAgencyService.SupplyPaymentTask != null)
                            supplyPaymentTaskRepository.RemoveById(supplyOrder.PortCustomAgencyService.SupplyPaymentTask.Id, user.Id);

                        if (supplyOrder.PortCustomAgencyService.AccountingPaymentTask != null)
                            supplyPaymentTaskRepository.RemoveById(supplyOrder.PortCustomAgencyService.AccountingPaymentTask.Id, user.Id);
                    }


                    if (supplyOrder.PlaneDeliveryService != null) {
                        if (supplyOrder.PlaneDeliveryService.SupplyPaymentTask != null)
                            supplyPaymentTaskRepository.RemoveById(supplyOrder.PlaneDeliveryService.SupplyPaymentTask.Id, user.Id);

                        if (supplyOrder.PlaneDeliveryService.AccountingPaymentTask != null)
                            supplyPaymentTaskRepository.RemoveById(supplyOrder.PlaneDeliveryService.AccountingPaymentTask.Id, user.Id);
                    }

                    if (supplyOrder.VehicleDeliveryService != null) {
                        if (supplyOrder.VehicleDeliveryService.SupplyPaymentTask != null)
                            supplyPaymentTaskRepository.RemoveById(supplyOrder.VehicleDeliveryService.SupplyPaymentTask.Id, user.Id);

                        if (supplyOrder.VehicleDeliveryService.AccountingPaymentTask != null)
                            supplyPaymentTaskRepository.RemoveById(supplyOrder.VehicleDeliveryService.AccountingPaymentTask.Id, user.Id);
                    }

                    supplyOrderRepository.Remove(message.OrderNetId);

                    Sender.Tell(
                        (true, string.Empty)
                    );
                } else {
                    Sender.Tell(
                        (false, SupplyOrderResourceNames.DELETE_ALL_INVOICES)
                    );
                }
            } else {
                Sender.Tell(
                    (false, SupplyOrderResourceNames.PARTIALLY_OR_FULLY_PLACED)
                );
            }
        } else {
            Sender.Tell(
                (false, SupplyOrderResourceNames.SUPPLY_ORDER_DOES_NOT_EXISTS)
            );
        }
    }

    private static void UpdateSupplyOrganizationAndAgreement(
        ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository,
        long supplyOrganizationAgreementId,
        decimal oldPrice,
        decimal accountingOldPrice,
        long newSupplyOrganizationAgreementId,
        decimal newPrice,
        decimal accountingNewPrice) {
        if (supplyOrganizationAgreementId.Equals(newSupplyOrganizationAgreementId) &&
            oldPrice.Equals(newPrice))
            return;

        SupplyOrganizationAgreement supplyOrganizationAgreement =
            supplyOrganizationAgreementRepository.GetById(supplyOrganizationAgreementId);

        supplyOrganizationAgreement.CurrentAmount =
            Math.Round(supplyOrganizationAgreement.CurrentAmount + oldPrice,
                2);

        supplyOrganizationAgreement.AccountingCurrentAmount =
            Math.Round(supplyOrganizationAgreement.AccountingCurrentAmount + accountingOldPrice,
                2);

        supplyOrganizationAgreementRepository.UpdateCurrentAmount(supplyOrganizationAgreement);

        SupplyOrganizationAgreement newSupplyOrganizationAgreement =
            supplyOrganizationAgreementRepository.GetById(newSupplyOrganizationAgreementId);

        newSupplyOrganizationAgreement.CurrentAmount =
            Math.Round(newSupplyOrganizationAgreement.CurrentAmount - newPrice,
                2);

        newSupplyOrganizationAgreement.AccountingCurrentAmount =
            Math.Round(newSupplyOrganizationAgreement.AccountingCurrentAmount - accountingNewPrice,
                2);

        supplyOrganizationAgreementRepository.UpdateCurrentAmount(newSupplyOrganizationAgreement);
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

    private static void CreateOrUpdateShipServices(
        UpdateSupplyOrderMessage message,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IDbConnection connection,
        ISupplyPaymentTaskRepository supplyPaymentTaskRepository,
        IInvoiceDocumentRepository invoiceDocumentRepository,
        IServiceDetailItemRepository serviceDetailItemRepository,
        IServiceDetailItemKeyRepository serviceDetailItemKeyRepository,
        ISupplyServiceNumberRepository supplyServiceNumberRepository,
        ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository,
        ICollection<PaymentTaskMessage> messagesToSend,
        InformationMessage informationMessage,
        User updatedBy,
        User headPolishLogistic,
        ISupplyInformationTaskRepository supplyInformationTaskRepository,
        IActProvidingServiceDocumentRepository actProvidingServiceDocumentRepository,
        ISupplyServiceAccountDocumentRepository supplyServiceAccountDocumentRepository) {
        if (message.SupplyOrder.PortWorkService != null) {
            message.SupplyOrder.PortWorkService.NetPrice = Math.Round(
                message.SupplyOrder.PortWorkService.GrossPrice * 100 / Convert.ToDecimal(100 + message.SupplyOrder.PortWorkService.VatPercent),
                2
            );
            message.SupplyOrder.PortWorkService.Vat = Math.Round(
                message.SupplyOrder.PortWorkService.GrossPrice - message.SupplyOrder.PortWorkService.NetPrice,
                2
            );

            message.SupplyOrder.PortWorkService.AccountingNetPrice = Math.Round(
                message.SupplyOrder.PortWorkService.AccountingGrossPrice * 100 / Convert.ToDecimal(100 + message.SupplyOrder.PortWorkService.AccountingVatPercent),
                2
            );
            message.SupplyOrder.PortWorkService.AccountingVat = Math.Round(
                message.SupplyOrder.PortWorkService.AccountingGrossPrice - message.SupplyOrder.PortWorkService.AccountingNetPrice,
                2
            );

            if (message.SupplyOrder.PortWorkService.FromDate.HasValue)
                message.SupplyOrder.PortWorkService.FromDate = TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortWorkService.FromDate.Value);

            if (message.SupplyOrder.PortWorkService.IsNew()) {
                if (message.SupplyOrder.PortWorkService.SupplyPaymentTask != null) {
                    message.SupplyOrder.PortWorkService.SupplyPaymentTask.UserId = message.SupplyOrder.PortWorkService.SupplyPaymentTask.User.Id;
                    message.SupplyOrder.PortWorkService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.SupplyOrder.PortWorkService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.PortWorkService;

                    message.SupplyOrder.PortWorkService.SupplyPaymentTask.PayToDate =
                        !message.SupplyOrder.PortWorkService.SupplyPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortWorkService.SupplyPaymentTask.PayToDate.Value);

                    message.SupplyOrder.PortWorkService.SupplyPaymentTask.NetPrice = message.SupplyOrder.PortWorkService.NetPrice;
                    message.SupplyOrder.PortWorkService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.PortWorkService.GrossPrice;

                    message.SupplyOrder.PortWorkService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(message.SupplyOrder.PortWorkService.SupplyPaymentTask);

                    messagesToSend.Add(new PaymentTaskMessage {
                        Amount = message.SupplyOrder.PortWorkService.GrossPrice,
                        Discount = Convert.ToDouble(message.SupplyOrder.PortWorkService.Vat),
                        CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                        PayToDate = message.SupplyOrder.PortWorkService.SupplyPaymentTask.PayToDate,
                        OrganisationName = message.SupplyOrder.PortWorkService?.PortWorkOrganization?.Name,
                        PaymentForm = "Port work"
                    });
                }

                if (message.SupplyOrder.PortWorkService.AccountingPaymentTask != null) {
                    message.SupplyOrder.PortWorkService.AccountingPaymentTask.UserId = message.SupplyOrder.PortWorkService.AccountingPaymentTask.User.Id;
                    message.SupplyOrder.PortWorkService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.SupplyOrder.PortWorkService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.PortWorkService;
                    message.SupplyOrder.PortWorkService.AccountingPaymentTask.IsAccounting = true;

                    message.SupplyOrder.PortWorkService.AccountingPaymentTask.PayToDate =
                        !message.SupplyOrder.PortWorkService.AccountingPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortWorkService.AccountingPaymentTask.PayToDate.Value);

                    message.SupplyOrder.PortWorkService.AccountingPaymentTask.NetPrice = message.SupplyOrder.PortWorkService.AccountingNetPrice;
                    message.SupplyOrder.PortWorkService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.PortWorkService.AccountingGrossPrice;

                    message.SupplyOrder.PortWorkService.AccountingPaymentTaskId = supplyPaymentTaskRepository.Add(message.SupplyOrder.PortWorkService.AccountingPaymentTask);

                    messagesToSend.Add(new PaymentTaskMessage {
                        Amount = message.SupplyOrder.PortWorkService.AccountingGrossPrice,
                        Discount = Convert.ToDouble(message.SupplyOrder.PortWorkService.AccountingVat),
                        CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                        PayToDate = message.SupplyOrder.PortWorkService.AccountingPaymentTask.PayToDate,
                        OrganisationName = message.SupplyOrder.PortWorkService?.PortWorkOrganization?.Name,
                        PaymentForm = "Port work"
                    });
                }

                if (message.SupplyOrder.PortWorkService.SupplyInformationTask != null) {
                    message.SupplyOrder.PortWorkService.SupplyInformationTask.UserId = updatedBy.Id;
                    message.SupplyOrder.PortWorkService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                    message.SupplyOrder.PortWorkService.AccountingSupplyCostsWithinCountry =
                        message.SupplyOrder.PortWorkService.SupplyInformationTask.GrossPrice;

                    message.SupplyOrder.PortWorkService.SupplyInformationTaskId =
                        supplyInformationTaskRepository.Add(message.SupplyOrder.PortWorkService.SupplyInformationTask);
                }

                if (message.SupplyOrder.PortWorkService.ActProvidingServiceDocument != null) {
                    if (message.SupplyOrder.PortWorkService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.PortWorkService.ActProvidingServiceDocument.Number =
                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.PortWorkService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.SupplyOrder.PortWorkService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.SupplyOrder.PortWorkService.ActProvidingServiceDocument);
                    } else if (message.SupplyOrder.PortWorkService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.PortWorkService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.PortWorkService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.SupplyOrder.PortWorkService.SupplyServiceAccountDocument != null) {
                    if (message.SupplyOrder.PortWorkService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.PortWorkService.SupplyServiceAccountDocument.Number =
                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.PortWorkService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                        message.SupplyOrder.PortWorkService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.SupplyOrder.PortWorkService.SupplyServiceAccountDocument);
                    } else if (message.SupplyOrder.PortWorkService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.PortWorkService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.PortWorkService.SupplyServiceAccountDocument.Id);
                    }
                }

                if (message.SupplyOrder.PortWorkService.SupplyOrganizationAgreement != null && !message.SupplyOrder.PortWorkService.SupplyOrganizationAgreement.IsNew()) {
                    message.SupplyOrder.PortWorkService.SupplyOrganizationAgreement =
                        supplyOrganizationAgreementRepository.GetById(message.SupplyOrder.PortWorkService.SupplyOrganizationAgreement.Id);

                    message.SupplyOrder.PortWorkService.SupplyOrganizationAgreement.CurrentAmount =
                        Math.Round(message.SupplyOrder.PortWorkService.SupplyOrganizationAgreement.CurrentAmount - message.SupplyOrder.PortWorkService.GrossPrice, 2);

                    message.SupplyOrder.PortWorkService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                        Math.Round(message.SupplyOrder.PortWorkService.SupplyOrganizationAgreement.AccountingCurrentAmount -
                                   message.SupplyOrder.PortWorkService.AccountingGrossPrice, 2);

                    supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.SupplyOrder.PortWorkService.SupplyOrganizationAgreement);
                }

                informationMessage.CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}";
                informationMessage.Title = $"�������� ���������� � {message.SupplyOrder.SupplyOrderNumber.Number}";
                informationMessage.Message = "������ ������ ";

                SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                    message.SupplyOrder.PortWorkService.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                else
                    message.SupplyOrder.PortWorkService.ServiceNumber = string.Format("P{0:D10}", 1);

                supplyServiceNumberRepository.Add(message.SupplyOrder.PortWorkService.ServiceNumber);

                message.SupplyOrder.PortWorkService.PortWorkOrganizationId = message.SupplyOrder.PortWorkService.PortWorkOrganization?.Id;
                if (message.SupplyOrder.PortWorkService.SupplyOrganizationAgreement != null)
                    message.SupplyOrder.PortWorkService.SupplyOrganizationAgreementId = message.SupplyOrder.PortWorkService.SupplyOrganizationAgreement.Id;
                message.SupplyOrder.PortWorkService.UserId = headPolishLogistic.Id;
                message.SupplyOrder.PortWorkServiceId = supplyRepositoriesFactory.NewPortWorkServiceRepository(connection).Add(message.SupplyOrder.PortWorkService);

                if (message.SupplyOrder.PortWorkService.InvoiceDocuments.Any(d => d.IsNew()))
                    invoiceDocumentRepository.Add(message.SupplyOrder.PortWorkService.InvoiceDocuments
                        .Where(d => d.IsNew())
                        .Select(d => {
                            d.PortWorkServiceId = message.SupplyOrder.PortWorkServiceId;

                            return d;
                        })
                    );

                if (message.SupplyOrder.PortWorkService.ServiceDetailItems.Any())
                    InsertOrUpdateServiceDetailItems(
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        message.SupplyOrder.PortWorkService.ServiceDetailItems
                            .Select(i => {
                                i.PortWorkServiceId = message.SupplyOrder.PortWorkServiceId;

                                return i;
                            })
                    );
            } else {
                PortWorkService existPortWorkService = supplyRepositoriesFactory
                    .NewPortWorkServiceRepository(connection)
                    .GetByIdWithoutIncludes(message.SupplyOrder.PortWorkService.Id);

                UpdateSupplyOrganizationAndAgreement(
                    supplyOrganizationAgreementRepository,
                    message.SupplyOrder.PortWorkService.SupplyOrganizationAgreementId,
                    existPortWorkService.GrossPrice,
                    existPortWorkService.AccountingGrossPrice,
                    message.SupplyOrder.PortWorkService.SupplyOrganizationAgreement.Id,
                    message.SupplyOrder.PortWorkService.GrossPrice,
                    message.SupplyOrder.PortWorkService.AccountingGrossPrice);

                message.SupplyOrder.PortWorkService.PortWorkOrganizationId =
                    message.SupplyOrder.PortWorkService.PortWorkOrganization.Id;
                message.SupplyOrder.PortWorkService.SupplyOrganizationAgreementId =
                    message.SupplyOrder.PortWorkService.SupplyOrganizationAgreement.Id;

                if (message.SupplyOrder.PortWorkService.InvoiceDocuments.Any()) {
                    invoiceDocumentRepository.RemoveAllByPortWorkServiceIdExceptProvided(
                        message.SupplyOrder.PortWorkService.Id,
                        message.SupplyOrder.PortWorkService.InvoiceDocuments.Where(d => !d.IsNew() && !d.Deleted).Select(d => d.Id)
                    );

                    if (message.SupplyOrder.PortWorkService.InvoiceDocuments.Any(d => d.IsNew()))
                        invoiceDocumentRepository.Add(message.SupplyOrder.PortWorkService.InvoiceDocuments
                            .Where(d => d.IsNew())
                            .Select(d => {
                                d.PortWorkServiceId = message.SupplyOrder.PortWorkService.Id;

                                return d;
                            })
                        );
                } else {
                    invoiceDocumentRepository.RemoveAllByPortWorkServiceId(message.SupplyOrder.PortWorkService.Id);
                }

                if (message.SupplyOrder.PortWorkService.ServiceDetailItems.Any()) {
                    serviceDetailItemRepository.RemoveAllByPortWorkServiceIdExceptProvided(
                        message.SupplyOrder.PortWorkService.Id,
                        message.SupplyOrder.PortWorkService.ServiceDetailItems.Where(i => !i.IsNew()).Select(i => i.Id)
                    );

                    InsertOrUpdateServiceDetailItems(
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        message.SupplyOrder.PortWorkService.ServiceDetailItems
                            .Select(i => {
                                i.PortWorkServiceId = message.SupplyOrder.PortWorkServiceId;

                                return i;
                            })
                    );
                } else {
                    serviceDetailItemRepository.RemoveAllByPortWorkServiceId(message.SupplyOrder.PortWorkService.Id);
                }

                if (message.SupplyOrder.PortWorkService.SupplyPaymentTask != null) {
                    if (message.SupplyOrder.PortWorkService.SupplyPaymentTask.IsNew()) {
                        message.SupplyOrder.PortWorkService.SupplyPaymentTask.UserId = message.SupplyOrder.PortWorkService.SupplyPaymentTask.User.Id;
                        message.SupplyOrder.PortWorkService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.PortWorkService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.PortWorkService;

                        message.SupplyOrder.PortWorkService.SupplyPaymentTask.PayToDate =
                            !message.SupplyOrder.PortWorkService.SupplyPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortWorkService.SupplyPaymentTask.PayToDate.Value);

                        message.SupplyOrder.PortWorkService.SupplyPaymentTask.NetPrice = message.SupplyOrder.PortWorkService.NetPrice;
                        message.SupplyOrder.PortWorkService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.PortWorkService.GrossPrice;

                        message.SupplyOrder.PortWorkService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(message.SupplyOrder.PortWorkService.SupplyPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.PortWorkService.GrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.PortWorkService.Vat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.PortWorkService.SupplyPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.PortWorkService?.PortWorkOrganization?.Name,
                            PaymentForm = "Port work"
                        });
                    } else {
                        if (message.SupplyOrder.PortWorkService.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.SupplyOrder.PortWorkService.SupplyPaymentTask.IsAvailableForPayment) {
                            if (message.SupplyOrder.PortWorkService.SupplyPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.PortWorkService.SupplyPaymentTask.Id, updatedBy.Id);

                                message.SupplyOrder.PortWorkService.SupplyPaymentTaskId = null;
                            } else {
                                message.SupplyOrder.PortWorkService.SupplyPaymentTask.PayToDate =
                                    !message.SupplyOrder.PortWorkService.SupplyPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortWorkService.SupplyPaymentTask.PayToDate.Value);

                                message.SupplyOrder.PortWorkService.SupplyPaymentTask.NetPrice = message.SupplyOrder.PortWorkService.NetPrice;
                                message.SupplyOrder.PortWorkService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.PortWorkService.GrossPrice;
                                message.SupplyOrder.PortWorkService.SupplyPaymentTask.UpdatedById = updatedBy.Id;

                                supplyPaymentTaskRepository.Update(message.SupplyOrder.PortWorkService.SupplyPaymentTask);
                            }
                        }
                    }
                }

                if (message.SupplyOrder.PortWorkService.AccountingPaymentTask != null) {
                    if (message.SupplyOrder.PortWorkService.AccountingPaymentTask.IsNew()) {
                        message.SupplyOrder.PortWorkService.AccountingPaymentTask.UserId = message.SupplyOrder.PortWorkService.AccountingPaymentTask.User.Id;
                        message.SupplyOrder.PortWorkService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.PortWorkService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.PortWorkService;
                        message.SupplyOrder.PortWorkService.AccountingPaymentTask.IsAccounting = true;

                        message.SupplyOrder.PortWorkService.AccountingPaymentTask.PayToDate =
                            !message.SupplyOrder.PortWorkService.AccountingPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortWorkService.AccountingPaymentTask.PayToDate.Value);

                        message.SupplyOrder.PortWorkService.AccountingPaymentTask.NetPrice = message.SupplyOrder.PortWorkService.AccountingNetPrice;
                        message.SupplyOrder.PortWorkService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.PortWorkService.AccountingGrossPrice;

                        message.SupplyOrder.PortWorkService.AccountingPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.SupplyOrder.PortWorkService.AccountingPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.PortWorkService.AccountingGrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.PortWorkService.AccountingVat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.PortWorkService.AccountingPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.PortWorkService?.PortWorkOrganization?.Name,
                            PaymentForm = "Port work"
                        });
                    } else {
                        if (message.SupplyOrder.PortWorkService.AccountingPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.SupplyOrder.PortWorkService.AccountingPaymentTask.IsAvailableForPayment) {
                            if (message.SupplyOrder.PortWorkService.AccountingPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.PortWorkService.AccountingPaymentTask.Id, updatedBy.Id);

                                message.SupplyOrder.PortWorkService.AccountingPaymentTaskId = null;
                            } else {
                                message.SupplyOrder.PortWorkService.AccountingPaymentTask.PayToDate =
                                    !message.SupplyOrder.PortWorkService.AccountingPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortWorkService.AccountingPaymentTask.PayToDate.Value);

                                message.SupplyOrder.PortWorkService.AccountingPaymentTask.NetPrice = message.SupplyOrder.PortWorkService.AccountingNetPrice;
                                message.SupplyOrder.PortWorkService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.PortWorkService.AccountingGrossPrice;
                                message.SupplyOrder.PortWorkService.AccountingPaymentTask.UpdatedById = updatedBy.Id;

                                supplyPaymentTaskRepository.Update(message.SupplyOrder.PortWorkService.AccountingPaymentTask);
                            }
                        }
                    }
                }

                if (message.SupplyOrder.PortWorkService.SupplyInformationTask != null) {
                    if (message.SupplyOrder.PortWorkService.SupplyInformationTask.IsNew()) {
                        message.SupplyOrder.PortWorkService.SupplyInformationTask.UserId = updatedBy.Id;
                        message.SupplyOrder.PortWorkService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                        message.SupplyOrder.PortWorkService.AccountingSupplyCostsWithinCountry =
                            message.SupplyOrder.PortWorkService.SupplyInformationTask.GrossPrice;

                        message.SupplyOrder.PortWorkService.SupplyInformationTaskId =
                            supplyInformationTaskRepository.Add(message.SupplyOrder.PortWorkService.SupplyInformationTask);
                    } else {
                        if (message.SupplyOrder.PortWorkService.SupplyInformationTask.Deleted) {
                            message.SupplyOrder.PortWorkService.SupplyInformationTask.DeletedById = updatedBy.Id;

                            supplyInformationTaskRepository.Remove(message.SupplyOrder.PortWorkService.SupplyInformationTask);

                            message.SupplyOrder.PortWorkService.SupplyInformationTaskId = null;
                        } else {
                            message.SupplyOrder.PortWorkService.SupplyInformationTask.UpdatedById = updatedBy.Id;
                            message.SupplyOrder.PortWorkService.SupplyInformationTask.UserId = updatedBy.Id;

                            message.SupplyOrder.PortWorkService.AccountingSupplyCostsWithinCountry =
                                message.SupplyOrder.PortWorkService.SupplyInformationTask.GrossPrice;

                            supplyInformationTaskRepository.Update(message.SupplyOrder.PortWorkService.SupplyInformationTask);
                        }
                    }
                }

                if (message.SupplyOrder.PortWorkService.ActProvidingServiceDocument != null) {
                    if (message.SupplyOrder.PortWorkService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.PortWorkService.ActProvidingServiceDocument.Number =
                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.PortWorkService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.SupplyOrder.PortWorkService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.SupplyOrder.PortWorkService.ActProvidingServiceDocument);
                    } else if (message.SupplyOrder.PortWorkService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.PortWorkService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.PortWorkService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.SupplyOrder.PortWorkService.SupplyServiceAccountDocument != null) {
                    if (message.SupplyOrder.PortWorkService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.PortWorkService.SupplyServiceAccountDocument.Number =
                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.PortWorkService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                        message.SupplyOrder.PortWorkService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.SupplyOrder.PortWorkService.SupplyServiceAccountDocument);
                    } else if (message.SupplyOrder.PortWorkService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.PortWorkService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.PortWorkService.SupplyServiceAccountDocument.Id);
                    }
                }

                supplyRepositoriesFactory.NewPortWorkServiceRepository(connection).Update(message.SupplyOrder.PortWorkService);
            }
        }

        if (message.SupplyOrder.PortCustomAgencyService != null) {
            message.SupplyOrder.PortCustomAgencyService.NetPrice = Math.Round(
                message.SupplyOrder.PortCustomAgencyService.GrossPrice * 100 / Convert.ToDecimal(100 + message.SupplyOrder.PortCustomAgencyService.VatPercent),
                2
            );
            message.SupplyOrder.PortCustomAgencyService.Vat = Math.Round(
                message.SupplyOrder.PortCustomAgencyService.GrossPrice - message.SupplyOrder.PortCustomAgencyService.NetPrice,
                2
            );

            message.SupplyOrder.PortCustomAgencyService.AccountingNetPrice = Math.Round(
                message.SupplyOrder.PortCustomAgencyService.AccountingGrossPrice * 100 /
                Convert.ToDecimal(100 + message.SupplyOrder.PortCustomAgencyService.AccountingVatPercent),
                2
            );
            message.SupplyOrder.PortCustomAgencyService.AccountingVat = Math.Round(
                message.SupplyOrder.PortCustomAgencyService.AccountingGrossPrice - message.SupplyOrder.PortCustomAgencyService.AccountingNetPrice,
                2
            );

            if (message.SupplyOrder.PortCustomAgencyService.FromDate.HasValue)
                message.SupplyOrder.PortCustomAgencyService.FromDate = TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortCustomAgencyService.FromDate.Value);

            if (message.SupplyOrder.PortCustomAgencyService.IsNew()) {
                if (message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask != null) {
                    message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.UserId = message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.User.Id;
                    message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.PortCustomAgencyService;

                    message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate =
                        !message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate.Value);

                    message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.NetPrice = message.SupplyOrder.PortCustomAgencyService.NetPrice;
                    message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.PortCustomAgencyService.GrossPrice;

                    message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTaskId =
                        supplyPaymentTaskRepository.Add(message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask);

                    messagesToSend.Add(new PaymentTaskMessage {
                        Amount = message.SupplyOrder.PortCustomAgencyService.GrossPrice,
                        Discount = Convert.ToDouble(message.SupplyOrder.PortCustomAgencyService.Vat),
                        CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                        PayToDate = message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate,
                        OrganisationName = message.SupplyOrder.PortCustomAgencyService?.PortCustomAgencyOrganization?.Name,
                        PaymentForm = "Port custom agency"
                    });
                }

                if (message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask != null) {
                    message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.UserId = message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.User.Id;
                    message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.PortCustomAgencyService;
                    message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.IsAccounting = true;

                    message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate =
                        !message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate.Value);

                    message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.NetPrice = message.SupplyOrder.PortCustomAgencyService.AccountingNetPrice;
                    message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.PortCustomAgencyService.AccountingGrossPrice;

                    message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTaskId =
                        supplyPaymentTaskRepository.Add(message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask);

                    messagesToSend.Add(new PaymentTaskMessage {
                        Amount = message.SupplyOrder.PortCustomAgencyService.AccountingGrossPrice,
                        Discount = Convert.ToDouble(message.SupplyOrder.PortCustomAgencyService.AccountingVat),
                        CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                        PayToDate = message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate,
                        OrganisationName = message.SupplyOrder.PortCustomAgencyService?.PortCustomAgencyOrganization?.Name,
                        PaymentForm = "Port custom agency"
                    });
                }

                if (message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask != null) {
                    message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.UserId = updatedBy.Id;
                    message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                    message.SupplyOrder.PortCustomAgencyService.AccountingSupplyCostsWithinCountry =
                        message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.GrossPrice;

                    message.SupplyOrder.PortCustomAgencyService.SupplyInformationTaskId =
                        supplyInformationTaskRepository.Add(message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask);
                }

                if (message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument != null) {
                    if (message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.Number =
                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument);
                    } else if (message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument != null) {
                    if (message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.Number =
                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                        message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument);
                    } else if (message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.Id);
                    }
                }

                informationMessage.CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}";
                informationMessage.Title = $"�������� ���������� � {message.SupplyOrder.SupplyOrderNumber.Number}";
                informationMessage.Message = "������� ����� ������� ";

                SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                    message.SupplyOrder.PortCustomAgencyService.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                else
                    message.SupplyOrder.PortCustomAgencyService.ServiceNumber = string.Format("P{0:D10}", 1);

                supplyServiceNumberRepository.Add(message.SupplyOrder.PortCustomAgencyService.ServiceNumber);

                message.SupplyOrder.PortCustomAgencyService.PortCustomAgencyOrganizationId = message.SupplyOrder.PortCustomAgencyService.PortCustomAgencyOrganization?.Id;
                message.SupplyOrder.PortCustomAgencyService.UserId = headPolishLogistic.Id;
                message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreementId = message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.Id;
                message.SupplyOrder.PortCustomAgencyServiceId =
                    supplyRepositoriesFactory.NewPortCustomAgencyServiceRepository(connection).Add(message.SupplyOrder.PortCustomAgencyService);

                if (message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement != null &&
                    !message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.IsNew()) {
                    message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement =
                        supplyOrganizationAgreementRepository.GetById(message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.Id);

                    message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.CurrentAmount =
                        Math.Round(
                            message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.CurrentAmount - message.SupplyOrder.PortCustomAgencyService.GrossPrice, 2);

                    message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                        Math.Round(
                            message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.AccountingCurrentAmount -
                            message.SupplyOrder.PortCustomAgencyService.AccountingGrossPrice, 2);

                    supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement);
                }

                if (message.SupplyOrder.PortCustomAgencyService.InvoiceDocuments.Any(d => d.IsNew()))
                    invoiceDocumentRepository.Add(message.SupplyOrder.PortCustomAgencyService.InvoiceDocuments
                        .Where(d => d.IsNew())
                        .Select(d => {
                            d.PortCustomAgencyServiceId = message.SupplyOrder.PortCustomAgencyServiceId;

                            return d;
                        })
                    );

                if (message.SupplyOrder.PortCustomAgencyService.ServiceDetailItems.Any())
                    InsertOrUpdateServiceDetailItems(
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        message.SupplyOrder.PortCustomAgencyService.ServiceDetailItems
                            .Select(i => {
                                i.PortCustomAgencyServiceId = message.SupplyOrder.PortCustomAgencyServiceId;

                                return i;
                            })
                    );
            } else {
                PortCustomAgencyService existPortCustomAgencyService = supplyRepositoriesFactory
                    .NewPortCustomAgencyServiceRepository(connection)
                    .GetByIdWithoutIncludes(message.SupplyOrder.PortCustomAgencyService.Id);

                UpdateSupplyOrganizationAndAgreement(
                    supplyOrganizationAgreementRepository,
                    message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreementId,
                    existPortCustomAgencyService.GrossPrice,
                    existPortCustomAgencyService.AccountingGrossPrice,
                    message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.Id,
                    message.SupplyOrder.PortCustomAgencyService.GrossPrice,
                    message.SupplyOrder.PortCustomAgencyService.AccountingGrossPrice);

                message.SupplyOrder.PortCustomAgencyService.PortCustomAgencyOrganizationId =
                    message.SupplyOrder.PortCustomAgencyService.PortCustomAgencyOrganization.Id;
                message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreementId =
                    message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.Id;

                if (message.SupplyOrder.PortCustomAgencyService.InvoiceDocuments.Any()) {
                    invoiceDocumentRepository.RemoveAllByPortCustomAgencyServiceIdExceptProvided(
                        message.SupplyOrder.PortCustomAgencyService.Id,
                        message.SupplyOrder.PortCustomAgencyService.InvoiceDocuments.Where(d => !d.IsNew() && !d.Deleted).Select(d => d.Id)
                    );

                    if (message.SupplyOrder.PortCustomAgencyService.InvoiceDocuments.Any(d => d.IsNew()))
                        invoiceDocumentRepository.Add(message.SupplyOrder.PortCustomAgencyService.InvoiceDocuments
                            .Where(d => d.IsNew())
                            .Select(d => {
                                d.PortCustomAgencyServiceId = message.SupplyOrder.PortCustomAgencyService.Id;

                                return d;
                            })
                        );
                } else {
                    invoiceDocumentRepository.RemoveAllByPortCustomAgencyServiceId(message.SupplyOrder.PortCustomAgencyService.Id);
                }

                if (message.SupplyOrder.PortCustomAgencyService.ServiceDetailItems.Any()) {
                    serviceDetailItemRepository.RemoveAllByPortCustomAgencyServiceIdExceptProvided(
                        message.SupplyOrder.PortCustomAgencyService.Id,
                        message.SupplyOrder.PortCustomAgencyService.ServiceDetailItems.Where(i => !i.IsNew()).Select(i => i.Id)
                    );

                    InsertOrUpdateServiceDetailItems(
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        message.SupplyOrder.PortCustomAgencyService.ServiceDetailItems
                            .Select(i => {
                                i.PortCustomAgencyServiceId = message.SupplyOrder.PortCustomAgencyServiceId;

                                return i;
                            })
                    );
                } else {
                    serviceDetailItemRepository.RemoveAllByPortCustomAgencyServiceId(message.SupplyOrder.PortCustomAgencyService.Id);
                }

                if (message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask != null) {
                    if (message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.IsNew()) {
                        message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.UserId = message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.User.Id;
                        message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.PortCustomAgencyService;

                        message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate =
                            !message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate.Value);

                        message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.NetPrice = message.SupplyOrder.PortCustomAgencyService.NetPrice;
                        message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.PortCustomAgencyService.GrossPrice;

                        message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.PortCustomAgencyService.GrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.PortCustomAgencyService.Vat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.PortCustomAgencyService?.PortCustomAgencyOrganization?.Name,
                            PaymentForm = "Port custom agency"
                        });
                    } else {
                        if (message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.IsAvailableForPayment) {
                            if (message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.Id, updatedBy.Id);

                                message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTaskId = null;
                            } else {
                                message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate =
                                    !message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate.Value);

                                message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.NetPrice = message.SupplyOrder.PortCustomAgencyService.NetPrice;
                                message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.PortCustomAgencyService.GrossPrice;
                                message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.UpdatedById = updatedBy.Id;

                                supplyPaymentTaskRepository.Update(message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask);
                            }
                        }
                    }
                }

                if (message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask != null) {
                    if (message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.IsNew()) {
                        message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.UserId = message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.User.Id;
                        message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.PortCustomAgencyService;
                        message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.IsAccounting = true;

                        message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate =
                            !message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate.Value);

                        message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.NetPrice = message.SupplyOrder.PortCustomAgencyService.AccountingNetPrice;
                        message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.PortCustomAgencyService.AccountingGrossPrice;

                        message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.PortCustomAgencyService.AccountingGrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.PortCustomAgencyService.AccountingVat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.PortCustomAgencyService?.PortCustomAgencyOrganization?.Name,
                            PaymentForm = "Port custom agency"
                        });
                    } else {
                        if (message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.IsAvailableForPayment) {
                            if (message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.Id, updatedBy.Id);

                                message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTaskId = null;
                            } else {
                                message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate =
                                    !message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate.Value);

                                message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.NetPrice = message.SupplyOrder.PortCustomAgencyService.AccountingNetPrice;
                                message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.PortCustomAgencyService.AccountingGrossPrice;
                                message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.UpdatedById = updatedBy.Id;

                                supplyPaymentTaskRepository.Update(message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask);
                            }
                        }
                    }
                }

                if (message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask != null) {
                    if (message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.IsNew()) {
                        message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.UserId = updatedBy.Id;
                        message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                        message.SupplyOrder.PortCustomAgencyService.AccountingSupplyCostsWithinCountry =
                            message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.GrossPrice;

                        message.SupplyOrder.PortCustomAgencyService.SupplyInformationTaskId =
                            supplyInformationTaskRepository.Add(message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask);
                    } else {
                        if (message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.Deleted) {
                            message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.DeletedById = updatedBy.Id;

                            supplyInformationTaskRepository.Remove(message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask);

                            message.SupplyOrder.PortCustomAgencyService.SupplyInformationTaskId = null;
                        } else {
                            message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.UpdatedById = updatedBy.Id;
                            message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.UserId = updatedBy.Id;

                            message.SupplyOrder.PortCustomAgencyService.AccountingSupplyCostsWithinCountry =
                                message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.GrossPrice;

                            supplyInformationTaskRepository.Update(message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask);
                        }
                    }
                }

                if (message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument != null) {
                    if (message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.Number =
                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument);
                    } else if (message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument != null) {
                    if (message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.Number =
                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                        message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument);
                    } else if (message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.Id);
                    }
                }

                supplyRepositoriesFactory.NewPortCustomAgencyServiceRepository(connection).Update(message.SupplyOrder.PortCustomAgencyService);
            }
        }

        if (message.SupplyOrder.TransportationService != null) {
            message.SupplyOrder.TransportationService.NetPrice = Math.Round(
                message.SupplyOrder.TransportationService.GrossPrice * 100 / Convert.ToDecimal(100 + message.SupplyOrder.TransportationService.VatPercent),
                2
            );
            message.SupplyOrder.TransportationService.Vat = Math.Round(
                message.SupplyOrder.TransportationService.GrossPrice - message.SupplyOrder.TransportationService.NetPrice,
                2
            );

            message.SupplyOrder.TransportationService.AccountingNetPrice = Math.Round(
                message.SupplyOrder.TransportationService.AccountingGrossPrice * 100 / Convert.ToDecimal(100 + message.SupplyOrder.TransportationService.AccountingVatPercent),
                2
            );
            message.SupplyOrder.TransportationService.AccountingVat = Math.Round(
                message.SupplyOrder.TransportationService.AccountingGrossPrice - message.SupplyOrder.TransportationService.AccountingNetPrice,
                2
            );

            if (message.SupplyOrder.TransportationService.FromDate.HasValue)
                message.SupplyOrder.TransportationService.FromDate = TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.TransportationService.FromDate.Value);

            if (message.SupplyOrder.TransportationService.IsNew()) {
                if (message.SupplyOrder.TransportationService.SupplyPaymentTask != null) {
                    message.SupplyOrder.TransportationService.SupplyPaymentTask.UserId = message.SupplyOrder.TransportationService.SupplyPaymentTask.User.Id;
                    message.SupplyOrder.TransportationService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.SupplyOrder.TransportationService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.TransportationService;

                    message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate =
                        !message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate.Value);

                    message.SupplyOrder.TransportationService.SupplyPaymentTask.NetPrice = message.SupplyOrder.TransportationService.NetPrice;
                    message.SupplyOrder.TransportationService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.TransportationService.GrossPrice;

                    message.SupplyOrder.TransportationService.SupplyPaymentTaskId =
                        supplyPaymentTaskRepository.Add(message.SupplyOrder.TransportationService.SupplyPaymentTask);

                    messagesToSend.Add(new PaymentTaskMessage {
                        Amount = message.SupplyOrder.TransportationService.GrossPrice,
                        Discount = Convert.ToDouble(message.SupplyOrder.TransportationService.Vat),
                        CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                        PayToDate = message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate,
                        OrganisationName = message.SupplyOrder.TransportationService?.TransportationOrganization?.Name,
                        PaymentForm = "Transportation service"
                    });
                }

                if (message.SupplyOrder.TransportationService.AccountingPaymentTask != null) {
                    message.SupplyOrder.TransportationService.AccountingPaymentTask.UserId = message.SupplyOrder.TransportationService.AccountingPaymentTask.User.Id;
                    message.SupplyOrder.TransportationService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.SupplyOrder.TransportationService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.TransportationService;
                    message.SupplyOrder.TransportationService.AccountingPaymentTask.IsAccounting = true;

                    message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate =
                        !message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate.Value);

                    message.SupplyOrder.TransportationService.AccountingPaymentTask.NetPrice = message.SupplyOrder.TransportationService.AccountingNetPrice;
                    message.SupplyOrder.TransportationService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.TransportationService.AccountingGrossPrice;

                    message.SupplyOrder.TransportationService.AccountingPaymentTaskId =
                        supplyPaymentTaskRepository.Add(message.SupplyOrder.TransportationService.AccountingPaymentTask);

                    messagesToSend.Add(new PaymentTaskMessage {
                        Amount = message.SupplyOrder.TransportationService.AccountingGrossPrice,
                        Discount = Convert.ToDouble(message.SupplyOrder.TransportationService.AccountingVat),
                        CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                        PayToDate = message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate,
                        OrganisationName = message.SupplyOrder.TransportationService?.TransportationOrganization?.Name,
                        PaymentForm = "Transportation service"
                    });
                }

                if (message.SupplyOrder.TransportationService.SupplyInformationTask != null) {
                    message.SupplyOrder.TransportationService.SupplyInformationTask.UserId = updatedBy.Id;
                    message.SupplyOrder.TransportationService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                    message.SupplyOrder.TransportationService.AccountingSupplyCostsWithinCountry =
                        message.SupplyOrder.TransportationService.SupplyInformationTask.GrossPrice;

                    message.SupplyOrder.TransportationService.SupplyInformationTaskId =
                        supplyInformationTaskRepository.Add(message.SupplyOrder.TransportationService.SupplyInformationTask);
                }

                if (message.SupplyOrder.TransportationService.ActProvidingServiceDocument != null) {
                    if (message.SupplyOrder.TransportationService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.TransportationService.ActProvidingServiceDocument.Number =
                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.TransportationService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.SupplyOrder.TransportationService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.SupplyOrder.TransportationService.ActProvidingServiceDocument);
                    } else if (message.SupplyOrder.TransportationService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.TransportationService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.TransportationService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.SupplyOrder.TransportationService.SupplyServiceAccountDocument != null) {
                    if (message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.Number =
                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                        message.SupplyOrder.TransportationService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.SupplyOrder.TransportationService.SupplyServiceAccountDocument);
                    } else if (message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.TransportationService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.Id);
                    }
                }

                informationMessage.CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}";
                informationMessage.Title = $"�������� ���������� � {message.SupplyOrder.SupplyOrderNumber.Number}";
                informationMessage.Message = "����������� �������";

                SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                    message.SupplyOrder.TransportationService.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                else
                    message.SupplyOrder.TransportationService.ServiceNumber = string.Format("P{0:D10}", 1);

                if (message.SupplyOrder.TransportationService.SupplyOrganizationAgreement != null &&
                    !message.SupplyOrder.TransportationService.SupplyOrganizationAgreement.IsNew()) {
                    message.SupplyOrder.TransportationService.SupplyOrganizationAgreement =
                        supplyOrganizationAgreementRepository.GetById(message.SupplyOrder.TransportationService.SupplyOrganizationAgreement.Id);

                    message.SupplyOrder.TransportationService.SupplyOrganizationAgreement.CurrentAmount =
                        Math.Round(
                            message.SupplyOrder.TransportationService.SupplyOrganizationAgreement.CurrentAmount -
                            message.SupplyOrder.TransportationService.GrossPrice -
                            2);

                    message.SupplyOrder.TransportationService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                        Math.Round(
                            message.SupplyOrder.TransportationService.SupplyOrganizationAgreement.AccountingCurrentAmount -
                            message.SupplyOrder.TransportationService.AccountingGrossPrice,
                            2);

                    supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.SupplyOrder.TransportationService.SupplyOrganizationAgreement);
                }

                supplyServiceNumberRepository.Add(message.SupplyOrder.TransportationService.ServiceNumber);

                message.SupplyOrder.TransportationService.TransportationOrganizationId = message.SupplyOrder.TransportationService.TransportationOrganization.Id;
                message.SupplyOrder.TransportationService.UserId = headPolishLogistic.Id;
                message.SupplyOrder.TransportationService.SupplyOrganizationAgreementId = message.SupplyOrder.TransportationService.SupplyOrganizationAgreement.Id;
                message.SupplyOrder.TransportationServiceId =
                    supplyRepositoriesFactory.NewTransportationServiceRepository(connection).Add(message.SupplyOrder.TransportationService);

                if (message.SupplyOrder.TransportationService.InvoiceDocuments.Any(d => d.IsNew()))
                    invoiceDocumentRepository.Add(message.SupplyOrder.TransportationService.InvoiceDocuments
                        .Where(d => d.IsNew())
                        .Select(d => {
                            d.TransportationServiceId = message.SupplyOrder.TransportationServiceId;

                            return d;
                        })
                    );

                if (message.SupplyOrder.TransportationService.ServiceDetailItems.Any())
                    InsertOrUpdateServiceDetailItems(
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        message.SupplyOrder.TransportationService.ServiceDetailItems
                            .Select(i => {
                                i.TransportationServiceId = message.SupplyOrder.TransportationServiceId;

                                return i;
                            })
                    );
            } else {
                TransportationService existTransportationService = supplyRepositoriesFactory
                    .NewTransportationServiceRepository(connection)
                    .GetByIdWithoutIncludes(message.SupplyOrder.TransportationService.Id);

                UpdateSupplyOrganizationAndAgreement(
                    supplyOrganizationAgreementRepository,
                    message.SupplyOrder.TransportationService.SupplyOrganizationAgreementId,
                    existTransportationService.GrossPrice,
                    existTransportationService.AccountingGrossPrice,
                    message.SupplyOrder.TransportationService.SupplyOrganizationAgreement.Id,
                    message.SupplyOrder.TransportationService.GrossPrice,
                    message.SupplyOrder.TransportationService.AccountingGrossPrice);

                message.SupplyOrder.TransportationService.TransportationOrganizationId =
                    message.SupplyOrder.TransportationService.TransportationOrganization.Id;
                message.SupplyOrder.TransportationService.SupplyOrganizationAgreementId =
                    message.SupplyOrder.TransportationService.SupplyOrganizationAgreement.Id;

                if (message.SupplyOrder.TransportationService.InvoiceDocuments.Any()) {
                    invoiceDocumentRepository.RemoveAllByTransportationServiceIdExceptProvided(
                        message.SupplyOrder.TransportationService.Id,
                        message.SupplyOrder.TransportationService.InvoiceDocuments.Where(d => !d.IsNew() && !d.Deleted).Select(d => d.Id)
                    );

                    if (message.SupplyOrder.TransportationService.InvoiceDocuments.Any(d => d.IsNew()))
                        invoiceDocumentRepository.Add(message.SupplyOrder.TransportationService.InvoiceDocuments
                            .Where(d => d.IsNew())
                            .Select(d => {
                                d.TransportationServiceId = message.SupplyOrder.TransportationService.Id;

                                return d;
                            })
                        );
                } else {
                    invoiceDocumentRepository.RemoveAllByTransportationServiceId(message.SupplyOrder.TransportationService.Id);
                }

                if (message.SupplyOrder.TransportationService.ServiceDetailItems.Any()) {
                    serviceDetailItemRepository.RemoveAllByTransportationServiceIdExceptProvided(
                        message.SupplyOrder.TransportationService.Id,
                        message.SupplyOrder.TransportationService.ServiceDetailItems.Where(i => !i.IsNew()).Select(i => i.Id)
                    );

                    InsertOrUpdateServiceDetailItems(
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        message.SupplyOrder.TransportationService.ServiceDetailItems
                            .Select(i => {
                                i.TransportationServiceId = message.SupplyOrder.TransportationServiceId;

                                return i;
                            })
                    );
                } else {
                    serviceDetailItemRepository.RemoveAllByTransportationServiceId(message.SupplyOrder.TransportationService.Id);
                }

                if (message.SupplyOrder.TransportationService.SupplyPaymentTask != null) {
                    if (message.SupplyOrder.TransportationService.SupplyPaymentTask.IsNew()) {
                        message.SupplyOrder.TransportationService.SupplyPaymentTask.UserId = message.SupplyOrder.TransportationService.SupplyPaymentTask.User.Id;
                        message.SupplyOrder.TransportationService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.TransportationService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.TransportationService;

                        message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate =
                            !message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate.Value);

                        message.SupplyOrder.TransportationService.SupplyPaymentTask.NetPrice = message.SupplyOrder.TransportationService.NetPrice;
                        message.SupplyOrder.TransportationService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.TransportationService.GrossPrice;

                        message.SupplyOrder.TransportationService.SupplyPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.SupplyOrder.TransportationService.SupplyPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.TransportationService.GrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.TransportationService.Vat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.TransportationService?.TransportationOrganization?.Name,
                            PaymentForm = "Transportation service"
                        });
                    } else {
                        if (message.SupplyOrder.TransportationService.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.SupplyOrder.TransportationService.SupplyPaymentTask.IsAvailableForPayment) {
                            if (message.SupplyOrder.TransportationService.SupplyPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.TransportationService.SupplyPaymentTask.Id, updatedBy.Id);

                                message.SupplyOrder.TransportationService.SupplyPaymentTaskId = null;
                            } else {
                                message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate =
                                    !message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate.Value);

                                message.SupplyOrder.TransportationService.SupplyPaymentTask.NetPrice = message.SupplyOrder.TransportationService.NetPrice;
                                message.SupplyOrder.TransportationService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.TransportationService.GrossPrice;
                                message.SupplyOrder.TransportationService.SupplyPaymentTask.UpdatedById = updatedBy.Id;

                                supplyPaymentTaskRepository.Update(message.SupplyOrder.TransportationService.SupplyPaymentTask);
                            }
                        }
                    }
                }

                if (message.SupplyOrder.TransportationService.AccountingPaymentTask != null) {
                    if (message.SupplyOrder.TransportationService.AccountingPaymentTask.IsNew()) {
                        message.SupplyOrder.TransportationService.AccountingPaymentTask.UserId = message.SupplyOrder.TransportationService.AccountingPaymentTask.User.Id;
                        message.SupplyOrder.TransportationService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.TransportationService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.TransportationService;
                        message.SupplyOrder.TransportationService.AccountingPaymentTask.IsAccounting = true;

                        message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate =
                            !message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate.Value);

                        message.SupplyOrder.TransportationService.AccountingPaymentTask.NetPrice = message.SupplyOrder.TransportationService.AccountingNetPrice;
                        message.SupplyOrder.TransportationService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.TransportationService.AccountingGrossPrice;

                        message.SupplyOrder.TransportationService.AccountingPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.SupplyOrder.TransportationService.AccountingPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.TransportationService.AccountingGrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.TransportationService.AccountingVat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.TransportationService?.TransportationOrganization?.Name,
                            PaymentForm = "Transportation service"
                        });
                    } else {
                        if (message.SupplyOrder.TransportationService.AccountingPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.SupplyOrder.TransportationService.AccountingPaymentTask.IsAvailableForPayment) {
                            if (message.SupplyOrder.TransportationService.AccountingPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.TransportationService.AccountingPaymentTask.Id, updatedBy.Id);

                                message.SupplyOrder.TransportationService.AccountingPaymentTaskId = null;
                            } else {
                                message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate =
                                    !message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate.Value);

                                message.SupplyOrder.TransportationService.AccountingPaymentTask.NetPrice = message.SupplyOrder.TransportationService.AccountingNetPrice;
                                message.SupplyOrder.TransportationService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.TransportationService.AccountingGrossPrice;
                                message.SupplyOrder.TransportationService.AccountingPaymentTask.UpdatedById = updatedBy.Id;

                                supplyPaymentTaskRepository.Update(message.SupplyOrder.TransportationService.AccountingPaymentTask);
                            }
                        }
                    }
                }

                if (message.SupplyOrder.TransportationService.SupplyInformationTask != null) {
                    if (message.SupplyOrder.TransportationService.SupplyInformationTask.IsNew()) {
                        message.SupplyOrder.TransportationService.SupplyInformationTask.UserId = updatedBy.Id;
                        message.SupplyOrder.TransportationService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                        message.SupplyOrder.TransportationService.AccountingSupplyCostsWithinCountry =
                            message.SupplyOrder.TransportationService.SupplyInformationTask.GrossPrice;

                        message.SupplyOrder.TransportationService.SupplyInformationTaskId =
                            supplyInformationTaskRepository.Add(message.SupplyOrder.TransportationService.SupplyInformationTask);
                    } else {
                        if (message.SupplyOrder.TransportationService.SupplyInformationTask.Deleted) {
                            message.SupplyOrder.TransportationService.SupplyInformationTask.DeletedById = updatedBy.Id;

                            supplyInformationTaskRepository.Remove(message.SupplyOrder.TransportationService.SupplyInformationTask);

                            message.SupplyOrder.TransportationService.SupplyInformationTaskId = null;
                        } else {
                            message.SupplyOrder.TransportationService.SupplyInformationTask.UpdatedById = updatedBy.Id;
                            message.SupplyOrder.TransportationService.SupplyInformationTask.UserId = updatedBy.Id;

                            message.SupplyOrder.TransportationService.AccountingSupplyCostsWithinCountry =
                                message.SupplyOrder.TransportationService.SupplyInformationTask.GrossPrice;

                            supplyInformationTaskRepository.Update(message.SupplyOrder.TransportationService.SupplyInformationTask);
                        }
                    }
                }

                if (message.SupplyOrder.TransportationService.ActProvidingServiceDocument != null) {
                    if (message.SupplyOrder.TransportationService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.TransportationService.ActProvidingServiceDocument.Number =
                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.TransportationService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.SupplyOrder.TransportationService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.SupplyOrder.TransportationService.ActProvidingServiceDocument);
                    } else if (message.SupplyOrder.TransportationService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.TransportationService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.TransportationService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.SupplyOrder.TransportationService.SupplyServiceAccountDocument != null) {
                    if (message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.Number =
                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                        message.SupplyOrder.TransportationService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.SupplyOrder.TransportationService.SupplyServiceAccountDocument);
                    } else if (message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.TransportationService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.Id);
                    }
                }

                supplyRepositoriesFactory.NewTransportationServiceRepository(connection).Update(message.SupplyOrder.TransportationService);
            }
        }

        if (message.SupplyOrder.CustomAgencyService != null) {
            message.SupplyOrder.CustomAgencyService.NetPrice = Math.Round(
                message.SupplyOrder.CustomAgencyService.GrossPrice * 100 / Convert.ToDecimal(100 + message.SupplyOrder.CustomAgencyService.VatPercent),
                2
            );
            message.SupplyOrder.CustomAgencyService.Vat = Math.Round(
                message.SupplyOrder.CustomAgencyService.GrossPrice - message.SupplyOrder.CustomAgencyService.NetPrice,
                2
            );

            message.SupplyOrder.CustomAgencyService.AccountingNetPrice = Math.Round(
                message.SupplyOrder.CustomAgencyService.AccountingGrossPrice * 100 / Convert.ToDecimal(100 + message.SupplyOrder.CustomAgencyService.AccountingVatPercent),
                2
            );
            message.SupplyOrder.CustomAgencyService.AccountingVat = Math.Round(
                message.SupplyOrder.CustomAgencyService.AccountingGrossPrice - message.SupplyOrder.CustomAgencyService.AccountingNetPrice,
                2
            );

            if (message.SupplyOrder.CustomAgencyService.FromDate.HasValue)
                message.SupplyOrder.CustomAgencyService.FromDate = TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.FromDate.Value);

            if (message.SupplyOrder.CustomAgencyService.IsNew()) {
                if (message.SupplyOrder.CustomAgencyService.SupplyPaymentTask != null) {
                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.UserId = message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.User.Id;
                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomAgencyService;

                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate =
                        !message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate.Value);

                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.NetPrice = message.SupplyOrder.CustomAgencyService.NetPrice;
                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.CustomAgencyService.GrossPrice;

                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask);

                    messagesToSend.Add(new PaymentTaskMessage {
                        Amount = message.SupplyOrder.CustomAgencyService.GrossPrice,
                        Discount = Convert.ToDouble(message.SupplyOrder.CustomAgencyService.Vat),
                        CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                        PayToDate = message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate,
                        OrganisationName = message.SupplyOrder.CustomAgencyService?.CustomAgencyOrganization?.Name,
                        PaymentForm = "Custom agency"
                    });
                }

                if (message.SupplyOrder.CustomAgencyService.AccountingPaymentTask != null) {
                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.UserId = message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.User.Id;
                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomAgencyService;
                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.IsAccounting = true;

                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate =
                        !message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate.Value);

                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.NetPrice = message.SupplyOrder.CustomAgencyService.AccountingNetPrice;
                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.CustomAgencyService.AccountingGrossPrice;

                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTaskId =
                        supplyPaymentTaskRepository.Add(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask);

                    messagesToSend.Add(new PaymentTaskMessage {
                        Amount = message.SupplyOrder.CustomAgencyService.GrossPrice,
                        Discount = Convert.ToDouble(message.SupplyOrder.CustomAgencyService.Vat),
                        CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                        PayToDate = message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate,
                        OrganisationName = message.SupplyOrder.CustomAgencyService?.CustomAgencyOrganization?.Name,
                        PaymentForm = "Custom agency"
                    });
                }

                if (message.SupplyOrder.CustomAgencyService.SupplyInformationTask != null) {
                    message.SupplyOrder.CustomAgencyService.SupplyInformationTask.UserId = updatedBy.Id;
                    message.SupplyOrder.CustomAgencyService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                    message.SupplyOrder.CustomAgencyService.AccountingSupplyCostsWithinCountry =
                        message.SupplyOrder.CustomAgencyService.SupplyInformationTask.GrossPrice;

                    message.SupplyOrder.CustomAgencyService.SupplyInformationTaskId =
                        supplyInformationTaskRepository.Add(message.SupplyOrder.CustomAgencyService.SupplyInformationTask);
                }

                if (message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument != null) {
                    if (message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Number =
                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument);
                    } else if (message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument != null) {
                    if (message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Number =
                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                        message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument);
                    } else if (message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Id);
                    }
                }

                informationMessage.CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}";
                informationMessage.Title = $"�������� ���������� � {message.SupplyOrder.SupplyOrderNumber.Number}";
                informationMessage.Message = "����� ������� ";

                SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                    message.SupplyOrder.CustomAgencyService.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                else
                    message.SupplyOrder.CustomAgencyService.ServiceNumber = string.Format("P{0:D10}", 1);

                supplyServiceNumberRepository.Add(message.SupplyOrder.CustomAgencyService.ServiceNumber);

                message.SupplyOrder.CustomAgencyService.CustomAgencyOrganizationId = message.SupplyOrder.CustomAgencyService.CustomAgencyOrganization.Id;
                message.SupplyOrder.CustomAgencyService.UserId = headPolishLogistic.Id;
                message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreementId = message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Id;
                message.SupplyOrder.CustomAgencyServiceId = supplyRepositoriesFactory.NewCustomAgencyServiceRepository(connection).Add(message.SupplyOrder.CustomAgencyService);

                if (message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement != null &&
                    !message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.IsNew()) {
                    message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement =
                        supplyOrganizationAgreementRepository.GetById(message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Id);

                    message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.CurrentAmount =
                        Math.Round(message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.CurrentAmount - message.SupplyOrder.CustomAgencyService.GrossPrice, 2);

                    message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                        Math.Round(message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.AccountingCurrentAmount -
                                   message.SupplyOrder.CustomAgencyService.AccountingGrossPrice, 2);

                    supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement);
                }

                if (message.SupplyOrder.CustomAgencyService.InvoiceDocuments.Any(d => d.IsNew()))
                    invoiceDocumentRepository.Add(message.SupplyOrder.CustomAgencyService.InvoiceDocuments
                        .Where(d => d.IsNew())
                        .Select(d => {
                            d.CustomAgencyServiceId = message.SupplyOrder.CustomAgencyServiceId;

                            return d;
                        })
                    );

                if (message.SupplyOrder.CustomAgencyService.ServiceDetailItems.Any())
                    InsertOrUpdateServiceDetailItems(
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        message.SupplyOrder.CustomAgencyService.ServiceDetailItems
                            .Select(i => {
                                i.CustomAgencyServiceId = message.SupplyOrder.CustomAgencyServiceId;

                                return i;
                            })
                    );
            } else {
                CustomAgencyService existCustomAgencyService = supplyRepositoriesFactory
                    .NewCustomAgencyServiceRepository(connection)
                    .GetByIdWithoutIncludes(message.SupplyOrder.CustomAgencyService.Id);

                UpdateSupplyOrganizationAndAgreement(
                    supplyOrganizationAgreementRepository,
                    message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreementId,
                    existCustomAgencyService.GrossPrice,
                    existCustomAgencyService.AccountingGrossPrice,
                    message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Id,
                    message.SupplyOrder.CustomAgencyService.GrossPrice,
                    message.SupplyOrder.CustomAgencyService.AccountingGrossPrice);

                message.SupplyOrder.CustomAgencyService.CustomAgencyOrganizationId =
                    message.SupplyOrder.CustomAgencyService.CustomAgencyOrganization.Id;
                message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreementId =
                    message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Id;

                if (message.SupplyOrder.CustomAgencyService.InvoiceDocuments.Any()) {
                    invoiceDocumentRepository.RemoveAllByCustomAgencyServiceIdExceptProvided(
                        message.SupplyOrder.CustomAgencyService.Id,
                        message.SupplyOrder.CustomAgencyService.InvoiceDocuments.Where(d => !d.IsNew() && !d.Deleted).Select(d => d.Id)
                    );

                    if (message.SupplyOrder.CustomAgencyService.InvoiceDocuments.Any(d => d.IsNew()))
                        invoiceDocumentRepository.Add(message.SupplyOrder.CustomAgencyService.InvoiceDocuments
                            .Where(d => d.IsNew())
                            .Select(d => {
                                d.CustomAgencyServiceId = message.SupplyOrder.CustomAgencyService.Id;

                                return d;
                            })
                        );
                } else {
                    invoiceDocumentRepository.RemoveAllByCustomAgencyServiceId(message.SupplyOrder.CustomAgencyService.Id);
                }

                if (message.SupplyOrder.CustomAgencyService.ServiceDetailItems.Any()) {
                    serviceDetailItemRepository.RemoveAllByCustomAgencyServiceIdExceptProvided(
                        message.SupplyOrder.CustomAgencyService.Id,
                        message.SupplyOrder.CustomAgencyService.ServiceDetailItems.Where(i => !i.IsNew()).Select(i => i.Id)
                    );

                    InsertOrUpdateServiceDetailItems(
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        message.SupplyOrder.CustomAgencyService.ServiceDetailItems
                            .Select(i => {
                                i.CustomAgencyServiceId = message.SupplyOrder.CustomAgencyServiceId;

                                return i;
                            })
                    );
                } else {
                    serviceDetailItemRepository.RemoveAllByCustomAgencyServiceId(message.SupplyOrder.CustomAgencyService.Id);
                }

                if (message.SupplyOrder.CustomAgencyService.SupplyPaymentTask != null) {
                    if (message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.IsNew()) {
                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.UserId = message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.User.Id;
                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomAgencyService;

                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate =
                            !message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate.Value);

                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.NetPrice = message.SupplyOrder.CustomAgencyService.NetPrice;
                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.CustomAgencyService.GrossPrice;

                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.CustomAgencyService.GrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.CustomAgencyService.Vat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.CustomAgencyService?.CustomAgencyOrganization?.Name,
                            PaymentForm = "Custom agency"
                        });
                    } else {
                        if (message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.IsAvailableForPayment) {
                            if (message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.Id, updatedBy.Id);

                                message.SupplyOrder.CustomAgencyService.SupplyPaymentTaskId = null;
                            } else {
                                message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate =
                                    !message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate.Value);

                                message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.NetPrice = message.SupplyOrder.CustomAgencyService.NetPrice;
                                message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.CustomAgencyService.GrossPrice;
                                message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.UpdatedById = updatedBy.Id;

                                supplyPaymentTaskRepository.Update(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask);
                            }
                        }
                    }
                }

                if (message.SupplyOrder.CustomAgencyService.AccountingPaymentTask != null) {
                    if (message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.IsNew()) {
                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.UserId = message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.User.Id;
                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomAgencyService;
                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.IsAccounting = true;

                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate =
                            !message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate.Value);

                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.NetPrice = message.SupplyOrder.CustomAgencyService.AccountingNetPrice;
                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.CustomAgencyService.AccountingGrossPrice;

                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.CustomAgencyService.AccountingGrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.CustomAgencyService.AccountingVat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.CustomAgencyService?.CustomAgencyOrganization?.Name,
                            PaymentForm = "Custom agency"
                        });
                    } else {
                        if (message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.IsAvailableForPayment) {
                            if (message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.Id, updatedBy.Id);

                                message.SupplyOrder.CustomAgencyService.AccountingPaymentTaskId = null;
                            } else {
                                message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate =
                                    !message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate.Value);

                                message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.NetPrice = message.SupplyOrder.CustomAgencyService.AccountingNetPrice;
                                message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.CustomAgencyService.AccountingGrossPrice;
                                message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.UpdatedById = updatedBy.Id;

                                supplyPaymentTaskRepository.Update(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask);
                            }
                        }
                    }
                }

                if (message.SupplyOrder.CustomAgencyService.SupplyInformationTask != null) {
                    if (message.SupplyOrder.CustomAgencyService.SupplyInformationTask.IsNew()) {
                        message.SupplyOrder.CustomAgencyService.SupplyInformationTask.UserId = updatedBy.Id;
                        message.SupplyOrder.CustomAgencyService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                        message.SupplyOrder.CustomAgencyService.AccountingSupplyCostsWithinCountry =
                            message.SupplyOrder.CustomAgencyService.SupplyInformationTask.GrossPrice;

                        message.SupplyOrder.CustomAgencyService.SupplyInformationTaskId =
                            supplyInformationTaskRepository.Add(message.SupplyOrder.CustomAgencyService.SupplyInformationTask);
                    } else {
                        if (message.SupplyOrder.CustomAgencyService.SupplyInformationTask.Deleted) {
                            message.SupplyOrder.CustomAgencyService.SupplyInformationTask.DeletedById = updatedBy.Id;

                            supplyInformationTaskRepository.Remove(message.SupplyOrder.CustomAgencyService.SupplyInformationTask);

                            message.SupplyOrder.CustomAgencyService.SupplyInformationTaskId = null;
                        } else {
                            message.SupplyOrder.CustomAgencyService.SupplyInformationTask.UpdatedById = updatedBy.Id;
                            message.SupplyOrder.CustomAgencyService.SupplyInformationTask.UserId = updatedBy.Id;

                            message.SupplyOrder.CustomAgencyService.AccountingSupplyCostsWithinCountry =
                                message.SupplyOrder.CustomAgencyService.SupplyInformationTask.GrossPrice;

                            supplyInformationTaskRepository.Update(message.SupplyOrder.CustomAgencyService.SupplyInformationTask);
                        }
                    }
                }

                if (message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument != null) {
                    if (message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Number =
                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument);
                    } else if (message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument != null) {
                    if (message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Number =
                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                        message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument);
                    } else if (message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Id);
                    }
                }

                supplyRepositoriesFactory.NewCustomAgencyServiceRepository(connection).Update(message.SupplyOrder.CustomAgencyService);
            }
        }
    }

    private static void CreateOrUpdatePlaneServices(
        UpdateSupplyOrderMessage message,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IDbConnection connection,
        ISupplyPaymentTaskRepository supplyPaymentTaskRepository,
        IInvoiceDocumentRepository invoiceDocumentRepository,
        IServiceDetailItemRepository serviceDetailItemRepository,
        IServiceDetailItemKeyRepository serviceDetailItemKeyRepository,
        ISupplyServiceNumberRepository supplyServiceNumberRepository,
        ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository,
        ICollection<PaymentTaskMessage> messagesToSend,
        InformationMessage informationMessage,
        User updatedBy,
        User headPolishLogistic,
        ISupplyInformationTaskRepository supplyInformationTaskRepository,
        IActProvidingServiceDocumentRepository actProvidingServiceDocumentRepository,
        ISupplyServiceAccountDocumentRepository supplyServiceAccountDocumentRepository) {
        if (message.SupplyOrder.PlaneDeliveryService != null) {
            message.SupplyOrder.PlaneDeliveryService.NetPrice = Math.Round(
                message.SupplyOrder.PlaneDeliveryService.GrossPrice * 100 / Convert.ToDecimal(100 + message.SupplyOrder.PlaneDeliveryService.VatPercent),
                2
            );
            message.SupplyOrder.PlaneDeliveryService.Vat = Math.Round(
                message.SupplyOrder.PlaneDeliveryService.GrossPrice - message.SupplyOrder.PlaneDeliveryService.NetPrice,
                2
            );

            message.SupplyOrder.PlaneDeliveryService.AccountingNetPrice = Math.Round(
                message.SupplyOrder.PlaneDeliveryService.AccountingGrossPrice * 100 / Convert.ToDecimal(100 + message.SupplyOrder.PlaneDeliveryService.AccountingVatPercent),
                2
            );
            message.SupplyOrder.PlaneDeliveryService.AccountingVat = Math.Round(
                message.SupplyOrder.PlaneDeliveryService.AccountingGrossPrice - message.SupplyOrder.PlaneDeliveryService.AccountingNetPrice,
                2
            );

            if (message.SupplyOrder.PlaneDeliveryService.FromDate.HasValue)
                message.SupplyOrder.PlaneDeliveryService.FromDate = TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PlaneDeliveryService.FromDate.Value);

            if (message.SupplyOrder.PlaneDeliveryService.IsNew()) {
                if (message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask != null) {
                    message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.UserId = message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.User.Id;
                    message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.PlaneDeliveryService;

                    message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.PayToDate =
                        !message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.PayToDate.Value);

                    message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.NetPrice = message.SupplyOrder.PlaneDeliveryService.NetPrice;
                    message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.PlaneDeliveryService.GrossPrice;

                    message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask);

                    messagesToSend.Add(new PaymentTaskMessage {
                        Amount = message.SupplyOrder.PlaneDeliveryService.GrossPrice,
                        Discount = Convert.ToDouble(message.SupplyOrder.PlaneDeliveryService.Vat),
                        CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                        PayToDate = message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.PayToDate,
                        OrganisationName = message.SupplyOrder.PlaneDeliveryService?.PlaneDeliveryOrganization?.Name,
                        PaymentForm = "Plane delivery"
                    });
                }

                if (message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask != null) {
                    message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.UserId = message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.User.Id;
                    message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.PlaneDeliveryService;
                    message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.IsAccounting = true;

                    message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.PayToDate =
                        !message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.PayToDate.Value);

                    message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.NetPrice = message.SupplyOrder.PlaneDeliveryService.AccountingNetPrice;
                    message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.PlaneDeliveryService.AccountingGrossPrice;

                    message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTaskId =
                        supplyPaymentTaskRepository.Add(message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask);

                    messagesToSend.Add(new PaymentTaskMessage {
                        Amount = message.SupplyOrder.PlaneDeliveryService.AccountingGrossPrice,
                        Discount = Convert.ToDouble(message.SupplyOrder.PlaneDeliveryService.AccountingVat),
                        CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                        PayToDate = message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.PayToDate,
                        OrganisationName = message.SupplyOrder.PlaneDeliveryService?.PlaneDeliveryOrganization?.Name,
                        PaymentForm = "Plane delivery"
                    });
                }

                if (message.SupplyOrder.PlaneDeliveryService.SupplyInformationTask != null) {
                    message.SupplyOrder.PlaneDeliveryService.SupplyInformationTask.UserId = updatedBy.Id;
                    message.SupplyOrder.PlaneDeliveryService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                    message.SupplyOrder.PlaneDeliveryService.AccountingSupplyCostsWithinCountry =
                        message.SupplyOrder.PlaneDeliveryService.SupplyInformationTask.GrossPrice;

                    message.SupplyOrder.PlaneDeliveryService.SupplyInformationTaskId =
                        supplyInformationTaskRepository.Add(message.SupplyOrder.PlaneDeliveryService.SupplyInformationTask);
                }

                if (message.SupplyOrder.PlaneDeliveryService.ActProvidingServiceDocument != null) {
                    if (message.SupplyOrder.PlaneDeliveryService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.PlaneDeliveryService.ActProvidingServiceDocument.Number =
                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.PlaneDeliveryService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.SupplyOrder.PlaneDeliveryService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.SupplyOrder.PlaneDeliveryService.ActProvidingServiceDocument);
                    } else if (message.SupplyOrder.PlaneDeliveryService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.PlaneDeliveryService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.PlaneDeliveryService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.SupplyOrder.PlaneDeliveryService.SupplyServiceAccountDocument != null) {
                    if (message.SupplyOrder.PlaneDeliveryService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.PlaneDeliveryService.SupplyServiceAccountDocument.Number =
                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.PlaneDeliveryService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                        message.SupplyOrder.PlaneDeliveryService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.SupplyOrder.PlaneDeliveryService.SupplyServiceAccountDocument);
                    } else if (message.SupplyOrder.PlaneDeliveryService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.PlaneDeliveryService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.PlaneDeliveryService.SupplyServiceAccountDocument.Id);
                    }
                }

                if (message.SupplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement != null &&
                    !message.SupplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement.IsNew()) {
                    message.SupplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement =
                        supplyOrganizationAgreementRepository.GetById(message.SupplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement.Id);

                    message.SupplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement.CurrentAmount =
                        Math.Round(message.SupplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement.CurrentAmount - message.SupplyOrder.PlaneDeliveryService.GrossPrice,
                            2);

                    message.SupplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                        Math.Round(message.SupplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement.AccountingCurrentAmount -
                                   message.SupplyOrder.PlaneDeliveryService.AccountingGrossPrice,
                            2);

                    supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.SupplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement);
                }

                informationMessage.CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}";
                informationMessage.Title = $"�������� ���������� � {message.SupplyOrder.SupplyOrderNumber.Number}";
                informationMessage.Message = "�������� ������";

                SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                    message.SupplyOrder.PlaneDeliveryService.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                else
                    message.SupplyOrder.PlaneDeliveryService.ServiceNumber = string.Format("P{0:D10}", 1);

                supplyServiceNumberRepository.Add(message.SupplyOrder.PlaneDeliveryService.ServiceNumber);

                message.SupplyOrder.PlaneDeliveryService.PlaneDeliveryOrganizationId = message.SupplyOrder.PlaneDeliveryService.PlaneDeliveryOrganization?.Id;
                message.SupplyOrder.PlaneDeliveryService.UserId = headPolishLogistic.Id;

                if (message.SupplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement != null)
                    message.SupplyOrder.PlaneDeliveryService.SupplyOrganizationAgreementId = message.SupplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement.Id;
                message.SupplyOrder.PlaneDeliveryServiceId =
                    supplyRepositoriesFactory.NewPlaneDeliveryServiceRepository(connection).Add(message.SupplyOrder.PlaneDeliveryService);

                if (message.SupplyOrder.PlaneDeliveryService.InvoiceDocuments.Any(d => d.IsNew()))
                    invoiceDocumentRepository.Add(message.SupplyOrder.PlaneDeliveryService.InvoiceDocuments
                        .Where(d => d.IsNew())
                        .Select(d => {
                            d.PlaneDeliveryServiceId = message.SupplyOrder.PlaneDeliveryServiceId;

                            return d;
                        })
                    );

                if (message.SupplyOrder.PlaneDeliveryService.ServiceDetailItems.Any())
                    InsertOrUpdateServiceDetailItems(
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        message.SupplyOrder.PlaneDeliveryService.ServiceDetailItems
                            .Select(i => {
                                i.PlaneDeliveryServiceId = message.SupplyOrder.PlaneDeliveryServiceId;

                                return i;
                            })
                    );
            } else {
                PlaneDeliveryService existPlaneDeliveryService = supplyRepositoriesFactory
                    .NewPlaneDeliveryServiceRepository(connection)
                    .GetByIdWithoutIncludes(message.SupplyOrder.PlaneDeliveryService.Id);

                UpdateSupplyOrganizationAndAgreement(
                    supplyOrganizationAgreementRepository,
                    message.SupplyOrder.PlaneDeliveryService.SupplyOrganizationAgreementId,
                    existPlaneDeliveryService.GrossPrice,
                    existPlaneDeliveryService.AccountingGrossPrice,
                    message.SupplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement.Id,
                    message.SupplyOrder.PlaneDeliveryService.GrossPrice,
                    message.SupplyOrder.PlaneDeliveryService.AccountingGrossPrice);

                message.SupplyOrder.PlaneDeliveryService.PlaneDeliveryOrganizationId =
                    message.SupplyOrder.PlaneDeliveryService.PlaneDeliveryOrganization.Id;
                message.SupplyOrder.PlaneDeliveryService.SupplyOrganizationAgreementId =
                    message.SupplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement.Id;

                if (message.SupplyOrder.PlaneDeliveryService.InvoiceDocuments.Any()) {
                    invoiceDocumentRepository.RemoveAllByPlaneDeliveryServiceIdExceptProvided(
                        message.SupplyOrder.PlaneDeliveryService.Id,
                        message.SupplyOrder.PlaneDeliveryService.InvoiceDocuments.Where(d => !d.IsNew() && !d.Deleted).Select(d => d.Id)
                    );

                    if (message.SupplyOrder.PlaneDeliveryService.InvoiceDocuments.Any(d => d.IsNew()))
                        invoiceDocumentRepository.Add(message.SupplyOrder.PlaneDeliveryService.InvoiceDocuments
                            .Where(d => d.IsNew())
                            .Select(d => {
                                d.PlaneDeliveryServiceId = message.SupplyOrder.PlaneDeliveryService.Id;

                                return d;
                            })
                        );
                } else {
                    invoiceDocumentRepository.RemoveAllByPlaneDeliveryServiceId(message.SupplyOrder.PlaneDeliveryService.Id);
                }

                if (message.SupplyOrder.PlaneDeliveryService.ServiceDetailItems.Any()) {
                    serviceDetailItemRepository.RemoveAllByPlaneDeliveryServiceIdExceptProvided(
                        message.SupplyOrder.PlaneDeliveryService.Id,
                        message.SupplyOrder.PlaneDeliveryService.ServiceDetailItems.Where(i => !i.IsNew()).Select(i => i.Id)
                    );

                    InsertOrUpdateServiceDetailItems(
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        message.SupplyOrder.PlaneDeliveryService.ServiceDetailItems
                            .Select(i => {
                                i.PlaneDeliveryServiceId = message.SupplyOrder.PlaneDeliveryServiceId;

                                return i;
                            })
                    );
                } else {
                    serviceDetailItemRepository.RemoveAllByPlaneDeliveryServiceId(message.SupplyOrder.PlaneDeliveryService.Id);
                }

                if (message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask != null) {
                    if (message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.IsNew()) {
                        message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.UserId = message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.User.Id;
                        message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.PlaneDeliveryService;

                        message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.PayToDate =
                            !message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.PayToDate.Value);

                        message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.NetPrice = message.SupplyOrder.PlaneDeliveryService.NetPrice;
                        message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.PlaneDeliveryService.GrossPrice;

                        message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.PlaneDeliveryService.GrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.PlaneDeliveryService.Vat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.PlaneDeliveryService?.PlaneDeliveryOrganization?.Name,
                            PaymentForm = "Plane delivery"
                        });
                    } else {
                        if (message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.IsAvailableForPayment) {
                            if (message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.Id, updatedBy.Id);

                                message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTaskId = null;
                            } else {
                                message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.PayToDate =
                                    !message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.PayToDate.Value);

                                message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.NetPrice = message.SupplyOrder.PlaneDeliveryService.NetPrice;
                                message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.PlaneDeliveryService.GrossPrice;
                                message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask.UpdatedById = updatedBy.Id;

                                supplyPaymentTaskRepository.Update(message.SupplyOrder.PlaneDeliveryService.SupplyPaymentTask);
                            }
                        }
                    }
                }

                if (message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask != null) {
                    if (message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.IsNew()) {
                        message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.UserId = message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.User.Id;
                        message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.PlaneDeliveryService;
                        message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.IsAccounting = true;

                        message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.PayToDate =
                            !message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.PayToDate.Value);

                        message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.NetPrice = message.SupplyOrder.PlaneDeliveryService.AccountingNetPrice;
                        message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.PlaneDeliveryService.AccountingGrossPrice;

                        message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.PlaneDeliveryService.AccountingGrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.PlaneDeliveryService.AccountingVat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.PlaneDeliveryService?.PlaneDeliveryOrganization?.Name,
                            PaymentForm = "Plane delivery"
                        });
                    } else {
                        if (message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.IsAvailableForPayment) {
                            if (message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.Id, updatedBy.Id);

                                message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTaskId = null;
                            } else {
                                message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.PayToDate =
                                    !message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.PayToDate.Value);

                                message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.NetPrice = message.SupplyOrder.PlaneDeliveryService.AccountingNetPrice;
                                message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.PlaneDeliveryService.AccountingGrossPrice;
                                message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask.UpdatedById = updatedBy.Id;

                                supplyPaymentTaskRepository.Update(message.SupplyOrder.PlaneDeliveryService.AccountingPaymentTask);
                            }
                        }
                    }
                }

                if (message.SupplyOrder.PlaneDeliveryService.SupplyInformationTask != null) {
                    if (message.SupplyOrder.PlaneDeliveryService.SupplyInformationTask.IsNew()) {
                        message.SupplyOrder.PlaneDeliveryService.SupplyInformationTask.UserId = updatedBy.Id;
                        message.SupplyOrder.PlaneDeliveryService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                        message.SupplyOrder.PlaneDeliveryService.AccountingSupplyCostsWithinCountry =
                            message.SupplyOrder.PlaneDeliveryService.SupplyInformationTask.GrossPrice;

                        message.SupplyOrder.PlaneDeliveryService.SupplyInformationTaskId =
                            supplyInformationTaskRepository.Add(message.SupplyOrder.PlaneDeliveryService.SupplyInformationTask);
                    } else {
                        if (message.SupplyOrder.PlaneDeliveryService.SupplyInformationTask.Deleted) {
                            message.SupplyOrder.PlaneDeliveryService.SupplyInformationTask.DeletedById = updatedBy.Id;

                            supplyInformationTaskRepository.Remove(message.SupplyOrder.PlaneDeliveryService.SupplyInformationTask);

                            message.SupplyOrder.PlaneDeliveryService.SupplyInformationTaskId = null;
                        } else {
                            message.SupplyOrder.PlaneDeliveryService.SupplyInformationTask.UpdatedById = updatedBy.Id;
                            message.SupplyOrder.PlaneDeliveryService.SupplyInformationTask.UserId = updatedBy.Id;

                            message.SupplyOrder.PlaneDeliveryService.AccountingSupplyCostsWithinCountry =
                                message.SupplyOrder.PlaneDeliveryService.SupplyInformationTask.GrossPrice;

                            supplyInformationTaskRepository.Update(message.SupplyOrder.PlaneDeliveryService.SupplyInformationTask);
                        }
                    }
                }

                if (message.SupplyOrder.PlaneDeliveryService.ActProvidingServiceDocument != null) {
                    if (message.SupplyOrder.PlaneDeliveryService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.PlaneDeliveryService.ActProvidingServiceDocument.Number =
                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.PlaneDeliveryService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.SupplyOrder.PlaneDeliveryService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.SupplyOrder.PlaneDeliveryService.ActProvidingServiceDocument);
                    } else if (message.SupplyOrder.PlaneDeliveryService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.PlaneDeliveryService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.PlaneDeliveryService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.SupplyOrder.PlaneDeliveryService.SupplyServiceAccountDocument != null) {
                    if (message.SupplyOrder.PlaneDeliveryService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.PlaneDeliveryService.SupplyServiceAccountDocument.Number =
                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.PlaneDeliveryService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                        message.SupplyOrder.PlaneDeliveryService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.SupplyOrder.PlaneDeliveryService.SupplyServiceAccountDocument);
                    } else if (message.SupplyOrder.PlaneDeliveryService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.PlaneDeliveryService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.PlaneDeliveryService.SupplyServiceAccountDocument.Id);
                    }
                }

                supplyRepositoriesFactory.NewPlaneDeliveryServiceRepository(connection).Update(message.SupplyOrder.PlaneDeliveryService);
            }
        }

        if (message.SupplyOrder.CustomAgencyService != null) {
            message.SupplyOrder.CustomAgencyService.NetPrice = Math.Round(
                message.SupplyOrder.CustomAgencyService.GrossPrice * 100 / Convert.ToDecimal(100 + message.SupplyOrder.CustomAgencyService.VatPercent),
                2
            );
            message.SupplyOrder.CustomAgencyService.Vat = Math.Round(
                message.SupplyOrder.CustomAgencyService.GrossPrice - message.SupplyOrder.CustomAgencyService.NetPrice,
                2
            );

            message.SupplyOrder.CustomAgencyService.AccountingNetPrice = Math.Round(
                message.SupplyOrder.CustomAgencyService.AccountingGrossPrice * 100 / Convert.ToDecimal(100 + message.SupplyOrder.CustomAgencyService.AccountingVatPercent),
                2
            );
            message.SupplyOrder.CustomAgencyService.AccountingVat = Math.Round(
                message.SupplyOrder.CustomAgencyService.AccountingGrossPrice - message.SupplyOrder.CustomAgencyService.AccountingNetPrice,
                2
            );

            if (message.SupplyOrder.CustomAgencyService.FromDate.HasValue)
                message.SupplyOrder.CustomAgencyService.FromDate = TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.FromDate.Value);

            if (message.SupplyOrder.CustomAgencyService.IsNew()) {
                if (message.SupplyOrder.CustomAgencyService.SupplyPaymentTask != null) {
                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.UserId = message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.User.Id;
                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomAgencyService;

                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate =
                        !message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate.Value);

                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.NetPrice = message.SupplyOrder.CustomAgencyService.NetPrice;
                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.CustomAgencyService.GrossPrice;

                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask);

                    messagesToSend.Add(new PaymentTaskMessage {
                        Amount = message.SupplyOrder.CustomAgencyService.GrossPrice,
                        Discount = Convert.ToDouble(message.SupplyOrder.CustomAgencyService.Vat),
                        CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                        PayToDate = message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate,
                        OrganisationName = message.SupplyOrder.CustomAgencyService?.CustomAgencyOrganization?.Name,
                        PaymentForm = "Custom agency"
                    });
                }

                if (message.SupplyOrder.CustomAgencyService.AccountingPaymentTask != null) {
                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.UserId = message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.User.Id;
                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomAgencyService;
                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.IsAccounting = true;

                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate =
                        !message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate.Value);

                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.NetPrice = message.SupplyOrder.CustomAgencyService.AccountingNetPrice;
                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.CustomAgencyService.AccountingGrossPrice;

                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTaskId =
                        supplyPaymentTaskRepository.Add(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask);

                    messagesToSend.Add(new PaymentTaskMessage {
                        Amount = message.SupplyOrder.CustomAgencyService.AccountingGrossPrice,
                        Discount = Convert.ToDouble(message.SupplyOrder.CustomAgencyService.AccountingVat),
                        CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                        PayToDate = message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate,
                        OrganisationName = message.SupplyOrder.CustomAgencyService?.CustomAgencyOrganization?.Name,
                        PaymentForm = "Custom agency"
                    });
                }

                if (message.SupplyOrder.CustomAgencyService.SupplyInformationTask != null) {
                    message.SupplyOrder.CustomAgencyService.SupplyInformationTask.UserId = updatedBy.Id;
                    message.SupplyOrder.CustomAgencyService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                    message.SupplyOrder.CustomAgencyService.AccountingSupplyCostsWithinCountry =
                        message.SupplyOrder.CustomAgencyService.SupplyInformationTask.GrossPrice;

                    message.SupplyOrder.CustomAgencyService.SupplyInformationTaskId =
                        supplyInformationTaskRepository.Add(message.SupplyOrder.CustomAgencyService.SupplyInformationTask);
                }

                if (message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument != null) {
                    if (message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Number =
                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument);
                    } else if (message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument != null) {
                    if (message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Number =
                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                        message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument);
                    } else if (message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Id);
                    }
                }

                if (message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement != null &&
                    !message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.IsNew()) {
                    message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement =
                        supplyOrganizationAgreementRepository.GetById(message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Id);

                    message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.CurrentAmount =
                        Math.Round(message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.CurrentAmount - message.SupplyOrder.CustomAgencyService.GrossPrice,
                            2);

                    message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                        Math.Round(message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.AccountingCurrentAmount -
                                   message.SupplyOrder.CustomAgencyService.AccountingGrossPrice,
                            2);

                    supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement);
                }

                informationMessage.CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}";
                informationMessage.Title = $"�������� ���������� � {message.SupplyOrder.SupplyOrderNumber.Number}";
                informationMessage.Message = "����� ������� ";

                SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                    message.SupplyOrder.CustomAgencyService.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                else
                    message.SupplyOrder.CustomAgencyService.ServiceNumber = string.Format("P{0:D10}", 1);

                supplyServiceNumberRepository.Add(message.SupplyOrder.CustomAgencyService.ServiceNumber);

                message.SupplyOrder.CustomAgencyService.CustomAgencyOrganizationId = message.SupplyOrder.CustomAgencyService.CustomAgencyOrganization?.Id;
                message.SupplyOrder.CustomAgencyService.UserId = headPolishLogistic.Id;
                message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreementId = message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Id;
                message.SupplyOrder.CustomAgencyServiceId = supplyRepositoriesFactory.NewCustomAgencyServiceRepository(connection).Add(message.SupplyOrder.CustomAgencyService);

                if (message.SupplyOrder.CustomAgencyService.InvoiceDocuments.Any(d => d.IsNew()))
                    invoiceDocumentRepository.Add(message.SupplyOrder.CustomAgencyService.InvoiceDocuments
                        .Where(d => d.IsNew())
                        .Select(d => {
                            d.CustomAgencyServiceId = message.SupplyOrder.CustomAgencyServiceId;

                            return d;
                        })
                    );

                if (message.SupplyOrder.CustomAgencyService.ServiceDetailItems.Any())
                    InsertOrUpdateServiceDetailItems(
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        message.SupplyOrder.CustomAgencyService.ServiceDetailItems
                            .Select(i => {
                                i.CustomAgencyServiceId = message.SupplyOrder.CustomAgencyServiceId;

                                return i;
                            })
                    );
            } else {
                CustomAgencyService existCustomAgencyService = supplyRepositoriesFactory
                    .NewCustomAgencyServiceRepository(connection)
                    .GetByIdWithoutIncludes(message.SupplyOrder.CustomAgencyService.Id);

                UpdateSupplyOrganizationAndAgreement(
                    supplyOrganizationAgreementRepository,
                    message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreementId,
                    existCustomAgencyService.GrossPrice,
                    existCustomAgencyService.AccountingGrossPrice,
                    message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Id,
                    message.SupplyOrder.CustomAgencyService.GrossPrice,
                    message.SupplyOrder.CustomAgencyService.AccountingGrossPrice);

                message.SupplyOrder.CustomAgencyService.CustomAgencyOrganizationId =
                    message.SupplyOrder.CustomAgencyService.CustomAgencyOrganization.Id;
                message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreementId =
                    message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Id;

                if (message.SupplyOrder.CustomAgencyService.InvoiceDocuments.Any()) {
                    invoiceDocumentRepository.RemoveAllByCustomAgencyServiceIdExceptProvided(
                        message.SupplyOrder.CustomAgencyService.Id,
                        message.SupplyOrder.CustomAgencyService.InvoiceDocuments.Where(d => !d.IsNew() && !d.Deleted).Select(d => d.Id)
                    );

                    if (message.SupplyOrder.CustomAgencyService.InvoiceDocuments.Any(d => d.IsNew()))
                        invoiceDocumentRepository.Add(message.SupplyOrder.CustomAgencyService.InvoiceDocuments
                            .Where(d => d.IsNew())
                            .Select(d => {
                                d.CustomAgencyServiceId = message.SupplyOrder.CustomAgencyService.Id;

                                return d;
                            })
                        );
                } else {
                    invoiceDocumentRepository.RemoveAllByCustomAgencyServiceId(message.SupplyOrder.CustomAgencyService.Id);
                }

                if (message.SupplyOrder.CustomAgencyService.ServiceDetailItems.Any()) {
                    serviceDetailItemRepository.RemoveAllByCustomAgencyServiceIdExceptProvided(
                        message.SupplyOrder.CustomAgencyService.Id,
                        message.SupplyOrder.CustomAgencyService.ServiceDetailItems.Where(i => !i.IsNew()).Select(i => i.Id)
                    );

                    InsertOrUpdateServiceDetailItems(
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        message.SupplyOrder.CustomAgencyService.ServiceDetailItems
                            .Select(i => {
                                i.CustomAgencyServiceId = message.SupplyOrder.CustomAgencyServiceId;

                                return i;
                            })
                    );
                } else {
                    serviceDetailItemRepository.RemoveAllByCustomAgencyServiceId(message.SupplyOrder.CustomAgencyService.Id);
                }

                if (message.SupplyOrder.CustomAgencyService.SupplyPaymentTask != null) {
                    if (message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.IsNew()) {
                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.UserId = message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.User.Id;
                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomAgencyService;

                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate =
                            !message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate.Value);

                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.NetPrice = message.SupplyOrder.CustomAgencyService.NetPrice;
                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.CustomAgencyService.GrossPrice;

                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.CustomAgencyService.GrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.CustomAgencyService.Vat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.CustomAgencyService?.CustomAgencyOrganization?.Name,
                            PaymentForm = "Custom agency"
                        });
                    } else {
                        if (message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.IsAvailableForPayment) {
                            if (message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.Id, updatedBy.Id);

                                message.SupplyOrder.CustomAgencyService.SupplyPaymentTaskId = null;
                            } else {
                                message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate =
                                    !message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate.Value);

                                message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.NetPrice = message.SupplyOrder.CustomAgencyService.NetPrice;
                                message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.CustomAgencyService.GrossPrice;
                                message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.UpdatedById = updatedBy.Id;

                                supplyPaymentTaskRepository.Update(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask);
                            }
                        }
                    }
                }

                if (message.SupplyOrder.CustomAgencyService.AccountingPaymentTask != null) {
                    if (message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.IsNew()) {
                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.UserId = message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.User.Id;
                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomAgencyService;
                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.IsAccounting = true;

                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate =
                            !message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate.Value);

                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.NetPrice = message.SupplyOrder.CustomAgencyService.AccountingNetPrice;
                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.CustomAgencyService.AccountingGrossPrice;

                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.CustomAgencyService.AccountingGrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.CustomAgencyService.AccountingVat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.CustomAgencyService?.CustomAgencyOrganization?.Name,
                            PaymentForm = "Custom agency"
                        });
                    } else {
                        if (message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.IsAvailableForPayment) {
                            if (message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.Id, updatedBy.Id);

                                message.SupplyOrder.CustomAgencyService.AccountingPaymentTaskId = null;
                            } else {
                                message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate =
                                    !message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate.Value);

                                message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.NetPrice = message.SupplyOrder.CustomAgencyService.AccountingNetPrice;
                                message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.CustomAgencyService.AccountingGrossPrice;
                                message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.UpdatedById = updatedBy.Id;

                                supplyPaymentTaskRepository.Update(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask);
                            }
                        }
                    }
                }

                if (message.SupplyOrder.CustomAgencyService.SupplyInformationTask != null) {
                    if (message.SupplyOrder.CustomAgencyService.SupplyInformationTask.IsNew()) {
                        message.SupplyOrder.CustomAgencyService.SupplyInformationTask.UserId = updatedBy.Id;
                        message.SupplyOrder.CustomAgencyService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                        message.SupplyOrder.CustomAgencyService.AccountingSupplyCostsWithinCountry =
                            message.SupplyOrder.CustomAgencyService.SupplyInformationTask.GrossPrice;

                        message.SupplyOrder.CustomAgencyService.SupplyInformationTaskId =
                            supplyInformationTaskRepository.Add(message.SupplyOrder.CustomAgencyService.SupplyInformationTask);
                    } else {
                        if (message.SupplyOrder.CustomAgencyService.SupplyInformationTask.Deleted) {
                            message.SupplyOrder.CustomAgencyService.SupplyInformationTask.DeletedById = updatedBy.Id;

                            supplyInformationTaskRepository.Remove(message.SupplyOrder.CustomAgencyService.SupplyInformationTask);

                            message.SupplyOrder.CustomAgencyService.SupplyInformationTaskId = null;
                        } else {
                            message.SupplyOrder.CustomAgencyService.SupplyInformationTask.UpdatedById = updatedBy.Id;
                            message.SupplyOrder.CustomAgencyService.SupplyInformationTask.UserId = updatedBy.Id;

                            message.SupplyOrder.CustomAgencyService.AccountingSupplyCostsWithinCountry =
                                message.SupplyOrder.CustomAgencyService.SupplyInformationTask.GrossPrice;

                            supplyInformationTaskRepository.Update(message.SupplyOrder.CustomAgencyService.SupplyInformationTask);
                        }
                    }
                }

                if (message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument != null) {
                    if (message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Number =
                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument);
                    } else if (message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument != null) {
                    if (message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Number =
                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                        message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument);
                    } else if (message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Id);
                    }
                }

                supplyRepositoriesFactory.NewCustomAgencyServiceRepository(connection).Update(message.SupplyOrder.CustomAgencyService);
            }
        }

        if (message.SupplyOrder.TransportationService != null) {
            message.SupplyOrder.TransportationService.NetPrice = Math.Round(
                message.SupplyOrder.TransportationService.GrossPrice * 100 / Convert.ToDecimal(100 + message.SupplyOrder.TransportationService.VatPercent),
                2
            );
            message.SupplyOrder.TransportationService.Vat = Math.Round(
                message.SupplyOrder.TransportationService.GrossPrice - message.SupplyOrder.TransportationService.NetPrice,
                2
            );

            message.SupplyOrder.TransportationService.AccountingNetPrice = Math.Round(
                message.SupplyOrder.TransportationService.AccountingGrossPrice * 100 / Convert.ToDecimal(100 + message.SupplyOrder.TransportationService.AccountingVatPercent),
                2
            );
            message.SupplyOrder.TransportationService.AccountingVat = Math.Round(
                message.SupplyOrder.TransportationService.AccountingGrossPrice - message.SupplyOrder.TransportationService.AccountingNetPrice,
                2
            );

            if (message.SupplyOrder.TransportationService.FromDate.HasValue)
                message.SupplyOrder.TransportationService.FromDate = TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.TransportationService.FromDate.Value);

            if (message.SupplyOrder.TransportationService.IsNew()) {
                if (message.SupplyOrder.TransportationService.SupplyPaymentTask != null) {
                    message.SupplyOrder.TransportationService.SupplyPaymentTask.UserId = message.SupplyOrder.TransportationService.SupplyPaymentTask.User.Id;
                    message.SupplyOrder.TransportationService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.SupplyOrder.TransportationService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.TransportationService;

                    message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate =
                        !message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate.Value);

                    message.SupplyOrder.TransportationService.SupplyPaymentTask.NetPrice = message.SupplyOrder.TransportationService.NetPrice;
                    message.SupplyOrder.TransportationService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.TransportationService.GrossPrice;

                    message.SupplyOrder.TransportationService.SupplyPaymentTaskId =
                        supplyPaymentTaskRepository.Add(message.SupplyOrder.TransportationService.SupplyPaymentTask);

                    messagesToSend.Add(new PaymentTaskMessage {
                        Amount = message.SupplyOrder.TransportationService.GrossPrice,
                        Discount = Convert.ToDouble(message.SupplyOrder.TransportationService.Vat),
                        CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                        PayToDate = message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate,
                        OrganisationName = message.SupplyOrder.TransportationService?.TransportationOrganization?.Name,
                        PaymentForm = "Transportation service"
                    });
                }

                if (message.SupplyOrder.TransportationService.AccountingPaymentTask != null) {
                    message.SupplyOrder.TransportationService.AccountingPaymentTask.UserId = message.SupplyOrder.TransportationService.AccountingPaymentTask.User.Id;
                    message.SupplyOrder.TransportationService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.SupplyOrder.TransportationService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.TransportationService;
                    message.SupplyOrder.TransportationService.AccountingPaymentTask.IsAccounting = true;

                    message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate =
                        !message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate.Value);

                    message.SupplyOrder.TransportationService.AccountingPaymentTask.NetPrice = message.SupplyOrder.TransportationService.AccountingNetPrice;
                    message.SupplyOrder.TransportationService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.TransportationService.AccountingGrossPrice;

                    message.SupplyOrder.TransportationService.AccountingPaymentTaskId =
                        supplyPaymentTaskRepository.Add(message.SupplyOrder.TransportationService.AccountingPaymentTask);

                    messagesToSend.Add(new PaymentTaskMessage {
                        Amount = message.SupplyOrder.TransportationService.AccountingGrossPrice,
                        Discount = Convert.ToDouble(message.SupplyOrder.TransportationService.AccountingVat),
                        CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                        PayToDate = message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate,
                        OrganisationName = message.SupplyOrder.TransportationService?.TransportationOrganization?.Name,
                        PaymentForm = "Transportation service"
                    });
                }

                if (message.SupplyOrder.TransportationService.SupplyInformationTask != null) {
                    message.SupplyOrder.TransportationService.SupplyInformationTask.UserId = updatedBy.Id;
                    message.SupplyOrder.TransportationService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                    message.SupplyOrder.TransportationService.AccountingSupplyCostsWithinCountry =
                        message.SupplyOrder.TransportationService.SupplyInformationTask.GrossPrice;

                    message.SupplyOrder.TransportationService.SupplyInformationTaskId =
                        supplyInformationTaskRepository.Add(message.SupplyOrder.TransportationService.SupplyInformationTask);
                }

                if (message.SupplyOrder.TransportationService.ActProvidingServiceDocument != null) {
                    if (message.SupplyOrder.TransportationService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.TransportationService.ActProvidingServiceDocument.Number =
                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.TransportationService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.SupplyOrder.TransportationService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.SupplyOrder.TransportationService.ActProvidingServiceDocument);
                    } else if (message.SupplyOrder.TransportationService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.TransportationService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.TransportationService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.SupplyOrder.TransportationService.SupplyServiceAccountDocument != null) {
                    if (message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.Number =
                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                        message.SupplyOrder.TransportationService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.SupplyOrder.TransportationService.SupplyServiceAccountDocument);
                    } else if (message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.TransportationService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.Id);
                    }
                }

                if (message.SupplyOrder.TransportationService.SupplyOrganizationAgreement != null &&
                    !message.SupplyOrder.TransportationService.SupplyOrganizationAgreement.IsNew()) {
                    message.SupplyOrder.TransportationService.SupplyOrganizationAgreement =
                        supplyOrganizationAgreementRepository.GetById(message.SupplyOrder.TransportationService.SupplyOrganizationAgreement.Id);

                    message.SupplyOrder.TransportationService.SupplyOrganizationAgreement.CurrentAmount =
                        Math.Round(
                            message.SupplyOrder.TransportationService.SupplyOrganizationAgreement.CurrentAmount - message.SupplyOrder.TransportationService.GrossPrice,
                            2);

                    message.SupplyOrder.TransportationService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                        Math.Round(
                            message.SupplyOrder.TransportationService.SupplyOrganizationAgreement.AccountingCurrentAmount -
                            message.SupplyOrder.TransportationService.AccountingGrossPrice,
                            2);

                    supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.SupplyOrder.TransportationService.SupplyOrganizationAgreement);
                }

                informationMessage.CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}";
                informationMessage.Title = $"�������� ���������� � {message.SupplyOrder.SupplyOrderNumber.Number}";
                informationMessage.Message = "����������� �������";

                SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                    message.SupplyOrder.TransportationService.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                else
                    message.SupplyOrder.TransportationService.ServiceNumber = string.Format("P{0:D10}", 1);

                supplyServiceNumberRepository.Add(message.SupplyOrder.TransportationService.ServiceNumber);

                message.SupplyOrder.TransportationService.TransportationOrganizationId = message.SupplyOrder.TransportationService.TransportationOrganization.Id;
                message.SupplyOrder.TransportationService.UserId = headPolishLogistic.Id;
                message.SupplyOrder.TransportationService.SupplyOrganizationAgreementId = message.SupplyOrder.TransportationService.SupplyOrganizationAgreement.Id;
                message.SupplyOrder.TransportationServiceId =
                    supplyRepositoriesFactory.NewTransportationServiceRepository(connection).Add(message.SupplyOrder.TransportationService);

                if (message.SupplyOrder.TransportationService.InvoiceDocuments.Any(d => d.IsNew()))
                    invoiceDocumentRepository.Add(message.SupplyOrder.TransportationService.InvoiceDocuments
                        .Where(d => d.IsNew())
                        .Select(d => {
                            d.TransportationServiceId = message.SupplyOrder.TransportationServiceId;

                            return d;
                        })
                    );

                if (message.SupplyOrder.TransportationService.ServiceDetailItems.Any())
                    InsertOrUpdateServiceDetailItems(
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        message.SupplyOrder.TransportationService.ServiceDetailItems
                            .Select(i => {
                                i.TransportationServiceId = message.SupplyOrder.TransportationServiceId;

                                return i;
                            })
                    );
            } else {
                TransportationService existTransportationService = supplyRepositoriesFactory
                    .NewTransportationServiceRepository(connection)
                    .GetByIdWithoutIncludes(message.SupplyOrder.TransportationService.Id);

                UpdateSupplyOrganizationAndAgreement(
                    supplyOrganizationAgreementRepository,
                    message.SupplyOrder.TransportationService.SupplyOrganizationAgreementId,
                    existTransportationService.GrossPrice,
                    existTransportationService.AccountingGrossPrice,
                    message.SupplyOrder.TransportationService.SupplyOrganizationAgreement.Id,
                    message.SupplyOrder.TransportationService.GrossPrice,
                    message.SupplyOrder.TransportationService.AccountingGrossPrice);

                message.SupplyOrder.TransportationService.TransportationOrganizationId =
                    message.SupplyOrder.TransportationService.TransportationOrganization.Id;
                message.SupplyOrder.TransportationService.SupplyOrganizationAgreementId =
                    message.SupplyOrder.TransportationService.SupplyOrganizationAgreement.Id;

                if (message.SupplyOrder.TransportationService.InvoiceDocuments.Any()) {
                    invoiceDocumentRepository.RemoveAllByTransportationServiceIdExceptProvided(
                        message.SupplyOrder.TransportationService.Id,
                        message.SupplyOrder.TransportationService.InvoiceDocuments.Where(d => !d.IsNew() && !d.Deleted).Select(d => d.Id)
                    );

                    if (message.SupplyOrder.TransportationService.InvoiceDocuments.Any(d => d.IsNew()))
                        invoiceDocumentRepository.Add(message.SupplyOrder.TransportationService.InvoiceDocuments
                            .Where(d => d.IsNew())
                            .Select(d => {
                                d.TransportationServiceId = message.SupplyOrder.TransportationService.Id;

                                return d;
                            })
                        );
                } else {
                    invoiceDocumentRepository.RemoveAllByTransportationServiceId(message.SupplyOrder.TransportationService.Id);
                }

                if (message.SupplyOrder.TransportationService.ServiceDetailItems.Any()) {
                    serviceDetailItemRepository.RemoveAllByTransportationServiceIdExceptProvided(
                        message.SupplyOrder.TransportationService.Id,
                        message.SupplyOrder.TransportationService.ServiceDetailItems.Where(i => !i.IsNew()).Select(i => i.Id)
                    );

                    InsertOrUpdateServiceDetailItems(
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        message.SupplyOrder.TransportationService.ServiceDetailItems
                            .Select(i => {
                                i.TransportationServiceId = message.SupplyOrder.TransportationServiceId;

                                return i;
                            })
                    );
                } else {
                    serviceDetailItemRepository.RemoveAllByTransportationServiceId(message.SupplyOrder.TransportationService.Id);
                }

                if (message.SupplyOrder.TransportationService.SupplyPaymentTask != null) {
                    if (message.SupplyOrder.TransportationService.SupplyPaymentTask.IsNew()) {
                        message.SupplyOrder.TransportationService.SupplyPaymentTask.UserId = message.SupplyOrder.TransportationService.SupplyPaymentTask.User.Id;
                        message.SupplyOrder.TransportationService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.TransportationService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.TransportationService;

                        message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate =
                            !message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate.Value);

                        message.SupplyOrder.TransportationService.SupplyPaymentTask.NetPrice = message.SupplyOrder.TransportationService.NetPrice;
                        message.SupplyOrder.TransportationService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.TransportationService.GrossPrice;

                        message.SupplyOrder.TransportationService.SupplyPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.SupplyOrder.TransportationService.SupplyPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.TransportationService.GrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.TransportationService.Vat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.TransportationService?.TransportationOrganization?.Name,
                            PaymentForm = "Transportation service"
                        });
                    } else {
                        if (message.SupplyOrder.TransportationService.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.SupplyOrder.TransportationService.SupplyPaymentTask.IsAvailableForPayment) {
                            if (message.SupplyOrder.TransportationService.SupplyPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.TransportationService.SupplyPaymentTask.Id, updatedBy.Id);

                                message.SupplyOrder.TransportationService.SupplyPaymentTaskId = null;
                            } else {
                                message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate =
                                    !message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.TransportationService.SupplyPaymentTask.PayToDate.Value);

                                message.SupplyOrder.TransportationService.SupplyPaymentTask.NetPrice = message.SupplyOrder.TransportationService.NetPrice;
                                message.SupplyOrder.TransportationService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.TransportationService.GrossPrice;
                                message.SupplyOrder.TransportationService.SupplyPaymentTask.UpdatedById = updatedBy.Id;

                                supplyPaymentTaskRepository.Update(message.SupplyOrder.TransportationService.SupplyPaymentTask);
                            }
                        }
                    }
                }

                if (message.SupplyOrder.TransportationService.AccountingPaymentTask != null) {
                    if (message.SupplyOrder.TransportationService.AccountingPaymentTask.IsNew()) {
                        message.SupplyOrder.TransportationService.AccountingPaymentTask.UserId = message.SupplyOrder.TransportationService.AccountingPaymentTask.User.Id;
                        message.SupplyOrder.TransportationService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.TransportationService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.TransportationService;
                        message.SupplyOrder.TransportationService.AccountingPaymentTask.IsAccounting = true;

                        message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate =
                            !message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate.Value);

                        message.SupplyOrder.TransportationService.AccountingPaymentTask.NetPrice = message.SupplyOrder.TransportationService.AccountingNetPrice;
                        message.SupplyOrder.TransportationService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.TransportationService.AccountingGrossPrice;

                        message.SupplyOrder.TransportationService.AccountingPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.SupplyOrder.TransportationService.AccountingPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.TransportationService.AccountingGrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.TransportationService.AccountingVat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.TransportationService?.TransportationOrganization?.Name,
                            PaymentForm = "Transportation service"
                        });
                    } else {
                        if (message.SupplyOrder.TransportationService.AccountingPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.SupplyOrder.TransportationService.AccountingPaymentTask.IsAvailableForPayment) {
                            if (message.SupplyOrder.TransportationService.AccountingPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.TransportationService.AccountingPaymentTask.Id, updatedBy.Id);

                                message.SupplyOrder.TransportationService.AccountingPaymentTaskId = null;
                            } else {
                                message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate =
                                    !message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.TransportationService.AccountingPaymentTask.PayToDate.Value);

                                message.SupplyOrder.TransportationService.AccountingPaymentTask.NetPrice = message.SupplyOrder.TransportationService.AccountingNetPrice;
                                message.SupplyOrder.TransportationService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.TransportationService.AccountingGrossPrice;
                                message.SupplyOrder.TransportationService.AccountingPaymentTask.UpdatedById = updatedBy.Id;

                                supplyPaymentTaskRepository.Update(message.SupplyOrder.TransportationService.AccountingPaymentTask);
                            }
                        }
                    }
                }

                if (message.SupplyOrder.TransportationService.SupplyInformationTask != null) {
                    if (message.SupplyOrder.TransportationService.SupplyInformationTask.IsNew()) {
                        message.SupplyOrder.TransportationService.SupplyInformationTask.UserId = updatedBy.Id;
                        message.SupplyOrder.TransportationService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                        message.SupplyOrder.TransportationService.AccountingSupplyCostsWithinCountry =
                            message.SupplyOrder.TransportationService.SupplyInformationTask.GrossPrice;

                        message.SupplyOrder.TransportationService.SupplyInformationTaskId =
                            supplyInformationTaskRepository.Add(message.SupplyOrder.TransportationService.SupplyInformationTask);
                    } else {
                        if (message.SupplyOrder.TransportationService.SupplyInformationTask.Deleted) {
                            message.SupplyOrder.TransportationService.SupplyInformationTask.DeletedById = updatedBy.Id;

                            supplyInformationTaskRepository.Remove(message.SupplyOrder.TransportationService.SupplyInformationTask);

                            message.SupplyOrder.TransportationService.SupplyInformationTaskId = null;
                        } else {
                            message.SupplyOrder.TransportationService.SupplyInformationTask.UpdatedById = updatedBy.Id;
                            message.SupplyOrder.TransportationService.SupplyInformationTask.UserId = updatedBy.Id;

                            message.SupplyOrder.TransportationService.AccountingSupplyCostsWithinCountry =
                                message.SupplyOrder.TransportationService.SupplyInformationTask.GrossPrice;

                            supplyInformationTaskRepository.Update(message.SupplyOrder.TransportationService.SupplyInformationTask);
                        }
                    }
                }

                if (message.SupplyOrder.TransportationService.ActProvidingServiceDocument != null) {
                    if (message.SupplyOrder.TransportationService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.TransportationService.ActProvidingServiceDocument.Number =
                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.TransportationService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.SupplyOrder.TransportationService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.SupplyOrder.TransportationService.ActProvidingServiceDocument);
                    } else if (message.SupplyOrder.TransportationService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.TransportationService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.TransportationService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.SupplyOrder.TransportationService.SupplyServiceAccountDocument != null) {
                    if (message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.Number =
                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                        message.SupplyOrder.TransportationService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.SupplyOrder.TransportationService.SupplyServiceAccountDocument);
                    } else if (message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.TransportationService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.TransportationService.SupplyServiceAccountDocument.Id);
                    }
                }

                supplyRepositoriesFactory.NewTransportationServiceRepository(connection).Update(message.SupplyOrder.TransportationService);
            }
        }
    }

    private static void CreateOrUpdateVehicleServices(
        UpdateSupplyOrderMessage message,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IDbConnection connection,
        ISupplyPaymentTaskRepository supplyPaymentTaskRepository,
        IInvoiceDocumentRepository invoiceDocumentRepository,
        IServiceDetailItemRepository serviceDetailItemRepository,
        IServiceDetailItemKeyRepository serviceDetailItemKeyRepository,
        ISupplyServiceNumberRepository supplyServiceNumberRepository,
        ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository,
        ICollection<PaymentTaskMessage> messagesToSend,
        InformationMessage informationMessage,
        User updatedBy,
        User headPolishLogistic,
        ISupplyInformationTaskRepository supplyInformationTaskRepository,
        IActProvidingServiceDocumentRepository actProvidingServiceDocumentRepository,
        ISupplyServiceAccountDocumentRepository supplyServiceAccountDocumentRepository) {
        if (message.SupplyOrder.VehicleDeliveryService != null) {
            message.SupplyOrder.VehicleDeliveryService.NetPrice = Math.Round(
                message.SupplyOrder.VehicleDeliveryService.GrossPrice * 100 / Convert.ToDecimal(100 + message.SupplyOrder.VehicleDeliveryService.VatPercent),
                2
            );
            message.SupplyOrder.VehicleDeliveryService.Vat = Math.Round(
                message.SupplyOrder.VehicleDeliveryService.GrossPrice - message.SupplyOrder.VehicleDeliveryService.NetPrice,
                2
            );

            message.SupplyOrder.VehicleDeliveryService.AccountingNetPrice = Math.Round(
                message.SupplyOrder.VehicleDeliveryService.AccountingGrossPrice * 100 /
                Convert.ToDecimal(100 + message.SupplyOrder.VehicleDeliveryService.AccountingVatPercent),
                2
            );
            message.SupplyOrder.VehicleDeliveryService.AccountingVat = Math.Round(
                message.SupplyOrder.VehicleDeliveryService.AccountingGrossPrice - message.SupplyOrder.VehicleDeliveryService.AccountingNetPrice,
                2
            );

            if (message.SupplyOrder.VehicleDeliveryService.FromDate.HasValue)
                message.SupplyOrder.VehicleDeliveryService.FromDate = TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.VehicleDeliveryService.FromDate.Value);

            try {
                if (message.SupplyOrder.VehicleDeliveryService.IsNew()) {
                    if (message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask != null) {
                        message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.UserId = message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.User.Id;
                        message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.VehicleDeliveryService;

                        message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.PayToDate =
                            !message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.PayToDate.Value);

                        message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.NetPrice = message.SupplyOrder.VehicleDeliveryService.NetPrice;
                        message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.VehicleDeliveryService.GrossPrice;

                        message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.VehicleDeliveryService.GrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.VehicleDeliveryService.Vat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.VehicleDeliveryService?.VehicleDeliveryOrganization?.Name,
                            PaymentForm = "Vehicle"
                        });
                    }

                    if (message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask != null) {
                        message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.UserId = message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.User.Id;
                        message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.VehicleDeliveryService;
                        message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.IsAccounting = true;

                        message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.PayToDate =
                            !message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.PayToDate.Value);

                        message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.NetPrice = message.SupplyOrder.VehicleDeliveryService.AccountingNetPrice;
                        message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.VehicleDeliveryService.AccountingGrossPrice;

                        message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.VehicleDeliveryService.AccountingGrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.VehicleDeliveryService.AccountingVat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.VehicleDeliveryService?.VehicleDeliveryOrganization?.Name,
                            PaymentForm = "Vehicle"
                        });
                    }

                    if (message.SupplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement != null &&
                        !message.SupplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement.IsNew()) {
                        message.SupplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement =
                            supplyOrganizationAgreementRepository.GetById(message.SupplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement.Id);

                        message.SupplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement.CurrentAmount =
                            Math.Round(
                                message.SupplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement.CurrentAmount -
                                message.SupplyOrder.VehicleDeliveryService.GrossPrice,
                                2);

                        message.SupplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                            Math.Round(
                                message.SupplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement.AccountingCurrentAmount -
                                message.SupplyOrder.VehicleDeliveryService.AccountingGrossPrice,
                                2);

                        supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.SupplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement);
                    }

                    if (message.SupplyOrder.VehicleDeliveryService.SupplyInformationTask != null) {
                        message.SupplyOrder.VehicleDeliveryService.SupplyInformationTask.UserId = updatedBy.Id;
                        message.SupplyOrder.VehicleDeliveryService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                        message.SupplyOrder.VehicleDeliveryService.AccountingSupplyCostsWithinCountry =
                            message.SupplyOrder.VehicleDeliveryService.SupplyInformationTask.GrossPrice;

                        message.SupplyOrder.VehicleDeliveryService.SupplyInformationTaskId =
                            supplyInformationTaskRepository.Add(message.SupplyOrder.VehicleDeliveryService.SupplyInformationTask);
                    }

                    if (message.SupplyOrder.VehicleDeliveryService.ActProvidingServiceDocument != null) {
                        if (message.SupplyOrder.VehicleDeliveryService.ActProvidingServiceDocument.IsNew()) {
                            ActProvidingServiceDocument lastRecord =
                                actProvidingServiceDocumentRepository.GetLastRecord();

                            if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                !string.IsNullOrEmpty(lastRecord.Number))
                                message.SupplyOrder.VehicleDeliveryService.ActProvidingServiceDocument.Number =
                                    string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                            else
                                message.SupplyOrder.VehicleDeliveryService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                            message.SupplyOrder.VehicleDeliveryService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                                .New(message.SupplyOrder.VehicleDeliveryService.ActProvidingServiceDocument);
                        } else if (message.SupplyOrder.VehicleDeliveryService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                            message.SupplyOrder.VehicleDeliveryService.ActProvidingServiceDocumentId = null;
                            actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.VehicleDeliveryService.ActProvidingServiceDocument.Id);
                        }
                    }

                    if (message.SupplyOrder.VehicleDeliveryService.SupplyServiceAccountDocument != null) {
                        if (message.SupplyOrder.VehicleDeliveryService.SupplyServiceAccountDocument.IsNew()) {
                            SupplyServiceAccountDocument lastRecord =
                                supplyServiceAccountDocumentRepository.GetLastRecord();

                            if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                !string.IsNullOrEmpty(lastRecord.Number))
                                message.SupplyOrder.VehicleDeliveryService.SupplyServiceAccountDocument.Number =
                                    string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                            else
                                message.SupplyOrder.VehicleDeliveryService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                            message.SupplyOrder.VehicleDeliveryService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                                .New(message.SupplyOrder.VehicleDeliveryService.SupplyServiceAccountDocument);
                        } else if (message.SupplyOrder.VehicleDeliveryService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                            message.SupplyOrder.VehicleDeliveryService.SupplyServiceAccountDocumentId = null;
                            supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.VehicleDeliveryService.SupplyServiceAccountDocument.Id);
                        }
                    }

                    informationMessage.CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}";
                    informationMessage.Title = $"�������� ���������� � {message.SupplyOrder.SupplyOrderNumber.Number}";
                    informationMessage.Message = "����������� ��������";

                    SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                    if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                        message.SupplyOrder.VehicleDeliveryService.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                    else
                        message.SupplyOrder.VehicleDeliveryService.ServiceNumber = string.Format("P{0:D10}", 1);

                    supplyServiceNumberRepository.Add(message.SupplyOrder.VehicleDeliveryService.ServiceNumber);

                    message.SupplyOrder.VehicleDeliveryService.VehicleDeliveryOrganizationId = message.SupplyOrder.VehicleDeliveryService.VehicleDeliveryOrganization?.Id;
                    message.SupplyOrder.VehicleDeliveryService.UserId = headPolishLogistic.Id;
                    if (message.SupplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement != null)
                        message.SupplyOrder.VehicleDeliveryService.SupplyOrganizationAgreementId = message.SupplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement.Id;
                    message.SupplyOrder.VehicleDeliveryServiceId =
                        supplyRepositoriesFactory.NewVehicleDeliveryServiceRepository(connection).Add(message.SupplyOrder.VehicleDeliveryService);

                    if (message.SupplyOrder.VehicleDeliveryService.InvoiceDocuments.Any(d => d.IsNew()))
                        invoiceDocumentRepository.Add(message.SupplyOrder.VehicleDeliveryService.InvoiceDocuments
                            .Where(d => d.IsNew())
                            .Select(d => {
                                d.VehicleDeliveryServiceId = message.SupplyOrder.VehicleDeliveryServiceId;

                                return d;
                            })
                        );

                    if (message.SupplyOrder.VehicleDeliveryService.ServiceDetailItems.Any())
                        InsertOrUpdateServiceDetailItems(
                            serviceDetailItemRepository,
                            serviceDetailItemKeyRepository,
                            message.SupplyOrder.VehicleDeliveryService.ServiceDetailItems
                                .Select(i => {
                                    i.VehicleDeliveryServiceId = message.SupplyOrder.VehicleDeliveryServiceId;

                                    return i;
                                })
                        );
                } else {
                    VehicleDeliveryService existVehicleDeliveryService = supplyRepositoriesFactory
                        .NewVehicleDeliveryServiceRepository(connection)
                        .GetByIdWithoutIncludes(message.SupplyOrder.VehicleDeliveryService.Id);

                    UpdateSupplyOrganizationAndAgreement(
                        supplyOrganizationAgreementRepository,
                        message.SupplyOrder.VehicleDeliveryService.SupplyOrganizationAgreementId,
                        existVehicleDeliveryService.GrossPrice,
                        existVehicleDeliveryService.AccountingGrossPrice,
                        message.SupplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement.Id,
                        message.SupplyOrder.VehicleDeliveryService.GrossPrice,
                        message.SupplyOrder.VehicleDeliveryService.AccountingGrossPrice);

                    message.SupplyOrder.VehicleDeliveryService.VehicleDeliveryOrganizationId =
                        message.SupplyOrder.VehicleDeliveryService.VehicleDeliveryOrganization.Id;
                    message.SupplyOrder.VehicleDeliveryService.SupplyOrganizationAgreementId =
                        message.SupplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement.Id;

                    if (message.SupplyOrder.VehicleDeliveryService.SupplyInformationTask != null) {
                        if (message.SupplyOrder.VehicleDeliveryService.SupplyInformationTask.IsNew()) {
                            message.SupplyOrder.VehicleDeliveryService.SupplyInformationTask.UserId = updatedBy.Id;
                            message.SupplyOrder.VehicleDeliveryService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                            message.SupplyOrder.VehicleDeliveryService.AccountingSupplyCostsWithinCountry =
                                message.SupplyOrder.VehicleDeliveryService.SupplyInformationTask.GrossPrice;

                            message.SupplyOrder.VehicleDeliveryService.SupplyInformationTaskId =
                                supplyInformationTaskRepository.Add(message.SupplyOrder.VehicleDeliveryService.SupplyInformationTask);
                        } else {
                            if (message.SupplyOrder.VehicleDeliveryService.SupplyInformationTask.Deleted) {
                                message.SupplyOrder.VehicleDeliveryService.SupplyInformationTask.DeletedById = updatedBy.Id;

                                supplyInformationTaskRepository.Remove(message.SupplyOrder.VehicleDeliveryService.SupplyInformationTask);

                                message.SupplyOrder.VehicleDeliveryService.SupplyInformationTaskId = null;
                            } else {
                                message.SupplyOrder.VehicleDeliveryService.SupplyInformationTask.UpdatedById = updatedBy.Id;
                                message.SupplyOrder.VehicleDeliveryService.SupplyInformationTask.UserId = updatedBy.Id;

                                message.SupplyOrder.VehicleDeliveryService.AccountingSupplyCostsWithinCountry =
                                    message.SupplyOrder.VehicleDeliveryService.SupplyInformationTask.GrossPrice;

                                supplyInformationTaskRepository.Update(message.SupplyOrder.VehicleDeliveryService.SupplyInformationTask);
                            }
                        }
                    }

                    if (message.SupplyOrder.VehicleDeliveryService.ActProvidingServiceDocument != null) {
                        if (message.SupplyOrder.VehicleDeliveryService.ActProvidingServiceDocument.IsNew()) {
                            ActProvidingServiceDocument lastRecord =
                                actProvidingServiceDocumentRepository.GetLastRecord();

                            if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                !string.IsNullOrEmpty(lastRecord.Number))
                                message.SupplyOrder.VehicleDeliveryService.ActProvidingServiceDocument.Number =
                                    string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                            else
                                message.SupplyOrder.VehicleDeliveryService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                            message.SupplyOrder.VehicleDeliveryService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                                .New(message.SupplyOrder.VehicleDeliveryService.ActProvidingServiceDocument);
                        } else if (message.SupplyOrder.VehicleDeliveryService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                            message.SupplyOrder.VehicleDeliveryService.ActProvidingServiceDocumentId = null;
                            actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.VehicleDeliveryService.ActProvidingServiceDocument.Id);
                        }
                    }

                    if (message.SupplyOrder.VehicleDeliveryService.InvoiceDocuments.Any()) {
                        invoiceDocumentRepository.RemoveAllByVehicleDeliveryServiceIdExceptProvided(
                            message.SupplyOrder.VehicleDeliveryService.Id,
                            message.SupplyOrder.VehicleDeliveryService.InvoiceDocuments.Where(d => !d.IsNew() && !d.Deleted).Select(d => d.Id)
                        );

                        if (message.SupplyOrder.VehicleDeliveryService.InvoiceDocuments.Any(d => d.IsNew()))
                            invoiceDocumentRepository.Add(message.SupplyOrder.VehicleDeliveryService.InvoiceDocuments
                                .Where(d => d.IsNew())
                                .Select(d => {
                                    d.VehicleDeliveryServiceId = message.SupplyOrder.VehicleDeliveryService.Id;

                                    return d;
                                })
                            );
                    } else {
                        invoiceDocumentRepository.RemoveAllByVehicleDeliveryServiceId(message.SupplyOrder.VehicleDeliveryService.Id);
                    }

                    if (message.SupplyOrder.VehicleDeliveryService.ServiceDetailItems.Any()) {
                        serviceDetailItemRepository.RemoveAllByVehicleDeliveryServiceIdExceptProvided(
                            message.SupplyOrder.VehicleDeliveryService.Id,
                            message.SupplyOrder.VehicleDeliveryService.ServiceDetailItems.Where(i => !i.IsNew()).Select(i => i.Id)
                        );

                        InsertOrUpdateServiceDetailItems(
                            serviceDetailItemRepository,
                            serviceDetailItemKeyRepository,
                            message.SupplyOrder.VehicleDeliveryService.ServiceDetailItems
                                .Select(i => {
                                    i.VehicleDeliveryServiceId = message.SupplyOrder.VehicleDeliveryServiceId;

                                    return i;
                                })
                        );
                    } else {
                        serviceDetailItemRepository.RemoveAllByVehicleDeliveryServiceId(message.SupplyOrder.VehicleDeliveryService.Id);
                    }

                    if (message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask != null) {
                        if (message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.IsNew()) {
                            message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.UserId = message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.User.Id;
                            message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                            message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.VehicleDeliveryService;

                            message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.PayToDate =
                                !message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.PayToDate.HasValue
                                    ? DateTime.UtcNow
                                    : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.PayToDate.Value);

                            message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.NetPrice = message.SupplyOrder.VehicleDeliveryService.NetPrice;
                            message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.VehicleDeliveryService.GrossPrice;

                            message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTaskId =
                                supplyPaymentTaskRepository.Add(message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask);

                            messagesToSend.Add(new PaymentTaskMessage {
                                Amount = message.SupplyOrder.VehicleDeliveryService.GrossPrice,
                                Discount = Convert.ToDouble(message.SupplyOrder.VehicleDeliveryService.Vat),
                                CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                                PayToDate = message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.PayToDate,
                                OrganisationName = message.SupplyOrder.VehicleDeliveryService?.VehicleDeliveryOrganization?.Name,
                                PaymentForm = "Vehicle"
                            });
                        } else {
                            if (message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                                && !message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.IsAvailableForPayment) {
                                if (message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.Deleted) {
                                    supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.Id, updatedBy.Id);

                                    message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTaskId = null;
                                } else {
                                    message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.PayToDate =
                                        !message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.PayToDate.HasValue
                                            ? DateTime.UtcNow
                                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.PayToDate.Value);

                                    message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.NetPrice = message.SupplyOrder.VehicleDeliveryService.NetPrice;
                                    message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.VehicleDeliveryService.GrossPrice;
                                    message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask.UpdatedById = updatedBy.Id;

                                    supplyPaymentTaskRepository.Update(message.SupplyOrder.VehicleDeliveryService.SupplyPaymentTask);
                                }
                            }
                        }
                    }

                    if (message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask != null) {
                        if (message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.IsNew()) {
                            message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.UserId = message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.User.Id;
                            message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                            message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.VehicleDeliveryService;
                            message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.IsAccounting = true;

                            message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.PayToDate =
                                !message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.PayToDate.HasValue
                                    ? DateTime.UtcNow
                                    : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.PayToDate.Value);

                            message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.NetPrice = message.SupplyOrder.VehicleDeliveryService.AccountingNetPrice;
                            message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.VehicleDeliveryService.AccountingGrossPrice;

                            message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTaskId =
                                supplyPaymentTaskRepository.Add(message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask);

                            messagesToSend.Add(new PaymentTaskMessage {
                                Amount = message.SupplyOrder.VehicleDeliveryService.AccountingGrossPrice,
                                Discount = Convert.ToDouble(message.SupplyOrder.VehicleDeliveryService.AccountingVat),
                                CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                                PayToDate = message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.PayToDate,
                                OrganisationName = message.SupplyOrder.VehicleDeliveryService?.VehicleDeliveryOrganization?.Name,
                                PaymentForm = "Vehicle"
                            });
                        } else {
                            if (message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                                && !message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.IsAvailableForPayment) {
                                if (message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.Deleted) {
                                    supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.Id, updatedBy.Id);

                                    message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTaskId = null;
                                } else {
                                    message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.PayToDate =
                                        !message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.PayToDate.HasValue
                                            ? DateTime.UtcNow
                                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.PayToDate.Value);

                                    message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.NetPrice = message.SupplyOrder.VehicleDeliveryService.AccountingNetPrice;
                                    message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.GrossPrice =
                                        message.SupplyOrder.VehicleDeliveryService.AccountingGrossPrice;
                                    message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask.UpdatedById = updatedBy.Id;

                                    supplyPaymentTaskRepository.Update(message.SupplyOrder.VehicleDeliveryService.AccountingPaymentTask);
                                }
                            }
                        }
                    }

                    if (message.SupplyOrder.VehicleDeliveryService.SupplyServiceAccountDocument != null) {
                        if (message.SupplyOrder.VehicleDeliveryService.SupplyServiceAccountDocument.IsNew()) {
                            SupplyServiceAccountDocument lastRecord =
                                supplyServiceAccountDocumentRepository.GetLastRecord();

                            if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                !string.IsNullOrEmpty(lastRecord.Number))
                                message.SupplyOrder.VehicleDeliveryService.SupplyServiceAccountDocument.Number =
                                    string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                            else
                                message.SupplyOrder.VehicleDeliveryService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                            message.SupplyOrder.VehicleDeliveryService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                                .New(message.SupplyOrder.VehicleDeliveryService.SupplyServiceAccountDocument);
                        } else if (message.SupplyOrder.VehicleDeliveryService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                            message.SupplyOrder.VehicleDeliveryService.SupplyServiceAccountDocumentId = null;
                            supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.VehicleDeliveryService.SupplyServiceAccountDocument.Id);
                        }
                    }

                    supplyRepositoriesFactory.NewVehicleDeliveryServiceRepository(connection).Update(message.SupplyOrder.VehicleDeliveryService);
                }
            } catch (Exception e) {
                Console.WriteLine(e);
                throw;
            }
        }

        if (message.SupplyOrder.CustomAgencyService != null) {
            message.SupplyOrder.CustomAgencyService.NetPrice = Math.Round(
                message.SupplyOrder.CustomAgencyService.GrossPrice * 100 / Convert.ToDecimal(100 + message.SupplyOrder.CustomAgencyService.VatPercent),
                2
            );
            message.SupplyOrder.CustomAgencyService.Vat = Math.Round(
                message.SupplyOrder.CustomAgencyService.GrossPrice - message.SupplyOrder.CustomAgencyService.NetPrice,
                2
            );

            message.SupplyOrder.CustomAgencyService.AccountingNetPrice = Math.Round(
                message.SupplyOrder.CustomAgencyService.AccountingGrossPrice * 100 / Convert.ToDecimal(100 + message.SupplyOrder.CustomAgencyService.AccountingVatPercent),
                2
            );
            message.SupplyOrder.CustomAgencyService.AccountingVat = Math.Round(
                message.SupplyOrder.CustomAgencyService.AccountingGrossPrice - message.SupplyOrder.CustomAgencyService.AccountingNetPrice,
                2
            );

            if (message.SupplyOrder.CustomAgencyService.FromDate.HasValue)
                message.SupplyOrder.CustomAgencyService.FromDate = TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.FromDate.Value);

            if (message.SupplyOrder.CustomAgencyService.IsNew()) {
                if (message.SupplyOrder.CustomAgencyService.SupplyPaymentTask != null) {
                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.UserId = message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.User.Id;
                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomAgencyService;

                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate =
                        !message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate.Value);

                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.NetPrice = message.SupplyOrder.CustomAgencyService.NetPrice;
                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.CustomAgencyService.GrossPrice;

                    message.SupplyOrder.CustomAgencyService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask);

                    messagesToSend.Add(new PaymentTaskMessage {
                        Amount = message.SupplyOrder.CustomAgencyService.GrossPrice,
                        Discount = Convert.ToDouble(message.SupplyOrder.CustomAgencyService.Vat),
                        CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                        PayToDate = message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate,
                        OrganisationName = message.SupplyOrder.CustomAgencyService?.CustomAgencyOrganization?.Name,
                        PaymentForm = "Custom agency"
                    });
                }

                if (message.SupplyOrder.CustomAgencyService.AccountingPaymentTask != null) {
                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.UserId = message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.User.Id;
                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomAgencyService;
                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.IsAccounting = true;

                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate =
                        !message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate.Value);

                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.NetPrice = message.SupplyOrder.CustomAgencyService.AccountingNetPrice;
                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.CustomAgencyService.AccountingGrossPrice;

                    message.SupplyOrder.CustomAgencyService.AccountingPaymentTaskId =
                        supplyPaymentTaskRepository.Add(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask);

                    messagesToSend.Add(new PaymentTaskMessage {
                        Amount = message.SupplyOrder.CustomAgencyService.AccountingGrossPrice,
                        Discount = Convert.ToDouble(message.SupplyOrder.CustomAgencyService.AccountingVat),
                        CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                        PayToDate = message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate,
                        OrganisationName = message.SupplyOrder.CustomAgencyService?.CustomAgencyOrganization?.Name,
                        PaymentForm = "Custom agency"
                    });
                }

                if (message.SupplyOrder.CustomAgencyService.SupplyInformationTask != null) {
                    message.SupplyOrder.CustomAgencyService.SupplyInformationTask.UserId = updatedBy.Id;
                    message.SupplyOrder.CustomAgencyService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                    message.SupplyOrder.CustomAgencyService.AccountingSupplyCostsWithinCountry =
                        message.SupplyOrder.CustomAgencyService.SupplyInformationTask.GrossPrice;

                    message.SupplyOrder.CustomAgencyService.SupplyInformationTaskId =
                        supplyInformationTaskRepository.Add(message.SupplyOrder.CustomAgencyService.SupplyInformationTask);
                }

                if (message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument != null) {
                    if (message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Number =
                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument);
                    } else if (message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument != null) {
                    if (message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Number =
                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                        message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument);
                    } else if (message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Id);
                    }
                }

                if (message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement != null &&
                    !message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.IsNew()) {
                    message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement =
                        supplyOrganizationAgreementRepository.GetById(message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Id);

                    message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.CurrentAmount =
                        Math.Round(message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.CurrentAmount - message.SupplyOrder.CustomAgencyService.GrossPrice,
                            2);

                    message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                        Math.Round(message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.AccountingCurrentAmount -
                                   message.SupplyOrder.CustomAgencyService.AccountingGrossPrice,
                            2);

                    supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement);
                }

                informationMessage.CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}";
                informationMessage.Title = $"�������� ���������� � {message.SupplyOrder.SupplyOrderNumber.Number}";
                informationMessage.Message = "����� ������� ";

                SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                    message.SupplyOrder.CustomAgencyService.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                else
                    message.SupplyOrder.CustomAgencyService.ServiceNumber = string.Format("P{0:D10}", 1);

                supplyServiceNumberRepository.Add(message.SupplyOrder.CustomAgencyService.ServiceNumber);

                message.SupplyOrder.CustomAgencyService.CustomAgencyOrganizationId = message.SupplyOrder.CustomAgencyService.CustomAgencyOrganization?.Id;
                message.SupplyOrder.CustomAgencyService.UserId = headPolishLogistic.Id;
                message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreementId = message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Id;
                message.SupplyOrder.CustomAgencyServiceId = supplyRepositoriesFactory.NewCustomAgencyServiceRepository(connection).Add(message.SupplyOrder.CustomAgencyService);

                if (message.SupplyOrder.CustomAgencyService.InvoiceDocuments.Any(d => d.IsNew()))
                    invoiceDocumentRepository.Add(message.SupplyOrder.CustomAgencyService.InvoiceDocuments
                        .Where(d => d.IsNew())
                        .Select(d => {
                            d.CustomAgencyServiceId = message.SupplyOrder.CustomAgencyServiceId;

                            return d;
                        })
                    );

                if (message.SupplyOrder.CustomAgencyService.ServiceDetailItems.Any())
                    InsertOrUpdateServiceDetailItems(
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        message.SupplyOrder.CustomAgencyService.ServiceDetailItems
                            .Select(i => {
                                i.CustomAgencyServiceId = message.SupplyOrder.CustomAgencyServiceId;

                                return i;
                            })
                    );
            } else {
                CustomAgencyService existCustomAgencyService = supplyRepositoriesFactory
                    .NewCustomAgencyServiceRepository(connection)
                    .GetByIdWithoutIncludes(message.SupplyOrder.CustomAgencyService.Id);

                UpdateSupplyOrganizationAndAgreement(
                    supplyOrganizationAgreementRepository,
                    message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreementId,
                    existCustomAgencyService.GrossPrice,
                    existCustomAgencyService.AccountingGrossPrice,
                    message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Id,
                    message.SupplyOrder.CustomAgencyService.GrossPrice,
                    message.SupplyOrder.CustomAgencyService.AccountingGrossPrice);

                message.SupplyOrder.CustomAgencyService.CustomAgencyOrganizationId =
                    message.SupplyOrder.CustomAgencyService.CustomAgencyOrganization.Id;
                message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreementId =
                    message.SupplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Id;

                if (message.SupplyOrder.CustomAgencyService.InvoiceDocuments.Any()) {
                    invoiceDocumentRepository.RemoveAllByCustomAgencyServiceIdExceptProvided(
                        message.SupplyOrder.CustomAgencyService.Id,
                        message.SupplyOrder.CustomAgencyService.InvoiceDocuments.Where(d => !d.IsNew() && !d.Deleted).Select(d => d.Id)
                    );

                    if (message.SupplyOrder.CustomAgencyService.InvoiceDocuments.Any(d => d.IsNew()))
                        invoiceDocumentRepository.Add(message.SupplyOrder.CustomAgencyService.InvoiceDocuments
                            .Where(d => d.IsNew())
                            .Select(d => {
                                d.CustomAgencyServiceId = message.SupplyOrder.CustomAgencyService.Id;

                                return d;
                            })
                        );
                } else {
                    invoiceDocumentRepository.RemoveAllByCustomAgencyServiceId(message.SupplyOrder.CustomAgencyService.Id);
                }

                if (message.SupplyOrder.CustomAgencyService.ServiceDetailItems.Any()) {
                    serviceDetailItemRepository.RemoveAllByCustomAgencyServiceIdExceptProvided(
                        message.SupplyOrder.CustomAgencyService.Id,
                        message.SupplyOrder.CustomAgencyService.ServiceDetailItems.Where(i => !i.IsNew()).Select(i => i.Id)
                    );

                    InsertOrUpdateServiceDetailItems(
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        message.SupplyOrder.CustomAgencyService.ServiceDetailItems
                            .Select(i => {
                                i.CustomAgencyServiceId = message.SupplyOrder.CustomAgencyServiceId;

                                return i;
                            })
                    );
                } else {
                    serviceDetailItemRepository.RemoveAllByCustomAgencyServiceId(message.SupplyOrder.CustomAgencyService.Id);
                }

                if (message.SupplyOrder.CustomAgencyService.SupplyInformationTask != null) {
                    if (message.SupplyOrder.CustomAgencyService.SupplyInformationTask.IsNew()) {
                        message.SupplyOrder.CustomAgencyService.SupplyInformationTask.UserId =
                            message.SupplyOrder.CustomAgencyService.SupplyInformationTask.User.Id;
                        message.SupplyOrder.CustomAgencyService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                        message.SupplyOrder.CustomAgencyService.AccountingSupplyCostsWithinCountry =
                            message.SupplyOrder.CustomAgencyService.SupplyInformationTask.GrossPrice;

                        message.SupplyOrder.CustomAgencyService.SupplyInformationTaskId =
                            supplyInformationTaskRepository.Add(message.SupplyOrder.CustomAgencyService.SupplyInformationTask);
                    } else {
                        if (message.SupplyOrder.CustomAgencyService.SupplyInformationTask.Deleted) {
                            message.SupplyOrder.CustomAgencyService.SupplyInformationTask.DeletedById = updatedBy.Id;

                            supplyInformationTaskRepository.Remove(message.SupplyOrder.CustomAgencyService.SupplyInformationTask);

                            message.SupplyOrder.CustomAgencyService.SupplyInformationTaskId = null;
                        } else {
                            message.SupplyOrder.CustomAgencyService.SupplyInformationTask.UpdatedById = updatedBy.Id;
                            message.SupplyOrder.CustomAgencyService.SupplyInformationTask.UserId =
                                message.SupplyOrder.CustomAgencyService.SupplyInformationTask.User.Id;

                            message.SupplyOrder.CustomAgencyService.AccountingSupplyCostsWithinCountry =
                                message.SupplyOrder.CustomAgencyService.SupplyInformationTask.GrossPrice;

                            supplyInformationTaskRepository.Update(message.SupplyOrder.CustomAgencyService.SupplyInformationTask);
                        }
                    }
                }

                if (message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument != null) {
                    if (message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Number =
                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument);
                    } else if (message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.CustomAgencyService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.SupplyOrder.CustomAgencyService.SupplyPaymentTask != null) {
                    if (message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.IsNew()) {
                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.UserId = message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.User.Id;
                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomAgencyService;

                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate =
                            !message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate.Value);

                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.NetPrice = message.SupplyOrder.CustomAgencyService.NetPrice;
                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.CustomAgencyService.GrossPrice;

                        message.SupplyOrder.CustomAgencyService.SupplyPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.CustomAgencyService.GrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.CustomAgencyService.Vat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.CustomAgencyService?.CustomAgencyOrganization?.Name,
                            PaymentForm = "Custom agency"
                        });
                    } else {
                        if (message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.IsAvailableForPayment) {
                            if (message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.Id, updatedBy.Id);

                                message.SupplyOrder.CustomAgencyService.SupplyPaymentTaskId = null;
                            } else {
                                message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate =
                                    !message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.PayToDate.Value);

                                message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.NetPrice = message.SupplyOrder.CustomAgencyService.NetPrice;
                                message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.CustomAgencyService.GrossPrice;
                                message.SupplyOrder.CustomAgencyService.SupplyPaymentTask.UpdatedById = updatedBy.Id;

                                supplyPaymentTaskRepository.Update(message.SupplyOrder.CustomAgencyService.SupplyPaymentTask);
                            }
                        }
                    }
                }

                if (message.SupplyOrder.CustomAgencyService.AccountingPaymentTask != null) {
                    if (message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.IsNew()) {
                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.UserId = message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.User.Id;
                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomAgencyService;
                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.IsAccounting = true;

                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate =
                            !message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate.Value);

                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.NetPrice = message.SupplyOrder.CustomAgencyService.AccountingNetPrice;
                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.CustomAgencyService.AccountingGrossPrice;

                        message.SupplyOrder.CustomAgencyService.AccountingPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.CustomAgencyService.AccountingGrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.CustomAgencyService.AccountingVat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.CustomAgencyService?.CustomAgencyOrganization?.Name,
                            PaymentForm = "Custom agency"
                        });
                    } else {
                        if (message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.IsAvailableForPayment) {
                            if (message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.Id, updatedBy.Id);

                                message.SupplyOrder.CustomAgencyService.AccountingPaymentTaskId = null;
                            } else {
                                message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate =
                                    !message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.PayToDate.Value);

                                message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.NetPrice = message.SupplyOrder.CustomAgencyService.AccountingNetPrice;
                                message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.CustomAgencyService.AccountingGrossPrice;
                                message.SupplyOrder.CustomAgencyService.AccountingPaymentTask.UpdatedById = updatedBy.Id;

                                supplyPaymentTaskRepository.Update(message.SupplyOrder.CustomAgencyService.AccountingPaymentTask);
                            }
                        }
                    }
                }

                if (message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument != null) {
                    if (message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Number =
                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                        message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument);
                    } else if (message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.CustomAgencyService.SupplyServiceAccountDocument.Id);
                    }
                }

                supplyRepositoriesFactory.NewCustomAgencyServiceRepository(connection).Update(message.SupplyOrder.CustomAgencyService);
            }
        }

        if (message.SupplyOrder.PortCustomAgencyService != null) {
            message.SupplyOrder.PortCustomAgencyService.NetPrice = Math.Round(
                message.SupplyOrder.PortCustomAgencyService.GrossPrice * 100 / Convert.ToDecimal(100 + message.SupplyOrder.PortCustomAgencyService.VatPercent),
                2
            );
            message.SupplyOrder.PortCustomAgencyService.Vat = Math.Round(
                message.SupplyOrder.PortCustomAgencyService.GrossPrice - message.SupplyOrder.PortCustomAgencyService.NetPrice,
                2
            );

            message.SupplyOrder.PortCustomAgencyService.AccountingNetPrice = Math.Round(
                message.SupplyOrder.PortCustomAgencyService.AccountingGrossPrice * 100 /
                Convert.ToDecimal(100 + message.SupplyOrder.PortCustomAgencyService.AccountingVatPercent),
                2
            );
            message.SupplyOrder.PortCustomAgencyService.AccountingVat = Math.Round(
                message.SupplyOrder.PortCustomAgencyService.AccountingGrossPrice - message.SupplyOrder.PortCustomAgencyService.AccountingNetPrice,
                2
            );

            if (message.SupplyOrder.PortCustomAgencyService.FromDate.HasValue)
                message.SupplyOrder.PortCustomAgencyService.FromDate = TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortCustomAgencyService.FromDate.Value);

            if (message.SupplyOrder.PortCustomAgencyService.IsNew()) {
                if (message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask != null) {
                    message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.UserId = message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.User.Id;
                    message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.PortCustomAgencyService;

                    message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate =
                        !message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate.Value);

                    message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.NetPrice = message.SupplyOrder.PortCustomAgencyService.NetPrice;
                    message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.PortCustomAgencyService.GrossPrice;

                    message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTaskId =
                        supplyPaymentTaskRepository.Add(message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask);

                    messagesToSend.Add(new PaymentTaskMessage {
                        Amount = message.SupplyOrder.PortCustomAgencyService.GrossPrice,
                        Discount = Convert.ToDouble(message.SupplyOrder.PortCustomAgencyService.Vat),
                        CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                        PayToDate = message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate,
                        OrganisationName = message.SupplyOrder.PortCustomAgencyService?.PortCustomAgencyOrganization?.Name,
                        PaymentForm = "Port custom agency"
                    });
                }

                if (message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask != null) {
                    message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.UserId = message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.User.Id;
                    message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.PortCustomAgencyService;
                    message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.IsAccounting = true;

                    message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate =
                        !message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate.Value);

                    message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.NetPrice = message.SupplyOrder.PortCustomAgencyService.AccountingNetPrice;
                    message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.PortCustomAgencyService.AccountingGrossPrice;

                    message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTaskId =
                        supplyPaymentTaskRepository.Add(message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask);

                    messagesToSend.Add(new PaymentTaskMessage {
                        Amount = message.SupplyOrder.PortCustomAgencyService.AccountingGrossPrice,
                        Discount = Convert.ToDouble(message.SupplyOrder.PortCustomAgencyService.AccountingVat),
                        CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                        PayToDate = message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate,
                        OrganisationName = message.SupplyOrder.PortCustomAgencyService?.PortCustomAgencyOrganization?.Name,
                        PaymentForm = "Port custom agency"
                    });
                }

                if (message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask != null) {
                    message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.UserId = updatedBy.Id;
                    message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                    message.SupplyOrder.PortCustomAgencyService.AccountingSupplyCostsWithinCountry =
                        message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.GrossPrice;

                    message.SupplyOrder.PortCustomAgencyService.SupplyInformationTaskId =
                        supplyInformationTaskRepository.Add(message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask);
                }

                if (message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument != null) {
                    if (message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.Number =
                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument);
                    } else if (message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument != null) {
                    if (message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.Number =
                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                        message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument);
                    } else if (message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.Id);
                    }
                }

                if (message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement != null &&
                    !message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.IsNew()) {
                    message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement =
                        supplyOrganizationAgreementRepository.GetById(message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.Id);

                    message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.CurrentAmount =
                        Math.Round(
                            message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.CurrentAmount - message.SupplyOrder.PortCustomAgencyService.GrossPrice,
                            2);

                    message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                        Math.Round(
                            message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.AccountingCurrentAmount -
                            message.SupplyOrder.PortCustomAgencyService.AccountingGrossPrice,
                            2);

                    supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement);
                }

                informationMessage.CreatedBy = $"{updatedBy.LastName} {updatedBy.FirstName}";
                informationMessage.Title = $"�������� ���������� � {message.SupplyOrder.SupplyOrderNumber.Number}";
                informationMessage.Message = "������� ����� ������� ";

                SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                    message.SupplyOrder.PortCustomAgencyService.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                else
                    message.SupplyOrder.PortCustomAgencyService.ServiceNumber = string.Format("P{0:D10}", 1);

                supplyServiceNumberRepository.Add(message.SupplyOrder.PortCustomAgencyService.ServiceNumber);

                message.SupplyOrder.PortCustomAgencyService.PortCustomAgencyOrganizationId = message.SupplyOrder.PortCustomAgencyService.PortCustomAgencyOrganization.Id;
                message.SupplyOrder.PortCustomAgencyService.UserId = headPolishLogistic.Id;
                message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreementId = message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.Id;
                message.SupplyOrder.PortCustomAgencyServiceId =
                    supplyRepositoriesFactory.NewPortCustomAgencyServiceRepository(connection).Add(message.SupplyOrder.PortCustomAgencyService);

                if (message.SupplyOrder.PortCustomAgencyService.InvoiceDocuments.Any(d => d.IsNew()))
                    invoiceDocumentRepository.Add(message.SupplyOrder.PortCustomAgencyService.InvoiceDocuments
                        .Where(d => d.IsNew())
                        .Select(d => {
                            d.PortCustomAgencyServiceId = message.SupplyOrder.PortCustomAgencyServiceId;

                            return d;
                        })
                    );

                if (message.SupplyOrder.PortCustomAgencyService.ServiceDetailItems.Any())
                    InsertOrUpdateServiceDetailItems(
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        message.SupplyOrder.PortCustomAgencyService.ServiceDetailItems
                            .Select(i => {
                                i.PortCustomAgencyServiceId = message.SupplyOrder.PortCustomAgencyServiceId;

                                return i;
                            })
                    );
            } else {
                PortCustomAgencyService existPortCustomAgencyService = supplyRepositoriesFactory
                    .NewPortCustomAgencyServiceRepository(connection)
                    .GetByIdWithoutIncludes(message.SupplyOrder.PortCustomAgencyService.Id);

                UpdateSupplyOrganizationAndAgreement(
                    supplyOrganizationAgreementRepository,
                    message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreementId,
                    existPortCustomAgencyService.GrossPrice,
                    existPortCustomAgencyService.AccountingGrossPrice,
                    message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.Id,
                    message.SupplyOrder.PortCustomAgencyService.GrossPrice,
                    message.SupplyOrder.PortCustomAgencyService.AccountingGrossPrice);

                message.SupplyOrder.PortCustomAgencyService.PortCustomAgencyOrganizationId =
                    message.SupplyOrder.PortCustomAgencyService.PortCustomAgencyOrganization.Id;
                message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreementId =
                    message.SupplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.Id;

                if (message.SupplyOrder.PortCustomAgencyService.InvoiceDocuments.Any()) {
                    invoiceDocumentRepository.RemoveAllByPortCustomAgencyServiceIdExceptProvided(
                        message.SupplyOrder.PortCustomAgencyService.Id,
                        message.SupplyOrder.PortCustomAgencyService.InvoiceDocuments.Where(d => !d.IsNew() && !d.Deleted).Select(d => d.Id)
                    );

                    if (message.SupplyOrder.PortCustomAgencyService.InvoiceDocuments.Any(d => d.IsNew()))
                        invoiceDocumentRepository.Add(message.SupplyOrder.PortCustomAgencyService.InvoiceDocuments
                            .Where(d => d.IsNew())
                            .Select(d => {
                                d.PortCustomAgencyServiceId = message.SupplyOrder.PortCustomAgencyService.Id;

                                return d;
                            })
                        );
                } else {
                    invoiceDocumentRepository.RemoveAllByPortCustomAgencyServiceId(message.SupplyOrder.PortCustomAgencyService.Id);
                }

                if (message.SupplyOrder.PortCustomAgencyService.ServiceDetailItems.Any()) {
                    serviceDetailItemRepository.RemoveAllByPortCustomAgencyServiceIdExceptProvided(
                        message.SupplyOrder.PortCustomAgencyService.Id,
                        message.SupplyOrder.PortCustomAgencyService.ServiceDetailItems.Where(i => !i.IsNew()).Select(i => i.Id)
                    );

                    InsertOrUpdateServiceDetailItems(
                        serviceDetailItemRepository,
                        serviceDetailItemKeyRepository,
                        message.SupplyOrder.PortCustomAgencyService.ServiceDetailItems
                            .Select(i => {
                                i.PortCustomAgencyServiceId = message.SupplyOrder.PortCustomAgencyServiceId;

                                return i;
                            })
                    );
                } else {
                    serviceDetailItemRepository.RemoveAllByPortCustomAgencyServiceId(message.SupplyOrder.PortCustomAgencyService.Id);
                }


                if (message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask != null) {
                    if (message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.IsNew()) {
                        message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.UserId =
                            message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.User.Id;
                        message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                        message.SupplyOrder.PortCustomAgencyService.AccountingSupplyCostsWithinCountry =
                            message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.GrossPrice;

                        message.SupplyOrder.PortCustomAgencyService.SupplyInformationTaskId =
                            supplyInformationTaskRepository.Add(message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask);
                    } else {
                        if (message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.Deleted) {
                            message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.DeletedById = updatedBy.Id;

                            supplyInformationTaskRepository.Remove(message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask);

                            message.SupplyOrder.PortCustomAgencyService.SupplyInformationTaskId = null;
                        } else {
                            message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.UpdatedById = updatedBy.Id;
                            message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.UserId =
                                message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.User.Id;

                            message.SupplyOrder.PortCustomAgencyService.AccountingSupplyCostsWithinCountry =
                                message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask.GrossPrice;

                            supplyInformationTaskRepository.Update(message.SupplyOrder.PortCustomAgencyService.SupplyInformationTask);
                        }
                    }
                }

                if (message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument != null) {
                    if (message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.Number =
                                string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument);
                    } else if (message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.SupplyOrder.PortCustomAgencyService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask != null) {
                    if (message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.IsNew()) {
                        message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.UserId = message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.User.Id;
                        message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.PortCustomAgencyService;

                        message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate =
                            !message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate.Value);

                        message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.NetPrice = message.SupplyOrder.PortCustomAgencyService.NetPrice;
                        message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.PortCustomAgencyService.GrossPrice;

                        message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.PortCustomAgencyService.GrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.PortCustomAgencyService.Vat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.PortCustomAgencyService?.PortCustomAgencyOrganization?.Name,
                            PaymentForm = "Port custom agency"
                        });
                    } else {
                        if (message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.IsAvailableForPayment) {
                            if (message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.Id, updatedBy.Id);

                                message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTaskId = null;
                            } else {
                                message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate =
                                    !message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.PayToDate.Value);

                                message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.NetPrice = message.SupplyOrder.PortCustomAgencyService.NetPrice;
                                message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.GrossPrice = message.SupplyOrder.PortCustomAgencyService.GrossPrice;
                                message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask.UpdatedById = updatedBy.Id;

                                supplyPaymentTaskRepository.Update(message.SupplyOrder.PortCustomAgencyService.SupplyPaymentTask);
                            }
                        }
                    }
                }

                if (message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask != null) {
                    if (message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.IsNew()) {
                        message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.UserId = message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.User.Id;
                        message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.PortCustomAgencyService;
                        message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.IsAccounting = true;

                        message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate =
                            !message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate.Value);

                        message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.NetPrice = message.SupplyOrder.PortCustomAgencyService.AccountingNetPrice;
                        message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.PortCustomAgencyService.AccountingGrossPrice;

                        message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask);

                        messagesToSend.Add(new PaymentTaskMessage {
                            Amount = message.SupplyOrder.PortCustomAgencyService.AccountingGrossPrice,
                            Discount = Convert.ToDouble(message.SupplyOrder.PortCustomAgencyService.AccountingVat),
                            CreatedBy = $"{headPolishLogistic.LastName} {headPolishLogistic.FirstName}",
                            PayToDate = message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate,
                            OrganisationName = message.SupplyOrder.PortCustomAgencyService?.PortCustomAgencyOrganization?.Name,
                            PaymentForm = "Port custom agency"
                        });
                    } else {
                        if (message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.IsAvailableForPayment) {
                            if (message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.Id, updatedBy.Id);

                                message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTaskId = null;
                            } else {
                                message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate =
                                    !message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.PayToDate.Value);

                                message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.NetPrice = message.SupplyOrder.PortCustomAgencyService.AccountingNetPrice;
                                message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.GrossPrice = message.SupplyOrder.PortCustomAgencyService.AccountingGrossPrice;
                                message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask.UpdatedById = updatedBy.Id;

                                supplyPaymentTaskRepository.Update(message.SupplyOrder.PortCustomAgencyService.AccountingPaymentTask);
                            }
                        }
                    }
                }

                if (message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument != null) {
                    if (message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.Number =
                                string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                        message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument);
                    } else if (message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.SupplyOrder.PortCustomAgencyService.SupplyServiceAccountDocument.Id);
                    }
                }

                supplyRepositoriesFactory.NewPortCustomAgencyServiceRepository(connection).Update(message.SupplyOrder.PortCustomAgencyService);
            }
        }
    }

    private static void InsertOrUpdateServiceDetailItems(
        IServiceDetailItemRepository serviceDetailItemRepository,
        IServiceDetailItemKeyRepository serviceDetailItemKeyRepository,
        IEnumerable<ServiceDetailItem> serviceDetailItems) {
        foreach (ServiceDetailItem item in serviceDetailItems) {
            if (item.ServiceDetailItemKey != null) {
                if (item.ServiceDetailItemKey.IsNew()) {
                    ServiceDetailItemKey existingKey =
                        serviceDetailItemKeyRepository
                            .GetByFieldsIfExists(
                                item.ServiceDetailItemKey.Name,
                                item.ServiceDetailItemKey.Symbol,
                                item.ServiceDetailItemKey.Type
                            );

                    if (existingKey != null)
                        item.ServiceDetailItemKeyId = existingKey.Id;
                    else
                        item.ServiceDetailItemKeyId = serviceDetailItemKeyRepository.Add(item.ServiceDetailItemKey);
                } else {
                    item.ServiceDetailItemKeyId = item.ServiceDetailItemKey.Id;
                }
            }

            if (item.UnitPrice > 0 && item.Qty > 0) {
                item.NetPrice = decimal.Round(item.UnitPrice * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero);

                item.Vat = decimal.Round(item.NetPrice * Convert.ToDecimal(item.VatPercent) / 100m, 2, MidpointRounding.AwayFromZero);

                item.GrossPrice = decimal.Round(item.NetPrice + item.Vat, 2, MidpointRounding.AwayFromZero);
            } else if (item.GrossPrice > 0 && item.Qty > 0) {
                if (item.VatPercent > 0)
                    item.Vat = decimal.Round(item.GrossPrice * 100m / (Convert.ToDecimal(item.VatPercent) + 100m), 2, MidpointRounding.AwayFromZero);
                else
                    item.Vat = 0m;

                item.NetPrice = decimal.Round(item.GrossPrice - item.Vat, 2, MidpointRounding.AwayFromZero);

                item.UnitPrice = decimal.Round(item.NetPrice / Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero);
            }
        }

        if (serviceDetailItems.Any(i => i.IsNew())) serviceDetailItemRepository.Add(serviceDetailItems.Where(i => i.IsNew()));

        if (serviceDetailItems.Any(i => !i.IsNew())) serviceDetailItemRepository.Update(serviceDetailItems.Where(i => !i.IsNew()));
    }
}