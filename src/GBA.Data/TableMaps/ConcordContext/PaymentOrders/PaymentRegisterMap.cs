using GBA.Domain.Entities.PaymentOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.PaymentOrders;

public sealed class PaymentRegisterMap : EntityBaseMap<PaymentRegister> {
    public override void Map(EntityTypeBuilder<PaymentRegister> entity) {
        base.Map(entity);

        entity.ToTable("PaymentRegister");

        entity.Property(e => e.Name).HasMaxLength(100);

        entity.Property(e => e.BankName).HasMaxLength(100);

        entity.Property(e => e.City).HasMaxLength(100);

        entity.Property(e => e.AccountNumber).HasMaxLength(50);

        entity.Property(e => e.IBAN).HasMaxLength(50);

        entity.Property(e => e.SwiftCode).HasMaxLength(50);

        entity.Property(e => e.SortCode).HasMaxLength(20);

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.CVV).HasMaxLength(3);

        entity.Ignore(e => e.TotalEuroAmount);

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.PaymentRegisters)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}