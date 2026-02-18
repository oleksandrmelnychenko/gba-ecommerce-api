using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Repositories.Consumables.Contracts;

namespace GBA.Domain.Repositories.Consumables;

public sealed class CompanyCarFuelingRepository : ICompanyCarFuelingRepository {
    private readonly IDbConnection _connection;

    public CompanyCarFuelingRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(CompanyCarFueling companyCarFueling) {
        return _connection.Query<long>(
                "INSERT INTO [CompanyCarFueling] " +
                "(FuelAmount, PricePerLiter, TotalPrice, CompanyCarID, OutcomePaymentOrderID, ConsumableProductOrganizationID, SupplyOrganizationAgreementID, VatPercent, VatAmount, UserID, TotalPriceWithVat, Updated) " +
                "VALUES (@FuelAmount, @PricePerLiter, @TotalPrice, @CompanyCarId, @OutcomePaymentOrderId, @ConsumableProductOrganizationId, @SupplyOrganizationAgreementId, @VatPercent, @VatAmount, @UserId, " +
                "@TotalPriceWithVat, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                companyCarFueling
            )
            .Single();
    }

    public void Add(IEnumerable<CompanyCarFueling> companyCarFuelings) {
        _connection.Execute(
            "INSERT INTO [CompanyCarFueling] " +
            "(FuelAmount, PricePerLiter, TotalPrice, CompanyCarId, OutcomePaymentOrderId, ConsumableProductOrganizationId, VatPercent, VatAmount, UserId, TotalPriceWithVat, Updated) " +
            "VALUES (@FuelAmount, @PricePerLiter, @TotalPrice, @CompanyCarId, @OutcomePaymentOrderId, @ConsumableProductOrganizationId, @VatPercent, @VatAmount, @UserId, " +
            "@TotalPriceWithVat, GETUTCDATE())",
            companyCarFuelings
        );
    }

    public void Update(CompanyCarFueling companyCarFueling) {
        _connection.Execute(
            "UPDATE [CompanyCarFueling] " +
            "SET FuelAmount = @FuelAmount, PricePerLiter = @PricePerLiter, TotalPrice = @TotalPrice, CompanyCarId = @CompanyCarId, VatPercent = @VatPercent, VatAmount = @VatAmount, " +
            "OutcomePaymentOrderId = @OutcomePaymentOrderId, ConsumableProductOrganizationId = @ConsumableProductOrganizationId, UserId = @UserId, " +
            "TotalPriceWithVat = @TotalPriceWithVat, Updated = getutcdate() " +
            "WHERE [CompanyCarFueling].ID = @Id",
            companyCarFueling
        );
    }

    public void Update(IEnumerable<CompanyCarFueling> companyCarFuelings) {
        _connection.Execute(
            "UPDATE [CompanyCarFueling] " +
            "SET FuelAmount = @FuelAmount, PricePerLiter = @PricePerLiter, TotalPrice = @TotalPrice, CompanyCarId = @CompanyCarId, VatPercent = @VatPercent, VatAmount = @VatAmount, " +
            "OutcomePaymentOrderId = @OutcomePaymentOrderId, ConsumableProductOrganizationId = @ConsumableProductOrganizationId, UserId = @UserId, " +
            "TotalPriceWithVat = @TotalPriceWithVat, Updated = getutcdate() " +
            "WHERE [CompanyCarFueling].ID = @Id",
            companyCarFuelings
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [CompanyCarFueling] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [CompanyCarFueling].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public CompanyCarFueling GetById(long id) {
        return _connection.Query<CompanyCarFueling>(
                "SELECT * " +
                "FROM [CompanyCarFueling] " +
                "WHERE [CompanyCarFueling].ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }
}