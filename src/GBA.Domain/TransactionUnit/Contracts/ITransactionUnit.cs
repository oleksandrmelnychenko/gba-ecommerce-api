using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GBA.Domain.TransactionUnit.Contracts;

public interface ITransactionUnit {
    void Complete();

    DbSet<TEntity> GetTable<TEntity>() where TEntity : EntityBase;
}