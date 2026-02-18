using System;
using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Messages.Consumables.CompanyCars;

public sealed class AddNewCompanyCarMessage {
    public AddNewCompanyCarMessage(CompanyCar companyCar, Guid userNetId) {
        CompanyCar = companyCar;

        UserNetId = userNetId;
    }

    public CompanyCar CompanyCar { get; set; }

    public Guid UserNetId { get; set; }
}