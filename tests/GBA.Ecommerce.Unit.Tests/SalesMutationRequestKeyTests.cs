using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Services.Infrastructure;
using GBA.Services.Infrastructure.SalesMutations;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class SalesMutationRequestKeyTests {
    [Fact]
    public void AddTo_ReusesOneDeterministicKeyForTheSameFrozenOutboundRequest() {
        const string operation = "Order invoice sale update";
        const string payload = "{\"NetUid\":\"sale-1\",\"Qty\":2}";
        using HttpRequestMessage first = new(HttpMethod.Post, "https://crm.test/sales/update/ecommerce");
        using HttpRequestMessage retry = new(HttpMethod.Post, "https://crm.test/sales/update/ecommerce");

        Guid firstKey = SalesMutationRequestKey.AddTo(first, operation, payload);
        Guid retryKey = SalesMutationRequestKey.AddTo(retry, operation, payload);

        Assert.NotEqual(Guid.Empty, firstKey);
        Assert.Equal(firstKey, retryKey);
        Assert.Equal(firstKey.ToString("D"), first.Headers.GetValues(SalesMutationRequestKey.HeaderName).Single());
        Assert.Equal(retryKey.ToString("D"), retry.Headers.GetValues(SalesMutationRequestKey.HeaderName).Single());
    }

    [Fact]
    public void Create_ChangesWhenOutboundPayloadOrMutationKindChanges() {
        Guid baseline = SalesMutationRequestKey.Create("Retail sale update", "{\"Qty\":1}");

        Assert.NotEqual(baseline, SalesMutationRequestKey.Create("Retail sale update", "{\"Qty\":2}"));
        Assert.NotEqual(baseline, SalesMutationRequestKey.Create("Quick sale update", "{\"Qty\":1}"));
    }

    [Fact]
    public void Prepare_WithExplicitInboundKey_PreservesThatKeyInThePayload() {
        Guid operationNetUid = Guid.NewGuid();

        SalesMutationRequestKey.PreparedSalesMutation prepared =
            SalesMutationRequestKey.Prepare(
                SalesMutationOperationNames.RetailSaleUpdate,
                "{\"NetUid\":\"sale-1\",\"OperationNetUid\":\"00000000-0000-0000-0000-000000000000\"}",
                operationNetUid);

        Assert.Equal(operationNetUid, prepared.OperationNetUid);
        Assert.Contains(operationNetUid.ToString("D"), prepared.Payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Guid.Empty.ToString("D"), prepared.Payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveInboundKey_ValidHeaderHasPriorityOverSaleNetUid() {
        Guid headerKey = Guid.NewGuid();
        Guid saleKey = Guid.NewGuid();

        bool valid = SalesCreationRequestKey.TryResolveInboundKey(
            headerKey.ToString("D"),
            saleKey,
            out Guid resolved);

        Assert.True(valid);
        Assert.Equal(headerKey, resolved);
    }

    [Fact]
    public void ResolveInboundKey_MissingHeaderUsesBackwardCompatibleSaleNetUid() {
        Guid saleKey = Guid.NewGuid();

        Assert.True(SalesCreationRequestKey.TryResolveInboundKey(null, saleKey, out Guid resolved));
        Assert.Equal(saleKey, resolved);
    }

    [Fact]
    public void ResolveInboundKey_InvalidPresentHeaderDoesNotFallbackToSaleNetUid() {
        Assert.False(SalesCreationRequestKey.TryResolveInboundKey(
            "not-a-uuid",
            Guid.NewGuid(),
            out Guid resolved));
        Assert.Equal(Guid.Empty, resolved);
        Assert.False(SalesCreationRequestKey.TryResolveInboundKey(
            string.Empty,
            Guid.NewGuid(),
            out resolved));
        Assert.Equal(Guid.Empty, resolved);
    }

    [Fact]
    public void ResolveInboundKey_WithoutHeaderOrExplicitSaleKeyFailsClosed() {
        Assert.False(SalesCreationRequestKey.TryResolveInboundKey(null, Guid.Empty, out Guid resolved));
        Assert.Equal(Guid.Empty, resolved);
    }

    [Fact]
    public void CreationFingerprint_PreservesOrderItemEnumerationOrder() {
        Guid operationNetUid = Guid.NewGuid();
        Guid clientNetUid = Guid.NewGuid();
        Sale first = CreateSale((17, 2d), (29, 4d));
        Sale retry = CreateSale((29, 4d), (17, 2d));

        SalesCreationRequest firstRequest = CreateRequest(
            operationNetUid,
            clientNetUid,
            false,
            first);
        SalesCreationRequest retryRequest = CreateRequest(
            operationNetUid,
            clientNetUid,
            false,
            retry);

        Assert.NotEqual(firstRequest.RequestFingerprint, retryRequest.RequestFingerprint);
        Assert.Throws<SalesCreationIdempotencyConflictException>(() =>
            SalesCreationRequestKey.EnsureMatches(retryRequest, ToEntry(firstRequest)));
    }

    [Fact]
    public void CreationFingerprint_UsesAttachmentButIgnoresServerGeneratedTtnPath() {
        Guid operationNetUid = Guid.NewGuid();
        Guid clientNetUid = Guid.NewGuid();
        Sale first = CreateSale((17, 2d));
        first.CustomersOwnTtn = new CustomersOwnTtn {
            Number = "TTN-1",
            TtnPDFPath = "/generated/first.pdf"
        };
        Sale retry = CreateSale((17, 2d));
        retry.CustomersOwnTtn = new CustomersOwnTtn {
            Number = "TTN-1",
            TtnPDFPath = "/generated/retry.pdf"
        };
        byte[] firstFile = Enumerable.Repeat((byte)1, 32).ToArray();
        byte[] differentFile = Enumerable.Repeat((byte)2, 32).ToArray();

        SalesCreationRequest baseline = SalesCreationRequestKey.Create(
            operationNetUid,
            SalesMutationOperationNames.OrderInvoiceSaleUpdate,
            clientNetUid,
            clientNetUid,
            false,
            first,
            firstFile);
        SalesCreationRequest exactRetry = SalesCreationRequestKey.Create(
            operationNetUid,
            SalesMutationOperationNames.OrderInvoiceSaleUpdate,
            clientNetUid,
            clientNetUid,
            false,
            retry,
            firstFile);
        SalesCreationRequest changedFile = SalesCreationRequestKey.Create(
            operationNetUid,
            SalesMutationOperationNames.OrderInvoiceSaleUpdate,
            clientNetUid,
            clientNetUid,
            false,
            retry,
            differentFile);

        Assert.Equal(baseline.RequestFingerprint, exactRetry.RequestFingerprint);
        Assert.NotEqual(baseline.RequestFingerprint, changedFile.RequestFingerprint);
    }

    [Theory]
    [InlineData("operation")]
    [InlineData("principal")]
    [InlineData("client")]
    [InlineData("mode")]
    [InlineData("payload")]
    public void EnsureMatches_RejectsAnyBindingMismatch(string mismatch) {
        Guid operationNetUid = Guid.NewGuid();
        Guid principalNetUid = Guid.NewGuid();
        Guid clientNetUid = Guid.NewGuid();
        SalesCreationRequest baseline = SalesCreationRequestKey.Create(
            operationNetUid,
            SalesMutationOperationNames.RetailSaleUpdate,
            principalNetUid,
            clientNetUid,
            false,
            CreateSale((17, 2d)));
        SalesCreationRequest changed = SalesCreationRequestKey.Create(
            mismatch == "operation" ? Guid.NewGuid() : operationNetUid,
            mismatch == "operation" ? SalesMutationOperationNames.QuickSaleUpdate : baseline.OperationName,
            mismatch == "principal" ? Guid.NewGuid() : principalNetUid,
            mismatch == "client" ? Guid.NewGuid() : clientNetUid,
            mismatch == "mode",
            CreateSale((17, mismatch == "payload" ? 3d : 2d)));

        SalesCreationLedgerEntry entry = ToEntry(baseline);

        Assert.Throws<SalesCreationIdempotencyConflictException>(() =>
            SalesCreationRequestKey.EnsureMatches(changed, entry));
    }

    private static SalesCreationRequest CreateRequest(
        Guid operationNetUid,
        Guid clientNetUid,
        bool mode,
        Sale sale) =>
        SalesCreationRequestKey.Create(
            operationNetUid,
            SalesMutationOperationNames.RetailSaleUpdate,
            clientNetUid,
            clientNetUid,
            mode,
            sale);

    private static SalesCreationLedgerEntry ToEntry(SalesCreationRequest request) => new() {
        OperationNetUid = request.OperationNetUid,
        OperationName = request.OperationName,
        PrincipalNetUid = request.PrincipalNetUid,
        ClientNetUid = request.ClientNetUid,
        ModeFlag = request.ModeFlag,
        RequestFingerprint = request.RequestFingerprint,
        ResponsePayload = "{\"NetUid\":\"95483133-e651-4d4a-aac0-b0792142b37b\"}",
        CreatedUtc = new DateTime(2026, 7, 15, 8, 0, 0, DateTimeKind.Utc),
        CompletedUtc = new DateTime(2026, 7, 15, 8, 0, 1, DateTimeKind.Utc)
    };

    private static Sale CreateSale(params (long ProductId, double Qty)[] items) {
        Sale sale = new() {
            Comment = "same request",
            Order = new Order()
        };
        foreach ((long productId, double qty) in items)
            sale.Order.OrderItems.Add(new OrderItem {
                ProductId = productId,
                Qty = qty,
                Product = new Product {
                    Id = productId,
                    NetUid = Guid.Parse($"00000000-0000-0000-0000-{productId:D12}")
                }
            });
        return sale;
    }
}
