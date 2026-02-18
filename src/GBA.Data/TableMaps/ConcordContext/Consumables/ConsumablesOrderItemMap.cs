using GBA.Domain.Entities.Consumables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Consumables;

public sealed class ConsumablesOrderItemMap : EntityBaseMap<ConsumablesOrderItem> {
    public override void Map(EntityTypeBuilder<ConsumablesOrderItem> entity) {
        base.Map(entity);

        entity.ToTable("ConsumablesOrderItem");

        entity.Property(e => e.ConsumableProductCategoryId).HasColumnName("ConsumableProductCategoryID");

        entity.Property(e => e.ConsumablesOrderId).HasColumnName("ConsumablesOrderID");

        entity.Property(e => e.ConsumableProductId).HasColumnName("ConsumableProductID");

        entity.Property(e => e.ConsumableProductOrganizationId).HasColumnName("ConsumableProductOrganizationID");

        entity.Property(e => e.SupplyOrganizationAgreementId).HasColumnName("SupplyOrganizationAgreementID");

        entity.Property(e => e.TotalPrice).HasColumnType("money");

        entity.Property(e => e.PricePerItem).HasColumnType("money");

        entity.Property(e => e.VAT).HasColumnType("money");

        entity.Property(e => e.TotalPriceWithVAT).HasColumnType("money");

        entity.HasOne(e => e.ConsumableProductCategory)
            .WithMany(e => e.ConsumablesOrderItems)
            .HasForeignKey(e => e.ConsumableProductCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ConsumablesOrder)
            .WithMany(e => e.ConsumablesOrderItems)
            .HasForeignKey(e => e.ConsumablesOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ConsumableProduct)
            .WithMany(e => e.ConsumablesOrderItems)
            .HasForeignKey(e => e.ConsumableProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ConsumableProductOrganization)
            .WithMany(e => e.ConsumablesOrderItems)
            .HasForeignKey(e => e.ConsumableProductOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrganizationAgreement)
            .WithMany(e => e.ConsumablesOrderItems)
            .HasForeignKey(e => e.SupplyOrganizationAgreementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}