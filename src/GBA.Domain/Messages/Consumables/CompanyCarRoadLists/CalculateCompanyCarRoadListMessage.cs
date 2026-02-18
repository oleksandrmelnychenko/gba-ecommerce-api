using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Messages.Consumables.CompanyCarRoadLists;

public sealed class CalculateCompanyCarRoadListMessage {
    public CalculateCompanyCarRoadListMessage(CompanyCarRoadList companyCarRoadList) {
        CompanyCarRoadList = companyCarRoadList;
    }

    public CompanyCarRoadList CompanyCarRoadList { get; set; }
}