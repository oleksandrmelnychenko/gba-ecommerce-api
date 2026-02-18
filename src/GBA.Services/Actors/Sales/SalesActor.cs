using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using Akka.Util.Internal;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.OrderPackages;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.Sales.SaleMerges;
using GBA.Domain.Entities.Sales.Shipments;
using GBA.Domain.Messages.Auditing;
using GBA.Domain.Messages.Communications.Hubs;
using GBA.Domain.Messages.Consignments;
using GBA.Domain.Messages.Deliveries.RecipientAddresses;
using GBA.Domain.Messages.Deliveries.Recipients;
using GBA.Domain.Messages.Sales;
using GBA.Domain.Messages.Sales.OrderItems;
using GBA.Domain.Messages.Sales.Reservations;
using GBA.Domain.Messages.Transporters;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Clients.RetailClients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.Delivery.Contracts;
using GBA.Domain.Repositories.DocumentMonths.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.UpdateDataCarriers.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Sales;

public sealed class SalesActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IDeliveryRepositoriesFactory _deliveryRepositoriesFactory;
    private readonly IDocumentMonthRepositoryFactory _documentMonthRepositoryFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IRetailClientRepositoriesFactory _retailClientRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly IUpdateDataCarrierRepositoryFactory _updateDataCarrierRepositoryFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public SalesActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IDeliveryRepositoriesFactory deliveryRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IDocumentMonthRepositoryFactory documentMonthRepositoryFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory,
        IRetailClientRepositoriesFactory retailClientRepositoriesFactory,
        IUpdateDataCarrierRepositoryFactory updateDataCarrierRepositoryFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _deliveryRepositoriesFactory = deliveryRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _documentMonthRepositoryFactory = documentMonthRepositoryFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;
        _retailClientRepositoriesFactory = retailClientRepositoriesFactory;
        _updateDataCarrierRepositoryFactory = updateDataCarrierRepositoryFactory;

        Receive<AddSaleMessage>(ProcessAddSaleMessage);

        Receive<AddSaleWithStatusesOnlyMessage>(ProcessAddSaleWithStatusesOnlyMessage);

        Receive<UpdateSaleMessage>(ProcessUpdateSaleMessage);

        Receive<UpdatStateSaleMessage>(ProcessStateUpdateSaleMessage);

        Receive<GetUpdateDataCarrierMessage>(ProcessGetUpdateDataCarrierMessage);

        Receive<UpdateSaleFromEcommerceMessage>(ProcessUpdateSaleFromECommerceMessage);

        Receive<AddSaleFutureReservationMessage>(ProcessAddSaleFutureReservationMessage);

        Receive<DeleteSaleFutureReservationByNetIdMessage>(ProcessDeleteSaleFutureReservationByNetIdMessage);

        Receive<GetExportRegisterInvoiceMessage>(ProcessGetExportRegisterInvoiceMessage);

        Receive<GetRegisterInvoiceMessage>(ProcessGetRegisterInvoiceMessage);

        Receive<DeleteSaleMessage>(ProcessDeleteSaleMessage);

        Receive<UpdateMergedSaleToBillMessage>(ProcessUpdateMergedSaleToBillMessage);

        Receive<UpdateOneTimeDiscountsOnSaleMessage>(ProcessUpdateOneTimeDiscountsOnSaleMessage);

        Receive<UpdateDeliveryRecipientMessage>(ProcessUpdateDeliveryRecipientMessage);

        Receive<UpdateDeliveryRecipientAddressMessage>(ProcessUpdateDeliveryRecipientAddressMessage);

        Receive<SwitchBillSaleUnderClientStructureMessage>(ProcessSwitchBillSaleUnderClientStructureMessage);

        Receive<ExportInvoiceForPaymentForSaleByNetIdForPrintingMessage>(ProcessExportInvoiceForPaymentForSaleByNetIdForPrintingMessage);

        Receive<ExportInvoiceForPaymentForSaleForPrintingFromLastStepMessage>(ProcessExportInvoiceForPaymentForSaleForPrintingFromLastStepMessage);
    }

    private void ProcessGetRegisterInvoiceMessage(GetRegisterInvoiceMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

        try {
            List<Sale> sales = saleRepository.GetAllRegisterIvoiceType(message.From, message.To, message.Value, message.Offset, message.Limit);
            Sender.Tell(sales);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetExportRegisterInvoiceMessage(GetExportRegisterInvoiceMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

        try {
            string xlsxFile = string.Empty;
            string pdfFile = string.Empty;
            List<Sale> sales = saleRepository.GetAllRegisterIvoiceType(message.From, message.To, message.Value, 0, 0);
            (xlsxFile, pdfFile) =
                _xlsFactoryManager
                    .NewSalesXlsManager()
                    .ExportUkRegisterInvoiceSaleToXlsx(
                        message.SaleInvoicesFolderPath,
                        sales,
                        message.To,
                        _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk"));

            Sender.Tell((xlsxFile, pdfFile));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessAddSaleMessage(AddSaleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
        ISaleNumberRepository saleNumberRepository = _saleRepositoriesFactory.NewSaleNumberRepository(connection);

        User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

        if (message.Sale.Transporter != null) {
            message.Sale.TransporterId = message.Sale.Transporter.Id;

            ActorReferenceManager.Instance.Get(BaseActorNames.TRANSPORTER_MANAGEMENT_ACTOR).Tell(new ChangeTransporterPriorityMessage(message.Sale.Transporter.NetUid));
        }

        if (message.Sale.SaleInvoiceDocument != null) {
            if (message.Sale.SaleInvoiceDocument.IsNew()) {
                message.Sale.SaleInvoiceDocumentId = _saleRepositoriesFactory.NewSaleInvoiceDocumentRepository(connection).Add(message.Sale.SaleInvoiceDocument);
            } else {
                _saleRepositoriesFactory.NewSaleInvoiceDocumentRepository(connection).Update(message.Sale.SaleInvoiceDocument);

                message.Sale.SaleInvoiceDocumentId = message.Sale.SaleInvoiceDocument.Id;
            }
        }

        if (message.Sale.DeliveryRecipient != null) {
            if (message.Sale.DeliveryRecipient.IsNew())
                message.Sale.DeliveryRecipientId = _deliveryRepositoriesFactory
                    .NewDeliveryRecipientRepository(connection)
                    .Add(message.Sale.DeliveryRecipient);
            else
                message.Sale.DeliveryRecipientId = message.Sale.DeliveryRecipient.Id;

            ActorReferenceManager.Instance.Get(BaseActorNames.DELIVERY_MANAGEMENT_ACTOR)
                .Tell(new ChangeDeliveryRecipientPriorityMessage((long)message.Sale.DeliveryRecipientId));
        }

        if (message.Sale.DeliveryRecipientAddress != null) {
            if (message.Sale.DeliveryRecipientAddress.IsNew())
                message.Sale.DeliveryRecipientAddressId = _deliveryRepositoriesFactory
                    .NewDeliveryRecipientAddressRepository(connection)
                    .Add(message.Sale.DeliveryRecipientAddress);
            else
                message.Sale.DeliveryRecipientAddressId = message.Sale.DeliveryRecipientAddress.Id;

            ActorReferenceManager.Instance.Get(BaseActorNames.DELIVERY_MANAGEMENT_ACTOR)
                .Tell(new ChangeDeliveryRecipientAddressPriorityMessage((long)message.Sale.DeliveryRecipientAddressId));
        }

        if (message.Sale.BaseLifeCycleStatus != null) {
            message.Sale.BaseLifeCycleStatusId = _saleRepositoriesFactory
                .NewBaseLifeCycleStatusRepository(connection)
                .Add(message.Sale.BaseLifeCycleStatus);
        } else {
            message.Sale.BaseLifeCycleStatus = new BaseLifeCycleStatus { SaleLifeCycleType = SaleLifeCycleType.New };
            message.Sale.BaseLifeCycleStatusId = _saleRepositoriesFactory
                .NewBaseLifeCycleStatusRepository(connection)
                .Add(message.Sale.BaseLifeCycleStatus);
        }

        if (message.Sale.BaseSalePaymentStatus != null) {
            message.Sale.BaseSalePaymentStatusId = _saleRepositoriesFactory
                .NewBaseSalePaymentStatusRepository(connection)
                .Add(message.Sale.BaseSalePaymentStatus);
        } else {
            message.Sale.BaseSalePaymentStatus = new BaseSalePaymentStatus { SalePaymentStatusType = SalePaymentStatusType.NotPaid };
            message.Sale.BaseSalePaymentStatusId = _saleRepositoriesFactory
                .NewBaseSalePaymentStatusRepository(connection)
                .Add(message.Sale.BaseSalePaymentStatus);
        }

        message.Sale.UserId = user.Id;

        if (message.Sale.ClientAgreement != null) {
            message.Sale.ClientAgreementId = message.Sale.ClientAgreement.Id;

            message.Sale.ClientAgreement =
                _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetByIdWithAgreementAndOrganization(message.Sale.ClientAgreementId);

            if (message.Sale.ClientAgreement.Agreement?.Organization != null) {
                SaleNumber lastSaleNumber = saleNumberRepository.GetLastRecordByOrganizationNetId(message.Sale.ClientAgreement.Agreement.Organization.NetUid);
                SaleNumber saleNumber;

                Organization organization = message.Sale.ClientAgreement.Agreement.Organization;
                string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

                if (lastSaleNumber != null && DateTime.Now.Year.Equals(lastSaleNumber.Created.Year))
                    saleNumber = new SaleNumber {
                        OrganizationId = organization.Id,
                        Value =
                            string.Format(
                                "{0}{1}{2}",
                                organization.Code,
                                currentMonth,
                                string.Format("{0:D8}",
                                    Convert.ToInt32(
                                        lastSaleNumber.Value.Substring(
                                            lastSaleNumber.Organization.Code.Length + currentMonth.Length,
                                            lastSaleNumber.Value.Length - (lastSaleNumber.Organization.Code.Length + currentMonth.Length))
                                    ) + 1)
                            )
                    };
                else
                    saleNumber = new SaleNumber {
                        OrganizationId = organization.Id,
                        Value = $"{organization.Code}{currentMonth}{string.Format("{0:D8}", 1)}"
                    };

                message.Sale.SaleNumberId = saleNumberRepository.Add(saleNumber);
            }
        }

        if (message.Sale.Order != null) {
            message.Sale.Order.ClientAgreementId = message.Sale.ClientAgreementId;
            message.Sale.Order.UserId = message.Sale.UserId;

            long orderId = _saleRepositoriesFactory.NewOrderRepository(connection).Add(message.Sale.Order);

            message.Sale.OrderId = orderId;
        }

        long saleId = saleRepository.Add(message.Sale);

        if (message.Sale.Order != null && message.Sale.Order.OrderItems.Any()) {
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            ClientAgreement clientAgreement =
                _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetWithOrganizationById(message.Sale.ClientAgreementId);

            foreach (OrderItem orderItem in message.Sale.Order.OrderItems.Where(i => i.IsNew() && i.Product != null && !i.Product.IsNew())) {
                IEnumerable<ProductAvailability> productAvailabilities =
                    productAvailabilityRepository
                        .GetByProductAndOrganizationIds(
                            orderItem.Product.Id,
                            (long)clientAgreement.Agreement.OrganizationId,
                            clientAgreement.Agreement.WithVATAccounting,
                            true,
                            clientAgreement.Agreement.Organization?.StorageId
                        );

                orderItem.UserId = user.Id;
                orderItem.OrderId = message.Sale.OrderId;

                orderItem.ProductId = orderItem.Product.Id;

                if (productAvailabilities.Sum(a => a.Amount) < orderItem.Qty)
                    orderItem.Qty = productAvailabilities.Sum(a => a.Amount);

                orderItem.Id = orderItemRepository.Add(orderItem);

                _saleRepositoriesFactory
                    .NewOrderItemMovementRepository(connection)
                    .Add(new OrderItemMovement {
                        OrderItemId = orderItem.Id,
                        UserId = user.Id,
                        Qty = orderItem.Qty
                    });

                double toDecreaseQty = orderItem.Qty;

                foreach (ProductAvailability productAvailability in productAvailabilities.Where(a => a.Amount > 0)) {
                    if (toDecreaseQty.Equals(0d)) break;

                    if (productAvailability.Amount >= toDecreaseQty) {
                        productReservationRepository
                            .Add(new ProductReservation {
                                OrderItemId = orderItem.Id,
                                ProductAvailabilityId = productAvailability.Id,
                                Qty = toDecreaseQty
                            });

                        productAvailability.Amount -= toDecreaseQty;

                        toDecreaseQty = 0d;
                    } else {
                        productReservationRepository
                            .Add(new ProductReservation {
                                OrderItemId = orderItem.Id,
                                ProductAvailabilityId = productAvailability.Id,
                                Qty = productAvailability.Amount
                            });

                        toDecreaseQty -= productAvailability.Amount;

                        productAvailability.Amount = 0d;
                    }

                    productAvailabilityRepository.Update(productAvailability);
                }
            }
        }

        Sale saleFromDb = saleRepository.GetById(saleId);

        string saleResourceName = SaleResourceNames.UPDATED;

        if (saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.Packaging))
            saleResourceName = SaleResourceNames.UPDATED_LIFE_CYCLE_STATUS;

        ActorReferenceManager.Instance.Get(SalesActorNames.BASE_SALES_GET_ACTOR)
            .Forward(
                new GetSaleStatisticWithResourceNameByNetIdMessage(
                    saleFromDb.NetUid,
                    saleResourceName,
                    false,
                    true
                )
            );

        if (message.Sale.BaseLifeCycleStatus != null && message.Sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.Packaging)) {
            SaveExchangeRates(_saleRepositoriesFactory, _exchangeRateRepositoriesFactory, connection, saleFromDb.Id);

            User updatedBy = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            saleRepository.SetChangedToInvoiceDateByNetId(saleFromDb.NetUid, updatedBy?.Id);

            saleFromDb = saleRepository.GetByNetId(saleFromDb.NetUid);

            SetDebtToClient(
                connection,
                _exchangeRateRepositoriesFactory,
                _clientRepositoriesFactory.NewClientAgreementRepository(connection),
                _clientRepositoriesFactory.NewClientInDebtRepository(connection),
                _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection),
                _currencyRepositoriesFactory.NewCurrencyRepository(connection),
                _saleRepositoriesFactory,
                saleFromDb
            );

            SaveExchangeRates(_saleRepositoriesFactory, _exchangeRateRepositoriesFactory, connection, saleFromDb.Id);

            SavePricesPerItem(
                _saleRepositoriesFactory,
                _exchangeRateRepositoriesFactory,
                _productRepositoriesFactory.NewProductGroupDiscountRepository(connection),
                connection,
                saleFromDb
            );

            _clientRepositoriesFactory.NewClientRegistrationTaskRepository(connection).SetDoneByClientId(saleFromDb.ClientAgreement.ClientId);

            _clientRepositoriesFactory.NewClientRepository(connection).SetTemporaryClientById(saleFromDb.ClientAgreement.ClientId);
        }

        if (!message.Sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New))
            ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                new RetrieveAndStoreAuditDataMessage(
                    message.UserNetId,
                    saleFromDb.NetUid,
                    "Sale",
                    new BaseLifeCycleStatus { SaleLifeCycleType = SaleLifeCycleType.New }
                )
            );

        ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
            new RetrieveAndStoreAuditDataMessage(message.UserNetId, saleFromDb.NetUid, "Sale", message.Sale.BaseLifeCycleStatus)
        );
    }

    private void ProcessAddSaleWithStatusesOnlyMessage(AddSaleWithStatusesOnlyMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
        ISaleNumberRepository saleNumberRepository = _saleRepositoriesFactory.NewSaleNumberRepository(connection);

        Sale saleFromDb = CreateNewSaleWithStatusesOnly(message,
            _saleRepositoriesFactory,
            _userRepositoriesFactory,
            connection,
            saleRepository,
            saleNumberRepository);

        if (message.OriginalMessage == null) {
            Sender.Tell(saleFromDb);
        } else {
            if (message.OriginalMessage is AddOrderItemMessage addOrderItemMessage) addOrderItemMessage.Sale = saleFromDb;

            if (message.OriginalMessage is ShiftSaleOrderItemMessage shiftSaleOrderItemMessage) shiftSaleOrderItemMessage.Sale = saleFromDb;

            ActorReferenceManager.Instance.Get(SalesActorNames.ORDER_ITEMS_ACTOR).Tell(message.OriginalMessage, Sender);
        }

        if (!message.Sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New))
            ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                new RetrieveAndStoreAuditDataMessage(
                    message.UserNetId,
                    saleFromDb.NetUid,
                    "Sale",
                    new BaseLifeCycleStatus { SaleLifeCycleType = SaleLifeCycleType.New }
                )
            );

        ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
            new RetrieveAndStoreAuditDataMessage(message.UserNetId, saleFromDb.NetUid, "Sale", message.Sale.BaseLifeCycleStatus)
        );
    }


    private void ProcessUpdateSaleMessage(UpdateSaleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
        IHistoryInvoiceEditRepository historyInvoiceEditRepository = _saleRepositoriesFactory.NewHistoryInvoiceEditRepository(connection);
        IWarehousesShipmentRepository warehousesShipmentRepository = _saleRepositoriesFactory.NewWarehousesShipmentRepository(connection);

        User updatedB = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UpdatedByNetId);
        if (!message.Sale.IsInvoice)
            if (!message.Sale.IsAcceptedToPacking) {
                message.Sale.UpdateUserId = updatedB.Id;
                saleRepository.UpdateUser(message.Sale);
            }

        if (message.Sale != null && !message.Sale.NetUid.Equals(Guid.Empty)) {
            Sale saleFromDb = saleRepository.GetByNetId(message.Sale.NetUid);
            if (saleFromDb.WarehousesShipment == null) saleFromDb.WarehousesShipmentId = SetWarehousesShipment(message.Sale, warehousesShipmentRepository, updatedB);

            saleFromDb.HistoryInvoiceEdit.ForEach(x => {
                historyInvoiceEditRepository.UpdateApproveUpdateFalse(x.NetUid);
            });
            BaseLifeCycleStatus oldSaleLifeCycleType = new() { SaleLifeCycleType = saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType };
            if (message.Sale.BaseLifeCycleStatus != null) {
                saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType = message.Sale.BaseLifeCycleStatus.SaleLifeCycleType;

                _saleRepositoriesFactory.NewBaseLifeCycleStatusRepository(connection).Update(saleFromDb.BaseLifeCycleStatus);
            }

            if (message.Sale.BaseSalePaymentStatus != null) {
                saleFromDb.BaseSalePaymentStatus.SalePaymentStatusType = message.Sale.BaseSalePaymentStatus.SalePaymentStatusType;
                _saleRepositoriesFactory.NewBaseSalePaymentStatusRepository(connection).Update(saleFromDb.BaseSalePaymentStatus);
            }

            if (message.Sale.Transporter != null) saleFromDb.TransporterId = message.Sale.Transporter.Id;

            if (message.Sale.DeliveryRecipient != null) {
                if (message.Sale.DeliveryRecipient.IsNew()) {
                    saleFromDb.DeliveryRecipientId = _deliveryRepositoriesFactory.NewDeliveryRecipientRepository(connection).Add(message.Sale.DeliveryRecipient);
                } else {
                    _deliveryRepositoriesFactory.NewDeliveryRecipientRepository(connection).Update(message.Sale.DeliveryRecipient);

                    saleFromDb.DeliveryRecipientId = message.Sale.DeliveryRecipient.Id;
                }
            }

            if (message.Sale.DeliveryRecipientAddress != null) {
                if (message.Sale.DeliveryRecipientAddress.IsNew()) {
                    if (saleFromDb.DeliveryRecipientId.HasValue) {
                        message.Sale.DeliveryRecipientAddress.DeliveryRecipientId = saleFromDb.DeliveryRecipientId ?? 0;

                        saleFromDb.DeliveryRecipientAddressId =
                            _deliveryRepositoriesFactory.NewDeliveryRecipientAddressRepository(connection).Add(message.Sale.DeliveryRecipientAddress);
                    }
                } else {
                    _deliveryRepositoriesFactory.NewDeliveryRecipientAddressRepository(connection).Update(message.Sale.DeliveryRecipientAddress);

                    saleFromDb.DeliveryRecipientAddressId = message.Sale.DeliveryRecipientAddress.Id;
                }
            }

            if (!string.IsNullOrEmpty(message.Sale.Comment)) saleFromDb.Comment = message.Sale.Comment;

            if (message.Sale.SaleInvoiceDocument != null) {
                if (message.Sale.SaleInvoiceDocument.IsNew()) {
                    saleFromDb.SaleInvoiceDocumentId = _saleRepositoriesFactory.NewSaleInvoiceDocumentRepository(connection).Add(message.Sale.SaleInvoiceDocument);
                } else {
                    _saleRepositoriesFactory.NewSaleInvoiceDocumentRepository(connection).Update(message.Sale.SaleInvoiceDocument);

                    saleFromDb.SaleInvoiceDocumentId = message.Sale.SaleInvoiceDocument.Id;
                }

                BaseLifeCycleStatus oldLifeCycleStatus = saleFromDb.BaseLifeCycleStatus;

                if (saleFromDb.BaseLifeCycleStatus != null) {
                    saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType = SaleLifeCycleType.Packaging;

                    _saleRepositoriesFactory.NewBaseLifeCycleStatusRepository(connection).Update(saleFromDb.BaseLifeCycleStatus);
                } else {
                    saleFromDb.BaseLifeCycleStatusId = _saleRepositoriesFactory.NewBaseLifeCycleStatusRepository(connection).Add(new BaseLifeCycleStatus {
                        SaleLifeCycleType = SaleLifeCycleType.Packaging
                    });
                }

                ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                    new RetrieveAndStoreAuditDataMessage(
                        message.UpdatedByNetId,
                        message.Sale.NetUid,
                        "Sale",
                        saleFromDb.BaseLifeCycleStatus,
                        oldLifeCycleStatus
                    )
                );
            }

            if (message.Sale.Order != null) {
                if (message.Sale.Order.OrderPackages.Any()) {
                    IOrderPackageRepository orderPackageRepository = _saleRepositoriesFactory.NewOrderPackageRepository(connection);
                    IOrderPackageItemRepository orderPackageItemRepository = _saleRepositoriesFactory.NewOrderPackageItemRepository(connection);
                    IOrderPackageUserRepository orderPackageUserRepository = _saleRepositoriesFactory.NewOrderPackageUserRepository(connection);

                    if (message.Sale.Order.OrderPackages.Any(p => !p.IsNew() && !p.Deleted))
                        _saleRepositoriesFactory.NewOrderPackageRepository(connection).RemoveAllByOrderIdExceptProvided(
                            message.Sale.Order.Id,
                            message.Sale.Order.OrderPackages
                                .Where(p => !p.IsNew() && !p.Deleted)
                                .Select(p => p.Id)
                        );
                    else
                        _saleRepositoriesFactory.NewOrderPackageRepository(connection).RemoveAllByOrderId(message.Sale.Order.Id);

                    foreach (OrderPackage package in message.Sale.Order.OrderPackages.Where(p => p.IsNew() && !p.Deleted)) {
                        if (!package.Lenght.Equals(0) && !package.Height.Equals(0) && !package.Width.Equals(0))
                            package.CBM = Math.Round(package.Lenght * package.Height * package.Width * 0.000001, 6);

                        package.OrderId = message.Sale.Order.Id;

                        package.Id = orderPackageRepository.Add(package);

                        if (package.OrderPackageItems.Any(i => i.IsNew() && !i.Deleted && !i.OrderItem.Equals(null)))
                            orderPackageItemRepository.Add(
                                package.OrderPackageItems
                                    .Where(i => i.IsNew() && !i.Deleted && !i.OrderItem.Equals(null))
                                    .Select(i => {
                                        i.OrderItemId = i.OrderItem.Id;
                                        i.OrderPackageId = package.Id;

                                        return i;
                                    })
                            );
                        if (package.OrderPackageUsers.Any(u => u.IsNew() && !u.Deleted && !u.User.Equals(null)))
                            orderPackageUserRepository.Add(
                                package.OrderPackageUsers
                                    .Where(u => u.IsNew() && !u.Deleted && !u.User.Equals(null))
                                    .Select(u => {
                                        u.UserId = u.User.Id;
                                        u.OrderPackageId = package.Id;

                                        return u;
                                    })
                            );
                    }

                    orderPackageRepository.Update(
                        message.Sale.Order.OrderPackages
                            .Where(p => !p.IsNew() && !p.Deleted)
                            .Select(package => {
                                if (!package.Lenght.Equals(0) && !package.Height.Equals(0) && !package.Width.Equals(0))
                                    package.CBM = Math.Round(package.Lenght * package.Height * package.Width * 0.000001, 6);

                                if (package.OrderPackageItems.Any()) {
                                    orderPackageItemRepository.RemoveAllByOrderPackageIdExceptProvided(
                                        package.Id,
                                        package.OrderPackageItems
                                            .Where(i => !i.IsNew() && !i.Deleted)
                                            .Select(i => i.Id)
                                    );

                                    if (package.OrderPackageItems.Any(i => i.IsNew() && !i.Deleted && !i.OrderItem.Equals(null)))
                                        orderPackageItemRepository.Add(
                                            package.OrderPackageItems
                                                .Where(i => i.IsNew() && !i.Deleted && !i.OrderItem.Equals(null))
                                                .Select(i => {
                                                    i.OrderItemId = i.OrderItem.Id;
                                                    i.OrderPackageId = package.Id;

                                                    return i;
                                                })
                                        );
                                } else {
                                    orderPackageItemRepository.RemoveAllByOrderPackageId(package.Id);
                                }

                                if (package.OrderPackageUsers.Any()) {
                                    orderPackageUserRepository.RemoveAllByOrderPackageIdExceptProvided(
                                        package.Id,
                                        package.OrderPackageUsers
                                            .Where(u => u.IsNew() && !u.Deleted)
                                            .Select(u => u.Id)
                                    );

                                    if (package.OrderPackageUsers.Any(u => u.IsNew() && !u.Deleted && !u.User.Equals(null)))
                                        orderPackageUserRepository.Add(
                                            package.OrderPackageUsers
                                                .Where(u => u.IsNew() && !u.Deleted && !u.User.Equals(null))
                                                .Select(u => {
                                                    u.UserId = u.User.Id;
                                                    u.OrderPackageId = package.Id;

                                                    return u;
                                                })
                                        );
                                } else {
                                    orderPackageUserRepository.RemoveAllByOrderPackageId(package.Id);
                                }

                                return package;
                            })
                    );
                } else {
                    _saleRepositoriesFactory.NewOrderPackageRepository(connection).RemoveAllByOrderId(message.Sale.Order.Id);
                }
            }

            if (message.Sale.ShipmentDate.HasValue && message.Sale.ShipmentDate.Value.Year.Equals(1)) message.Sale.ShipmentDate = null;

            if (message.Sale.CustomersOwnTtn != null && !message.Sale.CustomersOwnTtn.IsNew()) {
                saleRepository.UpdateCustomersOwnTtn(message.Sale.CustomersOwnTtn);

                saleFromDb.CustomersOwnTtn = message.Sale.CustomersOwnTtn;
            }

            if (message.Sale.CustomersOwnTtn != null && message.Sale.CustomersOwnTtn.IsNew()) {
                saleFromDb.CustomersOwnTtnId = saleRepository.AddCustomersOwnTtn(message.Sale.CustomersOwnTtn);

                saleFromDb.CustomersOwnTtn = saleRepository.GetCustomersOwnTtnById(saleFromDb.CustomersOwnTtnId.Value);
            }

            saleFromDb.IsPrinted = message.Sale.IsPrinted;
            saleFromDb.TTN = message.Sale.TTN;
            saleFromDb.ShippingAmount = message.Sale.ShippingAmount;
            saleFromDb.CashOnDeliveryAmount = message.Sale.CashOnDeliveryAmount;
            saleFromDb.IsCashOnDelivery = message.Sale.IsCashOnDelivery;
            saleFromDb.IsDevelopment = message.Sale.IsDevelopment;
            saleFromDb.IsPrintedPaymentInvoice = message.Sale.IsPrintedPaymentInvoice;
            saleFromDb.IsPrintedActProtocolEdit = message.Sale.IsPrintedActProtocolEdit;
            saleFromDb.HasDocuments = message.Sale.HasDocuments;
            saleFromDb.ShipmentDate = message.Sale.ShipmentDate;

            if (saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.Packaging) &&
                message.Sale.IsAcceptedToPacking) {
                saleRepository.UpdateIsAcceptedToPacking(message.Sale.Id, true);

                ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                    new RetrieveAndStoreAuditDataMessage(
                        message.UpdatedByNetId,
                        saleFromDb.NetUid,
                        "Sale",
                        saleFromDb.BaseLifeCycleStatus,
                        oldSaleLifeCycleType
                    )
                );
            }

            saleRepository.Update(saleFromDb);

            string saleResourceName = message.Sale.IsEdited
                ? SaleResourceNames.EDITED
                : !oldSaleLifeCycleType.SaleLifeCycleType.Equals(saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType)
                    ? SaleResourceNames.UPDATED_LIFE_CYCLE_STATUS
                    : SaleResourceNames.UPDATED;


            if (saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.Packaging) &&
                !oldSaleLifeCycleType.SaleLifeCycleType.Equals(saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType)) {
                User updatedBy = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UpdatedByNetId);

                saleRepository.SetChangedToInvoiceDateByNetId(saleFromDb.NetUid, updatedBy?.Id);

                if (saleFromDb.RetailClientId != null) {
                    IRetailPaymentStatusRepository retailPaymentStatusRepository = _retailClientRepositoriesFactory.NewRetailPaymentStatusRepository(connection);

                    RetailPaymentStatus retailPaymentStatus = retailPaymentStatusRepository.GetBySaleId(saleFromDb.Id);
                    retailPaymentStatusRepository.SetRetailPaymentStatusTypeById(RetailPaymentStatusType.ChangedToInvoice, retailPaymentStatus.Id);
                }

                saleFromDb = saleRepository.GetByNetId(saleFromDb.NetUid);

                SaveExchangeRates(_saleRepositoriesFactory, _exchangeRateRepositoriesFactory, connection, saleFromDb.Id);

                SavePricesPerItem(
                    _saleRepositoriesFactory,
                    _exchangeRateRepositoriesFactory,
                    _productRepositoriesFactory.NewProductGroupDiscountRepository(connection),
                    connection,
                    saleFromDb
                );

                SetDebtToClient(
                    connection,
                    _exchangeRateRepositoriesFactory,
                    _clientRepositoriesFactory.NewClientAgreementRepository(connection),
                    _clientRepositoriesFactory.NewClientInDebtRepository(connection),
                    _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection),
                    _currencyRepositoriesFactory.NewCurrencyRepository(connection),
                    _saleRepositoriesFactory,
                    saleFromDb
                );

                ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR)
                    .Tell(new StoreConsignmentMovementFromSaleMessage(saleFromDb.Id, Sender, saleResourceName));

                _clientRepositoriesFactory.NewClientRegistrationTaskRepository(connection).SetDoneByClientId(saleFromDb.ClientAgreement.ClientId);

                _clientRepositoriesFactory.NewClientRepository(connection).SetTemporaryClientById(saleFromDb.ClientAgreement.ClientId);
            } else {
                ActorReferenceManager.Instance.Get(SalesActorNames.BASE_SALES_GET_ACTOR)
                    .Forward(
                        new GetSaleStatisticWithResourceNameByNetIdMessage(
                            saleFromDb.NetUid,
                            saleResourceName,
                            false,
                            false,
                            true
                        )
                    );

                saleFromDb = saleRepository.GetByNetId(saleFromDb.NetUid);
            }

            if (!oldSaleLifeCycleType.SaleLifeCycleType.Equals(saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType))
                ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                    new RetrieveAndStoreAuditDataMessage(
                        message.UpdatedByNetId,
                        saleFromDb.NetUid,
                        "Sale",
                        saleFromDb.BaseLifeCycleStatus,
                        oldSaleLifeCycleType
                    )
                );
        } else {
            Sender.Tell(null);
        }
    }

    private void ProcessGetUpdateDataCarrierMessage(GetUpdateDataCarrierMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();

        Sender.Tell(_updateDataCarrierRepositoryFactory.NewUpdateDataCarrierRepository(connection).Get(message.SaleId));
    }

    private static long SetWarehousesShipment(Sale sale, IWarehousesShipmentRepository warehousesShipmentRepository, User updatedB) {
        WarehousesShipment warehousesShipment = new();

        warehousesShipment.HasDocument = sale.HasDocuments;
        warehousesShipment.IsCashOnDelivery = sale.IsCashOnDelivery;
        warehousesShipment.CashOnDeliveryAmount = sale.CashOnDeliveryAmount;
        warehousesShipment.ShipmentDate = sale.ShipmentDate;
        warehousesShipment.TTN = sale.TTN;
        if (sale.CustomersOwnTtn != null) {
            warehousesShipment.TtnPDFPath = sale.CustomersOwnTtn.TtnPDFPath;
            warehousesShipment.Number = sale.CustomersOwnTtn.Number;
        }

        if (!string.IsNullOrEmpty(sale.Comment)) warehousesShipment.Comment = sale.Comment;

        if (sale.DeliveryRecipientAddress != null) {
            warehousesShipment.City = sale.DeliveryRecipientAddress.City;
            warehousesShipment.Department = sale.DeliveryRecipientAddress.Department;
        }

        if (sale.DeliveryRecipient != null)
            if (!sale.DeliveryRecipient.IsNew()) {
                warehousesShipment.FullName = sale.DeliveryRecipient.FullName;
                warehousesShipment.MobilePhone = sale.DeliveryRecipient.MobilePhone;
            }

        if (sale.Transporter != null) warehousesShipment.TransporterId = sale.Transporter.Id;
        warehousesShipment.UserId = updatedB.Id;
        warehousesShipment.SaleId = sale.Id;
        warehousesShipment.IsDevelopment = false;


        WarehousesShipment warehousesShipments = warehousesShipmentRepository.Get(sale.Id);
        if (!warehousesShipments.IsNew()) return warehousesShipments.Id;

        return warehousesShipmentRepository.Add(warehousesShipment);
    }

    private static void UpdateWarehousesShipment(UpdatStateSaleMessage message, IWarehousesShipmentRepository warehousesShipmentRepository, User updatedB, long saleId) {
        WarehousesShipment warehousesShipment = warehousesShipmentRepository.Get(saleId);

        warehousesShipment.HasDocument = message.Sale.HasDocuments;
        warehousesShipment.IsCashOnDelivery = message.Sale.IsCashOnDelivery;
        warehousesShipment.CashOnDeliveryAmount = message.Sale.CashOnDeliveryAmount;
        warehousesShipment.ShipmentDate = message.Sale.ShipmentDate;
        warehousesShipment.TTN = message.Sale.TTN;
        if (message.Sale.CustomersOwnTtn != null) {
            warehousesShipment.TtnPDFPath = message.Sale.CustomersOwnTtn.TtnPDFPath;
            warehousesShipment.Number = message.Sale.CustomersOwnTtn.Number;
        }

        if (!string.IsNullOrEmpty(message.Sale.Comment)) warehousesShipment.Comment = message.Sale.Comment;

        if (message.Sale.DeliveryRecipientAddress != null) {
            warehousesShipment.City = message.Sale.DeliveryRecipientAddress.City;
            warehousesShipment.Department = message.Sale.DeliveryRecipientAddress.Department;
        }

        if (message.Sale.DeliveryRecipient != null) {
            //if (!message.Sale.DeliveryRecipient.IsNew()) {
            warehousesShipment.FullName = message.Sale.DeliveryRecipient.FullName;
            warehousesShipment.MobilePhone = message.Sale.DeliveryRecipient.MobilePhone;
            //}
        }

        if (message.Sale.Transporter != null) warehousesShipment.TransporterId = message.Sale.Transporter.Id;
        warehousesShipment.UserId = updatedB.Id;
        warehousesShipment.SaleId = message.Sale.Id;
        //warehousesShipment.IsDevelopment = false;


        //WarehousesShipment warehousesShipments = warehousesShipmentRepository.Get(message.Sale.Id);

        warehousesShipmentRepository.Update(warehousesShipment);
    }

    private static long SetUpdateDataCarrier(UpdatStateSaleMessage message, IUpdateDataCarrierRepository warehousesShipmentRepository, User updatedB) {
        UpdateDataCarrier updateDataCarrier = new();
        updateDataCarrier.IsEditTransporter = true;
        updateDataCarrier.HasDocument = message.Sale.HasDocuments;
        updateDataCarrier.IsCashOnDelivery = message.Sale.IsCashOnDelivery;
        updateDataCarrier.CashOnDeliveryAmount = message.Sale.CashOnDeliveryAmount;
        updateDataCarrier.ShipmentDate = message.Sale.ShipmentDate;
        updateDataCarrier.TTN = message.Sale.TTN;
        if (message.Sale.CustomersOwnTtn != null) {
            updateDataCarrier.TtnPDFPath = message.Sale.CustomersOwnTtn.TtnPDFPath;
            updateDataCarrier.Number = message.Sale.CustomersOwnTtn.Number;
        }

        if (!string.IsNullOrEmpty(message.Sale.Comment)) updateDataCarrier.Comment = message.Sale.Comment;

        if (message.Sale.DeliveryRecipientAddress != null) {
            updateDataCarrier.City = message.Sale.DeliveryRecipientAddress.City;
            updateDataCarrier.Department = message.Sale.DeliveryRecipientAddress.Department;
        }

        if (message.Sale.DeliveryRecipient != null) {
            //if (!message.Sale.DeliveryRecipient.IsNew()) {
            updateDataCarrier.FullName = message.Sale.DeliveryRecipient.FullName;
            updateDataCarrier.MobilePhone = message.Sale.DeliveryRecipient.MobilePhone;
            //}
        }

        if (message.Sale.Transporter != null) updateDataCarrier.TransporterId = message.Sale.Transporter.Id;
        //if (!message.Sale.UpdateDataCarrier.Any()) {
        //    updateDataCarrier.UserId = message.Sale.User.Id;
        //} else {
        //    updateDataCarrier.UserId = message.Sale.UpdateUserId;
        //}
        updateDataCarrier.UserId = updatedB.Id;
        updateDataCarrier.SaleId = message.Sale.Id;
        updateDataCarrier.IsDevelopment = false;

        return warehousesShipmentRepository.Add(updateDataCarrier);
    }

    private void ProcessStateUpdateSaleMessage(UpdatStateSaleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
        User updatedB = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UpdatedByNetId);
        IUpdateDataCarrierRepository updateDataCarrierRepository = _updateDataCarrierRepositoryFactory.NewUpdateDataCarrierRepository(connection);
        IWarehousesShipmentRepository warehousesShipmentRepository = _saleRepositoriesFactory.NewWarehousesShipmentRepository(connection);

        UpdateDataCarrier updateDataCarrier = new();

        updateDataCarrier.SaleId = message.Sale.Id;
        updateDataCarrier.IsDevelopment = false;
        if (!message.Sale.UpdateDataCarrier.Any()) {
            if (message.Sale.User != null)
                updateDataCarrier.UserId = message.Sale.User.Id;
            else
                updateDataCarrier.UserId = updatedB.Id;
        } else {
            updateDataCarrier.UserId = message.Sale.UpdateUserId;
        }

        if (!message.Sale.IsAcceptedToPacking) {
            message.Sale.UpdateUserId = updatedB.Id;
            saleRepository.UpdateUser(message.Sale);
        }

        if (message.Sale != null && !message.Sale.NetUid.Equals(Guid.Empty)) {
            Sale saleFromDb = saleRepository.GetByNetId(message.Sale.NetUid);

            if (!saleFromDb.IsPrinted) {
                UpdateWarehousesShipment(message, warehousesShipmentRepository, updatedB, message.Sale.Id);
            } else {
                SetUpdateDataCarrier(message, updateDataCarrierRepository, updatedB);
                message.Sale.UpdateUserId = updatedB.Id;
                saleRepository.UpdateUser(message.Sale);
            }

            BaseLifeCycleStatus oldSaleLifeCycleType = new() { SaleLifeCycleType = saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType };
            if (message.Sale.BaseLifeCycleStatus != null) {
                saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType = message.Sale.BaseLifeCycleStatus.SaleLifeCycleType;

                _saleRepositoriesFactory.NewBaseLifeCycleStatusRepository(connection).Update(saleFromDb.BaseLifeCycleStatus);
            }

            if (message.Sale.BaseSalePaymentStatus != null) {
                saleFromDb.BaseSalePaymentStatus.SalePaymentStatusType = message.Sale.BaseSalePaymentStatus.SalePaymentStatusType;
                _saleRepositoriesFactory.NewBaseSalePaymentStatusRepository(connection).Update(saleFromDb.BaseSalePaymentStatus);
            }

            if (message.Sale.Transporter != null) {
                updateDataCarrier.TransporterId = saleFromDb.TransporterId;
                saleFromDb.TransporterId = message.Sale.Transporter.Id;
            }


            if (message.Sale.DeliveryRecipient != null) {
                if (saleFromDb.DeliveryRecipient != null) {
                    updateDataCarrier.FullName = saleFromDb.DeliveryRecipient.FullName;
                    updateDataCarrier.MobilePhone = saleFromDb.DeliveryRecipient.MobilePhone;
                } else {
                    updateDataCarrier.FullName = message.Sale.DeliveryRecipient.FullName;
                    updateDataCarrier.MobilePhone = message.Sale.DeliveryRecipient.MobilePhone;
                }

                if (message.Sale.DeliveryRecipient.IsNew()) {
                    message.Sale.DeliveryRecipient.FullName = message.Sale.DeliveryRecipient.FullName;
                    message.Sale.DeliveryRecipient.MobilePhone = message.Sale.DeliveryRecipient.MobilePhone;

                    saleFromDb.DeliveryRecipientId = _deliveryRepositoriesFactory.NewDeliveryRecipientRepository(connection).Add(message.Sale.DeliveryRecipient);
                } else {
                    _deliveryRepositoriesFactory.NewDeliveryRecipientRepository(connection).Update(message.Sale.DeliveryRecipient);

                    saleFromDb.DeliveryRecipientId = message.Sale.DeliveryRecipient.Id;
                }
            }

            if (message.Sale.DeliveryRecipientAddress != null) {
                if (saleFromDb.DeliveryRecipientAddress != null) {
                    updateDataCarrier.City = saleFromDb.DeliveryRecipientAddress.City;
                    updateDataCarrier.Department = saleFromDb.DeliveryRecipientAddress.Department;
                } else {
                    updateDataCarrier.City = message.Sale.DeliveryRecipientAddress.City;
                    updateDataCarrier.Department = message.Sale.DeliveryRecipientAddress.Department;
                }

                if (message.Sale.DeliveryRecipientAddress.IsNew()) {
                    if (saleFromDb.DeliveryRecipientId.HasValue) {
                        message.Sale.DeliveryRecipientAddress.DeliveryRecipientId = saleFromDb.DeliveryRecipientId ?? 0;
                        message.Sale.DeliveryRecipientAddress.Value = message.Sale.DeliveryRecipientAddress.City + " " + message.Sale.DeliveryRecipientAddress.Department;
                        saleFromDb.DeliveryRecipientAddressId =
                            _deliveryRepositoriesFactory.NewDeliveryRecipientAddressRepository(connection).Add(message.Sale.DeliveryRecipientAddress);
                    }
                } else {
                    _deliveryRepositoriesFactory.NewDeliveryRecipientAddressRepository(connection).Update(message.Sale.DeliveryRecipientAddress);

                    saleFromDb.DeliveryRecipientAddressId = message.Sale.DeliveryRecipientAddress.Id;
                }
            }

            if (!string.IsNullOrEmpty(message.Sale.Comment)) {
                updateDataCarrier.Comment = saleFromDb.Comment;
                saleFromDb.Comment = message.Sale.Comment;
            }

            if (message.Sale.SaleInvoiceDocument != null) {
                if (message.Sale.SaleInvoiceDocument.IsNew()) {
                    saleFromDb.SaleInvoiceDocumentId = _saleRepositoriesFactory.NewSaleInvoiceDocumentRepository(connection).Add(message.Sale.SaleInvoiceDocument);
                } else {
                    _saleRepositoriesFactory.NewSaleInvoiceDocumentRepository(connection).Update(message.Sale.SaleInvoiceDocument);

                    saleFromDb.SaleInvoiceDocumentId = message.Sale.SaleInvoiceDocument.Id;
                }

                BaseLifeCycleStatus oldLifeCycleStatus = saleFromDb.BaseLifeCycleStatus;

                if (saleFromDb.BaseLifeCycleStatus != null) {
                    saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType = SaleLifeCycleType.Packaging;

                    _saleRepositoriesFactory.NewBaseLifeCycleStatusRepository(connection).Update(saleFromDb.BaseLifeCycleStatus);
                } else {
                    saleFromDb.BaseLifeCycleStatusId = _saleRepositoriesFactory.NewBaseLifeCycleStatusRepository(connection).Add(new BaseLifeCycleStatus {
                        SaleLifeCycleType = SaleLifeCycleType.Packaging
                    });
                }

                ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                    new RetrieveAndStoreAuditDataMessage(
                        message.UpdatedByNetId,
                        message.Sale.NetUid,
                        "Sale",
                        saleFromDb.BaseLifeCycleStatus,
                        oldLifeCycleStatus
                    )
                );
            }

            if (message.Sale.Order != null) {
                if (message.Sale.Order.OrderPackages.Any()) {
                    IOrderPackageRepository orderPackageRepository = _saleRepositoriesFactory.NewOrderPackageRepository(connection);
                    IOrderPackageItemRepository orderPackageItemRepository = _saleRepositoriesFactory.NewOrderPackageItemRepository(connection);
                    IOrderPackageUserRepository orderPackageUserRepository = _saleRepositoriesFactory.NewOrderPackageUserRepository(connection);

                    if (message.Sale.Order.OrderPackages.Any(p => !p.IsNew() && !p.Deleted))
                        _saleRepositoriesFactory.NewOrderPackageRepository(connection).RemoveAllByOrderIdExceptProvided(
                            message.Sale.Order.Id,
                            message.Sale.Order.OrderPackages
                                .Where(p => !p.IsNew() && !p.Deleted)
                                .Select(p => p.Id)
                        );
                    else
                        _saleRepositoriesFactory.NewOrderPackageRepository(connection).RemoveAllByOrderId(message.Sale.Order.Id);

                    foreach (OrderPackage package in message.Sale.Order.OrderPackages.Where(p => p.IsNew() && !p.Deleted)) {
                        if (!package.Lenght.Equals(0) && !package.Height.Equals(0) && !package.Width.Equals(0))
                            package.CBM = Math.Round(package.Lenght * package.Height * package.Width * 0.000001, 6);

                        package.OrderId = message.Sale.Order.Id;

                        package.Id = orderPackageRepository.Add(package);

                        if (package.OrderPackageItems.Any(i => i.IsNew() && !i.Deleted && !i.OrderItem.Equals(null)))
                            orderPackageItemRepository.Add(
                                package.OrderPackageItems
                                    .Where(i => i.IsNew() && !i.Deleted && !i.OrderItem.Equals(null))
                                    .Select(i => {
                                        i.OrderItemId = i.OrderItem.Id;
                                        i.OrderPackageId = package.Id;

                                        return i;
                                    })
                            );
                        if (package.OrderPackageUsers.Any(u => u.IsNew() && !u.Deleted && !u.User.Equals(null)))
                            orderPackageUserRepository.Add(
                                package.OrderPackageUsers
                                    .Where(u => u.IsNew() && !u.Deleted && !u.User.Equals(null))
                                    .Select(u => {
                                        u.UserId = u.User.Id;
                                        u.OrderPackageId = package.Id;

                                        return u;
                                    })
                            );
                    }

                    orderPackageRepository.Update(
                        message.Sale.Order.OrderPackages
                            .Where(p => !p.IsNew() && !p.Deleted)
                            .Select(package => {
                                if (!package.Lenght.Equals(0) && !package.Height.Equals(0) && !package.Width.Equals(0))
                                    package.CBM = Math.Round(package.Lenght * package.Height * package.Width * 0.000001, 6);

                                if (package.OrderPackageItems.Any()) {
                                    orderPackageItemRepository.RemoveAllByOrderPackageIdExceptProvided(
                                        package.Id,
                                        package.OrderPackageItems
                                            .Where(i => !i.IsNew() && !i.Deleted)
                                            .Select(i => i.Id)
                                    );

                                    if (package.OrderPackageItems.Any(i => i.IsNew() && !i.Deleted && !i.OrderItem.Equals(null)))
                                        orderPackageItemRepository.Add(
                                            package.OrderPackageItems
                                                .Where(i => i.IsNew() && !i.Deleted && !i.OrderItem.Equals(null))
                                                .Select(i => {
                                                    i.OrderItemId = i.OrderItem.Id;
                                                    i.OrderPackageId = package.Id;

                                                    return i;
                                                })
                                        );
                                } else {
                                    orderPackageItemRepository.RemoveAllByOrderPackageId(package.Id);
                                }

                                if (package.OrderPackageUsers.Any()) {
                                    orderPackageUserRepository.RemoveAllByOrderPackageIdExceptProvided(
                                        package.Id,
                                        package.OrderPackageUsers
                                            .Where(u => u.IsNew() && !u.Deleted)
                                            .Select(u => u.Id)
                                    );

                                    if (package.OrderPackageUsers.Any(u => u.IsNew() && !u.Deleted && !u.User.Equals(null)))
                                        orderPackageUserRepository.Add(
                                            package.OrderPackageUsers
                                                .Where(u => u.IsNew() && !u.Deleted && !u.User.Equals(null))
                                                .Select(u => {
                                                    u.UserId = u.User.Id;
                                                    u.OrderPackageId = package.Id;

                                                    return u;
                                                })
                                        );
                                } else {
                                    orderPackageUserRepository.RemoveAllByOrderPackageId(package.Id);
                                }

                                return package;
                            })
                    );
                } else {
                    _saleRepositoriesFactory.NewOrderPackageRepository(connection).RemoveAllByOrderId(message.Sale.Order.Id);
                }
            }

            if (message.Sale.ShipmentDate.HasValue && message.Sale.ShipmentDate.Value.Year.Equals(1)) message.Sale.ShipmentDate = null;

            if (saleFromDb.CustomersOwnTtn != null) {
                if (message.Sale.CustomersOwnTtnId == 0L) {
                    saleRepository.RemoveCustomersOwnTtn(saleFromDb.CustomersOwnTtn);
                    saleFromDb.CustomersOwnTtnId = null;
                } else {
                    saleRepository.UpdateCustomersOwnTtn(message.Sale.CustomersOwnTtn);

                    updateDataCarrier.TtnPDFPath = saleFromDb.CustomersOwnTtn.TtnPDFPath;

                    updateDataCarrier.Number = saleFromDb.CustomersOwnTtn.Number;

                    saleFromDb.CustomersOwnTtn = message.Sale.CustomersOwnTtn;
                }
            }

            if (message.Sale.CustomersOwnTtn != null && message.Sale.CustomersOwnTtn.IsNew()) {
                saleFromDb.CustomersOwnTtnId = saleRepository.AddCustomersOwnTtn(message.Sale.CustomersOwnTtn);

                //saleFromDb.CustomersOwnTtn = saleRepository.GetCustomersOwnTtnById(saleFromDb.CustomersOwnTtnId.Value);
                if (saleFromDb.CustomersOwnTtn == null) {
                    updateDataCarrier.TtnPDFPath = string.Empty;
                    updateDataCarrier.Number = string.Empty;
                } else {
                    updateDataCarrier.TtnPDFPath = saleFromDb.CustomersOwnTtn.TtnPDFPath;
                    updateDataCarrier.Number = saleFromDb.CustomersOwnTtn.Number;
                }
            }

            updateDataCarrier.HasDocument = saleFromDb.HasDocuments;
            updateDataCarrier.IsCashOnDelivery = saleFromDb.IsCashOnDelivery;
            updateDataCarrier.CashOnDeliveryAmount = saleFromDb.CashOnDeliveryAmount;
            updateDataCarrier.ShipmentDate = saleFromDb.ShipmentDate;
            updateDataCarrier.TTN = saleFromDb.TTN;

            saleFromDb.IsPrinted = message.Sale.IsPrinted;
            saleFromDb.TTN = message.Sale.TTN;
            saleFromDb.ShippingAmount = message.Sale.ShippingAmount;
            saleFromDb.CashOnDeliveryAmount = message.Sale.CashOnDeliveryAmount;
            saleFromDb.IsCashOnDelivery = message.Sale.IsCashOnDelivery;
            saleFromDb.IsPrintedPaymentInvoice = message.Sale.IsPrintedPaymentInvoice;
            saleFromDb.HasDocuments = message.Sale.HasDocuments;
            saleFromDb.ShipmentDate = message.Sale.ShipmentDate;
            if (saleFromDb.Transporter != null) updateDataCarrierRepository.Add(updateDataCarrier);

            if (saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.Packaging) &&
                message.Sale.IsAcceptedToPacking) {
                saleRepository.UpdateIsAcceptedToPacking(message.Sale.Id, true);

                ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                    new RetrieveAndStoreAuditDataMessage(
                        message.UpdatedByNetId,
                        saleFromDb.NetUid,
                        "Sale",
                        saleFromDb.BaseLifeCycleStatus,
                        oldSaleLifeCycleType
                    )
                );
            }

            saleRepository.Update(saleFromDb);

            string saleResourceName = message.Sale.IsEdited
                ? SaleResourceNames.EDITED
                : !oldSaleLifeCycleType.SaleLifeCycleType.Equals(saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType)
                    ? SaleResourceNames.UPDATED_LIFE_CYCLE_STATUS
                    : SaleResourceNames.UPDATED;


            if (saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.Packaging) &&
                !oldSaleLifeCycleType.SaleLifeCycleType.Equals(saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType)) {
                User updatedBy = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UpdatedByNetId);

                saleRepository.SetChangedToInvoiceDateByNetId(saleFromDb.NetUid, updatedBy?.Id);

                if (saleFromDb.RetailClientId != null) {
                    IRetailPaymentStatusRepository retailPaymentStatusRepository = _retailClientRepositoriesFactory.NewRetailPaymentStatusRepository(connection);

                    RetailPaymentStatus retailPaymentStatus = retailPaymentStatusRepository.GetBySaleId(saleFromDb.Id);
                    retailPaymentStatusRepository.SetRetailPaymentStatusTypeById(RetailPaymentStatusType.ChangedToInvoice, retailPaymentStatus.Id);
                }

                saleFromDb = saleRepository.GetByNetId(saleFromDb.NetUid);

                SaveExchangeRates(_saleRepositoriesFactory, _exchangeRateRepositoriesFactory, connection, saleFromDb.Id);

                SavePricesPerItem(
                    _saleRepositoriesFactory,
                    _exchangeRateRepositoriesFactory,
                    _productRepositoriesFactory.NewProductGroupDiscountRepository(connection),
                    connection,
                    saleFromDb
                );

                SetDebtToClient(
                    connection,
                    _exchangeRateRepositoriesFactory,
                    _clientRepositoriesFactory.NewClientAgreementRepository(connection),
                    _clientRepositoriesFactory.NewClientInDebtRepository(connection),
                    _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection),
                    _currencyRepositoriesFactory.NewCurrencyRepository(connection),
                    _saleRepositoriesFactory,
                    saleFromDb
                );

                ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR)
                    .Tell(new StoreConsignmentMovementFromSaleMessage(saleFromDb.Id, Sender, saleResourceName));

                _clientRepositoriesFactory.NewClientRegistrationTaskRepository(connection).SetDoneByClientId(saleFromDb.ClientAgreement.ClientId);

                _clientRepositoriesFactory.NewClientRepository(connection).SetTemporaryClientById(saleFromDb.ClientAgreement.ClientId);
            } else {
                ActorReferenceManager.Instance.Get(SalesActorNames.BASE_SALES_GET_ACTOR)
                    .Forward(
                        new GetSaleStatisticWithResourceNameByNetIdMessage(
                            saleFromDb.NetUid,
                            saleResourceName,
                            false,
                            false,
                            true
                        )
                    );

                saleFromDb = saleRepository.GetByNetId(saleFromDb.NetUid);
            }

            if (!oldSaleLifeCycleType.SaleLifeCycleType.Equals(saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType))
                ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                    new RetrieveAndStoreAuditDataMessage(
                        message.UpdatedByNetId,
                        saleFromDb.NetUid,
                        "Sale",
                        saleFromDb.BaseLifeCycleStatus,
                        oldSaleLifeCycleType
                    )
                );
        } else {
            Sender.Tell(null);
        }
    }

    private void ProcessUpdateSaleFromECommerceMessage(UpdateSaleFromEcommerceMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

        if (message.Sale != null && !message.Sale.NetUid.Equals(Guid.Empty)) {
            Sale saleFromDb = saleRepository.GetByNetId(message.Sale.NetUid);
            BaseLifeCycleStatus oldSaleLifeCycleType = new() { SaleLifeCycleType = saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType };

            if (message.Sale.BaseLifeCycleStatus != null) {
                saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType = message.Sale.BaseLifeCycleStatus.SaleLifeCycleType;
                _saleRepositoriesFactory.NewBaseLifeCycleStatusRepository(connection).Update(saleFromDb.BaseLifeCycleStatus);
            }

            if (message.Sale.BaseSalePaymentStatus != null) {
                saleFromDb.BaseSalePaymentStatus.SalePaymentStatusType = message.Sale.BaseSalePaymentStatus.SalePaymentStatusType;
                _saleRepositoriesFactory.NewBaseSalePaymentStatusRepository(connection).Update(saleFromDb.BaseSalePaymentStatus);
            }

            if (message.Sale.Transporter != null) saleFromDb.TransporterId = message.Sale.Transporter.Id;

            if (message.Sale.DeliveryRecipient != null) {
                if (!string.IsNullOrEmpty(message.Sale.DeliveryRecipient.MobilePhone)) {
                    if (message.Sale.DeliveryRecipient.IsNew()) {
                        message.Sale.DeliveryRecipient.ClientId = saleFromDb.ClientAgreement.ClientId;

                        message.Sale.DeliveryRecipient.Id =
                            _deliveryRepositoriesFactory.NewDeliveryRecipientRepository(connection).Add(message.Sale.DeliveryRecipient);
                    } else {
                        _deliveryRepositoriesFactory.NewDeliveryRecipientRepository(connection).Update(message.Sale.DeliveryRecipient);
                    }
                }

                saleFromDb.DeliveryRecipientId = message.Sale.DeliveryRecipient.Id;
            }

            if (message.Sale.DeliveryRecipientAddress != null) {
                if (message.Sale.DeliveryRecipientAddress.IsNew()) {
                    if (saleFromDb.DeliveryRecipientId.HasValue) {
                        message.Sale.DeliveryRecipientAddress.DeliveryRecipientId = saleFromDb.DeliveryRecipientId ?? 0;

                        saleFromDb.DeliveryRecipientAddressId =
                            _deliveryRepositoriesFactory.NewDeliveryRecipientAddressRepository(connection).Add(message.Sale.DeliveryRecipientAddress);
                    }
                } else {
                    saleFromDb.DeliveryRecipientAddressId = message.Sale.DeliveryRecipientAddress.Id;
                }
            }

            if (!string.IsNullOrEmpty(message.Sale.Comment)) saleFromDb.Comment = message.Sale.Comment;

            if (message.Sale.SaleInvoiceDocument != null) {
                if (message.Sale.SaleInvoiceDocument.IsNew()) {
                    saleFromDb.SaleInvoiceDocumentId = _saleRepositoriesFactory.NewSaleInvoiceDocumentRepository(connection).Add(message.Sale.SaleInvoiceDocument);
                } else {
                    _saleRepositoriesFactory.NewSaleInvoiceDocumentRepository(connection).Update(message.Sale.SaleInvoiceDocument);

                    saleFromDb.SaleInvoiceDocumentId = message.Sale.SaleInvoiceDocument.Id;
                }

                if (saleFromDb.BaseLifeCycleStatus != null) {
                    saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType = SaleLifeCycleType.Packaging;

                    _saleRepositoriesFactory.NewBaseLifeCycleStatusRepository(connection).Update(saleFromDb.BaseLifeCycleStatus);
                } else {
                    saleFromDb.BaseLifeCycleStatusId = _saleRepositoriesFactory.NewBaseLifeCycleStatusRepository(connection).Add(new BaseLifeCycleStatus {
                        SaleLifeCycleType = SaleLifeCycleType.Packaging
                    });
                }
            }

            if (message.Sale.Order != null) {
                if (message.Sale.Order.OrderPackages.Any()) {
                    IOrderPackageRepository orderPackageRepository = _saleRepositoriesFactory.NewOrderPackageRepository(connection);
                    IOrderPackageItemRepository orderPackageItemRepository = _saleRepositoriesFactory.NewOrderPackageItemRepository(connection);
                    IOrderPackageUserRepository orderPackageUserRepository = _saleRepositoriesFactory.NewOrderPackageUserRepository(connection);

                    if (message.Sale.Order.OrderPackages.Any(p => !p.IsNew() && !p.Deleted))
                        _saleRepositoriesFactory.NewOrderPackageRepository(connection).RemoveAllByOrderIdExceptProvided(
                            message.Sale.Order.Id,
                            message.Sale.Order.OrderPackages
                                .Where(p => !p.IsNew() && !p.Deleted)
                                .Select(p => p.Id)
                        );
                    else
                        _saleRepositoriesFactory.NewOrderPackageRepository(connection).RemoveAllByOrderId(message.Sale.Order.Id);

                    foreach (OrderPackage package in message.Sale.Order.OrderPackages.Where(p => p.IsNew() && !p.Deleted)) {
                        if (!package.Lenght.Equals(0) && !package.Height.Equals(0) && !package.Width.Equals(0))
                            package.CBM = Math.Round(package.Lenght * package.Height * package.Width * 0.000001, 6);

                        package.OrderId = message.Sale.Order.Id;

                        package.Id = orderPackageRepository.Add(package);

                        if (package.OrderPackageItems.Any(i => i.IsNew() && !i.Deleted && !i.OrderItem.Equals(null)))
                            orderPackageItemRepository.Add(
                                package.OrderPackageItems
                                    .Where(i => i.IsNew() && !i.Deleted && !i.OrderItem.Equals(null))
                                    .Select(i => {
                                        i.OrderItemId = i.OrderItem.Id;
                                        i.OrderPackageId = package.Id;

                                        return i;
                                    })
                            );
                        if (package.OrderPackageUsers.Any(u => u.IsNew() && !u.Deleted && !u.User.Equals(null)))
                            orderPackageUserRepository.Add(
                                package.OrderPackageUsers
                                    .Where(u => u.IsNew() && !u.Deleted && !u.User.Equals(null))
                                    .Select(u => {
                                        u.UserId = u.User.Id;
                                        u.OrderPackageId = package.Id;

                                        return u;
                                    })
                            );
                    }

                    orderPackageRepository.Update(
                        message.Sale.Order.OrderPackages
                            .Where(p => !p.IsNew() && !p.Deleted)
                            .Select(package => {
                                if (!package.Lenght.Equals(0) && !package.Height.Equals(0) && !package.Width.Equals(0))
                                    package.CBM = Math.Round(package.Lenght * package.Height * package.Width * 0.000001, 6);

                                if (package.OrderPackageItems.Any()) {
                                    orderPackageItemRepository.RemoveAllByOrderPackageIdExceptProvided(
                                        package.Id,
                                        package.OrderPackageItems
                                            .Where(i => !i.IsNew() && !i.Deleted)
                                            .Select(i => i.Id)
                                    );

                                    if (package.OrderPackageItems.Any(i => i.IsNew() && !i.Deleted && !i.OrderItem.Equals(null)))
                                        orderPackageItemRepository.Add(
                                            package.OrderPackageItems
                                                .Where(i => i.IsNew() && !i.Deleted && !i.OrderItem.Equals(null))
                                                .Select(i => {
                                                    i.OrderItemId = i.OrderItem.Id;
                                                    i.OrderPackageId = package.Id;

                                                    return i;
                                                })
                                        );
                                } else {
                                    orderPackageItemRepository.RemoveAllByOrderPackageId(package.Id);
                                }

                                if (package.OrderPackageUsers.Any()) {
                                    orderPackageUserRepository.RemoveAllByOrderPackageIdExceptProvided(
                                        package.Id,
                                        package.OrderPackageUsers
                                            .Where(u => u.IsNew() && !u.Deleted)
                                            .Select(u => u.Id)
                                    );

                                    if (package.OrderPackageUsers.Any(u => u.IsNew() && !u.Deleted && !u.User.Equals(null)))
                                        orderPackageUserRepository.Add(
                                            package.OrderPackageUsers
                                                .Where(u => u.IsNew() && !u.Deleted && !u.User.Equals(null))
                                                .Select(u => {
                                                    u.UserId = u.User.Id;
                                                    u.OrderPackageId = package.Id;

                                                    return u;
                                                })
                                        );
                                } else {
                                    orderPackageUserRepository.RemoveAllByOrderPackageId(package.Id);
                                }

                                return package;
                            })
                    );
                } else {
                    _saleRepositoriesFactory.NewOrderPackageRepository(connection).RemoveAllByOrderId(message.Sale.Order.Id);
                }
            }

            _saleRepositoriesFactory.NewSaleRepository(connection).Update(saleFromDb);

            string saleResourceName = SaleResourceNames.UPDATED;

            if (!oldSaleLifeCycleType.SaleLifeCycleType.Equals(saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType))
                saleResourceName = SaleResourceNames.UPDATED_LIFE_CYCLE_STATUS;

            if (saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.Packaging) &&
                !oldSaleLifeCycleType.SaleLifeCycleType.Equals(saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType)) {
                saleRepository.SetChangedToInvoiceDateByNetId(saleFromDb.NetUid, null);

                ActorReferenceManager.Instance.Get(SalesActorNames.BASE_SALES_GET_ACTOR)
                    .Forward(
                        new GetSaleStatisticWithResourceNameByNetIdMessage(
                            saleFromDb.NetUid,
                            saleResourceName,
                            true,
                            false,
                            true
                        )
                    );

                saleFromDb = saleRepository.GetByNetId(saleFromDb.NetUid);

                SaveExchangeRates(_saleRepositoriesFactory, _exchangeRateRepositoriesFactory, connection, saleFromDb.Id);

                SavePricesPerItem(
                    _saleRepositoriesFactory,
                    _exchangeRateRepositoriesFactory,
                    _productRepositoriesFactory.NewProductGroupDiscountRepository(connection),
                    connection,
                    saleFromDb
                );

                SetDebtToClient(
                    connection,
                    _exchangeRateRepositoriesFactory,
                    _clientRepositoriesFactory.NewClientAgreementRepository(connection),
                    _clientRepositoriesFactory.NewClientInDebtRepository(connection),
                    _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection),
                    _currencyRepositoriesFactory.NewCurrencyRepository(connection),
                    _saleRepositoriesFactory,
                    saleFromDb
                );

                _clientRepositoriesFactory.NewClientRegistrationTaskRepository(connection).SetDoneByClientId(saleFromDb.ClientAgreement.ClientId);

                _clientRepositoriesFactory.NewClientRepository(connection).SetTemporaryClientById(saleFromDb.ClientAgreement.ClientId);
            } else {
                ActorReferenceManager.Instance.Get(SalesActorNames.BASE_SALES_GET_ACTOR)
                    .Forward(new GetSaleStatisticWithResourceNameByNetIdMessage(saleFromDb.NetUid, saleResourceName));
            }
        } else {
            Sender.Tell(null);
        }
    }

    private void ProcessAddSaleFutureReservationMessage(AddSaleFutureReservationMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleFutureReservationRepository saleFutureReservationRepository = _saleRepositoriesFactory.NewSaleFutureReservationRepository(connection);

        Sender.Tell(
            saleFutureReservationRepository.GetById(
                saleFutureReservationRepository.Add(message)
            )
        );
    }

    private void ProcessDeleteSaleFutureReservationByNetIdMessage(DeleteSaleFutureReservationByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _saleRepositoriesFactory.NewSaleFutureReservationRepository(connection).Delete(message.NetId);
    }

    private void ProcessDeleteSaleMessage(DeleteSaleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _saleRepositoriesFactory.NewSaleRepository(connection).Remove(message.NetId);
    }

    private void ProcessUpdateMergedSaleToBillMessage(UpdateMergedSaleToBillMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            Sale saleFromDb = saleRepository.GetByNetIdWithSaleMerged(message.Sale.NetUid);

            Sale newSale = new() {
                ClientAgreement = saleFromDb.ClientAgreement,
                Order = new Order {
                    OrderSource = OrderSource.Local
                },
                TransporterId = message.Sale?.Transporter?.Id,
                Comment = message.Sale.Comment
            };

            if (message.Sale.Transporter != null) newSale.TransporterId = message.Sale.Transporter.Id;

            if (message.Sale.DeliveryRecipient != null) {
                if (message.Sale.DeliveryRecipient.IsNew()) {
                    newSale.DeliveryRecipientId = _deliveryRepositoriesFactory.NewDeliveryRecipientRepository(connection).Add(message.Sale.DeliveryRecipient);
                } else {
                    _deliveryRepositoriesFactory.NewDeliveryRecipientRepository(connection).Update(message.Sale.DeliveryRecipient);

                    newSale.DeliveryRecipientId = message.Sale.DeliveryRecipient.Id;
                }
            }

            if (message.Sale.DeliveryRecipientAddress != null) {
                if (message.Sale.DeliveryRecipientAddress.IsNew()) {
                    if (newSale.DeliveryRecipientId.HasValue) {
                        message.Sale.DeliveryRecipientAddress.DeliveryRecipientId = newSale.DeliveryRecipientId.Value;

                        newSale.DeliveryRecipientAddressId =
                            _deliveryRepositoriesFactory.NewDeliveryRecipientAddressRepository(connection).Add(message.Sale.DeliveryRecipientAddress);
                    }
                } else {
                    _deliveryRepositoriesFactory.NewDeliveryRecipientAddressRepository(connection).Update(message.Sale.DeliveryRecipientAddress);

                    newSale.DeliveryRecipientAddressId = message.Sale.DeliveryRecipientAddress.Id;
                }
            }

            List<OrderItem> orderItemsToDelete = new();
            List<OrderItem> orderItemsToUpdate = new();
            List<ProductReservation> productReservationsToUpdate = new();
            List<ProductAvailability> productAvailabilitiesToUpdate = new();

            foreach (OrderItem orderItem in message.Sale.Order.OrderItems) {
                if (saleFromDb.Order.OrderItems.Any(o => o.ProductId.Equals(orderItem.ProductId))) {
                    OrderItem orderItemFromDb = saleFromDb.Order.OrderItems.First(o => o.ProductId.Equals(orderItem.ProductId));

                    if (orderItemFromDb.Qty.Equals(orderItem.Qty)) {
                        orderItemsToDelete.Add(orderItemFromDb);
                    } else if (orderItemFromDb.Qty < orderItem.Qty) {
                        orderItemsToDelete.Add(orderItemFromDb);

                        IEnumerable<ProductAvailability> productAvailabilities =
                            productAvailabilityRepository
                                .GetByProductAndOrganizationIds(
                                    orderItem.ProductId,
                                    (long)saleFromDb.ClientAgreement.Agreement.OrganizationId,
                                    saleFromDb.ClientAgreement.Agreement.WithVATAccounting,
                                    true,
                                    saleFromDb.ClientAgreement.Agreement.Organization.StorageId ?? null
                                );

                        double toDecreaseQty = orderItem.Qty - orderItemFromDb.Qty;

                        if (productAvailabilities.Sum(a => a.Amount) < toDecreaseQty)
                            toDecreaseQty = productAvailabilities.Sum(a => a.Amount);

                        foreach (ProductAvailability productAvailability in productAvailabilities.Where(a => a.Amount > 0)) {
                            if (toDecreaseQty.Equals(0d)) break;

                            if (productAvailability.Amount >= toDecreaseQty) {
                                productAvailability.Amount -= toDecreaseQty;

                                toDecreaseQty = 0d;
                            } else {
                                toDecreaseQty -= productAvailability.Amount;

                                productAvailability.Amount = 0d;
                            }

                            productAvailabilityRepository.Update(productAvailability);
                        }
                    } else {
                        orderItemFromDb.Qty -= orderItem.Qty;

                        orderItemsToUpdate.Add(orderItemFromDb);

                        IEnumerable<ProductReservation> reservations =
                            productReservationRepository
                                .GetAllByOrderItemIdWithAvailability(
                                    orderItemFromDb.Id
                                );

                        double toRestoreAmount = orderItem.Qty;

                        foreach (ProductReservation reservation in reservations) {
                            if (toRestoreAmount.Equals(0d)) break;

                            if (reservation.Qty >= toRestoreAmount) {
                                reservation.Qty -= toRestoreAmount;

                                toRestoreAmount = 0d;

                                if (reservation.Qty > 0)
                                    productReservationRepository.Update(reservation);
                                else
                                    productReservationRepository.Delete(reservation.NetUid);
                            } else {
                                toRestoreAmount -= reservation.Qty;

                                productReservationRepository.Delete(reservation.NetUid);
                            }
                        }
                    }
                }

                newSale.Order.OrderItems.Add(new OrderItem {
                    Comment = orderItem.Comment,
                    IsValidForCurrentSale = orderItem.IsValidForCurrentSale,
                    ProductId = orderItem.ProductId,
                    Qty = orderItem.Qty,
                    UserId = orderItem.UserId,
                    Vat = orderItem.Vat
                });
            }

            orderItemRepository.Update(orderItemsToUpdate);
            orderItemRepository.Remove(orderItemsToDelete);

            productReservationRepository.Update(productReservationsToUpdate);

            productAvailabilityRepository.Update(productAvailabilitiesToUpdate);

            newSale = CreateNewBill(
                newSale,
                message.UserNetId,
                _saleRepositoriesFactory,
                _userRepositoriesFactory,
                connection,
                saleRepository);

            ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                new RetrieveAndStoreAuditDataMessage(
                    message.UserNetId,
                    newSale.NetUid,
                    "Sale",
                    newSale.BaseLifeCycleStatus
                )
            );

            BaseLifeCycleStatus oldStatus = new() { SaleLifeCycleType = newSale.BaseLifeCycleStatus.SaleLifeCycleType };

            newSale.BaseLifeCycleStatus.SaleLifeCycleType = SaleLifeCycleType.Packaging;
            newSale.ChangedToInvoice = DateTime.UtcNow;

            _saleRepositoriesFactory.NewBaseLifeCycleStatusRepository(connection).Update(newSale.BaseLifeCycleStatus);

            ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                new RetrieveAndStoreAuditDataMessage(
                    message.UserNetId,
                    newSale.NetUid,
                    "Sale",
                    newSale.BaseLifeCycleStatus,
                    oldStatus
                )
            );

            if (saleFromDb.OutputSaleMerges.Any()) {
                SaleMerged saleMerged = saleFromDb.OutputSaleMerges.First();

                _saleRepositoriesFactory.NewSaleMergedRepository(connection).Remove(saleMerged.NetUid);

                Sale outputMergedSale = saleRepository.GetByIdWithSaleMerged(saleMerged.OutputSaleId);

                if (!outputMergedSale.InputSaleMerges.Any()) saleRepository.Remove(outputMergedSale.NetUid);
            }

            ActorReferenceManager.Instance.Get(SalesActorNames.GET_SALE_BY_NET_ID_ACTOR).Forward(new GetSaleByNetIdMessage(newSale.NetUid));

            newSale = saleRepository.GetByNetId(newSale.NetUid);

            SetDebtToClient(
                connection,
                _exchangeRateRepositoriesFactory,
                _clientRepositoriesFactory.NewClientAgreementRepository(connection),
                _clientRepositoriesFactory.NewClientInDebtRepository(connection),
                _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection),
                _currencyRepositoriesFactory.NewCurrencyRepository(connection),
                _saleRepositoriesFactory,
                newSale
            );

            SaveExchangeRates(_saleRepositoriesFactory, _exchangeRateRepositoriesFactory, connection, newSale.Id);

            SavePricesPerItem(
                _saleRepositoriesFactory,
                _exchangeRateRepositoriesFactory,
                _productRepositoriesFactory.NewProductGroupDiscountRepository(connection),
                connection,
                newSale
            );

            _clientRepositoriesFactory.NewClientRegistrationTaskRepository(connection).SetDoneByClientId(saleFromDb.ClientAgreement.ClientId);

            _clientRepositoriesFactory.NewClientRepository(connection).SetTemporaryClientById(saleFromDb.ClientAgreement.ClientId);

            if (!saleFromDb.OutputSaleMerges.Any()) return;

            Sale outputSale = saleRepository.GetById(saleFromDb.OutputSaleMerges.First().OutputSaleId, true);

            orderItemsToDelete = new List<OrderItem>();
            orderItemsToUpdate = new List<OrderItem>();

            foreach (OrderItem orderItem in message.Sale.Order.OrderItems)
                if (outputSale.Order.OrderItems.Any(o => o.ProductId.Equals(orderItem.ProductId))) {
                    OrderItem orderItemFromDb = outputSale.Order.OrderItems.First(o => o.ProductId.Equals(orderItem.ProductId));

                    if (orderItemFromDb.Qty.Equals(orderItem.Qty) || orderItemFromDb.Qty < orderItem.Qty) {
                        orderItemsToDelete.Add(orderItemFromDb);
                    } else {
                        orderItemFromDb.Qty -= orderItem.Qty;

                        orderItemsToUpdate.Add(orderItemFromDb);

                        IEnumerable<ProductReservation> reservations =
                            productReservationRepository
                                .GetAllByOrderItemIdWithAvailability(
                                    orderItemFromDb.Id
                                );

                        double toRestoreAmount = orderItem.Qty;

                        foreach (ProductReservation reservation in reservations) {
                            if (toRestoreAmount.Equals(0d)) break;

                            if (reservation.Qty >= toRestoreAmount) {
                                reservation.Qty -= toRestoreAmount;

                                toRestoreAmount = 0d;

                                if (reservation.Qty > 0)
                                    productReservationRepository.Update(reservation);
                                else
                                    productReservationRepository.Delete(reservation.NetUid);
                            } else {
                                toRestoreAmount -= reservation.Qty;

                                productReservationRepository.Delete(reservation.NetUid);
                            }
                        }
                    }
                }

            orderItemRepository.Update(orderItemsToUpdate);
            orderItemRepository.Remove(orderItemsToDelete);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdateOneTimeDiscountsOnSaleMessage(UpdateOneTimeDiscountsOnSaleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.Sale != null) {
            if (!message.Sale.NetUid.Equals(Guid.Empty)) {
                ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

                Sale sale = saleRepository.GetByNetId(message.Sale.NetUid);

                if (sale != null) {
                    IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
                    IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);

                    User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                    sale.OneTimeDiscountComment = message.Sale.OneTimeDiscountComment;

                    saleRepository.UpdateDiscountComment(sale);

                    switch (sale.BaseLifeCycleStatus.SaleLifeCycleType) {
                        case SaleLifeCycleType.New: {
                            foreach (OrderItem orderItem in sale.Order.OrderItems) {
                                OrderItem receivedOrderItem = message.Sale.Order.OrderItems.FirstOrDefault(i => i.Id.Equals(orderItem.Id));

                                if (receivedOrderItem == null) continue;

                                if (!receivedOrderItem.OneTimeDiscount.Equals(orderItem.OneTimeDiscount)) {
                                    receivedOrderItem.DiscountUpdatedById = user.Id;

                                    orderItem.DiscountUpdatedById = user.Id;

                                    orderItemRepository.UpdateOneTimeDiscount(receivedOrderItem);
                                } else {
                                    orderItemRepository.UpdateOneTimeDiscountComment(receivedOrderItem);
                                }

                                orderItem.OneTimeDiscount = receivedOrderItem.OneTimeDiscount;
                            }

                            sale = saleRepository.GetByNetId(message.Sale.NetUid);

                            SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPrices(
                                sale,
                                exchangeRateRepository,
                                _currencyRepositoriesFactory.NewCurrencyRepository(connection)
                            );

                            Sender.Tell(new Tuple<Sale, string>(sale, string.Empty));
                            break;
                        }
                        case SaleLifeCycleType.Packaging: {
                            IDebtRepository debtRepository = _saleRepositoriesFactory.NewDebtRepository(connection);
                            IClientInDebtRepository clientInDebtRepository = _clientRepositoriesFactory.NewClientInDebtRepository(connection);
                            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);
                            IBaseSalePaymentStatusRepository salePaymentStatusRepository = _saleRepositoriesFactory.NewBaseSalePaymentStatusRepository(connection);

                            ExchangeRate euroExchangeRate = exchangeRateRepository.GetEuroExchangeRateByCurrentCulture();

                            sale = saleRepository.GetByNetId(message.Sale.NetUid);

                            ClientAgreement clientAgreement = clientAgreementRepository.GetById(sale.ClientAgreementId);
                            ClientInDebt clientInDebt = clientInDebtRepository.GetBySaleAndAgreementIdWithDeleted(sale.Id, sale.ClientAgreement.AgreementId);

                            SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPrices(sale, exchangeRateRepository,
                                _currencyRepositoriesFactory.NewCurrencyRepository(connection));

                            decimal totalAmount = sale.TotalAmount;
                            decimal totalAmountLocal = sale.Order.OrderItems.Sum(o => o.TotalAmountLocal);

                            foreach (OrderItem orderItem in sale.Order.OrderItems) {
                                OrderItem receivedOrderItem = message.Sale.Order.OrderItems.FirstOrDefault(i => i.Id.Equals(orderItem.Id));

                                if (receivedOrderItem != null) {
                                    if (!receivedOrderItem.OneTimeDiscount.Equals(orderItem.OneTimeDiscount)) {
                                        receivedOrderItem.DiscountUpdatedById = user.Id;

                                        orderItem.DiscountUpdatedById = user.Id;

                                        orderItemRepository.UpdateOneTimeDiscount(receivedOrderItem);
                                    } else {
                                        orderItemRepository.UpdateOneTimeDiscountComment(receivedOrderItem);
                                    }

                                    orderItem.OneTimeDiscount = receivedOrderItem.OneTimeDiscount;
                                }
                            }

                            if (sale.BaseSalePaymentStatus.SalePaymentStatusType.Equals(SalePaymentStatusType.NotPaid)) {
                                if (clientInDebt != null) {
                                    clientInDebt.Debt.Total = clientAgreement.Agreement.Currency.Code.ToLower().Equals("eur")
                                        ? sale.TotalAmount
                                        : sale.Order.OrderItems.Sum(o => o.TotalAmountLocal);

                                    debtRepository.Update(clientInDebt.Debt);
                                }
                            } else {
                                IIncomePaymentOrderRepository incomePaymentOrderRepository = _paymentOrderRepositoriesFactory.NewIncomePaymentOrderRepository(connection);
                                IIncomePaymentOrderSaleRepository incomePaymentOrderSaleRepository =
                                    _paymentOrderRepositoriesFactory.NewIncomePaymentOrderSaleRepository(connection);

                                IncomePaymentOrder lastPayment =
                                    incomePaymentOrderRepository
                                        .GetLastBySaleId(sale.Id);

                                bool increased = totalAmount < sale.TotalAmount;

                                if (lastPayment != null) {
                                    //Paid from income (and balance can be)
                                    if (increased)
                                        switch (sale.BaseSalePaymentStatus.SalePaymentStatusType) {
                                            case SalePaymentStatusType.PartialPaid: {
                                                if (clientInDebt != null) {
                                                    if (clientAgreement.Agreement.Currency.Code.ToLower().Equals("eur"))
                                                        clientInDebt.Debt.Total =
                                                            decimal.Round(
                                                                clientInDebt.Debt.Total + (sale.TotalAmount - totalAmount),
                                                                2,
                                                                MidpointRounding.AwayFromZero
                                                            );
                                                    else
                                                        clientInDebt.Debt.Total =
                                                            decimal.Round(
                                                                clientInDebt.Debt.Total + (sale.TotalAmountLocal - totalAmountLocal),
                                                                4,
                                                                MidpointRounding.AwayFromZero
                                                            );

                                                    debtRepository.Update(clientInDebt.Debt);
                                                }

                                                break;
                                            }
                                            case SalePaymentStatusType.Paid: {
                                                if (clientInDebt != null) {
                                                    if (clientAgreement.CurrentAmount > 0) {
                                                        if (clientAgreement.Agreement.Currency.Code.ToLower().Equals("eur")) {
                                                            decimal differenceAmount =
                                                                decimal.Round(sale.TotalAmount - totalAmount, 14, MidpointRounding.AwayFromZero);

                                                            if (clientAgreement.CurrentAmount >= differenceAmount) {
                                                                _clientRepositoriesFactory
                                                                    .NewClientBalanceMovementRepository(connection)
                                                                    .AddOutMovement(
                                                                        new ClientBalanceMovement {
                                                                            ClientAgreementId = clientAgreement.Id,
                                                                            Amount = differenceAmount,
                                                                            ExchangeRateAmount = 1m
                                                                        }
                                                                    );

                                                                clientAgreement.CurrentAmount =
                                                                    decimal.Round(
                                                                        clientAgreement.CurrentAmount - differenceAmount,
                                                                        2,
                                                                        MidpointRounding.AwayFromZero
                                                                    );

                                                                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);
                                                            } else {
                                                                _clientRepositoriesFactory
                                                                    .NewClientBalanceMovementRepository(connection)
                                                                    .AddOutMovement(
                                                                        new ClientBalanceMovement {
                                                                            ClientAgreementId = clientAgreement.Id,
                                                                            Amount = clientAgreement.CurrentAmount,
                                                                            ExchangeRateAmount = 1m
                                                                        }
                                                                    );

                                                                differenceAmount =
                                                                    decimal.Round(
                                                                        differenceAmount - clientAgreement.CurrentAmount,
                                                                        2,
                                                                        MidpointRounding.AwayFromZero
                                                                    );

                                                                clientAgreement.CurrentAmount = decimal.Zero;

                                                                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                                                                salePaymentStatusRepository
                                                                    .SetSalePaymentStatusTypeById(
                                                                        SalePaymentStatusType.PartialPaid,
                                                                        sale.BaseSalePaymentStatusId
                                                                    );

                                                                clientInDebtRepository.Restore(clientInDebt.NetUid);

                                                                clientInDebt.Debt.Total = differenceAmount;

                                                                debtRepository.Update(clientInDebt.Debt);
                                                            }
                                                        } else {
                                                            decimal differenceAmount =
                                                                decimal.Round(
                                                                    sale.TotalAmountLocal - totalAmountLocal,
                                                                    4,
                                                                    MidpointRounding.AwayFromZero
                                                                );

                                                            decimal currentAmount = clientAgreement.CurrentAmount * euroExchangeRate.Amount;

                                                            if (currentAmount >= differenceAmount) {
                                                                decimal euroDifferenceAmount =
                                                                    decimal.Round(
                                                                        differenceAmount / euroExchangeRate.Amount,
                                                                        2,
                                                                        MidpointRounding.AwayFromZero
                                                                    );

                                                                _clientRepositoriesFactory
                                                                    .NewClientBalanceMovementRepository(connection)
                                                                    .AddOutMovement(
                                                                        new ClientBalanceMovement {
                                                                            ClientAgreementId = clientAgreement.Id,
                                                                            Amount = euroDifferenceAmount,
                                                                            ExchangeRateAmount = euroExchangeRate.Amount
                                                                        }
                                                                    );

                                                                clientAgreement.CurrentAmount =
                                                                    decimal.Round(
                                                                        clientAgreement.CurrentAmount - euroDifferenceAmount,
                                                                        2,
                                                                        MidpointRounding.AwayFromZero
                                                                    );

                                                                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);
                                                            } else {
                                                                _clientRepositoriesFactory
                                                                    .NewClientBalanceMovementRepository(connection)
                                                                    .AddOutMovement(
                                                                        new ClientBalanceMovement {
                                                                            ClientAgreementId = clientAgreement.Id,
                                                                            Amount = clientAgreement.CurrentAmount,
                                                                            ExchangeRateAmount = euroExchangeRate.Amount
                                                                        }
                                                                    );

                                                                differenceAmount =
                                                                    decimal.Round(differenceAmount - currentAmount, 14, MidpointRounding.AwayFromZero);

                                                                clientAgreement.CurrentAmount = decimal.Zero;

                                                                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                                                                salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.PartialPaid,
                                                                    sale.BaseSalePaymentStatusId);

                                                                clientInDebtRepository.Restore(clientInDebt.NetUid);

                                                                clientInDebt.Debt.Total = differenceAmount;

                                                                debtRepository.Update(clientInDebt.Debt);
                                                            }
                                                        }
                                                    } else {
                                                        clientInDebtRepository.Restore(clientInDebt.NetUid);

                                                        clientInDebt.Debt.Total = clientAgreement.Agreement.Currency.Code.ToLower().Equals("eur")
                                                            ? decimal.Round(sale.TotalAmount - totalAmount, 14, MidpointRounding.AwayFromZero)
                                                            : decimal.Round(sale.TotalAmountLocal - totalAmountLocal, 14, MidpointRounding.AwayFromZero);

                                                        debtRepository.Update(clientInDebt.Debt);
                                                    }
                                                }

                                                break;
                                            }
                                            case SalePaymentStatusType.Overpaid when clientAgreement.Agreement.Currency.Code.ToLower().Equals("eur"): {
                                                decimal differenceAmount =
                                                    decimal.Round(sale.TotalAmount - totalAmount, 14, MidpointRounding.AwayFromZero);

                                                if (lastPayment.OverpaidAmount > differenceAmount) {
                                                    lastPayment.OverpaidAmount = decimal.Round(lastPayment.OverpaidAmount - differenceAmount, 14,
                                                        MidpointRounding.AwayFromZero);

                                                    incomePaymentOrderRepository.UpdateOverpaidAmountById(lastPayment.Id, lastPayment.OverpaidAmount);

                                                    IncomePaymentOrderSale lastSalePayment = lastPayment.IncomePaymentOrderSales.First();

                                                    lastSalePayment.Amount =
                                                        decimal.Round(lastSalePayment.Amount + differenceAmount, 14, MidpointRounding.AwayFromZero);

                                                    _paymentOrderRepositoriesFactory
                                                        .NewIncomePaymentOrderSaleRepository(connection)
                                                        .UpdateAmount(lastSalePayment);
                                                } else if (lastPayment.OverpaidAmount.Equals(differenceAmount)) {
                                                    salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Paid, sale.BaseSalePaymentStatusId);
                                                    saleRepository.UpdateIsAcceptedToPacking(sale.Id, true);
                                                    incomePaymentOrderRepository.UpdateOverpaidAmountById(lastPayment.Id, decimal.Zero);

                                                    IncomePaymentOrderSale lastSalePayment = lastPayment.IncomePaymentOrderSales.First();

                                                    lastSalePayment.Amount =
                                                        decimal.Round(lastSalePayment.Amount + differenceAmount, 14, MidpointRounding.AwayFromZero);

                                                    _paymentOrderRepositoriesFactory
                                                        .NewIncomePaymentOrderSaleRepository(connection)
                                                        .UpdateAmount(lastSalePayment);
                                                } else {
                                                    if (clientInDebt != null) {
                                                        clientInDebtRepository.Restore(clientInDebt.NetUid);

                                                        clientInDebt.Debt.Total =
                                                            decimal.Round(
                                                                differenceAmount - lastPayment.OverpaidAmount,
                                                                2,
                                                                MidpointRounding.AwayFromZero
                                                            );

                                                        incomePaymentOrderRepository.UpdateOverpaidAmountById(lastPayment.Id, decimal.Zero);

                                                        salePaymentStatusRepository
                                                            .SetSalePaymentStatusTypeById(
                                                                SalePaymentStatusType.PartialPaid,
                                                                sale.BaseSalePaymentStatusId
                                                            );

                                                        IncomePaymentOrderSale lastSalePayment = lastPayment.IncomePaymentOrderSales.First();

                                                        lastSalePayment.Amount =
                                                            decimal.Round(
                                                                lastSalePayment.Amount + lastPayment.OverpaidAmount,
                                                                2,
                                                                MidpointRounding.AwayFromZero
                                                            );

                                                        _paymentOrderRepositoriesFactory
                                                            .NewIncomePaymentOrderSaleRepository(connection)
                                                            .UpdateAmount(lastSalePayment);
                                                    }
                                                }

                                                break;
                                            }
                                            case SalePaymentStatusType.Overpaid: {
                                                decimal differenceAmount =
                                                    decimal.Round(sale.TotalAmount - totalAmount, 14, MidpointRounding.AwayFromZero);
                                                decimal euroDifferenceAmount =
                                                    decimal.Round(differenceAmount / euroExchangeRate.Amount, 14, MidpointRounding.AwayFromZero);

                                                if (lastPayment.OverpaidAmount > euroDifferenceAmount) {
                                                    lastPayment.OverpaidAmount =
                                                        decimal.Round(
                                                            lastPayment.OverpaidAmount - euroDifferenceAmount,
                                                            2,
                                                            MidpointRounding.AwayFromZero
                                                        );

                                                    incomePaymentOrderRepository.UpdateOverpaidAmountById(lastPayment.Id, decimal.Zero);

                                                    IncomePaymentOrderSale lastSalePayment = lastPayment.IncomePaymentOrderSales.First();

                                                    lastSalePayment.Amount =
                                                        decimal.Round(
                                                            lastSalePayment.Amount + differenceAmount / lastSalePayment.ExchangeRate,
                                                            2,
                                                            MidpointRounding.AwayFromZero
                                                        );

                                                    _paymentOrderRepositoriesFactory
                                                        .NewIncomePaymentOrderSaleRepository(connection)
                                                        .UpdateAmount(lastSalePayment);
                                                } else if (lastPayment.OverpaidAmount.Equals(euroDifferenceAmount)) {
                                                    salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Paid, sale.BaseSalePaymentStatusId);
                                                    saleRepository.UpdateIsAcceptedToPacking(sale.Id, true);

                                                    incomePaymentOrderRepository.UpdateOverpaidAmountById(lastPayment.Id, decimal.Zero);

                                                    IncomePaymentOrderSale lastSalePayment = lastPayment.IncomePaymentOrderSales.First();

                                                    lastSalePayment.Amount =
                                                        decimal.Round(
                                                            lastSalePayment.Amount + differenceAmount / lastSalePayment.ExchangeRate,
                                                            2,
                                                            MidpointRounding.AwayFromZero
                                                        );

                                                    _paymentOrderRepositoriesFactory
                                                        .NewIncomePaymentOrderSaleRepository(connection)
                                                        .UpdateAmount(lastSalePayment);
                                                } else {
                                                    if (clientInDebt != null) {
                                                        clientInDebtRepository.Restore(clientInDebt.NetUid);

                                                        decimal localOverpaidAmount =
                                                            decimal.Round(
                                                                lastPayment.OverpaidAmount * euroExchangeRate.Amount,
                                                                4,
                                                                MidpointRounding.AwayFromZero
                                                            );

                                                        clientInDebt.Debt.Total =
                                                            decimal.Round(differenceAmount - localOverpaidAmount, 14, MidpointRounding.AwayFromZero);

                                                        incomePaymentOrderRepository.UpdateOverpaidAmountById(lastPayment.Id, decimal.Zero);

                                                        salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.PartialPaid,
                                                            sale.BaseSalePaymentStatusId);

                                                        IncomePaymentOrderSale lastSalePayment = lastPayment.IncomePaymentOrderSales.First();

                                                        lastSalePayment.Amount =
                                                            decimal.Round(
                                                                lastSalePayment.Amount + lastPayment.OverpaidAmount,
                                                                2,
                                                                MidpointRounding.AwayFromZero
                                                            );

                                                        _paymentOrderRepositoriesFactory
                                                            .NewIncomePaymentOrderSaleRepository(connection)
                                                            .UpdateAmount(lastSalePayment);
                                                    }
                                                }

                                                break;
                                            }
                                            case SalePaymentStatusType.NotPaid:
                                            case SalePaymentStatusType.Refund:
                                            default:
                                                break;
                                        }
                                    else
                                        switch (sale.BaseSalePaymentStatus.SalePaymentStatusType) {
                                            case SalePaymentStatusType.PartialPaid: {
                                                if (clientInDebt != null) {
                                                    if (clientAgreement.Agreement.Currency.Code.ToLower().Equals("eur"))
                                                        clientInDebt.Debt.Total =
                                                            decimal.Round(
                                                                clientInDebt.Debt.Total - (totalAmount - sale.TotalAmount),
                                                                2,
                                                                MidpointRounding.AwayFromZero
                                                            );
                                                    else
                                                        clientInDebt.Debt.Total =
                                                            decimal.Round(
                                                                clientInDebt.Debt.Total - (totalAmountLocal - sale.TotalAmountLocal),
                                                                4,
                                                                MidpointRounding.AwayFromZero
                                                            );

                                                    if (clientInDebt.Debt.Total.Equals(0)) {
                                                        clientInDebtRepository.Remove(clientInDebt.NetUid);

                                                        salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Paid, sale.BaseSalePaymentStatusId);
                                                        saleRepository.UpdateIsAcceptedToPacking(sale.Id, true);
                                                    } else if (clientInDebt.Debt.Total < 0) {
                                                        decimal overpaidAmount;

                                                        if (clientAgreement.Agreement.Currency.Code.ToLower().Equals("eur"))
                                                            overpaidAmount = Math.Abs(clientInDebt.Debt.Total);
                                                        else
                                                            overpaidAmount =
                                                                decimal.Round(
                                                                    Math.Abs(clientInDebt.Debt.Total) / euroExchangeRate.Amount,
                                                                    2,
                                                                    MidpointRounding.AwayFromZero
                                                                );

                                                        incomePaymentOrderRepository.UpdateOverpaidAmountById(lastPayment.Id, overpaidAmount);

                                                        clientInDebtRepository.Remove(clientInDebt.NetUid);

                                                        salePaymentStatusRepository
                                                            .SetSalePaymentStatusTypeById(
                                                                SalePaymentStatusType.Overpaid,
                                                                sale.BaseSalePaymentStatusId
                                                            );
                                                        saleRepository.UpdateIsAcceptedToPacking(sale.Id, true);

                                                        clientAgreement.CurrentAmount =
                                                            decimal.Round(
                                                                clientAgreement.CurrentAmount + overpaidAmount,
                                                                2,
                                                                MidpointRounding.AwayFromZero
                                                            );

                                                        clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                                                        if (incomePaymentOrderSaleRepository.CheckIsMoreThanOnePaymentBySaleId(sale.Id)) {
                                                            //ToDo:
                                                        } else {
                                                            clientInDebt.Debt.Total = clientAgreement.Agreement.Currency.Code.ToLower().Equals("eur")
                                                                ? decimal.Round(sale.TotalAmount, 14, MidpointRounding.AwayFromZero)
                                                                : decimal.Round(sale.TotalAmountLocal, 14, MidpointRounding.AwayFromZero);

                                                            debtRepository.Update(clientInDebt.Debt);
                                                        }
                                                    } else {
                                                        debtRepository.Update(clientInDebt.Debt);
                                                    }
                                                }

                                                break;
                                            }
                                            case SalePaymentStatusType.Paid: {
                                                decimal overpaidAmount;

                                                if (clientAgreement.Agreement.Currency.Code.ToLower().Equals("eur"))
                                                    overpaidAmount = decimal.Round(totalAmount - sale.TotalAmount, 14, MidpointRounding.AwayFromZero);
                                                else
                                                    overpaidAmount =
                                                        decimal.Round(
                                                            (totalAmountLocal - sale.TotalAmountLocal) / euroExchangeRate.Amount,
                                                            2,
                                                            MidpointRounding.AwayFromZero
                                                        );

                                                incomePaymentOrderRepository.UpdateOverpaidAmountById(lastPayment.Id, overpaidAmount);

                                                salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Overpaid, sale.BaseSalePaymentStatusId);
                                                saleRepository.UpdateIsAcceptedToPacking(sale.Id, true);

                                                clientAgreement.CurrentAmount =
                                                    decimal.Round(clientAgreement.CurrentAmount + overpaidAmount, 14, MidpointRounding.AwayFromZero);

                                                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);
                                                break;
                                            }
                                            case SalePaymentStatusType.Overpaid: {
                                                decimal overpaidAmount;

                                                if (clientAgreement.Agreement.Currency.Code.ToLower().Equals("eur"))
                                                    overpaidAmount = decimal.Round(totalAmount - sale.TotalAmount, 14, MidpointRounding.AwayFromZero);
                                                else
                                                    overpaidAmount = decimal.Round(
                                                        (totalAmountLocal - sale.TotalAmountLocal) / euroExchangeRate.Amount,
                                                        2,
                                                        MidpointRounding.AwayFromZero
                                                    );

                                                clientAgreement.CurrentAmount =
                                                    decimal.Round(
                                                        clientAgreement.CurrentAmount + (overpaidAmount - lastPayment.OverpaidAmount),
                                                        2,
                                                        MidpointRounding.AwayFromZero
                                                    );

                                                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                                                overpaidAmount =
                                                    decimal.Round(overpaidAmount + lastPayment.OverpaidAmount, 14, MidpointRounding.AwayFromZero);

                                                incomePaymentOrderRepository.UpdateOverpaidAmountById(lastPayment.Id, overpaidAmount);

                                                IncomePaymentOrderSale lastSalePayment = lastPayment.IncomePaymentOrderSales.First();

                                                if (overpaidAmount < lastSalePayment.Amount) {
                                                    lastSalePayment.Amount =
                                                        decimal.Round(lastSalePayment.Amount - overpaidAmount, 14, MidpointRounding.AwayFromZero);

                                                    _paymentOrderRepositoriesFactory
                                                        .NewIncomePaymentOrderSaleRepository(connection)
                                                        .UpdateAmount(lastSalePayment);
                                                } else {
                                                    _paymentOrderRepositoriesFactory.NewIncomePaymentOrderSaleRepository(connection).Remove(lastSalePayment.Id);
                                                }

                                                break;
                                            }
                                            case SalePaymentStatusType.NotPaid:
                                            case SalePaymentStatusType.Refund:
                                            default:
                                                break;
                                        }
                                } else {
                                    //Paid from balance
                                    if (increased)
                                        switch (sale.BaseSalePaymentStatus.SalePaymentStatusType) {
                                            case SalePaymentStatusType.PartialPaid: {
                                                if (clientInDebt != null) {
                                                    if (clientAgreement.Agreement.Currency.Code.ToLower().Equals("eur"))
                                                        clientInDebt.Debt.Total =
                                                            decimal.Round(
                                                                clientInDebt.Debt.Total + (sale.TotalAmount - totalAmount),
                                                                2,
                                                                MidpointRounding.AwayFromZero
                                                            );
                                                    else
                                                        clientInDebt.Debt.Total =
                                                            decimal.Round(
                                                                clientInDebt.Debt.Total + (sale.TotalAmountLocal - totalAmountLocal),
                                                                4,
                                                                MidpointRounding.AwayFromZero
                                                            );

                                                    debtRepository.Update(clientInDebt.Debt);
                                                }

                                                break;
                                            }
                                            case SalePaymentStatusType.Paid: {
                                                if (clientInDebt != null) {
                                                    if (clientAgreement.CurrentAmount > 0) {
                                                        if (clientAgreement.Agreement.Currency.Code.ToLower().Equals("eur")) {
                                                            decimal differenceAmount =
                                                                decimal.Round(sale.TotalAmount - totalAmount, 14, MidpointRounding.AwayFromZero);

                                                            if (clientAgreement.CurrentAmount >= differenceAmount) {
                                                                _clientRepositoriesFactory
                                                                    .NewClientBalanceMovementRepository(connection)
                                                                    .AddOutMovement(
                                                                        new ClientBalanceMovement {
                                                                            ClientAgreementId = clientAgreement.Id,
                                                                            Amount = differenceAmount,
                                                                            ExchangeRateAmount = 1m
                                                                        }
                                                                    );

                                                                clientAgreement.CurrentAmount =
                                                                    decimal.Round(
                                                                        clientAgreement.CurrentAmount - differenceAmount,
                                                                        2,
                                                                        MidpointRounding.AwayFromZero
                                                                    );

                                                                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);
                                                            } else {
                                                                _clientRepositoriesFactory
                                                                    .NewClientBalanceMovementRepository(connection)
                                                                    .AddOutMovement(
                                                                        new ClientBalanceMovement {
                                                                            ClientAgreementId = clientAgreement.Id,
                                                                            Amount = clientAgreement.CurrentAmount,
                                                                            ExchangeRateAmount = 1m
                                                                        }
                                                                    );

                                                                differenceAmount =
                                                                    decimal.Round(
                                                                        differenceAmount - clientAgreement.CurrentAmount,
                                                                        2,
                                                                        MidpointRounding.AwayFromZero
                                                                    );

                                                                clientAgreement.CurrentAmount = decimal.Zero;

                                                                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                                                                salePaymentStatusRepository
                                                                    .SetSalePaymentStatusTypeById(
                                                                        SalePaymentStatusType.PartialPaid,
                                                                        sale.BaseSalePaymentStatusId
                                                                    );

                                                                clientInDebtRepository.Restore(clientInDebt.NetUid);

                                                                clientInDebt.Debt.Total = differenceAmount;

                                                                debtRepository.Update(clientInDebt.Debt);
                                                            }
                                                        } else {
                                                            decimal differenceAmount =
                                                                decimal.Round(
                                                                    sale.TotalAmountLocal - totalAmountLocal,
                                                                    4,
                                                                    MidpointRounding.AwayFromZero
                                                                );

                                                            decimal currentAmount = clientAgreement.CurrentAmount * euroExchangeRate.Amount;

                                                            if (currentAmount >= differenceAmount) {
                                                                decimal euroDifferenceAmount =
                                                                    decimal.Round(
                                                                        differenceAmount / euroExchangeRate.Amount,
                                                                        2,
                                                                        MidpointRounding.AwayFromZero
                                                                    );

                                                                _clientRepositoriesFactory
                                                                    .NewClientBalanceMovementRepository(connection)
                                                                    .AddOutMovement(
                                                                        new ClientBalanceMovement {
                                                                            ClientAgreementId = clientAgreement.Id,
                                                                            Amount = euroDifferenceAmount,
                                                                            ExchangeRateAmount = euroExchangeRate.Amount
                                                                        }
                                                                    );

                                                                clientAgreement.CurrentAmount =
                                                                    decimal.Round(
                                                                        clientAgreement.CurrentAmount - euroDifferenceAmount,
                                                                        2,
                                                                        MidpointRounding.AwayFromZero
                                                                    );

                                                                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);
                                                            } else {
                                                                _clientRepositoriesFactory
                                                                    .NewClientBalanceMovementRepository(connection)
                                                                    .AddOutMovement(
                                                                        new ClientBalanceMovement {
                                                                            ClientAgreementId = clientAgreement.Id,
                                                                            Amount = clientAgreement.CurrentAmount,
                                                                            ExchangeRateAmount = euroExchangeRate.Amount
                                                                        }
                                                                    );

                                                                differenceAmount =
                                                                    decimal.Round(differenceAmount - currentAmount, 14, MidpointRounding.AwayFromZero);

                                                                clientAgreement.CurrentAmount = decimal.Zero;

                                                                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                                                                salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.PartialPaid,
                                                                    sale.BaseSalePaymentStatusId);

                                                                clientInDebtRepository.Restore(clientInDebt.NetUid);

                                                                clientInDebt.Debt.Total = differenceAmount;

                                                                debtRepository.Update(clientInDebt.Debt);
                                                            }
                                                        }
                                                    } else {
                                                        clientInDebtRepository.Restore(clientInDebt.NetUid);

                                                        clientInDebt.Debt.Total = clientAgreement.Agreement.Currency.Code.ToLower().Equals("eur")
                                                            ? decimal.Round(sale.TotalAmount - totalAmount, 14, MidpointRounding.AwayFromZero)
                                                            : decimal.Round(sale.TotalAmountLocal - totalAmountLocal, 14, MidpointRounding.AwayFromZero);

                                                        debtRepository.Update(clientInDebt.Debt);
                                                    }
                                                }

                                                break;
                                            }
                                            case SalePaymentStatusType.Overpaid when clientAgreement.Agreement.Currency.Code.ToLower().Equals("eur"): {
                                                decimal differenceAmount =
                                                    decimal.Round(sale.TotalAmount - totalAmount, 14, MidpointRounding.AwayFromZero);

                                                if (clientAgreement.CurrentAmount > differenceAmount) {
                                                    clientAgreement.CurrentAmount =
                                                        decimal.Round(
                                                            clientAgreement.CurrentAmount - differenceAmount,
                                                            2,
                                                            MidpointRounding.AwayFromZero
                                                        );

                                                    _clientRepositoriesFactory
                                                        .NewClientBalanceMovementRepository(connection)
                                                        .AddOutMovement(
                                                            new ClientBalanceMovement {
                                                                ClientAgreementId = clientAgreement.Id,
                                                                Amount = differenceAmount,
                                                                ExchangeRateAmount = 1m
                                                            }
                                                        );

                                                    clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);
                                                } else if (clientAgreement.CurrentAmount.Equals(differenceAmount)) {
                                                    salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Paid, sale.BaseSalePaymentStatusId);
                                                    saleRepository.UpdateIsAcceptedToPacking(sale.Id, true);

                                                    _clientRepositoriesFactory
                                                        .NewClientBalanceMovementRepository(connection)
                                                        .AddOutMovement(
                                                            new ClientBalanceMovement {
                                                                ClientAgreementId = clientAgreement.Id,
                                                                Amount = clientAgreement.CurrentAmount,
                                                                ExchangeRateAmount = 1m
                                                            }
                                                        );

                                                    clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, decimal.Zero);
                                                } else {
                                                    if (clientInDebt != null) {
                                                        clientInDebtRepository.Restore(clientInDebt.NetUid);

                                                        clientInDebt.Debt.Total =
                                                            decimal.Round(
                                                                differenceAmount - clientAgreement.CurrentAmount,
                                                                2,
                                                                MidpointRounding.AwayFromZero
                                                            );

                                                        clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, decimal.Zero);

                                                        salePaymentStatusRepository
                                                            .SetSalePaymentStatusTypeById(
                                                                SalePaymentStatusType.PartialPaid,
                                                                sale.BaseSalePaymentStatusId
                                                            );
                                                    }
                                                }

                                                break;
                                            }
                                            case SalePaymentStatusType.Overpaid: {
                                                decimal differenceAmount =
                                                    decimal.Round(sale.TotalAmount - totalAmount, 14, MidpointRounding.AwayFromZero);
                                                decimal euroDifferenceAmount =
                                                    decimal.Round(differenceAmount / euroExchangeRate.Amount, 14, MidpointRounding.AwayFromZero);

                                                if (clientAgreement.CurrentAmount > euroDifferenceAmount) {
                                                    clientAgreement.CurrentAmount =
                                                        decimal.Round(
                                                            clientAgreement.CurrentAmount - euroDifferenceAmount,
                                                            2,
                                                            MidpointRounding.AwayFromZero
                                                        );

                                                    _clientRepositoriesFactory
                                                        .NewClientBalanceMovementRepository(connection)
                                                        .AddOutMovement(
                                                            new ClientBalanceMovement {
                                                                ClientAgreementId = clientAgreement.Id,
                                                                Amount = euroDifferenceAmount,
                                                                ExchangeRateAmount = euroExchangeRate.Amount
                                                            }
                                                        );

                                                    clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);
                                                } else if (clientAgreement.CurrentAmount.Equals(euroDifferenceAmount)) {
                                                    salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Paid, sale.BaseSalePaymentStatusId);
                                                    saleRepository.UpdateIsAcceptedToPacking(sale.Id, true);

                                                    _clientRepositoriesFactory
                                                        .NewClientBalanceMovementRepository(connection)
                                                        .AddOutMovement(
                                                            new ClientBalanceMovement {
                                                                ClientAgreementId = clientAgreement.Id,
                                                                Amount = clientAgreement.CurrentAmount,
                                                                ExchangeRateAmount = euroExchangeRate.Amount
                                                            }
                                                        );

                                                    clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, decimal.Zero);
                                                } else {
                                                    if (clientInDebt != null) {
                                                        clientInDebtRepository.Restore(clientInDebt.NetUid);

                                                        decimal localOverpaidAmount =
                                                            decimal.Round(
                                                                clientAgreement.CurrentAmount * euroExchangeRate.Amount,
                                                                4,
                                                                MidpointRounding.AwayFromZero
                                                            );

                                                        clientInDebt.Debt.Total =
                                                            decimal.Round(differenceAmount - localOverpaidAmount, 14, MidpointRounding.AwayFromZero);

                                                        _clientRepositoriesFactory
                                                            .NewClientBalanceMovementRepository(connection)
                                                            .AddOutMovement(
                                                                new ClientBalanceMovement {
                                                                    ClientAgreementId = clientAgreement.Id,
                                                                    Amount = clientAgreement.CurrentAmount,
                                                                    ExchangeRateAmount = euroExchangeRate.Amount
                                                                }
                                                            );

                                                        clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, decimal.Zero);

                                                        salePaymentStatusRepository
                                                            .SetSalePaymentStatusTypeById(
                                                                SalePaymentStatusType.PartialPaid,
                                                                sale.BaseSalePaymentStatusId
                                                            );
                                                    }
                                                }

                                                break;
                                            }
                                            case SalePaymentStatusType.NotPaid:
                                            case SalePaymentStatusType.Refund:
                                            default:
                                                break;
                                        }
                                    else
                                        switch (sale.BaseSalePaymentStatus.SalePaymentStatusType) {
                                            case SalePaymentStatusType.PartialPaid: {
                                                if (clientInDebt != null) {
                                                    clientInDebt.Debt.Total = clientAgreement.Agreement.Currency.Code.ToLower().Equals("eur")
                                                        ? decimal.Round(
                                                            clientInDebt.Debt.Total - (totalAmount - sale.TotalAmount),
                                                            2,
                                                            MidpointRounding.AwayFromZero
                                                        )
                                                        : decimal.Round(
                                                            clientInDebt.Debt.Total - (totalAmountLocal - sale.TotalAmount),
                                                            4,
                                                            MidpointRounding.AwayFromZero
                                                        );

                                                    debtRepository.Update(clientInDebt.Debt);
                                                }

                                                break;
                                            }
                                            case SalePaymentStatusType.Paid: {
                                                decimal overpaidAmount;

                                                if (clientAgreement.Agreement.Currency.Code.ToLower().Equals("eur"))
                                                    overpaidAmount = decimal.Round(totalAmount - sale.TotalAmount, 14, MidpointRounding.AwayFromZero);
                                                else
                                                    overpaidAmount =
                                                        decimal.Round(
                                                            (totalAmountLocal - sale.TotalAmountLocal) / euroExchangeRate.Amount,
                                                            2,
                                                            MidpointRounding.AwayFromZero
                                                        );

                                                clientAgreement.CurrentAmount =
                                                    decimal.Round(
                                                        clientAgreement.CurrentAmount + overpaidAmount,
                                                        2,
                                                        MidpointRounding.AwayFromZero
                                                    );

                                                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);
                                                break;
                                            }
                                            case SalePaymentStatusType.Overpaid: {
                                                decimal overpaidAmount;

                                                if (clientAgreement.Agreement.Currency.Code.ToLower().Equals("eur"))
                                                    overpaidAmount = decimal.Round(totalAmount - sale.TotalAmount, 14, MidpointRounding.AwayFromZero);
                                                else
                                                    overpaidAmount =
                                                        decimal.Round(
                                                            (totalAmountLocal - sale.TotalAmountLocal) / euroExchangeRate.Amount,
                                                            2,
                                                            MidpointRounding.AwayFromZero
                                                        );

                                                overpaidAmount =
                                                    decimal.Round(overpaidAmount + clientAgreement.CurrentAmount, 14, MidpointRounding.AwayFromZero);

                                                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, overpaidAmount);
                                                break;
                                            }
                                            case SalePaymentStatusType.NotPaid:
                                            case SalePaymentStatusType.Refund:
                                            default:
                                                break;
                                        }
                                }
                            }

                            Sender.Tell(new Tuple<Sale, string>(sale, string.Empty));
                            break;
                        }
                        case SaleLifeCycleType.Packaged:
                        case SaleLifeCycleType.Shipping:
                        case SaleLifeCycleType.Received:
                        case SaleLifeCycleType.Await:
                        default:
                            Sender.Tell(new Tuple<Sale, string>(null, SaleResourceNames.UPDATE_DISCOUNT_NOT_ALLOWED_AFTER_PACKING));
                            break;
                    }
                } else {
                    Sender.Tell(new Tuple<Sale, string>(null, "Such Sale does not exists in database"));
                }
            } else {
                Sender.Tell(new Tuple<Sale, string>(null, "Sale need to have existing NetUID"));
            }
        } else {
            Sender.Tell(new Tuple<Sale, string>(null, "Sale entity can not be null"));
        }
    }

    private void ProcessUpdateDeliveryRecipientMessage(UpdateDeliveryRecipientMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.DeliveryRecipient != null) {
            IDeliveryRecipientRepository deliveryRecipientRepository = _deliveryRepositoriesFactory.NewDeliveryRecipientRepository(connection);

            //if (message.DeliveryRecipient.IsNew()) {
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            Sale sale = saleRepository.GetByNetIdWithAgreement(message.NetId);

            if (sale != null) {
                message.DeliveryRecipient.ClientId = sale.ClientAgreement.ClientId;

                message.DeliveryRecipient.Id =
                    deliveryRecipientRepository
                        .Add(
                            message.DeliveryRecipient
                        );

                sale.DeliveryRecipientId = message.DeliveryRecipient.Id;

                saleRepository.Update(sale);
            }
            //} else {
            //    deliveryRecipientRepository
            //        .Update(
            //            message.DeliveryRecipient
            //        );
            //}

            Sender.Tell(
                deliveryRecipientRepository
                    .GetById(
                        message.DeliveryRecipient.Id
                    )
            );
        } else {
            Sender.Tell(
                message.DeliveryRecipient
            );
        }
    }

    private void ProcessUpdateDeliveryRecipientAddressMessage(UpdateDeliveryRecipientAddressMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.DeliveryRecipientAddress != null) {
            IDeliveryRecipientAddressRepository deliveryRecipientAddressRepository = _deliveryRepositoriesFactory.NewDeliveryRecipientAddressRepository(connection);

            //if (message.DeliveryRecipientAddress.IsNew()) {
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            Sale sale = saleRepository.GetByNetIdWithAgreement(message.NetId);

            if (sale?.DeliveryRecipientId != null) {
                message.DeliveryRecipientAddress.DeliveryRecipientId = sale.DeliveryRecipientId.Value;

                message.DeliveryRecipientAddress.Id =
                    deliveryRecipientAddressRepository
                        .Add(
                            message.DeliveryRecipientAddress
                        );

                sale.DeliveryRecipientAddressId = message.DeliveryRecipientAddress.Id;

                saleRepository.Update(sale);
            }
            //} 
            //else {
            //    deliveryRecipientAddressRepository
            //        .Update(
            //            message.DeliveryRecipientAddress
            //        );
            //}

            Sender.Tell(
                deliveryRecipientAddressRepository
                    .GetById(
                        message.DeliveryRecipientAddress.Id
                    )
            );
        } else {
            Sender.Tell(
                message.DeliveryRecipientAddress
            );
        }
    }

    private void ProcessSwitchBillSaleUnderClientStructureMessage(SwitchBillSaleUnderClientStructureMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            Sale sale = saleRepository.GetByNetId(message.SaleNetId);

            if (sale == null) throw new Exception("Sale with provided NetId does not exists");
            if (!sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New))
                throw new Exception("You can switch only the Bill Sale under client structure");

            ClientAgreement clientAgreement =
                _clientRepositoriesFactory
                    .NewClientAgreementRepository(connection)
                    .GetByNetId(
                        message.ClientAgreementNetId
                    );

            if (clientAgreement == null) throw new Exception("ClientAgreement with provided NetId does not exists");
            if (sale.ClientAgreementId.Equals(clientAgreement.Id)) throw new Exception("NeedDifferentAgreement");

            if (!sale.ClientAgreement.ClientId.Equals(clientAgreement.ClientId)) {
                IClientSubClientRepository clientSubClientRepository = _clientRepositoriesFactory.NewClientSubClientRepository(connection);

                ClientSubClient clientSubClient = clientSubClientRepository.GetByClientIdIfExists(sale.ClientAgreement.ClientId);

                if (clientSubClient == null) throw new Exception("Selected Sale is on Client that has no client structure");

                IEnumerable<ClientSubClient> subClients = clientSubClientRepository.GetAllByRootClientId(clientSubClient.RootClientId);

                if (!subClients.Any(s => s.SubClientId.Equals(clientAgreement.ClientId)) && !clientSubClient.RootClientId.Equals(clientAgreement.ClientId))
                    throw new Exception("You can switch Sale only inside client structure");
            }

            saleRepository.UpdateClientAgreementByIds(sale.Id, clientAgreement.Id);

            _saleRepositoriesFactory.NewOrderRepository(connection).UpdateClientAgreementByIds(sale.Id, clientAgreement.Id);

            sale = saleRepository.GetByNetId(message.SaleNetId);

            Sender.Tell(sale);

            ActorReferenceManager.Instance.Get(CommunicationsActorNames.HUBS_SENDER_ACTOR).Tell(new UpdatedSaleNotificationMessage(message.SaleNetId));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessExportInvoiceForPaymentForSaleByNetIdForPrintingMessage(ExportInvoiceForPaymentForSaleByNetIdForPrintingMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            Sale saleFromDb = saleRepository.GetByNetId(message.SaleNetId);

            string xlsxDocument = string.Empty;
            string pdfDocument = string.Empty;

            if (saleFromDb != null && saleFromDb.IsVatSale) {
                if (!saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.Packaged) &&
                    !saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.Packaging) &&
                    !saleFromDb.IsPrintedPaymentInvoice) {
                    User updatedBy = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                    BaseLifeCycleStatus oldLifeCycleStatus = saleFromDb.BaseLifeCycleStatus;

                    //saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType = SaleLifeCycleType.Packaging;

                    ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                        new RetrieveAndStoreAuditDataMessage(
                            message.UserNetId,
                            saleFromDb.NetUid,
                            "Sale",
                            saleFromDb.BaseLifeCycleStatus,
                            oldLifeCycleStatus
                        )
                    );

                    //_saleRepositoriesFactory.NewBaseLifeCycleStatusRepository(connection).Update(saleFromDb.BaseLifeCycleStatus);

                    //saleRepository.SetChangedToInvoiceDateByNetId(saleFromDb.NetUid, updatedBy?.Id);

                    saleFromDb = saleRepository.GetByNetId(saleFromDb.NetUid);

                    SaveExchangeRates(_saleRepositoriesFactory, _exchangeRateRepositoriesFactory, connection, saleFromDb.Id);

                    SavePricesPerItem(
                        _saleRepositoriesFactory,
                        _exchangeRateRepositoriesFactory,
                        _productRepositoriesFactory.NewProductGroupDiscountRepository(connection),
                        connection,
                        saleFromDb
                    );

                    SetDebtToClient(
                        connection,
                        _exchangeRateRepositoriesFactory,
                        _clientRepositoriesFactory.NewClientAgreementRepository(connection),
                        _clientRepositoriesFactory.NewClientInDebtRepository(connection),
                        _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection),
                        _currencyRepositoriesFactory.NewCurrencyRepository(connection),
                        _saleRepositoriesFactory,
                        saleFromDb
                    );

                    _clientRepositoriesFactory.NewClientRegistrationTaskRepository(connection).SetDoneByClientId(saleFromDb.ClientAgreement.ClientId);

                    _clientRepositoriesFactory.NewClientRepository(connection).SetTemporaryClientById(saleFromDb.ClientAgreement.ClientId);

                    saleFromDb = saleRepository.GetByNetId(message.SaleNetId);
                }

                if (!saleFromDb.BaseSalePaymentStatus.SalePaymentStatusType.Equals(SalePaymentStatusType.Paid)
                    && !saleFromDb.BaseSalePaymentStatus.SalePaymentStatusType.Equals(SalePaymentStatusType.Overpaid)) {
                    saleFromDb.IsLocked = true;
                    saleFromDb.IsPaymentBillDownloaded = true;
                    if (saleFromDb.BillDownloadDate == null) saleRepository.SetBillDownloadDateByNetId(saleFromDb.NetUid);
                    saleRepository.UpdateLockInfo(saleFromDb);

                    saleFromDb = saleRepository.GetByNetId(message.SaleNetId);
                }

                SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPrices(saleFromDb, _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
                    _currencyRepositoriesFactory.NewCurrencyRepository(connection));

                if (saleFromDb.ClientAgreement.Agreement.CurrencyId.HasValue && saleFromDb.ClientAgreement.Agreement.OrganizationId.HasValue) {
                    PaymentRegister paymentRegister =
                        _paymentOrderRepositoriesFactory
                            .NewPaymentRegisterRepository(connection)
                            .GetActiveBankAccountByCurrencyAndOrganizationIds(
                                saleFromDb.ClientAgreement.Agreement.CurrencyId.Value,
                                saleFromDb.ClientAgreement.Agreement.OrganizationId.Value
                            );

                    if (paymentRegister != null) saleFromDb.ClientAgreement.Agreement.Organization.PaymentRegisters.Add(paymentRegister);
                }

                saleRepository.UpdateIsPrintedPaymentInvoice(saleFromDb.Id);

                (xlsxDocument, pdfDocument) =
                    _xlsFactoryManager
                        .NewSalesXlsManager()
                        .ExportInvoiceForPaymentForSale(message.Path, saleFromDb);
            }

            if (saleFromDb != null) {
                ActorReferenceManager.Instance.Get(CommunicationsActorNames.HUBS_SENDER_ACTOR).Tell(new UpdatedSaleNotificationMessage(saleFromDb.NetUid));

                Sender.Tell((xlsxDocument, pdfDocument));
            } else {
                Sender.Tell((xlsxDocument, pdfDocument));
            }
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessExportInvoiceForPaymentForSaleForPrintingFromLastStepMessage(ExportInvoiceForPaymentForSaleForPrintingFromLastStepMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
            IWarehousesShipmentRepository warehousesShipmentRepository = _saleRepositoriesFactory.NewWarehousesShipmentRepository(connection);

            Sale saleFromDb = saleRepository.GetByIdWithAgreement(message.Sale.Id);
            User updatedB = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            if (saleFromDb.WarehousesShipmentId == 0) saleFromDb.WarehousesShipmentId = SetWarehousesShipment(message.Sale, warehousesShipmentRepository, updatedB);
            message.Sale.UpdateUserId = updatedB.Id;
            saleRepository.UpdateUser(message.Sale);

            string xlsxDocument = string.Empty;
            string pdfDocument = string.Empty;
            string xlsxInvoiceDocument = string.Empty;
            string pdfInvoiceDocument = string.Empty;

            if (saleFromDb != null && saleFromDb.IsVatSale) {
                BaseLifeCycleStatus oldSaleLifeCycleType = saleFromDb.BaseLifeCycleStatus;

                string saleResourceName =
                    !oldSaleLifeCycleType.SaleLifeCycleType.Equals(saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType)
                        ? SaleResourceNames.UPDATED_LIFE_CYCLE_STATUS
                        : SaleResourceNames.UPDATED;

                if (message.Sale.Transporter != null) saleFromDb.TransporterId = message.Sale.Transporter.Id;

                if (message.Sale.DeliveryRecipient != null) {
                    if (message.Sale.Transporter.Name != "��������") {
                        if (message.Sale.DeliveryRecipient.IsNew()) {
                            saleFromDb.DeliveryRecipientId = _deliveryRepositoriesFactory.NewDeliveryRecipientRepository(connection).Add(message.Sale.DeliveryRecipient);
                        } else {
                            _deliveryRepositoriesFactory.NewDeliveryRecipientRepository(connection).Update(message.Sale.DeliveryRecipient);

                            saleFromDb.DeliveryRecipientId = message.Sale.DeliveryRecipient.Id;
                        }
                    } else {
                        message.Sale.DeliveryRecipient.FullName = null;
                        if (message.Sale.DeliveryRecipient.IsNew()) {
                            saleFromDb.DeliveryRecipientId = _deliveryRepositoriesFactory.NewDeliveryRecipientRepository(connection).Add(message.Sale.DeliveryRecipient);
                        } else {
                            _deliveryRepositoriesFactory.NewDeliveryRecipientRepository(connection).Update(message.Sale.DeliveryRecipient);

                            saleFromDb.DeliveryRecipientId = message.Sale.DeliveryRecipient.Id;
                        }
                    }
                }

                if (message.Sale.DeliveryRecipientAddress != null) {
                    if (message.Sale.DeliveryRecipientAddress.IsNew()) {
                        if (saleFromDb.DeliveryRecipientId.HasValue) {
                            message.Sale.DeliveryRecipientAddress.DeliveryRecipientId = saleFromDb.DeliveryRecipientId.Value;

                            saleFromDb.DeliveryRecipientAddressId =
                                _deliveryRepositoriesFactory.NewDeliveryRecipientAddressRepository(connection).Add(message.Sale.DeliveryRecipientAddress);
                        }
                    } else {
                        _deliveryRepositoriesFactory.NewDeliveryRecipientAddressRepository(connection).Update(message.Sale.DeliveryRecipientAddress);

                        saleFromDb.DeliveryRecipientAddressId = message.Sale.DeliveryRecipientAddress.Id;
                    }
                }

                if (saleFromDb.CustomersOwnTtn != null)
                    if (message.Sale.CustomersOwnTtnId == 0L) {
                        saleRepository.RemoveCustomersOwnTtn(saleFromDb.CustomersOwnTtn);
                        saleFromDb.CustomersOwnTtnId = null;
                    }

                if (message.Sale.CustomersOwnTtn != null && message.Sale.CustomersOwnTtn.IsNew())
                    saleFromDb.CustomersOwnTtnId = saleRepository.AddCustomersOwnTtn(message.Sale.CustomersOwnTtn);
                saleFromDb.IsCashOnDelivery = message.Sale.IsCashOnDelivery;
                saleFromDb.CashOnDeliveryAmount = message.Sale.CashOnDeliveryAmount;

                saleFromDb.Comment = message.Sale.Comment;
                saleFromDb.Order.OrderItems = message.Sale.Order.OrderItems;

                saleRepository.Update(saleFromDb);

                saleFromDb = saleRepository.GetByNetId(saleFromDb.NetUid);

                if (!saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.Packaged)
                    && !saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.Packaging)
                    && !saleFromDb.IsPaymentBillDownloaded) {
                    User updatedBy = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                    BaseLifeCycleStatus oldLifeCycleStatus = saleFromDb.BaseLifeCycleStatus;

                    saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType = SaleLifeCycleType.Packaging;

                    ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                        new RetrieveAndStoreAuditDataMessage(
                            message.UserNetId,
                            saleFromDb.NetUid,
                            "Sale",
                            saleFromDb.BaseLifeCycleStatus,
                            oldLifeCycleStatus
                        )
                    );

                    _saleRepositoriesFactory.NewBaseLifeCycleStatusRepository(connection).Update(saleFromDb.BaseLifeCycleStatus);

                    if (saleFromDb.RetailClientId != null) {
                        IRetailPaymentStatusRepository retailPaymentStatusRepository = _retailClientRepositoriesFactory.NewRetailPaymentStatusRepository(connection);

                        RetailPaymentStatus retailPaymentStatus = retailPaymentStatusRepository.GetBySaleId(saleFromDb.Id);
                        retailPaymentStatusRepository.SetRetailPaymentStatusTypeById(RetailPaymentStatusType.ChangedToInvoice, retailPaymentStatus.Id);
                    }

                    saleRepository.SetChangedToInvoiceDateByNetId(saleFromDb.NetUid, updatedBy?.Id);
                    saleRepository.SetBillDownloadDateByNetId(saleFromDb.NetUid);

                    saleFromDb = saleRepository.GetByNetId(saleFromDb.NetUid);

                    if (saleFromDb.ClientAgreement.Agreement.NumberDaysDebt > 0)
                        saleRepository.UpdateIsAcceptedToPacking(saleFromDb.Id, true);

                    SaveExchangeRates(_saleRepositoriesFactory, _exchangeRateRepositoriesFactory, connection, saleFromDb.Id);

                    SavePricesPerItem(
                        _saleRepositoriesFactory,
                        _exchangeRateRepositoriesFactory,
                        _productRepositoriesFactory.NewProductGroupDiscountRepository(connection),
                        connection,
                        saleFromDb,
                        true
                    );

                    SetDebtToClient(
                        connection,
                        _exchangeRateRepositoriesFactory,
                        _clientRepositoriesFactory.NewClientAgreementRepository(connection),
                        _clientRepositoriesFactory.NewClientInDebtRepository(connection),
                        _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection),
                        _currencyRepositoriesFactory.NewCurrencyRepository(connection),
                        _saleRepositoriesFactory,
                        saleFromDb
                    );

                    if (saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.Packaging) &&
                        !oldSaleLifeCycleType.SaleLifeCycleType.Equals(saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType))
                        ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR)
                            .Tell(new StoreConsignmentMovementFromSaleMessage(saleFromDb.Id, Self, saleResourceName));

                    _clientRepositoriesFactory.NewClientRegistrationTaskRepository(connection).SetDoneByClientId(saleFromDb.ClientAgreement.ClientId);

                    _clientRepositoriesFactory.NewClientRepository(connection).SetTemporaryClientById(saleFromDb.ClientAgreement.ClientId);

                    saleFromDb = saleRepository.GetByNetId(saleFromDb.NetUid);
                }

                if (saleFromDb.IsPaymentBillDownloaded) {
                    User updatedBy = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                    BaseLifeCycleStatus oldLifeCycleStatus = saleFromDb.BaseLifeCycleStatus;

                    saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType = SaleLifeCycleType.Packaging;

                    ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                        new RetrieveAndStoreAuditDataMessage(
                            message.UserNetId,
                            saleFromDb.NetUid,
                            "Sale",
                            saleFromDb.BaseLifeCycleStatus,
                            oldLifeCycleStatus
                        )
                    );

                    _saleRepositoriesFactory.NewBaseLifeCycleStatusRepository(connection).Update(saleFromDb.BaseLifeCycleStatus);

                    if (saleFromDb.RetailClientId != null) {
                        IRetailPaymentStatusRepository retailPaymentStatusRepository = _retailClientRepositoriesFactory.NewRetailPaymentStatusRepository(connection);

                        RetailPaymentStatus retailPaymentStatus = retailPaymentStatusRepository.GetBySaleId(saleFromDb.Id);
                        retailPaymentStatusRepository.SetRetailPaymentStatusTypeById(RetailPaymentStatusType.ChangedToInvoice, retailPaymentStatus.Id);
                    }

                    saleRepository.SetChangedToInvoiceDateByNetId(saleFromDb.NetUid, updatedBy?.Id);

                    saleFromDb = saleRepository.GetByNetId(saleFromDb.NetUid);

                    if (saleFromDb.ClientAgreement.Agreement.NumberDaysDebt > 0)
                        saleRepository.UpdateIsAcceptedToPacking(saleFromDb.Id, true);

                    if (saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.Packaging) &&
                        !oldSaleLifeCycleType.SaleLifeCycleType.Equals(saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType))
                        ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR)
                            .Tell(new StoreConsignmentMovementFromSaleMessage(saleFromDb.Id, Self, saleResourceName));

                    _clientRepositoriesFactory.NewClientRegistrationTaskRepository(connection).SetDoneByClientId(saleFromDb.ClientAgreement.ClientId);

                    _clientRepositoriesFactory.NewClientRepository(connection).SetTemporaryClientById(saleFromDb.ClientAgreement.ClientId);

                    saleFromDb = saleRepository.GetByNetId(saleFromDb.NetUid);
                }

                if (!saleFromDb.BaseSalePaymentStatus.SalePaymentStatusType.Equals(SalePaymentStatusType.Paid)
                    && !saleFromDb.BaseSalePaymentStatus.SalePaymentStatusType.Equals(SalePaymentStatusType.Overpaid)) {
                    saleFromDb.IsLocked = true;
                    saleFromDb.IsPaymentBillDownloaded = true;

                    saleRepository.UpdateLockInfo(saleFromDb);

                    saleFromDb = saleRepository.GetByNetId(saleFromDb.NetUid);
                }

                SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPrices(saleFromDb, _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
                    _currencyRepositoriesFactory.NewCurrencyRepository(connection));

                if (saleFromDb.ClientAgreement.Agreement.CurrencyId.HasValue && saleFromDb.ClientAgreement.Agreement.OrganizationId.HasValue) {
                    PaymentRegister paymentRegister =
                        _paymentOrderRepositoriesFactory
                            .NewPaymentRegisterRepository(connection)
                            .GetActiveBankAccountByCurrencyAndOrganizationIds(
                                saleFromDb.ClientAgreement.Agreement.CurrencyId.Value,
                                saleFromDb.ClientAgreement.Agreement.OrganizationId.Value
                            );

                    if (paymentRegister != null) saleFromDb.ClientAgreement.Agreement.Organization.PaymentRegisters.Add(paymentRegister);
                }

                (xlsxDocument, pdfDocument) =
                    _xlsFactoryManager
                        .NewSalesXlsManager()
                        .ExportInvoiceForPaymentForSale(message.Path, saleFromDb);

                saleRepository.UpdateIsPrintedPaymentInvoice(saleFromDb.Id);
            }

            if (saleFromDb != null)
                saleRepository.UpdateIsPrintedPaymentInvoice(saleFromDb.Id);

            Sale saleForInvoice = saleRepository.GetByNetIdWithProductLocations(saleFromDb?.NetUid ?? Guid.Empty);

            if (saleForInvoice != null) {
                if (saleForInvoice.SaleInvoiceDocument == null) {
                    SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPricesWithUsdCalculations(saleForInvoice,
                        _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection));

                    if (!saleForInvoice.SaleInvoiceNumberId.HasValue) {
                        ISaleInvoiceNumberRepository saleInvoiceNumberRepository = _saleRepositoriesFactory.NewSaleInvoiceNumberRepository(connection);

                        string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

                        saleForInvoice.SaleInvoiceNumber = new SaleInvoiceNumber {
                            Number =
                                string.Format(
                                    "{0:D4}",
                                    Convert.ToInt64(
                                        saleForInvoice.SaleNumber.Value.Substring(
                                            saleForInvoice.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length,
                                            saleForInvoice.SaleNumber.Value.Length -
                                            (saleForInvoice.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length)
                                        )
                                    )
                                )
                        };

                        saleForInvoice.SaleInvoiceNumberId = saleInvoiceNumberRepository.Add(saleForInvoice.SaleInvoiceNumber);

                        saleRepository.UpdateSaleInvoiceNumber(saleForInvoice);
                    }

                    (xlsxInvoiceDocument, pdfInvoiceDocument) =
                        _xlsFactoryManager
                            .NewSalesXlsManager()
                            .ExportUkInvoiceIsVatSaleToXlsx(
                                message.Path,
                                saleForInvoice,
                                _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
                            );
                } else {
                    SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPricesWithUsdCalculations(saleForInvoice,
                        _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection));

                    if (!saleForInvoice.SaleInvoiceNumberId.HasValue) {
                        ISaleInvoiceNumberRepository saleInvoiceNumberRepository = _saleRepositoriesFactory.NewSaleInvoiceNumberRepository(connection);

                        string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

                        saleForInvoice.SaleInvoiceNumber = new SaleInvoiceNumber {
                            Number =
                                string.Format(
                                    "{0:D4}",
                                    Convert.ToInt64(
                                        saleForInvoice.SaleNumber.Value.Substring(
                                            saleForInvoice.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length,
                                            saleForInvoice.SaleNumber.Value.Length -
                                            (saleForInvoice.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length)
                                        )
                                    )
                                )
                        };

                        saleForInvoice.SaleInvoiceNumberId = saleInvoiceNumberRepository.Add(saleForInvoice.SaleInvoiceNumber);

                        saleRepository.UpdateSaleInvoiceNumber(saleForInvoice);
                    }

                    (xlsxInvoiceDocument, pdfInvoiceDocument) =
                        _xlsFactoryManager
                            .NewSalesXlsManager()
                            .ExportPlInvoiceToXlsx(message.Path, saleForInvoice);
                }
            }

            if (saleFromDb != null) {
                ActorReferenceManager.Instance.Get(CommunicationsActorNames.HUBS_SENDER_ACTOR).Tell(new UpdatedSaleNotificationMessage(saleFromDb.NetUid));

                Sender.Tell((xlsxDocument, pdfDocument, xlsxInvoiceDocument, pdfInvoiceDocument, saleFromDb.IsAcceptedToPacking));
            } else {
                Sender.Tell((xlsxDocument, pdfDocument, xlsxInvoiceDocument, pdfInvoiceDocument, false));
            }
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private static void SetDebtToClient(
        IDbConnection connection,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        IClientAgreementRepository clientAgreementRepository,
        IClientInDebtRepository clientInDebtRepository,
        IClientBalanceMovementRepository clientBalanceMovementRepository,
        ICurrencyRepository currencyRepository,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        Sale sale) {
        ClientAgreement clientAgreement = clientAgreementRepository.GetById(sale.ClientAgreement.Id);
        ClientInDebt clientInDebtFromDb = clientInDebtRepository.GetBySaleAndClientAgreementIds(sale.Id, clientAgreement.Id);
        ISaleRepository saleRepository = saleRepositoriesFactory.NewSaleRepository(connection);
        ISaleInvoiceDocumentRepository saleInvoiceDocumentRepository = saleRepositoriesFactory.NewSaleInvoiceDocumentRepository(connection);
        IBaseSalePaymentStatusRepository baseSalePaymentStatusRepository = saleRepositoriesFactory.NewBaseSalePaymentStatusRepository(connection);
        IDebtRepository debtRepository = saleRepositoriesFactory.NewDebtRepository(connection);

        decimal exchangeRateAmount = decimal.Zero;

        if (clientAgreement.Agreement.Currency.Code.ToLower().Equals("usd")) {
            Currency euro = currencyRepository.GetEURCurrencyIfExists();

            if (euro != null) {
                ICrossExchangeRateRepository crossExchangeRateRepository = exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection);

                CrossExchangeRate crossExchangeRate = crossExchangeRateRepository.GetByCurrenciesIds(euro.Id, clientAgreement.Agreement.Currency.Id);

                if (crossExchangeRate != null) exchangeRateAmount = crossExchangeRate.Amount;
            }
        } else {
            exchangeRateAmount = exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection).GetEuroExchangeRateByCurrentCulture().Amount;
        }

        decimal total = decimal.Zero;
        decimal totalEuro = decimal.Zero;

        if (sale.ClientAgreement.Agreement.Currency.Code.ToLower().Equals("usd")) {
            decimal euroUsdExchangeRateAmount =
                exchangeRateRepositoriesFactory
                    .NewExchangeRateRepository(connection)
                    .GetEuroToUsdExchangeRateAmountByFromDate(sale.ChangedToInvoice ?? DateTime.UtcNow);

            foreach (OrderItem orderItem in sale.Order.OrderItems) {
                orderItem.TotalAmount =
                    decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal =
                    decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty) * orderItem.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                total =
                    decimal.Round(total + orderItem.TotalAmountLocal, 14, MidpointRounding.AwayFromZero);
                totalEuro =
                    decimal.Round(totalEuro + orderItem.TotalAmount, 14, MidpointRounding.AwayFromZero);
            }

            if (sale.SaleInvoiceDocument != null && sale.SaleInvoiceDocument.ShippingAmount > 0) {
                sale.SaleInvoiceDocument.ShippingAmountEur =
                    decimal.Round(sale.SaleInvoiceDocument.ShippingAmount / euroUsdExchangeRateAmount, 14, MidpointRounding.AwayFromZero);
                sale.SaleInvoiceDocument.ShippingAmountEurWithoutVat =
                    decimal.Round(sale.SaleInvoiceDocument.ShippingAmountWithoutVat / euroUsdExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                total += sale.SaleInvoiceDocument.ShippingAmount;
                totalEuro += sale.SaleInvoiceDocument.ShippingAmountEur;

                saleInvoiceDocumentRepository.UpdateShippingAmount(sale.SaleInvoiceDocument);
            }
        } else {
            foreach (OrderItem orderItem in sale.Order.OrderItems) {
                orderItem.Product.CurrentLocalPrice =
                    decimal.Round(orderItem.PricePerItem * orderItem.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                orderItem.TotalAmount =
                    decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal =
                    decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty) * orderItem.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                total =
                    decimal.Round(total + orderItem.TotalAmountLocal, 14, MidpointRounding.AwayFromZero);
                totalEuro =
                    decimal.Round(totalEuro + orderItem.TotalAmount, 14, MidpointRounding.AwayFromZero);
            }

            if (sale.SaleInvoiceDocument != null && sale.SaleInvoiceDocument.ShippingAmount > 0) {
                if (sale.ClientAgreement.Agreement.Currency.Code.ToLower().Equals("eur")) {
                    sale.SaleInvoiceDocument.ShippingAmountEur = sale.SaleInvoiceDocument.ShippingAmount;
                    sale.SaleInvoiceDocument.ShippingAmountEurWithoutVat = sale.SaleInvoiceDocument.ShippingAmountWithoutVat;
                } else {
                    sale.SaleInvoiceDocument.ShippingAmountEur =
                        decimal.Round(sale.SaleInvoiceDocument.ShippingAmount / exchangeRateAmount, 14, MidpointRounding.AwayFromZero);
                    sale.SaleInvoiceDocument.ShippingAmountEurWithoutVat =
                        decimal.Round(sale.SaleInvoiceDocument.ShippingAmountWithoutVat / exchangeRateAmount, 14, MidpointRounding.AwayFromZero);
                }

                total += sale.SaleInvoiceDocument.ShippingAmount;
                totalEuro += sale.SaleInvoiceDocument.ShippingAmountEur;

                saleInvoiceDocumentRepository.UpdateShippingAmount(sale.SaleInvoiceDocument);
            }
        }

        if (sale.ClientAgreement.Agreement.Currency.Code.ToLower().Equals("eur")) total = totalEuro;

        if (clientAgreement.CurrentAmount >= totalEuro) {
            clientAgreement.CurrentAmount = Math.Round(clientAgreement.CurrentAmount - totalEuro, 14);

            clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

            clientBalanceMovementRepository
                .AddOutMovement(
                    new ClientBalanceMovement {
                        ClientAgreementId = clientAgreement.Id,
                        Amount = totalEuro,
                        ExchangeRateAmount = !exchangeRateAmount.Equals(decimal.Zero) ? exchangeRateAmount : 1m
                    }
                );

            baseSalePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Paid, sale.BaseSalePaymentStatusId);
            saleRepository.UpdateIsAcceptedToPacking(sale.Id, true);
        } else {
            if (clientAgreement.CurrentAmount > decimal.Zero) {
                totalEuro = decimal.Round(totalEuro - clientAgreement.CurrentAmount, 14, MidpointRounding.AwayFromZero);

                clientBalanceMovementRepository
                    .AddOutMovement(
                        new ClientBalanceMovement {
                            ClientAgreementId = clientAgreement.Id,
                            Amount = clientAgreement.CurrentAmount,
                            ExchangeRateAmount = !exchangeRateAmount.Equals(decimal.Zero) ? exchangeRateAmount : 1m
                        }
                    );

                clientAgreement.CurrentAmount = decimal.Zero;

                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                baseSalePaymentStatusRepository
                    .SetSalePaymentStatusTypeById(SalePaymentStatusType.PartialPaid, sale.BaseSalePaymentStatusId);

                if (!clientAgreement.Agreement.Currency.Code.Equals("EUR"))
                    total = totalEuro * exchangeRateAmount;
                else
                    total = totalEuro;
            }

            if (clientInDebtFromDb != null) {
                clientInDebtFromDb.Debt.Total = total;

                debtRepository.Update(clientInDebtFromDb.Debt);
            } else {
                Debt debt = new() {
                    Days = 0,
                    Total = total
                };

                ClientInDebt clientInDebt = new() {
                    AgreementId = clientAgreement.AgreementId,
                    ClientId = clientAgreement.ClientId,
                    DebtId = debtRepository.Add(debt),
                    SaleId = sale.Id
                };

                clientInDebtRepository.Add(clientInDebt);
            }
        }

        ClientInDebt expiredDebt = clientInDebtRepository.GetExpiredDebtByClientAgreementId(clientAgreement.Id);

        if (expiredDebt != null)
            saleRepository.SetIsAcceptedToPackingFalse(sale.Id);
    }

    private static void SavePricesPerItem(
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoryFactory,
        IProductGroupDiscountRepository productGroupDiscountRepository,
        IDbConnection connection,
        Sale sale,
        bool isDiscount = false) {
        IExchangeRateRepository exchangeRateRepository = exchangeRateRepositoryFactory.NewExchangeRateRepository(connection);

        decimal vatRate = sale.SaleInvoiceDocument?.Vat ?? 0m;

        ExchangeRate currentExchangeRateAmount = exchangeRateRepository.GetEuroExchangeRateByCurrentCulture();

        foreach (OrderItem orderItem in sale.Order.OrderItems) {
            decimal currentExchangeRateAmountFiltered = exchangeRateRepository.GetEuroExchangeRateByCurrentCultureFiltered(
                orderItem.Product.NetUid,
                sale.RetailClientId.HasValue || sale.IsVatSale,
                orderItem.IsFromReSale,
                sale.ClientAgreement.Agreement.Currency.Id
            );

            if (!isDiscount)
                orderItem.PricePerItemWithoutVat =
                    decimal.Round(
                        orderItem.Product.CurrentPrice - orderItem.Product.CurrentPrice * orderItem.OneTimeDiscount / 100,
                        14,
                        MidpointRounding.AwayFromZero
                    );
            else
                orderItem.PricePerItemWithoutVat =
                    decimal.Round(
                        orderItem.Product.CurrentPrice,
                        14,
                        MidpointRounding.AwayFromZero
                    );
            orderItem.PricePerItem =
                decimal.Round(
                    orderItem.PricePerItemWithoutVat + orderItem.PricePerItemWithoutVat * vatRate / 100,
                    14,
                    MidpointRounding.AwayFromZero
                );

            orderItem.ExchangeRateAmount = currentExchangeRateAmountFiltered;

            if (!orderItem.Product.ProductProductGroups.Any()) continue;

            long productGroupId = orderItem.Product.ProductProductGroups.First().ProductGroupId;

            ProductGroupDiscount discount = productGroupDiscountRepository.GetByProductGroupAndClientAgreementIdsIfExists(sale.ClientAgreementId, productGroupId);

            if (discount != null) orderItem.DiscountAmount = Convert.ToDecimal(discount.DiscountRate);
        }

        if (sale.SaleInvoiceDocument != null) {
            sale.SaleInvoiceDocument.ExchangeRateAmount = currentExchangeRateAmount.Amount;

            sale.SaleInvoiceDocument.ShippingAmount =
                decimal.Round(
                    sale.SaleInvoiceDocument.ShippingAmount + sale.SaleInvoiceDocument.ShippingAmount * vatRate / 100,
                    2,
                    MidpointRounding.AwayFromZero
                );

            saleRepositoriesFactory.NewSaleInvoiceDocumentRepository(connection).UpdateExchangeRateAmountAndAmounts(sale.SaleInvoiceDocument);
        }

        saleRepositoriesFactory.NewOrderItemRepository(connection).Update(sale.Order.OrderItems);
    }

    private static Sale CreateNewSaleWithStatusesOnly(
        AddSaleWithStatusesOnlyMessage message,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IUserRepositoriesFactory userRepositoryFactory,
        IDbConnection connection,
        ISaleRepository saleRepository,
        ISaleNumberRepository saleNumberRepository) {
        message.Sale.BaseLifeCycleStatus = new BaseLifeCycleStatus { SaleLifeCycleType = SaleLifeCycleType.New };

        message.Sale.BaseLifeCycleStatusId = saleRepositoriesFactory
            .NewBaseLifeCycleStatusRepository(connection)
            .Add(message.Sale.BaseLifeCycleStatus);

        if (message.Sale.SaleInvoiceDocument != null) {
            if (message.Sale.SaleInvoiceDocument.IsNew()) {
                message.Sale.SaleInvoiceDocumentId = saleRepositoriesFactory.NewSaleInvoiceDocumentRepository(connection).Add(message.Sale.SaleInvoiceDocument);
            } else {
                saleRepositoriesFactory.NewSaleInvoiceDocumentRepository(connection).Update(message.Sale.SaleInvoiceDocument);

                message.Sale.SaleInvoiceDocumentId = message.Sale.SaleInvoiceDocument.Id;
            }
        }

        message.Sale.BaseSalePaymentStatus = new BaseSalePaymentStatus { SalePaymentStatusType = SalePaymentStatusType.NotPaid };

        message.Sale.BaseSalePaymentStatusId = saleRepositoriesFactory
            .NewBaseSalePaymentStatusRepository(connection)
            .Add(message.Sale.BaseSalePaymentStatus);

        if (!message.UserNetId.Equals(Guid.Empty)) {
            User user = userRepositoryFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            if (user != null) message.Sale.UserId = user.Id;
        }

        message.Sale.ClientAgreementId = message.Sale.ClientAgreement.Id;

        if (message.Sale.ClientAgreement.Agreement?.Organization != null) {
            SaleNumber lastSaleNumber = saleNumberRepository.GetLastRecordByOrganizationNetId(message.Sale.ClientAgreement.Agreement.Organization.NetUid);
            SaleNumber saleNumber;

            Organization organization = message.Sale.ClientAgreement.Agreement.Organization;
            string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

            if (message.OriginalMessage is AddOrderItemMessage addOrderItemMessage &&
                !string.IsNullOrEmpty(addOrderItemMessage.SaleNumber))
                saleNumber = new SaleNumber {
                    Value = addOrderItemMessage.SaleNumber,
                    OrganizationId = organization.Id
                };
            else if (lastSaleNumber != null && !string.IsNullOrEmpty(lastSaleNumber.Value) && DateTime.Now.Year.Equals(lastSaleNumber.Created.Year))
                saleNumber = new SaleNumber {
                    OrganizationId = organization.Id,
                    Value =
                        string.Format(
                            "{0}{1}{2}",
                            organization.Code,
                            currentMonth,
                            string.Format("{0:D8}",
                                Convert.ToInt32(
                                    lastSaleNumber.Value.Substring(
                                        lastSaleNumber.Organization.Code.Length + currentMonth.Length,
                                        lastSaleNumber.Value.Length - (lastSaleNumber.Organization.Code.Length + currentMonth.Length))) + 1)
                        )
                };
            else
                saleNumber = new SaleNumber {
                    OrganizationId = organization.Id,
                    Value = $"{organization.Code}{currentMonth}{string.Format("{0:D8}", 1)}"
                };

            message.Sale.SaleNumberId = saleNumberRepository.Add(saleNumber);
        }

        if (message.Sale.Order != null) {
            message.Sale.Order.ClientAgreementId = message.Sale.ClientAgreementId;
            message.Sale.Order.UserId = message.Sale.UserId;

            long orderId = saleRepositoriesFactory.NewOrderRepository(connection).Add(message.Sale.Order);

            message.Sale.OrderId = orderId;
        }

        long saleId = saleRepository.Add(message.Sale);

        Sale saleFromDb = saleRepository.GetByIdWithAgreement(saleId);

        return saleFromDb;
    }

    private static Sale CreateNewBill(
        Sale sale,
        Guid userNetId,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IUserRepositoriesFactory userRepositoryFactory,
        IDbConnection connection,
        ISaleRepository saleRepository) {
        ISaleNumberRepository saleNumberRepository = saleRepositoriesFactory.NewSaleNumberRepository(connection);

        sale.BaseLifeCycleStatus = new BaseLifeCycleStatus { SaleLifeCycleType = SaleLifeCycleType.New };

        sale.BaseLifeCycleStatusId = saleRepositoriesFactory
            .NewBaseLifeCycleStatusRepository(connection)
            .Add(sale.BaseLifeCycleStatus);

        sale.BaseSalePaymentStatus = new BaseSalePaymentStatus { SalePaymentStatusType = SalePaymentStatusType.NotPaid };

        sale.BaseSalePaymentStatusId = saleRepositoriesFactory
            .NewBaseSalePaymentStatusRepository(connection)
            .Add(sale.BaseSalePaymentStatus);

        if (!userNetId.Equals(Guid.Empty)) {
            User user = userRepositoryFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(userNetId);

            if (user != null) sale.UserId = user.Id;
        }

        if (sale.SaleInvoiceDocument != null) {
            if (sale.SaleInvoiceDocument.IsNew()) {
                sale.SaleInvoiceDocumentId = saleRepositoriesFactory.NewSaleInvoiceDocumentRepository(connection).Add(sale.SaleInvoiceDocument);
            } else {
                saleRepositoriesFactory.NewSaleInvoiceDocumentRepository(connection).Update(sale.SaleInvoiceDocument);

                sale.SaleInvoiceDocumentId = sale.SaleInvoiceDocument.Id;
            }
        }

        sale.ClientAgreementId = sale.ClientAgreement.Id;

        if (sale.ClientAgreement.Agreement?.Organization != null) {
            SaleNumber lastSaleNumber = saleNumberRepository.GetLastRecordByOrganizationNetId(sale.ClientAgreement.Agreement.Organization.NetUid);
            SaleNumber saleNumber;

            Organization organization = sale.ClientAgreement.Agreement.Organization;
            string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

            if (lastSaleNumber != null && DateTime.Now.Year.Equals(lastSaleNumber.Created.Year))
                saleNumber = new SaleNumber {
                    OrganizationId = organization.Id,
                    Value =
                        string.Format(
                            "{0}{1}{2}",
                            organization.Code,
                            currentMonth,
                            string.Format("{0:D8}",
                                Convert.ToInt32(
                                    lastSaleNumber.Value.Substring(
                                        lastSaleNumber.Organization.Code.Length + currentMonth.Length,
                                        lastSaleNumber.Value.Length - (lastSaleNumber.Organization.Code.Length + currentMonth.Length))) + 1)
                        )
                };
            else
                saleNumber = new SaleNumber {
                    OrganizationId = organization.Id,
                    Value = $"{organization.Code}{currentMonth}{string.Format("{0:D8}", 1)}"
                };

            sale.SaleNumberId = saleNumberRepository.Add(saleNumber);
        }

        sale.Order.ClientAgreementId = sale.ClientAgreementId;
        sale.Order.UserId = sale.UserId;

        long orderId = saleRepositoriesFactory.NewOrderRepository(connection).Add(sale.Order);

        sale.OrderId = orderId;

        foreach (OrderItem orderItem in sale.Order.OrderItems) orderItem.OrderId = orderId;

        saleRepositoriesFactory.NewOrderItemRepository(connection).Add(sale.Order.OrderItems);

        long saleId = saleRepository.Add(sale);

        return saleRepository.GetByIdWithAgreement(saleId);
    }

    private static void SaveExchangeRates(
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoryFactory,
        IDbConnection connection,
        long saleId) {
        List<SaleExchangeRate> saleExchangeRates =
            exchangeRateRepositoryFactory
                .NewExchangeRateRepository(connection)
                .GetAllByCulture()
                .Select(exchangeRate =>
                    new SaleExchangeRate {
                        ExchangeRateId = exchangeRate.Id,
                        Value = exchangeRate.Amount,
                        SaleId = saleId
                    }
                ).ToList();

        saleRepositoriesFactory.NewSaleExchangeRateRepository(connection).Add(saleExchangeRates);
    }
}