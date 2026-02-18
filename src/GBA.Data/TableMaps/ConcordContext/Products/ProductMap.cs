using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductMap : EntityBaseMap<Product> {
    public override void Map(EntityTypeBuilder<Product> entity) {
        base.Map(entity);

        entity.ToTable("Product");

        entity.Property(e => e.MeasureUnitId).HasColumnName("MeasureUnitID");

        entity.Property(e => e.ParentAmgId).HasColumnName("ParentAmgID");

        entity.Property(e => e.ParentFenixId).HasColumnName("ParentFenixID");

        entity.Property(e => e.SourceAmgId).HasColumnName("SourceAmgID");

        entity.Property(e => e.SourceFenixId).HasColumnName("SourceFenixID");

        entity.Property(e => e.ParentAmgId).HasMaxLength(16);

        entity.Property(e => e.ParentFenixId).HasMaxLength(16);

        entity.Property(e => e.SourceAmgId).HasMaxLength(16);

        entity.Property(e => e.SourceFenixId).HasMaxLength(16);

        entity.Property(e => e.Top).HasMaxLength(3);

        entity.Ignore(e => e.CurrentPrice);

        entity.Ignore(e => e.CurrentPriceReSale);

        entity.Ignore(e => e.CurrentLocalPriceReSale);

        entity.Ignore(e => e.CurrentLocalPrice);

        entity.Ignore(e => e.CurrentWithVatPrice);

        entity.Ignore(e => e.CurrentLocalWithVatPrice);

        entity.Ignore(e => e.NextSearchedProducts);

        entity.Ignore(e => e.ProductIncomes);

        entity.Ignore(e => e.CalculatedPrices);

        entity.Ignore(e => e.Notes);

        entity.Ignore(e => e.RefId);

        entity.Ignore(e => e.ProductSlug);

        entity.Ignore(e => e.AvailableQtyPl);

        entity.Ignore(e => e.AvailableQtyUk);

        entity.Ignore(e => e.AvailableQtyPlVAT);

        entity.Ignore(e => e.AvailableQtyUkVAT);

        entity.Ignore(e => e.AvailableDefectiveQtyUk);

        entity.Ignore(e => e.AvailableQtyRoad);

        entity.Ignore(e => e.AvailableDefectiveQtyPl);

        entity.Ignore(e => e.AvailableQtyUkReSale);

        entity.Ignore(e => e.CurrentPriceEurToUah);

        entity.Ignore(e => e.CurrentPriceReSaleEurToUah);

        entity.Ignore(e => e.ProductGroupNames);

        entity.Ignore(e => e.CurrencyCode);

        entity.HasOne(e => e.MeasureUnit)
            .WithMany(e => e.Products)
            .HasForeignKey(e => e.MeasureUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.Property(e => e.Name).HasMaxLength(120);

        entity.Property(e => e.NameUA).HasMaxLength(120);

        entity.Property(e => e.NamePL).HasMaxLength(120);

        entity.Property(e => e.SearchName).HasMaxLength(120);

        entity.Property(e => e.SearchNameUA).HasMaxLength(120);

        entity.Property(e => e.SearchNamePL).HasMaxLength(120);

        entity.Property(e => e.Description).HasMaxLength(2000);

        entity.Property(e => e.DescriptionUA).HasMaxLength(2000);

        entity.Property(e => e.DescriptionPL).HasMaxLength(2000);

        entity.Property(e => e.SearchDescription).HasMaxLength(2000);

        entity.Property(e => e.SearchDescriptionUA).HasMaxLength(2000);

        entity.Property(e => e.SearchDescriptionPL).HasMaxLength(2000);

        entity.Property(e => e.NotesUA).HasMaxLength(2000);

        entity.Property(e => e.NotesPL).HasMaxLength(2000);

        entity.Property(e => e.SynonymsUA).HasMaxLength(2000);

        entity.Property(e => e.SynonymsPL).HasMaxLength(2000);

        entity.Property(e => e.SearchSynonymsUA).HasMaxLength(2000);

        entity.Property(e => e.SearchSynonymsPL).HasMaxLength(2000);

        entity.Property(e => e.MainOriginalNumber).HasMaxLength(80);

        entity.Property(e => e.VendorCode).HasMaxLength(40);

        entity.Property(e => e.SearchVendorCode).HasMaxLength(40);

        entity.Property(e => e.Size).HasMaxLength(100);

        entity.Property(e => e.SearchSize).HasMaxLength(100);

        entity.HasIndex(e => new { e.Name, e.Deleted }).IsUnique(false);

        entity.HasIndex(e => new { e.MainOriginalNumber, e.Deleted }).IsUnique(false);

        entity.HasIndex(e => new { e.Description, e.Deleted }).IsUnique(false);

        entity.HasIndex(e => new { e.VendorCode, e.Deleted }).IsUnique(false);

        entity.HasIndex(e => new { e.Deleted, e.SearchNamePL, e.SearchVendorCode }).IsUnique(false);

        entity.HasIndex(e => new { e.Deleted, e.SearchNameUA, e.SearchVendorCode }).IsUnique(false);

        //entity.HasIndex(e => new { e.Deleted, e.Id, e.Description, e.MainOriginalNumber, e.Name, e.VendorCode, e.SearchDescriptionPL, e.SearchNamePL });

        //entity.HasIndex(e => new { e.Deleted, e.Id, e.Description, e.MainOriginalNumber, e.Name, e.VendorCode, e.DescriptionUA, e.NameUA });

        entity.HasIndex(e => e.NetUid).IsUnique();
    }
}