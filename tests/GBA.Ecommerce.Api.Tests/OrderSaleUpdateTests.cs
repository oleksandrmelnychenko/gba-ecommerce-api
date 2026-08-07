using System.Net.Http;
using GBA.Services.Services.Orders;

namespace GBA.Ecommerce.Api.Tests;

public sealed class OrderSaleUpdateTests {
    [Fact]
    public void Crm_sale_update_rejects_an_empty_operation_identity() {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            OrderService.CreateEcommerceSaleUpdateRequest(
                "http://crm/api/v1/en/sales/update/ecommerce",
                "{\"netUid\":\"sale-1\"}",
                Guid.Empty));

        Assert.Equal("operationNetUid", exception.ParamName);
    }

    [Fact]
    public async Task Crm_sale_update_has_a_unique_operation_identity() {
        Guid firstOperationNetUid = Guid.NewGuid();
        Guid secondOperationNetUid = Guid.NewGuid();
        using HttpRequestMessage firstRequest =
            OrderService.CreateEcommerceSaleUpdateRequest(
                "http://crm/api/v1/en/sales/update/ecommerce",
                "{\"netUid\":\"sale-1\"}",
                firstOperationNetUid);
        using HttpRequestMessage secondRequest =
            OrderService.CreateEcommerceSaleUpdateRequest(
                "http://crm/api/v1/en/sales/update/ecommerce",
                "{\"netUid\":\"sale-2\"}",
                secondOperationNetUid);

        string firstKey = Assert.Single(
            firstRequest.Headers.GetValues("Idempotency-Key"));
        string secondKey = Assert.Single(
            secondRequest.Headers.GetValues("Idempotency-Key"));

        Assert.Equal(firstOperationNetUid.ToString("D"), firstKey);
        Assert.Equal(secondOperationNetUid.ToString("D"), secondKey);
        Assert.Equal(
            "{\"netUid\":\"sale-1\"}",
            await firstRequest.Content!.ReadAsStringAsync());
    }
}
