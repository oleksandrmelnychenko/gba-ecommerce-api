using System.Data;
using System.Globalization;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.PaymentOrders.PaymentMovements;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Services.Actors.PaymentOrders;

public sealed class PaymentMovementActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;

    public PaymentMovementActor(
        IDbConnectionFactory connectionFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;

        Receive<AddNewPaymentMovementMessage>(ProcessAddNewPaymentMovementMessage);

        Receive<UpdatePaymentMovementMessage>(ProcessUpdatePaymentMovementMessage);

        Receive<DeletePaymentMovementByNetIdMessage>(ProcessDeletePaymentMovementByNetIdMessage);
    }

    private void ProcessAddNewPaymentMovementMessage(AddNewPaymentMovementMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPaymentMovementRepository paymentMovementRepository = _paymentOrderRepositoriesFactory.NewPaymentMovementRepository(connection);

        message.PaymentMovement.Id = paymentMovementRepository.Add(message.PaymentMovement);

        _paymentOrderRepositoriesFactory.NewPaymentMovementTranslationRepository(connection).Add(new PaymentMovementTranslation {
            Name = message.PaymentMovement.OperationName,
            PaymentMovementId = message.PaymentMovement.Id,
            CultureCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        });

        Sender.Tell(paymentMovementRepository.GetById(message.PaymentMovement.Id));
    }

    private void ProcessUpdatePaymentMovementMessage(UpdatePaymentMovementMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPaymentMovementRepository paymentMovementRepository = _paymentOrderRepositoriesFactory.NewPaymentMovementRepository(connection);
        IPaymentMovementTranslationRepository paymentMovementTranslationRepository =
            _paymentOrderRepositoriesFactory.NewPaymentMovementTranslationRepository(connection);

        paymentMovementRepository.Update(message.PaymentMovement);

        PaymentMovementTranslation translation = paymentMovementTranslationRepository.GetByPaymentMovementId(message.PaymentMovement.Id);

        if (translation != null) {
            translation.Name = message.PaymentMovement.OperationName;

            paymentMovementTranslationRepository.Update(translation);
        } else {
            paymentMovementTranslationRepository.Add(new PaymentMovementTranslation {
                Name = message.PaymentMovement.OperationName,
                PaymentMovementId = message.PaymentMovement.Id,
                CultureCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            });
        }

        Sender.Tell(paymentMovementRepository.GetById(message.PaymentMovement.Id));
    }

    private void ProcessDeletePaymentMovementByNetIdMessage(DeletePaymentMovementByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _paymentOrderRepositoriesFactory.NewPaymentMovementRepository(connection).Remove(message.NetId);
    }
}