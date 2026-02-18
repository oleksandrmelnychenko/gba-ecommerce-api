using System;
using System.Diagnostics;
using GBA.Domain.DataSourceAdapters.SQL.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.TransactionUnit.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GBA.Domain.TransactionUnit;

public class TransactionUnit : ITransactionUnit, IDisposable {
    private readonly ISqlDbContext _sqliteDbContext;

    /// <summary>
    /// ctor().
    /// </summary>
    /// <param name="sqliteContextFactory"></param>
    public TransactionUnit([FromServices] ISqlContextFactory sqliteContextFactory) {
        _sqliteDbContext = sqliteContextFactory.New();
    }

    public void Dispose() {
        _sqliteDbContext.DbContext.Dispose();
    }

    public DbSet<TEntity> GetTable<TEntity>() where TEntity : EntityBase {
        return _sqliteDbContext.DbContext.Set<TEntity>();
    }

    public void Complete() {
        try {
            _sqliteDbContext.DbContext.SaveChanges();
        } catch (DbUpdateConcurrencyException) {
            //foreach (var item in exc.Entries) {
            //    var rowVersion = item.GetDatabaseValues().GetValue<object>(TransactionUnitConstants.VERSION_COLUMN_NAME);

            //    item.Property(TransactionUnitConstants.VERSION_COLUMN_NAME).OriginalValue = rowVersion;
            //}

            _sqliteDbContext.DbContext.SaveChanges();
        } catch (DbUpdateException) {
            throw;
        } catch (Exception) {
            Debugger.Break();
        }
    }
}