using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.PaymentOrders;

public sealed class PaymentMovementTranslationRepository : IPaymentMovementTranslationRepository {
    private readonly IDbConnection _connection;

    public PaymentMovementTranslationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(PaymentMovementTranslation paymentMovementTranslation) {
        _connection.Execute(
            "INSERT INTO [PaymentMovementTranslation] (Name, PaymentMovementId, CultureCode, Updated) " +
            "VALUES (@Name, @PaymentMovementId, @CultureCode, getutcdate())",
            paymentMovementTranslation
        );
    }

    public void Update(PaymentMovementTranslation paymentMovementTranslation) {
        _connection.Execute(
            "UPDATE [PaymentMovementTranslation] " +
            "SET Name = @Name, PaymentMovementId = @PaymentMovementId, CultureCode = @CultureCode, Updated = getutcdate() " +
            "WHERE [PaymentMovementTranslation].ID = @Id",
            paymentMovementTranslation
        );
    }

    public PaymentMovementTranslation GetByPaymentMovementId(long id) {
        return _connection.Query<PaymentMovementTranslation>(
                "SELECT TOP(1) * " +
                "FROM [PaymentMovementTranslation] " +
                "WHERE [PaymentMovementTranslation].PaymentMovementID = @Id " +
                "AND [PaymentMovementTranslation].CultureCode = @Culture",
                new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }
}