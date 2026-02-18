using System.Data;

namespace GBA.Domain.Repositories.AllegroServices.Contracts;

public interface IAllegroServicesRepositoriesFactory {
    IAllegroCategoryRepository NewAllegroCategoryRepository(IDbConnection connection);

    IAllegroProductReservationRepository NewAllegroProductReservationRepository(IDbConnection connection);
}