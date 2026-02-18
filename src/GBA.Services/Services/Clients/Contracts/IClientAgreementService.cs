using System;
using System.Threading.Tasks;
using GBA.Domain.Entities.Clients;

namespace GBA.Services.Services.Clients.Contracts;

public interface IClientAgreementService {
    Task AddDefaultAgreementForClient(Client client, bool isLocalPayment);

    Task<Client> UpdateSelectedClientAgreement(Guid clientNetId, Guid agreementNetId);
}