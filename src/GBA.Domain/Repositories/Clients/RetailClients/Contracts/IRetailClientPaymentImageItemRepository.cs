using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.RetailClients.Contracts;

public interface IRetailClientPaymentImageItemRepository {
    long Add(RetailClientPaymentImageItem paymentImageItem);

    void Update(RetailClientPaymentImageItem paymentImageItem);

    void Update(IEnumerable<RetailClientPaymentImageItem> paymentImageItems);

    void Remove(long id);

    RetailClientPaymentImageItem GetById(long id);

    IEnumerable<RetailClientPaymentImageItem> GetAllByRetailClientPaymentImageId(long id);
}