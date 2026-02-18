using Akka.Actor;
using GBA.Domain.Repositories.Identities.Contracts;

namespace GBA.Services.Actors.UserManagement;

public sealed class UserNameAvailabilityActor : ReceiveActor {
    private readonly IIdentityRepositoriesFactory _identityRepositoriesFactory;

    public UserNameAvailabilityActor(IIdentityRepositoriesFactory identityRepositoriesFactory) {
        _identityRepositoriesFactory = identityRepositoriesFactory;

        ReceiveAsync<string>(async message => {
            Sender.Tell(await _identityRepositoriesFactory.NewIdentityRepository().IsUserNameAvailable(message));
        });
    }
}