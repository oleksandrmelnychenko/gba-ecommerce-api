using System;
using System.Data;
using System.Globalization;
using System.Linq;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Messages.Organizations;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Services.Actors.Organizations;

public sealed class OrganizationsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IOrganizationRepositoriesFactory _organizationRepositoriesFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;

    public OrganizationsActor(
        IDbConnectionFactory connectionFactory,
        IOrganizationRepositoriesFactory organizationRepositoriesFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _organizationRepositoriesFactory = organizationRepositoriesFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;

        Receive<AddOrganizationMessage>(ProcessAddOrganizationMessage);

        Receive<UpdateOrganizationMessage>(ProcessUpdateOrganizationMessage);

        Receive<GetAllOrganizationsMessage>(ProcessGetAllOrganizationsMessage);

        Receive<GetOrganizationByNetIdMessage>(ProcessGetOrganizationByNetIdMessage);

        Receive<DeleteOrganizationMessage>(ProcessDeleteOrganizationMessage);
    }

    private void ProcessAddOrganizationMessage(AddOrganizationMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IOrganizationRepository organizationRepository = _organizationRepositoriesFactory.NewOrganizationRepository(connection);

        message.Organization.CurrencyId = message.Organization?.Currency?.Id;
        message.Organization.StorageId = message.Organization?.Storage?.Id;
        message.Organization.TaxInspectionId = message.Organization?.TaxInspection?.Id;
        message.Organization.VatRateId = message.Organization.VatRate?.Id;

        if (string.IsNullOrEmpty(message.Organization.Culture)) message.Organization.Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        if (string.IsNullOrEmpty(message.Organization.Code)) message.Organization.Code = string.Empty;

        if (message.Organization.OrganizationTranslations.Any(x => x.CultureCode.Equals("uk")))
            message.Organization.Name = message.Organization.OrganizationTranslations.First(x => x.CultureCode.Equals("uk")).Name;

        long organizationId = organizationRepository.Add(message.Organization);

        if (message.Organization.OrganizationTranslations.Any(o => !string.IsNullOrEmpty(o.Name))) {
            message.Organization.OrganizationTranslations.ToList().ForEach(t => t.OrganizationId = organizationId);

            _organizationRepositoriesFactory.NewOrganizationTranslationRepository(connection).Add(message.Organization.OrganizationTranslations);
        }

        Sender.Tell(organizationRepository.GetById(organizationId));
    }

    private void ProcessUpdateOrganizationMessage(UpdateOrganizationMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IOrganizationRepository organizationRepository = _organizationRepositoriesFactory.NewOrganizationRepository(connection);
        IPaymentRegisterRepository paymentRegisterRepository =
            _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection);

        try {
            message.Organization.CurrencyId = message.Organization?.Currency?.Id;
            message.Organization.StorageId = message.Organization?.Storage?.Id;
            message.Organization.TaxInspectionId = message.Organization?.TaxInspection?.Id;

            paymentRegisterRepository.UpdateAllNotMainByOrganizationId(message.Organization.Id);

            if (message.Organization.MainPaymentRegister != null) paymentRegisterRepository.UpdateIsMainById(message.Organization.MainPaymentRegister.Id);

            if (string.IsNullOrEmpty(message.Organization.Culture)) message.Organization.Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            if (string.IsNullOrEmpty(message.Organization.Code)) message.Organization.Code = string.Empty;

            if (message.Organization.VatRate != null)
                message.Organization.VatRateId = message.Organization.VatRate.Id;
            else
                message.Organization.VatRateId = null;

            organizationRepository.Update(message.Organization);

            if (message.Organization.OrganizationTranslations.Any(o => !string.IsNullOrEmpty(o.Name))) {
                IOrganizationTranslationRepository organizationTranslationRepository =
                    _organizationRepositoriesFactory.NewOrganizationTranslationRepository(connection);

                message.Organization.OrganizationTranslations.ToList().ForEach(t => {
                    if (t.IsNew()) {
                        t.OrganizationId = message.Organization.Id;

                        organizationTranslationRepository.Add(t);
                    } else {
                        organizationTranslationRepository.Update(t);
                    }
                });
            }

            Sender.Tell(organizationRepository.GetByNetId(message.Organization.NetUid));
        } catch {
            Sender.Tell(organizationRepository.GetByNetId(message.Organization.NetUid));
        }
    }

    private void ProcessGetAllOrganizationsMessage(GetAllOrganizationsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_organizationRepositoriesFactory.NewOrganizationRepository(connection).GetAll());
    }

    private void ProcessGetOrganizationByNetIdMessage(GetOrganizationByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_organizationRepositoriesFactory.NewOrganizationRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessDeleteOrganizationMessage(DeleteOrganizationMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IOrganizationRepository organizationRepository = _organizationRepositoriesFactory.NewOrganizationRepository(connection);

        Organization organization = organizationRepository.GetByNetId(message.NetId);

        if (organization != null) {
            if (!organizationRepository.IsAssignedToAnyAgreement(organization.Id)) {
                organizationRepository.Remove(message.NetId);

                Sender.Tell(new Tuple<string, Organization>(string.Empty, organization));
            } else {
                Sender.Tell(new Tuple<string, Organization>(OrganizationResourceNames.ASSIGNED_TO_AGREEMENT, null));
            }
        } else {
            Sender.Tell(new Tuple<string, Organization>(string.Empty, null));
        }
    }
}