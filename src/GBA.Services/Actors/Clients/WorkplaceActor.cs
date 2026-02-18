using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using GBA.Common.IdentityConfiguration.Roles;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.EntityHelpers;
using GBA.Domain.IdentityEntities;
using GBA.Domain.Messages.Clients;
using GBA.Domain.Messages.Workplaces;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Identities.Contracts;

namespace GBA.Services.Actors.Clients;

public sealed class WorkplaceActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IIdentityRepositoriesFactory _identityRepositoriesFactory;

    public WorkplaceActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IIdentityRepositoriesFactory identityRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _identityRepositoriesFactory = identityRepositoriesFactory;

        ReceiveAsync<AddWorkplaceMessage>(ProcessAddWorkplaceMessage);

        ReceiveAsync<UpdateWorkplaceMessage>(ProcessUpdateWorkplaceMessage);

        ReceiveAsync<DeleteWorkplaceMessage>(ProcessDeleteWorkplaceMessage);
    }

    private async Task ProcessUpdateWorkplaceMessage(UpdateWorkplaceMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            if (message.Workplace == null || message.Workplace.IsNew()) {
                Sender.Tell(new Tuple<List<Workplace>, string>(new List<Workplace>(), "Workplace cannot be null"));
            } else {
                IWorkplaceRepository workplaceRepository = _clientRepositoriesFactory.NewWorkplaceRepository(connection);
                IIdentityRepository identityRepository = _identityRepositoriesFactory.NewIdentityRepository();

                Workplace workplaceToUpdate = message.Workplace;

                Workplace workplaceFromDb = workplaceRepository.GetById(workplaceToUpdate.Id);

                if (!workplaceToUpdate.Email.Equals(workplaceFromDb.Email)) {
                    UserIdentity user = await identityRepository.GetUserByNetId(workplaceFromDb.NetUid.ToString());
                    user.Email = workplaceToUpdate.Email;
                    user.UserName = workplaceToUpdate.Email;

                    await identityRepository.UpdateUserName(user);
                }

                if (!workplaceToUpdate.FirstName.Equals(workplaceFromDb.FirstName) || !workplaceToUpdate.LastName.Equals(workplaceFromDb.LastName)) {
                    workplaceToUpdate.Abbreviation = string.Empty;
                    if (!string.IsNullOrEmpty(workplaceToUpdate.LastName)) {
                        char[] chars = workplaceToUpdate.LastName.ToCharArray();

                        if (chars.Any()) workplaceToUpdate.Abbreviation += chars.First();
                    }

                    if (!string.IsNullOrEmpty(workplaceToUpdate.FirstName)) {
                        char[] chars = workplaceToUpdate.FirstName.ToCharArray();

                        if (chars.Any()) workplaceToUpdate.Abbreviation += chars.First();
                    }
                }

                List<WorkplaceClientAgreement> workplaceClientAgreementsFromDb =
                    workplaceRepository.GetWorkplaceClientAgreementsByWorkplaceId(workplaceToUpdate.Id).ToList();

                foreach (WorkplaceClientAgreement workplaceClientAgreement in workplaceClientAgreementsFromDb) {
                    if (workplaceToUpdate.WorkplaceClientAgreements.Any(e => e.Id.Equals(workplaceClientAgreement.Id))) continue;

                    workplaceClientAgreement.Deleted = true;
                    workplaceRepository.RemoveWorkplaceClientAgreementById(workplaceClientAgreement.Id);
                }

                List<WorkplaceClientAgreement> workplaceClientAgreementsToUpdate = workplaceToUpdate.WorkplaceClientAgreements.ToList();

                foreach (WorkplaceClientAgreement workplaceClientAgreement in
                         workplaceClientAgreementsToUpdate.Where(e => e.IsNew() && e.NetUid.Equals(Guid.Empty))) {
                    if (!workplaceToUpdate.WorkplaceClientAgreements.Any(e => e.IsSelected && !e.Deleted))
                        workplaceClientAgreement.IsSelected = true;

                    workplaceClientAgreement.WorkplaceId = workplaceToUpdate.Id;
                    workplaceClientAgreement.ClientAgreementId = workplaceClientAgreement.ClientAgreement.Id;

                    workplaceClientAgreement.Id = workplaceRepository.AddWorkplaceClientAgreement(workplaceClientAgreement);
                }

                if (workplaceClientAgreementsToUpdate.Count > 0 && !workplaceClientAgreementsToUpdate.Any(e => e.IsSelected && !e.Deleted)) {
                    WorkplaceClientAgreement workplaceClientAgreement = workplaceClientAgreementsToUpdate.First(e => !e.Deleted);
                    workplaceClientAgreement.IsSelected = true;

                    workplaceRepository.UpdateWorkplaceClientAgreement(workplaceClientAgreement);
                }

                workplaceRepository.Update(workplaceToUpdate);

                Sender.Tell(new Tuple<List<Workplace>, string>(workplaceRepository.GetWorkplacesByMainClientId(workplaceToUpdate.MainClientId).ToList(), null));
            }
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private async Task ProcessDeleteWorkplaceMessage(DeleteWorkplaceMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            if (message.NetId.Equals(Guid.Empty)) {
                Sender.Tell(new Tuple<List<Workplace>, string>(new List<Workplace>(), "NetId cannot be null"));
            } else {
                IWorkplaceRepository workplaceRepository = _clientRepositoriesFactory.NewWorkplaceRepository(connection);
                IIdentityRepository identityRepository = _identityRepositoriesFactory.NewIdentityRepository();

                Workplace workplace = workplaceRepository.GetByNetId(message.NetId);

                await identityRepository.DisableUser(workplace.NetUid);

                workplaceRepository.DisableById(workplace.Id);

                Sender.Tell(new Tuple<List<Workplace>, string>(workplaceRepository.GetWorkplacesByMainClientId(workplace.MainClientId).ToList(), null));
            }
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private async Task ProcessAddWorkplaceMessage(AddWorkplaceMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            IWorkplaceRepository workplaceRepository = _clientRepositoriesFactory.NewWorkplaceRepository(connection);
            IIdentityRepository identityRepository = _identityRepositoriesFactory.NewIdentityRepository();

            Workplace workplace = message.Workplace;

            if (!string.IsNullOrEmpty(workplace.LastName)) {
                char[] chars = workplace.LastName.ToCharArray();

                if (chars.Any()) workplace.Abbreviation += chars.First();
            }

            if (!string.IsNullOrEmpty(workplace.FirstName)) {
                char[] chars = workplace.FirstName.ToCharArray();

                if (chars.Any()) workplace.Abbreviation += chars.First();
            }

            if (string.IsNullOrEmpty(workplace.Region)) workplace.Region = "uk";

            workplace.Id = workplaceRepository.AddWorkplace(workplace);

            foreach (WorkplaceClientAgreement workplaceClientAgreement in workplace.WorkplaceClientAgreements) {
                if (!workplace.WorkplaceClientAgreements.Any(e => e.IsSelected))
                    workplaceClientAgreement.IsSelected = true;

                workplaceClientAgreement.WorkplaceId = workplace.Id;
                workplaceClientAgreement.ClientAgreementId = workplaceClientAgreement.ClientAgreement.Id;

                workplaceRepository.AddWorkplaceClientAgreement(workplaceClientAgreement);
            }

            string password = workplace.Password;

            workplace = workplaceRepository.GetById(workplace.Id);

            UserIdentity user = new() {
                Email = workplace.Email,
                UserName = !string.IsNullOrEmpty(workplace.Email)
                    ? workplace.Email
                    : workplace.PhoneNumber,
                PhoneNumber = workplace.PhoneNumber,
                NetId = workplace.NetUid,
                Region = workplace.Region,
                UserType = IdentityUserType.Workplace
            };

            IdentityResponse response = await identityRepository.CreateUser(user, password, false);

            if (response.Succeeded)
                await identityRepository.AddUserRoleAndClaims(user, IdentityRoles.Workplace);
            else
                workplaceRepository.RemoveById(workplace.Id);

            Sender.Tell(new Tuple<IdentityResponse, IEnumerable<Workplace>>(response, workplaceRepository.GetWorkplacesByMainClientId(workplace.MainClientId)));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}