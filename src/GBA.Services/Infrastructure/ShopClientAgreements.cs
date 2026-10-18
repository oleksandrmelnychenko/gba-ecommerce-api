using System;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Services.Infrastructure;

public static class ShopClientAgreements {
    public const string RequiredMessage = "A valid client agreement is required.";

    public static ClientAgreement Resolve(IClientAgreementRepository clientAgreementRepository, Guid actorNetId) {
        ArgumentNullException.ThrowIfNull(clientAgreementRepository);

        return clientAgreementRepository.GetSelectedByClientNetId(actorNetId)
               ?? clientAgreementRepository.GetSelectedByWorkplaceNetId(actorNetId)
               ?? clientAgreementRepository.GetSelectedByClientNotSelectedNetId(actorNetId);
    }

    public static (ClientAgreement ClientAgreement, Workplace Workplace) ResolveWithWorkplace(
        Guid actorNetId,
        IClientAgreementRepository clientAgreementRepository,
        IWorkplaceRepository workplaceRepository) {
        ArgumentNullException.ThrowIfNull(clientAgreementRepository);
        ArgumentNullException.ThrowIfNull(workplaceRepository);

        ClientAgreement clientAgreement = clientAgreementRepository.GetSelectedByClientNetId(actorNetId);
        Workplace workplace = null;

        if (clientAgreement == null) {
            workplace = workplaceRepository.GetByNetId(actorNetId);
            clientAgreement = workplace == null
                ? clientAgreementRepository.GetSelectedByClientNotSelectedNetId(actorNetId)
                : clientAgreementRepository.GetSelectedByWorkplaceNetId(actorNetId);
        }

        if (clientAgreement?.Agreement?.Organization == null)
            throw new ArgumentException(RequiredMessage);

        return (clientAgreement, workplace);
    }
}
