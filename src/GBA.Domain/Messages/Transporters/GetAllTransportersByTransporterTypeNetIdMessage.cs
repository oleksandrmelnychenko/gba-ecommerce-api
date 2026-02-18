using System;

namespace GBA.Domain.Messages.Transporters;

public sealed class GetAllTransportersByTransporterTypeNetIdMessage {
    public GetAllTransportersByTransporterTypeNetIdMessage(Guid transporterTypeNetId) {
        TransporterTypeNetId = transporterTypeNetId;
    }

    public Guid TransporterTypeNetId { get; set; }
}