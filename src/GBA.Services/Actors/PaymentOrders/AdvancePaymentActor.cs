using System;
using System.Data;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Messages.PaymentOrders.AdvancePayments;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.PaymentOrders;

public sealed class AdvancePaymentActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public AdvancePaymentActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;

        Receive<AddNewAdvancePaymentMessage>(ProcessAddNewAdvancePaymentMessage);

        Receive<UpdateAdvancePaymentMessage>(ProcessUpdateAdvancePaymentMessage);
    }

    private void ProcessAddNewAdvancePaymentMessage(AddNewAdvancePaymentMessage message) {
        try {
            using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.AdvancePayment == null) throw new Exception(AdvancePaymentResourceNames.ADVANCE_PAYMENT_EMPTY);

            if (!message.AdvancePayment.IsNew()) throw new Exception(AdvancePaymentResourceNames.ADVANCE_PAYMENT_INVALID_PAYLOAD);

            if (message.AdvancePayment.Organization == null || message.AdvancePayment.Organization.IsNew())
                throw new Exception(AdvancePaymentResourceNames.NOT_SPECIFY_ORGANIZATION);

            if ((message.AdvancePayment.ClientAgreement == null || message.AdvancePayment.ClientAgreement.IsNew()) &&
                (message.AdvancePayment.OrganizationClientAgreement == null || message.AdvancePayment.OrganizationClientAgreement.IsNew()))
                throw new Exception(AdvancePaymentResourceNames.COUNTER_PARTY_AGREEMENT_NOT_SPECIFIED);

            if (message.AdvancePayment.Amount <= decimal.Zero) throw new Exception(AdvancePaymentResourceNames.LESS_AMOUNT);

            if (message.TaxFreeNetId.Equals(Guid.Empty) && message.SadNetId.Equals(Guid.Empty)) throw new Exception(AdvancePaymentResourceNames.INVALID_TAX_FREE_OR_SAD);

            TaxFree taxFree =
                message.TaxFreeNetId.Equals(Guid.Empty)
                    ? null
                    : _supplyUkraineRepositoriesFactory.NewTaxFreeRepository(connection, exchangeRateConnection).GetByNetId(message.TaxFreeNetId);

            Sad sad =
                message.SadNetId.Equals(Guid.Empty)
                    ? null
                    : _supplyUkraineRepositoriesFactory.NewSadRepository(connection, exchangeRateConnection).GetByNetIdWithoutIncludes(message.SadNetId);

            if (taxFree == null && sad == null) throw new Exception(AdvancePaymentResourceNames.INVALID_TAX_FREE_OR_SAD);

            IAdvancePaymentRepository advancePaymentRepository = _paymentOrderRepositoriesFactory.NewAdvancePaymentRepository(connection);

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            if (message.AdvancePayment.Organization != null && !message.AdvancePayment.Organization.IsNew())
                message.AdvancePayment.OrganizationId = message.AdvancePayment.Organization.Id;

            if (message.AdvancePayment.ClientAgreement != null && !message.AdvancePayment.ClientAgreement.IsNew())
                message.AdvancePayment.ClientAgreementId = message.AdvancePayment.ClientAgreement.Id;

            if (message.AdvancePayment.OrganizationClientAgreement != null && !message.AdvancePayment.OrganizationClientAgreement.IsNew())
                message.AdvancePayment.OrganizationClientAgreementId = message.AdvancePayment.OrganizationClientAgreement.Id;

            message.AdvancePayment.UserId = user.Id;
            message.AdvancePayment.TaxFreeId = taxFree?.Id;
            message.AdvancePayment.SadId = sad?.Id;
            message.AdvancePayment.FromDate = message.AdvancePayment.FromDate.Year.Equals(1) ? DateTime.UtcNow : message.AdvancePayment.FromDate;

            AdvancePayment lastRecord = advancePaymentRepository.GetLastRecord();

            if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                message.AdvancePayment.Number = string.Format("{0:D6}", 1);
            else
                message.AdvancePayment.Number = string.Format("{0:D6}", Convert.ToInt32(lastRecord.Number) + 1);

            message.AdvancePayment.Id = advancePaymentRepository.Add(message.AdvancePayment);

            Sender.Tell(advancePaymentRepository.GetById(message.AdvancePayment.Id));
        } catch (Exception e) {
            Sender.Tell(e);
        }
    }

    private void ProcessUpdateAdvancePaymentMessage(UpdateAdvancePaymentMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.AdvancePayment == null) throw new Exception(AdvancePaymentResourceNames.ADVANCE_PAYMENT_EMPTY);

            if (message.AdvancePayment.IsNew()) throw new Exception(AdvancePaymentResourceNames.ADVANCE_PAYMENT_INVALID_PAYLOAD);

            if (message.AdvancePayment.Organization == null || message.AdvancePayment.Organization.IsNew())
                throw new Exception(AdvancePaymentResourceNames.NOT_SPECIFY_ORGANIZATION);

            if ((message.AdvancePayment.ClientAgreement == null || message.AdvancePayment.ClientAgreement.IsNew()) &&
                (message.AdvancePayment.OrganizationClientAgreement == null || message.AdvancePayment.OrganizationClientAgreement.IsNew()))
                throw new Exception(AdvancePaymentResourceNames.COUNTER_PARTY_AGREEMENT_NOT_SPECIFIED);

            if (message.AdvancePayment.Amount <= decimal.Zero) throw new Exception(AdvancePaymentResourceNames.LESS_AMOUNT);

            IAdvancePaymentRepository advancePaymentRepository = _paymentOrderRepositoriesFactory.NewAdvancePaymentRepository(connection);

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            if (message.AdvancePayment.Organization != null && !message.AdvancePayment.Organization.IsNew())
                message.AdvancePayment.OrganizationId = message.AdvancePayment.Organization.Id;

            if (message.AdvancePayment.ClientAgreement != null && !message.AdvancePayment.ClientAgreement.IsNew())
                message.AdvancePayment.ClientAgreementId = message.AdvancePayment.ClientAgreement.Id;

            if (message.AdvancePayment.OrganizationClientAgreement != null && !message.AdvancePayment.OrganizationClientAgreement.IsNew())
                message.AdvancePayment.OrganizationClientAgreementId = message.AdvancePayment.OrganizationClientAgreement.Id;

            message.AdvancePayment.UserId = user.Id;
            message.AdvancePayment.FromDate = message.AdvancePayment.FromDate.Year.Equals(1) ? DateTime.UtcNow : message.AdvancePayment.FromDate;

            advancePaymentRepository.Update(message.AdvancePayment);

            Sender.Tell(advancePaymentRepository.GetById(message.AdvancePayment.Id));
        } catch (Exception e) {
            Sender.Tell(e);
        }
    }
}