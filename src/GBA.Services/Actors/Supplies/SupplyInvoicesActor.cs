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
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.Supplies.PackingLists;
using GBA.Domain.Messages.Consignments;
using GBA.Domain.Messages.Products.ProductSpecifications;
using GBA.Domain.Messages.Supplies;
using GBA.Domain.Messages.Supplies.DeliveryProductProtocols;
using GBA.Domain.Messages.Supplies.Documents;
using GBA.Domain.Messages.Supplies.Invoices;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.DeliveryProductProtocols.Contracts;
using GBA.Domain.Repositories.Supplies.HelperServices.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Supplies;

public sealed class SupplyInvoicesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public SupplyInvoicesActor(
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

        Receive<AddOrUpdateSupplyInvoiceMessage>(ProcessAddOrUpdateSupplyInvoiceMessage);

        Receive<AddOrUpdateSupplyInvoiceFromFileMessage>(ProcessAddOrUpdateSupplyInvoiceFromFileMessage);

        Receive<UpdateInvoiceDocumentsMessage>(ProcessUpdateInvoiceDocumentsMessage);

        Receive<DeleteInvoiceDocumentMessage>(ProcessDeleteInvoiceDocumentMessage);

        Receive<UpdateStatusOnShippedMessage>(ProcessUpdateStatusOnShippedMessage);

        Receive<DeleteSupplyInvoiceByNetIdMessage>(ProcessDeleteSupplyInvoiceByNetIdMessage);

        Receive<AddOrUpdateSupplyInvoiceOrderItemsMessage>(ProcessAddOrUpdateSupplyInvoiceOrderItemsMessage);

        Receive<UpdateVatPercentToAllSupplyInvoicePackingListsMessage>(ProcessUpdateVatPercentToAllSupplyInvoicePackingListsMessage);

        Receive<UploadProductSpecificationForInvoiceMessage>(ProcessUploadProductSpecificationForInvoice);

        Receive<UpdateSupplyInvoiceItemGrossPriceMessage>(ProcessUpdateSupplyInvoiceGrossPrice);

        Receive<AddDocumentsToSupplyInvoiceMessage>(ProcessAddDocumentsToSupplyInvoice);

        Receive<AddDocumentToInvoiceForOrderMessage>(ProcessAddDocumentToInvoiceForOrder);
    }

    private void ProcessAddOrUpdateSupplyInvoiceMessage(AddOrUpdateSupplyInvoiceMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyInvoiceRepository supplyInvoiceRepository = _supplyRepositoriesFactory.NewSupplyInvoiceRepository(connection);
        IInvoiceDocumentRepository invoiceDocumentRepository = _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection);

        SupplyOrder supplyOrder = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetByNetIdIfExist(message.SupplyOrderNetId);

        if (supplyOrder != null) {
            List<long> newPaymentTaskIds = new();
            long supplyInvoiceId;

            if (message.SupplyInvoice.IsNew()) {
                message.SupplyInvoice.SupplyOrderId = supplyOrder.Id;

                supplyInvoiceId = supplyInvoiceRepository.Add(message.SupplyInvoice);
            } else {
                supplyInvoiceId = message.SupplyInvoice.Id;
                supplyInvoiceRepository.Update(message.SupplyInvoice);
            }

            if (message.SupplyInvoice.InvoiceDocuments.Any(d => d.IsNew())) {
                List<InvoiceDocument> invoiceDocumentsToUpdate = new();
                List<InvoiceDocument> invoiceDocumentsToAdd = new();

                foreach (InvoiceDocument invoiceDocument in message.SupplyInvoice.InvoiceDocuments)
                    if (invoiceDocument.IsNew()) {
                        invoiceDocument.SupplyInvoiceId = supplyInvoiceId;
                        invoiceDocumentsToAdd.Add(invoiceDocument);
                    } else {
                        invoiceDocumentsToUpdate.Add(invoiceDocument);
                    }

                if (invoiceDocumentsToUpdate.Any()) invoiceDocumentRepository.Update(invoiceDocumentsToUpdate);

                if (invoiceDocumentsToAdd.Any()) invoiceDocumentRepository.Add(invoiceDocumentsToAdd);
            }

            if (message.SupplyInvoice.PaymentDeliveryProtocols.Any())
                AddOrUpdatePaymentDeliveryProtocols(
                    supplyInvoiceId,
                    message.SupplyInvoice.PaymentDeliveryProtocols,
                    newPaymentTaskIds,
                    _supplyRepositoriesFactory.NewSupplyOrderPaymentDeliveryProtocolRepository(connection),
                    _supplyRepositoriesFactory.NewSupplyOrderPaymentDeliveryProtocolKeyRepository(connection),
                    _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection)
                );

            Sender.Tell(supplyInvoiceRepository.GetById(supplyInvoiceId));
        } else {
            Sender.Tell(null);
        }
    }

    private void ProcessAddOrUpdateSupplyInvoiceFromFileMessage(AddOrUpdateSupplyInvoiceFromFileMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (string.IsNullOrEmpty(message.SupplyInvoice.Number)) throw new Exception(SupplyInvoiceResourceNames.EMPTY_INVOICE_NUMBER);

            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
            ISupplyInvoiceRepository supplyInvoiceRepository = _supplyRepositoriesFactory.NewSupplyInvoiceRepository(connection);
            IInvoiceDocumentRepository invoiceDocumentRepository = _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection);
            ISupplyOrderItemRepository supplyOrderItemRepository = _supplyRepositoriesFactory.NewSupplyOrderItemRepository(connection);
            IActReconciliationRepository actReconciliationRepository = _supplyUkraineRepositoriesFactory.NewActReconciliationRepository(connection);
            IActReconciliationItemRepository actReconciliationItemRepository = _supplyUkraineRepositoriesFactory.NewActReconciliationItemRepository(connection);
            ISupplyInvoiceOrderItemRepository supplyInvoiceOrderItemRepository = _supplyRepositoriesFactory.NewSupplyInvoiceOrderItemRepository(connection);

            SupplyOrder supplyOrder = supplyOrderRepository.GetByNetIdForDocumentUpload(message.SupplyOrderNetId);

            if (supplyOrder == null)
                throw new Exception(SupplyInvoiceResourceNames.SUPPLY_ORDER_NOT_EXISTS);
            if (supplyOrder.SupplyProForm == null) throw new Exception(SupplyInvoiceResourceNames.CREATE_PRO_FORM_FIRST);

            List<ParsedProduct> parsedProducts =
                _xlsFactoryManager
                    .NewParseConfigurationXlsManager()
                    .GetProductsFromSupplyDocumentsByConfiguration(message.PathToFile, message.DocumentParseConfiguration);

            foreach (ParsedProduct parsedProduct in parsedProducts) {
                Product product = getSingleProductRepository.GetProductByVendorCode(parsedProduct.VendorCode);

                if (product == null) throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.NoProductByVendorCode, 0, 0, parsedProduct.VendorCode);

                if (supplyOrder.SupplyOrderItems.Any(i => i.ProductId.Equals(product.Id))) {
                    SupplyOrderItem fromList =
                        supplyOrderItemRepository.GetByOrderAndProductIdAndQtyWithInvoiceItemsIfExists(supplyOrder.Id, product.Id, parsedProduct.Qty,
                            Math.Round(parsedProduct.UnitPrice, 4, MidpointRounding.AwayFromZero));

                    if (fromList == null) {
                        //supplyOrder
                        //    .SupplyOrderItems
                        //    .Add(new SupplyOrderItem {
                        //        ProductId = product.Id,
                        //        Qty = parsedProduct.Qty,
                        //        NetWeight = parsedProduct.NetWeight,
                        //        UnitPrice = parsedProduct.UnitPrice,
                        //        GrossWeight = parsedProduct.GrossWeight,
                        //        TotalAmount = decimal.Round(parsedProduct.UnitPrice * Convert.ToDecimal(parsedProduct.Qty), 2, MidpointRounding.AwayFromZero)
                        //    });
                    }
                    //if (fromList.Qty < parsedProduct.Qty + fromList.SupplyInvoiceOrderItems.Sum(i => i.Qty))
                    //    fromList.Qty = parsedProduct.Qty + fromList.SupplyInvoiceOrderItems.Sum(i => i.Qty);
                    //fromList.IsUpdated = true;
                    //fromList.NetWeight = parsedProduct.NetWeight;
                    //fromList.UnitPrice = parsedProduct.UnitPrice;
                    //fromList.GrossWeight = parsedProduct.GrossWeight;
                    //fromList.TotalAmount = decimal.Round(parsedProduct.UnitPrice * Convert.ToDecimal(parsedProduct.Qty), 2, MidpointRounding.AwayFromZero);
                    //supplyOrderItemRepository.Update(fromList);
                }
                //supplyOrder
                //    .SupplyOrderItems
                //    .Add(new SupplyOrderItem {
                //        ProductId = product.Id,
                //        Qty = parsedProduct.Qty,
                //        NetWeight = parsedProduct.NetWeight,
                //        UnitPrice = parsedProduct.UnitPrice,
                //        GrossWeight = parsedProduct.GrossWeight,
                //        TotalAmount = decimal.Round(parsedProduct.UnitPrice * Convert.ToDecimal(parsedProduct.Qty), 2, MidpointRounding.AwayFromZero)
                //    });
            }

            supplyOrder.NetPrice = decimal.Round(supplyOrder.SupplyOrderItems.Sum(i => i.TotalAmount), 2, MidpointRounding.AwayFromZero);
            supplyOrder.Qty = Math.Round(supplyOrder.SupplyOrderItems.Sum(i => i.Qty), 2, MidpointRounding.AwayFromZero);

            supplyOrderRepository.Update(supplyOrder);

            supplyOrderItemRepository
                .Add(
                    supplyOrder
                        .SupplyOrderItems
                        .Where(i => i.IsNew())
                        .Select(i => {
                            i.SupplyOrderId = supplyOrder.Id;

                            return i;
                        })
                );

            supplyOrder = supplyOrderRepository.GetByNetIdForDocumentUpload(message.SupplyOrderNetId);

            if (supplyOrder.SupplyInvoices.Any(i => i.Number.Equals(message.SupplyInvoice.Number))) {
                User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                SupplyInvoice invoiceFromList = supplyOrder.SupplyInvoices.First(i => i.Number.Equals(message.SupplyInvoice.Number));

                foreach (SupplyInvoiceOrderItem item in invoiceFromList.SupplyInvoiceOrderItems) item.SupplyOrderItem = null;

                message.SupplyInvoice.Id = invoiceFromList.Id;

                invoiceDocumentRepository
                    .Add(
                        message
                            .SupplyInvoice
                            .InvoiceDocuments
                            .Where(i => i.IsNew())
                            .Select(document => {
                                document.SupplyInvoiceId = message.SupplyInvoice.Id;

                                return document;
                            })
                    );

                foreach (ParsedProduct parsedProduct in parsedProducts) {
                    Product product = getSingleProductRepository.GetProductByVendorCode(parsedProduct.VendorCode);

                    SupplyOrderItem? fromList = supplyOrder.SupplyOrderItems.FirstOrDefault(i =>
                        i.ProductId.Equals(product.Id) && i.Qty.Equals(parsedProduct.Qty) && i.UnitPrice.Equals(parsedProduct.UnitPrice));

                    //if (fromList == null) continue;

                    if (fromList != null && invoiceFromList.SupplyInvoiceOrderItems.Any(i => i.SupplyOrderItemId.Equals(fromList.Id))) {
                        SupplyInvoiceOrderItem invoiceItemFromList = invoiceFromList.SupplyInvoiceOrderItems.First(i => i.SupplyOrderItemId.Equals(fromList.Id));

                        invoiceItemFromList.RowNumber = parsedProduct.RowNumber;
                        invoiceItemFromList.Qty = parsedProduct.Qty;
                        invoiceItemFromList.SupplyOrderItem = fromList;
                        invoiceItemFromList.UnitPrice = parsedProduct.UnitPrice;
                    } else {
                        invoiceFromList
                            .SupplyInvoiceOrderItems
                            .Add(
                                new SupplyInvoiceOrderItem {
                                    Qty = parsedProduct.Qty,
                                    SupplyOrderItem = fromList,
                                    SupplyOrderItemId = fromList?.Id,
                                    UnitPrice = parsedProduct.UnitPrice,
                                    SupplyInvoiceId = invoiceFromList.Id,
                                    RowNumber = parsedProduct.RowNumber,
                                    ProductId = product.Id
                                }
                            );
                    }
                }

                supplyInvoiceOrderItemRepository
                    .RemoveAllByInvoiceIdExceptProvided(
                        invoiceFromList.Id,
                        invoiceFromList
                            .SupplyInvoiceOrderItems
                            .Where(i => !i.IsNew() && i.SupplyOrderItem != null)
                            .Select(i => i.Id)
                    );

                ActReconciliation actReconciliation = actReconciliationRepository.GetBySupplyInvoiceId(message.SupplyInvoice.Id);

                if (actReconciliation == null) {
                    actReconciliation = new ActReconciliation {
                        FromDate = message.SupplyInvoice.DateFrom ?? message.SupplyInvoice.Created,
                        ResponsibleId = user.Id,
                        SupplyInvoiceId = message.SupplyInvoice.Id,
                        Comment = message.SupplyInvoice.Comment,
                        Number = invoiceFromList.ServiceNumber
                    };

                    actReconciliation.Id = actReconciliationRepository.Add(actReconciliation);
                }

                foreach (SupplyInvoiceOrderItem item in invoiceFromList.SupplyInvoiceOrderItems.Where(i => i.IsNew()))
                    actReconciliationItemRepository
                        .Add(new ActReconciliationItem {
                            ActReconciliationId = actReconciliation.Id,
                            SupplyInvoiceOrderItemId =
                                supplyInvoiceOrderItemRepository
                                    .Add(item),
                            ProductId = item.ProductId,
                            HasDifference = true,
                            NegativeDifference = true,
                            QtyDifference = item.Qty,
                            ActualQty = 0d,
                            OrderedQty = item.Qty,
                            UnitPrice = item.UnitPrice,
                            NetWeight = item.SupplyOrderItem?.NetWeight ?? 0
                        });

                foreach (SupplyInvoiceOrderItem item in invoiceFromList.SupplyInvoiceOrderItems.Where(i => !i.IsNew() && i.SupplyOrderItem != null)) {
                    ActReconciliationItem reconciliationItem = actReconciliationItemRepository.GetBySupplyInvoiceOrderItemId(item.Id);

                    if (reconciliationItem != null) {
                        reconciliationItem.QtyDifference = item.Qty;
                        reconciliationItem.OrderedQty = item.Qty;

                        actReconciliationItemRepository.FullUpdate(reconciliationItem);
                    } else {
                        actReconciliationItemRepository
                            .Add(new ActReconciliationItem {
                                ActReconciliationId = actReconciliation.Id,
                                SupplyInvoiceOrderItemId = item.Id,
                                ProductId = item.ProductId,
                                HasDifference = true,
                                NegativeDifference = true,
                                QtyDifference = item.Qty,
                                ActualQty = 0d,
                                OrderedQty = item.Qty,
                                UnitPrice = item.UnitPrice,
                                NetWeight = item.SupplyOrderItem?.NetWeight ?? 0
                            });
                    }
                }

                supplyInvoiceOrderItemRepository
                    .Update(
                        invoiceFromList
                            .SupplyInvoiceOrderItems
                            .Where(i => !i.IsNew() && i.SupplyOrderItem != null)
                    );

                invoiceFromList.NetPrice =
                    decimal.Round(
                        invoiceFromList
                            .SupplyInvoiceOrderItems
                            .Where(i => i.SupplyOrderItem != null)
                            .Sum(i =>
                                decimal.Round(i.UnitPrice * Convert.ToDecimal(i.Qty), 2, MidpointRounding.AwayFromZero)
                            ),
                        2,
                        MidpointRounding.AwayFromZero
                    );

                supplyInvoiceRepository.RestoreSupplyInvoice(invoiceFromList.Id);

                supplyInvoiceRepository.Update(invoiceFromList);
            } else {
                IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
                ISupplyServiceNumberRepository supplyServiceNumberRepository = _supplyRepositoriesFactory.NewSupplyServiceNumberRepository(connection);
                ISupplyInformationDeliveryProtocolRepository supplyInformationDeliveryProtocolRepository =
                    _supplyRepositoriesFactory.NewSupplyInformationDeliveryProtocolRepository(connection);
                ISupplyInformationDeliveryProtocolKeyRepository supplyInformationDeliveryProtocolKeyRepository =
                    _supplyRepositoriesFactory.NewSupplyInformationDeliveryProtocolKeyRepository(connection);

                User user = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

                message.SupplyInvoice.SupplyOrderId = supplyOrder.Id;

                if (message.SupplyInvoice.DateFrom.HasValue)
                    message.SupplyInvoice.DateFrom = TimeZoneInfo.ConvertTimeToUtc(message.SupplyInvoice.DateFrom ?? message.SupplyInvoice.Created);

                if (message.SupplyInvoice.PaymentTo.HasValue)
                    message.SupplyInvoice.PaymentTo = TimeZoneInfo.ConvertTimeToUtc(message.SupplyInvoice.PaymentTo ?? message.SupplyInvoice.Created);

                SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                    message.SupplyInvoice.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                else
                    message.SupplyInvoice.ServiceNumber = string.Format("P{0:D10}", 1);

                supplyServiceNumberRepository.Add(message.SupplyInvoice.ServiceNumber);

                message.SupplyInvoice.NetPrice =
                    parsedProducts
                        .Sum(p => decimal.Round(p.UnitPrice * Convert.ToDecimal(p.Qty), 2, MidpointRounding.AwayFromZero));

                message.SupplyInvoice.Id = supplyInvoiceRepository.Add(message.SupplyInvoice);

                invoiceDocumentRepository
                    .Add(
                        message
                            .SupplyInvoice
                            .InvoiceDocuments
                            .Where(i => i.IsNew())
                            .Select(document => {
                                document.SupplyInvoiceId = message.SupplyInvoice.Id;

                                return document;
                            })
                    );

                ActReconciliation actReconciliation = new() {
                    FromDate = message.SupplyInvoice.DateFrom ?? message.SupplyInvoice.Created,
                    ResponsibleId = user.Id,
                    SupplyInvoiceId = message.SupplyInvoice.Id,
                    Comment = message.SupplyInvoice.Comment,
                    Number = message.SupplyInvoice.ServiceNumber
                };

                actReconciliation.Id = actReconciliationRepository.Add(actReconciliation);

                foreach (ParsedProduct parsedProduct in parsedProducts) {
                    Product product = getSingleProductRepository.GetProductByVendorCode(parsedProduct.VendorCode);

                    SupplyOrderItem? fromList = supplyOrder.SupplyOrderItems.FirstOrDefault(i =>
                        i.ProductId.Equals(product.Id) && i.Qty.Equals(parsedProduct.Qty) && i.UnitPrice.Equals(parsedProduct.UnitPrice));
                    //if (fromList == null) continue;

                    actReconciliationItemRepository
                        .Add(new ActReconciliationItem {
                            ActReconciliationId = actReconciliation.Id,
                            SupplyInvoiceOrderItemId =
                                supplyInvoiceOrderItemRepository
                                    .Add(
                                        new SupplyInvoiceOrderItem {
                                            Qty = parsedProduct.Qty,
                                            SupplyOrderItemId = fromList?.Id,
                                            SupplyInvoiceId = message.SupplyInvoice.Id,
                                            UnitPrice = parsedProduct.UnitPrice,
                                            RowNumber = parsedProduct.RowNumber,
                                            ProductIsImported = message.DocumentParseConfiguration.ProductIsImported,
                                            ProductId = product.Id
                                        }
                                    ),
                            ProductId = product.Id,
                            HasDifference = true,
                            NegativeDifference = true,
                            QtyDifference = parsedProduct.Qty,
                            ActualQty = 0d,
                            OrderedQty = parsedProduct.Qty,
                            UnitPrice = fromList?.UnitPrice ?? 0,
                            NetWeight = fromList?.NetWeight ?? 0
                        });
                }

                List<SupplyInformationDeliveryProtocolKey> defaultInformationKeys =
                    supplyInformationDeliveryProtocolKeyRepository
                        .GetAllDefaultByTransportationTypeAndDestination(supplyOrder.TransportationType, KeyAssignedTo.SupplyInvoice);

                if (defaultInformationKeys.Any())
                    supplyInformationDeliveryProtocolRepository.Add(defaultInformationKeys.Select(key => new SupplyInformationDeliveryProtocol {
                        SupplyInformationDeliveryProtocolKeyId = key.Id,
                        SupplyInvoiceId = message.SupplyInvoice.Id,
                        UserId = user.Id,
                        Created = message.SupplyInvoice.DateFrom ?? DateTime.UtcNow,
                        IsDefault = true
                    }));
            }

            Sender.Tell(
                supplyInvoiceRepository.GetById(message.SupplyInvoice.Id)
            );

            SupplyInvoice invoice = supplyInvoiceRepository.GetByIdWithoutIncludes(message.SupplyInvoice.Id);

            if (invoice != null)
                ActorReferenceManager.Instance.Get(BaseActorNames.PRODUCTS_MANAGEMENT_ACTOR)
                    .Tell(new UpdateInvoiceProductSpecificationAssignmentsMessage(invoice.NetUid));
        } catch (SupplyDocumentParseException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdateInvoiceDocumentsMessage(UpdateInvoiceDocumentsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.SupplyInvoice.InvoiceDocuments.Any())
            _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection).Update(message.SupplyInvoice.InvoiceDocuments);

        Sender.Tell(_supplyRepositoriesFactory
            .NewSupplyOrderRepository(connection)
            .GetByNetId(message.NetId)
        );
    }

    private void ProcessDeleteInvoiceDocumentMessage(DeleteInvoiceDocumentMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _supplyRepositoriesFactory
            .NewInvoiceDocumentRepository(connection)
            .Remove(message.NetId);
    }

    private void ProcessUpdateStatusOnShippedMessage(UpdateStatusOnShippedMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyInvoiceRepository supplyInvoiceRepository = _supplyRepositoriesFactory.NewSupplyInvoiceRepository(connection);

        SupplyInvoice supplyInvoice = supplyInvoiceRepository.GetByNetId(message.NetId);

        if (supplyInvoice != null) {
            if (!supplyInvoice.IsShipped.Equals(false)) return;

            supplyInvoice.IsShipped = true;
            supplyInvoiceRepository.SetIsShipped(supplyInvoice);

            Sender.Tell(supplyInvoiceRepository.GetById(supplyInvoice.Id));
        } else {
            Sender.Tell(null);
        }
    }

    private void ProcessDeleteSupplyInvoiceByNetIdMessage(DeleteSupplyInvoiceByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyInvoiceRepository supplyInvoiceRepository = _supplyRepositoriesFactory.NewSupplyInvoiceRepository(connection);

        SupplyInvoice supplyInvoice = supplyInvoiceRepository.GetByNetIdWithAllIncludes(message.NetId);

        if (supplyInvoice != null) {
            if (!supplyInvoice.PackingLists.Any(p => p.IsPlaced || p.PackingListPackageOrderItems.Any(i => i.PlacedQty > 0))) {
                IActReconciliationRepository actReconciliationRepository = _supplyUkraineRepositoriesFactory.NewActReconciliationRepository(connection);

                supplyInvoiceRepository
                    .Remove(message.NetId);

                ActReconciliation actReconciliation = actReconciliationRepository.GetBySupplyInvoiceId(supplyInvoice.Id);

                if (actReconciliation != null) actReconciliationRepository.Remove(actReconciliation.Id);

                _supplyRepositoriesFactory
                    .NewSupplyInvoiceOrderItemRepository(connection)
                    .RemoveAllByInvoiceId(
                        supplyInvoice.Id
                    );

                Sender.Tell(
                    (true, string.Empty)
                );
            } else {
                Sender.Tell(
                    (false, "UnableToDeleteInvoiceWithProductIncomes")
                );
            }
        } else {
            Sender.Tell(
                (false, "Specified Invoice does not exists")
            );
        }

        if (supplyInvoice != null)
            ActorReferenceManager.Instance.Get(SupplyActorNames.SUPPLY_INVOICE_ACTOR).Tell(new UpdateSupplyInvoiceItemGrossPriceMessage(
                _supplyRepositoriesFactory
                    .NewSupplyInvoiceRepository(connection)
                    .GetBySupplyOrderId(supplyInvoice.SupplyOrderId).Select(x => x.Id),
                message.UserNetId
            ));
    }

    private void ProcessAddOrUpdateSupplyInvoiceOrderItemsMessage(AddOrUpdateSupplyInvoiceOrderItemsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.SupplyInvoice.Equals(null)) {
            Sender.Tell(new Tuple<SupplyInvoice, string>(null, "Supply invoice can not be empty."));
        } else if (message.SupplyInvoice.IsNew()) {
            Sender.Tell(new Tuple<SupplyInvoice, string>(null, "New supply invoice is not valid input for current request."));
        } else if (message.SupplyInvoice.SupplyInvoiceOrderItems.Any(i => i.SupplyOrderItem.Equals(null))) {
            Sender.Tell(new Tuple<SupplyInvoice, string>(null, "Each SupplyInvoiceOrderItem should have SupplyOrderItems inside of it's entity."));
        } else if (message.SupplyInvoice.SupplyInvoiceOrderItems.Any()) {
            ISupplyInvoiceOrderItemRepository supplyInvoiceOrderItemRepository = _supplyRepositoriesFactory.NewSupplyInvoiceOrderItemRepository(connection);

            if (message.SupplyInvoice.SupplyInvoiceOrderItems.Any(i => !i.IsNew() && !i.Qty.Equals(0))) {
                supplyInvoiceOrderItemRepository.RemoveAllByInvoiceIdExceptProvided(
                    message.SupplyInvoice.Id,
                    message.SupplyInvoice.SupplyInvoiceOrderItems.Where(i => !i.IsNew() && !i.Qty.Equals(0)).Select(i => i.Id)
                );

                supplyInvoiceOrderItemRepository.Update(message.SupplyInvoice.SupplyInvoiceOrderItems.Where(i => !i.IsNew() && !i.Qty.Equals(0)));
            } else {
                supplyInvoiceOrderItemRepository.RemoveAllByInvoiceId(message.SupplyInvoice.Id);
            }

            if (message.SupplyInvoice.SupplyInvoiceOrderItems.Any(i => i.IsNew() && !i.Qty.Equals(0)))
                supplyInvoiceOrderItemRepository.Add(
                    message.SupplyInvoice.SupplyInvoiceOrderItems
                        .Where(i => i.IsNew() && !i.Qty.Equals(0))
                        .Select(i => {
                            i.SupplyInvoiceId = message.SupplyInvoice.Id;
                            i.SupplyOrderItemId = i.SupplyOrderItem.Id;

                            return i;
                        })
                );

            Sender.Tell(new Tuple<SupplyInvoice, string>(
                _supplyRepositoriesFactory
                    .NewSupplyInvoiceRepository(connection)
                    .GetByNetIdWithAllIncludes(message.SupplyInvoice.NetUid),
                string.Empty));
        } else {
            _supplyRepositoriesFactory.NewSupplyInvoiceOrderItemRepository(connection).RemoveAllByInvoiceId(message.SupplyInvoice.Id);

            Sender.Tell(new Tuple<SupplyInvoice, string>(
                _supplyRepositoriesFactory
                    .NewSupplyInvoiceRepository(connection)
                    .GetByNetIdWithAllIncludes(message.SupplyInvoice.NetUid),
                string.Empty));
        }
    }

    private void ProcessUpdateVatPercentToAllSupplyInvoicePackingListsMessage(UpdateVatPercentToAllSupplyInvoicePackingListsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.SupplyInvoice != null) {
            IPackingListPackageOrderItemRepository packingListPackageOrderItemRepository =
                _supplyRepositoriesFactory.NewPackingListPackageOrderItemRepository(connection);

            foreach (PackingList packList in message.SupplyInvoice.PackingLists)
                packingListPackageOrderItemRepository
                    .UpdateVatPercent(
                        packList.PackingListPackageOrderItems
                    );

            Sender.Tell(
                _supplyRepositoriesFactory
                    .NewSupplyInvoiceRepository(connection)
                    .GetByNetIdWithAllIncludes(
                        message.SupplyInvoice.NetUid
                    )
            );
        } else {
            Sender.Tell(
                message.SupplyInvoice
            );
        }
    }

    private void ProcessUploadProductSpecificationForInvoice(UploadProductSpecificationForInvoiceMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        SupplyInvoice invoice =
            _supplyRepositoriesFactory
                .NewSupplyInvoiceRepository(connection)
                .GetByNetIdWithItemsAndSpecification(message.InvoiceNetId);

        if (invoice == null) {
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
                    Locale = invoice.SupplyOrder.Organization.Culture,
                    ProductId = product.Id,
                    Product = product,
                    AddedById = user.Id,
                    DutyPercent =
                        decimal.Round(parsedSpecification.Duty * 100 / customsValue, 1, MidpointRounding.AwayFromZero),
                    VATPercent =
                        decimal.Round(parsedSpecification.VATValue * 100 / totalCustoms, 1, MidpointRounding.AwayFromZero),
                    Price = parsedSpecification.Price,
                    Qty = parsedSpecification.Qty
                });
        }

        foreach (ProductSpecification specification in specifications) {
            SupplyInvoiceOrderItem invoiceOrderItem =
                invoice.SupplyInvoiceOrderItems.FirstOrDefault(i =>
                    i.ProductId.Equals(specification.ProductId) && i.Qty.Equals(specification.Qty) && i.UnitPrice.Equals(specification.Price));

            if (invoiceOrderItem == null) {
                result.MissingProducts.Add(specification.Product.VendorCode);

                continue;
            }

            if (invoiceOrderItem.ProductSpecification == null) {
                ProductSpecification activeSpecification =
                    specificationRepository
                        .GetActiveByProductIdAndLocale(
                            specification.ProductId,
                            invoice.SupplyOrder.Organization.Culture
                        );

                specification.Id = specificationRepository.Add(specification);

                orderProductSpecificationRepository.Add(new OrderProductSpecification {
                    SupplyInvoiceId = invoice.Id,
                    ProductSpecificationId = specification.Id,
                    Qty = invoiceOrderItem.Qty,
                    UnitPrice = specification.Price
                });

                result.SuccessfullyUpdatedProducts.Add(specification.Product.VendorCode);

                if (activeSpecification != null) continue;

                specificationRepository.SetInactiveByProductId(specification.ProductId, invoice.SupplyOrder.Organization.Culture);

                specification.IsActive = true;

                specificationRepository.Update(specification);
            } else {
                if (invoiceOrderItem.ProductSpecification.SpecificationCode == specification.SpecificationCode &&
                    invoiceOrderItem.ProductSpecification.Name == specification.Name &&
                    invoiceOrderItem.ProductSpecification.DutyPercent == specification.DutyPercent) {
                    if (invoiceOrderItem.ProductSpecification.CustomsValue == specification.CustomsValue &&
                        invoiceOrderItem.ProductSpecification.Duty == specification.Duty &&
                        invoiceOrderItem.ProductSpecification.VATValue == specification.VATValue) {
                        result.UpdateNotRequiredProducts.Add(specification.Product.VendorCode);

                        continue;
                    }

                    specification.Id = invoiceOrderItem.ProductSpecification.Id;
                    specificationRepository.Update(specification);
                }

                ProductSpecification activeSpecification =
                    specificationRepository
                        .GetActiveByProductIdAndLocale(
                            specification.ProductId,
                            invoice.SupplyOrder.Organization.Culture
                        );

                specification.Id = specificationRepository.Add(specification);

                orderProductSpecificationRepository.Add(new OrderProductSpecification {
                    SupplyInvoiceId = invoice.Id,
                    ProductSpecificationId = specification.Id,
                    Qty = invoiceOrderItem.Qty,
                    UnitPrice = specification.Price
                });

                result.SuccessfullyUpdatedProducts.Add(specification.Product.VendorCode);

                if (activeSpecification != null) continue;

                specificationRepository.SetInactiveByProductId(specification.ProductId, invoice.SupplyOrder.Organization.Culture);

                specification.IsActive = true;

                specificationRepository.Update(specification);
            }
        }

        Sender.Tell(result);

        ProcessUpdateSupplyInvoiceGrossPrice(new UpdateSupplyInvoiceItemGrossPriceMessage(
            new List<long> { invoice.Id }, message.UserNetId));
    }

    private static void AddOrUpdatePaymentDeliveryProtocols(
        long supplyInvoiceId,
        IEnumerable<SupplyOrderPaymentDeliveryProtocol> paymentDeliveryProtocols,
        ICollection<long> newPaymentTaskIds,
        ISupplyOrderPaymentDeliveryProtocolRepository supplyOrderPaymentDeliveryProtocolRepository,
        ISupplyOrderPaymentDeliveryProtocolKeyRepository supplyOrderPaymentDeliveryProtocolKeyRepository,
        ISupplyPaymentTaskRepository supplyPaymentTaskRepository) {
        List<SupplyOrderPaymentDeliveryProtocol> paymentDeliveryProtocolsToUpdate = new();
        List<SupplyOrderPaymentDeliveryProtocol> paymentDeliveryProtocolsToAdd = new();

        List<SupplyPaymentTask> supplyPaymentTasksToUpdate = new();
        List<SupplyOrderPaymentDeliveryProtocolKey> supplyOrderPaymentDeliveryProtocolKeysToUpdate = new();

        foreach (SupplyOrderPaymentDeliveryProtocol paymentDeliveryProtocol in paymentDeliveryProtocols) {
            paymentDeliveryProtocol.SupplyInvoiceId = supplyInvoiceId;

            if (paymentDeliveryProtocol.User != null) paymentDeliveryProtocol.UserId = paymentDeliveryProtocol.User.Id;

            if (paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey != null) {
                if (paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey.IsNew()) {
                    paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKeyId = supplyOrderPaymentDeliveryProtocolKeyRepository
                        .Add(paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey);
                } else {
                    paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKeyId = paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey.Id;
                    supplyOrderPaymentDeliveryProtocolKeysToUpdate.Add(paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey);
                }
            }

            if (paymentDeliveryProtocol.SupplyPaymentTask != null) {
                if (paymentDeliveryProtocol.SupplyPaymentTask.User != null)
                    paymentDeliveryProtocol.SupplyPaymentTask.UserId = paymentDeliveryProtocol.SupplyPaymentTask.User.Id;

                if (paymentDeliveryProtocol.SupplyPaymentTask.IsNew()) {
                    paymentDeliveryProtocol.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;

                    paymentDeliveryProtocol.SupplyPaymentTask.PayToDate = !paymentDeliveryProtocol.SupplyPaymentTask.PayToDate.HasValue
                        ? DateTime.UtcNow.Date
                        : paymentDeliveryProtocol.SupplyPaymentTask.PayToDate.Value.Date;

                    paymentDeliveryProtocol.SupplyPaymentTask.NetPrice = paymentDeliveryProtocol.Value;
                    paymentDeliveryProtocol.SupplyPaymentTask.GrossPrice = paymentDeliveryProtocol.Value;

                    long paymentTaskId = supplyPaymentTaskRepository
                        .Add(paymentDeliveryProtocol.SupplyPaymentTask);

                    newPaymentTaskIds.Add(paymentTaskId);

                    paymentDeliveryProtocol.SupplyPaymentTaskId = paymentTaskId;
                } else {
                    if (paymentDeliveryProtocol.SupplyPaymentTask.TaskStatusUpdated == null && paymentDeliveryProtocol.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.Done))
                        supplyPaymentTaskRepository.UpdateTaskStatus(paymentDeliveryProtocol.SupplyPaymentTask);

                    supplyPaymentTasksToUpdate.Add(paymentDeliveryProtocol.SupplyPaymentTask);
                }
            }

            if (paymentDeliveryProtocol.IsNew())
                paymentDeliveryProtocolsToAdd.Add(paymentDeliveryProtocol);
            else
                paymentDeliveryProtocolsToUpdate.Add(paymentDeliveryProtocol);
        }

        if (paymentDeliveryProtocolsToUpdate.Any()) supplyOrderPaymentDeliveryProtocolRepository.Update(paymentDeliveryProtocolsToUpdate);

        if (paymentDeliveryProtocolsToAdd.Any()) supplyOrderPaymentDeliveryProtocolRepository.Add(paymentDeliveryProtocolsToAdd);

        if (supplyPaymentTasksToUpdate.Any()) supplyPaymentTaskRepository.Update(supplyPaymentTasksToUpdate);

        if (supplyOrderPaymentDeliveryProtocolKeysToUpdate.Any()) supplyOrderPaymentDeliveryProtocolKeyRepository.Update(supplyOrderPaymentDeliveryProtocolKeysToUpdate);
    }

    private void ProcessAddDocumentsToSupplyInvoice(AddDocumentsToSupplyInvoiceMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyInvoiceDeliveryDocumentRepository supplyInvoiceDeliveryDocumentRepository =
                _supplyRepositoriesFactory.NewSupplyInvoiceDeliveryDocumentRepository(connection);
            IDeliveryProductProtocolRepository deliveryProductProtocolRepository =
                _supplyRepositoriesFactory.NewDeliveryProductProtocolRepository(connection);

            DeliveryProductProtocol protocol =
                deliveryProductProtocolRepository.GetById(message.SupplyInvoice.DeliveryProductProtocolId ?? 0);

            SupplyInvoice existInvoice =
                _supplyRepositoriesFactory
                    .NewSupplyInvoiceRepository(connection)
                    .GetById(message.SupplyInvoice.Id);

            if (message.SupplyInvoice.SupplyInvoiceDeliveryDocuments.Any()) {
                supplyInvoiceDeliveryDocumentRepository
                    .RemoveBySupplyInvoiceIdExceptProvided(
                        message.SupplyInvoice.Id,
                        message.SupplyInvoice.SupplyInvoiceDeliveryDocuments.Select(x => x.Id)
                    );

                supplyInvoiceDeliveryDocumentRepository.Remove(
                    message.SupplyInvoice.SupplyInvoiceDeliveryDocuments
                        .Where(x => x.Deleted.Equals(true)));

                foreach (SupplyInvoiceDeliveryDocument document in message.SupplyInvoice.SupplyInvoiceDeliveryDocuments.Where(document => document.IsNew())) {
                    SupplyInvoiceDeliveryDocument lastRecord =
                        supplyInvoiceDeliveryDocumentRepository
                            .GetLastRecord();

                    document.SupplyInvoiceId = message.SupplyInvoice.Id;

                    SupplyDeliveryDocument deliveryDocument =
                        _supplyRepositoriesFactory
                            .NewSupplyDeliveryDocumentRepository(connection)
                            .GetForInvoiceByTransportationType(
                                protocol.TransportationType
                            );

                    if (deliveryDocument != null) {
                        document.SupplyDeliveryDocumentId = deliveryDocument.Id;
                    } else {
                        long newDeliveryDocumentId = _supplyRepositoriesFactory
                            .NewSupplyDeliveryDocumentRepository(connection)
                            .Add(new SupplyDeliveryDocument {
                                Created = DateTime.Now,
                                Name = "Invoice",
                                TransportationType = protocol.TransportationType
                            });

                        document.SupplyDeliveryDocumentId = newDeliveryDocumentId;
                    }

                    document.Number =
                        lastRecord != null && !string.IsNullOrEmpty(lastRecord.Number)
                            ? string.Format(
                                "{0:D9}",
                                Convert.ToInt64(
                                    lastRecord
                                        .Number
                                ) + 1
                            )
                            : string.Format(
                                "{0:D9}",
                                1
                            );

                    supplyInvoiceDeliveryDocumentRepository.Add(document);
                }
            }

            _supplyRepositoriesFactory
                .NewSupplyInvoiceRepository(connection)
                .UpdateCustomDeclarationData(message.SupplyInvoice);

            Sender.Tell(deliveryProductProtocolRepository.GetByNetId(
                protocol.NetUid
            ));

            if (message.SupplyInvoice.DateCustomDeclaration.HasValue && !message.SupplyInvoice.DateCustomDeclaration.Value.Equals(existInvoice.DateCustomDeclaration))
                ActorReferenceManager.Instance.Get(SupplyActorNames.DELIVERY_PRODUCT_PROTOCOL).Tell(new ResetGrossPriceInProtocolMessage(
                    protocol.NetUid,
                    message.UserNetId
                ));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessAddDocumentToInvoiceForOrder(AddDocumentToInvoiceForOrderMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyInvoiceDeliveryDocumentRepository supplyInvoiceDeliveryDocumentRepository =
                _supplyRepositoriesFactory.NewSupplyInvoiceDeliveryDocumentRepository(connection);
            ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory
                .NewSupplyOrderRepository(connection);

            SupplyOrder order =
                supplyOrderRepository.GetById(message.SupplyInvoice.SupplyOrderId);

            if (message.SupplyInvoice.SupplyInvoiceDeliveryDocuments.Any()) {
                supplyInvoiceDeliveryDocumentRepository
                    .RemoveBySupplyInvoiceIdExceptProvided(
                        message.SupplyInvoice.Id,
                        message.SupplyInvoice.SupplyInvoiceDeliveryDocuments.Select(x => x.Id)
                    );

                supplyInvoiceDeliveryDocumentRepository.Remove(
                    message.SupplyInvoice.SupplyInvoiceDeliveryDocuments
                        .Where(x => x.Deleted.Equals(true)));

                foreach (SupplyInvoiceDeliveryDocument document in message.SupplyInvoice.SupplyInvoiceDeliveryDocuments.Where(document => document.IsNew())) {
                    SupplyInvoiceDeliveryDocument lastRecord =
                        supplyInvoiceDeliveryDocumentRepository
                            .GetLastRecord();

                    document.SupplyInvoiceId = message.SupplyInvoice.Id;


                    SupplyDeliveryDocument deliveryDocument =
                        _supplyRepositoriesFactory
                            .NewSupplyDeliveryDocumentRepository(connection)
                            .GetForInvoiceByTransportationType(
                                order.TransportationType
                            );

                    if (deliveryDocument != null) {
                        document.SupplyDeliveryDocumentId = deliveryDocument.Id;
                    } else {
                        long newDeliveryDocumentId = _supplyRepositoriesFactory
                            .NewSupplyDeliveryDocumentRepository(connection)
                            .Add(new SupplyDeliveryDocument {
                                Created = DateTime.Now,
                                Name = "Invoice",
                                TransportationType = order.TransportationType
                            });

                        document.SupplyDeliveryDocumentId = newDeliveryDocumentId;
                    }

                    document.Number =
                        lastRecord != null && !string.IsNullOrEmpty(lastRecord.Number)
                            ? string.Format(
                                "{0:D9}",
                                Convert.ToInt64(
                                    lastRecord
                                        .Number
                                ) + 1
                            )
                            : string.Format(
                                "{0:D9}",
                                1
                            );

                    supplyInvoiceDeliveryDocumentRepository.Add(document);
                }
            }

            _supplyRepositoriesFactory
                .NewSupplyInvoiceRepository(connection)
                .UpdateCustomDeclarationData(message.SupplyInvoice);

            Sender.Tell(supplyOrderRepository.GetByNetId(
                order.NetUid
            ));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdateSupplyInvoiceGrossPrice(
        UpdateSupplyInvoiceItemGrossPriceMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyOrderRepository supplyOrderRepository =
                _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
            ICurrencyRepository currencyRepository =
                _currencyRepositoriesFactory.NewCurrencyRepository(connection);
            IGovExchangeRateRepository govExchangeRateRepository =
                _exchangeRateRepositoriesFactory.NewGovExchangeRateRepository(connection);
            IPackingListPackageOrderItemRepository packingListPackageOrderItemRepository =
                _supplyRepositoriesFactory.NewPackingListPackageOrderItemRepository(connection);
            IGovCrossExchangeRateRepository govCrossExchangeRateRepository = _exchangeRateRepositoriesFactory.NewGovCrossExchangeRateRepository(connection);
            ISupplyInvoiceBillOfLadingServiceRepository supplyInvoiceBillOfLadingServiceRepository =
                _supplyRepositoriesFactory.NewSupplyInvoiceBillOfLadingServiceRepository(connection);
            ISupplyInvoiceMergedServiceRepository supplyInvoiceMergedServiceRepository =
                _supplyRepositoriesFactory.NewSupplyInvoiceMergedServiceRepository(connection);
            IPackingListPackageOrderItemSupplyServiceRepository packingListPackageOrderItemSupplyServiceRepository =
                _supplyRepositoriesFactory.NewPackingListPackageOrderItemSupplyServiceRepository(connection);

            List<SupplyInvoice> supplyInvoices =
                _supplyRepositoriesFactory
                    .NewSupplyInvoiceRepository(connection)
                    .GetByIds(message.SupplyInvoiceIds);

            List<long> supplyInvoicesWithCompletedProtocolIds = new();

            Currency eur = currencyRepository.GetEURCurrencyIfExists();
            Currency uah = currencyRepository.GetUAHCurrencyIfExists();

            User user = _userRepositoriesFactory.NewUserRepository(connection)
                .GetByNetId(message.UserNetId);

            foreach (SupplyInvoice invoice in supplyInvoices) {
                DeliveryProductProtocol protocol = new();

                if (invoice.DeliveryProductProtocolId.HasValue)
                    protocol =
                        _supplyRepositoriesFactory.NewDeliveryProductProtocolRepository(connection)
                            .GetById(invoice.DeliveryProductProtocolId.Value);

                SupplyOrder supplyOrder = supplyOrderRepository.GetByNetId(invoice.SupplyOrder.NetUid);

                decimal govExchangeRateFromInvoiceAmount =
                    GetGovExchangeRateOnDateToUah(
                        supplyOrder.ClientAgreement.Agreement.Currency,
                        invoice.DateCustomDeclaration ?? invoice.Created,
                        govExchangeRateRepository,
                        currencyRepository
                    );

                GovExchangeRate govExchangeRate =
                    govExchangeRateRepository
                        .GetByCurrencyIdAndCode(uah.Id, eur.Code, invoice.DateCustomDeclaration ?? invoice.Created);

                decimal govExchangeRateFromUahToEur = govExchangeRate.Amount;

                double qtySupplyInvoiceInSupplyOrder = supplyOrderRepository.GetQtySupplyInvoiceById(supplyOrder.Id);

                decimal totalNetPrice = 0m;
                decimal totalGrossPrice = 0m;
                decimal totalAccountingGrossPrice = 0m;
                decimal totalGeneralAccountingGrossPrice = 0m;

                foreach (PackingList packListFromDb in invoice.PackingLists)
                foreach (PackingListPackageOrderItem packingListItem in packListFromDb.PackingListPackageOrderItems) {
                    if (!govExchangeRateFromInvoiceAmount.Equals(0))
                        packingListItem.UnitPriceUah =
                            govExchangeRateFromInvoiceAmount > 0
                                ? packingListItem.UnitPrice * govExchangeRateFromInvoiceAmount
                                : Math.Abs(
                                    packingListItem.UnitPrice / govExchangeRateFromInvoiceAmount);
                    else
                        packingListItem.UnitPriceUah = packingListItem.UnitPrice;

                    packingListItem.ExchangeRateAmount = govExchangeRateFromInvoiceAmount;
                    packingListItem.ExchangeRateAmountUahToEur = govExchangeRateFromUahToEur;

                    totalNetPrice += packingListItem.UnitPriceUah * Convert.ToDecimal(packingListItem.Qty);
                }

                foreach (CustomService service in supplyOrder.CustomServices) {
                    decimal govExchangeRateAmount =
                        service.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            service.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? service.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal servicePricePerItem = service.GrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice + servicePricePerItem / (0 - govExchangeRateAmount)
                            : totalGrossPrice + servicePricePerItem * govExchangeRateAmount;

                    decimal govAccountingExchangeRateAmount =
                        service.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            service.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? service.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal serviceAccountingPricePerItem = service.AccountingGrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    if (service.IsIncludeAccountingValue)
                        totalAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;
                    else
                        totalGeneralAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;

                    decimal grossPercentCurrentService =
                        servicePricePerItem / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        serviceAccountingPricePerItem / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, service.Id, TypeService.CustomService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = service.SupplyOrganizationAgreement.Currency.Id,
                                    Name = "����������� ����� " + service.Number,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    CustomServiceId = service.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? service.Created,
                                    Updated = DateTime.Now
                                };

                            if (service.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? service.Created;

                            existItem.CurrencyId = service.SupplyOrganizationAgreement.Currency.Id;

                            if (service.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                foreach (MergedService service in supplyOrder.MergedServices) {
                    decimal govExchangeRateAmount =
                        service.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            service.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? service.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal servicePricePerItem = service.GrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice + servicePricePerItem / (0 - govExchangeRateAmount)
                            : totalGrossPrice + servicePricePerItem * govExchangeRateAmount;

                    decimal govAccountingExchangeRateAmount =
                        service.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            service.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? service.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal serviceAccountingPricePerItem = service.AccountingGrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    if (service.IsIncludeAccountingValue)
                        totalAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;
                    else
                        totalGeneralAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;

                    decimal grossPercentCurrentService =
                        servicePricePerItem / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        serviceAccountingPricePerItem / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, service.Id, TypeService.MergedService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = service.SupplyOrganizationAgreement.Currency.Id,
                                    Name = "��'������� ����� " + service.Number,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    MergedServiceId = service.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? service.Created,
                                    Updated = DateTime.Now
                                };

                            if (service.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? service.Created;

                            existItem.CurrencyId = service.SupplyOrganizationAgreement.Currency.Id;

                            if (service.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                if (supplyOrder.PortWorkService != null) {
                    decimal govExchangeRateAmount =
                        supplyOrder.PortWorkService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.PortWorkService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.PortWorkService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );
                    decimal servicePricePerItem = supplyOrder.PortWorkService.GrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice + servicePricePerItem / (0 - govExchangeRateAmount)
                            : totalGrossPrice + servicePricePerItem * govExchangeRateAmount;

                    decimal govAccountingExchangeRateAmount =
                        supplyOrder.PortWorkService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.PortWorkService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.PortWorkService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal serviceAccountingPricePerItem = supplyOrder.PortWorkService.AccountingGrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    if (supplyOrder.PortWorkService.IsIncludeAccountingValue)
                        totalAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;
                    else
                        totalGeneralAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;

                    decimal grossPercentCurrentService =
                        servicePricePerItem / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        serviceAccountingPricePerItem / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, supplyOrder.PortWorkService.Id, TypeService.PortWorkService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = supplyOrder.PortWorkService.SupplyOrganizationAgreement.Currency.Id,
                                    Name = "����� �������� ���� " + supplyOrder.PortWorkService.Number,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    PortWorkServiceId = supplyOrder.PortWorkService.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.PortWorkService.Created,
                                    Updated = DateTime.Now
                                };

                            if (supplyOrder.PortWorkService.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.PortWorkService.Created;

                            existItem.CurrencyId = supplyOrder.PortWorkService.SupplyOrganizationAgreement.Currency.Id;

                            if (supplyOrder.PortWorkService.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                if (supplyOrder.TransportationService != null) {
                    decimal govExchangeRateAmount =
                        supplyOrder.TransportationService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.TransportationService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.TransportationService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal servicePricePerItem = supplyOrder.TransportationService.GrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice + servicePricePerItem / (0 - govExchangeRateAmount)
                            : totalGrossPrice + servicePricePerItem * govExchangeRateAmount;

                    decimal govAccountingExchangeRateAmount =
                        supplyOrder.TransportationService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.TransportationService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.TransportationService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal serviceAccountingPricePerItem = supplyOrder.TransportationService.AccountingGrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    if (supplyOrder.TransportationService.IsIncludeAccountingValue)
                        totalAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;
                    else
                        totalGeneralAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;

                    decimal grossPercentCurrentService =
                        servicePricePerItem / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        serviceAccountingPricePerItem / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, supplyOrder.TransportationService.Id,
                                    TypeService.TransportationService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = supplyOrder.TransportationService.SupplyOrganizationAgreement.Currency.Id,
                                    Name = "������������ ����� " + supplyOrder.TransportationService.Number,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    TransportationServiceId = supplyOrder.TransportationService.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.TransportationService.Created,
                                    Updated = DateTime.Now
                                };

                            if (supplyOrder.TransportationService.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.TransportationService.Created;

                            existItem.CurrencyId = supplyOrder.TransportationService.SupplyOrganizationAgreement.Currency.Id;

                            if (supplyOrder.TransportationService.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                if (supplyOrder.CustomAgencyService != null) {
                    decimal govExchangeRateAmount =
                        supplyOrder.CustomAgencyService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.CustomAgencyService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal servicePricePerItem = supplyOrder.CustomAgencyService.GrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice + servicePricePerItem / (0 - govExchangeRateAmount)
                            : totalGrossPrice + servicePricePerItem * govExchangeRateAmount;

                    decimal govAccountingExchangeRateAmount =
                        supplyOrder.CustomAgencyService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.CustomAgencyService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal serviceAccountingPricePerItem = supplyOrder.CustomAgencyService.AccountingGrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    if (supplyOrder.CustomAgencyService.IsIncludeAccountingValue)
                        totalAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;
                    else
                        totalGeneralAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;

                    decimal grossPercentCurrentService =
                        servicePricePerItem / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        serviceAccountingPricePerItem / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, supplyOrder.CustomAgencyService.Id, TypeService.CustomAgencyService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = supplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Currency.Id,
                                    Name = "����� ������� ��������� " + supplyOrder.CustomAgencyService.Number,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    CustomAgencyServiceId = supplyOrder.CustomAgencyService.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.CustomAgencyService.Created,
                                    Updated = DateTime.Now
                                };

                            if (supplyOrder.CustomAgencyService.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.CustomAgencyService.Created;

                            existItem.CurrencyId = supplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Currency.Id;

                            if (supplyOrder.CustomAgencyService.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                if (supplyOrder.PortCustomAgencyService != null) {
                    decimal govExchangeRateAmount =
                        supplyOrder.PortCustomAgencyService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.PortCustomAgencyService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal servicePricePerItem = supplyOrder.PortCustomAgencyService.GrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice + servicePricePerItem / (0 - govExchangeRateAmount)
                            : totalGrossPrice + servicePricePerItem * govExchangeRateAmount;

                    decimal govAccountingExchangeRateAmount =
                        supplyOrder.PortCustomAgencyService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.PortCustomAgencyService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal serviceAccountingPricePerItem = supplyOrder.PortCustomAgencyService.AccountingGrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    if (supplyOrder.PortCustomAgencyService.IsIncludeAccountingValue)
                        totalAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;
                    else
                        totalGeneralAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;

                    decimal grossPercentCurrentService =
                        servicePricePerItem / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        serviceAccountingPricePerItem / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, supplyOrder.PortCustomAgencyService.Id,
                                    TypeService.PortCustomAgencyService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = supplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.Currency.Id,
                                    Name = "����� ��������� �������� ��������� " + supplyOrder.PortCustomAgencyService.Number,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    PortCustomAgencyServiceId = supplyOrder.PortCustomAgencyService.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.PortCustomAgencyService.Created,
                                    Updated = DateTime.Now
                                };

                            if (supplyOrder.PortCustomAgencyService.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.PortCustomAgencyService.Created;

                            existItem.CurrencyId = supplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.Currency.Id;

                            if (supplyOrder.PortCustomAgencyService.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                if (supplyOrder.PlaneDeliveryService != null) {
                    decimal govExchangeRateAmount =
                        supplyOrder.PlaneDeliveryService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.PlaneDeliveryService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal servicePricePerItem = supplyOrder.PlaneDeliveryService.GrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice + servicePricePerItem / (0 - govExchangeRateAmount)
                            : totalGrossPrice + servicePricePerItem * govExchangeRateAmount;

                    decimal govAccountingExchangeRateAmount =
                        supplyOrder.PlaneDeliveryService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.PlaneDeliveryService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal serviceAccountingPricePerItem = supplyOrder.PlaneDeliveryService.AccountingGrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    if (supplyOrder.PlaneDeliveryService.IsIncludeAccountingValue)
                        totalAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;
                    else
                        totalGeneralAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;

                    decimal grossPercentCurrentService =
                        servicePricePerItem / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        serviceAccountingPricePerItem / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, supplyOrder.PlaneDeliveryService.Id,
                                    TypeService.PlaneDeliveryService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = supplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement.Currency.Id,
                                    Name = "����� �������� ������ " + supplyOrder.PlaneDeliveryService.Number,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    PlaneDeliveryServiceId = supplyOrder.PlaneDeliveryService.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.PlaneDeliveryService.Created,
                                    Updated = DateTime.Now
                                };

                            if (supplyOrder.PlaneDeliveryService.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.PlaneDeliveryService.Created;

                            existItem.CurrencyId = supplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement.Currency.Id;

                            if (supplyOrder.PlaneDeliveryService.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                if (supplyOrder.VehicleDeliveryService != null) {
                    decimal govExchangeRateAmount =
                        supplyOrder.VehicleDeliveryService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.VehicleDeliveryService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal servicePricePerItem = supplyOrder.VehicleDeliveryService.GrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice + servicePricePerItem / (0 - govExchangeRateAmount)
                            : totalGrossPrice + servicePricePerItem * govExchangeRateAmount;

                    decimal govAccountingExchangeRateAmount =
                        supplyOrder.VehicleDeliveryService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.VehicleDeliveryService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal serviceAccountingPricePerItem = supplyOrder.VehicleDeliveryService.AccountingGrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    if (supplyOrder.VehicleDeliveryService.IsIncludeAccountingValue)
                        totalAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;
                    else
                        totalGeneralAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;

                    decimal grossPercentCurrentService =
                        servicePricePerItem / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        serviceAccountingPricePerItem / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, supplyOrder.VehicleDeliveryService.Id,
                                    TypeService.VehicleDeliveryService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = supplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement.Currency.Id,
                                    Name = "����� ��������� ���������� " + supplyOrder.VehicleDeliveryService.Number,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    VehicleDeliveryServiceId = supplyOrder.VehicleDeliveryService.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.VehicleDeliveryService.Created,
                                    Updated = DateTime.Now
                                };

                            if (supplyOrder.VehicleDeliveryService.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.PlaneDeliveryService.Created;

                            existItem.CurrencyId = supplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement.Currency.Id;

                            if (supplyOrder.VehicleDeliveryService.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                if (supplyOrder.AdditionalPaymentCurrency != null && supplyOrder.AdditionalAmount > 0) {
                    decimal govExchangeRateAmount =
                        GetGovExchangeRateUk(
                            supplyOrder.AdditionalPaymentCurrency,
                            eur,
                            govExchangeRateRepository,
                            govCrossExchangeRateRepository
                        );

                    decimal servicePricePerItem = supplyOrder.AdditionalAmount / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice +
                              servicePricePerItem / (0 - govExchangeRateAmount)
                            : totalGrossPrice +
                              servicePricePerItem * govExchangeRateAmount;
                }

                List<SupplyInvoiceBillOfLadingService> supplyInvoiceBillOfLadingServices =
                    supplyInvoiceBillOfLadingServiceRepository.GetBySupplyInvoiceId(invoice.Id);

                List<SupplyInvoiceMergedService> supplyInvoiceMergedServices =
                    supplyInvoiceMergedServiceRepository.GetBySupplyInvoiceId(invoice.Id);

                foreach (SupplyInvoiceMergedService supplyInvoiceMergedService in supplyInvoiceMergedServices) {
                    Currency currency = currencyRepository.GetByMergedServiceId(supplyInvoiceMergedService.MergedServiceId);

                    string serviceName = supplyInvoiceMergedService.MergedService.ConsumableProduct.Name + " " +
                                         supplyInvoiceMergedService.MergedService.Number;

                    decimal govExchangeRateAmount =
                        supplyInvoiceMergedService.MergedService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            currency,
                            invoice.DateCustomDeclaration ?? supplyInvoiceMergedService.MergedService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice + supplyInvoiceMergedService.Value / (0 - govExchangeRateAmount)
                            : totalGrossPrice + supplyInvoiceMergedService.Value * govExchangeRateAmount;

                    decimal govAccountingExchangeRateAmount =
                        supplyInvoiceMergedService.MergedService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            currency,
                            invoice.DateCustomDeclaration ?? supplyInvoiceMergedService.MergedService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    if (supplyInvoiceMergedService.MergedService.IsIncludeAccountingValue)
                        totalAccountingGrossPrice =
                            govAccountingExchangeRateAmount < 0
                                ? totalAccountingGrossPrice + supplyInvoiceMergedService.AccountingValue / (0 - govAccountingExchangeRateAmount)
                                : totalAccountingGrossPrice + supplyInvoiceMergedService.AccountingValue * govAccountingExchangeRateAmount;
                    else
                        totalGeneralAccountingGrossPrice =
                            govAccountingExchangeRateAmount < 0
                                ? totalGeneralAccountingGrossPrice + supplyInvoiceMergedService.AccountingValue / (0 - govAccountingExchangeRateAmount)
                                : totalGeneralAccountingGrossPrice + supplyInvoiceMergedService.AccountingValue * govAccountingExchangeRateAmount;

                    decimal grossPercentCurrentService =
                        supplyInvoiceMergedService.Value / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        supplyInvoiceMergedService.AccountingValue / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, supplyInvoiceMergedService.MergedService.Id,
                                    TypeService.MergedService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = currency.Id,
                                    Name = serviceName,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    MergedServiceId = supplyInvoiceMergedService.MergedService.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyInvoiceMergedService.MergedService.Created,
                                    Updated = DateTime.Now
                                };

                            if (supplyInvoiceMergedService.MergedService.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyInvoiceMergedService.MergedService.Created;

                            existItem.CurrencyId = currency.Id;

                            if (supplyInvoiceMergedService.MergedService.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                foreach (SupplyInvoiceBillOfLadingService supplyInvoice in supplyInvoiceBillOfLadingServices) {
                    Currency currency = currencyRepository.GetByBillOfLadingServiceId(supplyInvoice.BillOfLadingServiceId);

                    string serviceName = supplyInvoice.BillOfLadingService.TypeBillOfLadingService.Equals(TypeBillOfLadingService.Container)
                        ? "��������� " + supplyInvoice.BillOfLadingService.Number
                        : "��������� " + supplyInvoice.BillOfLadingService.Number;

                    decimal govExchangeRateAmount =
                        supplyInvoice.BillOfLadingService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            currency,
                            invoice.DateCustomDeclaration ?? supplyInvoice.BillOfLadingService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? decimal.Round(
                                totalGrossPrice + supplyInvoice.Value / (0 - govExchangeRateAmount),
                                2,
                                MidpointRounding.AwayFromZero
                            )
                            : decimal.Round(
                                totalGrossPrice + supplyInvoice.Value * govExchangeRateAmount,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                    decimal govAccountingExchangeRateAmount =
                        supplyInvoice.BillOfLadingService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            currency,
                            invoice.DateCustomDeclaration ?? supplyInvoice.BillOfLadingService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    if (supplyInvoice.BillOfLadingService.IsIncludeAccountingValue)
                        totalAccountingGrossPrice = totalAccountingGrossPrice +
                            govAccountingExchangeRateAmount < 0
                                ? decimal.Round(
                                    totalAccountingGrossPrice + supplyInvoice.AccountingValue / (0 - govAccountingExchangeRateAmount),
                                    2,
                                    MidpointRounding.AwayFromZero
                                )
                                : decimal.Round(
                                    totalAccountingGrossPrice + supplyInvoice.AccountingValue * govAccountingExchangeRateAmount,
                                    2,
                                    MidpointRounding.AwayFromZero
                                );
                    else
                        totalGeneralAccountingGrossPrice = totalAccountingGrossPrice +
                            govAccountingExchangeRateAmount < 0
                                ? decimal.Round(
                                    totalGeneralAccountingGrossPrice + supplyInvoice.AccountingValue / (0 - govAccountingExchangeRateAmount),
                                    2,
                                    MidpointRounding.AwayFromZero
                                )
                                : decimal.Round(
                                    totalGeneralAccountingGrossPrice + supplyInvoice.AccountingValue * govAccountingExchangeRateAmount,
                                    2,
                                    MidpointRounding.AwayFromZero
                                );

                    decimal grossPercentCurrentService =
                        supplyInvoice.Value / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        supplyInvoice.AccountingValue / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, supplyInvoice.BillOfLadingService.Id,
                                    TypeService.BillOfLadingService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = currency.Id,
                                    Name = serviceName,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    BillOfLadingServiceId = supplyInvoice.BillOfLadingService.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyInvoice.BillOfLadingService.Created,
                                    Updated = DateTime.Now
                                };

                            if (supplyInvoice.BillOfLadingService.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyInvoice.BillOfLadingService.Created;

                            existItem.CurrencyId = currency.Id;

                            if (supplyInvoice.BillOfLadingService.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                foreach (PackingList packingList in invoice.PackingLists) {
                    if (!packingList.ContainerServiceId.HasValue && !packingList.VehicleServiceId.HasValue) continue;
                    decimal govExchangeRateAmount;
                    decimal accountingGovExchangeRateAmount;
                    TypeService typeService;
                    bool isIncludeAccountingValue;
                    DateTime createdService;
                    long serviceId;
                    Currency fromContainerOrVehicleService;
                    string serviceName;
                    if (packingList.ContainerServiceId.HasValue) {
                        fromContainerOrVehicleService = currencyRepository.GetByContainerServiceId(packingList.ContainerServiceId.Value);
                        ContainerService containerService =
                            _supplyRepositoriesFactory
                                .NewContainerServiceRepository(connection)
                                .GetById(packingList.ContainerServiceId.Value);

                        govExchangeRateAmount =
                            containerService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                                fromContainerOrVehicleService,
                                invoice.DateCustomDeclaration ?? containerService.Created,
                                govExchangeRateRepository,
                                currencyRepository
                            );

                        accountingGovExchangeRateAmount =
                            containerService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                                fromContainerOrVehicleService,
                                invoice.DateCustomDeclaration ?? containerService.Created,
                                govExchangeRateRepository,
                                currencyRepository
                            );

                        serviceId = packingList.ContainerServiceId.Value;
                        createdService = containerService.Created;
                        isIncludeAccountingValue = containerService.IsIncludeAccountingValue;
                        typeService = TypeService.ContainerService;
                        serviceName = "��������� " + containerService.Number;
                    } else {
                        fromContainerOrVehicleService = currencyRepository.GetByVehicleServiceId(packingList.VehicleServiceId.Value);

                        VehicleService vehicleService =
                            _supplyRepositoriesFactory
                                .NewVehicleServiceRepository(connection)
                                .GetById(packingList.VehicleServiceId.Value);

                        govExchangeRateAmount =
                            vehicleService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                                fromContainerOrVehicleService,
                                invoice.DateCustomDeclaration ?? vehicleService.Created,
                                govExchangeRateRepository,
                                currencyRepository
                            );

                        accountingGovExchangeRateAmount =
                            vehicleService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                                fromContainerOrVehicleService,
                                invoice.DateCustomDeclaration ?? vehicleService.Created,
                                govExchangeRateRepository,
                                currencyRepository
                            );

                        serviceId = packingList.VehicleServiceId.Value;
                        createdService = vehicleService.Created;
                        isIncludeAccountingValue = vehicleService.IsIncludeAccountingValue;
                        typeService = TypeService.VehicleService;
                        serviceName = "��������� " + vehicleService.Number;
                    }

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? decimal.Round(
                                totalGrossPrice + packingList.ExtraCharge / (0 - govExchangeRateAmount),
                                2,
                                MidpointRounding.AwayFromZero
                            )
                            : decimal.Round(
                                totalGrossPrice + packingList.ExtraCharge * govExchangeRateAmount,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                    totalAccountingGrossPrice =
                        accountingGovExchangeRateAmount < 0
                            ? decimal.Round(
                                totalAccountingGrossPrice + packingList.AccountingExtraCharge / (0 - accountingGovExchangeRateAmount),
                                2,
                                MidpointRounding.AwayFromZero
                            )
                            : decimal.Round(
                                totalAccountingGrossPrice + packingList.AccountingExtraCharge * accountingGovExchangeRateAmount,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                    decimal totalNetPricePackingList =
                        packingList.PackingListPackageOrderItems.Sum(packingListItem => packingListItem.UnitPrice * Convert.ToDecimal(packingListItem.Qty));

                    decimal grossPercentCurrentService =
                        packingList.ExtraCharge / totalNetPricePackingList;

                    decimal accountingGrossPercentCurrentService =
                        packingList.AccountingExtraCharge / totalNetPricePackingList;

                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, serviceId,
                                    typeService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = fromContainerOrVehicleService.Id,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    Name = serviceName,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? createdService,
                                    Updated = DateTime.Now
                                };

                            if (typeService.Equals(TypeService.ContainerService))
                                newItem.ContainerServiceId = serviceId;
                            else
                                newItem.VehicleServiceId = serviceId;

                            if (isIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? createdService;

                            existItem.CurrencyId = fromContainerOrVehicleService.Id;

                            if (isIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                decimal deliveryTotal = govExchangeRateFromInvoiceAmount > 0
                    ? invoice.DeliveryAmount * govExchangeRateFromInvoiceAmount
                    : Math.Abs(
                        invoice.DeliveryAmount / govExchangeRateFromInvoiceAmount);

                decimal deliveryPercent = deliveryTotal * 100 / totalNetPrice;

                decimal grossPercent = totalGrossPrice * 100 / totalNetPrice;

                decimal accountingGrossPercent = totalAccountingGrossPrice * 100 / totalNetPrice;

                decimal generalAccountingGrossPercent = totalGeneralAccountingGrossPrice * 100 / totalNetPrice;

                if (invoice.SupplyOrder.AdditionalPercent > 0)
                    accountingGrossPercent += accountingGrossPercent * (Convert.ToDecimal(invoice.SupplyOrder.AdditionalPercent) / 100);

                foreach (PackingList packingList in invoice.PackingLists) {
                    decimal containerPricePerItem = 0m;
                    decimal accountingContainerPricePerItem = 0m;

                    double qtyItems = packingList.PackingListPackageOrderItems.Sum(x => x.Qty);

                    if (!packingList.ExtraCharge.Equals(0))
                        containerPricePerItem = packingList.ExtraCharge / Convert.ToDecimal(qtyItems);

                    if (!packingList.AccountingExtraCharge.Equals(0))
                        accountingContainerPricePerItem = packingList.AccountingExtraCharge / Convert.ToDecimal(qtyItems);

                    foreach (PackingListPackageOrderItem packingListItem in packingList.PackingListPackageOrderItems) {
                        ProductSpecification actuallyProductSpecification =
                            _productRepositoriesFactory.NewProductSpecificationRepository(connection)
                                .GetByProductAndSupplyInvoiceIdsIfExists(
                                    packingListItem.SupplyInvoiceOrderItem.Product.Id,
                                    invoice.Id);

                        if (actuallyProductSpecification != null) {
                            packingListItem.VatAmount = actuallyProductSpecification.VATValue;
                            packingListItem.VatPercent = actuallyProductSpecification.VATPercent;
                        }

                        decimal productSpecificationValues =
                            actuallyProductSpecification != null ? actuallyProductSpecification.Duty + actuallyProductSpecification.VATValue : 0;

                        decimal totalNetPriceItem = packingListItem.UnitPriceUah * Convert.ToDecimal(packingListItem.Qty);

                        decimal specificationValuesPerUnit = productSpecificationValues * 100 / totalNetPriceItem;

                        decimal accountingGrossPercentPerItem = accountingGrossPercent + specificationValuesPerUnit + deliveryPercent;

                        packingListItem.DeliveryPerItem =
                            decimal.Round(
                                packingListItem.UnitPriceUah * deliveryPercent / 100 / govExchangeRateFromInvoiceAmount,
                                14,
                                MidpointRounding.AwayFromZero
                            );

                        packingListItem.ContainerUnitPriceEur = containerPricePerItem;
                        packingListItem.AccountingContainerUnitPriceEur = accountingContainerPricePerItem;

                        packingListItem.GrossUnitPriceEur =
                            decimal.Round(
                                packingListItem.UnitPriceUah * grossPercent / 100 / govExchangeRateFromUahToEur,
                                14,
                                MidpointRounding.AwayFromZero
                            );

                        packingListItem.AccountingGeneralGrossUnitPriceEur =
                            decimal.Round(
                                packingListItem.UnitPriceUah * generalAccountingGrossPercent / 100 / govExchangeRateFromUahToEur,
                                14,
                                MidpointRounding.AwayFromZero
                            );

                        packingListItem.AccountingGrossUnitPriceEur =
                            decimal.Round(
                                (packingListItem.UnitPriceUah +
                                 packingListItem.UnitPriceUah * accountingGrossPercentPerItem / 100) / govExchangeRateFromUahToEur,
                                14,
                                MidpointRounding.AwayFromZero
                            );

                        if (!govExchangeRateFromUahToEur.Equals(1))
                            packingListItem.UnitPriceEur =
                                Math.Abs(govExchangeRateFromUahToEur > 0
                                    ? packingListItem.UnitPriceUah / govExchangeRateFromUahToEur
                                    : Math.Abs(packingListItem.UnitPriceUah * govExchangeRateFromUahToEur));
                        else
                            packingListItem.UnitPriceEur = packingListItem.UnitPrice;
                    }

                    packingListPackageOrderItemRepository.Update(packingList.PackingListPackageOrderItems);
                }

                if (!protocol.Id.Equals(0) && protocol.IsCompleted && user.UserRole.UserRoleType.Equals(UserRoleType.GBA))
                    supplyInvoicesWithCompletedProtocolIds.Add(invoice.Id);
            }

            if (supplyInvoicesWithCompletedProtocolIds.Any())
                ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new UpdateConsignmentItemGrossPriceMessage(
                    supplyInvoicesWithCompletedProtocolIds,
                    user.NetUid
                ));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private static decimal GetGovExchangeRateUk(
        Currency from,
        Currency to,
        IGovExchangeRateRepository govExchangeRateRepository,
        IGovCrossExchangeRateRepository govCrossExchangeRateRepository) {
        if (from.Id.Equals(to.Id))
            return 1m;

        GovExchangeRate exchangeRate =
            govExchangeRateRepository.GetByCurrencyIdAndCode(to.Id, from.Code);

        if (exchangeRate != null) return exchangeRate.Amount;

        exchangeRate =
            govExchangeRateRepository.GetByCurrencyIdAndCode(from.Id, to.Code);

        if (exchangeRate != null)
            return exchangeRate.Amount;

        GovCrossExchangeRate crossExchangeRate =
            govCrossExchangeRateRepository.GetByCurrenciesIds(to.Id, from.Id);

        if (crossExchangeRate != null) return decimal.Zero - crossExchangeRate.Amount;

        crossExchangeRate =
            govCrossExchangeRateRepository.GetByCurrenciesIds(from.Id, to.Id);

        return crossExchangeRate?.Amount ?? 1m;
    }

    private static decimal GetGovExchangeRateOnDateToUah(
        Currency from,
        DateTime onDate,
        IGovExchangeRateRepository govExchangeRateRepository,
        ICurrencyRepository currencyRepository) {
        Currency uah = currencyRepository.GetUAHCurrencyIfExists();

        if (from.Id.Equals(uah.Id))
            return 1m;

        GovExchangeRate govExchangeRate =
            govExchangeRateRepository
                .GetByCurrencyIdAndCode(
                    uah.Id, from.Code, onDate);

        return govExchangeRate?.Amount ?? 1m;
    }
}