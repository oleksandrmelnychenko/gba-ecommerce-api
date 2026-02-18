using System.Collections.Generic;
using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Messages.Consumables.CompanyCars;

public sealed class CalculateCompanyCarFuelingsMessage {
    public CalculateCompanyCarFuelingsMessage(IEnumerable<CompanyCarFueling> companyCarFuelings) {
        CompanyCarFuelings = companyCarFuelings;
    }

    public IEnumerable<CompanyCarFueling> CompanyCarFuelings { get; set; }
}