using System;
using GBA.Domain.FilterEntities;

namespace GBA.Domain.Messages.SearchStrategy;

public sealed class GetAllByQueryMessage {
    public GetAllByQueryMessage(GetQuery getQuery, Guid? userNetId = null, Guid? clientAgreementNetId = null) {
        GetQuery = getQuery;

        UserNetId = userNetId.Value;

        ClientAgreementNetId = clientAgreementNetId;
    }

    public GetQuery GetQuery { get; set; }

    public Guid? UserNetId { get; set; }

    public Guid? ClientAgreementNetId { get; set; }
}