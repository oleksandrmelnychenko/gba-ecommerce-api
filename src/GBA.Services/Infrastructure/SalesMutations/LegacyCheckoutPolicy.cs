using System;
using System.Data;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Common.ResourceNames;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Services.Infrastructure.SalesMutations;

/// <summary>Checks persisted checkout ownership and performs narrowly scoped transactional writes.</summary>
public static class LegacyCheckoutPolicy {
    /// <summary>Acquires the existing ecommerce product locks in ascending identity order.</summary>
    public static void LockProducts(IDbConnection connection, IEnumerable<long> products) {
        foreach (long id in products.Distinct().OrderBy(id => id)) {
            if (id <= 0) throw new ArgumentException("A product identity is required.");
            int result = connection.ExecuteScalar<int>("""
                DECLARE @result int;
                EXEC @result=sys.sp_getapplock @Resource=@resource,@LockMode=N'Exclusive',
                    @LockOwner=N'Transaction',@LockTimeout=60000;
                SELECT @result;
                """, new { resource = $"gba-ecommerce:product:{id}" });
            if (result < 0) throw new TimeoutException("Unable to lock checkout products.");
        }
    }
    /// <summary>Locks a live, unexpired cart or unprocessed offer owned by the exact authenticated root client.</summary>
    /// <remarks>Workplace carts and another client's agreements are deliberately outside these two checkout routes.</remarks>
    public static void RequireCart(IDbConnection connection, ClientShoppingCart cart, Guid principal, bool offer) {
        if (cart == null || cart.Id <= 0 || cart.Deleted || cart.IsOffer != offer || cart.WorkplaceId.HasValue
            || cart.ValidUntil.Date < DateTime.Now.Date || offer && cart.IsOfferProcessed)
            throw new ArgumentException("The shopping cart or offer is unavailable or expired.");
        int matches = connection.ExecuteScalar<int>("""
            SELECT COUNT(*) FROM dbo.ClientShoppingCart c WITH(UPDLOCK,HOLDLOCK)
            JOIN dbo.ClientAgreement a WITH(HOLDLOCK) ON a.ID=c.ClientAgreementID
            JOIN dbo.Client cl WITH(HOLDLOCK) ON cl.ID=a.ClientID
            WHERE c.ID=@Id AND c.NetUID=@NetUid AND c.ClientAgreementID=@ClientAgreementId
                AND c.Deleted=0 AND c.IsOffer=@offer AND c.WorkplaceID IS NULL
                AND a.Deleted=0 AND cl.Deleted=0 AND cl.NetUID=@principal
                AND (@offer=0 OR c.IsOfferProcessed=0);
            """, new { cart.Id, cart.NetUid, cart.ClientAgreementId, offer, principal });
        if (matches != 1) throw new ArgumentException("The shopping cart or offer does not belong to this client.");
    }

    /// <summary>Requires the cart's persisted agreement and VAT mode, not a newly selected client default.</summary>
    public static void RequireAgreement(ClientShoppingCart cart, ClientAgreement agreement) {
        if (agreement == null || agreement.Id != cart.ClientAgreementId || agreement.Deleted
            || agreement.Agreement == null || agreement.Agreement.Deleted
            || agreement.Agreement.Organization == null || agreement.Agreement.Organization.Deleted
            || agreement.Agreement.WithVATAccounting != cart.IsVatCart)
            throw new ArgumentException("The cart agreement is unavailable or its VAT mode has changed.");
    }

    /// <summary>Rejects nonfinite, zero and negative requested quantities before reservation changes.</summary>
    public static void RequireQuantity(double quantity) {
        if (!double.IsFinite(quantity) || quantity <= 0)
            throw new ArgumentException("Item quantity must be finite and greater than zero.");
    }

    /// <summary>Moves exactly the observed cart items without overwriting their quantity or financial fields.</summary>
    public static void MoveCartItems(IDbConnection connection, ClientShoppingCart cart, long orderId) {
        foreach (OrderItem item in cart.OrderItems.OrderBy(item => item.Id)) {
            RequireQuantity(item.Qty);
            int changed = connection.Execute("""
                UPDATE dbo.OrderItem SET ClientShoppingCartID=NULL,OrderID=@orderId,Updated=GETUTCDATE()
                WHERE ID=@Id AND NetUID=@NetUid AND ClientShoppingCartID=@cartId
                    AND OrderID IS NULL AND ProductID=@ProductId AND Qty=@Qty AND Deleted=0;
                """, new { item.Id, item.NetUid, item.ProductId, item.Qty, cartId = cart.Id, orderId });
            if (changed != 1) throw new SalesCreationIdempotencyConflictException("A cart item changed during checkout.");
        }
    }

    /// <summary>Allocates the next current-month eight-digit suffix while holding the matching SQL range.</summary>
    public static long AddSaleNumber(IDbConnection connection, ISaleNumberRepository repository, ClientAgreement agreement) {
        string prefix = agreement.Agreement.Organization.Code + MonthCodesResourceNames.GetCurrentMonthCode();
        long maximum = connection.ExecuteScalar<long>("""
            SELECT ISNULL(MAX(TRY_CONVERT(bigint,SUBSTRING(Value,LEN(@prefix)+1,8))),0)
            FROM dbo.SaleNumber WITH(UPDLOCK,HOLDLOCK)
            WHERE OrganizationID=@organizationId AND LEFT(Value,LEN(@prefix))=@prefix
                AND LEN(Value)=LEN(@prefix)+8 AND YEAR(Created)=@year;
            """, new { prefix, organizationId = agreement.Agreement.Organization.Id, year = DateTime.Now.Year });
        if (maximum >= 99999999) throw new InvalidOperationException("Sale number sequence is exhausted.");
        return repository.Add(new SaleNumber { OrganizationId = agreement.Agreement.Organization.Id,
            Value = prefix + (maximum + 1).ToString("D8", CultureInfo.InvariantCulture) });
    }
}
