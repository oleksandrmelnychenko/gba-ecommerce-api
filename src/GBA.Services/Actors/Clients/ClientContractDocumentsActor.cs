using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Clients;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Services.Actors.Clients;

public sealed class ClientContractDocumentsActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public ClientContractDocumentsActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;

        Receive<UpdateClientContractDocumentsMessage>(ProcessUpdateClientContractDocumentsMessage);

        Receive<DeleteClientContractDocumentMessage>(ProcessDeleteClientContractDocumentMessage);
    }

    private void ProcessUpdateClientContractDocumentsMessage(UpdateClientContractDocumentsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IClientContractDocumentRepository clientContractDocumentRepository =
            _clientRepositoriesFactory
                .NewClientContractDocumentRepository(connection);

        clientContractDocumentRepository
            .Add(
                message
                    .Client
                    .ClientContractDocuments
                    .Where(d => d.IsNew())
                    .Select(d => {
                        d.ClientId = message.Client.Id;

                        return d;
                    })
            );

        clientContractDocumentRepository
            .Update(
                message.Client.ClientContractDocuments.Where(d => !d.IsNew() && !d.Deleted)
            );

        clientContractDocumentRepository
            .Remove(
                message.Client.ClientContractDocuments.Where(d => !d.IsNew() && d.Deleted).Select(d => d.Id)
            );

        Sender.Tell(_clientRepositoriesFactory.NewClientRepository(connection).GetByNetId(message.Client.NetUid));
    }

    private void ProcessDeleteClientContractDocumentMessage(DeleteClientContractDocumentMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _clientRepositoriesFactory
            .NewClientContractDocumentRepository(connection)
            .Remove(message.NetId);
    }
}