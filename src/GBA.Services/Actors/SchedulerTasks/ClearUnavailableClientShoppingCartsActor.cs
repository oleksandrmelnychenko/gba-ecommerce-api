using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Messages.SchedulerTasks;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Services.Actors.SchedulerTasks;

public sealed class ClearUnavailableClientShoppingCartsActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;

    public ClearUnavailableClientShoppingCartsActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;

        ReceiveAsync<ClearUnavailableClientShoppingCartsMessage>(ProcessClearUnavailableClientShoppingCartsMessageAsync);

        ReceiveAsync<ClearExpiredOrderItemsInClientShoppingCartsMessage>(ProcessClearExpiredOrderItemsInClientShoppingCartsMessageAsync);
    }

    private async Task ProcessClearExpiredOrderItemsInClientShoppingCartsMessageAsync(ClearExpiredOrderItemsInClientShoppingCartsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            await ClearAllExpiredOrderItemsInClientShoppingCarts(connection);

            string logFilePath = $"{ConfigurationManager.EnvironmentRootPath}\\Logs\\carts_clear_log.txt";

            string logData = string.Format("\r\n Operation: SUCCESS \r\n Finished at {0} UTC \r\n", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));

            File.AppendAllText(logFilePath, logData);
        } catch (Exception exc) {
            string logFilePath = $"{ConfigurationManager.EnvironmentRootPath}\\carts_clear_log.txt";

            string format = "\r\n Operation: FAILED \r\n Finished at {0} UTC \r\n Exception: \n {1} \r\n InnerException: \r\n {2} \r\n StackTrace: \r\n {3} \r\n";

            string logData = string.Format(format, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture), exc.Message, exc.InnerException?.Message, exc.StackTrace);

            File.AppendAllText(logFilePath, logData);
        }
    }

    private Task ClearAllExpiredOrderItemsInClientShoppingCarts(IDbConnection connection) {
        return Task.Factory.StartNew(() => {
            IClientShoppingCartRepository clientShoppingCartRepository = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

            List<ClientShoppingCart> cartsToDelete = clientShoppingCartRepository.GetAllExistingExpiredClientShoppingCarts();

            foreach (ClientShoppingCart cart in cartsToDelete) {
                foreach (OrderItem orderItem in cart.OrderItems) {
                    IEnumerable<ProductReservation> reservations =
                        productReservationRepository
                            .GetAllByOrderItemIdWithAvailability(
                                orderItem.Id
                            );

                    foreach (ProductReservation reservation in reservations) {
                        reservation.ProductAvailability.Amount += reservation.Qty;

                        productAvailabilityRepository.Update(reservation.ProductAvailability);

                        productReservationRepository.Delete(reservation.NetUid);
                    }
                }

                clientShoppingCartRepository.Remove(cart.NetUid);
            }
        }, TaskCreationOptions.LongRunning);
    }

    private async Task ProcessClearUnavailableClientShoppingCartsMessageAsync(ClearUnavailableClientShoppingCartsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            await ClearAllUnavailableClientShoppingCarts(connection);

            string logFilePath = $"{ConfigurationManager.EnvironmentRootPath}\\Logs\\carts_clear_log.txt";

            string logData = string.Format("\r\n Operation: SUCCESS \r\n Finished at {0} UTC \r\n", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));

            File.AppendAllText(logFilePath, logData);
        } catch (Exception exc) {
            string logFilePath = $"{ConfigurationManager.EnvironmentRootPath}\\carts_clear_log.txt";

            string format = "\r\n Operation: FAILED \r\n Finished at {0} UTC \r\n Exception: \n {1} \r\n InnerException: \r\n {2} \r\n StackTrace: \r\n {3} \r\n";

            string logData = string.Format(format, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture), exc.Message, exc.InnerException?.Message, exc.StackTrace);

            File.AppendAllText(logFilePath, logData);
        }
    }

    private Task ClearAllUnavailableClientShoppingCarts(
        IDbConnection connection) {
        return Task.Factory.StartNew(() => {
            IClientShoppingCartRepository clientShoppingCartRepository = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

            List<ClientShoppingCart> cartsToDelete = clientShoppingCartRepository.GetAllExistingUnavailableCarts();

            foreach (ClientShoppingCart cart in cartsToDelete) {
                foreach (OrderItem orderItem in cart.OrderItems) {
                    IEnumerable<ProductReservation> reservations =
                        productReservationRepository
                            .GetAllByOrderItemIdWithAvailability(
                                orderItem.Id
                            );

                    foreach (ProductReservation reservation in reservations) {
                        reservation.ProductAvailability.Amount += reservation.Qty;

                        productAvailabilityRepository.Update(reservation.ProductAvailability);

                        productReservationRepository.Delete(reservation.NetUid);
                    }
                }

                clientShoppingCartRepository.Remove(cart.NetUid);
            }
        }, TaskCreationOptions.LongRunning);
    }
}