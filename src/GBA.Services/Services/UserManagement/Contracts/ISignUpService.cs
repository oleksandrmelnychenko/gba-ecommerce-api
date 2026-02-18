using System;
using System.Threading.Tasks;
using GBA.Domain.Entities.Clients;
using GBA.Domain.EntityHelpers;

namespace GBA.Services.Services.UserManagement.Contracts;

public interface ISignUpService {
    Task<Tuple<IdentityResponse, Client>> SignUp(Client clientProfile, string password, string login, bool isLocalPayment);
}