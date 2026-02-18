using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientShoppingCartRepository {
    long Add(ClientShoppingCart clientShoppingCart);

    long AddAsOffer(ClientShoppingCart clientShoppingCart);

    ClientShoppingCart GetById(long id);

    ClientShoppingCart GetByNetId(Guid netId);

    ClientShoppingCart GetByClientNetId(Guid netId, bool withVat);

    ClientShoppingCart GetByClientAgreementNetId(Guid netId, bool withVat, long? workplaceId = null);

    ClientShoppingCart GetLastOfferByCulture(string culture);

    List<ClientShoppingCart> GetAllExistingUnavailableCarts();

    List<ClientShoppingCart> GetAllExistingExpiredClientShoppingCarts();

    List<ClientShoppingCart> GetAllAvailableOffersByClientNetId(Guid netId);

    List<ClientShoppingCart> GetAllValidClientShoppingCarts();

    List<ClientShoppingCart> GetAllOffersFiltered(DateTime from, DateTime to);

    void UpdateValidUntilDate(ClientShoppingCart clientShoppingCart);

    void UpdateProcessingStatus(ClientShoppingCart clientShoppingCart);

    void Remove(Guid netId);

    void SetProcessedByNetId(Guid netId);
}