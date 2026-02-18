using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class SaleInvoiceDocumentRepository : ISaleInvoiceDocumentRepository {
    private readonly IDbConnection _connection;

    public SaleInvoiceDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SaleInvoiceDocument saleInvoiceDocument) {
        return _connection.Query<long>(
                "INSERT INTO [SaleInvoiceDocument] (PaymentType, ClientPaymentType, City, Vat, ShippingAmount, ShippingAmountEur, ShippingAmountWithoutVat, ShippingAmountEurWithoutVat, " +
                "Updated) " +
                "VALUES (@PaymentType, @ClientPaymentType, @City, @Vat, @ShippingAmount, 0.00, @ShippingAmount, 0.00, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                saleInvoiceDocument
            )
            .Single();
    }

    public void Update(SaleInvoiceDocument saleInvoiceDocument) {
        _connection.Execute(
            "UPDATE [SaleInvoiceDocument] " +
            "SET PaymentType = @PaymentType, ClientPaymentType = @ClientPaymentType, City = @City, Vat = @Vat, ShippingAmount = @ShippingAmount, " +
            "ShippingAmountWithoutVat = @ShippingAmount, Updated = getutcdate() " +
            "WHERE [SaleInvoiceDocument].NetUID = @NetUid",
            saleInvoiceDocument
        );
    }

    public void UpdateShippingAmount(SaleInvoiceDocument saleInvoiceDocument) {
        _connection.Execute(
            "UPDATE [SaleInvoiceDocument] " +
            "SET ShippingAmountEur = @ShippingAmountEur, ShippingAmountEurWithoutVat = @ShippingAmountEurWithoutVat, Updated = getutcdate() " +
            "WHERE [SaleInvoiceDocument].NetUID = @NetUid",
            saleInvoiceDocument
        );
    }

    public void UpdateExchangeRateAmount(SaleInvoiceDocument saleInvoiceDocument) {
        _connection.Execute(
            "UPDATE [SaleInvoiceDocument] " +
            "SET ExchangeRateAmount = @ExchangeRateAmount, Updated = getutcdate() " +
            "WHERE [SaleInvoiceDocument].NetUID = @NetUid",
            saleInvoiceDocument
        );
    }

    public void UpdateExchangeRateAmountAndAmounts(SaleInvoiceDocument saleInvoiceDocument) {
        _connection.Execute(
            "UPDATE [SaleInvoiceDocument] " +
            "SET ExchangeRateAmount = @ExchangeRateAmount, ShippingAmount = @ShippingAmount, ShippingAmountEur = @ShippingAmountEur, " +
            "ShippingAmountWithoutVat = @ShippingAmountWithoutVat, ShippingAmountEurWithoutVat = @ShippingAmountEurWithoutVat, Updated = getutcdate() " +
            "WHERE [SaleInvoiceDocument].NetUID = @NetUid",
            saleInvoiceDocument
        );
    }
}