using System;
using System.Threading.Tasks;
using GBA.Domain.Entities.Clients;

namespace GBA.Services.Services.Clients.Contracts;

public interface IClientService {
    Task<Client> GetByNetId(Guid netId);

    Task<Client> GetRootClientBySubClientNerId(Guid netId);

    Task<RetailClient> GetRetailClientByNetId(Guid netId);
    Task<(RetailClient, string)> GetRetailClientByNetIdCheckOrderItems(Guid netId);

    Task<RetailClient> AddRetailClient(RetailClient client);
}