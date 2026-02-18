using Microsoft.EntityFrameworkCore;

namespace GBA.Domain.DataSourceAdapters.SQL.Contracts;

public interface ISqlDbContext {
    DbContext DbContext { get; }
}