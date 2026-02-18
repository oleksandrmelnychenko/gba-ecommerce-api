using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Supports.SupportVideos;
using GBA.Domain.Repositories.Supports.Contracts;

namespace GBA.Services.Actors.Supports;

public sealed class SupportVideoActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupportRepositoriesFactory _supportRepositoriesFactory;

    public SupportVideoActor(
        IDbConnectionFactory connectionFactory,
        ISupportRepositoriesFactory supportRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _supportRepositoriesFactory = supportRepositoriesFactory;

        Receive<AddOrUpdateSupportVideoMessage>(ProcessAddOrUpdateSupportVideoMessage);

        Receive<GetAllSupportVideosMessage>(_ => ProcessGetAllSupportVideosMessage());
    }

    private void ProcessAddOrUpdateSupportVideoMessage(AddOrUpdateSupportVideoMessage message) {
        if (message?.SupportVideo == null) {
            Sender.Tell(null);
            return;
        }

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupportVideoRepository supportVideoRepository = _supportRepositoriesFactory.NewSupportVideoRepository(connection);

        if (message.SupportVideo.IsNew())
            message.SupportVideo.Id = supportVideoRepository.Add(message.SupportVideo);
        else
            supportVideoRepository.Update(message.SupportVideo);

        Sender.Tell(supportVideoRepository.GetById(message.SupportVideo.Id));
    }

    private void ProcessGetAllSupportVideosMessage() {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supportRepositoriesFactory
                .NewSupportVideoRepository(connection)
                .GetAll()
        );
    }
}