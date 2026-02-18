using GBA.Domain.Entities.Sales.Shipments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public sealed class UpdateDataCarrierMap : EntityBaseMap<UpdateDataCarrier> {
    public override void Map(EntityTypeBuilder<UpdateDataCarrier> entity) {
        base.Map(entity);

        entity.ToTable("UpdateDataCarrier");

        entity.Property(e => e.TTN).HasColumnName("TTN");

        entity.Property(e => e.ShipmentDate).HasColumnName("ShipmentDate");

        entity.Property(e => e.UserId).HasColumnName("UserId");

        entity.Property(e => e.IsDevelopment).HasDefaultValueSql("0");

        entity.Property(e => e.Comment).HasMaxLength(450);

        entity.Property(e => e.CashOnDeliveryAmount).HasColumnType("money");

        entity.Property(e => e.Number).HasColumnName("Number");

        entity.Property(e => e.MobilePhone).HasColumnName("MobilePhone");

        entity.Property(e => e.TransporterId).HasColumnName("TransporterId");

        entity.Property(e => e.City).HasColumnName("City");

        entity.Property(e => e.Department).HasColumnName("Department");

        entity.Property(e => e.TtnPDFPath).HasColumnName("TtnPDFPath");

        entity.Property(e => e.FullName).HasColumnName("FullName");

        entity.Property(e => e.HasDocument).HasColumnName("HasDocument");

        entity.Property(e => e.IsCashOnDelivery).HasColumnName("IsCashOnDelivery");

        entity.Property(e => e.ApproveUpdate).HasColumnName("ApproveUpdate");

        entity.Property(e => e.IsEditTransporter).HasColumnName("IsEditTransporter");

        entity.Ignore(e => e.TotalRowsQty);
    }
}