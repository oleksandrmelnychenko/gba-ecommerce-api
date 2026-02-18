using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public sealed class DeliveryExpenseRepository : IDeliveryExpenseRepository {
    private readonly IDbConnection _connection;

    public DeliveryExpenseRepository(IDbConnection connection) {
        _connection = connection;
    }

    public DeliveryExpense GetById(long id) {
        return _connection.Query<DeliveryExpense>(
            "SELECT * FROM [DeliveryExpense] " +
            "WHERE Deleted = 0 " +
            "AND ID = @Id ",
            new { Id = id }).FirstOrDefault();
    }

    public long Add(DeliveryExpense deliveryExpense) {
        return _connection.Query<long>(
            "INSERT INTO [DeliveryExpense] ([InvoiceNumber], [FromDate], [GrossAmount], [VatPercent], [AccountingGrossAmount], " +
            "[AccountingVatPercent], [SupplyOrderUkraineID], [SupplyOrganizationID], [SupplyOrganizationAgreementID], " +
            "[ConsumableProductID], [ActProvidingServiceDocumentID], [ActProvidingServiceID], [AccountingActProvidingServiceID], [UserID], [Updated]) " +
            "VALUES (@InvoiceNumber, @FromDate, @GrossAmount, @VatPercent, @AccountingGrossAmount, @AccountingVatPercent, " +
            "@SupplyOrderUkraineId, @SupplyOrganizationId, @SupplyOrganizationAgreementId, @ConsumableProductId, @ActProvidingServiceDocumentId, " +
            "@ActProvidingServiceId, @AccountingActProvidingServiceId, @UserId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY() ",
            deliveryExpense).FirstOrDefault();
    }


    public void Update(DeliveryExpense deliveryExpense) {
        _connection.Execute(
            "UPDATE [DeliveryExpense] SET [InvoiceNumber] = @InvoiceNumber, [FromDate] = @FromDate, " +
            "[GrossAmount] = @GrossAmount, [VatPercent] = @VatPercent, [AccountingGrossAmount] = @AccountingGrossAmount, " +
            "[AccountingVatPercent] = @AccountingVatPercent, [SupplyOrganizationID] = @SupplyOrganizationID, " +
            "[SupplyOrganizationAgreementID] = @SupplyOrganizationAgreementID, [ConsumableProductID] = @ConsumableProductID, [UserID] = @UserID, " +
            "[ActProvidingServiceID] = @ActProvidingServiceId, [AccountingActProvidingServiceID] = @AccountingActProvidingServiceId, [Updated] = GETUTCDATE() " +
            "WHERE [DeliveryExpense].ID = @Id ", deliveryExpense);
    }
}