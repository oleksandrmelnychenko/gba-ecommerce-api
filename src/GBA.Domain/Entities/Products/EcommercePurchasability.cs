namespace GBA.Domain.Entities.Products;

/// <summary>
/// Single source of truth for "may this product be put in a cart and bought in the shop?".
///
/// <para>
/// The Product table carries four flags that are easy to confuse. What they actually mean in
/// this system:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <b>Deleted</b> - soft delete. A deleted product is not a product any more. Never sellable.
/// </description></item>
/// <item><description>
/// <b>IsForWeb</b> - "published to the online shop". This is the catalogue-membership flag and
/// the only flag the browse surface filters on
/// (<c>ElasticsearchProductSearchService</c>: <c>term { isForWeb = true }</c>). Anything a
/// shopper can reach in the shop must therefore be buyable, or the shop lies to the shopper.
/// This is the flag that legitimately gates a purchase.
/// </description></item>
/// <item><description>
/// <b>IsForSale</b> - "розпродаж": the product is on PROMOTION. It is a marketing marker, not a
/// permission. <c>dbo.GetCalculatedProductPriceWithSharesAndVat</c> derives
/// <c>@PromotionalProduct = 1</c> from it, and the storefront renders it as a promo badge
/// (<c>product-card.tsx</c>: <c>isPromotion = IsForSale || IsForZeroSale || Top.includes('9')</c>).
/// Only 7,705 of 373,263 web-visible products carry it, so using it as a purchase gate rejected
/// 98% of the catalogue at checkout while the same 98% stayed browsable and addable to the cart.
/// It must NOT gate a purchase.
/// </description></item>
/// <item><description>
/// <b>IsForZeroSale</b> - the same family of promo/clearance markers as IsForSale. Also a
/// display marker, also not a permission.
/// </description></item>
/// </list>
///
/// <para>
/// Hence the gate is: <c>not Deleted AND IsForWeb</c>.
/// </para>
/// <para>
/// Stock is deliberately NOT part of this predicate. An out-of-stock web product is a legitimate
/// backorder, not a rejection: the misplaced-item branch (<c>IsMisplacedItem</c>) in
/// <c>OrderService</c> splits it onto a <c>MisplacedSale</c>. Gating on stock here would silently
/// delete backorderable lines.
/// </para>
/// <para>
/// A resolvable, positive price IS required, but it is checked separately by
/// <see cref="HasSellablePrice"/>: it needs the agreement-resolved price rather than a column,
/// and it deserves its own error so an unpriced product is never booked at 0.00.
/// </para>
/// </summary>
public static class EcommercePurchasability {
    public const string NotAvailableMessage = "The requested product is not available for ecommerce.";

    public const string NotPricedMessage =
        "The requested product has no price for this agreement and cannot be sold.";

    /// <summary>
    /// True when the product exists, is not soft-deleted and is published to the shop.
    /// </summary>
    public static bool IsPurchasable(Product product) {
        return product != null && !product.Deleted && product.IsForWeb;
    }

    /// <summary>
    /// True when the agreement-resolved price is usable for a sale. The pricing UDFs return NULL
    /// when no price applies to the caller's agreement, which Dapper leaves as 0 on the
    /// non-nullable column, so a zero price means "unpriced", not "free".
    /// </summary>
    public static bool HasSellablePrice(Product product) {
        return product != null && product.CurrentPrice > 0m;
    }
}
