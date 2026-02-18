using System.Collections.Generic;
using GBA.Domain.Entities.Ecommerce;

namespace GBA.Domain.EntityHelpers;

public sealed class FullSeoPageModel {
    public SeoPage HomePage { get; set; }

    public SeoPage ProductsPage { get; set; }

    public SeoPage AboutCompanyPage { get; set; }

    public SeoPage PhotoGalleryPage { get; set; }

    public SeoPage ContactsPage { get; set; }

    public List<EcommerceContacts> EcommerceContactsList { get; set; }

    public EcommerceContactInfo EcommerceContactInfo { get; set; }

    public RetailPaymentTypeTranslate RetailPaymentTypeTranslate { get; set; }

    public PaymentCard PaymentCard { get; set; }
}