using System;
using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Messages.Consumables.CompanyCarRoadLists;

public sealed class UpdateCompanyCarRoadListMessage {
    public UpdateCompanyCarRoadListMessage(CompanyCarRoadList companyCarRoadList, Guid userNetId) {
        CompanyCarRoadList = companyCarRoadList;

        UserNetId = userNetId;
    }

    public CompanyCarRoadList CompanyCarRoadList { get; set; }

    public Guid UserNetId { get; set; }
}