using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Translations.OrganizationTranslations;
using GBA.Domain.Repositories.Organizations.Contracts;

namespace GBA.Services.Actors.Translations;

public sealed class OrganizationTranslationsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IOrganizationRepositoriesFactory _organizationRepositoriesFactory;

    public OrganizationTranslationsActor(
        IDbConnectionFactory connectionFactory,
        IOrganizationRepositoriesFactory organizationRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _organizationRepositoriesFactory = organizationRepositoriesFactory;

        Receive<AddOrganizationTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IOrganizationTranslationRepository organizationTranslationRepository = _organizationRepositoriesFactory.NewOrganizationTranslationRepository(connection);

            message.OrganizationTranslation.OrganizationId = message.OrganizationTranslation.Organization.Id;

            Sender.Tell(organizationTranslationRepository.GetById(organizationTranslationRepository.Add(message.OrganizationTranslation)));
        });

        Receive<UpdateOrganizationTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IOrganizationTranslationRepository organizationTranslationRepository = _organizationRepositoriesFactory.NewOrganizationTranslationRepository(connection);

            organizationTranslationRepository.Update(message.OrganizationTranslation);

            Sender.Tell(organizationTranslationRepository.GetByNetId(message.OrganizationTranslation.NetUid));
        });

        Receive<GetAllOrganizationTranslationsMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_organizationRepositoriesFactory.NewOrganizationTranslationRepository(connection).GetAll());
        });

        Receive<GetOrganizationTranslationByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_organizationRepositoriesFactory.NewOrganizationTranslationRepository(connection).GetByNetId(message.NetId));
        });

        Receive<DeleteOrganizationTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _organizationRepositoriesFactory.NewOrganizationTranslationRepository(connection).Remove(message.NetId);
        });
    }
}