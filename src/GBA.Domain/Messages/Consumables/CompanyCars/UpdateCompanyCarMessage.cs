using System;
using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Messages.Consumables.CompanyCars;

public sealed class UpdateCompanyCarMessage {
    public UpdateCompanyCarMessage(CompanyCar companyCar, Guid userNetId) {
        CompanyCar = companyCar;

        UserNetId = userNetId;
    }

    public CompanyCar CompanyCar { get; set; }

    public Guid UserNetId { get; set; }
}