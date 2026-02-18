using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientMap : EntityBaseMap<Client> {
    public override void Map(EntityTypeBuilder<Client> entity) {
        base.Map(entity);

        entity.ToTable("Client");

        entity.Property(e => e.Name).HasMaxLength(150);

        entity.Property(e => e.FirstName).HasMaxLength(150);

        entity.Property(e => e.MiddleName).HasMaxLength(150);

        entity.Property(e => e.LastName).HasMaxLength(150);

        entity.Property(e => e.FullName).HasMaxLength(200);

        entity.Property(e => e.Street).HasMaxLength(250);

        entity.Property(e => e.ZipCode).HasMaxLength(250);

        entity.Property(e => e.HouseNumber).HasMaxLength(250);

        entity.Property(e => e.Manager).HasMaxLength(250);

        entity.Property(e => e.TIN).HasMaxLength(30);

        entity.Property(e => e.USREOU).HasMaxLength(30);

        entity.Property(e => e.SupplierCode).HasMaxLength(40);

        entity.Property(e => e.ClearCartAfterDays).HasDefaultValue(3);

        entity.Property(e => e.IsSubClient).HasDefaultValueSql("0");

        entity.Property(e => e.IsTradePoint).HasDefaultValueSql("0");

        entity.Property(e => e.IsBlocked).HasDefaultValueSql("0");

        entity.Property(e => e.IsFromECommerce).HasDefaultValueSql("0");

        entity.Property(e => e.RegionId).HasColumnName("RegionID");

        entity.Property(e => e.RegionCodeId).HasColumnName("RegionCodeID");

        entity.Property(e => e.CountryId).HasColumnName("CountryID");

        entity.Property(e => e.ClientBankDetailsId).HasColumnName("ClientBankDetailsID");

        entity.Property(e => e.TermsOfDeliveryId).HasColumnName("TermsOfDeliveryID");

        entity.Property(e => e.PackingMarkingId).HasColumnName("PackingMarkingID");

        entity.Property(e => e.PackingMarkingPaymentId).HasColumnName("PackingMarkingPaymentID");

        entity.Property(e => e.SourceAmgId).HasColumnName("SourceAmgID");

        entity.Property(e => e.SourceFenixId).HasColumnName("SourceFenixID");

        entity.Property(e => e.MainManagerId).HasColumnName("MainManagerID");

        entity.Property(e => e.MainClientId).HasColumnName("MainClientID");

        entity.Property(e => e.OriginalRegionCode).HasMaxLength(10);

        entity.Property(e => e.SourceAmgId).HasMaxLength(16);

        entity.Property(e => e.SourceFenixId).HasMaxLength(16);

        entity.Ignore(e => e.PerfectClients);

        entity.Ignore(e => e.RootClient);

        entity.Ignore(e => e.RefId);

        entity.Ignore(e => e.NameDistance);

        entity.Ignore(e => e.AccountBalance);

        entity.Ignore(e => e.CurrentWorkplace);

        entity.Ignore(e => e.ClientGroupName);

        entity.Ignore(e => e.IsSupplier);

        entity.Ignore(e => e.TotalCurrentAmount);

        entity.HasOne(e => e.Region)
            .WithMany(e => e.Clients)
            .HasForeignKey(e => e.RegionId);

        entity.HasOne(e => e.Country)
            .WithMany(e => e.Clients)
            .HasForeignKey(e => e.CountryId);

        entity.HasOne(e => e.ClientBankDetails)
            .WithMany(e => e.Clients)
            .HasForeignKey(e => e.ClientBankDetailsId);

        entity.HasOne(e => e.TermsOfDelivery)
            .WithMany(e => e.Clients)
            .HasForeignKey(e => e.TermsOfDeliveryId);

        entity.HasOne(e => e.PackingMarking)
            .WithMany(e => e.Clients)
            .HasForeignKey(e => e.PackingMarkingId);

        entity.HasOne(e => e.PackingMarkingPayment)
            .WithMany(e => e.Clients)
            .HasForeignKey(e => e.PackingMarkingPaymentId);

        entity.HasOne(e => e.MainManager)
            .WithMany(e => e.Clients)
            .HasForeignKey(e => e.MainManagerId);

        entity.HasOne(e => e.MainClient)
            .WithMany(e => e.GroupSubClients)
            .HasForeignKey(e => e.MainClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(e => e.NetUid).IsUnique();
    }
}