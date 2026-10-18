using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Services.Services.Orders;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class OrderProductReferenceTests {
    [Fact]
    public void Nested_product_net_id_remains_the_preferred_reference() {
        Guid expectedNetId = Guid.NewGuid();
        Mock<IGetSingleProductRepository> repository = new(MockBehavior.Strict);
        OrderItem orderItem = new() {
            ProductId = 41,
            Product = new Product {
                Id = 42,
                NetUid = expectedNetId
            }
        };

        Guid actualNetId = OrderService.ResolveProductNetId(repository.Object, orderItem);

        Assert.Equal(expectedNetId, actualNetId);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public void Top_level_product_id_is_resolved_to_the_canonical_net_id() {
        Guid expectedNetId = Guid.NewGuid();
        Mock<IGetSingleProductRepository> repository = new();
        repository
            .Setup(candidate => candidate.GetById(42))
            .Returns(new Product { Id = 42, NetUid = expectedNetId });
        OrderItem orderItem = new() { ProductId = 42 };

        Guid actualNetId = OrderService.ResolveProductNetId(repository.Object, orderItem);

        Assert.Equal(expectedNetId, actualNetId);
        repository.Verify(candidate => candidate.GetById(42), Times.Once);
    }

    [Fact]
    public void Nested_product_id_is_supported_when_the_net_id_is_missing() {
        Guid expectedNetId = Guid.NewGuid();
        Mock<IGetSingleProductRepository> repository = new();
        repository
            .Setup(candidate => candidate.GetById(42))
            .Returns(new Product { Id = 42, NetUid = expectedNetId });
        OrderItem orderItem = new() {
            Product = new Product { Id = 42 }
        };

        Guid actualNetId = OrderService.ResolveProductNetId(repository.Object, orderItem);

        Assert.Equal(expectedNetId, actualNetId);
        repository.Verify(candidate => candidate.GetById(42), Times.Once);
    }

    [Fact]
    public void Vendor_code_is_supported_for_legacy_guest_cart_items() {
        Guid expectedNetId = Guid.NewGuid();
        Mock<IGetSingleProductRepository> repository = new();
        repository
            .Setup(candidate => candidate.GetProductByVendorCode("SEM9490"))
            .Returns(new Product { NetUid = expectedNetId, VendorCode = "SEM9490" });
        OrderItem orderItem = new() {
            Product = new Product { VendorCode = " SEM9490 " }
        };

        Guid actualNetId = OrderService.ResolveProductNetId(repository.Object, orderItem);

        Assert.Equal(expectedNetId, actualNetId);
        repository.Verify(
            candidate => candidate.GetProductByVendorCode("SEM9490"),
            Times.Once);
    }

    [Fact]
    public void Missing_or_unknown_product_reference_stays_invalid() {
        Mock<IGetSingleProductRepository> repository = new();
        repository
            .Setup(candidate => candidate.GetById(404))
            .Returns((Product)null!);
        repository
            .Setup(candidate => candidate.GetProductByVendorCode("UNKNOWN"))
            .Returns((Product)null!);

        Assert.Equal(
            Guid.Empty,
            OrderService.ResolveProductNetId(repository.Object, new OrderItem()));
        Assert.Equal(
            Guid.Empty,
            OrderService.ResolveProductNetId(
                repository.Object,
                new OrderItem { ProductId = 404 }));
        Assert.Equal(
            Guid.Empty,
            OrderService.ResolveProductNetId(
                repository.Object,
                new OrderItem {
                    Product = new Product { VendorCode = "UNKNOWN" }
                }));
    }
}
