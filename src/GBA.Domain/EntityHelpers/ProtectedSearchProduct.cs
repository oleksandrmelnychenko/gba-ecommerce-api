using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.EntityHelpers;

public sealed class ProtectedSearchProduct {
    public long Id { get; set; }
    public Guid NetUid { get; set; }
    public string VendorCode { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Size { get; set; }
    public string PackingStandard { get; set; }
    public string OrderStandard { get; set; }
    public string UCGFEA { get; set; }
    public string Volume { get; set; }
    public string Top { get; set; }
    public double AvailableQtyUk { get; set; }
    public double AvailableQtyRoad { get; set; }
    public double AvailableQtyUkVAT { get; set; }
    public double AvailableQtyPl { get; set; }
    public double AvailableQtyPlVAT { get; set; }
    public double Weight { get; set; }
    public bool HasAnalogue { get; set; }
    public bool HasComponent { get; set; }
    public bool HasImage { get; set; }
    public bool IsForWeb { get; set; }
    public bool IsForSale { get; set; }
    public bool IsForZeroSale { get; set; }
    public string MainOriginalNumber { get; set; }
    public List<string> OriginalNumbers { get; set; }
    public string Image { get; set; }
    public long MeasureUnitId { get; set; }

    public string P { get; set; }
    public string CurrencyCode { get; set; }
    public long T { get; set; }

    public ProductSlug ProductSlug { get; set; }

    public static ProtectedSearchProduct FromSearchProduct(FromSearchProduct product, Func<decimal[], long, string> encoder, long timestamp) {
        return new ProtectedSearchProduct {
            Id = product.Id,
            NetUid = product.NetUid,
            VendorCode = product.VendorCode,
            Name = product.Name,
            Description = product.Description,
            Size = product.Size,
            PackingStandard = product.PackingStandard,
            OrderStandard = product.OrderStandard,
            UCGFEA = product.UCGFEA,
            Volume = product.Volume,
            Top = product.Top,
            AvailableQtyUk = product.AvailableQtyUk,
            AvailableQtyRoad = product.AvailableQtyRoad,
            AvailableQtyUkVAT = product.AvailableQtyUkVAT,
            AvailableQtyPl = product.AvailableQtyPl,
            AvailableQtyPlVAT = product.AvailableQtyPlVAT,
            Weight = product.Weight,
            HasAnalogue = product.HasAnalogue,
            HasComponent = product.HasComponent,
            HasImage = product.HasImage,
            IsForWeb = product.IsForWeb,
            IsForSale = product.IsForSale,
            IsForZeroSale = product.IsForZeroSale,
            MainOriginalNumber = product.MainOriginalNumber,
            OriginalNumbers = product.OriginalNumbers,
            Image = product.Image,
            MeasureUnitId = product.MeasureUnitId,
            P = encoder(new[] {
                product.CurrentPrice,
                product.CurrentLocalPrice,
                product.CurrentWithVatPrice,
                product.CurrentLocalWithVatPrice
            }, timestamp),
            CurrencyCode = product.CurrencyCode,
            T = timestamp,
            ProductSlug = product.ProductSlug
        };
    }
}
