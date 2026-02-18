using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.MapConfigurations;

public abstract class EntityTypeConfiguration<TEntity> where TEntity : class {
    public abstract void Map(EntityTypeBuilder<TEntity> builder);
}