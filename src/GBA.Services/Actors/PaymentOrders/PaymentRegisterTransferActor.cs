using System;
using System.Data;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Messages.PaymentOrders.PaymentRegisters;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.PaymentOrders;

public sealed class PaymentRegisterTransferActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public PaymentRegisterTransferActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;

        Receive<AddNewPaymentRegisterTransferMessage>(ProcessAddNewPaymentRegisterTransferMessage);

        Receive<UpdatePaymentRegisterTransferMessage>(ProcessUpdatePaymentRegisterTransferMessage);

        Receive<CancelPaymentRegisterTransferByNetIdMessage>(ProcessCancelPaymentRegisterTransferByNetIdMessage);
    }

    private void ProcessAddNewPaymentRegisterTransferMessage(AddNewPaymentRegisterTransferMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.PaymentRegisterTransfer == null) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterTransferResourceNames.PAYMENT_REGISTER_TRANSFER_EMPTY, false, null));
        } else if (!message.PaymentRegisterTransfer.IsNew()) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterTransferResourceNames.PAYMENT_REGISTER_TRANSFER_EMPTY, false, null));
        } else if (message.PaymentRegisterTransfer.FromPaymentCurrencyRegister == null && message.PaymentRegisterTransfer.FromPaymentCurrencyRegisterId.Equals(0)) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterResourceNames.FROM_REGISTER_NOT_SPECIFIED, false, null));
        } else if (message.PaymentRegisterTransfer.ToPaymentCurrencyRegister == null && message.PaymentRegisterTransfer.ToPaymentCurrencyRegisterId.Equals(0)) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterResourceNames.TO_REGISTER_NOT_SPECIFIED, false, null));
        } else if (message.PaymentRegisterTransfer.Amount <= decimal.Zero) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterResourceNames.AMOUNT_NOT_SPECIFIED, false, null));
        } else if (message.PaymentRegisterTransfer.FromPaymentCurrencyRegister != null && message.PaymentRegisterTransfer.FromPaymentCurrencyRegister.IsNew()) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(
                PaymentRegisterTransferResourceNames.PAYMENT_CURRENCY_REGISTER_IS_NEW, false, null));
        } else if (message.PaymentRegisterTransfer.ToPaymentCurrencyRegister != null && message.PaymentRegisterTransfer.ToPaymentCurrencyRegister.IsNew()) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(
                PaymentRegisterTransferResourceNames.PAYMENT_CURRENCY_REGISTER_IS_NEW, false, null));
        } else {
            if (message.PaymentRegisterTransfer.FromPaymentCurrencyRegister != null)
                message.PaymentRegisterTransfer.FromPaymentCurrencyRegisterId = message.PaymentRegisterTransfer.FromPaymentCurrencyRegister.Id;
            if (message.PaymentRegisterTransfer.ToPaymentCurrencyRegister != null)
                message.PaymentRegisterTransfer.ToPaymentCurrencyRegisterId = message.PaymentRegisterTransfer.ToPaymentCurrencyRegister.Id;
            if (message.PaymentRegisterTransfer.FromPaymentCurrencyRegisterId.Equals(message.PaymentRegisterTransfer.ToPaymentCurrencyRegisterId)) {
                Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterTransferResourceNames.CAN_NOT_TRANSFER_MONEY, false, null));
            } else {
                IPaymentCurrencyRegisterRepository paymentCurrencyRegisterRepository =
                    _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection);

                PaymentCurrencyRegister fromToPaymentCurrencyRegister =
                    paymentCurrencyRegisterRepository.GetById(message.PaymentRegisterTransfer.FromPaymentCurrencyRegisterId);
                PaymentCurrencyRegister toToPaymentCurrencyRegister =
                    paymentCurrencyRegisterRepository.GetById(message.PaymentRegisterTransfer.ToPaymentCurrencyRegisterId);

                if (fromToPaymentCurrencyRegister == null || toToPaymentCurrencyRegister == null) {
                    Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterResourceNames.TO_CURRENCY_NOT_SPECIFIED, false, null));
                } else if (!fromToPaymentCurrencyRegister.CurrencyId.Equals(toToPaymentCurrencyRegister.CurrencyId)) {
                    Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterResourceNames.FROM_CURRENCY_NOT_SPECIFIED, false, null));
                } else if (fromToPaymentCurrencyRegister.Amount < message.PaymentRegisterTransfer.Amount) {
                    Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterResourceNames.INSUFFICIENT_OF_FUNDS, false, null));
                } else {
                    IPaymentRegisterTransferRepository paymentRegisterTransferRepository =
                        _paymentOrderRepositoriesFactory.NewPaymentRegisterTransferRepository(connection);

                    message.PaymentRegisterTransfer.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

                    PaymentRegisterTransfer lastRecord = paymentRegisterTransferRepository.GetLastRecord();

                    if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                        message.PaymentRegisterTransfer.Number = string.Format("{0:D5}", 1);
                    else
                        message.PaymentRegisterTransfer.Number = string.Format("{0:D5}", Convert.ToInt32(lastRecord.Number) + 1);

                    if (message.PaymentRegisterTransfer.FromDate.Year.Equals(1)) message.PaymentRegisterTransfer.FromDate = DateTime.UtcNow;

                    message.PaymentRegisterTransfer.Id = paymentRegisterTransferRepository.Add(message.PaymentRegisterTransfer);

                    if (message.PaymentRegisterTransfer.PaymentMovementOperation != null &&
                        (!message.PaymentRegisterTransfer.PaymentMovementOperation.PaymentMovementId.Equals(0) ||
                         message.PaymentRegisterTransfer.PaymentMovementOperation.PaymentMovement != null)
                       ) {
                        if (message.PaymentRegisterTransfer.PaymentMovementOperation.PaymentMovement != null)
                            message.PaymentRegisterTransfer.PaymentMovementOperation.PaymentMovementId =
                                message.PaymentRegisterTransfer.PaymentMovementOperation.PaymentMovement.Id;

                        message.PaymentRegisterTransfer.PaymentMovementOperation.PaymentRegisterTransferId = message.PaymentRegisterTransfer.Id;

                        _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection)
                            .Add(message.PaymentRegisterTransfer.PaymentMovementOperation);
                    }

                    fromToPaymentCurrencyRegister.Amount = Math.Round(fromToPaymentCurrencyRegister.Amount - message.PaymentRegisterTransfer.Amount, 2);
                    toToPaymentCurrencyRegister.Amount = Math.Round(toToPaymentCurrencyRegister.Amount + message.PaymentRegisterTransfer.Amount, 2);

                    paymentCurrencyRegisterRepository.UpdateAmount(fromToPaymentCurrencyRegister);
                    paymentCurrencyRegisterRepository.UpdateAmount(toToPaymentCurrencyRegister);

                    Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterResourceNames.TRANSFERED_SUCCESSFUL, true,
                        paymentRegisterTransferRepository.GetById(message.PaymentRegisterTransfer.Id)));
                }
            }
        }
    }

    private void ProcessUpdatePaymentRegisterTransferMessage(UpdatePaymentRegisterTransferMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.PaymentRegisterTransfer == null) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterTransferResourceNames.PAYMENT_REGISTER_TRANSFER_EMPTY, false, null));
        } else if (message.PaymentRegisterTransfer.IsNew()) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterTransferResourceNames.PAYMENT_REGISTER_TRANSFER_EMPTY, false, null));
        } else {
            IPaymentRegisterTransferRepository paymentRegisterTransferRepository = _paymentOrderRepositoriesFactory.NewPaymentRegisterTransferRepository(connection);

            message.PaymentRegisterTransfer.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

            if (message.PaymentRegisterTransfer.FromDate.Year.Equals(1)) message.PaymentRegisterTransfer.FromDate = DateTime.UtcNow;

            if (message.PaymentRegisterTransfer.PaymentMovementOperation != null) {
                if (message.PaymentRegisterTransfer.PaymentMovementOperation.IsNew()) {
                    if (message.PaymentRegisterTransfer.PaymentMovementOperation.PaymentMovement != null)
                        message.PaymentRegisterTransfer.PaymentMovementOperation.PaymentMovementId =
                            message.PaymentRegisterTransfer.PaymentMovementOperation.PaymentMovement.Id;

                    message.PaymentRegisterTransfer.PaymentMovementOperation.PaymentRegisterTransferId = message.PaymentRegisterTransfer.Id;

                    _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection).Add(message.PaymentRegisterTransfer.PaymentMovementOperation);
                } else {
                    if (message.PaymentRegisterTransfer.PaymentMovementOperation.PaymentMovement != null)
                        message.PaymentRegisterTransfer.PaymentMovementOperation.PaymentMovementId =
                            message.PaymentRegisterTransfer.PaymentMovementOperation.PaymentMovement.Id;

                    _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection).Update(message.PaymentRegisterTransfer.PaymentMovementOperation);
                }
            }

            paymentRegisterTransferRepository.Update(message.PaymentRegisterTransfer);

            Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterResourceNames.TRANSFER_UPDATED, true,
                paymentRegisterTransferRepository.GetById(message.PaymentRegisterTransfer.Id)));
        }
    }

    private void ProcessCancelPaymentRegisterTransferByNetIdMessage(CancelPaymentRegisterTransferByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPaymentRegisterTransferRepository paymentRegisterTransferRepository = _paymentOrderRepositoriesFactory.NewPaymentRegisterTransferRepository(connection);

        PaymentRegisterTransfer transfer = paymentRegisterTransferRepository.GetByNetId(message.NetId);

        if (transfer != null) {
            if (transfer.IsCanceled) {
                Sender.Tell(new Tuple<bool, string, PaymentRegisterTransfer>(false, PaymentRegisterResourceNames.TRANSFER_ALREADY_CANCELED, transfer));
            } else {
                IPaymentCurrencyRegisterRepository paymentCurrencyRegisterRepository =
                    _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection);

                if (transfer.Created.Year.Equals(DateTime.Now.Year) && transfer.Created.Month.Equals(DateTime.Now.Month) &&
                    transfer.Created.Day.Equals(DateTime.Now.Day)) {
                    decimal amountBeforeOperation =
                        paymentCurrencyRegisterRepository.GetPaymentCurrencyRegisterAmountByIdAtSpecifiedDate(transfer.Created, transfer.ToPaymentCurrencyRegisterId);

                    decimal amountOfFutureOperations = paymentCurrencyRegisterRepository
                        .GetAmountOfAllOperationsAfterDateByIds(
                            transfer.Created,
                            transfer.ToPaymentCurrencyRegister.Id,
                            transfer.ToPaymentCurrencyRegister.CurrencyId,
                            transfer.ToPaymentCurrencyRegister.PaymentRegisterId
                        );

                    if (amountBeforeOperation + amountOfFutureOperations >= decimal.Zero) {
                        transfer.ToPaymentCurrencyRegister.Amount = Math.Round(transfer.ToPaymentCurrencyRegister.Amount - transfer.Amount, 2);
                        transfer.FromPaymentCurrencyRegister.Amount = Math.Round(transfer.FromPaymentCurrencyRegister.Amount + transfer.Amount, 2);

                        if (transfer.ToPaymentCurrencyRegister.Amount >= decimal.Zero) {
                            paymentCurrencyRegisterRepository.UpdateAmount(transfer.FromPaymentCurrencyRegister);
                            paymentCurrencyRegisterRepository.UpdateAmount(transfer.ToPaymentCurrencyRegister);

                            paymentRegisterTransferRepository.SetCanceled(message.NetId);

                            transfer.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

                            paymentRegisterTransferRepository.Update(transfer);

                            Sender.Tell(new Tuple<bool, string, PaymentRegisterTransfer>(true, PaymentRegisterResourceNames.TRANSFER_CANCELED,
                                paymentRegisterTransferRepository.GetByNetId(message.NetId)));
                        } else {
                            Sender.Tell(new Tuple<bool, string, PaymentRegisterTransfer>(false, PaymentRegisterResourceNames.CANCEL_FAILED_INSUFFICIENT_OF_FUNDS,
                                paymentRegisterTransferRepository.GetByNetId(message.NetId)));
                        }
                    } else {
                        Sender.Tell(new Tuple<bool, string, PaymentRegisterTransfer>(false, PaymentRegisterResourceNames.CANCEL_FAILED_INSUFFICIENT_OF_FUNDS,
                            paymentRegisterTransferRepository.GetByNetId(message.NetId)));
                    }
                } else {
                    Sender.Tell(new Tuple<bool, string, PaymentRegisterTransfer>(false, PaymentRegisterResourceNames.CANCEL_OLD_TRANSFER_NOT_ALLOWED,
                        paymentRegisterTransferRepository.GetByNetId(message.NetId)));
                }
            }
        } else {
            Sender.Tell(new Tuple<bool, string, PaymentRegisterTransfer>(false, PaymentRegisterTransferResourceNames.TRANSFER_EMPTY, null));
        }
    }
}