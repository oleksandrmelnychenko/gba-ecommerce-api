using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Services.Infrastructure.SalesMutations;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class LegacyCheckoutPolicyTests {
    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void InvalidQuantitiesAreRejected(double quantity) =>
        Assert.Throws<ArgumentException>(() => LegacyCheckoutPolicy.RequireQuantity(quantity));

    [Theory]
    [InlineData(0.125d)]
    [InlineData(1d)]
    [InlineData(double.MaxValue)]
    public void PositiveFiniteQuantitiesAreAccepted(double quantity) =>
        LegacyCheckoutPolicy.RequireQuantity(quantity);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AgreementMustMatchExactCartAndVatMode(bool withVat) {
        ClientShoppingCart cart = new() { ClientAgreementId = 41, IsVatCart = withVat };
        ClientAgreement agreement = ValidAgreement(withVat);

        LegacyCheckoutPolicy.RequireAgreement(cart, agreement);

        agreement.Id = 42;
        Assert.Throws<ArgumentException>(() => LegacyCheckoutPolicy.RequireAgreement(cart, agreement));
        agreement.Id = 41;
        agreement.Agreement.WithVATAccounting = !withVat;
        Assert.Throws<ArgumentException>(() => LegacyCheckoutPolicy.RequireAgreement(cart, agreement));
    }

    [Theory]
    [InlineData("client-agreement-missing")]
    [InlineData("client-agreement-deleted")]
    [InlineData("agreement-missing")]
    [InlineData("agreement-deleted")]
    [InlineData("organization-missing")]
    [InlineData("organization-deleted")]
    public void UnavailableAgreementChainIsRejected(string unavailablePart) {
        ClientShoppingCart cart = new() { ClientAgreementId = 41 };
        ClientAgreement agreement = ValidAgreement(false);
        switch (unavailablePart) {
            case "client-agreement-missing": agreement = null!; break;
            case "client-agreement-deleted": agreement.Deleted = true; break;
            case "agreement-missing": agreement.Agreement = null!; break;
            case "agreement-deleted": agreement.Agreement.Deleted = true; break;
            case "organization-missing": agreement.Agreement.Organization = null!; break;
            case "organization-deleted": agreement.Agreement.Organization.Deleted = true; break;
        }

        Assert.Throws<ArgumentException>(() => LegacyCheckoutPolicy.RequireAgreement(cart, agreement));
    }

    [Fact]
    public void RequestFingerprintCapturesPayloadBeforeLaterMutation() {
        Guid operation = Guid.NewGuid();
        Guid principal = Guid.NewGuid();
        Dictionary<string, object> payload = new() {
            ["Offer"] = Guid.NewGuid(), ["Quantity"] = 2d, ["Comment"] = "original"
        };
        SalesCreationRequest original = LegacyCheckoutExecutor.Request(operation, principal, "offer-checkout-v1", false, payload);
        SalesCreationRequest identical = LegacyCheckoutExecutor.Request(operation, principal, "offer-checkout-v1", false, payload);
        byte[] beforeMutation = original.RequestFingerprint.ToArray();

        payload["Quantity"] = 3d;
        SalesCreationRequest changed = LegacyCheckoutExecutor.Request(operation, principal, "offer-checkout-v1", false, payload);

        Assert.Equal(beforeMutation, original.RequestFingerprint);
        SalesCreationRequestKey.EnsureMatches(identical, Entry(original));
        Assert.Throws<SalesCreationIdempotencyConflictException>(() => SalesCreationRequestKey.EnsureMatches(changed, Entry(original)));
    }

    [Theory]
    [InlineData("principal")]
    [InlineData("operation")]
    [InlineData("flow")]
    [InlineData("mode")]
    [InlineData("offer")]
    [InlineData("comment")]
    public void ReceiptCannotBeReusedAcrossDifferentBindings(string changedPart) {
        Guid operation = Guid.NewGuid();
        Guid principal = Guid.NewGuid();
        Guid offer = Guid.NewGuid();
        SalesCreationRequest original = LegacyCheckoutExecutor.Request(operation, principal, "offer-checkout-v1", false,
            new { Offer = offer, Quantity = 2d, Comment = "original" });
        SalesCreationRequest changed = LegacyCheckoutExecutor.Request(
            changedPart == "operation" ? Guid.NewGuid() : operation,
            changedPart == "principal" ? Guid.NewGuid() : principal,
            changedPart == "flow" ? "cart-checkout-v1" : "offer-checkout-v1",
            changedPart == "mode",
            new {
                Offer = changedPart == "offer" ? Guid.NewGuid() : offer,
                Quantity = 2d,
                Comment = changedPart == "comment" ? "changed" : "original"
            });

        Assert.Throws<SalesCreationIdempotencyConflictException>(() => SalesCreationRequestKey.EnsureMatches(changed, Entry(original)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyOperationOrPrincipalCannotCreateReceiptBinding(bool emptyPrincipal) {
        Assert.Throws<ArgumentException>(() => LegacyCheckoutExecutor.Request(
            emptyPrincipal ? Guid.NewGuid() : Guid.Empty,
            emptyPrincipal ? Guid.Empty : Guid.NewGuid(),
            "cart-checkout-v1", false, new { WithVat = false }));
    }

    private static ClientAgreement ValidAgreement(bool withVat) => new() {
        Id = 41,
        Agreement = new Agreement {
            Id = 51, WithVATAccounting = withVat,
            Organization = new Organization { Id = 61 }
        }
    };

    private static SalesCreationLedgerEntry Entry(SalesCreationRequest request) => new() {
        OperationNetUid = request.OperationNetUid,
        OperationName = request.OperationName,
        PrincipalNetUid = request.PrincipalNetUid,
        ClientNetUid = request.ClientNetUid,
        ModeFlag = request.ModeFlag,
        RequestFingerprint = request.RequestFingerprint
    };
}
