using System;
using System.Data;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Messages.PaymentOrders.PaymentRegisters;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.PaymentOrders;

public sealed class PaymentRegisterCurrencyExchangeActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public PaymentRegisterCurrencyExchangeActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;

        Receive<AddNewPaymentRegisterCurrencyExchangeMessage>(ProcessAddNewPaymentRegisterCurrencyExchangeMessage);

        Receive<UpdatePaymentRegisterCurrencyExchangeMessage>(ProcessUpdatePaymentRegisterCurrencyExchangeMessage);

        Receive<CancelPaymentRegisterCurrencyExchangeByNetIdMessage>(ProcessCancelPaymentRegisterCurrencyExchangeByNetIdMessage);
    }

    private void ProcessAddNewPaymentRegisterCurrencyExchangeMessage(AddNewPaymentRegisterCurrencyExchangeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.PaymentRegisterCurrencyExchange == null) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterCurrencyExchange>(PaymentRegisterCurrencyExchangeResourceNames.PAYMENT_REGISTER_CURRENCY_EXCHANGE_EMPTY,
                false, null));
        } else if (!message.PaymentRegisterCurrencyExchange.IsNew()) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterCurrencyExchange>(PaymentRegisterCurrencyExchangeResourceNames.PAYMENT_REGISTER_CURRENCY_EXCHANGE_EMPTY,
                false, null));
        } else if (message.PaymentRegisterCurrencyExchange.FromPaymentCurrencyRegister == null &&
                   message.PaymentRegisterCurrencyExchange.FromPaymentCurrencyRegisterId.Equals(0)) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterCurrencyExchange>(PaymentRegisterResourceNames.FROM_CURRENCY_NOT_SPECIFIED, false, null));
        } else if (message.PaymentRegisterCurrencyExchange.ToPaymentCurrencyRegister == null &&
                   message.PaymentRegisterCurrencyExchange.ToPaymentCurrencyRegisterId.Equals(0)) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterCurrencyExchange>(PaymentRegisterResourceNames.TO_CURRENCY_NOT_SPECIFIED, false, null));
        } else if (message.PaymentRegisterCurrencyExchange.Amount <= decimal.Zero) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterCurrencyExchange>(PaymentRegisterResourceNames.AMOUNT_NOT_SPECIFIED, false, null));
        } else if (message.PaymentRegisterCurrencyExchange.ExchangeRate.Equals(decimal.Zero)) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterCurrencyExchange>(PaymentRegisterResourceNames.EXCHANGE_RATE_NOT_SPECIFIED, false, null));
        } else if (message.PaymentRegisterCurrencyExchange.FromPaymentCurrencyRegister != null &&
                   message.PaymentRegisterCurrencyExchange.FromPaymentCurrencyRegister.IsNew()) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterCurrencyExchange>(
                PaymentRegisterCurrencyExchangeResourceNames.PAYMENT_REGISTER_CURRENCY_EXCHANGE_EMPTY, false, null));
        } else if (message.PaymentRegisterCurrencyExchange.ToPaymentCurrencyRegister != null &&
                   message.PaymentRegisterCurrencyExchange.ToPaymentCurrencyRegister.IsNew()) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterCurrencyExchange>(
                PaymentRegisterCurrencyExchangeResourceNames.PAYMENT_REGISTER_CURRENCY_EXCHANGE_EMPTY, false, null));
        } else {
            if (message.PaymentRegisterCurrencyExchange.FromPaymentCurrencyRegister != null)
                message.PaymentRegisterCurrencyExchange.FromPaymentCurrencyRegisterId = message.PaymentRegisterCurrencyExchange.FromPaymentCurrencyRegister.Id;
            if (message.PaymentRegisterCurrencyExchange.ToPaymentCurrencyRegister != null)
                message.PaymentRegisterCurrencyExchange.ToPaymentCurrencyRegisterId = message.PaymentRegisterCurrencyExchange.ToPaymentCurrencyRegister.Id;
            if (message.PaymentRegisterCurrencyExchange.CurrencyTrader != null) {
                if (message.PaymentRegisterCurrencyExchange.CurrencyTrader.IsNew())
                    message.PaymentRegisterCurrencyExchange.CurrencyTraderId = _currencyRepositoriesFactory.NewCurrencyTraderRepository(connection)
                        .Add(message.PaymentRegisterCurrencyExchange.CurrencyTrader);
                else
                    message.PaymentRegisterCurrencyExchange.CurrencyTraderId = message.PaymentRegisterCurrencyExchange.CurrencyTrader.Id;
            }

            if (message.PaymentRegisterCurrencyExchange.FromPaymentCurrencyRegisterId.Equals(message.PaymentRegisterCurrencyExchange.ToPaymentCurrencyRegisterId)) {
                Sender.Tell(new Tuple<string, bool, PaymentRegisterCurrencyExchange>(PaymentRegisterCurrencyExchangeResourceNames.CAN_NOT_CONVERT_MONEY, false, null));
            } else {
                IPaymentCurrencyRegisterRepository paymentCurrencyRegisterRepository =
                    _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection);

                PaymentCurrencyRegister fromToPaymentCurrencyRegister =
                    paymentCurrencyRegisterRepository.GetById(message.PaymentRegisterCurrencyExchange.FromPaymentCurrencyRegisterId);
                PaymentCurrencyRegister toToPaymentCurrencyRegister =
                    paymentCurrencyRegisterRepository.GetById(message.PaymentRegisterCurrencyExchange.ToPaymentCurrencyRegisterId);

                if (fromToPaymentCurrencyRegister == null || toToPaymentCurrencyRegister == null) {
                    Sender.Tell(new Tuple<string, bool, PaymentRegisterCurrencyExchange>(
                        PaymentRegisterCurrencyExchangeResourceNames.PAYMENT_REGISTER_CURRENCY_EXCHANGE_EMPTY, false, null));
                } else if (fromToPaymentCurrencyRegister.CurrencyId.Equals(toToPaymentCurrencyRegister.CurrencyId)) {
                    Sender.Tell(new Tuple<string, bool, PaymentRegisterCurrencyExchange>(PaymentRegisterCurrencyExchangeResourceNames.DIFFERENT_CURRENCIES,
                        false, null));
                } else if (fromToPaymentCurrencyRegister.Amount < message.PaymentRegisterCurrencyExchange.Amount) {
                    Sender.Tell(new Tuple<string, bool, PaymentRegisterCurrencyExchange>(PaymentRegisterResourceNames.INSUFFICIENT_OF_FUNDS, false, null));
                } else {
                    IPaymentRegisterCurrencyExchangeRepository paymentRegisterCurrencyExchangeRepository =
                        _paymentOrderRepositoriesFactory.NewPaymentRegisterCurrencyExchangeRepository(connection);

                    message.PaymentRegisterCurrencyExchange.UserId =
                        _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

                    PaymentRegisterCurrencyExchange lastRecord = paymentRegisterCurrencyExchangeRepository.GetLastRecord();

                    if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                        message.PaymentRegisterCurrencyExchange.Number = string.Format("{0:D5}", 1);
                    else
                        message.PaymentRegisterCurrencyExchange.Number = string.Format("{0:D5}", Convert.ToInt32(lastRecord.Number) + 1);

                    if (message.PaymentRegisterCurrencyExchange.FromDate.Year.Equals(1)) message.PaymentRegisterCurrencyExchange.FromDate = DateTime.UtcNow;

                    message.PaymentRegisterCurrencyExchange.Id = paymentRegisterCurrencyExchangeRepository.Add(message.PaymentRegisterCurrencyExchange);

                    if (message.PaymentRegisterCurrencyExchange.PaymentMovementOperation != null &&
                        (!message.PaymentRegisterCurrencyExchange.PaymentMovementOperation.PaymentMovementId.Equals(0) ||
                         message.PaymentRegisterCurrencyExchange.PaymentMovementOperation.PaymentMovement != null)
                       ) {
                        if (message.PaymentRegisterCurrencyExchange.PaymentMovementOperation.PaymentMovement != null)
                            message.PaymentRegisterCurrencyExchange.PaymentMovementOperation.PaymentMovementId =
                                message.PaymentRegisterCurrencyExchange.PaymentMovementOperation.PaymentMovement.Id;

                        message.PaymentRegisterCurrencyExchange.PaymentMovementOperation.PaymentRegisterCurrencyExchangeId = message.PaymentRegisterCurrencyExchange.Id;

                        _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection)
                            .Add(message.PaymentRegisterCurrencyExchange.PaymentMovementOperation);
                    }

                    fromToPaymentCurrencyRegister.Amount = Math.Round(fromToPaymentCurrencyRegister.Amount - message.PaymentRegisterCurrencyExchange.Amount, 2);

                    if (fromToPaymentCurrencyRegister.Currency.Code.ToLower().Equals("uah"))
                        toToPaymentCurrencyRegister.Amount =
                            Math.Round(
                                toToPaymentCurrencyRegister.Amount +
                                Math.Round(message.PaymentRegisterCurrencyExchange.Amount / message.PaymentRegisterCurrencyExchange.ExchangeRate, 2)
                                , 2);
                    else
                        toToPaymentCurrencyRegister.Amount =
                            Math.Round(
                                toToPaymentCurrencyRegister.Amount +
                                Math.Round(message.PaymentRegisterCurrencyExchange.Amount * message.PaymentRegisterCurrencyExchange.ExchangeRate, 2)
                                , 2);

                    paymentCurrencyRegisterRepository.UpdateAmount(fromToPaymentCurrencyRegister);
                    paymentCurrencyRegisterRepository.UpdateAmount(toToPaymentCurrencyRegister);

                    Sender.Tell(new Tuple<string, bool, PaymentRegisterCurrencyExchange>(PaymentRegisterResourceNames.CURRENCY_CHANGED_SUCCESSFULLY, true,
                        paymentRegisterCurrencyExchangeRepository.GetById(message.PaymentRegisterCurrencyExchange.Id)));
                }
            }
        }
    }

    private void ProcessUpdatePaymentRegisterCurrencyExchangeMessage(UpdatePaymentRegisterCurrencyExchangeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.PaymentRegisterCurrencyExchange == null) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterCurrencyExchange>(PaymentRegisterCurrencyExchangeResourceNames.PAYMENT_REGISTER_CURRENCY_EXCHANGE_EMPTY,
                false, null));
        } else if (message.PaymentRegisterCurrencyExchange.IsNew()) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterCurrencyExchange>(PaymentRegisterCurrencyExchangeResourceNames.PAYMENT_REGISTER_CURRENCY_EXCHANGE_EMPTY,
                false, null));
        } else {
            if (message.PaymentRegisterCurrencyExchange.CurrencyTrader != null)
                message.PaymentRegisterCurrencyExchange.CurrencyTraderId = message.PaymentRegisterCurrencyExchange.CurrencyTrader.Id;

            IPaymentRegisterCurrencyExchangeRepository paymentRegisterCurrencyExchangeRepository =
                _paymentOrderRepositoriesFactory.NewPaymentRegisterCurrencyExchangeRepository(connection);

            message.PaymentRegisterCurrencyExchange.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

            if (message.PaymentRegisterCurrencyExchange.PaymentMovementOperation != null) {
                if (message.PaymentRegisterCurrencyExchange.PaymentMovementOperation.IsNew()) {
                    if (message.PaymentRegisterCurrencyExchange.PaymentMovementOperation.PaymentMovement != null)
                        message.PaymentRegisterCurrencyExchange.PaymentMovementOperation.PaymentMovementId =
                            message.PaymentRegisterCurrencyExchange.PaymentMovementOperation.PaymentMovement.Id;

                    message.PaymentRegisterCurrencyExchange.PaymentMovementOperation.PaymentRegisterCurrencyExchangeId = message.PaymentRegisterCurrencyExchange.Id;

                    _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection)
                        .Add(message.PaymentRegisterCurrencyExchange.PaymentMovementOperation);
                } else {
                    if (message.PaymentRegisterCurrencyExchange.PaymentMovementOperation.PaymentMovement != null)
                        message.PaymentRegisterCurrencyExchange.PaymentMovementOperation.PaymentMovementId =
                            message.PaymentRegisterCurrencyExchange.PaymentMovementOperation.PaymentMovement.Id;

                    _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection)
                        .Update(message.PaymentRegisterCurrencyExchange.PaymentMovementOperation);
                }
            }

            paymentRegisterCurrencyExchangeRepository.Update(message.PaymentRegisterCurrencyExchange);

            Sender.Tell(new Tuple<string, bool, PaymentRegisterCurrencyExchange>(PaymentRegisterResourceNames.CURRENCY_EXCHANGE_UPDATED, true,
                paymentRegisterCurrencyExchangeRepository.GetById(message.PaymentRegisterCurrencyExchange.Id)));
        }
    }

    private void ProcessCancelPaymentRegisterCurrencyExchangeByNetIdMessage(CancelPaymentRegisterCurrencyExchangeByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPaymentRegisterCurrencyExchangeRepository paymentRegisterCurrencyExchangeRepository =
            _paymentOrderRepositoriesFactory.NewPaymentRegisterCurrencyExchangeRepository(connection);

        PaymentRegisterCurrencyExchange currencyExchange = paymentRegisterCurrencyExchangeRepository.GetByNetId(message.NetId);

        if (currencyExchange != null) {
            if (currencyExchange.IsCanceled) {
                Sender.Tell(new Tuple<bool, string, PaymentRegisterCurrencyExchange>(false, PaymentRegisterResourceNames.CURRENCY_EXCHANGE_ALREADY_CANCELED,
                    currencyExchange));
            } else {
                IPaymentCurrencyRegisterRepository paymentCurrencyRegisterRepository =
                    _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection);

                if (currencyExchange.Created.Year.Equals(DateTime.Now.Year) && currencyExchange.Created.Month.Equals(DateTime.Now.Month) &&
                    currencyExchange.Created.Day.Equals(DateTime.Now.Day)) {
                    decimal amountBeforeOperation =
                        paymentCurrencyRegisterRepository.GetPaymentCurrencyRegisterAmountByIdAtSpecifiedDate(currencyExchange.Created,
                            currencyExchange.ToPaymentCurrencyRegisterId);

                    decimal amountOfFutureOperations = paymentCurrencyRegisterRepository
                        .GetAmountOfAllOperationsAfterDateByIds(
                            currencyExchange.Created,
                            currencyExchange.ToPaymentCurrencyRegister.Id,
                            currencyExchange.ToPaymentCurrencyRegister.CurrencyId,
                            currencyExchange.ToPaymentCurrencyRegister.PaymentRegisterId
                        );

                    if (amountBeforeOperation + amountOfFutureOperations >= decimal.Zero) {
                        currencyExchange.FromPaymentCurrencyRegister.Amount =
                            Math.Round(currencyExchange.FromPaymentCurrencyRegister.Amount + currencyExchange.Amount, 2);

                        if (currencyExchange.FromPaymentCurrencyRegister.Currency.Code.ToLower().Equals("uah"))
                            currencyExchange.ToPaymentCurrencyRegister.Amount =
                                Math.Round(
                                    currencyExchange.ToPaymentCurrencyRegister.Amount -
                                    Math.Round(currencyExchange.Amount / currencyExchange.ExchangeRate, 2)
                                    , 2);
                        else
                            currencyExchange.ToPaymentCurrencyRegister.Amount =
                                Math.Round(
                                    currencyExchange.ToPaymentCurrencyRegister.Amount -
                                    Math.Round(currencyExchange.Amount * currencyExchange.ExchangeRate, 2)
                                    , 2);

                        if (currencyExchange.ToPaymentCurrencyRegister.Amount >= decimal.Zero) {
                            paymentCurrencyRegisterRepository.UpdateAmount(currencyExchange.FromPaymentCurrencyRegister);
                            paymentCurrencyRegisterRepository.UpdateAmount(currencyExchange.ToPaymentCurrencyRegister);

                            paymentRegisterCurrencyExchangeRepository.SetCanceled(message.NetId);

                            currencyExchange.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

                            paymentRegisterCurrencyExchangeRepository.Update(currencyExchange);

                            Sender.Tell(new Tuple<bool, string, PaymentRegisterCurrencyExchange>(true, PaymentRegisterResourceNames.CURRENCY_EXCHANGE_CANCELED,
                                paymentRegisterCurrencyExchangeRepository.GetByNetId(message.NetId)));
                        } else {
                            Sender.Tell(new Tuple<bool, string, PaymentRegisterCurrencyExchange>(false,
                                PaymentRegisterResourceNames.CANCEL_FAILED_INSUFFICIENT_OF_FUNDS, paymentRegisterCurrencyExchangeRepository.GetByNetId(message.NetId)));
                        }
                    } else {
                        Sender.Tell(new Tuple<bool, string, PaymentRegisterCurrencyExchange>(false, PaymentRegisterResourceNames.CANCEL_FAILED_INSUFFICIENT_OF_FUNDS,
                            paymentRegisterCurrencyExchangeRepository.GetByNetId(message.NetId)));
                    }
                } else {
                    Sender.Tell(new Tuple<bool, string, PaymentRegisterCurrencyExchange>(false, PaymentRegisterResourceNames.CANCEL_OLD_CURRENCY_EXCHANGE_NOT_ALLOWED,
                        paymentRegisterCurrencyExchangeRepository.GetByNetId(message.NetId)));
                }
            }
        } else {
            Sender.Tell(new Tuple<bool, string, PaymentRegisterCurrencyExchange>(false, PaymentRegisterCurrencyExchangeResourceNames.CURRENCY_EXCHANGE_NOT_EXIST,
                null));
        }
    }
}