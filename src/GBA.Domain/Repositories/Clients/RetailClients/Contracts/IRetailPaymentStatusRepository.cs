using GBA.Common.Helpers;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.RetailClients.Contracts;

public interface IRetailPaymentStatusRepository {
    long Add(RetailPaymentStatus retailPaymentStatus);

    void Update(RetailPaymentStatus retailPaymentStatus);

    void SetRetailPaymentStatusTypeById(RetailPaymentStatusType type, long id);

    RetailPaymentStatus GetById(long id);

    RetailPaymentStatus GetBySaleId(long id);
}