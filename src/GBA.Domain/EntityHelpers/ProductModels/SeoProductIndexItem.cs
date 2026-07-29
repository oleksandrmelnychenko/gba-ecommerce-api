using System;

namespace GBA.Domain.EntityHelpers.ProductModels;

public sealed class SeoProductIndexItem {
    public Guid NetUid { get; set; }

    public string VendorCode { get; set; }

    public string SlugUk { get; set; }

    public string SlugRu { get; set; }

    public DateTime Updated { get; set; }
}
