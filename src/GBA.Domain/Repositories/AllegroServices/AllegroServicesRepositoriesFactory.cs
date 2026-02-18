using System.Data;
using GBA.Domain.Repositories.AllegroServices.Contracts;

namespace GBA.Domain.Repositories.AllegroServices;

public class AllegroServicesRepositoriesFactory : IAllegroServicesRepositoriesFactory {
    public IAllegroCategoryRepository NewAllegroCategoryRepository(IDbConnection connection) {
        return new AllegroCategoryRepository(connection);
    }

    public IAllegroProductReservationRepository NewAllegroProductReservationRepository(IDbConnection connection) {
        return new AllegroProductReservationRepository(connection);
    }
}