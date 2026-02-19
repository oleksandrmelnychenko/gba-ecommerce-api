using GBA.Common.Helpers;
using GBA.Data.MapConfigurations;
using GBA.Data.TableMaps.ConcordDataAnalytic.HistoryOrderItems;
using GBA.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace GBA.Data;

public class ConcordDataAnalyticContext : DbContext {
    public ConcordDataAnalyticContext() { }


    public ConcordDataAnalyticContext(DbContextOptions<ConcordDataAnalyticContext> optionsBuilder) : base(optionsBuilder) { }
    public virtual DbSet<StockStateStorage> StockStateStorage { get; set; }
    public virtual DbSet<ProductPlacementDataHistory> ProductPlacementDataHistory { get; set; }
    public virtual DbSet<ProductAvailabilityDataHistory> ProductAvailabilityDataHistory { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        if (!optionsBuilder.IsConfigured) {
            optionsBuilder.UseSqlServer(ConfigurationManager.LocalDataAnalyticConnectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.AddConfiguration(new StockStateStorageMap());
        modelBuilder.AddConfiguration(new ProductPlacementDataHistoryeMap());
        modelBuilder.AddConfiguration(new ProductAvailabilityDataHistoryMap());
    }
}