using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.PaymentOrders;

public sealed class PaymentCostMovementTranslationRepository : IPaymentCostMovementTranslationRepository {
    private readonly IDbConnection _connection;

    public PaymentCostMovementTranslationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(PaymentCostMovementTranslation paymentMovementTranslation) {
        _connection.Execute(
            "INSERT INTO [PaymentCostMovementTranslation] (OperationName, PaymentCostMovementId, CultureCode, Updated) " +
            "VALUES (@OperationName, @PaymentCostMovementId, @CultureCode, getutcdate())",
            paymentMovementTranslation
        );
    }

    public void Update(PaymentCostMovementTranslation paymentMovementTranslation) {
        _connection.Execute(
            "UPDATE [PaymentCostMovementTranslation] " +
            "SET OperationName = @OperationName, PaymentCostMovementID = @PaymentCostMovementID, CultureCode = @CultureCode, Updated = getutcdate() " +
            "WHERE [PaymentCostMovementTranslation].ID = @Id",
            paymentMovementTranslation
        );
    }

    public PaymentCostMovementTranslation GetByPaymentMovementId(long id) {
        return _connection.Query<PaymentCostMovementTranslation>(
                "SELECT TOP(1) * " +
                "FROM [PaymentCostMovementTranslation] " +
                "WHERE [PaymentCostMovementTranslation].PaymentCostMovementID = @Id " +
                "AND [PaymentCostMovementTranslation].CultureCode = @Culture",
                new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }
}