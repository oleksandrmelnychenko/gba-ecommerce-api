using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.AuditEntities;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Helpers;
using GBA.Domain.Messages.Auditing;
using GBA.Domain.Repositories.Auditing.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Auditing;

public sealed class AuditingActor : ReceiveActor {
    private readonly IAuditRepositoriesFactory _auditRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public AuditingActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IAuditRepositoriesFactory auditRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _auditRepositoriesFactory = auditRepositoriesFactory;

        Receive<RetrieveAndStoreAuditDataMessage>(ProcessRetrieveAndStoreAuditDataMessage);

        Receive<GetAllAuditDataByBaseEntityNetIdMessage>(ProcessGetAllAuditDataByBaseEntityNetIdMessage);

        Receive<GetAllAuditDataByNetIdLimitedMessage>(ProcessGetAllAuditDataByNetIdLimitedMessage);
    }

    private void ProcessRetrieveAndStoreAuditDataMessage(RetrieveAndStoreAuditDataMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UpdatedBy);

        if (user != null) {
            AuditEntity entity = ChangeTracker.GetAuditInfo(message.OldEntity, message.NewEntity);

            if (message.PerdefinedNewProperties != null)
                foreach (AuditEntityProperty property in message.PerdefinedNewProperties)
                    entity.NewValues.Add(property);
            if (message.PerdefinedOldProperties != null)
                foreach (AuditEntityProperty property in message.PerdefinedOldProperties)
                    entity.OldValues.Add(property);

            if (message.IsRemove) {
                entity.Type = AuditEventType.Delete;
                entity.UpdatedByNetUid = user.NetUid;
                entity.EntityName = message.EntityName;
                entity.BaseEntityNetUid = message.EntityNetId;
                entity.UpdatedBy = $"{user.FirstName} {user.LastName}";

                _auditRepositoriesFactory.NewAuditRepository(connection).Add(entity);
            } else {
                if (entity.NewValues.Any()) {
                    IAuditPropertiesRepository auditPropertiesRepository = _auditRepositoriesFactory.NewAuditPropertiesRepository(connection);

                    entity.UpdatedByNetUid = user.NetUid;
                    entity.EntityName = message.EntityName;
                    entity.BaseEntityNetUid = message.EntityNetId;
                    entity.UpdatedBy = $"{user.FirstName} {user.LastName}";

                    long auditEntityId = _auditRepositoriesFactory.NewAuditRepository(connection).Add(entity);

                    entity.NewValues.ToList().ForEach(p => p.AuditEntityId = auditEntityId);

                    auditPropertiesRepository.Add(entity.NewValues);

                    if (entity.OldValues.Any()) {
                        entity.OldValues.ToList().ForEach(p => p.AuditEntityId = auditEntityId);

                        auditPropertiesRepository.Add(entity.OldValues);
                    }
                }
            }
        }

        Sender.Tell(new object());
    }

    private void ProcessGetAllAuditDataByBaseEntityNetIdMessage(GetAllAuditDataByBaseEntityNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_auditRepositoriesFactory.NewAuditRepository(connection).GetAllByBaseEntityNetUid(message.NetId));
    }

    private void ProcessGetAllAuditDataByNetIdLimitedMessage(GetAllAuditDataByNetIdLimitedMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (string.IsNullOrEmpty(message.FieldName))
            Sender.Tell(
                _auditRepositoriesFactory
                    .NewAuditRepository(connection)
                    .GetAllByNetIdLimited(message.NetId, message.Limit, message.Offset)
            );
        else
            Sender.Tell(
                _auditRepositoriesFactory
                    .NewAuditRepository(connection)
                    .GetAllByNetIdAndSpecificFieldLimited(message.NetId, message.Limit, message.Offset, message.FieldName)
            );
    }
}