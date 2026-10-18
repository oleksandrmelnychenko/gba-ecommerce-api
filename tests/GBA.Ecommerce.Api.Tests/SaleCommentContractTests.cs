using GBA.Data;
using GBA.Domain.Entities.Sales;
using GBA.Services.Services.Orders;
using Microsoft.EntityFrameworkCore;

namespace GBA.Ecommerce.Api.Tests;

public sealed class SaleCommentContractTests {
    [Fact]
    public void Comment_over_the_database_limit_is_rejected_before_sale_creation() {
        Sale sale = new() {
            Comment = new string('К', Sale.CommentMaxLength + 1)
        };

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => OrderService.ValidateSaleComment(sale));

        Assert.Equal("sale", exception.ParamName);
        Assert.Contains(Sale.CommentMaxLength.ToString(), exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(Sale.CommentMaxLength)]
    public void Comment_at_or_below_the_database_limit_is_accepted(int length) {
        Sale sale = new() {
            Comment = new string('К', length)
        };

        OrderService.ValidateSaleComment(sale);
    }

    [Fact]
    public void Entity_mapping_uses_the_same_comment_limit_as_creation_validation() {
        DbContextOptions<ConcordContext> options =
            new DbContextOptionsBuilder<ConcordContext>()
                .UseSqlServer("Server=localhost;Database=contract-only;Integrated Security=true;TrustServerCertificate=true")
                .Options;
        using ConcordContext context = new(options);

        int? mappedMaxLength = context.Model
            .FindEntityType(typeof(Sale))!
            .FindProperty(nameof(Sale.Comment))!
            .GetMaxLength();

        Assert.Equal(Sale.CommentMaxLength, mappedMaxLength);
    }
}
