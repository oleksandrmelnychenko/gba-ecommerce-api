using System;

namespace GBA.Domain.Messages.Consumables.CompanyCarRoadLists;

public sealed class GetAllCompanyCarRoadListsFilteredMessage {
    public GetAllCompanyCarRoadListsFilteredMessage(Guid companyCarNetId, DateTime from, DateTime to) {
        CompanyCarNetId = companyCarNetId;

        From = from;

        To = to;
    }

    public Guid CompanyCarNetId { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }
}