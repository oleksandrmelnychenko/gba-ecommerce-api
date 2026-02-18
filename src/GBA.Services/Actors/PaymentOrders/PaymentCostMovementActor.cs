using System.Data;
using System.Globalization;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.PaymentOrders.PaymentCostMovements;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Services.Actors.PaymentOrders;

public sealed class PaymentCostMovementActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;

    public PaymentCostMovementActor(
        IDbConnectionFactory connectionFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;

        Receive<AddNewPaymentCostMovementMessage>(ProcessAddNewPaymentCostMovementMessage);

        Receive<UpdatePaymentCostMovementMessage>(ProcessUpdatePaymentCostMovementMessage);

        Receive<DeletePaymentCostMovementByNetIdMessage>(ProcessDeletePaymentCostMovementByNetIdMessage);
    }

    private void ProcessAddNewPaymentCostMovementMessage(AddNewPaymentCostMovementMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPaymentCostMovementRepository paymentCostMovementRepository = _paymentOrderRepositoriesFactory.NewPaymentCostMovementRepository(connection);

        message.PaymentCostMovement.Id = paymentCostMovementRepository.Add(message.PaymentCostMovement);

        _paymentOrderRepositoriesFactory.NewPaymentCostMovementTranslationRepository(connection).Add(new PaymentCostMovementTranslation {
            OperationName = message.PaymentCostMovement.OperationName,
            PaymentCostMovementId = message.PaymentCostMovement.Id,
            CultureCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        });

        Sender.Tell(paymentCostMovementRepository.GetById(message.PaymentCostMovement.Id));
    }

    private void ProcessUpdatePaymentCostMovementMessage(UpdatePaymentCostMovementMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPaymentCostMovementRepository paymentCostMovementRepository = _paymentOrderRepositoriesFactory.NewPaymentCostMovementRepository(connection);
        IPaymentCostMovementTranslationRepository paymentCostMovementTranslationRepository =
            _paymentOrderRepositoriesFactory.NewPaymentCostMovementTranslationRepository(connection);

        paymentCostMovementRepository.Update(message.PaymentCostMovement);

        PaymentCostMovementTranslation translation = paymentCostMovementTranslationRepository.GetByPaymentMovementId(message.PaymentCostMovement.Id);

        if (translation != null) {
            translation.OperationName = message.PaymentCostMovement.OperationName;

            paymentCostMovementTranslationRepository.Update(translation);
        } else {
            paymentCostMovementTranslationRepository.Add(new PaymentCostMovementTranslation {
                OperationName = message.PaymentCostMovement.OperationName,
                PaymentCostMovementId = message.PaymentCostMovement.Id,
                CultureCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            });
        }

        Sender.Tell(paymentCostMovementRepository.GetById(message.PaymentCostMovement.Id));
    }

    private void ProcessDeletePaymentCostMovementByNetIdMessage(DeletePaymentCostMovementByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _paymentOrderRepositoriesFactory.NewPaymentCostMovementRepository(connection).Remove(message.NetId);
    }
}