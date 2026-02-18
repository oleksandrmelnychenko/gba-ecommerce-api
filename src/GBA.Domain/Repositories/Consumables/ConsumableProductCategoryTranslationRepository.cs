using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Repositories.Consumables.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Consumables;

public sealed class ConsumableProductCategoryTranslationRepository : IConsumableProductCategoryTranslationRepository {
    private readonly IDbConnection _connection;

    public ConsumableProductCategoryTranslationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ConsumableProductCategoryTranslation consumableProductCategoryTranslation) {
        _connection.Execute(
            "INSERT INTO [ConsumableProductCategoryTranslation] (Name, Description, ConsumableProductCategoryId, CultureCode, Updated) " +
            "VALUES (@Name, @Description, @ConsumableProductCategoryId, @CultureCode, getutcdate())",
            consumableProductCategoryTranslation
        );
    }

    public void Update(ConsumableProductCategoryTranslation consumableProductCategoryTranslation) {
        _connection.Execute(
            "UPDATE [ConsumableProductCategoryTranslation] " +
            "SET Name = @Name, Description = @Description, ConsumableProductCategoryId = @ConsumableProductCategoryId, CultureCode = @CultureCode, Updated = getutcdate() " +
            "WHERE [ConsumableProductCategoryTranslation].ID = @Id",
            consumableProductCategoryTranslation
        );
    }

    public ConsumableProductCategoryTranslation GetByConsumableProductCategoryId(long id) {
        return _connection.Query<ConsumableProductCategoryTranslation>(
                "SELECT TOP(1) * " +
                "FROM [ConsumableProductCategoryTranslation] " +
                "WHERE [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = @Id " +
                "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture",
                new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }
}