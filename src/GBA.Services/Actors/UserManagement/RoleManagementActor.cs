using Akka.Actor;
using GBA.Domain.Messages.UserManagement.RoleManagement;
using GBA.Domain.Repositories.Identities.Contracts;

namespace GBA.Services.Actors.UserManagement;

public sealed class RoleManagementActor : ReceiveActor {
    private readonly IIdentityRepositoriesFactory _identityRepositoriesFactory;

    public RoleManagementActor(IIdentityRepositoriesFactory identityRepositoriesFactory) {
        _identityRepositoriesFactory = identityRepositoriesFactory;

        Receive<GetAllRolesMessage>(_ => {
            Sender.Tell(_identityRepositoriesFactory.NewIdentityRolesRepository().GetAllExistingRoles());
        });

        ReceiveAsync<AssignRoleMessage>(async message => {
            Sender.Tell(await _identityRepositoriesFactory.NewIdentityRolesRepository().AssignUserToRole(message.UserName, message.Role));
        });

        ReceiveAsync<UnassignRoleMessage>(async message => {
            Sender.Tell(await _identityRepositoriesFactory.NewIdentityRolesRepository().UnassignUserFromRole(message.UserName, message.Role));
        });
    }
}