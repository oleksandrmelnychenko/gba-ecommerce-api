using System.Collections.Generic;
using GBA.Domain.Entities;

namespace GBA.Domain.Repositories.DocumentMonths.Contracts;

public interface IDocumentMonthRepository {
    IEnumerable<DocumentMonth> GetAllByCulture(string culture);
}