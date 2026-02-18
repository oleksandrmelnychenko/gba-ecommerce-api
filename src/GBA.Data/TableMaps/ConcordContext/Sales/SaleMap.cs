using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.Shipments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public sealed class SaleMap : EntityBaseMap<Sale> {
    public override void Map(EntityTypeBuilder<Sale> entity) {
        base.Map(entity);

        entity.ToTable("Sale");

        entity.Property(e => e.ClientAgreementId).HasColumnName("ClientAgreementID");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.UpdateUserId).HasColumnName("UpdateUserID");

        entity.Property(e => e.ChangedToInvoiceById).HasColumnName("ChangedToInvoiceByID");

        entity.Property(e => e.OrderId).HasColumnName("OrderID");

        entity.Property(e => e.BaseLifeCycleStatusId).HasColumnName("BaseLifeCycleStatusID");

        entity.Property(e => e.BaseSalePaymentStatusId).HasColumnName("BaseSalePaymentStatusID");

        entity.Property(e => e.DeliveryRecipientAddressId).HasColumnName("DeliveryRecipientAddressID");

        entity.Property(e => e.DeliveryRecipientId).HasColumnName("DeliveryRecipientID");

        entity.Property(e => e.SaleNumberId).HasColumnName("SaleNumberID");

        entity.Property(e => e.ShiftStatusId).HasColumnName("ShiftStatusID");

        entity.Property(e => e.SaleInvoiceDocumentId).HasColumnName("SaleInvoiceDocumentID");

        entity.Property(e => e.SaleInvoiceNumberId).HasColumnName("SaleInvoiceNumberID");

        entity.Property(e => e.TransporterId).HasColumnName("TransporterID");

        entity.Property(e => e.TaxFreePackListId).HasColumnName("TaxFreePackListID");

        entity.Property(e => e.WorkplaceId).HasColumnName("WorkplaceID");

        entity.Property(e => e.SadId).HasColumnName("SadID");

        entity.Property(e => e.CustomersOwnTtnId).HasColumnName("CustomersOwnTtnID");

        entity.Property(e => e.ShippingAmount).HasColumnType("money");

        entity.Property(e => e.ShippingAmountEur).HasColumnType("money");

        entity.Property(e => e.CashOnDeliveryAmount).HasColumnType("money");

        entity.Property(e => e.IsVatSale).HasDefaultValueSql("0");

        entity.Property(e => e.IsLocked).HasDefaultValueSql("0");

        entity.Property(e => e.IsImported).HasDefaultValueSql("0");

        entity.Property(e => e.IsDevelopment).HasDefaultValueSql("0");

        entity.Property(e => e.IsPrintedActProtocolEdit).HasDefaultValueSql("0");

        entity.Property(e => e.ExpiredDays).HasDefaultValueSql("0.00");

        entity.Property(e => e.Comment).HasMaxLength(450);

        entity.Property(e => e.OneTimeDiscountComment).HasMaxLength(450);

        entity.Ignore(e => e.TotalCount);

        entity.Ignore(e => e.TotalAmount);

        entity.Ignore(e => e.TotalAmountLocal);

        entity.Ignore(e => e.VatAmount);

        entity.Ignore(e => e.IsSent);
        entity.Ignore(e => e.IsInvoice);

        entity.Ignore(e => e.VatAmountPln);

        entity.Ignore(e => e.UserFullName);

        entity.Ignore(e => e.TotalWeight);

        entity.Ignore(e => e.TotalAmountEurToUah);

        entity.Ignore(e => e.IsEdited);

        entity.Ignore(e => e.TotalRowsQty);

        entity.HasOne(e => e.User)
            .WithMany(e => e.Sales)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.TaxFreePackList)
            .WithMany(e => e.Sales)
            .HasForeignKey(e => e.TaxFreePackListId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Sad)
            .WithMany(e => e.Sales)
            .HasForeignKey(e => e.SadId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ChangedToInvoiceBy)
            .WithMany(e => e.SalesChangedToInvoice)
            .HasForeignKey(e => e.ChangedToInvoiceById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Order)
            .WithMany(e => e.Sales)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.WarehousesShipment)
            .WithOne(e => e.Sale)
            .HasForeignKey<WarehousesShipment>(e => e.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ClientAgreement)
            .WithMany(e => e.Sales)
            .HasForeignKey(e => e.ClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.BaseLifeCycleStatus)
            .WithMany(e => e.Sales)
            .HasForeignKey(e => e.BaseLifeCycleStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.BaseSalePaymentStatus)
            .WithMany(e => e.Sales)
            .HasForeignKey(e => e.BaseSalePaymentStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.DeliveryRecipientAddress)
            .WithMany(e => e.Sales)
            .HasForeignKey(e => e.DeliveryRecipientAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.DeliveryRecipient)
            .WithMany(e => e.Sales)
            .HasForeignKey(e => e.DeliveryRecipientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ShiftStatus)
            .WithMany(e => e.Sales)
            .HasForeignKey(e => e.ShiftStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SaleInvoiceDocument)
            .WithMany(e => e.Sales)
            .HasForeignKey(e => e.SaleInvoiceDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SaleInvoiceNumber)
            .WithMany(e => e.Sales)
            .HasForeignKey(e => e.SaleInvoiceNumberId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Transporter)
            .WithMany(e => e.Sales)
            .HasForeignKey(e => e.TransporterId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.RetailClient)
            .WithMany(e => e.Sales)
            .HasForeignKey(e => e.RetailClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Workplace)
            .WithMany(e => e.Sales)
            .HasForeignKey(e => e.WorkplaceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CustomersOwnTtn)
            .WithMany(e => e.Sales)
            .HasForeignKey(e => e.CustomersOwnTtnId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(e => e.NetUid).IsUnique();
    }
}