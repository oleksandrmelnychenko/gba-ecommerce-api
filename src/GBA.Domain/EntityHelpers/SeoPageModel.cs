using System.Collections.Generic;
using GBA.Domain.Entities.Ecommerce;

namespace GBA.Domain.EntityHelpers;

public sealed class SeoPageModel {
    public List<SeoPage> EcommercePages { get; set; }

    public List<EcommerceContacts> EcommerceContactsList { get; set; }

    public EcommerceContactInfo EcommerceContactInfo { get; set; }

    public RetailPaymentTypeTranslate RetailPaymentTypeTranslate { get; set; }
}