using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.Sales.SaleMerges;
using GBA.Domain.Messages.Auditing;
using GBA.Domain.Messages.Sales;
using GBA.Domain.Messages.SchedulerTasks;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.SchedulerTasks;

public sealed class MergeSalesTaskActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoryFactory;

    public MergeSalesTaskActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoryFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoryFactory = userRepositoryFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;

        ReceiveAsync<MergeAllSalesMessage>(ProcessMergeAllSalesMessageAsync);
    }

    private async Task ProcessMergeAllSalesMessageAsync(MergeAllSalesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            await MergeAllSalesForAllClients(connection, ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR));

            string logFilePath = $"{ConfigurationManager.EnvironmentRootPath}\\Logs\\merge_log.txt";

            string logData = string.Format("\r\n Operation: SUCCESS \r\n Finished at {0} UTC \r\n", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));

            File.AppendAllText(logFilePath, logData);
        } catch (Exception exc) {
            string logFilePath = $"{ConfigurationManager.EnvironmentRootPath}\\merge_log.txt";

            string format = "\r\n Operation: FAILED \r\n Finished at {0} UTC \r\n Exception: \n {1} \r\n InnerException: \r\n {2} \r\n StackTrace: \r\n {3} \r\n";

            string logData = string.Format(format, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture), exc.Message, exc.InnerException?.Message, exc.StackTrace);

            File.AppendAllText(logFilePath, logData);
        }
    }

    private Task MergeAllSalesForAllClients(
        IDbConnection connection,
        IActorRef auditActorRef) {
        return Task.Factory.StartNew(() => {
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            ISaleNumberRepository saleNumberRepository = _saleRepositoriesFactory.NewSaleNumberRepository(connection);
            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
            IOrderItemMergedRepository orderItemMergedRepository = _saleRepositoriesFactory.NewOrderItemMergedRepository(connection);
            IUserRepository userRepository = _userRepositoryFactory.NewUserRepository(connection);

            List<Client> allRootClients = _clientRepositoriesFactory.NewClientRepository(connection).GetAllRootClientsWithoutIncludes();

            foreach (Client client in allRootClients) {
                List<long> toMergeSaleIdsUa = saleRepository.GetAllSaleIdsThatNeedToBeMergedByRootClientNetId(client.NetUid, "uk");
                List<long> toMergeVatSaleIdsUa = saleRepository.GetAllSaleIdsThatNeedToBeMergedByRootClientNetId(client.NetUid, "uk", true);
                List<long> toMergeSaleIdsPl = saleRepository.GetAllSaleIdsThatNeedToBeMergedByRootClientNetId(client.NetUid, "pl");
                List<long> toMergeVatSaleIdsPl = saleRepository.GetAllSaleIdsThatNeedToBeMergedByRootClientNetId(client.NetUid, "pl", true);

                if (toMergeSaleIdsUa.Count > 1) {
                    MergeProvidedSales(
                        connection,
                        auditActorRef,
                        _clientRepositoriesFactory,
                        _saleRepositoriesFactory,
                        _userRepositoryFactory,
                        saleRepository,
                        orderItemRepository,
                        saleNumberRepository,
                        productReservationRepository,
                        orderItemMergedRepository,
                        userRepository,
                        client,
                        toMergeSaleIdsUa
                    );
                } else if (toMergeSaleIdsUa.Count.Equals(1)) {
                    Sale sale = saleRepository.GetByIdWithoutIncludes(toMergeSaleIdsUa.First());

                    saleRepository.SetNewUpdatedDate(sale);
                }

                if (toMergeVatSaleIdsUa.Count > 1) {
                    MergeProvidedSales(
                        connection,
                        auditActorRef,
                        _clientRepositoriesFactory,
                        _saleRepositoriesFactory,
                        _userRepositoryFactory,
                        saleRepository,
                        orderItemRepository,
                        saleNumberRepository,
                        productReservationRepository,
                        orderItemMergedRepository,
                        userRepository,
                        client,
                        toMergeVatSaleIdsUa,
                        true
                    );
                } else if (toMergeVatSaleIdsUa.Count.Equals(1)) {
                    Sale sale = saleRepository.GetByIdWithoutIncludes(toMergeVatSaleIdsUa.First());

                    saleRepository.SetNewUpdatedDate(sale);
                }

                if (toMergeSaleIdsPl.Count > 1) {
                    MergeProvidedSales(
                        connection,
                        auditActorRef,
                        _clientRepositoriesFactory,
                        _saleRepositoriesFactory,
                        _userRepositoryFactory,
                        saleRepository,
                        orderItemRepository,
                        saleNumberRepository,
                        productReservationRepository,
                        orderItemMergedRepository,
                        userRepository,
                        client,
                        toMergeSaleIdsPl
                    );
                } else if (toMergeSaleIdsPl.Count.Equals(1)) {
                    Sale sale = saleRepository.GetByIdWithoutIncludes(toMergeSaleIdsPl.First());

                    saleRepository.SetNewUpdatedDate(sale);
                }

                if (toMergeVatSaleIdsPl.Count > 1) {
                    MergeProvidedSales(
                        connection,
                        auditActorRef,
                        _clientRepositoriesFactory,
                        _saleRepositoriesFactory,
                        _userRepositoryFactory,
                        saleRepository,
                        orderItemRepository,
                        saleNumberRepository,
                        productReservationRepository,
                        orderItemMergedRepository,
                        userRepository,
                        client,
                        toMergeVatSaleIdsPl,
                        true
                    );
                } else if (toMergeVatSaleIdsPl.Count.Equals(1)) {
                    Sale sale = saleRepository.GetByIdWithoutIncludes(toMergeVatSaleIdsPl.First());

                    saleRepository.SetNewUpdatedDate(sale);
                }
            }
        }, TaskCreationOptions.LongRunning);
    }

    private static void MergeProvidedSales(
        IDbConnection connection,
        IActorRef auditActorRef,
        IClientRepositoriesFactory clientRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IUserRepositoriesFactory userRepositoryFactory,
        ISaleRepository saleRepository,
        IOrderItemRepository orderItemRepository,
        ISaleNumberRepository saleNumberRepository,
        IProductReservationRepository productReservationRepository,
        IOrderItemMergedRepository orderItemMergedRepository,
        IUserRepository userRepository,
        Client client,
        List<long> toMergeSaleIds,
        bool withVat = false) {
        ClientAgreement clientAgreement = clientRepositoriesFactory.NewClientAgreementRepository(connection).GetActiveByRootClientNetId(client.NetUid, withVat);

        Sale newSale = new() {
            ClientAgreement = clientAgreement,
            Order = new Order {
                OrderSource = OrderSource.Local
            }
        };

        Guid userNetId;

        userNetId = client.ClientManagers.Any() ? client.ClientManagers.First().UserProfile.NetUid : userRepository.GetFirstSalesManagerNetUid();

        newSale = CreateNewSaleWithStatusesOnly(new AddSaleWithStatusesOnlyMessage(newSale, userNetId),
            saleRepositoriesFactory,
            userRepositoryFactory,
            connection,
            saleRepository,
            saleNumberRepository);

        auditActorRef.Tell(new RetrieveAndStoreAuditDataMessage(userNetId, newSale.NetUid, "Sale", newSale.BaseLifeCycleStatus));

        List<Sale> salesToMerge = saleRepository.GetAllByIds(toMergeSaleIds);

        List<SaleMerged> mergedSales = new();
        List<OrderItemMerged> mergedOrderItems = new();

        foreach (Sale sale in salesToMerge) {
            sale.IsMerged = true;

            mergedSales.Add(
                new SaleMerged {
                    InputSaleId = sale.Id,
                    OutputSaleId = newSale.Id
                }
            );

            foreach (OrderItem orderItem in sale.Order.OrderItems) {
                if (!orderItem.OrderId.HasValue) continue;

                long orderItemId = orderItemRepository.Add(new OrderItem {
                    Comment = orderItem.Comment,
                    IsValidForCurrentSale = orderItem.IsValidForCurrentSale,
                    OrderId = newSale.OrderId,
                    ProductId = orderItem.ProductId,
                    Qty = orderItem.Qty,
                    UserId = orderItem.UserId,
                    Vat = orderItem.Vat
                });

                IEnumerable<ProductReservation> productReservations =
                    productReservationRepository
                        .GetByOrderItemId(
                            orderItem.Id
                        );

                foreach (ProductReservation reservation in productReservations) {
                    reservation.OrderItemId = orderItemId;

                    productReservationRepository.Update(reservation);
                }

                mergedOrderItems.Add(new OrderItemMerged {
                    OldOrderId = orderItem.OrderId.Value,
                    OrderItemId = orderItemId,
                    OldOrderItemId = orderItem.Id
                });
            }
        }

        saleRepository.Update(salesToMerge);

        orderItemMergedRepository.Add(mergedOrderItems);

        saleRepositoriesFactory.NewSaleMergedRepository(connection).Add(mergedSales);
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

            if (lastSaleNumber != null && DateTime.Now.Year.Equals(lastSaleNumber.Created.Year))
                saleNumber = new SaleNumber {
                    OrganizationId = organization.Id,
                    Value =
                        string.Format(
                            "{0}{1}",
                            organization.Code,
                            string.Format(
                                "{0:D10}",
                                Convert.ToInt32(lastSaleNumber.Value.Substring(organization.Code.Length, lastSaleNumber.Value.Length - organization.Code.Length)) + 1
                            )
                        )
                };
            else
                saleNumber = new SaleNumber {
                    OrganizationId = organization.Id,
                    Value = $"{organization.Code}{string.Format("{0:D10}", 1)}"
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
}