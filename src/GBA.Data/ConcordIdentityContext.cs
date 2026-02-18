using GBA.Common.Helpers;
using GBA.Common.IdentityConfiguration.Entities;
using GBA.Data.MapConfigurations;
using GBA.Data.TableMaps.ConcordContext.UserManagement;
using GBA.Domain.IdentityEntities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GBA.Data;

public class ConcordIdentityContext : IdentityDbContext<UserIdentity> {
    public ConcordIdentityContext() { }

    public ConcordIdentityContext(DbContextOptions<ConcordIdentityContext> options) : base(options) { }
    public virtual DbSet<UserToken> UserToken { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        optionsBuilder.UseSqlServer(ConfigurationManager.LocalIdentityConnectionString);
    }

    protected override void OnModelCreating(ModelBuilder builder) {
        base.OnModelCreating(builder);

        builder.AddConfiguration(new UserTokenMap());
    }
}