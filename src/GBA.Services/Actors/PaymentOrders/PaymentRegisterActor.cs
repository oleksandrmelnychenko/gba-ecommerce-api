using System;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Messages.PaymentOrders.PaymentRegisters;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Services.Actors.PaymentOrders;

public sealed class PaymentRegisterActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;

    public PaymentRegisterActor(
        IDbConnectionFactory connectionFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;

        Receive<AddNewPaymentRegisterMessage>(ProcessAddNewPaymentRegisterMessage);

        Receive<UpdatePaymentRegisterMessage>(ProcessUpdatePaymentRegisterMessage);

        Receive<DeletePaymentRegisterByNetIdMessage>(ProcessDeletePaymentRegisterByNetIdMessage);

        Receive<SetActivePaymentRegisterByNetIdMessage>(ProcessSetActivePaymentRegisterByNetIdMessage);

        Receive<SetSelectedPaymentRegisterByNetId>(ProcessSetSelectedPaymentRegisterByNetId);
    }

    private void ProcessAddNewPaymentRegisterMessage(AddNewPaymentRegisterMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (!message.PaymentRegister.PaymentCurrencyRegisters.Any(r => r.IsNew() && (!r.CurrencyId.Equals(0) || r.Currency != null)))
                throw new Exception(PaymentRegisterResourceNames.TO_CURRENCY_NOT_SPECIFIED);

            IPaymentRegisterRepository paymentRegisterRepository = _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection);

            message.PaymentRegister.OrganizationId =
                message.PaymentRegister.Organization?.Id ?? message.PaymentRegister.OrganizationId;

            message.PaymentRegister.Id = paymentRegisterRepository.Add(message.PaymentRegister);

            if (message.PaymentRegister.PaymentCurrencyRegisters.Any(r => r.IsNew() && (!r.CurrencyId.Equals(0) || r.Currency != null))) {
                if (message.PaymentRegister.Type.Equals(PaymentRegisterType.Bank) && message.PaymentRegister.IsActive) {
                    PaymentCurrencyRegister paymentCurrencyRegister =
                        message
                            .PaymentRegister
                            .PaymentCurrencyRegisters
                            .First(r => r.IsNew() && (!r.CurrencyId.Equals(0) || r.Currency != null));

                    paymentRegisterRepository
                        .SetInactiveByOrganizationAndCurrencyIds(
                            message.PaymentRegister.OrganizationId,
                            paymentCurrencyRegister.Currency?.Id ?? paymentCurrencyRegister.CurrencyId
                        );

                    paymentRegisterRepository.SetActiveById(message.PaymentRegister.Id);
                }

                _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection).Add(
                    message
                        .PaymentRegister
                        .PaymentCurrencyRegisters
                        .Where(r => r.IsNew() && (!r.CurrencyId.Equals(0) || r.Currency != null))
                        .Select(r => {
                            r.CurrencyId = r.Currency?.Id ?? r.CurrencyId;
                            r.PaymentRegisterId = message.PaymentRegister.Id;
                            r.InitialAmount = r.Amount;

                            return r;
                        })
                );
            }

            Sender.Tell(paymentRegisterRepository.GetById(message.PaymentRegister.Id));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdatePaymentRegisterMessage(UpdatePaymentRegisterMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPaymentRegisterRepository paymentRegisterRepository = _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection);

        message.PaymentRegister.OrganizationId = message.PaymentRegister.Organization.Id;

        paymentRegisterRepository.Update(message.PaymentRegister);

        if (message.PaymentRegister.Type.Equals(PaymentRegisterType.Cash))
            if (message.PaymentRegister.PaymentCurrencyRegisters.Any(r => r.IsNew() && (!r.CurrencyId.Equals(0) || r.Currency != null)))
                _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection).Add(
                    message
                        .PaymentRegister
                        .PaymentCurrencyRegisters
                        .Where(r => r.IsNew() && (!r.CurrencyId.Equals(0) || r.Currency != null))
                        .Select(r => {
                            r.CurrencyId = r.Currency?.Id ?? r.CurrencyId;
                            r.PaymentRegisterId = message.PaymentRegister.Id;
                            r.InitialAmount = r.Amount;

                            return r;
                        })
                );

        if (message.PaymentRegister.IsActive && message.PaymentRegister.Type.Equals(PaymentRegisterType.Bank))
            if (message.PaymentRegister.PaymentCurrencyRegisters.Any()) {
                PaymentCurrencyRegister paymentCurrencyRegister = message.PaymentRegister.PaymentCurrencyRegisters.First();

                paymentRegisterRepository
                    .SetInactiveByOrganizationAndCurrencyIds(
                        message.PaymentRegister.OrganizationId,
                        paymentCurrencyRegister.Currency?.Id ?? paymentCurrencyRegister.CurrencyId
                    );

                paymentRegisterRepository.SetActiveById(message.PaymentRegister.Id);
            }

        Sender.Tell(paymentRegisterRepository.GetById(message.PaymentRegister.Id));
    }

    private void ProcessDeletePaymentRegisterByNetIdMessage(DeletePaymentRegisterByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection).Remove(message.NetId);
    }

    private void ProcessSetActivePaymentRegisterByNetIdMessage(SetActivePaymentRegisterByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPaymentRegisterRepository paymentRegisterRepository = _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection);

        PaymentRegister paymentRegister = paymentRegisterRepository.GetByNetId(message.NetId);

        if (paymentRegister == null || !paymentRegister.Type.Equals(PaymentRegisterType.Bank) || !paymentRegister.PaymentCurrencyRegisters.Any()) return;

        PaymentCurrencyRegister paymentCurrencyRegister = paymentRegister.PaymentCurrencyRegisters.First();

        paymentRegisterRepository.SetInactiveByOrganizationAndCurrencyIds(paymentRegister.OrganizationId, paymentCurrencyRegister.CurrencyId);

        paymentRegisterRepository.SetActiveById(paymentRegister.Id);
    }

    private void ProcessSetSelectedPaymentRegisterByNetId(SetSelectedPaymentRegisterByNetId message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPaymentRegisterRepository paymentRegisterRepository = _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection);

        PaymentRegister selectedPaymentRegister = paymentRegisterRepository.GetIsSelected();

        if (selectedPaymentRegister != null && !selectedPaymentRegister.NetUid.Equals(message.NetId)) paymentRegisterRepository.DeselectByNetId(selectedPaymentRegister.NetUid);

        PaymentRegister paymentRegister = paymentRegisterRepository.GetByNetId(message.NetId);

        if (paymentRegister == null || !paymentRegister.Type.Equals(PaymentRegisterType.Card) || !paymentRegister.PaymentCurrencyRegisters.Any()) return;

        paymentRegisterRepository.SetSelectedByNetId(paymentRegister.NetUid);

        Sender.Tell(paymentRegisterRepository.GetAllForRetail(null));
    }
}