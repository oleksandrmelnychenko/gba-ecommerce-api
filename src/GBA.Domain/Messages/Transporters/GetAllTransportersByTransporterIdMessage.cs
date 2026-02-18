using System;

namespace GBA.Domain.Messages.Transporters;

public sealed class GetAllTransportersByTransporterIdMessage {
    public GetAllTransportersByTransporterIdMessage(Guid transporterTypeNetId) {
        TransporterTypeNetId = transporterTypeNetId;
    }

    public Guid TransporterTypeNetId { get; set; }
}