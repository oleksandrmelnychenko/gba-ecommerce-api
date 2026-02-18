using System.Threading.Tasks;
using GBA.Domain.Entities.Clients;

namespace GBA.Services.Services.Clients.Contracts;

public interface IClientRegistrationTaskService {
    Task Add(Client client);
}