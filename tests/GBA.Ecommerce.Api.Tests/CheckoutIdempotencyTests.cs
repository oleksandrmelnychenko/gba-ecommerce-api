using System;
using System.Collections.Generic;
using System.Text.Json;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;
using GBA.Services.Infrastructure.Checkout;

namespace GBA.Ecommerce.Api.Tests;

public sealed class CheckoutIdempotencyTests {
    [Theory]
    [InlineData("4c1f9f4e-5f0b-4b8e-9d0a-1c2b3d4e5f60")]
    [InlineData(" 4C1F9F4E-5F0B-4B8E-9D0A-1C2B3D4E5F60 ")]
    public void Idempotency_key_accepts_a_non_empty_uuid(string header) {
        Assert.True(CheckoutIdempotencyKey.TryParse(header, out Guid key));
        Assert.Equal(Guid.Parse("4c1f9f4e-5f0b-4b8e-9d0a-1c2b3d4e5f60"), key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-uuid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("4c1f9f4e-5f0b-4b8e-9d0a-1c2b3d4e5f60-4c1f9f4e-5f0b-4b8e-9d0a-1c2b3d4e5f60")]
    public void Idempotency_key_rejects_missing_empty_or_malformed_values(string? header) {
        Assert.False(CheckoutIdempotencyKey.TryParse(header!, out Guid key));
        Assert.Equal(Guid.Empty, key);
    }

    [Fact]
    public void Fingerprint_is_stable_and_length_prefixed() {
        Assert.Equal(CheckoutFingerprint.Compute("a", "bc"), CheckoutFingerprint.Compute("a", "bc"));
        Assert.NotEqual(CheckoutFingerprint.Compute("ab", "c"), CheckoutFingerprint.Compute("a", "bc"));
        Assert.Equal(CheckoutFingerprint.Length, CheckoutFingerprint.Compute().Length);
    }

    [Fact]
    public void Invoice_fingerprint_changes_with_any_part_of_the_submitted_checkout() {
        byte[] baseline = CheckoutFingerprint.ForInvoice("0", "{\"Comment\":\"a\"}", "TTN-1", new byte[] { 1, 2 });

        Assert.Equal(baseline, CheckoutFingerprint.ForInvoice("0", "{\"Comment\":\"a\"}", "TTN-1", new byte[] { 1, 2 }));
        Assert.NotEqual(baseline, CheckoutFingerprint.ForInvoice("1", "{\"Comment\":\"a\"}", "TTN-1", new byte[] { 1, 2 }));
        Assert.NotEqual(baseline, CheckoutFingerprint.ForInvoice("0", "{\"Comment\":\"b\"}", "TTN-1", new byte[] { 1, 2 }));
        Assert.NotEqual(baseline, CheckoutFingerprint.ForInvoice("0", "{\"Comment\":\"a\"}", "TTN-2", new byte[] { 1, 2 }));
        Assert.NotEqual(baseline, CheckoutFingerprint.ForInvoice("0", "{\"Comment\":\"a\"}", "TTN-1", new byte[] { 1, 3 }));
        Assert.NotEqual(baseline, CheckoutFingerprint.ForInvoice("0", "{\"Comment\":\"a\"}", "TTN-1", null!));
    }

    [Fact]
    public void Offer_fingerprint_ignores_item_order_but_not_selection_quantity_or_cart_mode() {
        Guid offerNetUid = Guid.NewGuid();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        byte[] ordered = CheckoutFingerprint.ForOffer(Offer(offerNetUid, (first, 1), (second, 2.5)), false);

        Assert.Equal(ordered, CheckoutFingerprint.ForOffer(Offer(offerNetUid, (second, 2.5), (first, 1)), false));
        Assert.NotEqual(ordered, CheckoutFingerprint.ForOffer(Offer(offerNetUid, (first, 1), (second, 3)), false));
        Assert.NotEqual(ordered, CheckoutFingerprint.ForOffer(Offer(offerNetUid, (first, 1)), false));
        Assert.NotEqual(ordered, CheckoutFingerprint.ForOffer(Offer(offerNetUid, (first, 1), (second, 2.5)), true));
        Assert.NotEqual(ordered, CheckoutFingerprint.ForOffer(Offer(Guid.NewGuid(), (first, 1), (second, 2.5)), false));
    }

    [Fact]
    public void Cart_fingerprint_is_bound_to_the_vat_mode() {
        Assert.Equal(CheckoutFingerprint.ForCart(true), CheckoutFingerprint.ForCart(true));
        Assert.NotEqual(CheckoutFingerprint.ForCart(true), CheckoutFingerprint.ForCart(false));
    }

    [Fact]
    public void Attempt_requires_a_key_and_a_sha256_fingerprint() {
        Assert.Throws<ArgumentException>(() => new CheckoutAttempt(Guid.Empty, CheckoutFingerprint.ForCart(false)));
        Assert.Throws<ArgumentException>(() => new CheckoutAttempt(Guid.NewGuid(), new byte[] { 1 }));
        Assert.Throws<ArgumentException>(() => new CheckoutAttempt(Guid.NewGuid(), null!));
    }

    [Fact]
    public void Request_matches_only_its_own_receipt() {
        Guid principal = Guid.NewGuid();
        CheckoutAttempt attempt = new(Guid.NewGuid(), CheckoutFingerprint.ForCart(false));
        CheckoutRequest request = new("operation", attempt, principal, principal, false);

        Assert.True(request.IsSameRequestAs(Entry(request)));
        Assert.False(request.IsSameRequestAs(null!));

        CheckoutLedgerEntry otherFingerprint = Entry(request);
        otherFingerprint.RequestFingerprint = CheckoutFingerprint.ForCart(true);
        Assert.False(request.IsSameRequestAs(otherFingerprint));

        CheckoutLedgerEntry otherPrincipal = Entry(request);
        otherPrincipal.PrincipalNetUid = Guid.NewGuid();
        Assert.False(request.IsSameRequestAs(otherPrincipal));

        CheckoutLedgerEntry otherOperation = Entry(request);
        otherOperation.OperationName = "other";
        Assert.False(request.IsSameRequestAs(otherOperation));

        CheckoutLedgerEntry otherMode = Entry(request);
        otherMode.ModeFlag = true;
        Assert.False(request.IsSameRequestAs(otherMode));
    }

    [Fact]
    public void Request_rejects_operations_that_do_not_fit_the_ledger() {
        CheckoutAttempt attempt = new(Guid.NewGuid(), CheckoutFingerprint.ForCart(false));

        Assert.Throws<ArgumentException>(() =>
            new CheckoutRequest(new string('x', CheckoutRequest.MaxOperationNameLength + 1), attempt, Guid.NewGuid(), Guid.NewGuid(), false));
        Assert.Throws<ArgumentException>(() =>
            new CheckoutRequest("operation", attempt, Guid.Empty, Guid.NewGuid(), false));
    }

    [Fact]
    public void Stored_receipt_round_trips_the_created_sale_response() {
        Sale created = new() {
            Id = 41,
            NetUid = Guid.NewGuid(),
            Created = new DateTime(2026, 10, 18, 10, 11, 12, DateTimeKind.Utc),
            OrderId = 77,
            ClientAgreementId = 9,
            Comment = "Leave at the door",
            IsVatSale = true,
            SaleNumber = new SaleNumber { Id = 5, Value = "K1000000042", OrganizationId = 3 },
            Order = new Order { Id = 77, NetUid = Guid.NewGuid(), OrderSource = OrderSource.Shop }
        };

        Sale response = CheckoutSaleResponse.From(created);
        string payload = JsonSerializer.Serialize(response, CheckoutExecutor.ReceiptJsonOptions);
        Sale? replay = JsonSerializer.Deserialize<Sale>(payload, CheckoutExecutor.ReceiptJsonOptions);

        Assert.NotNull(replay);
        Assert.Equal(created.Id, replay.Id);
        Assert.Equal(created.NetUid, replay.NetUid);
        Assert.Equal(created.Created, replay.Created);
        Assert.Equal(created.OrderId, replay.OrderId);
        Assert.Equal(created.Comment, replay.Comment);
        Assert.True(replay.IsVatSale);
        Assert.Equal("K1000000042", replay.SaleNumber.Value);
        Assert.Equal(created.Order.NetUid, replay.Order.NetUid);
        Assert.Equal(
            JsonSerializer.Serialize(response, CheckoutExecutor.ReceiptJsonOptions),
            JsonSerializer.Serialize(replay, CheckoutExecutor.ReceiptJsonOptions));
    }

    private static ClientShoppingCart Offer(Guid netUid, params (Guid NetUid, double Qty)[] items) {
        ClientShoppingCart offer = new() { NetUid = netUid, OrderItems = new List<OrderItem>() };
        foreach ((Guid itemNetUid, double qty) in items)
            offer.OrderItems.Add(new OrderItem { NetUid = itemNetUid, Qty = qty });
        return offer;
    }

    private static CheckoutLedgerEntry Entry(CheckoutRequest request) {
        return new CheckoutLedgerEntry {
            OperationNetUid = request.OperationNetUid,
            OperationName = request.OperationName,
            PrincipalNetUid = request.PrincipalNetUid,
            ClientNetUid = request.ClientNetUid,
            ModeFlag = request.ModeFlag,
            RequestFingerprint = (byte[])request.RequestFingerprint.Clone()
        };
    }
}
