using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.RetailClients.Contracts;

public interface IRetailClientRepository {
    long Add(RetailClient client);

    void Update(RetailClient retailClient);

    RetailClient GetByPhoneNumber(string number);

    RetailClient GetByNetId(Guid netId);

    RetailClient GetRetailClientById(long id);

    IEnumerable<RetailClient> GetAll();

    IEnumerable<RetailClient> GetAllFiltered(string value, long limit, long offset);
}