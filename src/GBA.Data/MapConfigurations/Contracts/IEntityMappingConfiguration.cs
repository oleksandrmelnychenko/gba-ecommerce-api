using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.MapConfigurations.Contracts;

public interface IEntityMappingConfiguration {
    void Map(ModelBuilder b);
}

public interface IEntityMappingConfiguration<T> : IEntityMappingConfiguration where T : class {
    void Map(EntityTypeBuilder<T> builder);
}