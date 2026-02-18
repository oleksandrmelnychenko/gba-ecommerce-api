using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Repositories.Consumables.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Consumables;

public sealed class ConsumableProductTranslationRepository : IConsumableProductTranslationRepository {
    private readonly IDbConnection _connection;

    public ConsumableProductTranslationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ConsumableProductTranslation consumableProductTranslation) {
        _connection.Execute(
            "INSERT INTO [ConsumableProductTranslation] (Name, ConsumableProductId, CultureCode, Updated) " +
            "VALUES (@Name, @ConsumableProductId, @CultureCode, getutcdate())",
            consumableProductTranslation
        );
    }

    public void Update(ConsumableProductTranslation consumableProductTranslation) {
        _connection.Execute(
            "UPDATE [ConsumableProductTranslation] " +
            "SET Name = @Name, ConsumableProductId = @ConsumableProductId, CultureCode = @CultureCode, Updated = getutcdate() " +
            "WHERE [ConsumableProductTranslation].ID = @Id",
            consumableProductTranslation
        );
    }

    public ConsumableProductTranslation GetByConsumableProductId(long id) {
        return _connection.Query<ConsumableProductTranslation>(
                "SELECT * " +
                "FROM [ConsumableProductTranslation] " +
                "WHERE [ConsumableProductTranslation].ConsumableProductID = @Id " +
                "AND [ConsumableProductTranslation].CultureCode = @Culture",
                new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }
}