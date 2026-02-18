using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.DataSync;
using GBA.Domain.Repositories.DataSync.Contracts;

namespace GBA.Services.Actors.DataSync.BaseDataSyncGetActors;

public sealed class BaseDocumentsAfterSyncGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDataSyncRepositoriesFactory _dataSyncRepositoriesFactory;

    public BaseDocumentsAfterSyncGetActor(
        IDbConnectionFactory connectionFactory,
        IDataSyncRepositoriesFactory dataSyncRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _dataSyncRepositoriesFactory = dataSyncRepositoriesFactory;

        Receive<GetAllDocumentsAfterSyncMessage>(ProcessGetAllDocumentsAfterSyncMessage);

        Receive<GetDataSyncInfoMessage>(ProcessGetDataSyncInfo);
    }

    private void ProcessGetAllDocumentsAfterSyncMessage(GetAllDocumentsAfterSyncMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();

        IDocumentsAfterSyncRepository documentsAfterSyncRepository = _dataSyncRepositoriesFactory.NewDocumentsAfterSyncRepository(connection);

        Sender.Tell(documentsAfterSyncRepository.GetMappedDocumentsFiltered(
            message.From,
            message.To,
            message.Limit,
            message.Offset,
            message.Name.Trim(),
            message.Type));
    }

    private void ProcessGetDataSyncInfo(GetDataSyncInfoMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();

        IDataSyncOperationRepository dataSyncOperationRepository =
            _dataSyncRepositoriesFactory.NewDataSyncOperationRepository(connection);

        Sender.Tell(dataSyncOperationRepository.GetLastDataSyncInfo(message.ForAmg, message.From, message.To));
    }
}