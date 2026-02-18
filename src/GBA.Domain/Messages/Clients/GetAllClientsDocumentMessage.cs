using System;
using GBA.Domain.FilterEntities;

namespace GBA.Domain.Messages.Clients;

public sealed class GetAllClientsDocumentMessage {
    public GetAllClientsDocumentMessage(GetQuery getQuery, string pathToFolder, Guid userNetId) {
        GetQuery = getQuery;
        PathToFolder = pathToFolder;
        UserNetId = userNetId;
    }

    public GetQuery GetQuery { get; }
    public string PathToFolder { get; }
    public Guid UserNetId { get; }
}