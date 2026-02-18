using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Ecommerce;
using GBA.Domain.Repositories.Ecommerce.Contracts;

namespace GBA.Domain.Repositories.Ecommerce;

public sealed class EcommerceRetailPaymentTypeTranslateRepository : IEcommerceRetailPaymentTypeTranslateRepository {
    private readonly IDbConnection _dbConnection;

    public EcommerceRetailPaymentTypeTranslateRepository(IDbConnection dbConnection) {
        _dbConnection = dbConnection;
    }

    public void Add(RetailPaymentTypeTranslate retailPaymentTypeTranslate) {
        _dbConnection.Execute(
            "INSERT INTO [RetailPaymentTypeTranslate] ([LowPrice], [FullPrice], [CultureCode], [Comment], [FastOrderSuccessMessage], [ScreenshotMessage], [Updated]) " +
            "VALUES (@LowPrice, @FullPrice, @CultureCode, @Comment, @FastOrderSuccessMessage, @ScreenshotMessage, GETUTCDATE()) ",
            retailPaymentTypeTranslate
        );
    }

    public List<RetailPaymentTypeTranslate> GetAllRetailPayments() {
        return _dbConnection.Query<RetailPaymentTypeTranslate>(
            "SELECT * FROM [RetailPaymentType] " +
            "WHERE Deleted = 0 ").ToList();
    }

    public long Update(RetailPaymentTypeTranslate retailPaymentType) {
        return _dbConnection.Execute(
            "UPDATE [RetailPaymentTypeTranslate] SET " +
            "[LowPrice] = @LowPrice, " +
            "[FullPrice] = @FullPrice, " +
            "[CultureCode] = @CultureCode, " +
            "[Comment] = @Comment, " +
            "[FastOrderSuccessMessage] = @FastOrderSuccessMessage, " +
            "[ScreenshotMessage] = @ScreenshotMessage, " +
            "[Updated] = GETUTCDATE() " +
            "WHERE [NetUID] = @NetUid",
            retailPaymentType);
    }

    public RetailPaymentTypeTranslate GetByCultureCode(string code) {
        return _dbConnection.Query<RetailPaymentTypeTranslate>(
            "SELECT * FROM [RetailPaymentTypeTranslate] " +
            "WHERE [CultureCode] = @Code " +
            "AND [Deleted] = 0 ",
            new { Code = code }).FirstOrDefault();
    }

    public RetailPaymentTypeTranslate GetLast() {
        return _dbConnection.Query<RetailPaymentTypeTranslate>(
            "SELECT TOP (1) * FROM [RetailPaymentTypeTranslate] " +
            "WHERE [Deleted] = 0 ").FirstOrDefault();
    }
}