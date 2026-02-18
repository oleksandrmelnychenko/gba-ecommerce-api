using GBA.Domain.DataSourceAdapters.SQL.Contracts;
using Microsoft.EntityFrameworkCore;

namespace GBA.Domain.DataSourceAdapters.SQL;

public class SqlDbContext : ISqlDbContext {
    public SqlDbContext(DbContext dbContext) {
        DbContext = dbContext;
    }

    public DbContext DbContext { get; }
}